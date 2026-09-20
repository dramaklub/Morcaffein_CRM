using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Admin dashboard: Revenue vs Discounts over time.
    /// Three lines, single Y-axis (₱), so the reader can directly compare
    /// how the discount lines move relative to the net-revenue line.
    ///   - Net Revenue (brand brown, thick) — what customers actually paid
    ///   - Campaign Discounts (caramel) — promo discounts, a business choice
    ///   - PWD/Senior Discounts (grey) — legally-mandated, kept separate
    ///
    /// The story: if the two discount lines trend up while net revenue stays
    /// flat, the shop is becoming more promotion-dependent to move the same
    /// amount of product.
    /// </summary>
    public class RevenueVsDiscountsControl : Panel
    {
        private readonly Chart _chart;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Label _statusLabel;

        public RevenueVsDiscountsControl()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            Margin = new Padding(3);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = "Revenue vs Discounts",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
            };

            _subtitleLabel = new Label
            {
                Text = "What customers paid, and how much came off as discounts",
                Dock = DockStyle.Top,
                Height = 20,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _statusLabel = new Label
            {
                Text = "No revenue in this period.",
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

            // X axis
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.MajorTickMark.Enabled = true;
            area.AxisX.MajorTickMark.LineColor = BrandPalette.Border;
            area.AxisX.LineColor = BrandPalette.Border;
            area.AxisX.Interval = 1;
            area.AxisX.IsMarginVisible = true;
            area.AxisX.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisX.LabelStyle.ForeColor = BrandPalette.NeutralText;

            // Y axis — currency, whole pesos
            area.AxisY.MajorGrid.LineColor = BrandPalette.Border;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.LineColor = Color.Transparent;
            area.AxisY.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisY.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.LabelStyle.Format = "₱#,##0";
            area.AxisY.Minimum = 0;
            area.AxisY.IsStartedFromZero = true;

            _chart.ChartAreas.Add(area);

            var legend = new Legend("L")
            {
                Font = BrandPalette.BrandFontBold(8f),
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

        public void BindData(RevenueVsDiscountsDto data)
        {
            if (data == null || data.Buckets.Count == 0 ||
                data.Buckets.All(b => b.NetRevenue == 0 && b.CampaignDiscount == 0 && b.PwdSeniorDiscount == 0))
            {
                _statusLabel.Text = "No revenue in this period.";
                _statusLabel.Visible = true;
                _chart.Visible = false;
                _subtitleLabel.Text = "What customers paid, and how much came off as discounts";
                return;
            }

            _statusLabel.Visible = false;
            _chart.Visible = true;
            _chart.Series.Clear();

            var totalNet = data.Buckets.Sum(b => b.NetRevenue);
            var totalDiscount = data.Buckets.Sum(b => b.CampaignDiscount + b.PwdSeniorDiscount);
            var discountPct = totalNet + totalDiscount > 0
                ? totalDiscount / (totalNet + totalDiscount) * 100m
                : 0m;

            _subtitleLabel.Text = $"{discountPct:0}% of revenue went to discounts";

            // ── Line 1: Net Revenue (dominant) ──
            var seriesNet = new Series("Net Revenue")
            {
                ChartType = SeriesChartType.Line,
                ChartArea = "MainArea",
                Legend = "L",
                Color = BrandPalette.Brand,
                BorderWidth = 3,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 7,
                MarkerColor = BrandPalette.Brand,
                MarkerBorderColor = BrandPalette.SurfacePrimary,
                MarkerBorderWidth = 2,
                IsXValueIndexed = true,
            };

            // ── Line 2: Campaign Discounts ──
            var seriesCampaign = new Series("Campaign Discounts")
            {
                ChartType = SeriesChartType.Line,
                ChartArea = "MainArea",
                Legend = "L",
                Color = Color.FromArgb(190, 130, 60),
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Diamond,
                MarkerSize = 7,
                MarkerColor = Color.FromArgb(190, 130, 60),
                MarkerBorderColor = BrandPalette.SurfacePrimary,
                MarkerBorderWidth = 2,
                IsXValueIndexed = true,
            };

            // ── Line 3: PWD/Senior Discounts ──
            var seriesPwd = new Series("PWD/Senior")
            {
                ChartType = SeriesChartType.Line,
                ChartArea = "MainArea",
                Legend = "L",
                Color = BrandPalette.Tone60,
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Triangle,
                MarkerSize = 7,
                MarkerColor = BrandPalette.Tone60,
                MarkerBorderColor = BrandPalette.SurfacePrimary,
                MarkerBorderWidth = 2,
                IsXValueIndexed = true,
            };

            foreach (var bucket in data.Buckets)
            {
                seriesNet.Points.AddXY(bucket.BucketLabel, (double)bucket.NetRevenue);
                seriesCampaign.Points.AddXY(bucket.BucketLabel, (double)bucket.CampaignDiscount);
                seriesPwd.Points.AddXY(bucket.BucketLabel, (double)bucket.PwdSeniorDiscount);
            }

            // Attach tooltips after the points are added
            for (int i = 0; i < data.Buckets.Count; i++)
            {
                var b = data.Buckets[i];
                seriesNet.Points[i].ToolTip = $"{b.BucketLabel}\nNet: ₱{b.NetRevenue:N0}";
                seriesCampaign.Points[i].ToolTip = $"{b.BucketLabel}\nCampaign: ₱{b.CampaignDiscount:N0}";
                seriesPwd.Points[i].ToolTip = $"{b.BucketLabel}\nPWD/Senior: ₱{b.PwdSeniorDiscount:N0}";
            }

            _chart.Series.Add(seriesNet);
            _chart.Series.Add(seriesCampaign);
            _chart.Series.Add(seriesPwd);
            _chart.Invalidate();
        }
    }
}