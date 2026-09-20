using System.Drawing;

namespace CRM_Morcaffein.Theme
{
    /// <summary>
    /// Every color in the UI should come from this class or be a plain white/system neutral.
    /// The brand roasted-coffee brown (#572400) — and tints/tones/shades of it — is the only
    /// hue used for branding, primary actions, and content-area accents (see HeaderBackground,
    /// ButtonPrimaryFill). Large chrome surfaces (the sidebar, in particular) are explicitly
    /// allowed to lean on plain white/light-grey neutrals instead of brand tints for a lighter
    /// feel — see the Nav* tokens below. Status/tier indicators are plain text with no color
    /// coding at all for now (no blues/greens/reds/yellows anywhere).
    /// </summary>
    public static class BrandPalette
    {
        public static readonly Color Brand = ColorTranslator.FromHtml("#572400");

        // --- Shades (brand mixed toward black) : darker, for pressed states / headers ---
        public static readonly Color Shade10 = Shade(Brand, 0.10);
        public static readonly Color Shade25 = Shade(Brand, 0.25);
        public static readonly Color Shade45 = Shade(Brand, 0.45); // pressed button state

        // --- Tints (brand mixed toward white) : lighter, for surfaces / hover / disabled ---
        public static readonly Color Tint10 = Tint(Brand, 0.10);
        public static readonly Color Tint25 = Tint(Brand, 0.25); // hover state
        public static readonly Color Tint50 = Tint(Brand, 0.50);
        public static readonly Color Tint75 = Tint(Brand, 0.75); // subtle card background
        public static readonly Color Tint85 = Tint(Brand, 0.85); // sidebar chrome — a step darker than the page background
        public static readonly Color Tint90 = Tint(Brand, 0.90); // page background
        public static readonly Color Tint95 = Tint(Brand, 0.95); // near-white panel background

        // --- Tones (brand mixed toward mid-grey) : muted, for borders / disabled fills ---
        public static readonly Color Tone35 = Tone(Brand, 0.35);
        public static readonly Color Tone60 = Tone(Brand, 0.60); // disabled control fill
        public static readonly Color Tone80 = Tone(Brand, 0.80); // divider lines

        // --- Neutrals: white/black and brand-mixed greys only ---
        public static readonly Color White = Color.White;
        public static readonly Color Black = Color.Black;
        public static readonly Color NeutralText = Shade(Brand, 0.55);   // body text (brand-tinted near-black)
        public static readonly Color NeutralTextMuted = Tone(Brand, 0.45); // secondary text

        // --- Semantic roles, expressed via VALUE not hue ---
        public static Color SurfacePrimary => White;
        public static Color SurfaceSecondary => Tint95;
        public static Color SurfaceCard => Tint90;
        public static Color Border => Tone80;

        // Table headers: a light, brand-tinted band with dark text — not a full-brand fill.
        // The main color stays reserved for primary actions (buttons) so it doesn't compete
        // for attention across every grid in the app. Swapped with RowSelected below: the
        // header now takes the stronger of the two tints, so it stands out over a selected row.
        public static Color HeaderBackground => Tint50;
        public static Color HeaderText => NeutralText;

        // Selected row highlight — was the header's tint, now the lighter one.
        public static Color RowSelected => Tint90;

        public static Color ButtonPrimaryFill => Brand;
        public static Color ButtonPrimaryHover => Shade10;
        public static Color ButtonPrimaryPressed => Shade25;
        public static Color ButtonPrimaryText => White;

        public static Color ButtonSecondaryFill => Tint75;
        public static Color ButtonSecondaryHover => Tint50;
        public static Color ButtonSecondaryText => Shade(Brand, 0.30);

        // Plain neutral grey — deliberately NOT a tint/shade of the brand color, so a
        // disabled button reads as "off" rather than as a duller version of the same action.
        public static Color ButtonDisabledFill => Color.FromArgb(255, 210, 205, 198);
        public static Color ButtonDisabledText => Color.FromArgb(255, 135, 128, 120);

        // "Positive" state (points earned, resolved feedback, success) -> lightest fill, dark text
        public static Color StatusPositiveFill => Tint75;
        public static Color StatusPositiveText => Shade25;

        // "Attention" state (low stock, pending feedback, warnings) -> mid-tone, bolder text
        public static Color StatusAttentionFill => Tint50;
        public static Color StatusAttentionText => Shade45;

        // "Negative" state (dismissed, locked account, errors) -> darkest shade, high contrast text
        public static Color StatusNegativeFill => Shade25;
        public static Color StatusNegativeText => White;

        // --- Sidebar/nav chrome: a distinct step in tone from both the content background
        // (Tint95) and the table/card surfaces (White), so the sidebar reads as its own
        // region rather than blending into whatever's next to it. ---
        public static Color NavBackground => Tint85;
        public static Color NavFooterBackground => Tint75;
        public static Color NavText => NeutralTextMuted;
        public static Color NavActiveBackground => White;
        public static Color NavActiveText => Brand;
        public static Color NavHoverBackground => Color.White;

        // Unfilled star rating — Border (Tone80) reads too faint against white; Tone35 keeps
        // the same neutral hue family but with enough contrast to actually see the outline.
        public static Color RatingInactive => Tone35;

        // --- Top bar: thin brand strip above the content/sidebar (icon mark + tab name) ---
        public static Color TopBarBackground => White;
        public static Color TopBarBorder => Border;

        public static Font BrandFontRegular(float size) => TryCreateFont("Bahnschrift SemiLight", size, FontStyle.Regular);
        public static Font BrandFontBold(float size) => TryCreateFont("Bahnschrift SemiBold", size, FontStyle.Bold);

        private static Font TryCreateFont(string family, float size, FontStyle style)
        {
            try
            {
                return new Font(family, size, style);
            }
            catch
            {
                // Falls back gracefully if the custom font isn't installed on a given machine.
                return new Font(FontFamily.GenericSansSerif, size, style);
            }
        }

        private static Color Tint(Color c, double amount) => Mix(c, Color.White, amount);
        private static Color Shade(Color c, double amount) => Mix(c, Color.Black, amount);

        // "Tone" = mix toward a neutral mid-grey derived from the brand itself, for muted/disabled elements.
        private static Color Tone(Color c, double amount)
        {
            var grey = Mix(c, Color.Black, 0.5);
            grey = Mix(grey, Color.White, 0.5);
            return Mix(c, grey, amount);
        }

        private static Color Mix(Color a, Color b, double amount)
        {
            amount = Math.Clamp(amount, 0.0, 1.0);
            int r = (int)Math.Round(a.R + (b.R - a.R) * amount);
            int g = (int)Math.Round(a.G + (b.G - a.G) * amount);
            int bl = (int)Math.Round(a.B + (b.B - a.B) * amount);
            return Color.FromArgb(255, r, g, bl);
        }
    }
}
