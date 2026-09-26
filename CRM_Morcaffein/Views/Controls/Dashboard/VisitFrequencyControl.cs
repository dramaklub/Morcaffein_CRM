using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Admin dashboard: Customer Visit Frequency Distribution.
    /// Ranked rows with a scaled bar behind each, same visual language as the
    /// Top Coffees card. Reads as a funnel — one-time visitors at the top,
    /// regulars at the bottom. The shape of the drop-off tells you what kind of
    /// business you are.
    /// </summary>
    public class VisitFrequencyControl : Panel
    {
        private readonly Label _titleLabel;
        private readonly Label _statusLabel;
        private readonly Panel _listHost;

        private const int RowHeight = 44;
        private const int RowGap = 4;
        private const int BarHeight = 36;

        private static readonly Font LabelFont = BrandPalette.BrandFontBold(10f);
        private static readonly Font CountFont = BrandPalette.BrandFontBold(10.5f);

        public VisitFrequencyControl()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            Margin = new Padding(3);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = "Customer Visit Frequency",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
            };

            _statusLabel = new Label
            {
                Text = "No orders recorded yet.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Visible = false,
            };

            _listHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0),
            };

            Controls.Add(_listHost);
            Controls.Add(_statusLabel);
            Controls.Add(_titleLabel);
        }

        public void BindData(VisitFrequencyDto data)
        {
            _listHost.Controls.Clear();

            if (data == null || data.Buckets.Count == 0 || data.TotalCustomers == 0)
            {
                _statusLabel.Text = "No orders recorded yet.";
                _statusLabel.Visible = true;
                _listHost.Visible = false;
                return;
            }

            _statusLabel.Visible = false;
            _listHost.Visible = true;

            var maxCount = data.Buckets.Max(b => b.CustomerCount);
            if (maxCount <= 0) maxCount = 1;

            var y = 0;
            foreach (var bucket in data.Buckets)
            {
                var row = BuildRow(bucket, (double)bucket.CustomerCount / maxCount);
                row.Location = new Point(0, y);
                row.Width = _listHost.ClientSize.Width;
                _listHost.Controls.Add(row);
                y += RowHeight + RowGap;
            }

            _listHost.Resize -= ListHost_Resize;
            _listHost.Resize += ListHost_Resize;
        }

        private void ListHost_Resize(object? sender, EventArgs e)
        {
            foreach (Control c in _listHost.Controls)
                c.Width = _listHost.ClientSize.Width;
        }

        private static Panel BuildRow(VisitFrequencyBucket bucket, double fraction)
        {
            var row = new Panel
            {
                Height = RowHeight,
                BackColor = Color.Transparent,
            };

            row.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var w = row.ClientSize.Width;
                if (w <= 0) return;

                // Background bar scaled to this bucket's count vs the largest.
                var barWidth = (int)Math.Max(4, (w - 8) * fraction);
                var barRect = new Rectangle(4, (RowHeight - BarHeight) / 2, barWidth, BarHeight);

                using (var barBrush = new SolidBrush(Color.FromArgb(30, 87, 36, 0)))
                    g.FillRectangle(barBrush, barRect);

                // Bucket label, left-aligned.
                var labelRect = new Rectangle(16, 0, w - 200, RowHeight);
                TextRenderer.DrawText(g, bucket.Label, LabelFont, labelRect,
                    BrandPalette.NeutralText,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // Count, right-aligned.
                var countRect = new Rectangle(w - 180, 0, 160, RowHeight);
                TextRenderer.DrawText(g, $"{bucket.CustomerCount:N0} members", CountFont, countRect,
                    BrandPalette.Brand,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            };

            return row;
        }
    }
}