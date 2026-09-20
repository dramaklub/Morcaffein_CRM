using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Top Products card — a ranked list, not a chart. Each row shows:
    ///   [ rank badge ]  [ product name ]  [ units ]  [ revenue ]
    /// with a subtle bar behind the row scaled to the product's revenue
    /// relative to the top seller. The bar gives an at-a-glance "how dominant
    /// is #1" read without needing to scan the numbers.
    ///
    /// Ranked list (not a chart) because a Manager reading this wants to see
    /// the ordering and the actual numbers, not just relative bar heights.
    /// </summary>
    public class TopProductsControl : Panel
    {
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Label _statusLabel;
        private readonly Panel _listHost;

        private const int RowHeight = 46;
        private const int RowGap = 4;
        private const int BarHeight = 40;

        private static readonly Font RankFont = BrandPalette.BrandFontBold(11f);
        private static readonly Font NameFont = BrandPalette.BrandFontBold(10f);
        private static readonly Font CategoryFont = BrandPalette.BrandFontRegular(8f);
        private static readonly Font UnitsFont = BrandPalette.BrandFontRegular(9f);
        private static readonly Font RevenueFont = BrandPalette.BrandFontBold(10.5f);

        public TopProductsControl()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            Margin = new Padding(3);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = "Top Coffees",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
            };

            _subtitleLabel = new Label
            {
                Text = "Best-selling drinks by revenue",
                Dock = DockStyle.Top,
                Height = 20,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _statusLabel = new Label
            {
                Text = "No sales in this period.",
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
                Padding = new Padding(0, 4, 0, 0),
            };

            Controls.Add(_listHost);
            Controls.Add(_statusLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_titleLabel);
        }

        public void BindData(TopProductsResponseDto data)
        {
            _listHost.Controls.Clear();

            if (data == null || data.Products.Count == 0)
            {
                _statusLabel.Text = "No sales in this period.";
                _statusLabel.Visible = true;
                _listHost.Visible = false;
                _subtitleLabel.Text = "Best-selling drinks by revenue";
                return;
            }

            _statusLabel.Visible = false;
            _listHost.Visible = true;

            var totalRevenue = data.Products.Sum(p => p.Revenue);
            _subtitleLabel.Text = $"{data.Products.Count} products  ·  ₱{totalRevenue:N0} combined";

            var maxRevenue = data.Products.Max(p => p.Revenue);
            if (maxRevenue <= 0) maxRevenue = 1m;

            // Lay rows out absolutely, top to bottom.
            var y = 0;
            for (int i = 0; i < data.Products.Count; i++)
            {
                var p = data.Products[i];
                var row = BuildRow(i + 1, p, (double)(p.Revenue / maxRevenue));
                row.Location = new Point(0, y);
                row.Width = _listHost.ClientSize.Width;
                _listHost.Controls.Add(row);
                y += RowHeight + RowGap;
            }

            // Reflow on resize so rows stretch to fill the card's width.
            _listHost.Resize -= ListHost_Resize;
            _listHost.Resize += ListHost_Resize;
        }

        private void ListHost_Resize(object? sender, EventArgs e)
        {
            foreach (Control c in _listHost.Controls)
                c.Width = _listHost.ClientSize.Width;
        }

        /// <summary>
        /// One row. Bar is drawn behind, then rank / name / category / units / revenue
        /// are painted on top via TextRenderer. Everything self-painted in OnPaint
        /// of a small inner Panel so the row scales cleanly.
        /// </summary>
        private static Panel BuildRow(int rank, TopProductItem item, double fraction)
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

                // ---- Background bar (behind everything) ----
                // Scaled to this row's revenue relative to the top seller.
                var barWidth = (int)Math.Max(4, (w - 8) * fraction);
                var barRect = new Rectangle(4, (RowHeight - BarHeight) / 2, barWidth, BarHeight);

                using (var barBrush = new SolidBrush(Color.FromArgb(30, 87, 36, 0)))   // Brand @ 12%
                    g.FillRectangle(barBrush, barRect);

                // ---- Rank badge (left) ----
                var rankRect = new Rectangle(12, 0, 28, RowHeight);
                TextRenderer.DrawText(g, $"#{rank}", RankFont, rankRect,
                    BrandPalette.Brand,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // ---- Product name + category (middle-left) ----
                var nameLeft = 48;
                var nameRect = new Rectangle(nameLeft, 6, w - nameLeft - 200, 18);
                TextRenderer.DrawText(g, item.ProductName, NameFont, nameRect,
                    BrandPalette.NeutralText,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                var catRect = new Rectangle(nameLeft, 24, w - nameLeft - 200, 14);
                TextRenderer.DrawText(g, item.ProductCategory, CategoryFont, catRect,
                    BrandPalette.NeutralTextMuted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // ---- Units (right, above revenue) ----
                var unitsRect = new Rectangle(w - 200, 8, 90, 14);
                TextRenderer.DrawText(g, $"{item.UnitsSold:N0} sold", UnitsFont, unitsRect,
                    BrandPalette.NeutralTextMuted,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

                // ---- Revenue (right, prominent) ----
                var revRect = new Rectangle(w - 200, 22, 90, 18);
                TextRenderer.DrawText(g, $"₱{item.Revenue:N0}", RevenueFont, revRect,
                    BrandPalette.Brand,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            };

            return row;
        }
    }
}