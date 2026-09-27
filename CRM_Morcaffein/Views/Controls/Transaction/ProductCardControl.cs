using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    public class ProductCardControl : Panel
    {
        public Product Product { get; }

        public event EventHandler? CardClicked;
        public event EventHandler<ProductSize>? SizeQuickAdded;

        private const int SizeRowHeight = 28;
        private const int CardPadding = 8;
        private const int NameAreaHeight = 62;
        private const int PriceAreaHeight = 18;

        private static readonly string[] SizeLabels = { "S", "M", "L", "XL" };

        // ============ SHARED PAINT RESOURCES ============
        private static readonly Font NameFont = BrandPalette.BrandFontBold(9.5f);
        private static readonly Font PriceFont = BrandPalette.BrandFontRegular(8.5f);
        private static readonly Font SizeButtonFont = BrandPalette.BrandFontBold(8.5f);
        private static readonly Font IconFont = BrandPalette.BrandFontBold(28f);

        private static readonly SolidBrush BrandBrush = new(BrandPalette.Brand);
        private static readonly SolidBrush HoverBrush = new(BrandPalette.Shade10);
        private static readonly SolidBrush PlaceholderBrush = new(BrandPalette.Tint75);

        private static readonly Pen BorderPen = new(BrandPalette.Border);
        private static readonly Pen SeparatorPen = new(BrandPalette.Tint25);

        // =====================================================================

        private readonly List<ProductSize> _sizes;
        private Image? _cachedImage;
        private bool _imageLoadAttempted;

        private Rectangle[] _sizeButtonRects = Array.Empty<Rectangle>();
        private int _hoveredSizeIndex = -1;

        public ProductCardControl(Product product)
        {
            Product = product;
            _sizes = product.ProductSizes.ToList();

            Size = new Size(150, 200);      // taller card — image gets more room
            Margin = new Padding(4);
            BackColor = BrandPalette.SurfacePrimary;
            Cursor = Cursors.Hand;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            // Computed here AND on every resize — the card's actual width is set by the
            // TableLayoutPanel cell it's placed in, not by the constructor's Size.
            RecomputeSizeButtonRects();

            MouseEnter += (s, e) => { BackColor = BrandPalette.Tint90; Invalidate(); };
            MouseLeave += (s, e) =>
            {
                BackColor = BrandPalette.SurfacePrimary;
                if (_hoveredSizeIndex != -1) { _hoveredSizeIndex = -1; Invalidate(); }
                else { Invalidate(); }
            };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecomputeSizeButtonRects();
            Invalidate();
        }

        private void RecomputeSizeButtonRects()
        {
            if (_sizes.Count == 0)
            {
                _sizeButtonRects = Array.Empty<Rectangle>();
                return;
            }

            _sizeButtonRects = new Rectangle[_sizes.Count];
            var buttonWidth = Width / _sizes.Count;
            for (int i = 0; i < _sizes.Count; i++)
            {
                var x = i * buttonWidth;
                var w = (i == _sizes.Count - 1) ? Width - x : buttonWidth;
                _sizeButtonRects[i] = new Rectangle(x, Height - SizeRowHeight, w, SizeRowHeight);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cachedImage?.Dispose();
                _cachedImage = null;
            }
            base.Dispose(disposing);
        }

        // =====================================================================
        // PAINTING
        // =====================================================================

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var borderPen = new Pen(BrandPalette.Border);
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

            // Layout:
            //   [   large image area with rounded top    ]  (fills most of the card)
            //   [   product name                         ]
            //   [   from ₱price                          ]
            //   [   S | M | L | XL   size buttons         ]
            var nameTop = Height - SizeRowHeight - NameAreaHeight - PriceAreaHeight - CardPadding;
            var imageHeight = nameTop - CardPadding;

            var imageRect = new Rectangle(CardPadding, CardPadding, Width - CardPadding * 2, imageHeight);
            DrawProductImage(g, imageRect);

            // Product name — TextRenderer with WordBreak only. Adding
            // EndEllipsis was forcing single-line truncation before the wrap
            // could happen ("Pure Matcha (Hot)" rendered as "Pure Matcha ...").
            // Without it, the text wraps naturally and the rectangle clips at
            // the boundary if there's overflow.
            var nameRect = new Rectangle(CardPadding, nameTop, Width - CardPadding * 2, NameAreaHeight);
            TextRenderer.DrawText(
                g,
                Product.ProductName,
                NameFont,
                nameRect,
                BrandPalette.NeutralText,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

            // Price
            var priceRect = new Rectangle(CardPadding, nameTop + NameAreaHeight, Width - CardPadding * 2, PriceAreaHeight);
            TextRenderer.DrawText(
                g,
                $"from ₱{Product.BasePrice:0}",
                PriceFont,
                priceRect,
                BrandPalette.NeutralTextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            // Size buttons
            DrawSizeRow(g);
        }

        private void DrawSizeRow(Graphics g)
        {
            if (_sizes.Count == 0) return;

            for (int i = 0; i < _sizes.Count; i++)
            {
                var rect = _sizeButtonRects[i];
                g.FillRectangle(i == _hoveredSizeIndex ? HoverBrush : BrandBrush, rect);

                var label = i < SizeLabels.Length ? SizeLabels[i] : (i + 1).ToString();
                TextRenderer.DrawText(g, label, SizeButtonFont, rect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                if (i < _sizes.Count - 1)
                    g.DrawLine(SeparatorPen, rect.Right, rect.Top + 4, rect.Right, rect.Bottom - 4);
            }
        }

        private void DrawProductImage(Graphics g, Rectangle imageRect)
        {
            if (!_imageLoadAttempted)
            {
                _imageLoadAttempted = true;
                _cachedImage = TryLoadImage(Product.LocalImagePath);
            }

            if (_cachedImage is not null)
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;

                // "Cover" fit — fill the entire image area, crop the overflow. This is
                // what you want for a modern-looking card: image fills the space, no
                // letterboxing, no empty bands.
                var scaleX = (float)imageRect.Width / _cachedImage.Width;
                var scaleY = (float)imageRect.Height / _cachedImage.Height;
                var scale = Math.Max(scaleX, scaleY);

                var drawWidth = (int)(_cachedImage.Width * scale);
                var drawHeight = (int)(_cachedImage.Height * scale);
                var drawX = imageRect.X + (imageRect.Width - drawWidth) / 2;
                var drawY = imageRect.Y + (imageRect.Height - drawHeight) / 2;

                // Clip to the image rect so the "cover" doesn't bleed over the card.
                var saved = g.Clip;
                g.SetClip(imageRect, System.Drawing.Drawing2D.CombineMode.Replace);
                g.DrawImage(_cachedImage, drawX, drawY, drawWidth, drawHeight);
                g.Clip = saved;
                return;
            }

            // Placeholder: tint block + large coffee glyph
            g.FillRectangle(PlaceholderBrush, imageRect);
            TextRenderer.DrawText(g, "☕", IconFont, imageRect, BrandPalette.Tint25,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // =====================================================================
        // INTERACTION
        // =====================================================================

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            for (int i = 0; i < _sizeButtonRects.Length; i++)
            {
                if (_sizeButtonRects[i].Contains(e.Location))
                {
                    SizeQuickAdded?.Invoke(this, _sizes[i]);
                    return;
                }
            }

            CardClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int newHover = -1;
            for (int i = 0; i < _sizeButtonRects.Length; i++)
            {
                if (_sizeButtonRects[i].Contains(e.Location)) { newHover = i; break; }
            }

            if (newHover != _hoveredSizeIndex)
            {
                _hoveredSizeIndex = newHover;
                Invalidate();
            }
        }

        // =====================================================================
        // IMAGE LOADING
        // =====================================================================

        private static Image? TryLoadImage(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            try
            {
                var fullPath = Path.IsPathRooted(relativePath)
                    ? relativePath
                    : Path.Combine(AppContext.BaseDirectory, relativePath);

                if (!File.Exists(fullPath))
                    return null;

                using var stream = File.OpenRead(fullPath);
                return Image.FromStream(stream);
            }
            catch
            {
                return null;
            }
        }
    }
}