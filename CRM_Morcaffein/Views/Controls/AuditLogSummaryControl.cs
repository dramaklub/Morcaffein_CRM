using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Chart 10: Staff Overrides & Audit Logs (Admin Only).
    /// Displays counts of manual point adjustments, bonus points awarded, and pending decisions.
    /// Clean borderless card strip.
    /// </summary>
    public class AuditLogSummaryControl : Panel
    {
        private readonly Label _titleLabel;
        private readonly Label _statusLabel;
        private readonly TableLayoutPanel _cardsGrid;

        private readonly Label _totalAdjValLabel = MakeValueLabel();
        private readonly Label _approvedAdjValLabel = MakeValueLabel();
        private readonly Label _bonusPtsValLabel = MakeValueLabel();
        private readonly Label _pendingCampValLabel = MakeValueLabel();

        public AuditLogSummaryControl()
        {
            Height    = 160;
            Margin    = new Padding(0, 0, 0, 10);
            BackColor = BrandPalette.SurfacePrimary;

            _titleLabel = new Label
            {
                Text      = "Staff Overrides & Governance Logs",
                Dock      = DockStyle.Top,
                Height    = 32,
                Padding   = new Padding(14, 8, 0, 0),
                Font      = BrandPalette.BrandFontBold(10.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var divider = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = BrandPalette.Border,
            };

            _statusLabel = new Label
            {
                Text      = "Loading audit summary log...",
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _cardsGrid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 1,
                Padding     = new Padding(12, 8, 12, 8),
                Visible     = false,
            };

            for (int i = 0; i < 4; i++)
                _cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _cardsGrid.Controls.Add(BuildMetricCard("Point Requests", _totalAdjValLabel), 0, 0);
            _cardsGrid.Controls.Add(BuildMetricCard("Approved Adjustments", _approvedAdjValLabel), 1, 0);
            _cardsGrid.Controls.Add(BuildMetricCard("CSAT Bonus Points", _bonusPtsValLabel), 2, 0);
            _cardsGrid.Controls.Add(BuildMetricCard("Pending Campaigns", _pendingCampValLabel), 3, 0);

            Controls.Add(_cardsGrid);
            Controls.Add(_statusLabel);
            Controls.Add(divider);
            Controls.Add(_titleLabel);
        }

        private static Label MakeValueLabel() => new()
        {
            Text      = "0",
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = BrandPalette.BrandFontBold(13f),
            ForeColor = BrandPalette.Brand,
        };

        private static Panel BuildMetricCard(string title, Label valueLabel)
        {
            var p = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
                Padding   = new Padding(6),
                Margin    = new Padding(4),
            };

            var titleLbl = new Label
            {
                Text      = title,
                Dock      = DockStyle.Top,
                Height    = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            p.Controls.Add(valueLabel);
            p.Controls.Add(titleLbl);
            return p;
        }

        public void BindData(AuditLogSummaryDto data)
        {
            _statusLabel.Visible = false;
            _cardsGrid.Visible   = true;

            _totalAdjValLabel.Text     = $"{data.TotalManualPointAdjustments}";
            _approvedAdjValLabel.Text  = $"{data.ApprovedPointAdjustments} approved ({data.RejectedPointAdjustments} rejected)";
            _bonusPtsValLabel.Text     = $"{data.FeedbackBonusPointsAwarded} tickets";
            _pendingCampValLabel.Text  = $"{data.PendingCampaignApprovals} pending";
        }
    }
}
