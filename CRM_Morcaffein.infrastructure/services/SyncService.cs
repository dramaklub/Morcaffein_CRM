using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.data;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.infrastructure.services
{
    /// <summary>
    /// Pushes unsynced rows from the local DBs to their cloud replicas.
    ///
    /// Tenant sync:
    ///   - Reads CompanyDatabase.CloudServerName / CloudDatabaseName from the
    ///     LOCAL master to find the target.
    ///   - Opens a cloud TenantCrmDbContext via ICloudTenantDbContextFactory.
    ///   - For each synced table (FK-safe order), reads local rows with
    ///     IsSynced = 0, inserts any missing ones on cloud (skip-if-exists by
    ///     PK), marks every processed local row IsSynced = 1.
    ///   - One-way, insert-only, no conflict resolution.
    ///
    /// Master sync:
    ///   - Reads local master entities with IsSynced = 0.
    ///   - Inserts missing ones into cloud master via ICloudMasterDbContextFactory.
    ///   - Marks local rows IsSynced = 1.
    ///
    /// All failures are captured and returned in the result object — the
    /// worker logs and retries on the next tick. Nothing is rolled back; the
    /// local DB is the source of truth.
    /// </summary>
    public class SyncService
    {
        private readonly MasterCrmDbContext _localMaster;
        private readonly ITenantDbContextFactory _localTenantFactory;
        private readonly ICloudTenantDbContextFactory _cloudTenantFactory;
        private readonly ICloudMasterDbContextFactory _cloudMasterFactory;

        private const int BatchSize = 100;

        public SyncService(
            MasterCrmDbContext localMaster,
            ITenantDbContextFactory localTenantFactory,
            ICloudTenantDbContextFactory cloudTenantFactory,
            ICloudMasterDbContextFactory cloudMasterFactory)
        {
            _localMaster = localMaster;
            _localTenantFactory = localTenantFactory;
            _cloudTenantFactory = cloudTenantFactory;
            _cloudMasterFactory = cloudMasterFactory;
        }

        // =====================================================================
        // TENANT SYNC
        // =====================================================================

        public async Task<TenantSyncResult> RunTenantAsync(int companyId, CancellationToken ct = default)
        {
            var result = new TenantSyncResult
            {
                CompanyId = companyId,
                StartedAt = DateTime.UtcNow,
            };

            // Skip if there's no cloud replica configured.
            var hasCloudReplica = await _localMaster.CompanyDatabases
                .AsNoTracking()
                .AnyAsync(cd => cd.CompanyId == companyId
                             && cd.IsActive
                             && cd.CloudServerName != null
                             && cd.CloudDatabaseName != null, ct);

            if (!hasCloudReplica)
            {
                result.Success = true;   // not an error — no replica, nothing to do
                result.FinishedAt = DateTime.UtcNow;
                return result;
            }

            try
            {
                await using var local = await _localTenantFactory.CreateAsync(companyId);
                await using var cloud = await _cloudTenantFactory.CreateAsync(companyId);

                // FK-safe order: parents before children.
                await PushBranchesAsync(local, cloud, result, ct);
                await PushEmployeesAsync(local, cloud, result, ct);
                await PushCredentialsAsync(local, cloud, result, ct);
                await PushCustomersAsync(local, cloud, result, ct);
                await PushCampaignsAsync(local, cloud, result, ct);
                await PushOrdersAsync(local, cloud, result, ct);
                await PushOrderItemsAsync(local, cloud, result, ct);
                await PushOrderItemModifiersAsync(local, cloud, result, ct);
                await PushCampaignRedeemsAsync(local, cloud, result, ct);
                await PushPointLedgersAsync(local, cloud, result, ct);
                await PushFeedbacksAsync(local, cloud, result, ct);
                await PushPointAdjustmentRequestsAsync(local, cloud, result, ct);

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;

                // EF wraps DbUpdateException. The interesting bit is the innermost
                // inner exception (the raw SQL error, e.g. "Violation of UNIQUE KEY
                // constraint 'IX_CompanyUsers_Username'").
                var inner = ex;
                while (inner.InnerException is not null)
                    inner = inner.InnerException;

                result.Error = inner == ex
                    ? ex.Message
                    : $"{ex.Message} → {inner.Message}";
            }

            result.FinishedAt = DateTime.UtcNow;
            return result;
        }

        // ---- Per-table push methods ----

        private static async Task PushBranchesAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.Branches
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.BranchId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.BranchId).ToList();
            var existing = await cloud.Branches
                .Where(x => ids.Contains(x.BranchId))
                .Select(x => x.BranchId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.BranchId)) { skipped++; }
                else { cloud.Branches.Add(CloneBranch(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "Branches", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "Branches", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushEmployeesAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.Employees
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.EmployeeId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.EmployeeId).ToList();
            var existing = await cloud.Employees
                .Where(x => ids.Contains(x.EmployeeId))
                .Select(x => x.EmployeeId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.EmployeeId)) { skipped++; }
                else { cloud.Employees.Add(CloneEmployee(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "Employees", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "Employees", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushCredentialsAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.Credentials
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.CredentialId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.CredentialId).ToList();
            var existing = await cloud.Credentials
                .Where(x => ids.Contains(x.CredentialId))
                .Select(x => x.CredentialId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.CredentialId)) { skipped++; }
                else { cloud.Credentials.Add(CloneCredential(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "Credentials", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "Credentials", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushCustomersAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.Customers
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.CustomerId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.CustomerId).ToList();
            var existing = await cloud.Customers
                .Where(x => ids.Contains(x.CustomerId))
                .Select(x => x.CustomerId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.CustomerId)) { skipped++; }
                else { cloud.Customers.Add(CloneCustomer(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "Customers", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "Customers", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushCampaignsAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.Campaigns
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.CampaignId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.CampaignId).ToList();
            var existing = await cloud.Campaigns
                .Where(x => ids.Contains(x.CampaignId))
                .Select(x => x.CampaignId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.CampaignId)) { skipped++; }
                else { cloud.Campaigns.Add(CloneCampaign(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "Campaigns", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "Campaigns", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushOrdersAsync(
    TenantCrmDbContext local, TenantCrmDbContext cloud,
    TenantSyncResult result, CancellationToken ct)
        {
            // Loop until drained. Orders has child tables (OrderItems,
            // CampaignRedeems). Child inserts will fail if the parent
            // Order isn't on cloud yet, so push every pending Order in
            // this tick rather than a single 100-row batch.
            int inserted = 0, skipped = 0;

            while (true)
            {
                var rows = await local.Orders
                    .Where(x => !x.IsSynced)
                    .OrderBy(x => x.OrderId)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (rows.Count == 0) break;

                var ids = rows.Select(r => r.OrderId).ToList();
                var existing = await cloud.Orders
                    .Where(x => ids.Contains(x.OrderId))
                    .Select(x => x.OrderId)
                    .ToListAsync(ct);

                foreach (var r in rows)
                {
                    if (existing.Contains(r.OrderId)) { skipped++; }
                    else { cloud.Orders.Add(CloneOrder(r)); inserted++; }
                }

                await SaveWithIdentityInsertAsync(cloud, "Orders", ct);

                foreach (var r in rows) r.IsSynced = true;
                await local.SaveChangesAsync(ct);

                if (rows.Count < BatchSize) break;
            }

            if (inserted == 0 && skipped == 0) return;

            result.Tables.Add(new TableSyncCount { TableName = "Orders", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushOrderItemsAsync(
    TenantCrmDbContext local, TenantCrmDbContext cloud,
    TenantSyncResult result, CancellationToken ct)
        {
            // Loop until drained. OrderItems has a child table
            // (OrderItemModifiers) whose FK constraint requires every
            // referenced OrderItem to exist on cloud before modifiers can
            // push. A single 100-row batch isn't enough when there are
            // hundreds pending — modifiers would fail on the not-yet-pushed
            // parents. Looping drains the parent table in one tick.
            int inserted = 0, skipped = 0;

            while (true)
            {
                var rows = await local.OrderItems
                    .Where(x => !x.IsSynced)
                    .OrderBy(x => x.OrderItemId)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (rows.Count == 0) break;

                var ids = rows.Select(r => r.OrderItemId).ToList();
                var existing = await cloud.OrderItems
                    .Where(x => ids.Contains(x.OrderItemId))
                    .Select(x => x.OrderItemId)
                    .ToListAsync(ct);

                foreach (var r in rows)
                {
                    if (existing.Contains(r.OrderItemId)) { skipped++; }
                    else { cloud.OrderItems.Add(CloneOrderItem(r)); inserted++; }
                }

                await SaveWithIdentityInsertAsync(cloud, "OrderItems", ct);

                foreach (var r in rows) r.IsSynced = true;
                await local.SaveChangesAsync(ct);

                // If fewer than a full batch came back, we've drained it.
                if (rows.Count < BatchSize) break;
            }

            if (inserted == 0 && skipped == 0) return;   // nothing was pending

            result.Tables.Add(new TableSyncCount { TableName = "OrderItems", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushOrderItemModifiersAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.OrderItemModifiers
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.OrderItemModifierId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.OrderItemModifierId).ToList();
            var existing = await cloud.OrderItemModifiers
                .Where(x => ids.Contains(x.OrderItemModifierId))
                .Select(x => x.OrderItemModifierId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.OrderItemModifierId)) { skipped++; }
                else { cloud.OrderItemModifiers.Add(CloneOrderItemModifier(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "OrderItemModifiers", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "OrderItemModifiers", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushCampaignRedeemsAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.CampaignRedeems
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.RedeemId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.RedeemId).ToList();
            var existing = await cloud.CampaignRedeems
                .Where(x => ids.Contains(x.RedeemId))
                .Select(x => x.RedeemId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.RedeemId)) { skipped++; }
                else { cloud.CampaignRedeems.Add(CloneCampaignRedeem(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "CampaignRedeems", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "CampaignRedeems", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushPointLedgersAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.PointLedgers
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.PointLedgerId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.PointLedgerId).ToList();
            var existing = await cloud.PointLedgers
                .Where(x => ids.Contains(x.PointLedgerId))
                .Select(x => x.PointLedgerId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.PointLedgerId)) { skipped++; }
                else { cloud.PointLedgers.Add(ClonePointLedger(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "PointLedgers", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "PointLedgers", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushFeedbacksAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.Feedbacks
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.TicketId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.TicketId).ToList();
            var existing = await cloud.Feedbacks
                .Where(x => ids.Contains(x.TicketId))
                .Select(x => x.TicketId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.TicketId)) { skipped++; }
                else { cloud.Feedbacks.Add(CloneFeedback(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "Feedbacks", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "Feedbacks", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        private static async Task PushPointAdjustmentRequestsAsync(
            TenantCrmDbContext local, TenantCrmDbContext cloud,
            TenantSyncResult result, CancellationToken ct)
        {
            var rows = await local.PointAdjustmentRequests
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.PointAdjustmentRequestId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.PointAdjustmentRequestId).ToList();
            var existing = await cloud.PointAdjustmentRequests
                .Where(x => ids.Contains(x.PointAdjustmentRequestId))
                .Select(x => x.PointAdjustmentRequestId)
                .ToListAsync(ct);

            // Phase 1 — build the insert list and push to cloud FIRST.
            // If this throws, local stays IsSynced = 0 and the next tick retries.
            int inserted = 0, skipped = 0;
            foreach (var r in rows)
            {
                if (existing.Contains(r.PointAdjustmentRequestId)) { skipped++; }
                else { cloud.PointAdjustmentRequests.Add(ClonePointAdjustmentRequest(r)); inserted++; }
            }

            await SaveWithIdentityInsertAsync(cloud, "PointAdjustmentRequests", ct);

            // Phase 2 — only after cloud accepted, mark local as synced.
            foreach (var r in rows) r.IsSynced = true;
            await local.SaveChangesAsync(ct);

            result.Tables.Add(new TableSyncCount { TableName = "PointAdjustmentRequests", Inserted = inserted, Skipped = skipped });
            result.TotalRowsSynced += inserted;
            result.TotalRowsSkipped += skipped;
        }

        // =====================================================================
        // MASTER SYNC
        // =====================================================================

        public async Task<MasterSyncResult> RunMasterAsync(CancellationToken ct = default)
        {
            var result = new MasterSyncResult { StartedAt = DateTime.UtcNow };

            try
            {
                using var cloudMaster = _cloudMasterFactory.Create();

                // FK-safe: Companies first, then Plans, then CompanyDatabases,
                // CompanyUsers, Subscriptions, TermsAndConditions, SuperAdmins.
                await PushMasterCompaniesAsync(cloudMaster, result, ct);
                await PushMasterPlansAsync(cloudMaster, result, ct);
                await PushMasterCompanyDatabasesAsync(cloudMaster, result, ct);
                await PushMasterSuperAdminsAsync(cloudMaster, result, ct);
                await PushMasterCompanyUsersAsync(cloudMaster, result, ct);
                await PushMasterSubscriptionsAsync(cloudMaster, result, ct);
                await PushMasterTermsAsync(cloudMaster, result, ct);

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;

                // EF wraps DbUpdateException. The interesting bit is the innermost
                // inner exception (the raw SQL error, e.g. "Violation of UNIQUE KEY
                // constraint 'IX_CompanyUsers_Username'").
                var inner = ex;
                while (inner.InnerException is not null)
                    inner = inner.InnerException;

                result.Error = inner == ex
                    ? ex.Message
                    : $"{ex.Message} → {inner.Message}";
            }

            result.FinishedAt = DateTime.UtcNow;
            return result;
        }

        // =====================================================================
        // MASTER PUSH — UPDATE-IF-DIFFERENT
        // =====================================================================
        //
        // Master entities are small in number (~30 rows total) and can be
        // edited after being created (a plan change, a T&C body update, a
        // username rename). The tenant-side sync is insert-only, but for
        // the master side we do update-if-different: if a row exists on
        // cloud but its scalar fields differ from local, we overwrite.
        //
        // "Local always wins" — no conflict resolution. Cloud is a replica.
        //
        // The comparison uses the scalar fields that matter for each
        // entity. ID/CreatedAt are set once and never change; comparing
        // them is harmless but unnecessary.

        private async Task PushMasterCompaniesAsync(
            MasterCrmDbContext cloud, MasterSyncResult result, CancellationToken ct)
        {
            var rows = await _localMaster.Companies
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.CompanyId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.CompanyId).ToList();
            var existingCloud = await cloud.Companies
                .Where(x => ids.Contains(x.CompanyId))
                .ToDictionaryAsync(x => x.CompanyId, ct);

            int inserted = 0, updated = 0;

            foreach (var r in rows)
            {
                if (existingCloud.TryGetValue(r.CompanyId, out var c))
                {
                    bool differs =
                        c.CompanyCode != r.CompanyCode ||
                        c.CompanyName != r.CompanyName ||
                        c.IsActive != r.IsActive ||
                        c.CurrentPlanId != r.CurrentPlanId;

                    if (differs)
                    {
                        c.CompanyCode = r.CompanyCode;
                        c.CompanyName = r.CompanyName;
                        c.IsActive = r.IsActive;
                        c.CurrentPlanId = r.CurrentPlanId;
                        updated++;
                    }
                }
                else
                {
                    cloud.Companies.Add(CloneCompany(r));
                    inserted++;
                }

                r.IsSynced = true;
            }

            await SaveWithIdentityInsertAsync(cloud, "Companies", ct);
            await _localMaster.SaveChangesAsync(ct);

            if (inserted > 0 || updated > 0)
            {
                result.Tables.Add(new TableSyncCount
                {
                    TableName = "Companies",
                    Inserted = inserted,
                    Skipped = updated,   // reusing the field for "updated"
                });
                result.TotalRowsSynced += inserted + updated;
            }
        }

        private async Task PushMasterPlansAsync(
            MasterCrmDbContext cloud, MasterSyncResult result, CancellationToken ct)
        {
            var rows = await _localMaster.Plans
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.PlanId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.PlanId).ToList();
            var existingCloud = await cloud.Plans
                .Where(x => ids.Contains(x.PlanId))
                .ToDictionaryAsync(x => x.PlanId, ct);

            int inserted = 0, updated = 0;

            foreach (var r in rows)
            {
                if (existingCloud.TryGetValue(r.PlanId, out var c))
                {
                    bool differs =
                        c.Name != r.Name ||
                        c.Description != r.Description ||
                        c.ModulesCsv != r.ModulesCsv ||
                        c.MonthlyPrice != r.MonthlyPrice ||
                        c.IsActive != r.IsActive;

                    if (differs)
                    {
                        c.Name = r.Name;
                        c.Description = r.Description;
                        c.ModulesCsv = r.ModulesCsv;
                        c.MonthlyPrice = r.MonthlyPrice;
                        c.IsActive = r.IsActive;
                        c.UpdatedAt = r.UpdatedAt;
                        updated++;
                    }
                }
                else
                {
                    cloud.Plans.Add(ClonePlan(r));
                    inserted++;
                }

                r.IsSynced = true;
            }

            await SaveWithIdentityInsertAsync(cloud, "Plans", ct);
            await _localMaster.SaveChangesAsync(ct);

            if (inserted > 0 || updated > 0)
            {
                result.Tables.Add(new TableSyncCount
                {
                    TableName = "Plans",
                    Inserted = inserted,
                    Skipped = updated,
                });
                result.TotalRowsSynced += inserted + updated;
            }
        }

        private async Task PushMasterCompanyDatabasesAsync(
            MasterCrmDbContext cloud, MasterSyncResult result, CancellationToken ct)
        {
            var rows = await _localMaster.CompanyDatabases
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.CompanyDatabaseId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.CompanyDatabaseId).ToList();
            var existingCloud = await cloud.CompanyDatabases
                .Where(x => ids.Contains(x.CompanyDatabaseId))
                .ToDictionaryAsync(x => x.CompanyDatabaseId, ct);

            int inserted = 0, updated = 0;

            foreach (var r in rows)
            {
                if (existingCloud.TryGetValue(r.CompanyDatabaseId, out var c))
                {
                    bool differs =
                        c.ServerName != r.ServerName ||
                        c.DatabaseName != r.DatabaseName ||
                        c.IsActive != r.IsActive ||
                        c.CredentialKey != r.CredentialKey;

                    if (differs)
                    {
                        c.ServerName = r.ServerName;
                        c.DatabaseName = r.DatabaseName;
                        c.IsActive = r.IsActive;
                        c.CredentialKey = r.CredentialKey;
                        updated++;
                    }
                }
                else
                {
                    cloud.CompanyDatabases.Add(CloneCompanyDatabase(r));
                    inserted++;
                }

                r.IsSynced = true;
            }

            await SaveWithIdentityInsertAsync(cloud, "CompanyDatabases", ct);
            await _localMaster.SaveChangesAsync(ct);

            if (inserted > 0 || updated > 0)
            {
                result.Tables.Add(new TableSyncCount
                {
                    TableName = "CompanyDatabases",
                    Inserted = inserted,
                    Skipped = updated,
                });
                result.TotalRowsSynced += inserted + updated;
            }
        }

        private async Task PushMasterSuperAdminsAsync(
            MasterCrmDbContext cloud, MasterSyncResult result, CancellationToken ct)
        {
            var rows = await _localMaster.SuperAdmins
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.SuperAdminId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.SuperAdminId).ToList();
            var existingCloud = await cloud.SuperAdmins
                .Where(x => ids.Contains(x.SuperAdminId))
                .ToDictionaryAsync(x => x.SuperAdminId, ct);

            int inserted = 0, updated = 0;

            foreach (var r in rows)
            {
                if (existingCloud.TryGetValue(r.SuperAdminId, out var c))
                {
                    bool differs =
                        c.Username != r.Username ||
                        c.PasswordHash != r.PasswordHash ||
                        c.FullName != r.FullName ||
                        c.Email != r.Email ||
                        c.IsActive != r.IsActive;

                    if (differs)
                    {
                        c.Username = r.Username;
                        c.PasswordHash = r.PasswordHash;
                        c.FullName = r.FullName;
                        c.Email = r.Email;
                        c.IsActive = r.IsActive;
                        c.UpdatedAt = r.UpdatedAt;
                        updated++;
                    }
                }
                else
                {
                    cloud.SuperAdmins.Add(CloneSuperAdmin(r));
                    inserted++;
                }

                r.IsSynced = true;
            }

            await SaveWithIdentityInsertAsync(cloud, "SuperAdmins", ct);
            await _localMaster.SaveChangesAsync(ct);

            if (inserted > 0 || updated > 0)
            {
                result.Tables.Add(new TableSyncCount
                {
                    TableName = "SuperAdmins",
                    Inserted = inserted,
                    Skipped = updated,
                });
                result.TotalRowsSynced += inserted + updated;
            }
        }

        private async Task PushMasterCompanyUsersAsync(
            MasterCrmDbContext cloud, MasterSyncResult result, CancellationToken ct)
        {
            var rows = await _localMaster.CompanyUsers
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.CompanyUserId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.CompanyUserId).ToList();
            var existingCloud = await cloud.CompanyUsers
                .Where(x => ids.Contains(x.CompanyUserId))
                .ToDictionaryAsync(x => x.CompanyUserId, ct);

            int inserted = 0, updated = 0;

            foreach (var r in rows)
            {
                if (existingCloud.TryGetValue(r.CompanyUserId, out var c))
                {
                    bool differs =
                        c.Username != r.Username ||
                        c.CompanyId != r.CompanyId;

                    if (differs)
                    {
                        c.Username = r.Username;
                        c.CompanyId = r.CompanyId;
                        updated++;
                    }
                }
                else
                {
                    cloud.CompanyUsers.Add(CloneCompanyUser(r));
                    inserted++;
                }

                r.IsSynced = true;
            }

            await SaveWithIdentityInsertAsync(cloud, "CompanyUsers", ct);
            await _localMaster.SaveChangesAsync(ct);

            if (inserted > 0 || updated > 0)
            {
                result.Tables.Add(new TableSyncCount
                {
                    TableName = "CompanyUsers",
                    Inserted = inserted,
                    Skipped = updated,
                });
                result.TotalRowsSynced += inserted + updated;
            }
        }

        private async Task PushMasterSubscriptionsAsync(
            MasterCrmDbContext cloud, MasterSyncResult result, CancellationToken ct)
        {
            var rows = await _localMaster.Subscriptions
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.SubscriptionId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.SubscriptionId).ToList();
            var existingCloud = await cloud.Subscriptions
                .Where(x => ids.Contains(x.SubscriptionId))
                .ToDictionaryAsync(x => x.SubscriptionId, ct);

            int inserted = 0, updated = 0;

            foreach (var r in rows)
            {
                if (existingCloud.TryGetValue(r.SubscriptionId, out var c))
                {
                    bool differs =
                        c.CompanyId != r.CompanyId ||
                        c.PlanId != r.PlanId ||
                        c.StartedAt != r.StartedAt ||
                        c.EndedAt != r.EndedAt ||
                        c.TermsAcceptedBy != r.TermsAcceptedBy ||
                        c.TermsAcceptedAt != r.TermsAcceptedAt;

                    if (differs)
                    {
                        c.CompanyId = r.CompanyId;
                        c.PlanId = r.PlanId;
                        c.StartedAt = r.StartedAt;
                        c.EndedAt = r.EndedAt;
                        c.TermsAcceptedBy = r.TermsAcceptedBy;
                        c.TermsAcceptedAt = r.TermsAcceptedAt;
                        c.UpdatedAt = r.UpdatedAt;
                        updated++;
                    }
                }
                else
                {
                    cloud.Subscriptions.Add(CloneSubscription(r));
                    inserted++;
                }

                r.IsSynced = true;
            }

            await SaveWithIdentityInsertAsync(cloud, "Subscriptions", ct);
            await _localMaster.SaveChangesAsync(ct);

            if (inserted > 0 || updated > 0)
            {
                result.Tables.Add(new TableSyncCount
                {
                    TableName = "Subscriptions",
                    Inserted = inserted,
                    Skipped = updated,
                });
                result.TotalRowsSynced += inserted + updated;
            }
        }

        private async Task PushMasterTermsAsync(
            MasterCrmDbContext cloud, MasterSyncResult result, CancellationToken ct)
        {
            var rows = await _localMaster.TermsAndConditions
                .Where(x => !x.IsSynced)
                .OrderBy(x => x.TermsId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (rows.Count == 0) return;

            var ids = rows.Select(r => r.TermsId).ToList();
            var existingCloud = await cloud.TermsAndConditions
                .Where(x => ids.Contains(x.TermsId))
                .ToDictionaryAsync(x => x.TermsId, ct);

            int inserted = 0, updated = 0;

            foreach (var r in rows)
            {
                if (existingCloud.TryGetValue(r.TermsId, out var c))
                {
                    bool differs =
                        c.PlanId != r.PlanId ||
                        c.Title != r.Title ||
                        c.Body != r.Body ||
                        c.Version != r.Version ||
                        c.EffectiveFrom != r.EffectiveFrom;

                    if (differs)
                    {
                        c.PlanId = r.PlanId;
                        c.Title = r.Title;
                        c.Body = r.Body;
                        c.Version = r.Version;
                        c.EffectiveFrom = r.EffectiveFrom;
                        c.UpdatedAt = r.UpdatedAt;
                        updated++;
                    }
                }
                else
                {
                    cloud.TermsAndConditions.Add(CloneTermsAndConditions(r));
                    inserted++;
                }

                r.IsSynced = true;
            }

            await SaveWithIdentityInsertAsync(cloud, "TermsAndConditions", ct);
            await _localMaster.SaveChangesAsync(ct);

            if (inserted > 0 || updated > 0)
            {
                result.Tables.Add(new TableSyncCount
                {
                    TableName = "TermsAndConditions",
                    Inserted = inserted,
                    Skipped = updated,
                });
                result.TotalRowsSynced += inserted + updated;
            }
        }

        // =====================================================================
        // CLONE METHODS — one per entity type, scalar fields only, no navs
        // =====================================================================
        //
        // EF can't attach the same object instance to two DbContexts, so we
        // copy the scalar fields into a fresh instance for the cloud context.
        // Nav properties (Customer, Employee, etc.) are left null on purpose:
        // the FKs are already set as scalars, and cloud already has the parent
        // rows (or will by the time the child push runs, because parent tables
        // push first).

        private static Branch CloneBranch(Branch r) => new()
        {
            BranchId = r.BranchId,
            BranchName = r.BranchName,
            Address = r.Address,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static Employee CloneEmployee(Employee r) => new()
        {
            EmployeeId = r.EmployeeId,
            FirstName = r.FirstName,
            LastName = r.LastName,
            ContactNumber = r.ContactNumber,
            Email = r.Email,
            Role = r.Role,
            BranchId = r.BranchId,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static Credential CloneCredential(Credential r) => new()
        {
            CredentialId = r.CredentialId,
            Username = r.Username,
            PasswordHash = r.PasswordHash,
            EmployeeId = r.EmployeeId,
            LastLoginDate = r.LastLoginDate,
            FailedLoginAttempts = r.FailedLoginAttempts,
            IsLocked = r.IsLocked,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static Customer CloneCustomer(Customer r) => new()
        {
            CustomerId = r.CustomerId,
            FirstName = r.FirstName,
            LastName = r.LastName,
            ContactNumber = r.ContactNumber,
            Email = r.Email,
            Birthday = r.Birthday,
            LastVisitDate = r.LastVisitDate,
            PointsBalance = r.PointsBalance,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static Campaign CloneCampaign(Campaign r) => new()
        {
            CampaignId = r.CampaignId,
            PromoName = r.PromoName,
            PromoCategory = r.PromoCategory,
            DiscountType = r.DiscountType,
            DiscountValue = r.DiscountValue,
            TargetTier = r.TargetTier,
            MinPurchaseAmount = r.MinPurchaseAmount,
            MaxDiscountAmount = r.MaxDiscountAmount,
            MaxRedemptionsPerCustomer = r.MaxRedemptionsPerCustomer,
            RequiredProductId = r.RequiredProductId,
            RewardProductId = r.RewardProductId,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            MadeBy = r.MadeBy,
            Reason = r.Reason,
            Description = r.Description,
            BranchId = r.BranchId,
            Status = r.Status,
            DecidedBy = r.DecidedBy,
            DecidedAt = r.DecidedAt,
            DecisionNotes = r.DecisionNotes,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static Order CloneOrder(Order r) => new()
        {
            OrderId = r.OrderId,
            CustomerId = r.CustomerId,
            CreatedBy = r.CreatedBy,
            SubTotal = r.SubTotal,
            TotalDiscount = r.TotalDiscount,
            TotalAmount = r.TotalAmount,
            PointsEarned = r.PointsEarned,
            OrderDate = r.OrderDate,
            Status = r.Status,
            HasPwdSeniorDiscount = r.HasPwdSeniorDiscount,
            PwdSeniorDiscountAmount = r.PwdSeniorDiscountAmount,
            BranchId = r.BranchId,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static OrderItem CloneOrderItem(OrderItem r) => new()
        {
            OrderItemId = r.OrderItemId,
            OrderId = r.OrderId,
            ProductId = r.ProductId,
            ProductSizeId = r.ProductSizeId,
            SizeNameSnapshot = r.SizeNameSnapshot,
            BaseSizePrice = r.BaseSizePrice,
            Quantity = r.Quantity,
            UnitPrice = r.UnitPrice,
            SubTotal = r.SubTotal,
            IsSynced = true,
        };

        private static OrderItemModifier CloneOrderItemModifier(OrderItemModifier r) => new()
        {
            OrderItemModifierId = r.OrderItemModifierId,
            OrderItemId = r.OrderItemId,
            ProductModifierId = r.ProductModifierId,
            ProductModifierNameSnapshot = r.ProductModifierNameSnapshot,
            UnitPrice = r.UnitPrice,
            IsSynced = true,
        };

        private static CampaignRedeem CloneCampaignRedeem(CampaignRedeem r) => new()
        {
            RedeemId = r.RedeemId,
            CampaignId = r.CampaignId,
            OrderId = r.OrderId,
            DiscountUsed = r.DiscountUsed,
            RedeemDate = r.RedeemDate,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static PointLedger ClonePointLedger(PointLedger r) => new()
        {
            PointLedgerId = r.PointLedgerId,
            CustomerId = r.CustomerId,
            PointsChange = r.PointsChange,
            Type = r.Type,
            Reason = r.Reason,
            OrderId = r.OrderId,
            PerformedBy = r.PerformedBy,
            Timestamp = r.Timestamp,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static Feedback CloneFeedback(Feedback r) => new()
        {
            TicketId = r.TicketId,
            CustomerId = r.CustomerId,
            LoggedBy = r.LoggedBy,
            Rating = r.Rating,
            Category = r.Category,
            StaffNotes = r.StaffNotes,
            Status = r.Status,
            DateLogged = r.DateLogged,
            DecidedBy = r.DecidedBy,
            DecidedAt = r.DecidedAt,
            DecisionNotes = r.DecisionNotes,
            BranchId = r.BranchId,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static PointAdjustmentRequest ClonePointAdjustmentRequest(PointAdjustmentRequest r) => new()
        {
            PointAdjustmentRequestId = r.PointAdjustmentRequestId,
            CustomerId = r.CustomerId,
            PointsChange = r.PointsChange,
            Reason = r.Reason,
            RequestedBy = r.RequestedBy,
            RequestedAt = r.RequestedAt,
            Status = r.Status,
            ApprovedBy = r.ApprovedBy,
            DecidedAt = r.DecidedAt,
            DecisionNotes = r.DecisionNotes,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static Company CloneCompany(Company r) => new()
        {
            CompanyId = r.CompanyId,
            CompanyCode = r.CompanyCode,
            CompanyName = r.CompanyName,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            CurrentPlanId = r.CurrentPlanId,
            IsSynced = true,
        };

        private static Plan ClonePlan(Plan r) => new()
        {
            PlanId = r.PlanId,
            Name = r.Name,
            Description = r.Description,
            ModulesCsv = r.ModulesCsv,
            MonthlyPrice = r.MonthlyPrice,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static CompanyDatabase CloneCompanyDatabase(CompanyDatabase r) => new()
        {
            CompanyDatabaseId = r.CompanyDatabaseId,
            CompanyId = r.CompanyId,
            ServerName = r.ServerName,
            DatabaseName = r.DatabaseName,
            IsActive = r.IsActive,
            CredentialKey = r.CredentialKey,
            CloudServerName = r.CloudServerName,
            CloudDatabaseName = r.CloudDatabaseName,
            IsSynced = true,
        };

        private static SuperAdmin CloneSuperAdmin(SuperAdmin r) => new()
        {
            SuperAdminId = r.SuperAdminId,
            Username = r.Username,
            PasswordHash = r.PasswordHash,
            FullName = r.FullName,
            Email = r.Email,
            IsActive = r.IsActive,
            LastLoginDate = r.LastLoginDate,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static CompanyUser CloneCompanyUser(CompanyUser r) => new()
        {
            CompanyUserId = r.CompanyUserId,
            CompanyId = r.CompanyId,
            Username = r.Username,
            CreatedAt = r.CreatedAt,
            IsSynced = true,
        };

        private static Subscription CloneSubscription(Subscription r) => new()
        {
            SubscriptionId = r.SubscriptionId,
            CompanyId = r.CompanyId,
            PlanId = r.PlanId,
            StartedAt = r.StartedAt,
            EndedAt = r.EndedAt,
            TermsAcceptedBy = r.TermsAcceptedBy,
            TermsAcceptedAt = r.TermsAcceptedAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        private static TermsAndConditions CloneTermsAndConditions(TermsAndConditions r) => new()
        {
            TermsId = r.TermsId,
            PlanId = r.PlanId,
            Title = r.Title,
            Body = r.Body,
            Version = r.Version,
            EffectiveFrom = r.EffectiveFrom,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            IsSynced = true,
        };

        // =====================================================================
        // IDENTITY_INSERT HELPER
        // =====================================================================
        //
        // Cloned rows carry their original PKs, and cloud already has the same
        // schema (identity PKs). By default EF omits identity columns from the
        // INSERT, so SQL Server generates fresh IDs — which breaks FK integrity
        // for children that reference the parent's original ID.
        //
        // The fix is SET IDENTITY_INSERT, which allows explicit PK values for
        // the duration of the SESSION (physical connection). Critical detail:
        // ExecuteSqlRawAsync and SaveChangesAsync must run on the SAME
        // connection, or the flag lives on the wrong session. We do this by
        // explicitly opening the DbConnection first and issuing the SET
        // command through it. SaveChangesAsync then reuses the open connection
        // instead of pulling a fresh one from the pool.

        private static async Task SaveWithIdentityInsertAsync(
            DbContext context, string tableName, CancellationToken ct)
        {
            var connection = context.Database.GetDbConnection();
            var wasClosed = connection.State != System.Data.ConnectionState.Open;

            if (wasClosed)
                await connection.OpenAsync(ct);

            bool identityInsertOn = false;

            try
            {
                // Turn IDENTITY_INSERT on for this table on THIS connection.
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"SET IDENTITY_INSERT [{tableName}] ON";
                    try
                    {
                        await cmd.ExecuteNonQueryAsync(ct);
                        identityInsertOn = true;
                    }
                    catch
                    {
                        // Table doesn't have an identity column — no wrapper
                        // needed. Fall through to a plain SaveChanges.
                        identityInsertOn = false;
                    }
                }

                // SaveChangesAsync will reuse the open connection, so the
                // IDENTITY_INSERT flag applies to its INSERT statements.
                await context.SaveChangesAsync(ct);
            }
            finally
            {
                if (identityInsertOn)
                {
                    try
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = $"SET IDENTITY_INSERT [{tableName}] OFF";
                        await cmd.ExecuteNonQueryAsync(ct);
                    }
                    catch { /* best effort — connection is about to be closed anyway */ }
                }

                if (wasClosed)
                    await connection.CloseAsync();
            }
        }
    }
}