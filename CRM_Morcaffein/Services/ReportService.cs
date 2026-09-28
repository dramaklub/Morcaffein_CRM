using CRM_Morcaffein.winforms.Services.Api;
using CRM_Morcaffein.winforms.Services.Reports;

namespace CRM_Morcaffein.winforms.Services
{
    /// <summary>
    /// Orchestrates report generation: fetch DTOs via DashboardService, build a
    /// ReportDocument, render it to PDF bytes. Caller (the dashboard's Export
    /// button) writes the bytes to disk via SaveFileDialog.
    ///
    /// Every branch-scoped DTO is fetched with the caller's current branch
    /// filter so the exported report matches what's on screen. The server
    /// (ResolveEffectiveBranchIdAsync) forces pinned users to their own branch
    /// regardless of what we send, so no additional client-side gating is
    /// needed here — we just pass through what the topbar currently holds.
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
            var pos = await _dash.GetPosDiagnosticsAsync(days, branchId);
            var peak = await _dash.GetPeakHoursAsync();
            var age = await _dash.GetAgeDemographicsAsync(days, branchId);
            var topPrd = await _dash.GetTopProductsAsync(days, 5, branchId);
            var lift = await _dash.GetCampaignLiftAsync(days, branchId);
            var csat = await _dash.GetFeedbackRatingsAsync(days, branchId);
            var tier = await _dash.GetLoyaltyTierProgressionAsync(6);

            var doc = ReportBuilder.BuildManager(days, pos, peak, age, topPrd, lift, csat, tier);
            return MigraDocRenderer.Render(doc);
        }

        public async Task<byte[]> GenerateAdminReportAsync(int days, int? branchId)
        {
            var pos = await _dash.GetPosDiagnosticsAsync(days, branchId);
            var ltv = await _dash.GetCustomerLtvChurnAsync(days);
            var revDisc = await _dash.GetRevenueVsDiscountsAsync(days);
            var newRet = await _dash.GetNewVsReturningAsync(6);
            var peak = await _dash.GetPeakHoursAsync();
            var audit = await _dash.GetAuditSummaryAsync(days);

            var doc = ReportBuilder.BuildAdmin(days, pos, ltv, revDisc, newRet, peak, audit);
            return MigraDocRenderer.Render(doc);
        }

        public async Task<byte[]> GenerateTransactionLedgerAsync(int days, int? branchId)
        {
            var ledger = await _dash.GetTransactionLedgerAsync(days, branchId);

            var doc = ReportBuilder.BuildTransactionLedger(days, ledger);
            return MigraDocRenderer.Render(doc);
        }

        /// <summary>
        /// Platform-wide report for the SuperAdmin. Only fetches the one
        /// endpoint — the SuperAdmin dashboard data is a single combined
        /// payload already.
        /// </summary>
        public async Task<byte[]> GenerateSuperAdminReportAsync()
        {
            var dashboard = await _superAdminApi.GetDashboardAsync();

            var doc = ReportBuilder.BuildSuperAdmin(dashboard);
            return MigraDocRenderer.Render(doc);
        }
    }
}