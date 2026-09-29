using CRM_Morcaffein.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace CRM_Morcaffein.infrastructure.services
{
    /// <summary>
    /// Builds a MasterCrmDbContext pointed at the cloud master. Reads the
    /// "CloudMaster" connection string from appsettings.json. No caching
    /// beyond the built options — the cloud master connection string never
    /// changes at runtime.
    /// </summary>
    public class CloudMasterDbContextFactory : ICloudMasterDbContextFactory
    {
        private readonly IConfiguration _configuration;
        private DbContextOptions<MasterCrmDbContext>? _options;

        public CloudMasterDbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public MasterCrmDbContext Create()
        {
            _options ??= BuildOptions();
            return new MasterCrmDbContext(_options);
        }

        private DbContextOptions<MasterCrmDbContext> BuildOptions()
        {
            var connectionString = _configuration.GetConnectionString("CloudMaster");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'CloudMaster' is not configured in appsettings.json.");
            }

            return new DbContextOptionsBuilder<MasterCrmDbContext>()
                .UseSqlServer(connectionString, sql =>
                {
                    // Retry transient failures (network blips, brief cloud outages)
                    // automatically before surfacing an exception. Matches the
                    // treatment on CloudTenantDbContextFactory.
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                })
                .Options;
        }
    }
}