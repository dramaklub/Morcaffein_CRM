using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Chart 7: Cash Register & POS Diagnostics (Manager Only).
    /// Displays gross sales, campaign discounts, PWD/Senior discounts, and net sales totals.
    /// Clean borderless card strip.
    /// </summary>
    public class PosDiagnosticsControl : Panel
    {
        private readonly Label _titleLabel;
        private readonly Label _statusLabel;
        private readonly TableLayoutPanel _cardsGrid;

        private readonly Label _grossValLabel = MakeValueLabel();
        private readonly Label _campaignDiscValLabel = MakeValueLabel();
        private readonly Label _pwdDiscValLabel = MakeValueLabel();
        private readonly Label _netValLabel = MakeValueLabel();

        public PosDiagnosticsControl()
        {
            Height = 160;
            Margin = new Padding(0, 0, 0, 10);
            BackColor = BrandPalette.SurfacePrimary;

            _titleLabel = new Label
            {
                Text = "Register Summary",
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(14, 8, 0, 0),
                Font = BrandPalette.BrandFontBold(10.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BrandPalette.Border,
            };

            _statusLabel = new Label
            {
                Text = "Loading register diagnostics...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _cardsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(12, 8, 12, 8),
                Visible = false,
            };

            for (int i = 0; i < 4; i++)
                _cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _cardsGrid.Controls.Add(BuildMetricCard("Gross Sales", _grossValLabel), 0, 0);
            _cardsGrid.Controls.Add(BuildMetricCard("Promo Discounts", _campaignDiscValLabel), 1, 0);
            _cardsGrid.Controls.Add(BuildMetricCard("PWD / Senior", _pwdDiscValLabel), 2, 0);
            _cardsGrid.Controls.Add(BuildMetricCard("Net Revenue", _netValLabel), 3, 0);

            Controls.Add(_cardsGrid);
            Controls.Add(_statusLabel);
            Controls.Add(divider);
            Controls.Add(_titleLabel);
        }

        private static Label MakeValueLabel() => new()
        {
            Text = "₱0.00",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = BrandPalette.BrandFontBold(13f),
            ForeColor = BrandPalette.Brand,
        };

        private static Panel BuildMetricCard(string title, Label valueLabel)
        {
            var p = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
                Padding = new Padding(8),
                Margin = new Padding(4),
            };

            var titleLbl = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            p.Controls.Add(valueLabel);
            p.Controls.Add(titleLbl);
            return p;
        }

        public void BindData(PosDiagnosticsDto data)
        {
            _statusLabel.Visible = false;
            _cardsGrid.Visible = true;

            _grossValLabel.Text = $"₱{data.GrossSales:N2}\n({data.TotalTransactions} orders)";
            _campaignDiscValLabel.Text = $"₱{data.TotalCampaignDiscounts:N2}\n({data.CampaignTransactionCount} used)";
            _pwdDiscValLabel.Text = $"₱{data.TotalPwdSeniorDiscounts:N2}\n({data.PwdSeniorTransactionCount} used)";
            _netValLabel.Text = $"₱{data.NetSales:N2}";
        }
    }
}
