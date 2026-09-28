using CRM_Morcaffein.infrastructure.data;

namespace CRM_Morcaffein.infrastructure.services
{
    /// <summary>
    /// Builds a TenantCrmDbContext pointed at a company's CLOUD replica,
    /// for the sync worker. Mirror of ITenantDbContextFactory, but reads
    /// CompanyDatabase.CloudServerName/CloudDatabaseName instead of the
    /// local ServerName/DatabaseName.
    ///
    /// Throws if the company has no cloud replica configured
    /// (CloudServerName is null) — the worker catches that and skips.
    /// </summary>
    public interface ICloudTenantDbContextFactory
    {
        Task<TenantCrmDbContext> CreateAsync(int companyId);
    }
}