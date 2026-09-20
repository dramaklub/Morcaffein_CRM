using System.Drawing.Drawing2D;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Clickable KPI summary pod card for the Bento Dashboard grid.
    /// Clean layout: Left accent bar, top-left title, right-aligned large value,
    /// optional secondary context line under the value.
    /// </summary>
    public class KpiSummaryPodControl : Panel
    {
        public event EventHandler? CardClicked;

        private string _title = "Metric";
        private string _value = "—";
        private string _secondary = string.Empty;
        private bool _isHovered;

        private static readonly Font TitleFont = BrandPalette.BrandFontBold(9f);
        private static readonly Font ValueFont = BrandPalette.BrandFontBold(22f);
        private static readonly Font SecondaryFont = BrandPalette.BrandFontRegular(8.5f);

        public KpiSummaryPodControl(string title, string initialValue)
        {
            _title = title;
            _value = initialValue;

            Height = 90;
            BackColor = Color.Transparent; // we paint everything ourselves
            Cursor = Cursors.Hand;

            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);
        }

        // Backwards-compatible overloads (keep existing call sites working)
        public KpiSummaryPodControl(string icon, string title, string initialValue, string ignored)
            : this(title, initialValue) { }

        public KpiSummaryPodControl(string icon, string title, string initialValue)
            : this(title, initialValue) { }

        public void UpdateData(string value)
        {
            _value = value;
            Invalidate();
        }

        public void UpdateData(string value, string secondary)
        {
            _value = value;
            _secondary = secondary ?? string.Empty;
            Invalidate();
        }

        // Keep the old signature so existing calls still compile
        public void UpdateData(string value, string ignored, bool dummy) => UpdateData(value);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            const int radius = 10;

            // Card fill
            using var path = RoundedRect(rect, radius);
            var fillColor = _isHovered ? BrandPalette.Tint85 : BrandPalette.SurfacePrimary;
            using (var fill = new SolidBrush(fillColor))
                g.FillPath(fill, path);

            // Very subtle border (gives definition against SurfaceSecondary)
            using (var pen = new Pen(_isHovered ? BrandPalette.Tone60 : BrandPalette.Tone80, 1f))
                g.DrawPath(pen, path);

            // Left accent bar
            using (var accent = new SolidBrush(BrandPalette.Brand))
                g.FillRectangle(accent, new Rectangle(rect.X + 1, rect.Y + 10, 4, rect.Height - 20));

            const int padL = 18;
            const int padR = 16;

            // Title (top-left)
            var titleRect = new Rectangle(padL, 11, Width - padL - padR, 18);
            TextRenderer.DrawText(g, _title, TitleFont,
                titleRect, BrandPalette.NeutralTextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // Large value (right-aligned, dominant)
            var hasSecondary = !string.IsNullOrWhiteSpace(_secondary);
            var valueTop = hasSecondary ? 30 : 34;
            var valueHeight = hasSecondary ? 32 : 44;

            var valueRect = new Rectangle(padL, valueTop, Width - padL - padR, valueHeight);
            TextRenderer.DrawText(g, _value, ValueFont,
                valueRect, BrandPalette.Brand,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // Optional secondary context line (under the value, still right-aligned)
            if (hasSecondary)
            {
                var secRect = new Rectangle(padL, 62, Width - padL - padR, 16);
                TextRenderer.DrawText(g, _secondary, SecondaryFont,
                    secRect, BrandPalette.NeutralTextMuted,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle b, int r)
        {
            int d = r * 2;
            var sz = new Size(d, d);
            var arc = new Rectangle(b.Location, sz);
            var p = new GraphicsPath();

            p.AddArc(arc, 180, 90);
            arc.X = b.Right - d; p.AddArc(arc, 270, 90);
            arc.Y = b.Bottom - d; p.AddArc(arc, 0, 90);
            arc.X = b.Left; p.AddArc(arc, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}