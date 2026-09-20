using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.Controllers
{
    /// <summary>
    /// UI controller for the Dashboard tab (WinForms). Pure coordination: the View
    /// calls these methods, they forward to the API. All aggregation logic lives on
    /// the API side (DashboardController in CRM_Morcaffein.api).
    /// </summary>
    public class DashboardController
    {
        private readonly DashboardApi _api;

        public DashboardController()
        {
            _api = new DashboardApi();
        }

        public Task<LoyaltyTierProgressionDto> GetLoyaltyTierProgressionAsync(int months = 6)
            => _api.GetLoyaltyTierProgressionAsync(months);

        public Task<PeakHoursDto> GetPeakHoursAsync()
            => _api.GetPeakHoursAsync();

        public Task<AgeDemographicsAovDto> GetAgeDemographicsAsync(int days = 30)
            => _api.GetAgeDemographicsAsync(days);

        public Task<ActiveCampaignLiftDto> GetCampaignLiftAsync(int days = 14)
            => _api.GetCampaignLiftAsync(days);

        public Task<FeedbackRatingDistributionDto> GetFeedbackRatingsAsync(int days = 30)
            => _api.GetFeedbackRatingsAsync(days);

        public Task<BaristaSalesPerformanceDto> GetBaristaSalesAsync(int days = 30)
            => _api.GetBaristaSalesAsync(days);

        public Task<PosDiagnosticsDto> GetPosDiagnosticsAsync(int days = 30)
            => _api.GetPosDiagnosticsAsync(days);

        public Task<CampaignRoiDto> GetCampaignRoiAsync(int days = 30)
            => _api.GetCampaignRoiAsync(days);

        public Task<CustomerLtvChurnDto> GetCustomerLtvChurnAsync(int days = 30)
            => _api.GetCustomerLtvChurnAsync(days);

        public Task<AuditLogSummaryDto> GetAuditSummaryAsync(int days = 30)
            => _api.GetAuditSummaryAsync(days);

        public Task<List<RecentOrderSummaryDto>> GetRecentOrdersAsync(int days = 30, int limit = 10)
    => _api.GetRecentOrdersAsync(days, limit);

        public Task<RevenueVsDiscountsDto> GetRevenueVsDiscountsAsync(int days = 90)
            => _api.GetRevenueVsDiscountsAsync(days);

        public Task<NewVsReturningDto> GetNewVsReturningAsync(int months = 6)
            => _api.GetNewVsReturningAsync(months);

        public Task<TopProductsResponseDto> GetTopProductsAsync(int days = 30, int limit = 5)
            => _api.GetTopProductsAsync(days, limit);
    }
}
