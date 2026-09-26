using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class BranchApi
    {
        private readonly HttpClient _http;

        public BranchApi()
        {
            _http = ApiClientFactory.Create();
        }

        // GET api/tenant/{companyId}/branches
        public async Task<List<BranchResponseDto>> GetBranchesAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/branches");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<BranchResponseDto>>();
            return result ?? new List<BranchResponseDto>();
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