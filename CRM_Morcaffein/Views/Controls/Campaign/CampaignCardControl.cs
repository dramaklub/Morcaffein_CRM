using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// One campaign tile on the Manager's Campaign tab. Paints itself entirely in
    /// OnPaint with no child controls (same convention as ProductCardControl — cheaper
    /// to build, no layout flicker when many cards are created at once).
    ///
    /// Layout:
    ///   [ Campaign name (bold, 2 lines max) ]
    ///   [ Category label (muted small)      ]
    ///   [ Discount summary (brand color)    ]
    ///   [ Date range (muted small)          ]
    ///   [ Status pill (bottom)              ]
    ///
    /// Click anywhere on the card to open the detail modal — the whole tile is the
    /// hit target, there are no interactive sub-regions.
    /// </summary>
    public class CampaignCardControl : Panel
    {
        public CampaignResponseDto Campaign { get; }

        public event EventHandler? CardClicked;

        // ---- Card metrics ----
        private const int CardPadding = 12;
        private const int NameHeight = 40;     // room for up to two lines
        private const int CategoryHeight = 20;
        private const int DiscountHeight = 28;
        private const int DateRangeHeight = 20;
        private const int StatusHeight = 24;

        // ---- Shared paint resources (static so all cards share one instance) ----
        private static readonly Font NameFont = BrandPalette.BrandFontBold(11f);
        private static readonly Font CategoryFont = BrandPalette.BrandFontRegular(8.5f);
        private static readonly Font DiscountFont = BrandPalette.BrandFontBold(13f);
        private static readonly Font DateFont = BrandPalette.BrandFontRegular(8.5f);
        private static readonly Font StatusFont = BrandPalette.BrandFontBold(9f);

        private static readonly SolidBrush NameBrush = new(BrandPalette.NeutralText);
        private static readonly SolidBrush CategoryBrush = new(BrandPalette.NeutralTextMuted);
        private static readonly SolidBrush DiscountBrush = new(BrandPalette.Brand);
        private static readonly SolidBrush DateBrush = new(BrandPalette.NeutralTextMuted);

        private static readonly StringFormat WrapEllipsisFormat = new()
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.LineLimit,
        };

        public CampaignCardControl(CampaignResponseDto campaign)
        {
            Campaign = campaign;

            Size = new Size(260, 200);
            Margin = new Padding(8);
            BackColor = BrandPalette.SurfacePrimary;
            Cursor = Cursors.Hand;

            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);

            Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);

            MouseEnter += (s, e) => { BackColor = BrandPalette.Tint90; Invalidate(); };
            MouseLeave += (s, e) => { BackColor = BrandPalette.SurfacePrimary; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Border
            using var borderPen = new Pen(BrandPalette.Border);
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

            int y = CardPadding;

            // ---- Campaign name (up to two lines) ----
            var nameRect = new RectangleF(
                CardPadding, y,
                Width - CardPadding * 2,
                NameHeight);
            g.DrawString(Campaign.PromoName, NameFont, NameBrush, nameRect, WrapEllipsisFormat);
            y += NameHeight;

            // ---- Category ----
            var categoryRect = new RectangleF(
                CardPadding, y,
                Width - CardPadding * 2,
                CategoryHeight);
            g.DrawString(Campaign.PromoCategory, CategoryFont, CategoryBrush, categoryRect, WrapEllipsisFormat);
            y += CategoryHeight + 4;

            // ---- Discount summary ----
            var discountText = FormatDiscount(Campaign.DiscountType, Campaign.DiscountValue);
            var discountRect = new RectangleF(
                CardPadding, y,
                Width - CardPadding * 2,
                DiscountHeight);
            g.DrawString(discountText, DiscountFont, DiscountBrush, discountRect, WrapEllipsisFormat);
            y += DiscountHeight + 2;

            // ---- Date range ----
            var dateText = $"{Campaign.StartDate:MMM d, yyyy} – {Campaign.EndDate:MMM d, yyyy}";
            var dateRect = new RectangleF(
                CardPadding, y,
                Width - CardPadding * 2,
                DateRangeHeight);
            g.DrawString(dateText, DateFont, DateBrush, dateRect, WrapEllipsisFormat);

            // ---- Status pill, bottom of the card ----
            DrawStatusPill(g);
        }

        /// <summary>
        /// Renders the campaign's status as a filled pill at the bottom-left of the card.
        /// Active = filled brand tint, Inactive/Pending/Rejected = muted neutral.
        /// </summary>
        private void DrawStatusPill(Graphics g)
        {
            var label = string.IsNullOrWhiteSpace(Campaign.Status) ? "—" : Campaign.Status;

            var isActive = label.Equals("Active", StringComparison.OrdinalIgnoreCase);

            var fillColor = isActive ? BrandPalette.Tint75 : BrandPalette.Tone35;
            var textColor = isActive ? BrandPalette.Shade25 : BrandPalette.Tint95;

            // Measure the text so the pill hugs its contents.
            var textSize = g.MeasureString(label, StatusFont);

            var pillPaddingX = 10;
            var pillWidth = (int)Math.Ceiling(textSize.Width) + pillPaddingX * 2;
            var pillRect = new Rectangle(
                CardPadding,
                Height - CardPadding - StatusHeight,
                pillWidth,
                StatusHeight);

            using var fillBrush = new SolidBrush(fillColor);
            using var textBrush = new SolidBrush(textColor);

            // Rounded-ish pill via a small corner radius approximation.
            var radius = StatusHeight / 2;
            using var path = RoundedRect(pillRect, radius);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillPath(fillBrush, path);

            var textRect = new RectangleF(
                pillRect.X,
                pillRect.Y,
                pillRect.Width,
                pillRect.Height);
            var centered = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            g.DrawString(label, StatusFont, textBrush, textRect, centered);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            var d = radius * 2;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static string FormatDiscount(string discountType, decimal discountValue)
        {
            if (discountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
                return $"{discountValue:0.##}% off";

            return $"₱{discountValue:0.##} off";
        }
    }
}