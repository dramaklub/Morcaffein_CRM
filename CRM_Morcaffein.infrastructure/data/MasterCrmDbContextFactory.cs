using CRM_Morcaffein.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRM_Morcaffein.infrastructure.data
{
    /// <summary>
    /// Design-time only: used by `dotnet ef migrations add/update` for MasterCrmDbContext.
    /// Points at the local master DB. Never runs at runtime.
    /// </summary>
    public class MasterCrmDbContextFactory : IDesignTimeDbContextFactory<MasterCrmDbContext>
    {
        public MasterCrmDbContext CreateDbContext(string[] args)
        {
            const string connectionString =
                "Server=(localdb)\\CRM_Master;" +
                "Database=CRM_Morcaffein_MasterCrm;" +
                "Trusted_Connection=True;" +
                "TrustServerCertificate=True;" +
                "MultipleActiveResultSets=True;";

            var options = new DbContextOptionsBuilder<MasterCrmDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new MasterCrmDbContext(options);
        }
    }
}