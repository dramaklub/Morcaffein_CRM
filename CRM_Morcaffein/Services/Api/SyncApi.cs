using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    /// <summary>
    /// HTTP client for the sync status endpoint. Used by FrmMain's topbar
    /// indicator, which polls every 5 seconds.
    /// </summary>
    public class SyncApi
    {
        private readonly HttpClient _http;

        public SyncApi()
        {
            _http = ApiClientFactory.Create();
        }

        // GET api/tenant/{companyId}/sync/status
        // Returns null on any failure (network error, non-2xx) so the
        // indicator can render a neutral "unknown" state without crashing.
        public async Task<SyncStatusResponse?> GetStatusAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync(
                    $"api/tenant/{ApiConfig.CompanyId}/sync/status", ct);

                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<SyncStatusResponse>(ct);
            }
            catch
            {
                return null;
            }
        }
    }
}