using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.winforms.Services.Api;
using CRM_Morcaffein.winforms.Services.Reports;

namespace CRM_Morcaffein.winforms.Services
{
    /// <summary>
    /// Orchestrates report generation: fetch DTOs via DashboardService, build a
    /// ReportDocument, render it to PDF bytes.
    ///
    /// BI-gated sections are conditionally included based on the tenant's
    /// entitlement. A Basic tenant (no BusinessIntelligence module) gets a
    /// reduced report that mirrors what their dashboard actually shows —
    /// rather than a full report with sections they shouldn't see.
    ///
    /// The server independently rejects BI endpoint calls for non-BI tenants,
    /// so this client-side filtering is UX, not a security boundary.
    /// </summary>
    public class ReportService
    {
        private readonly DashboardService _dash;
        private readonly SuperAdminApi _superAdminApi;

        public ReportService()
        {
            _dash = new DashboardService();
            _superAdminApi = new SuperAdminApi();
        }

        public async Task<byte[]> GenerateManagerReportAsync(int days, int? branchId)
        {
            var hasBI = EmployeeSession.Current?.HasModule("BusinessIntelligence") == true;

            // Always-present (operational) data
            var pos = await _dash.GetPosDiagnosticsAsync(days, branchId);
            var peak = await _dash.GetPeakHoursAsync();
            var csat = await _dash.GetFeedbackRatingsAsync(days, branchId);

            // BI-gated data — only fetched for BusinessIntelligence tenants.
            AgeDemographicsAovDto? age = null;
            TopProductsResponseDto? topPrd = null;
            ActiveCampaignLiftDto? lift = null;
            LoyaltyTierProgressionDto? tier = null;

            if (hasBI)
            {
                age = await _dash.GetAgeDemographicsAsync(days, branchId);
                topPrd = await _dash.GetTopProductsAsync(days, 5, branchId);
                lift = await _dash.GetCampaignLiftAsync(days, branchId);
                tier = await _dash.GetLoyaltyTierProgressionAsync(6);
            }

            var doc = ReportBuilder.BuildManager(
                days, pos, peak, age, topPrd, lift, csat, tier, hasBI);

            return MigraDocRenderer.Render(doc);
        }

        public async Task<byte[]> GenerateAdminReportAsync(int days, int? branchId)
        {
            var hasBI = EmployeeSession.Current?.HasModule("BusinessIntelligence") == true;

            // Always-present (operational) data
            // Always-present (operational) data
            var pos = await _dash.GetPosDiagnosticsAsync(days, branchId);
            var peak = await _dash.GetPeakHoursAsync();
            var audit = await _dash.GetAuditSummaryAsync(days);

            // BI-gated data
            CustomerLtvChurnDto? ltv = null;
            RevenueVsDiscountsDto? revDisc = null;
            NewVsReturningDto? newRet = null;

            if (hasBI)
            {
                ltv = await _dash.GetCustomerLtvChurnAsync(days);
                revDisc = await _dash.GetRevenueVsDiscountsAsync(days);
                newRet = await _dash.GetNewVsReturningAsync(6);
            }

            // For Basic-tier Admin, the dashboard shows Recent Employees + Recent Feedback
            // instead of the BI charts. Include those in the report too.
            RecentEmployeesDto? recentEmp = null;
            RecentFeedbackDto? recentFb = null;

            if (!hasBI)
            {
                recentEmp = await _dash.GetRecentEmployeesAsync(5);
                recentFb = await _dash.GetRecentFeedbackAsync(5, branchId);
            }

            var doc = ReportBuilder.BuildAdmin(
                days, pos, ltv, revDisc, newRet, peak, audit,
                hasBI, recentEmp, recentFb);

            return MigraDocRenderer.Render(doc);
        }

        public async Task<byte[]> GenerateTransactionLedgerAsync(int days, int? branchId)
        {
            var ledger = await _dash.GetTransactionLedgerAsync(days, branchId);
            var doc = ReportBuilder.BuildTransactionLedger(days, ledger);
            return MigraDocRenderer.Render(doc);
        }

        /// <summary>
        /// Platform-wide report for the SuperAdmin.
        /// </summary>
        public async Task<byte[]> GenerateSuperAdminReportAsync()
        {
            var dashboard = await _superAdminApi.GetDashboardAsync();
            var doc = ReportBuilder.BuildSuperAdmin(dashboard);
            return MigraDocRenderer.Render(doc);
        }
    }
}