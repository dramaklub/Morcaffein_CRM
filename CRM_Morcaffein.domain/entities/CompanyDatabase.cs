using System;
using System.Collections.Generic;
using System.Text;

namespace CRM_Morcaffein.domain.entities
{
    /// <summary>
    /// Routing row: which physical SQL Server + database hosts a tenant's
    /// data, plus where its cloud replica lives. Lives in the master DB.
    ///
    /// ServerName/DatabaseName point at the LOCAL tenant (the source of truth
    /// during normal operation). CloudServerName/CloudDatabaseName point at
    /// the CLOUD replica, used by the sync worker when pushing rows.
    ///
    /// CredentialKey indexes into appsettings "TenantCredentials" and is
    /// shared between both — the factory branches on whether the host string
    /// starts with "(localdb)" to decide Windows auth vs SQL auth.
    /// </summary>
    public class CompanyDatabase
    {
        public int CompanyDatabaseId { get; set; }
        public int CompanyId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public string CredentialKey { get; set; } = string.Empty;

        /// <summary>
        /// Cloud replica host. Null means "no cloud replica configured" — the
        /// sync worker will skip this tenant. Populated for companies 1/2/3
        /// with their respective db67xxx.public.databaseasp.net hosts.
        /// </summary>
        public string? CloudServerName { get; set; }

        /// <summary>
        /// Cloud replica database name. Null means "no cloud replica configured".
        /// </summary>
        public string? CloudDatabaseName { get; set; }

        public Company? Company { get; set; }

        /// <summary>
        /// Sync flag for master-side entities. Master CRUD (SuperAdmin
        /// creating companies, assigning plans, etc.) writes locally and
        /// leaves this false; the worker pushes to cloud master and flips.
        /// </summary>
        public bool IsSynced { get; set; } = false;
    }
}