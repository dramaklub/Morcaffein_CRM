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
        // Active only. Everyone can read.
        public async Task<List<BranchResponseDto>> GetBranchesAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/branches");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<BranchResponseDto>>();
            return result ?? new List<BranchResponseDto>();
        }

        // GET api/tenant/{companyId}/branches/all
        // Active + inactive. Admin-only on the server.
        public async Task<List<BranchResponseDto>> GetAllBranchesAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/branches/all");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<BranchResponseDto>>();
            return result ?? new List<BranchResponseDto>();
        }

        // POST api/tenant/{companyId}/branches
        public async Task<BranchResponseDto> CreateAsync(CreateBranchRequest request)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/branches", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<BranchResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty branch.");
        }

        // PUT api/tenant/{companyId}/branches/{id}
        public async Task<BranchResponseDto> UpdateAsync(int branchId, UpdateBranchRequest request)
        {
            var response = await _http.PutAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/branches/{branchId}", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<BranchResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty branch.");
        }

        // POST api/tenant/{companyId}/branches/{id}/deactivate
        public async Task<BranchResponseDto> DeactivateAsync(int branchId)
        {
            var response = await _http.PostAsync(
                $"api/tenant/{ApiConfig.CompanyId}/branches/{branchId}/deactivate",
                content: null);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<BranchResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty branch.");
        }

        // POST api/tenant/{companyId}/branches/{id}/reactivate
        public async Task<BranchResponseDto> ReactivateAsync(int branchId)
        {
            var response = await _http.PostAsync(
                $"api/tenant/{ApiConfig.CompanyId}/branches/{branchId}/reactivate",
                content: null);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<BranchResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty branch.");
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