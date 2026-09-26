using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>Barista revenue — simple column bar chart per barista.</summary>
    public class BaristaSalesPerformanceControl : Panel
    {
        private readonly Chart _chart;
        private readonly Label _titleLabel;
        private readonly Label _statusLabel;

        private static readonly Color[] BarColors =
        {
            BrandPalette.Brand,
            BrandPalette.Tint50,
            BrandPalette.Tint90,
            Color.FromArgb(90, 130, 190),
            Color.FromArgb(160, 90, 160),
        };

        public BaristaSalesPerformanceControl()
        {
            Height      = 280;
            MinimumSize = new Size(100, 100);
            Margin      = new Padding(0, 0, 0, 10);
            BackColor   = BrandPalette.SurfacePrimary;

            _titleLabel = new Label
            {
                Text      = "Barista Sales",
                Dock      = DockStyle.Top,
                Height    = 32,
                Padding   = new Padding(14, 8, 0, 0),
                Font      = BrandPalette.BrandFontBold(10.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BrandPalette.Border };

            _statusLabel = new Label
            {
                Text      = "No records yet.",
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
            };
            area.AxisX.MajorGrid.Enabled   = false;
            area.AxisY.MajorGrid.LineColor  = BrandPalette.Border;
            area.AxisX.Interval             = 1;
            area.AxisX.IsMarginVisible      = true;
            area.AxisX.LabelStyle.Font      = BrandPalette.BrandFontRegular(8.5f);
            area.AxisY.LabelStyle.Font      = BrandPalette.BrandFontRegular(8.5f);
            area.AxisX.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.Title                = "Revenue ₱";
            area.AxisY.TitleFont            = BrandPalette.BrandFontBold(8f);

            _chart.ChartAreas.Add(area);

            Controls.Add(_chart);
            Controls.Add(_statusLabel);
            Controls.Add(divider);
            Controls.Add(_titleLabel);
        }

        public void BindData(BaristaSalesPerformanceDto data)
        {
            if (data.Baristas.Count == 0 || data.Baristas.All(b => b.OrdersProcessed == 0))
            {
                _statusLabel.Text    = "No barista records yet.";
                _statusLabel.Visible = true;
                _chart.Visible       = false;
                return;
            }

            _statusLabel.Visible = false;
            _chart.Visible       = true;
            _chart.Series.Clear();

            var series = new Series("Revenue")
            {
                ChartType           = SeriesChartType.Column,
                ChartArea           = "MainArea",
                Color               = BrandPalette.Brand,
                IsValueShownAsLabel = true,
                Font                = BrandPalette.BrandFontBold(8.5f),
                LabelForeColor      = BrandPalette.NeutralText,
                BorderWidth         = 0,
            };
            series["PointWidth"] = "0.5";

            int ci = 0;
            foreach (var b in data.Baristas)
            {
                var idx = series.Points.AddXY(b.BaristaName, (double)b.TotalRevenue);
                series.Points[idx].Label = $"₱{b.TotalRevenue:0}";
                series.Points[idx].Color = BarColors[ci % BarColors.Length];
                ci++;
            }

            _chart.Series.Add(series);
            _chart.Invalidate();
        }
    }
}