using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>Loyalty Tier Progression — Stacked column chart.</summary>
    public class LoyaltyTierProgressionControl : UserControl
    {
        private readonly DashboardController _controller;
        private Chart _chart = null!;
        private Label _titleLabel = null!;
        private Label _subtitleLabel = null!;
        private Label _emptyLabel = null!;

        // Brand-aligned tier colors
        private static readonly Color RegularColor = BrandPalette.Tone60;
        private static readonly Color SilverColor = Color.FromArgb(168, 168, 175); // warm silver
        private static readonly Color GoldColor = Color.FromArgb(196, 146, 42);  // richer amber-gold

        public LoyaltyTierProgressionControl()
        {
            _controller = new DashboardController();
            BuildLayout();
        }

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(14, 10, 14, 12);

            // Title
            _titleLabel = new Label
            {
                Text = "Member Growth by Tier",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(10.5f),
                ForeColor = BrandPalette.NeutralText,
            };

            // Subtle context line under title
            _subtitleLabel = new Label
            {
                Text = "How many members in each tier, month by month",
                Dock = DockStyle.Top,
                Height = 18,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            // Empty / sparse state (shown when almost no movement)
            _emptyLabel = new Label
            {
                Text = "Not enough tier movement yet.\nKeep logging sales to see growth.",
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
            };

            var area = new ChartArea("main")
            {
                BackColor = BrandPalette.SurfacePrimary,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
            };

            // X axis
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisX.LineColor = BrandPalette.Border;
            area.AxisX.Interval = 1;
            area.AxisX.IsMarginVisible = true;
            area.AxisX.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisX.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;

            // Y axis
            area.AxisY.MajorGrid.LineColor = BrandPalette.Border;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.LineColor = Color.Transparent;
            area.AxisY.LabelStyle.Font = BrandPalette.BrandFontRegular(8f);
            area.AxisY.LabelStyle.ForeColor = BrandPalette.NeutralTextMuted;
            area.AxisY.IsStartedFromZero = true;

            // Give the plot a little breathing room
            area.Position = new ElementPosition(2, 4, 96, 78);
            area.InnerPlotPosition = new ElementPosition(8, 6, 88, 88);

            _chart.ChartAreas.Add(area);

            // Legend — bottom, centered, no box
            var legend = new Legend("main")
            {
                Font = BrandPalette.BrandFontRegular(8.5f),
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                BackColor = Color.Transparent,
                LegendStyle = LegendStyle.Row,
                IsDockedInsideChartArea = false,
                TableStyle = LegendTableStyle.Auto,
            };
            legend.Position = new ElementPosition(10, 88, 80, 10);
            _chart.Legends.Add(legend);

            AddSeries("Regular", RegularColor);
            AddSeries("Silver", SilverColor);
            AddSeries("Gold", GoldColor);

            Controls.Add(_chart);
            Controls.Add(_emptyLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_titleLabel);
        }

        private void AddSeries(string name, Color color)
        {
            var series = new Series(name)
            {
                // Column (not StackedColumn) — WinForms Chart automatically
                // clusters same-type Column series side by side per category, so
                // this renders Regular/Silver/Gold as 3 separate bars per month
                // instead of piling them into one stacked bar.
                ChartType = SeriesChartType.Column,
                ChartArea = "main",
                Legend = "main",
                Color = color,
                BorderWidth = 0,
                BorderColor = color,
            };
            // Slightly narrower columns look more modern
            series["PointWidth"] = "0.48";
            _chart.Series.Add(series);
        }

        public void BindData(LoyaltyTierProgressionDto data)
        {
            if (data == null) return;

            foreach (var series in _chart.Series)
                series.Points.Clear();

            if (data.Months == null || data.Months.Count == 0)
            {
                ShowEmpty(true);
                return;
            }

            int totalMembers = 0;
            int nonRegular = 0;

            foreach (var month in data.Months)
            {
                _chart.Series["Regular"].Points.AddXY(FormatMonth(month.MonthLabel), month.RegularCount);
                _chart.Series["Silver"].Points.AddXY(FormatMonth(month.MonthLabel), month.SilverCount);
                _chart.Series["Gold"].Points.AddXY(FormatMonth(month.MonthLabel), month.GoldCount);

                totalMembers = month.RegularCount + month.SilverCount + month.GoldCount;
                nonRegular = month.SilverCount + month.GoldCount;
            }

            // If almost everyone is still Regular, show a soft empty-ish message
            // but still keep the chart visible so the structure is clear
            bool sparse = totalMembers > 0 && nonRegular == 0;

            ShowEmpty(false);

            // Force a clean redraw
            _chart.Invalidate();
        }

        private void ShowEmpty(bool empty)
        {
            _emptyLabel.Visible = empty;
            _chart.Visible = !empty;
        }

        private static string FormatMonth(string monthLabel)
        {
            // "2026-06" → "Jun"
            if (DateTime.TryParse(monthLabel + "-01", out var dt))
                return dt.ToString("MMM");
            return monthLabel;
        }
    }
}