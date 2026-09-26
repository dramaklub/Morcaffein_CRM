using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Modal dialog for Customer Happiness (CSAT) KPI pod.
    /// Clean horizontal bar chart for 1-5 star ratings with pure brand styling.
    /// </summary>
    public class CustomerHappinessDetailModal : Form
    {
        public CustomerHappinessDetailModal(FeedbackRatingDistributionDto data)
        {
            Text            = "Customer Satisfaction (CSAT)";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ClientSize      = new Size(540, 460);
            BackColor       = BrandPalette.SurfacePrimary;
            Font            = BrandPalette.BrandFontRegular(9.5f);
            Padding         = new Padding(24);

            BuildLayout(data);
        }

        private void BuildLayout(FeedbackRatingDistributionDto data)
        {
            // ── Top Header Panel ──
            var headerPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 64,
                BackColor = BrandPalette.SurfacePrimary,
            };

            var titleLabel = new Label
            {
                Text      = "Customer Satisfaction (CSAT)",
                Location  = new Point(0, 0),
                Size      = new Size(492, 28),
                Font      = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var subtitleLabel = new Label
            {
                Text      = data.TotalFeedbacks > 0
                    ? $"Average: {data.AverageRating:0.0} ★    ·    {data.TotalFeedbacks:N0} reviews"
                    : "Average: —    ·    0 reviews",
                Location  = new Point(0, 28),
                Size      = new Size(492, 24),
                Font      = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var divider = new Panel
            {
                Location  = new Point(0, 56),
                Size      = new Size(492, 1),
                BackColor = BrandPalette.Border,
            };

            headerPanel.Controls.Add(divider);
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(titleLabel);

            // ── Bottom Button Row ──
            var btnRow = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 46,
                BackColor = BrandPalette.SurfacePrimary,
            };
            var closeBtn = new Button
            {
                Text      = "Close",
                Width     = 110,
                Height    = 36,
                Dock      = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font      = BrandPalette.BrandFontBold(9.5f),
                Cursor    = Cursors.Hand,
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => Close();
            btnRow.Controls.Add(closeBtn);

            // ── Center Chart Panel ──
            var chartPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(0, 12, 0, 12),
                BackColor = BrandPalette.SurfacePrimary,
            };

            var chart = new Chart
            {
                Dock            = DockStyle.Fill,
                BackColor       = BrandPalette.SurfacePrimary,
                BorderlineColor = Color.Transparent,
                BorderlineWidth = 0,
            };

            var area = new ChartArea("Main")
            {
                BackColor   = BrandPalette.SurfacePrimary,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
            };

            // Category axis (Stars)
            area.AxisX.MajorGrid.Enabled       = false;
            area.AxisX.MajorTickMark.Enabled   = false;
            area.AxisX.LineColor               = Color.Transparent;
            area.AxisX.Interval                = 1;
            area.AxisX.IsMarginVisible         = true;
            area.AxisX.LabelStyle.Font         = BrandPalette.BrandFontBold(9.5f);
            area.AxisX.LabelStyle.ForeColor    = BrandPalette.NeutralText;

            // ── Primary Y axis (left) — Net Revenue, whole pesos ──
            area.AxisY.MajorGrid.LineColor = BrandPalette.Border;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.LineColor = Color.Transparent;
            area.AxisY.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisY.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.LabelStyle.Format = "₱#,##0";
            area.AxisY.Minimum = 0;
            area.AxisY.IsStartedFromZero = true;
            area.AxisY.Title = "Net Revenue (₱)";
            area.AxisY.TitleFont = BrandPalette.BrandFontBold(8f);
            area.AxisY.TitleForeColor = BrandPalette.NeutralTextMuted;

            // ── Secondary Y axis (right) — discounts, own scale ──
            // Without this, discount amounts (typically 5–20% of revenue) hug the
            // ₱0 line and read as "no discounts at all" even when they're real.
            area.AxisY2.MajorGrid.Enabled = false;
            area.AxisY2.MajorTickMark.Enabled = false;
            area.AxisY2.LineColor = Color.Transparent;
            area.AxisY2.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisY2.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY2.LabelStyle.Format = "₱#,##0";
            area.AxisY2.Minimum = 0;
            area.AxisY2.IsStartedFromZero = true;
            area.AxisY2.Title = "Discounts (₱)";
            area.AxisY2.TitleFont = BrandPalette.BrandFontBold(8f);
            area.AxisY2.TitleForeColor = BrandPalette.NeutralTextMuted;

            var maxCount = data.RatingBreakdown.Count > 0 ? data.RatingBreakdown.Max(r => r.Count) : 0;
            area.AxisY.Maximum = Math.Max(5, maxCount + (maxCount > 0 ? (int)Math.Ceiling(maxCount * 0.25) : 2));

            chart.ChartAreas.Add(area);

            var series = new Series("Ratings")
            {
                ChartType           = SeriesChartType.Bar,
                ChartArea           = "Main",
                IsValueShownAsLabel = true,
                IsXValueIndexed     = true,
                Font                = BrandPalette.BrandFontBold(9f),
                LabelForeColor      = BrandPalette.NeutralText,
                Color               = BrandPalette.Brand,
                BorderWidth         = 0,
            };
            series["PointWidth"]     = "0.52";
            series["BarLabelStyle"]  = "Right";

            // Add all 5 ratings (1★ through 5★)
            for (int rating = 1; rating <= 5; rating++)
            {
                var pt = data.RatingBreakdown.FirstOrDefault(r => r.Rating == rating);
                var count = pt?.Count ?? 0;
                var pct = data.TotalFeedbacks > 0 ? (decimal)count / data.TotalFeedbacks * 100m : 0m;

                var idx = series.Points.AddY(count);
                var point = series.Points[idx];
                point.AxisLabel = $"{rating} ★";
                point.Color = BrandPalette.Brand;
                point.Label = $"{count}  ({pct:0}%)";
                point.ToolTip = $"{rating} ★ · {count} reviews ({pct:0}%)";
            }

            chart.Series.Add(series);
            chartPanel.Controls.Add(chart);

            // Compose cleanly without dock collision
            Controls.Add(chartPanel);
            Controls.Add(btnRow);
            Controls.Add(headerPanel);

            CancelButton = closeBtn;
        }
    }
}