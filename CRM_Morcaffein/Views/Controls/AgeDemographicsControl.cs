using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Age Demographics vs Spending Power.
    /// Left  = Doughnut ring of member share by age.
    /// Right = AOV breakdown list with ample per-row spacing.
    ///
    /// The "Unknown" bracket (members with no birthday on file) is deliberately
    /// hidden from BOTH the doughnut and the AOV list — a large bucket of
    /// unknown-birthday members dominates the chart and conveys nothing useful.
    /// The API still returns the bracket; this control just skips it.
    /// </summary>
    public class AgeDemographicsControl : Panel
    {
        private readonly Chart _chart;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Label _statusLabel;
        private readonly Panel _aovPanel;

        private static readonly Color[] BracketColors =
        {
            BrandPalette.Tint50,
            BrandPalette.Tint25,
            BrandPalette.Brand,
            Color.FromArgb(120, 70, 30),
            BrandPalette.Tone60,
        };

        private const string UnknownBracketLabel = "Unknown";

        public AgeDemographicsControl()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            Margin = new Padding(3);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = "Who's Shopping With Us",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
            };

            _subtitleLabel = new Label
            {
                Text = "Members by age · What they spend on average",
                Dock = DockStyle.Top,
                Height = 18,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _statusLabel = new Label
            {
                Text = "No age demographic data yet.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Visible = false,
            };

            // AOV side panel — wider than before so labels and values never clip.
            _aovPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 170,
                BackColor = BrandPalette.SurfacePrimary,
                Padding = new Padding(14, 4, 0, 0),
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

            var area = new ChartArea("Main")
            {
                BackColor = BrandPalette.SurfacePrimary,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
            };
            area.AxisX.Enabled = AxisEnabled.False;
            area.AxisY.Enabled = AxisEnabled.False;
            area.Position = new ElementPosition(0, 0, 100, 78);
            _chart.ChartAreas.Add(area);

            var legend = new Legend("L")
            {
                Font = BrandPalette.BrandFontRegular(8f),
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                BackColor = Color.Transparent,
                LegendStyle = LegendStyle.Row,
                IsDockedInsideChartArea = false,
            };
            legend.Position = new ElementPosition(2, 80, 96, 18);
            _chart.Legends.Add(legend);

            Controls.Add(_chart);
            Controls.Add(_aovPanel);
            Controls.Add(_statusLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_titleLabel);
        }

        public void BindData(AgeDemographicsAovDto data)
        {
            // Filter out the Unknown bracket before doing anything — the API may
            // still send it, but the client renders only the real age brackets.
            var brackets = (data?.Brackets ?? new List<AgeBracketPoint>())
                .Where(b => !b.BracketLabel.Equals(UnknownBracketLabel, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (brackets.Count == 0 ||
                brackets.All(b => b.ActiveMemberCount == 0 && b.AverageOrderValue == 0))
            {
                _statusLabel.Visible = true;
                _chart.Visible = false;
                _aovPanel.Visible = false;
                _subtitleLabel.Text = "Members by age · What they spend on average";
                return;
            }

            _statusLabel.Visible = false;
            _chart.Visible = true;
            _aovPanel.Visible = true;
            _chart.Series.Clear();
            _aovPanel.Controls.Clear();

            var totalMembers = brackets.Sum(b => b.ActiveMemberCount);
            _subtitleLabel.Text = totalMembers > 0
                ? $"{totalMembers:N0} total members · Ring = share, Right = Avg Spend"
                : "Members by age · What they spend on average";

            // ── Doughnut chart ──
            var series = new Series("Members")
            {
                ChartType = SeriesChartType.Doughnut,
                ChartArea = "Main",
                Legend = "L",
            };
            series["DoughnutRadius"] = "54";
            series["PieLabelStyle"] = "Disabled";

            int ci = 0;
            foreach (var b in brackets)
            {
                var idx = series.Points.AddY(b.ActiveMemberCount);
                var pt = series.Points[idx];
                pt.Color = BracketColors[ci % BracketColors.Length];
                pt.LegendText = $"{b.BracketLabel} ({b.PercentageOfTotal:0}%)";
                pt.ToolTip = $"{b.BracketLabel}\n{b.ActiveMemberCount} members ({b.PercentageOfTotal:0.0}%)\nAvg Spend: ₱{b.AverageOrderValue:N0}";
                ci++;
            }
            _chart.Series.Add(series);

            // ── AOV list on the right ──
            // Each row is 44px tall so the two lines (bracket label above, amount
            // below) have real vertical room. The badge is centered against the
            // whole row, not pinned to a fixed Y. Labels auto-size so they measure
            // themselves and can't clip; the previous fixed-size labels + a too-
            // narrow panel were what made "18-24" and "<18" render as overlapping
            // glyphs.
            const int RowHeight = 44;
            const int BadgeSize = 10;

            var header = new Label
            {
                Text = "Avg. Spend",
                Dock = DockStyle.Top,
                Height = 20,
                Font = BrandPalette.BrandFontBold(8f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            var listFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent,
            };

            ci = 0;
            foreach (var b in brackets)
            {
                var color = BracketColors[ci % BracketColors.Length];
                var hasSpend = b.AverageOrderValue > 0;

                var row = new Panel
                {
                    Width = 156,
                    Height = RowHeight,
                    Margin = new Padding(0, 0, 0, 2),
                    BackColor = Color.Transparent,
                };

                // Color badge — vertically centered against the whole row.
                var badge = new Panel
                {
                    Width = BadgeSize,
                    Height = BadgeSize,
                    Location = new Point(0, (RowHeight - BadgeSize) / 2),
                    BackColor = color,
                };

                // Bracket label — auto-sized, sits on the row's upper line.
                var bracketLbl = new Label
                {
                    Text = b.BracketLabel,
                    AutoSize = true,
                    Location = new Point(18, 4),
                    Font = BrandPalette.BrandFontRegular(8.5f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                    BackColor = Color.Transparent,
                };

                // Amount — the headline number, right under the bracket label.
                var amountLbl = new Label
                {
                    Text = hasSpend ? $"₱{b.AverageOrderValue:N0}" : "—",
                    AutoSize = true,
                    Location = new Point(18, 20),
                    Font = BrandPalette.BrandFontBold(10f),
                    ForeColor = hasSpend ? BrandPalette.Brand : BrandPalette.NeutralTextMuted,
                    BackColor = Color.Transparent,
                };

                row.Controls.Add(badge);
                row.Controls.Add(bracketLbl);
                row.Controls.Add(amountLbl);
                listFlow.Controls.Add(row);
                ci++;
            }

            _aovPanel.Controls.Add(listFlow);
            _aovPanel.Controls.Add(header);

            _chart.Invalidate();
        }
    }
}