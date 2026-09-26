using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.Controllers
{
    /// <summary>
    /// UI controller for the Dashboard tab (WinForms). Pure coordination: the View
    /// calls these methods, they forward to the API. All aggregation logic lives on
    /// the API side (DashboardController in CRM_Morcaffein.api).
    ///
    /// Branch scoping: methods that aggregate orders, feedbacks, redemptions, or
    /// product sales accept an optional `branchId`. Passing null means "All
    /// Branches" (company-wide). The view's topbar selector supplies the current
    /// value from EmployeeSession.CurrentBranchId.
    ///
    /// Company-wide metrics (peak hours, LTV, retention, audit, revenue-vs-discounts,
    /// new-vs-returning, visit frequency, campaign ROI, barista sales) deliberately
    /// do NOT take a branchId — those don't meaningfully scope to one location.
    /// </summary>
    public class DashboardController
    {
        private readonly DashboardApi _api;

        public DashboardController()
        {
            _api = new DashboardApi();
        }

        // ============================================================
        // Company-wide endpoints (no branch scoping)
        // ============================================================

        public Task<LoyaltyTierProgressionDto> GetLoyaltyTierProgressionAsync(int months = 6)
            => _api.GetLoyaltyTierProgressionAsync(months);

        public Task<PeakHoursDto> GetPeakHoursAsync()
            => _api.GetPeakHoursAsync();

        public Task<CustomerLtvChurnDto> GetCustomerLtvChurnAsync(int days = 30)
            => _api.GetCustomerLtvChurnAsync(days);

        public Task<AuditLogSummaryDto> GetAuditSummaryAsync(int days = 30)
            => _api.GetAuditSummaryAsync(days);

        public Task<RevenueVsDiscountsDto> GetRevenueVsDiscountsAsync(int days = 90)
            => _api.GetRevenueVsDiscountsAsync(days);

        public Task<NewVsReturningDto> GetNewVsReturningAsync(int months = 6)
            => _api.GetNewVsReturningAsync(months);

        public Task<VisitFrequencyDto> GetVisitFrequencyAsync()
            => _api.GetVisitFrequencyAsync();

        public Task<CampaignRoiDto> GetCampaignRoiAsync(int days = 30)
            => _api.GetCampaignRoiAsync(days);

        public Task<BaristaSalesPerformanceDto> GetBaristaSalesAsync(int days = 30)
            => _api.GetBaristaSalesAsync(days);

        // ============================================================
        // Branch-scoped endpoints (null = all branches)
        // ============================================================

        public Task<PosDiagnosticsDto> GetPosDiagnosticsAsync(int days = 30, int? branchId = null)
            => _api.GetPosDiagnosticsAsync(days, branchId);

        public Task<List<RecentOrderSummaryDto>> GetRecentOrdersAsync(
            int days = 30, int limit = 10, int? branchId = null)
            => _api.GetRecentOrdersAsync(days, limit, branchId);

        public Task<TransactionLedgerDto> GetTransactionLedgerAsync(int days = 30, int? branchId = null)
            => _api.GetTransactionLedgerAsync(days, branchId);

        public Task<ActiveCampaignLiftDto> GetCampaignLiftAsync(int days = 14, int? branchId = null)
            => _api.GetCampaignLiftAsync(days, branchId);

        public Task<AgeDemographicsAovDto> GetAgeDemographicsAsync(int days = 30, int? branchId = null)
            => _api.GetAgeDemographicsAsync(days, branchId);

        public Task<TopProductsResponseDto> GetTopProductsAsync(
            int days = 30, int limit = 5, int? branchId = null)
            => _api.GetTopProductsAsync(days, limit, branchId);

        public Task<FeedbackRatingDistributionDto> GetFeedbackRatingsAsync(
            int days = 30, int? branchId = null)
            => _api.GetFeedbackRatingsAsync(days, branchId);

        public Task<RecentEmployeesDto> GetRecentEmployeesAsync(int limit = 5)
             => _api.GetRecentEmployeesAsync(limit);

        public Task<RecentFeedbackDto> GetRecentFeedbackAsync(int limit = 5, int? branchId = null)
            => _api.GetRecentFeedbackAsync(limit, branchId);
    }
}