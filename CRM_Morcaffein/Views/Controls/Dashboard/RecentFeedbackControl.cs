using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Basic-tier Admin dashboard card: the most recently logged feedback
    /// tickets. Same visual language as VisitFrequencyControl — title,
    /// subtitle, self-painted list rows with star rating and status.
    ///
    /// Clicking anywhere on a row raises RowClicked with the TicketId, so
    /// the host view can navigate to the Feedback tab.
    /// </summary>
    public class RecentFeedbackControl : Panel
    {
        public event EventHandler<int>? RowClicked;

        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Label _statusLabel;
        private readonly Panel _listHost;

        private const int RowHeight = 44;
        private const int RowGap = 4;

        private static readonly Font NameFont = BrandPalette.BrandFontBold(10f);
        private static readonly Font MetaFont = BrandPalette.BrandFontRegular(9f);
        private static readonly Font StarFont = BrandPalette.BrandFontBold(10f);

        public RecentFeedbackControl()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            Margin = new Padding(3);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = "Recent Feedback",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
            };

            _subtitleLabel = new Label
            {
                Text = "Latest customer reviews",
                Dock = DockStyle.Top,
                Height = 20,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _statusLabel = new Label
            {
                Text = "Loading…",
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
            Controls.Add(_subtitleLabel);
            Controls.Add(_titleLabel);
        }

        public void BindData(RecentFeedbackDto data)
        {
            _listHost.Controls.Clear();

            if (data == null || data.Feedbacks.Count == 0)
            {
                _statusLabel.Text = "No feedback yet.";
                _statusLabel.Visible = true;
                _listHost.Visible = false;
                return;
            }

            _statusLabel.Visible = false;
            _listHost.Visible = true;

            var y = 0;
            foreach (var fb in data.Feedbacks)
            {
                var row = BuildRow(fb);
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

        private Panel BuildRow(RecentFeedbackItem fb)
        {
            var row = new Panel
            {
                Height = RowHeight,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };

            row.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var w = row.ClientSize.Width;
                if (w <= 0) return;

                // Left: customer name (top) + category · status (bottom)
                var nameRect = new Rectangle(0, 6, w - 120, 18);
                TextRenderer.DrawText(g, fb.CustomerName, NameFont, nameRect,
                    BrandPalette.NeutralText,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                var metaText = string.IsNullOrWhiteSpace(fb.Category)
                    ? fb.Status
                    : $"{fb.Category}  ·  {fb.Status}";

                var metaRect = new Rectangle(0, 24, w - 120, 16);
                TextRenderer.DrawText(g, metaText, MetaFont, metaRect,
                    BrandPalette.NeutralTextMuted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // Right: star rating (e.g. "★★★★☆")
                var stars = new string('★', Math.Clamp(fb.Rating, 0, 5))
                          + new string('☆', Math.Clamp(5 - fb.Rating, 0, 5));
                var starRect = new Rectangle(w - 110, 6, 110, 34);
                TextRenderer.DrawText(g, stars, StarFont, starRect,
                    BrandPalette.Brand,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            };

            row.Click += (s, e) => RowClicked?.Invoke(this, fb.TicketId);
            return row;
        }
    }
}