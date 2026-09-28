using System.Drawing.Drawing2D;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// A small colored circle reflecting the sync state. Purely visual —
    /// FrmMain owns the polling loop and calls SetState(...) after each fetch.
    ///
    /// Colors:
    ///   Green  — online, nothing pending
    ///   Yellow — pending rows waiting to be pushed
    ///   Red    — last sync attempt failed
    ///   Gray   — unknown / never synced / fetch failed
    /// </summary>
    public class SyncIndicatorControl : Control
    {
        public enum SyncState { Unknown, Online, Pending, Failed }

        private SyncState _state = SyncState.Unknown;
        private readonly ToolTip _tooltip;

        public SyncIndicatorControl()
        {
            Width = 16;
            Height = 16;
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw, true);

            _tooltip = new ToolTip
            {
                InitialDelay = 300,
                ReshowDelay = 200,
                AutoPopDelay = 8000,
            };
        }

        public void SetState(SyncState state, string tooltip)
        {
            _state = state;
            _tooltip.SetToolTip(this, tooltip);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var d = Math.Min(Width, Height) - 4;
            var rect = new Rectangle(
                (Width - d) / 2,
                (Height - d) / 2,
                d, d);

            var fill = _state switch
            {
                SyncState.Online => Color.FromArgb(255, 82, 172, 100),
                SyncState.Pending => Color.FromArgb(255, 220, 178, 66),
                SyncState.Failed => Color.FromArgb(255, 202, 76, 76),
                _ => Color.FromArgb(255, 160, 160, 160),
            };

            using var brush = new SolidBrush(fill);
            g.FillEllipse(brush, rect);

            using var border = new Pen(Color.FromArgb(60, 0, 0, 0), 1);
            g.DrawEllipse(border, rect);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tooltip.Dispose();
            base.Dispose(disposing);
        }
    }
}