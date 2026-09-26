using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>Customer LTV per tier — column chart with churn count in title.</summary>
    public class CustomerLtvChurnControl : Panel
    {
        private readonly Chart _chart;
        private readonly Label _titleLabel;
        private readonly Label _statusLabel;

        private static readonly Color[] TierColors =
        {
            BrandPalette.NeutralTextMuted,           // Regular
            Color.FromArgb(192, 192, 192),           // Silver
            Color.FromArgb(212, 175,  55),           // Gold
        };

        public CustomerLtvChurnControl()
        {
            Height      = 280;
            MinimumSize = new Size(100, 100);
            Margin      = new Padding(0, 0, 0, 16);
            BackColor   = BrandPalette.SurfacePrimary;

            _titleLabel = new Label
            {
                Text      = "Avg LTV by Tier",
                Dock      = DockStyle.Top,
                Height    = 36,
                Padding   = new Padding(14, 10, 0, 0),
                Font      = BrandPalette.BrandFontBold(10.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BrandPalette.Border };

            _statusLabel = new Label
            {
                Text      = "No LTV data yet.",
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _chart = new Chart
            {
                Dock            = DockStyle.Fill,
                MinimumSize     = new Size(50, 50),
                BackColor       = BrandPalette.SurfacePrimary,
                BorderlineColor = Color.Transparent,
                BorderlineWidth = 0,
                Visible         = false,
            };

            var area = new ChartArea("MainArea")
            {
                BackColor   = BrandPalette.SurfacePrimary,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
                Position    = new ElementPosition(2, 2, 96, 94),
            };
            area.AxisX.MajorGrid.Enabled   = false;
            area.AxisY.MajorGrid.LineColor  = BrandPalette.Border;
            area.AxisX.LabelStyle.Font      = BrandPalette.BrandFontRegular(9f);
            area.AxisY.LabelStyle.Font      = BrandPalette.BrandFontRegular(8.5f);
            area.AxisX.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.Title                = "Avg ₱ Spend";
            area.AxisY.TitleFont            = BrandPalette.BrandFontBold(8f);

            _chart.ChartAreas.Add(area);

            Controls.Add(_chart);
            Controls.Add(_statusLabel);
            Controls.Add(divider);
            Controls.Add(_titleLabel);
        }

        public void BindData(CustomerLtvChurnDto data)
        {
            _titleLabel.Text     = $"Avg LTV by Tier  ·  {data.AtRiskHighTierCount} at-risk";
            _statusLabel.Visible = false;
            _chart.Visible       = true;
            _chart.Series.Clear();

            var series = new Series("LTV")
            {
                ChartType           = SeriesChartType.Column,
                ChartArea           = "MainArea",
                Color               = BrandPalette.Brand,
                IsValueShownAsLabel = true,
                Font                = BrandPalette.BrandFontBold(8.5f),
                LabelForeColor      = BrandPalette.NeutralText,
                BorderWidth         = 0,
            };

            var tiers = new[]
            {
                ("Regular", data.RegularTierAvgLtv),
                ("Silver",  data.SilverTierAvgLtv),
                ("Gold",    data.GoldTierAvgLtv),
            };

            int ci = 0;
            foreach (var (name, val) in tiers)
            {
                var idx = series.Points.AddXY(name, (double)val);
                series.Points[idx].Label = $"₱{val:N0}";
                series.Points[idx].Color = TierColors[ci++];
            }

            _chart.Series.Add(series);
            _chart.Invalidate();
        }
    }
}