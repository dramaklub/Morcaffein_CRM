using CRM_Morcaffein.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRM_Morcaffein.infrastructure.data
{
    /// <summary>
    /// Design-time only: used by `dotnet ef migrations add/update`. The connection string
    /// is inlined because this file never runs at runtime — it only exists so EF's
    /// migration tooling can build a TenantCrmDbContext without the full DI stack.
    ///
    /// If the tenant DB ever moves, update the string here AND in the API's
    /// appsettings.json. This is the only place the string is duplicated.
    /// </summary>
    public class TenantCrmDbContextFactory : IDesignTimeDbContextFactory<TenantCrmDbContext>
    {
        public TenantCrmDbContext CreateDbContext(string[] args)
        {
            //const string connectionString =
            //    "Server=db67068.public.databaseasp.net;" +
            //    "Database=db67068;" +
            //    "User Id=db67068;" +
            //    "Password=iG#9H8g?2d@A;" +
            //    "Encrypt=True;" +
            //    "TrustServerCertificate=True;" +
            //    "MultipleActiveResultSets=True;";

            const string connectionString =
                "Server=(localdb)\\MSSQLLocalDB;" +
                "Database=CRM_Morcaffein_TenantCrm;" +
                "Trusted_Connection=True;" +
                "TrustServerCertificate=True;" +
                "MultipleActiveResultSets=True;";

            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new TenantCrmDbContext(options);
        }
    }
}