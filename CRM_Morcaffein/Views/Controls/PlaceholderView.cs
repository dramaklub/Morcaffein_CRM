using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Reusable placeholder for tabs that don't have their real UI yet.
    /// Shows a centered title + short description.
    /// </summary>
    public class PlaceholderView : UserControl
    {
        public PlaceholderView(string title, string description)
        {
            Dock = DockStyle.Fill;
            BackColor = BrandPalette.SurfaceSecondary;

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                Padding = new Padding(24),
                Margin = new Padding(24),
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontBold(20f),
                ForeColor = BrandPalette.NeutralText,
            };

            var descriptionLabel = new Label
            {
                Text = description,
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            var spacer = new Panel { Dock = DockStyle.Top, Height = 80 };

            card.Controls.Add(descriptionLabel);
            card.Controls.Add(titleLabel);
            card.Controls.Add(spacer);

            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
                Padding = new Padding(24),
            };
            outer.Controls.Add(card);

            Controls.Add(outer);
        }
    }
}