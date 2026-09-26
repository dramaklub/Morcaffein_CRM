using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Customer Ratings — horizontal bar chart (1★–5★), star-color-coded bars,
    /// brand-themed. Mirrors the modal's visual (CustomerHappinessDetailModal) as
    /// a dashboard card.
    ///
    /// Horizontal bars via SeriesChartType.Bar. In this library, AxisX is ALWAYS
    /// the category axis regardless of Bar vs Column — so the "1★".."5★" labels
    /// go on AxisX, and the numeric count properties (Minimum, IsStartedFromZero,
    /// grid lines) go on AxisY. The earlier broken version had numeric-axis
    /// properties on AxisX, which scrambled all 5 bars into one merged block.
    /// This version puts every property on its correct axis.
    /// </summary>
    public class FeedbackRatingDistributionControl : Panel
    {
        private readonly Chart _chart;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Label _statusLabel;

        private static readonly Color[] StarColors =
        {
            Color.FromArgb(220,  60,  60),  // 1★
            Color.FromArgb(230, 140,  40),  // 2★
            Color.FromArgb(210, 180,  40),  // 3★
            Color.FromArgb(100, 180,  80),  // 4★
            Color.FromArgb( 50, 160,  80),  // 5★
        };

        public FeedbackRatingDistributionControl()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            Margin = new Padding(3);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = "Customer Ratings",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
            };

            _subtitleLabel = new Label
            {
                Text = "How customers rate their visits",
                Dock = DockStyle.Top,
                Height = 20,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _statusLabel = new Label
            {
                Text = "No reviews yet.",
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
                Position = new ElementPosition(2, 2, 96, 94),
            };

            // AxisX = CATEGORY axis (1★..5★ labels). No numeric properties here.
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisX.LineColor = Color.Transparent;
            area.AxisX.Interval = 1;
            area.AxisX.IsMarginVisible = true;
            area.AxisX.LabelStyle.Font = BrandPalette.BrandFontBold(9.5f);
            area.AxisX.LabelStyle.ForeColor = BrandPalette.NeutralText;

            // AxisY = VALUE axis (count). Numeric properties belong here.
            area.AxisY.MajorGrid.LineColor = BrandPalette.Border;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.LineColor = Color.Transparent;
            area.AxisY.LabelStyle.Font = BrandPalette.BrandFontRegular(8.5f);
            area.AxisY.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.Minimum = 0;
            area.AxisY.IsStartedFromZero = true;

            _chart.ChartAreas.Add(area);

            Controls.Add(_chart);
            Controls.Add(_statusLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_titleLabel);
        }

        public void BindData(FeedbackRatingDistributionDto data)
        {
            if (data == null || data.TotalFeedbacks == 0)
            {
                _statusLabel.Text = "No reviews yet.";
                _statusLabel.Visible = true;
                _chart.Visible = false;
                _subtitleLabel.Text = "How customers rate their visits";
                return;
            }

            _statusLabel.Visible = false;
            _chart.Visible = true;
            _chart.Series.Clear();

            _subtitleLabel.Text = $"{data.AverageRating:0.0} ★ average  ·  {data.TotalFeedbacks:N0} reviews";

            var area = _chart.ChartAreas["MainArea"];
            var maxCount = data.RatingBreakdown.Count > 0
                ? data.RatingBreakdown.Max(r => r.Count)
                : 0;

            // For a Bar chart, values grow along the X direction (which is the
            // VALUE direction for horizontal bars). So the "how far the longest
            // bar goes" cap goes on AxisX.Maximum in a Bar chart — this is the
            // one place where Bar and Column diverge on the axis role, and it's
            // why the earlier version got scrambled by putting the cap on the
            // wrong axis. In this library, for SeriesChartType.Bar, the numeric
            // direction is X even though the label axis is also X. To avoid the
            // ambiguity entirely, we let the chart auto-scale and only set
            // Minimum on AxisX (which is the value origin for a Bar chart).
            area.AxisX.Minimum = 0;
            area.AxisX.IsStartedFromZero = true;

            var series = new Series("Ratings")
            {
                ChartType = SeriesChartType.Bar,
                ChartArea = "MainArea",
                IsValueShownAsLabel = true,
                IsXValueIndexed = true,
                Font = BrandPalette.BrandFontBold(9f),
                LabelForeColor = BrandPalette.NeutralText,
                Color = BrandPalette.Brand,
                BorderWidth = 0,
            };
            series["PointWidth"] = "0.52";

            for (int rating = 1; rating <= 5; rating++)
            {
                var pt = data.RatingBreakdown.FirstOrDefault(r => r.Rating == rating);
                var count = pt?.Count ?? 0;
                var pct = data.TotalFeedbacks > 0
                    ? (decimal)count / data.TotalFeedbacks * 100m
                    : 0m;

                var idx = series.Points.AddXY($"{rating} ★", count);
                var point = series.Points[idx];
                point.Color = StarColors[Math.Clamp(rating - 1, 0, StarColors.Length - 1)];
                point.Label = $"{count}  ({pct:0}%)";
                point.ToolTip = $"{rating} ★ · {count} reviews ({pct:0}%)";
            }

            _chart.Series.Add(series);
            _chart.Invalidate();
        }
    }
}