namespace CRM_Morcaffein.winforms.Services.Api
{
    public static class ApiConfig
    {
        public const string BaseUrl = "http://localhost:5035";
        public const int CompanyId = 1;

        /// <summary>
        /// The JWT for the currently logged-in employee. Set by FrmLogin after a
        /// successful authentication. Every Api* class reads this and attaches it as
        /// an "Authorization: Bearer &lt;token&gt;" header on outgoing requests.
        /// Null until the user logs in.
        /// </summary>
        public static string? AuthToken { get; set; }

        /// <summary>Clears the token on logout.</summary>
        public static void ClearAuthToken() => AuthToken = null;
    }
}