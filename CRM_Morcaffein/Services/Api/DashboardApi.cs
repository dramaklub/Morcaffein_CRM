using System.Net.Http.Json;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Api
{
    public class DashboardApi
    {
        private readonly HttpClient _http;

        public DashboardApi()
        {
            _http = ApiClientFactory.Create();
        }

        // GET api/tenant/{companyId}/dashboard/loyalty-tier-progression
        public async Task<LoyaltyTierProgressionDto> GetLoyaltyTierProgressionAsync(int months = 6)
        {
            var response = await _http.GetAsync(
                $"api/tenant/{ApiConfig.CompanyId}/dashboard/loyalty-tier-progression?months={months}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<LoyaltyTierProgressionDto>();
            return result ?? new LoyaltyTierProgressionDto();
        }

        // GET api/tenant/{companyId}/dashboard/peak-hours (static, not affected by range filter)
        public async Task<PeakHoursDto> GetPeakHoursAsync()
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/peak-hours");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<PeakHoursDto>();
            return result ?? new PeakHoursDto();
        }

        // GET api/tenant/{companyId}/dashboard/age-demographics
        public async Task<AgeDemographicsAovDto> GetAgeDemographicsAsync(int days = 30)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/age-demographics?days={days}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<AgeDemographicsAovDto>();
            return result ?? new AgeDemographicsAovDto();
        }

        // GET api/tenant/{companyId}/dashboard/campaign-lift
        public async Task<ActiveCampaignLiftDto> GetCampaignLiftAsync(int days = 14)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/campaign-lift?days={days}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<ActiveCampaignLiftDto>();
            return result ?? new ActiveCampaignLiftDto();
        }

        // GET api/tenant/{companyId}/dashboard/feedback-ratings
        public async Task<FeedbackRatingDistributionDto> GetFeedbackRatingsAsync(int days = 30)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/feedback-ratings?days={days}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<FeedbackRatingDistributionDto>();
            return result ?? new FeedbackRatingDistributionDto();
        }

        // GET api/tenant/{companyId}/dashboard/barista-sales
        public async Task<BaristaSalesPerformanceDto> GetBaristaSalesAsync(int days = 30)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/barista-sales?days={days}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<BaristaSalesPerformanceDto>();
            return result ?? new BaristaSalesPerformanceDto();
        }

        // GET api/tenant/{companyId}/dashboard/pos-diagnostics
        public async Task<PosDiagnosticsDto> GetPosDiagnosticsAsync(int days = 30)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/pos-diagnostics?days={days}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<PosDiagnosticsDto>();
            return result ?? new PosDiagnosticsDto();
        }

        // GET api/tenant/{companyId}/dashboard/campaign-roi
        public async Task<CampaignRoiDto> GetCampaignRoiAsync(int days = 30)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/campaign-roi?days={days}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<CampaignRoiDto>();
            return result ?? new CampaignRoiDto();
        }

        // GET api/tenant/{companyId}/dashboard/customer-ltv-churn
        public async Task<CustomerLtvChurnDto> GetCustomerLtvChurnAsync(int days = 30)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/customer-ltv-churn?days={days}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<CustomerLtvChurnDto>();
            return result ?? new CustomerLtvChurnDto();
        }

        // GET api/tenant/{companyId}/dashboard/audit-summary
        public async Task<AuditLogSummaryDto> GetAuditSummaryAsync(int days = 30)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/audit-summary?days={days}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<AuditLogSummaryDto>();
            return result ?? new AuditLogSummaryDto();
        }

        // GET api/tenant/{companyId}/dashboard/recent-orders
        public async Task<List<RecentOrderSummaryDto>> GetRecentOrdersAsync(int days = 30, int limit = 10)
        {
            var response = await _http.GetAsync($"api/tenant/{ApiConfig.CompanyId}/dashboard/recent-orders?days={days}&limit={limit}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<List<RecentOrderSummaryDto>>();
            return result ?? new List<RecentOrderSummaryDto>();
        }

        // GET api/tenant/{companyId}/dashboard/revenue-vs-discounts
        public async Task<RevenueVsDiscountsDto> GetRevenueVsDiscountsAsync(int days = 90)
        {
            var response = await _http.GetAsync(
                $"api/tenant/{ApiConfig.CompanyId}/dashboard/revenue-vs-discounts?days={days}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<RevenueVsDiscountsDto>();
            return result ?? new RevenueVsDiscountsDto();
        }

        // GET api/tenant/{companyId}/dashboard/new-vs-returning
        public async Task<NewVsReturningDto> GetNewVsReturningAsync(int months = 6)
        {
            var response = await _http.GetAsync(
                $"api/tenant/{ApiConfig.CompanyId}/dashboard/new-vs-returning?months={months}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<NewVsReturningDto>();
            return result ?? new NewVsReturningDto();
        }
        // GET api/tenant/{companyId}/dashboard/top-products
        public async Task<TopProductsResponseDto> GetTopProductsAsync(int days = 30, int limit = 5)
        {
            var response = await _http.GetAsync(
                $"api/tenant/{ApiConfig.CompanyId}/dashboard/top-products?days={days}&limit={limit}");
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<TopProductsResponseDto>();
            return result ?? new TopProductsResponseDto();
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
