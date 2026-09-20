using CRM_Morcaffein.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace CRM_Morcaffein.infrastructure.services
{
    public class TenantDatabaseResolver : ITenantDatabaseResolver
    {
        private readonly MasterCrmDbContext _masterDb;

        // Process-lifetime cache. CompanyDatabase rows don't change at runtime, so once we
        // resolve a tenant's DB info, we never need to hit the master DB again for that ID.
        // Avoids a remote round-trip on every single API call.
        private static readonly ConcurrentDictionary<int, TenantDatabaseInfo> _cache = new();

        public TenantDatabaseResolver(MasterCrmDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        public async Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId)
        {
            if (_cache.TryGetValue(companyId, out var cached))
                return cached;

            var tenantDatabase = await _masterDb.CompanyDatabases
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.IsActive);

            if (tenantDatabase == null)
            {
                throw new InvalidOperationException(
                    $"No active tenant database found for CompanyId {companyId}.");
            }

            var info = new TenantDatabaseInfo
            {
                ServerName = tenantDatabase.ServerName,
                DatabaseName = tenantDatabase.DatabaseName,
                CredentialKey = tenantDatabase.CredentialKey
            };

            _cache[companyId] = info;
            return info;
        }
    }
}