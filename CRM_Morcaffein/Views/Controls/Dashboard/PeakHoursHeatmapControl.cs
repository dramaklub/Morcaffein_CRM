using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Peak Hours Transaction Velocity — self-painted heatmap (hour × day of week).
    /// </summary>
    public class PeakHoursHeatmapControl : UserControl
    {
        private readonly DashboardService _controller;
        private Label _titleLabel = null!;
        private Label _subtitleLabel = null!;
        private Label _errorLabel = null!;
        private Panel _gridHost = null!;
        private readonly ToolTip _cellToolTip = new();

        private const int StartHour = 7;
        private const int EndHour = 21; // inclusive → 7am–9pm
        private static readonly string[] DayLabels = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        private const int RowHeaderWidth = 36;
        private const int ColHeaderHeight = 22;
        private const int LegendHeight = 18;

        private PeakHoursDto _data = new();

        public PeakHoursHeatmapControl()
        {
            _controller = new DashboardService();
            BuildLayout();
        }

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(14, 10, 14, 10);

            _titleLabel = new Label
            {
                Text = "Peak Hours Transaction Velocity",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(10.5f),
                ForeColor = BrandPalette.NeutralText,
            };

            _subtitleLabel = new Label
            {
                Text = "When customers visit, by hour and day",
                Dock = DockStyle.Top,
                Height = 18,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _errorLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 16,
                Font = BrandPalette.BrandFontRegular(8f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _gridHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
            };
            _gridHost.Paint += GridHost_Paint;
            _gridHost.MouseMove += GridHost_MouseMove;
            _gridHost.MouseLeave += (s, e) =>
            {
                _cellToolTip.Hide(_gridHost);
                _lastHoverCell = null;
            };
            _gridHost.Resize += (s, e) => _gridHost.Invalidate();

            Controls.Add(_gridHost);
            Controls.Add(_errorLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_titleLabel);
        }

        public void BindData(PeakHoursDto data)
        {
            _data = data ?? new PeakHoursDto();
            _errorLabel.Text = string.Empty;

            var total = _data.Cells.Sum(c => c.TransactionCount);
            _subtitleLabel.Text = total > 0
                ? $"{total:N0} orders shown · darker = busier"
                : "When customers visit, by hour and day";

            _gridHost.Invalidate();
        }

        // ── geometry ──────────────────────────────────────────────────────────

        private int HourCount => EndHour - StartHour + 1;

        private (float cellW, float cellH) GetCellSize()
        {
            var w = Math.Max(1, _gridHost.ClientSize.Width - RowHeaderWidth);
            var h = Math.Max(1, _gridHost.ClientSize.Height - ColHeaderHeight - LegendHeight);
            return ((float)w / HourCount, (float)h / DayLabels.Length);
        }

        // ── paint ─────────────────────────────────────────────────────────────

        private void GridHost_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var (cellW, cellH) = GetCellSize();
            var maxCount = _data.Cells.Count > 0 ? _data.Cells.Max(c => c.TransactionCount) : 0;

            using var headerFont = BrandPalette.BrandFontRegular(7.5f);
            using var cellFont = BrandPalette.BrandFontBold(7.5f);
            using var headerBrush = new SolidBrush(BrandPalette.NeutralTextMuted);

            // Column headers (hours)
            for (var h = 0; h < HourCount; h++)
            {
                var hour = StartHour + h;
                var label = FormatHour(hour);
                var rect = new RectangleF(RowHeaderWidth + h * cellW, 0, cellW, ColHeaderHeight);
                g.DrawString(label, headerFont, headerBrush, rect,
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            // Rows + cells
            for (var d = 0; d < DayLabels.Length; d++)
            {
                var rowRect = new RectangleF(0, ColHeaderHeight + d * cellH, RowHeaderWidth, cellH);
                g.DrawString(DayLabels[d], headerFont, headerBrush, rowRect,
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });

                for (var h = 0; h < HourCount; h++)
                {
                    var hour = StartHour + h;
                    var match = _data.Cells.FirstOrDefault(c => c.DayOfWeek == d && c.Hour == hour);
                    var count = match?.TransactionCount ?? 0;

                    // Inset cells slightly for a cleaner grid
                    var cellRect = new RectangleF(
                        RowHeaderWidth + h * cellW + 1,
                        ColHeaderHeight + d * cellH + 1,
                        Math.Max(1, cellW - 2),
                        Math.Max(1, cellH - 2));

                    using var fill = new SolidBrush(ColorForIntensity(count, maxCount));
                    g.FillRectangle(fill, cellRect);

                    if (count > 0 && cellW > 18 && cellH > 14)
                    {
                        var textColor = count > maxCount * 0.55
                            ? Color.White
                            : BrandPalette.NeutralText;
                        using var textBrush = new SolidBrush(textColor);
                        g.DrawString(count.ToString(), cellFont, textBrush, cellRect,
                            new StringFormat
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center,
                            });
                    }
                }
            }

            // Mini intensity legend at the bottom
            DrawLegend(g, maxCount);
        }

        private void DrawLegend(Graphics g, int maxCount)
        {
            var y = _gridHost.ClientSize.Height - LegendHeight + 2;
            var legendW = Math.Min(160, _gridHost.ClientSize.Width - RowHeaderWidth - 8);
            var x = RowHeaderWidth;

            using var font = BrandPalette.BrandFontRegular(7f);
            using var brush = new SolidBrush(BrandPalette.NeutralTextMuted);

            g.DrawString("Low", font, brush, x, y);

            var barX = x + 24;
            var barW = legendW - 56;
            const int steps = 8;
            var stepW = barW / steps;

            for (int i = 0; i < steps; i++)
            {
                var intensity = (int)Math.Round(maxCount * (i + 1) / (double)steps);
                using var fill = new SolidBrush(ColorForIntensity(Math.Max(1, intensity), Math.Max(1, maxCount)));
                g.FillRectangle(fill, barX + i * stepW, y + 2, stepW - 1, 10);
            }

            g.DrawString("High", font, brush, barX + barW + 4, y);
        }

        private static string FormatHour(int hour)
        {
            if (hour == 0) return "12a";
            if (hour < 12) return $"{hour}a";
            if (hour == 12) return "12p";
            return $"{hour - 12}p";
        }

        /// <summary>
        /// Empty cells stay a warm neutral; filled cells ramp from Tint90 → Brand.
        /// </summary>
        private static Color ColorForIntensity(int count, int max)
        {
            if (count <= 0) return Color.FromArgb(245, 242, 238); // warm empty
            if (max <= 0) return BrandPalette.Tint75;

            var ratio = Math.Clamp((double)count / max, 0.12, 1.0);
            var brand = BrandPalette.Brand;
            var light = Color.FromArgb(250, 246, 240);

            return Color.FromArgb(
                (int)(light.R + (brand.R - light.R) * ratio),
                (int)(light.G + (brand.G - light.G) * ratio),
                (int)(light.B + (brand.B - light.B) * ratio));
        }

        // ── hover ─────────────────────────────────────────────────────────────

        private (int day, int hour)? _lastHoverCell;

        private void GridHost_MouseMove(object? sender, MouseEventArgs e)
        {
            var (cellW, cellH) = GetCellSize();

            if (e.X < RowHeaderWidth || e.Y < ColHeaderHeight ||
                e.Y > ColHeaderHeight + DayLabels.Length * cellH)
            {
                _cellToolTip.Hide(_gridHost);
                _lastHoverCell = null;
                return;
            }

            var h = (int)((e.X - RowHeaderWidth) / cellW);
            var d = (int)((e.Y - ColHeaderHeight) / cellH);

            if (h < 0 || h >= HourCount || d < 0 || d >= DayLabels.Length)
            {
                _cellToolTip.Hide(_gridHost);
                _lastHoverCell = null;
                return;
            }

            if (_lastHoverCell == (d, h)) return;
            _lastHoverCell = (d, h);

            var hour = StartHour + h;
            var match = _data.Cells.FirstOrDefault(c => c.DayOfWeek == d && c.Hour == hour);
            var count = match?.TransactionCount ?? 0;
            var revenue = match?.Revenue ?? 0m;

            var tip = count > 0
                ? $"{DayLabels[d]} {FormatHour(hour)}  ·  {count} orders  ·  ₱{revenue:N0}"
                : $"{DayLabels[d]} {FormatHour(hour)}  ·  no orders";

            _cellToolTip.Show(tip, _gridHost, e.X + 14, e.Y + 14, 2500);
        }
    }
}