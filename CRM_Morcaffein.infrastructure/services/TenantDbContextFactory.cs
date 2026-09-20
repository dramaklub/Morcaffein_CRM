using CRM_Morcaffein.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace CRM_Morcaffein.infrastructure.services
{
    public class TenantDbContextFactory : ITenantDbContextFactory
    {
        private readonly ITenantDatabaseResolver _resolver;
        private readonly IConfiguration _configuration;

        // Cache built DbContextOptions per company. DbContextOptions is designed to be
        // built ONCE and reused — building it per call forces EF to re-evaluate the
        // provider, break the connection pool, and open a fresh SQL connection every
        // request. This one cache is the difference between ~500ms and ~50ms per call.
        private static readonly ConcurrentDictionary<int, DbContextOptions<TenantCrmDbContext>> _optionsCache = new();

        public TenantDbContextFactory(
            ITenantDatabaseResolver resolver,
            IConfiguration configuration)
        {
            _resolver = resolver;
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

            var databaseInfo = await _resolver.GetDatabaseInfoAsync(companyId);

            // LocalDB uses Windows authentication (Trusted_Connection) — there's no
            // user/password to look up. Cloud SQL Server needs credentials from config.
            var isLocalDb = databaseInfo.ServerName.Contains("(localdb)", StringComparison.OrdinalIgnoreCase);

            string connectionString;

            if (isLocalDb)
            {
                connectionString =
                    $"Server={databaseInfo.ServerName};" +
                    $"Database={databaseInfo.DatabaseName};" +
                    $"Trusted_Connection=True;" +
                    $"TrustServerCertificate=True;" +
                    $"MultipleActiveResultSets=True;";
            }
            else
            {
                var userId = _configuration[$"TenantCredentials:{databaseInfo.CredentialKey}:UserId"];
                var password = _configuration[$"TenantCredentials:{databaseInfo.CredentialKey}:Password"];

                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException(
                        $"Credentials not found for key '{databaseInfo.CredentialKey}'.");
                }

                connectionString =
                    $"Server={databaseInfo.ServerName};" +
                    $"Database={databaseInfo.DatabaseName};" +
                    $"User Id={userId};" +
                    $"Password={password};" +
                    $"Encrypt=True;" +
                    $"TrustServerCertificate=True;" +
                    $"MultipleActiveResultSets=True;" +
                    $"Min Pool Size=2;" +
                    $"Max Pool Size=10;" +
                    $"Connection Lifetime=300;";
            }

            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            _optionsCache[companyId] = options;
            return options;
        }
    }
}