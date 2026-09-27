using System.Net;
using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;


namespace CRM_Morcaffein.winforms.Services.Api
{
    public class CampaignApi
    {
        private readonly HttpClient _http;

        public CampaignApi()
        {
            _http = ApiClientFactory.Create();
        }

        public async Task<List<CampaignResponseDto>> GetActiveCampaignsAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/campaigns");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<CampaignResponseDto>>();
            return result ?? new List<CampaignResponseDto>();
        }

        public async Task<CampaignResponseDto?> GetByIdAsync(int campaignId)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/campaigns/{campaignId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<CampaignResponseDto>();
        }

        // GET api/tenant/{companyId}/campaigns/manage
        // Returns every campaign regardless of status — the Manager's list.
        public async Task<List<CampaignResponseDto>> GetForManagementAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/campaigns/manage");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<CampaignResponseDto>>();
            return result ?? new List<CampaignResponseDto>();
        }

        // POST api/tenant/{companyId}/campaigns
        public async Task<CampaignResponseDto> CreateAsync(CreateCampaignRequest request)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/campaigns", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<CampaignResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty campaign.");
        }

        // PUT api/tenant/{companyId}/campaigns/{id}
        public async Task<CampaignResponseDto> UpdateAsync(int campaignId, UpdateCampaignRequest request)
        {
            var response = await _http.PutAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/campaigns/{campaignId}", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<CampaignResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty campaign.");
        }

        // POST api/tenant/{companyId}/campaigns/{id}/resubmit
        // Moves an Inactive campaign back to Pending for re-approval.
        public async Task<CampaignResponseDto> ResubmitAsync(int campaignId, int performedBy)
        {
            var body = new CampaignActionRequest { PerformedBy = performedBy };
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/campaigns/{campaignId}/resubmit", body);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<CampaignResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty campaign.");
        }

        // POST api/tenant/{companyId}/campaigns/{id}/deactivate
        public async Task<CampaignResponseDto> DeactivateAsync(int campaignId, int performedBy)
        {
            var body = new CampaignActionRequest { PerformedBy = performedBy };
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/campaigns/{campaignId}/deactivate", body);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<CampaignResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty campaign.");
        }
        // GET api/tenant/{companyId}/campaigns/pending
        public async Task<List<CampaignResponseDto>> GetPendingForApprovalAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/campaigns/pending");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<CampaignResponseDto>>();
            return result ?? new List<CampaignResponseDto>();
        }

        // POST api/tenant/{companyId}/campaigns/{id}/approve
        // Returns the campaign plus the outcome of the retention-email send
        // that fires after approval. See ApproveCampaignResponse.
        public async Task<ApproveCampaignResponse> ApproveAsync(int campaignId, int decidedBy, string? notes)
        {
            var body = new DecideCampaignRequest { DecidedBy = decidedBy, Notes = notes };
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/campaigns/{campaignId}/approve", body);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<ApproveCampaignResponse>();
            return result ?? throw new InvalidOperationException("API returned an empty approve response.");
        }

        // POST api/tenant/{companyId}/campaigns/{id}/reject
        public async Task<CampaignResponseDto> RejectAsync(int campaignId, int decidedBy, string notes)
        {
            var body = new DecideCampaignRequest { DecidedBy = decidedBy, Notes = notes };
            var response = await _http.PostAsJsonAsync(
                $"api/tenant/{ApiConfig.CompanyId}/campaigns/{campaignId}/reject", body);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<CampaignResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty campaign.");
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