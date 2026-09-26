using CRM_Morcaffein.winforms.Services.Api;
using CRM_Morcaffein.winforms.Services.Reports;
using CRM_Morcaffein.winforms.Views;

namespace CRM_Morcaffein
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // PDFsharp 6.x doesn't load system fonts on its own. Register a resolver
            // that reads TTFs from the Windows fonts folder before any PDF work runs —
            // otherwise MigraDoc throws when it tries to resolve even its built-in
            // fallback fonts (Courier New etc.).
            PdfSharp.Fonts.GlobalFontSettings.FontResolver = new WindowsFontResolver();

            // Prewarm the API (fire-and-forget, ignore failures).
            _ = Task.Run(async () =>
            {
                try
                {
                    using var http = new HttpClient
                    {
                        BaseAddress = new Uri(ApiConfig.BaseUrl),
                        Timeout = TimeSpan.FromSeconds(10),
                    };
                    await http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/customers?page=1&pageSize=1");
                }
                catch { }
            });

            // Loop: show login, then the appropriate shell, until the user
            // closes the login window. Two shells exist — FrmMain for regular
            // staff (Barista/Manager/Admin) and FrmSuperAdminMain for platform
            // operators. FrmLogin sets ApiConfig.IsSuperAdminSession to tell
            // us which one to open.
            while (true)
            {
                using var login = new FrmLogin();
                if (login.ShowDialog() != DialogResult.OK)
                    return;   // user closed login — exit

                bool closedViaLogout;

                if (ApiConfig.IsSuperAdminSession)
                {
                    var superShell = new FrmSuperAdminMain();
                    Application.Run(superShell);
                    closedViaLogout = superShell.ClosedViaLogout;
                }
                else
                {
                    var main = new FrmMain();
                    Application.Run(main);
                    closedViaLogout = main.ClosedViaLogout;
                }

                // If the user closed the shell via the X button, exit the app.
                // If they clicked Logout, loop back to the login screen.
                if (!closedViaLogout)
                    return;
            }
        }
    }
}