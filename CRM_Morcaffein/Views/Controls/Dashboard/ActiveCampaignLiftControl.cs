using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Sales & Promo Usage — dual-axis line chart.
    /// Line 1 (Primary Left Y): Daily Sales (Brand Brown, circle markers)
    /// Line 2 (Secondary Right Y): Promos Used (Caramel, diamond markers)
    ///
    /// X-axis label density is auto-computed: for short ranges (≤14 days) every
    /// day is labeled; for longer ranges only every Nth day is labeled, keeping
    /// the axis readable without crowding.
    /// </summary>
    public class ActiveCampaignLiftControl : Panel
    {
        private readonly Chart _chart;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Label _statusLabel;

        public ActiveCampaignLiftControl()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            Margin = new Padding(3);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = "Sales & Promo Usage",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
            };

            _subtitleLabel = new Label
            {
                Text = "Daily sales vs promo usage",
                Dock = DockStyle.Top,
                Height = 20,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _statusLabel = new Label
            {
                Text = "No sales or promo activity in this period.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Visible = false,
            };

            _chart = new Chart
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(50, 50),
                BackColor = BrandPalette.SurfacePrimary,
                BorderlineColor = Color.Transparent,
                BorderlineWidth = 0,
                Visible = false,
            };

            var area = new ChartArea("MainArea")
            {
                BackColor = BrandPalette.SurfacePrimary,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
            };

            // ── X axis (dates) ──
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.MajorTickMark.Enabled = true;
            area.AxisX.MajorTickMark.LineColor = BrandPalette.Border;
            area.AxisX.LineColor = BrandPalette.Border;
            area.AxisX.IsMarginVisible = true;
            area.AxisX.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisX.LabelStyle.ForeColor = BrandPalette.NeutralText;
            area.AxisX.LabelStyle.Angle = 0;
            // Interval is set dynamically in BindData based on point count.

            // ── Primary Y axis (left — Sales, whole pesos) ──
            area.AxisY.MajorGrid.LineColor = BrandPalette.Border;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.LineColor = Color.Transparent;
            area.AxisY.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisY.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.LabelStyle.Format = "₱#,##0";     // ← whole pesos, no decimals
            area.AxisY.Title = "Sales (₱)";
            area.AxisY.TitleFont = BrandPalette.BrandFontBold(8f);
            area.AxisY.TitleForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.Minimum = 0;
            area.AxisY.IsStartedFromZero = true;

            // ── Secondary Y axis (right — Promos Used count) ──
            area.AxisY2.MajorGrid.Enabled = false;
            area.AxisY2.MajorTickMark.Enabled = false;
            area.AxisY2.LineColor = Color.Transparent;
            area.AxisY2.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisY2.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY2.LabelStyle.Format = "0";
            area.AxisY2.Title = "Promos Used";
            area.AxisY2.TitleFont = BrandPalette.BrandFontBold(8f);
            area.AxisY2.TitleForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY2.Minimum = 0;
            area.AxisY2.IsStartedFromZero = true;

            _chart.ChartAreas.Add(area);

            var legend = new Legend("L")
            {
                Font = BrandPalette.BrandFontBold(8.5f),
                ForeColor = BrandPalette.NeutralText,
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                BackColor = Color.Transparent,
                LegendStyle = LegendStyle.Row,
                IsDockedInsideChartArea = false,
            };
            _chart.Legends.Add(legend);

            Controls.Add(_chart);
            Controls.Add(_statusLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_titleLabel);
        }

        public void BindData(ActiveCampaignLiftDto data)
        {
            if (data == null || data.DailyPoints.Count == 0 ||
                data.DailyPoints.All(p => p.DailyRevenue == 0 && p.DailyRedemptions == 0))
            {
                _statusLabel.Text = "No sales or promo activity in this period.";
                _statusLabel.Visible = true;
                _chart.Visible = false;
                _subtitleLabel.Text = "Daily sales vs promo usage";
                return;
            }

            _statusLabel.Visible = false;
            _chart.Visible = true;
            _chart.Series.Clear();

            var totalRev = data.DailyPoints.Sum(p => p.DailyRevenue);
            var totalRed = data.DailyPoints.Sum(p => p.DailyRedemptions);
            _subtitleLabel.Text = $"Total Sales: ₱{totalRev:N0}    ·    Total Promos Used: {totalRed:N0}";

            var area = _chart.ChartAreas["MainArea"];
            var maxRev = (double)data.DailyPoints.Max(p => p.DailyRevenue);
            var maxRed = (double)data.DailyPoints.Max(p => p.DailyRedemptions);

            area.AxisY.Maximum = Math.Max(100, maxRev * 1.2);
            area.AxisY2.Maximum = Math.Max(5, maxRed * 1.3);

            // ── Dynamic X-axis label density ──
            // For ≤14 days, show every day. For >14 days, show every Nth day where N
            // is chosen so we get roughly 6-8 labels across the range.
            var pointCount = data.DailyPoints.Count;
            int interval;
            if (pointCount <= 14) interval = 1;
            else if (pointCount <= 21) interval = 3;
            else if (pointCount <= 35) interval = 5;
            else interval = Math.Max(1, pointCount / 7);
            area.AxisX.Interval = interval;

            // ── Line 1: Sales ──
            var seriesRev = new Series("Sales")
            {
                ChartType = SeriesChartType.Line,
                ChartArea = "MainArea",
                Legend = "L",
                Color = BrandPalette.Brand,
                BorderWidth = 3,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 8,
                MarkerColor = BrandPalette.Brand,
                MarkerBorderColor = BrandPalette.SurfacePrimary,
                MarkerBorderWidth = 2,
                IsXValueIndexed = true,
                YAxisType = AxisType.Primary,
            };

            // ── Line 2: Promos Used ──
            var seriesRed = new Series("Promos Used")
            {
                ChartType = SeriesChartType.Line,
                ChartArea = "MainArea",
                Legend = "L",
                Color = Color.FromArgb(190, 130, 60),
                BorderWidth = 3,
                MarkerStyle = MarkerStyle.Diamond,
                MarkerSize = 9,
                MarkerColor = Color.FromArgb(190, 130, 60),
                MarkerBorderColor = BrandPalette.SurfacePrimary,
                MarkerBorderWidth = 2,
                IsXValueIndexed = true,
                YAxisType = AxisType.Secondary,
            };

            foreach (var pt in data.DailyPoints)
            {
                var revIdx = seriesRev.Points.AddXY(pt.DateLabel, (double)pt.DailyRevenue);
                seriesRev.Points[revIdx].ToolTip = $"{pt.DateLabel}\nSales: ₱{pt.DailyRevenue:N0}";

                var redIdx = seriesRed.Points.AddXY(pt.DateLabel, pt.DailyRedemptions);
                seriesRed.Points[redIdx].ToolTip = $"{pt.DateLabel}\nPromos Used: {pt.DailyRedemptions}";
            }

            _chart.Series.Add(seriesRev);
            _chart.Series.Add(seriesRed);
            _chart.Invalidate();
        }
    }
}