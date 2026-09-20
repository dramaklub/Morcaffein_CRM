using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.winforms.Services.Reports;

namespace CRM_Morcaffein.Controllers
{
    /// <summary>
    /// Orchestrates report generation: fetch DTOs via DashboardController, build a
    /// ReportDocument, render it to PDF bytes. Caller (the dashboard's Export
    /// button) writes the bytes to disk via SaveFileDialog.
    /// </summary>
    public class ReportController
    {
        private readonly DashboardController _dash;

        public ReportController() { _dash = new DashboardController(); }

        public async Task<byte[]> GenerateManagerReportAsync(int days)
        {
            var pos = await _dash.GetPosDiagnosticsAsync(days);
            var peak = await _dash.GetPeakHoursAsync();
            var age = await _dash.GetAgeDemographicsAsync(days);
            var topPrd = await _dash.GetTopProductsAsync(days, 5);
            var lift = await _dash.GetCampaignLiftAsync(days);
            var csat = await _dash.GetFeedbackRatingsAsync(days);
            var tier = await _dash.GetLoyaltyTierProgressionAsync(6);

            var doc = ReportBuilder.BuildManager(days, pos, peak, age, topPrd, lift, csat, tier);
            return MigraDocRenderer.Render(doc);
        }

        public async Task<byte[]> GenerateAdminReportAsync(int days)
        {
            var pos = await _dash.GetPosDiagnosticsAsync(days);
            var ltv = await _dash.GetCustomerLtvChurnAsync(days);
            var revDisc = await _dash.GetRevenueVsDiscountsAsync(days);
            var newRet = await _dash.GetNewVsReturningAsync(6);
            var peak = await _dash.GetPeakHoursAsync();
            var audit = await _dash.GetAuditSummaryAsync(days);

            var doc = ReportBuilder.BuildAdmin(days, pos, ltv, revDisc, newRet, peak, audit);
            return MigraDocRenderer.Render(doc);
        }
    }
}