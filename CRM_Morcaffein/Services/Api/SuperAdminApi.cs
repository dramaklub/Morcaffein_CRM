using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class SuperAdminApi
    {
        private readonly HttpClient _http;

        public SuperAdminApi()
        {
            _http = ApiClientFactory.Create();
        }

        // GET api/superadmin/companies
        public async Task<List<SuperAdminCompanyDto>> GetCompaniesAsync()
        {
            var response = await _http.GetAsync("api/superadmin/companies");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<SuperAdminCompanyDto>>();
            return result ?? new List<SuperAdminCompanyDto>();
        }

        // GET api/superadmin/plans
        public async Task<List<SuperAdminPlanDto>> GetPlansAsync()
        {
            var response = await _http.GetAsync("api/superadmin/plans");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<SuperAdminPlanDto>>();
            return result ?? new List<SuperAdminPlanDto>();
        }

        // POST api/superadmin/companies/{id}/assign-plan
        public async Task<SubscriptionResponseDto> AssignPlanAsync(
            int companyId,
            int planId,
            string termsAcceptedBy)
        {
            var body = new AssignPlanRequest
            {
                PlanId = planId,
                TermsAcceptedBy = termsAcceptedBy,
            };

            var response = await _http.PostAsJsonAsync(
                $"api/superadmin/companies/{companyId}/assign-plan", body);

            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<SubscriptionResponseDto>();
            return result ?? throw new InvalidOperationException("API returned an empty subscription.");
        }

        // GET api/superadmin/companies/{id}/subscriptions
        public async Task<List<SubscriptionResponseDto>> GetSubscriptionsAsync(int companyId)
        {
            var response = await _http.GetAsync(
                $"api/superadmin/companies/{companyId}/subscriptions");

            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<SubscriptionResponseDto>>();
            return result ?? new List<SubscriptionResponseDto>();
        }

        public async Task<SuperAdminDashboardDto> GetDashboardAsync()
        {
            var response = await _http.GetAsync("api/superadmin/dashboard");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<SuperAdminDashboardDto>();
            return result ?? new SuperAdminDashboardDto();
        }
        // GET api/superadmin/terms
        public async Task<List<TermsSummaryDto>> GetTermsListAsync()
        {
            var response = await _http.GetAsync("api/superadmin/terms");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<TermsSummaryDto>>();
            return result ?? new List<TermsSummaryDto>();
        }

        // GET api/superadmin/terms/{id}
        public async Task<TermsDetailDto> GetTermsDetailAsync(int termsId)
        {
            var response = await _http.GetAsync($"api/superadmin/terms/{termsId}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<TermsDetailDto>();
            return result ?? new TermsDetailDto();
        }

        // PUT api/superadmin/terms/{id}
        public async Task<TermsDetailDto> UpdateTermsAsync(int termsId, string body)
        {
            var request = new UpdateTermsRequest { Body = body };

            var response = await _http.PutAsJsonAsync(
                $"api/superadmin/terms/{termsId}", request);

            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<TermsDetailDto>();
            return result ?? throw new InvalidOperationException("API returned an empty terms document.");
        }

        // GET api/superadmin/companies/{id}/admin
        public async Task<SuperAdminAdminAccountDto> GetAdminAsync(int companyId)
        {
            var response = await _http.GetAsync(
                $"api/superadmin/companies/{companyId}/admin");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<SuperAdminAdminAccountDto>();
            return result ?? new SuperAdminAdminAccountDto();
        }

        // POST api/superadmin/companies/{id}/admin
        public async Task<SuperAdminAdminAccountDto> CreateAdminAsync(
            int companyId,
            CreateCompanyAdminRequest request)
        {
            var response = await _http.PostAsJsonAsync(
                $"api/superadmin/companies/{companyId}/admin", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<SuperAdminAdminAccountDto>();
            return result ?? throw new InvalidOperationException("API returned an empty admin account.");
        }

        // PUT api/superadmin/companies/{id}/admin
        public async Task<SuperAdminAdminAccountDto> UpdateAdminAsync(
            int companyId,
            UpdateCompanyAdminRequest request)
        {
            var response = await _http.PutAsJsonAsync(
                $"api/superadmin/companies/{companyId}/admin", request);
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<SuperAdminAdminAccountDto>();
            return result ?? throw new InvalidOperationException("API returned an empty admin account.");
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