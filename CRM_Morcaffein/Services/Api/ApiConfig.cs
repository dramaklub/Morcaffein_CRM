namespace CRM_Morcaffein.winforms.Services.Api
{
    public static class ApiConfig
    {
        public const string BaseUrl = "http://localhost:5035";
        /// <summary>
        /// The tenant this session is talking to. Set at login from the
        /// login response's companyId. Previously a hardcoded const = 1,
        /// which meant every login hit Tenant A regardless of which
        /// username was entered. Now dynamic — resolved by the server
        /// from the CompanyUsers routing table.
        ///
        /// Reads before login (rare — some early boot code) will see 0,
        /// which is invalid and will produce a clear "Company 0 not
        /// found" error rather than silently hitting the wrong tenant.
        /// </summary>
        public static int CompanyId { get; set; } = 1;

        /// <summary>
        /// The JWT for the currently logged-in employee. Set by FrmLogin after a
        /// successful authentication. Every Api* class reads this and attaches it as
        /// an "Authorization: Bearer &lt;token&gt;" header on outgoing requests.
        /// Null until the user logs in.
        /// </summary>
        public static string? AuthToken { get; set; }
        /// <summary>
        /// Set to true when the last successful login was a SuperAdmin login.
        /// Program.cs reads this after FrmLogin closes to decide whether to open
        /// FrmMain (regular staff shell) or FrmSuperAdminMain (platform console).
        /// Reset on logout.
        /// </summary>
        public static bool IsSuperAdminSession { get; set; }
        /// <summary>
        /// Full name of the logged-in SuperAdmin. Populated at login; used by
        /// FrmSuperAdminMain's sidebar footer. Null for regular employee sessions.
        /// </summary>
        public static string? SuperAdminName { get; set; }

        public static void ClearAuthToken()
        {
            AuthToken = null;
            IsSuperAdminSession = false;
            SuperAdminName = null;
            CompanyId = 1;   // reset to default on logout
        }
    }
}