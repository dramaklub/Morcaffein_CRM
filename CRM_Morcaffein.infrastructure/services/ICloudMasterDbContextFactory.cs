using CRM_Morcaffein.infrastructure.data;

namespace CRM_Morcaffein.infrastructure.services
{
    /// <summary>
    /// Builds a MasterCrmDbContext pointed at the CLOUD master. Single
    /// instance — there's one cloud master for the whole platform.
    ///
    /// Used by the sync worker to push master-level entities (Companies,
    /// Subscriptions, CompanyUsers, SuperAdmins, Plans, TermsAndConditions,
    /// CompanyDatabases) to the cloud.
    /// </summary>
    public interface ICloudMasterDbContextFactory
    {
        MasterCrmDbContext Create();
    }
}