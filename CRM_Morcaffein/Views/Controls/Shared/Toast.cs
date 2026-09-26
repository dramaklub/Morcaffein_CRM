using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Lightweight, non-blocking confirmation message. A small rounded card that slides
    /// in at the bottom-center of its host, holds for a few seconds, then fades out and
    /// disposes itself. Multiple toasts stack vertically (newest at the bottom).
    ///
    /// Usage:  Toast.Show(this, "Employee created.", ToastKind.Success);
    /// The host can be any Control — typically a UserControl or Form. The toast parents
    /// itself to the host, positions itself, brings itself to front, and cleans up.
    ///
    /// Why a Panel and not a real Form: no title bar, no taskbar entry, no window
    /// activation, no flicker on show. It behaves like a floating child control rather
    /// than a window, which is what we want for transient confirmations.
    /// </summary>
    public enum ToastKind
    {
        Success,
        Info,
        Error,
    }

    public static class Toast
    {
        // Vertical gap between stacked toasts and from the bottom edge of the host.
        private const int BottomMargin = 24;
        private const int ToastGap = 8;
        private const int ToastHeight = 40;
        private const int ToastMinWidth = 260;
        private const int ToastMaxWidth = 460;
        private const int HorizontalPadding = 20;

        // How long the toast stays fully visible before the fade begins.
        private const int HoldMilliseconds = 2400;

        public static void Show(Control host, string message, ToastKind kind = ToastKind.Success)
        {
            if (host is null || string.IsNullOrWhiteSpace(message))
                return;

            // Reparent to the top-level Form if we can find one. The passed-in host is often
            // a UserControl whose card/grid/pagination children were added to the card, not
            // the UserControl — so a toast added to the UserControl would sit as a SIBLING of
            // the card and could render UNDER the pagination bar, since BringToFront() only
            // reorders siblings within one parent. Parenting to the Form makes the toast a
            // child of the form itself, above every other control in the window. If there's
            // no Form (e.g. previewing a UserControl in isolation), fall back to the passed
            // host.
            var parentForm = host.FindForm();
            var effectiveHost = (Control?)parentForm ?? host;

            // Fonts are shared through BrandPalette; measure the message to size the card.
            var font = BrandPalette.BrandFontRegular(9.5f);
            var textWidth = TextRenderer.MeasureText(message, font).Width;

            var width = Math.Clamp(textWidth + HorizontalPadding * 2, ToastMinWidth, ToastMaxWidth);

            var toast = new Panel
            {
                Width = width,
                Height = ToastHeight,
                BackColor = FillColorFor(kind),
                // No border — the fill color carries the state.
            };

            var label = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = font,
                ForeColor = TextColorFor(kind),
                BackColor = Color.Transparent,
                Padding = new Padding(HorizontalPadding, 0, HorizontalPadding, 0),
                AutoEllipsis = true,
            };
            toast.Controls.Add(label);

            effectiveHost.Controls.Add(toast);
            toast.BringToFront();

            // Position: bottom-center of the host, stacked above any existing toasts.
            void Reposition()
            {
                var hostWidth = effectiveHost.ClientSize.Width;
                var hostHeight = effectiveHost.ClientSize.Height;

                var x = Math.Max(0, (hostWidth - toast.Width) / 2);

                // Count toasts already on the effective host (excluding this one) to compute
                // the stack offset.
                var existing = effectiveHost.Controls.OfType<Panel>()
                    .Count(c => c != toast && c.Tag is string tag && tag == ToastTag);

                var y = hostHeight - BottomMargin - toast.Height
                        - existing * (ToastHeight + ToastGap);

                toast.Location = new Point(x, y);
            }

            toast.Tag = ToastTag;   // marker so Reposition can find siblings
            Reposition();

            // Recompute position if the host resizes while the toast is alive.
            EventHandler? onResize = null;
            onResize = (s, e) => Reposition();
            effectiveHost.Resize += onResize;

            // Hold, then fade, then dispose.
            var holdTimer = new System.Windows.Forms.Timer { Interval = HoldMilliseconds };
            holdTimer.Tick += (s, e) =>
            {
                holdTimer.Stop();
                holdTimer.Dispose();

                // Hard remove — no fade. WinForms child controls can't truly fade without
                // layered-window tricks, and a color-lerp "fade" both leaves a visible trail
                // on hosts whose background differs from the surface behind the toast, and
                // takes seconds to reach its exit condition. A clean cut after the hold is
                // the correct behavior for a transient confirmation.
                effectiveHost.Resize -= onResize;
                effectiveHost.Controls.Remove(toast);
                toast.Dispose();
            };
            holdTimer.Start();
        }

        private const string ToastTag = "morcaffein.toast";

        private static Color FillColorFor(ToastKind kind) => kind switch
        {
            ToastKind.Success => BrandPalette.StatusPositiveFill,
            ToastKind.Error => BrandPalette.StatusNegativeFill,
            _ => BrandPalette.SurfaceCard,
        };

        private static Color TextColorFor(ToastKind kind) => kind switch
        {
            ToastKind.Success => BrandPalette.StatusPositiveText,
            ToastKind.Error => BrandPalette.StatusNegativeText,
            _ => BrandPalette.NeutralText,
        };

    }
}