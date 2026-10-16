using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    /// <summary>
    /// HTTP client for the caller's own tenant context — currently just
    /// entitlements (what the tenant's plan allows). Called once at login.
    /// </summary>
    public class MeApi
    {
        private readonly HttpClient _http;

        public MeApi()
        {
            _http = ApiClientFactory.Create();
        }

        public async Task<EntitlementsResponseDto> GetEntitlementsAsync()
        {
            var response = await _http.GetAsync(
                $"api/tenant/{ApiConfig.CompanyId}/me/entitlements");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<EntitlementsResponseDto>();
            return result ?? new EntitlementsResponseDto();
        }

        // GET api/tenant/{companyId}/me/terms
        public async Task<TenantTermsDto> GetTermsAsync()
        {
            var response = await _http.GetAsync(
                $"api/tenant/{ApiConfig.CompanyId}/me/terms");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<TenantTermsDto>();
            return result ?? new TenantTermsDto { HasTerms = false };
        }

        private static async Task EnsureSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(body)
                    ? $"API returned {(int)response.StatusCode}."
                    : body);
        }
    }
}