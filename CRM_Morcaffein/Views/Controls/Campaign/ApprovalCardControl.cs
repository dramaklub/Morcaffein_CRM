using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// One horizontal card in the Admin's Approvals queue. Paints itself entirely in
    /// OnPaint, no child controls, same convention as ProductCardControl and
    /// CampaignCardControl.
    ///
    /// Layout (two lines, full row width, ~72px tall):
    ///   Line 1: [ Promo name (bold) ]  [ Category (muted) ]  [ Discount (brand, bold) ]
    ///   Line 2: [ Date range · by Creator (muted small) ]              [ Status pill ]
    ///
    /// The status pill is drawn flush right on line 2, using the same fill/text scheme
    /// as CampaignCardControl so the two views read consistently.
    ///
    /// Click anywhere on the card to open its detail in the drawer.
    /// </summary>
    public class ApprovalCardControl : Panel
    {
        public CampaignResponseDto Campaign { get; }

        public event EventHandler? CardClicked;

        // ---- Card metrics ----
        private const int CardPadding = 16;
        private const int Line1Top = 14;
        private const int Line1Height = 24;
        private const int Line2Top = 40;
        private const int Line2Height = 20;
        private const int PillHeight = 22;
        private const int PillPaddingX = 12;

        // ---- Shared paint resources (static so all cards share one instance) ----
        private static readonly Font NameFont = BrandPalette.BrandFontBold(11f);
        private static readonly Font CategoryFont = BrandPalette.BrandFontRegular(9f);
        private static readonly Font DiscountFont = BrandPalette.BrandFontBold(11f);
        private static readonly Font MetaFont = BrandPalette.BrandFontRegular(8.5f);
        private static readonly Font StatusFont = BrandPalette.BrandFontBold(9f);

        private static readonly SolidBrush NameBrush = new(BrandPalette.NeutralText);
        private static readonly SolidBrush CategoryBrush = new(BrandPalette.NeutralTextMuted);
        private static readonly SolidBrush DiscountBrush = new(BrandPalette.Brand);
        private static readonly SolidBrush MetaBrush = new(BrandPalette.NeutralTextMuted);

        private static readonly StringFormat WrappingFormat = new()
        {
            // No NoWrap — the name area is tall enough for two lines, and this
            // is what stops "Vanilla Matcha Latte" from clipping to "Vanilla Matc…".
            Trimming = StringTrimming.EllipsisCharacter,
        };

        public ApprovalCardControl(CampaignResponseDto campaign)
        {
            Campaign = campaign;

            Height = 72;
            Margin = new Padding(0, 0, 0, 8);
            BackColor = BrandPalette.SurfacePrimary;
            Cursor = Cursors.Hand;

            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);

            Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);

            MouseEnter += (s, e) => { BackColor = BrandPalette.Tint95; Invalidate(); };
            MouseLeave += (s, e) => { BackColor = BrandPalette.SurfacePrimary; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Border
            using var borderPen = new Pen(BrandPalette.Border);
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

            var usableWidth = Width - CardPadding * 2;

            // ---- Line 1 ----
            // Left to right: name, category, discount. Positioned by measuring each so
            // they can't collide.
            var nameSize = g.MeasureString(Campaign.PromoName, NameFont);
            var nameWidth = (int)Math.Ceiling(nameSize.Width);
            var nameRect = new RectangleF(CardPadding, Line1Top, nameWidth, Line1Height);
            g.DrawString(Campaign.PromoName, NameFont, NameBrush, nameRect, WrappingFormat);

            var categoryX = CardPadding + nameWidth + 16;
            var categorySize = g.MeasureString(Campaign.PromoCategory, CategoryFont);
            var categoryWidth = (int)Math.Ceiling(categorySize.Width);
            var categoryRect = new RectangleF(categoryX, Line1Top + 4, categoryWidth, Line1Height);
            g.DrawString(Campaign.PromoCategory, CategoryFont, CategoryBrush, categoryRect, WrappingFormat);

            var discountX = categoryX + categoryWidth + 16;
            var discountText = FormatDiscount(Campaign.DiscountType, Campaign.DiscountValue);
            var discountRect = new RectangleF(
                discountX, Line1Top,
                Math.Max(0, Width - CardPadding - discountX),
                Line1Height);
            g.DrawString(discountText, DiscountFont, DiscountBrush, discountRect, WrappingFormat);

            // ---- Line 2 ----
            // Date range + creator on the left, status pill flush right.
            var metaText = $"{Campaign.StartDate:MMM d, yyyy} – {Campaign.EndDate:MMM d, yyyy}";
            if (!string.IsNullOrWhiteSpace(Campaign.CreatedByName) && Campaign.CreatedByName != "—")
                metaText += $"  ·  by {Campaign.CreatedByName}";

            var metaRect = new RectangleF(
                CardPadding, Line2Top,
                usableWidth - 160,   // reserve room for the pill on the right
                Line2Height);
            g.DrawString(metaText, MetaFont, MetaBrush, metaRect, WrappingFormat);

            DrawStatusPill(g);
        }

        /// <summary>
        /// Renders the status pill flush right on line 2, matching the color scheme used
        /// by CampaignCardControl: Active = light brand-tinted fill with dark text;
        /// everything else = dark warm-grey fill with the lightest brand tint as text.
        /// </summary>
        private void DrawStatusPill(Graphics g)
        {
            var label = string.IsNullOrWhiteSpace(Campaign.Status) ? "—" : Campaign.Status;
            var isActive = label.Equals("Active", StringComparison.OrdinalIgnoreCase);

            var fillColor = isActive ? BrandPalette.Tint75 : BrandPalette.Tone35;
            var textColor = isActive ? BrandPalette.Shade25 : BrandPalette.Tint95;

            var textSize = g.MeasureString(label, StatusFont);
            var pillWidth = (int)Math.Ceiling(textSize.Width) + PillPaddingX * 2;
            var pillX = Width - CardPadding - pillWidth;
            var pillY = Line2Top - 2;   // slight upward nudge so the pill centers on line 2

            var pillRect = new Rectangle(pillX, pillY, pillWidth, PillHeight);

            using var fillBrush = new SolidBrush(fillColor);
            using var textBrush = new SolidBrush(textColor);

            // Rounded pill via the same helper pattern as CampaignCardControl.
            using var path = RoundedRect(pillRect, PillHeight / 2);

            var savedMode = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillPath(fillBrush, path);
            g.SmoothingMode = savedMode;

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

        private static string FormatDiscount(string type, decimal value) =>
            type.Equals("Percentage", StringComparison.OrdinalIgnoreCase)
                ? $"{value:0.##}% off"
                : $"₱{value:0.##} off";
    }
}