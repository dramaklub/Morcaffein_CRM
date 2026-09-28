using CRM_Morcaffein.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRM_Morcaffein.infrastructure.services
{
    /// <summary>
    /// Background service that runs the sync pipeline on a timer. Every
    /// IntervalSeconds it:
    ///
    ///   1. Syncs the master (once per tick — not per company).
    ///   2. Syncs each active company's tenant DB sequentially.
    ///   3. Updates the SyncStatusStore for each company.
    ///   4. Logs a one-line summary per target.
    ///
    /// Concurrency: a SemaphoreSlim(1,1) skips the tick if the previous tick
    /// is still running. If a cloud round-trip takes longer than
    /// IntervalSeconds, we don't want overlapping passes stomping each other
    /// on the same DbContext instance.
    ///
    /// Scoping: the worker is a singleton, but SyncService depends on Scoped
    /// services (MasterCrmDbContext, the tenant factories). Each tick creates
    /// a fresh IServiceScope, resolves SyncService from it, runs, and disposes.
    ///
    /// Errors: nothing escapes the tick loop. SyncService already captures
    /// exceptions into result objects; the worker's own try/catch is a belt-
    /// and-suspenders for bugs in the service itself. Every failure is logged
    /// and written to SyncStatusStore, which the client polls for the topbar
    /// indicator.
    /// </summary>
    public class SyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SyncStatusStore _statusStore;
        private readonly SyncOptions _options;
        private readonly ILogger<SyncWorker> _logger;

        private readonly SemaphoreSlim _tickGate = new(1, 1);

        public SyncWorker(
            IServiceScopeFactory scopeFactory,
            SyncStatusStore statusStore,
            IOptions<SyncOptions> options,
            ILogger<SyncWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _statusStore = statusStore;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("[Sync] Worker disabled via configuration. Idling.");
                return;
            }

            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds));
            _logger.LogInformation("[Sync] Worker started. Interval = {Interval}s.", interval.TotalSeconds);

            // Initial delay before the first tick. Prevents a sync storm on
            // API boot while other startup work is still settling.
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using var timer = new PeriodicTimer(interval);

            // Run one tick immediately, then wait for the timer.
            await RunTickAsync(stoppingToken);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunTickAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }

            _logger.LogInformation("[Sync] Worker stopped.");
        }

        private async Task RunTickAsync(CancellationToken ct)
        {
            // Skip this tick if the previous one is still in flight.
            if (!await _tickGate.WaitAsync(0, ct))
            {
                _logger.LogDebug("[Sync] Previous tick still running; skipping this one.");
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();

                // ---- Master sync (once per tick) ----
                await RunMasterSyncAsync(syncService, ct);

                // ---- Tenant sync (sequential per company) ----
                var companyIds = await GetActiveCompanyIdsAsync(scope.ServiceProvider, ct);
                foreach (var companyId in companyIds)
                {
                    if (ct.IsCancellationRequested) break;
                    await RunTenantSyncAsync(syncService, companyId, ct);
                }
            }
            catch (Exception ex)
            {
                // Should never happen — SyncService captures its own exceptions.
                // If it does, log it but don't kill the worker.
                _logger.LogError(ex, "[Sync] Tick failed unexpectedly.");
            }
            finally
            {
                _tickGate.Release();
            }
        }

        private async Task RunMasterSyncAsync(SyncService syncService, CancellationToken ct)
        {
            var result = await syncService.RunMasterAsync(ct);

            if (result.Success)
            {
                _logger.LogInformation(
                    "[Sync] Master: OK. Synced {Synced}, skipped {Skipped}, took {Ms}ms.",
                    result.TotalRowsSynced,
                    result.TotalRowsSkipped,
                    (result.FinishedAt - result.StartedAt).TotalMilliseconds);

                _statusStore.UpdateMaster(s =>
                {
                    s.IsOnline = true;
                    s.LastSyncedAt = result.FinishedAt;
                    s.LastError = null;
                    s.PendingCount = 0;
                    s.CloudHost = null;   // master host is not per-company
                });
            }
            else
            {
                _logger.LogWarning(
                    "[Sync] Master: FAILED — {Error}",
                    result.Error);

                _statusStore.UpdateMaster(s =>
                {
                    s.IsOnline = false;
                    s.LastError = result.Error;
                    s.LastSyncedAt = result.FinishedAt;
                });
            }
        }

        private async Task RunTenantSyncAsync(
            SyncService syncService, int companyId, CancellationToken ct)
        {
            var result = await syncService.RunTenantAsync(companyId, ct);

            // Count pending rows AFTER the sync, so the indicator reflects the
            // state the client should see right now.
            int pending;
            string? cloudHost;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var master = scope.ServiceProvider.GetRequiredService<MasterCrmDbContext>();

                var row = await master.CompanyDatabases
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cd => cd.CompanyId == companyId && cd.IsActive, ct);

                cloudHost = row?.CloudServerName;

                if (row is null || row.CloudServerName is null)
                {
                    pending = 0;   // no cloud replica — nothing counts as pending
                }
                else
                {
                    // Count unsynced rows across all synced tables. Sum across
                    // 12 COUNTs — cheap on LocalDB, and gives an accurate
                    // "pending" number for the indicator.
                    var tenantFactory = scope.ServiceProvider.GetRequiredService<ITenantDbContextFactory>();
                    await using var tenant = await tenantFactory.CreateAsync(companyId);

                    pending =
                        await tenant.Branches.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.Employees.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.Credentials.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.Customers.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.Campaigns.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.Orders.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.OrderItems.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.OrderItemModifiers.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.CampaignRedeems.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.PointLedgers.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.Feedbacks.CountAsync(x => !x.IsSynced, ct) +
                        await tenant.PointAdjustmentRequests.CountAsync(x => !x.IsSynced, ct);
                }
            }
            catch (Exception ex)
            {
                // Count failed — don't kill the tick. Report pending as -1 so
                // the client can distinguish "unknown" from "zero".
                _logger.LogWarning(ex,
                    "[Sync] Company {CompanyId}: pending count failed.", companyId);
                pending = -1;
                cloudHost = null;
            }

            if (result.Success)
            {
                _logger.LogInformation(
                    "[Sync] Company {CompanyId}: OK. Synced {Synced}, skipped {Skipped}, pending {Pending}, took {Ms}ms.",
                    companyId,
                    result.TotalRowsSynced,
                    result.TotalRowsSkipped,
                    pending,
                    (result.FinishedAt - result.StartedAt).TotalMilliseconds);

                _statusStore.UpdateCompany(companyId, s =>
                {
                    s.IsOnline = true;
                    s.LastSyncedAt = result.FinishedAt;
                    s.LastError = null;
                    s.PendingCount = pending;
                    s.CloudHost = cloudHost;
                });
            }
            else
            {
                _logger.LogWarning(
                    "[Sync] Company {CompanyId}: FAILED — {Error}",
                    companyId, result.Error);

                _statusStore.UpdateCompany(companyId, s =>
                {
                    s.IsOnline = false;
                    s.LastError = result.Error;
                    s.LastSyncedAt = result.FinishedAt;
                    s.PendingCount = pending;
                    s.CloudHost = cloudHost;
                });
            }
        }

        private static async Task<List<int>> GetActiveCompanyIdsAsync(
            IServiceProvider scopedProvider, CancellationToken ct)
        {
            var master = scopedProvider.GetRequiredService<MasterCrmDbContext>();
            return await master.Companies
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyId)
                .Select(c => c.CompanyId)
                .ToListAsync(ct);
        }
    }
}