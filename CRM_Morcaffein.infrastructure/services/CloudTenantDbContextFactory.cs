using CRM_Morcaffein.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace CRM_Morcaffein.infrastructure.services
{
    /// <summary>
    /// Same shape as TenantDbContextFactory, but points at the cloud replica
    /// host for each company (CompanyDatabase.CloudServerName /
    /// CloudDatabaseName). Used exclusively by the sync worker to push rows.
    ///
    /// Cloud is always SQL auth (never (localdb)), so the credential lookup
    /// always runs. CredentialKey is shared with the local side — same
    /// TenantCredentials section in appsettings.json.
    /// </summary>
    public class CloudTenantDbContextFactory : ICloudTenantDbContextFactory
    {
        private readonly MasterCrmDbContext _master;
        private readonly IConfiguration _configuration;

        // Cached per company, same reasoning as the local factory — build
        // DbContextOptions once and reuse.
        private static readonly ConcurrentDictionary<int, DbContextOptions<TenantCrmDbContext>> _optionsCache = new();

        public CloudTenantDbContextFactory(
            MasterCrmDbContext master,
            IConfiguration configuration)
        {
            _master = master;
            _configuration = configuration;
        }

        public async Task<TenantCrmDbContext> CreateAsync(int companyId)
        {
            var options = await GetOrBuildOptionsAsync(companyId);
            return new TenantCrmDbContext(options);
        }

        private async Task<DbContextOptions<TenantCrmDbContext>> GetOrBuildOptionsAsync(int companyId)
        {
            if (_optionsCache.TryGetValue(companyId, out var cached))
                return cached;

            // Read the CompanyDatabase row directly — no resolver, because the
            // resolver only knows about the local side.
            var row = await _master.CompanyDatabases
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive);

            if (row is null)
            {
                throw new InvalidOperationException(
                    $"No active CompanyDatabase row for CompanyId {companyId}.");
            }

            if (string.IsNullOrWhiteSpace(row.CloudServerName) ||
                string.IsNullOrWhiteSpace(row.CloudDatabaseName))
            {
                throw new InvalidOperationException(
                    $"Company {companyId} has no cloud replica configured " +
                    $"(CloudServerName or CloudDatabaseName is null).");
            }

            var userId = _configuration[$"TenantCredentials:{row.CredentialKey}:UserId"];
            var password = _configuration[$"TenantCredentials:{row.CredentialKey}:Password"];

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    $"Credentials not found for key '{row.CredentialKey}'.");
            }

            var connectionString =
                $"Server={row.CloudServerName};" +
                $"Database={row.CloudDatabaseName};" +
                $"User Id={userId};" +
                $"Password={password};" +
                $"Encrypt=True;" +
                $"TrustServerCertificate=True;" +
                $"MultipleActiveResultSets=True;" +
                $"Min Pool Size=2;" +
                $"Max Pool Size=10;" +
                $"Connection Lifetime=300;";

            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                // Retry transient failures (network blips, brief cloud outages)
                // automatically before surfacing an exception. This makes the
                // worker's tick resilient to a few seconds of dropped connectivity
                // without bouncing the exception up to the catch block.
                sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            })
            .Options;

            _optionsCache[companyId] = options;
            return options;
        }
    }
}