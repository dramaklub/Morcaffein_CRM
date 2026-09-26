using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Admin dashboard: New vs Returning Customers per month.
    /// Stacked column — Returning at the bottom (the retention signal),
    /// New on top (acquisition). If the bottom segment stays thin while the
    /// top grows, the shop is acquiring members without retaining them.
    ///
    /// Counts are DISTINCT CUSTOMERS, not orders. "How many people came back"
    /// is the strategic signal; order counts are operational and belong on
    /// the Manager dashboard.
    /// </summary>
    public class NewVsReturningControl : Panel
    {
        private readonly Chart _chart;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Label _statusLabel;

        public NewVsReturningControl()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            Margin = new Padding(3);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = "New vs Returning Customers",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
            };

            _subtitleLabel = new Label
            {
                Text = "How many members came back, and how many were new",
                Dock = DockStyle.Top,
                Height = 20,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _statusLabel = new Label
            {
                Text = "No customer activity in this period.",
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

            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.MajorTickMark.Enabled = true;
            area.AxisX.MajorTickMark.LineColor = BrandPalette.Border;
            area.AxisX.LineColor = BrandPalette.Border;
            area.AxisX.Interval = 1;
            area.AxisX.IsMarginVisible = true;
            area.AxisX.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisX.LabelStyle.ForeColor = BrandPalette.NeutralText;

            area.AxisY.MajorGrid.LineColor = BrandPalette.Border;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.LineColor = Color.Transparent;
            area.AxisY.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisY.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.LabelStyle.Format = "0";
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

        public void BindData(NewVsReturningDto data)
        {
            if (data == null || data.Buckets.Count == 0 ||
                data.Buckets.All(b => b.NewCustomerCount == 0 && b.ReturningCustomerCount == 0))
            {
                _statusLabel.Text = "No customer activity in this period.";
                _statusLabel.Visible = true;
                _chart.Visible = false;
                _subtitleLabel.Text = "How many members came back, and how many were new";
                return;
            }

            _statusLabel.Visible = false;
            _chart.Visible = true;
            _chart.Series.Clear();

            var totalNew = data.Buckets.Sum(b => b.NewCustomerCount);
            var totalReturning = data.Buckets.Sum(b => b.ReturningCustomerCount);
            var totalCustomers = totalNew + totalReturning;
            var retentionPct = totalCustomers > 0
                ? (decimal)totalReturning / totalCustomers * 100m
                : 0m;

            _subtitleLabel.Text =
                $"{totalCustomers:N0} customer visits  ·  {retentionPct:0}% from returning members";

            // ── Returning (bottom of the stack) — the good signal ──
            var seriesReturning = new Series("Returning")
            {
                ChartType = SeriesChartType.StackedColumn,
                ChartArea = "MainArea",
                Legend = "L",
                Color = BrandPalette.Brand,
                BorderWidth = 0,
                IsValueShownAsLabel = false,
                IsXValueIndexed = true,   // ← treat each point as a discrete category
            };
            seriesReturning["PointWidth"] = "0.5";

            // ── New (top of the stack) — acquisition ──
            var seriesNew = new Series("New")
            {
                ChartType = SeriesChartType.StackedColumn,
                ChartArea = "MainArea",
                Legend = "L",
                Color = BrandPalette.Tint50,
                BorderWidth = 0,
                IsValueShownAsLabel = false,
                IsXValueIndexed = true,   // ← same
            };
            seriesNew["PointWidth"] = "0.5";

            for (int i = 0; i < data.Buckets.Count; i++)
            {
                var bucket = data.Buckets[i];

                var rIdx = seriesReturning.Points.AddY(bucket.ReturningCustomerCount);
                seriesReturning.Points[rIdx].AxisLabel = bucket.MonthLabel;

                var nIdx = seriesNew.Points.AddY(bucket.NewCustomerCount);
                seriesNew.Points[nIdx].AxisLabel = bucket.MonthLabel;
            }

            // Tooltips
            for (int i = 0; i < data.Buckets.Count; i++)
            {
                var b = data.Buckets[i];
                var total = b.NewCustomerCount + b.ReturningCustomerCount;
                seriesReturning.Points[i].ToolTip = $"{b.MonthLabel}\nReturning: {b.ReturningCustomerCount}";
                seriesNew.Points[i].ToolTip = $"{b.MonthLabel}\nNew: {b.NewCustomerCount}\nTotal: {total}";
            }

            _chart.Series.Add(seriesReturning);
            _chart.Series.Add(seriesNew);
            _chart.Invalidate();
        }
    }
}