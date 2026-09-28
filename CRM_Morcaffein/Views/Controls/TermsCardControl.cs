using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// A single Terms & Conditions card. Title, version, effective date.
    /// Raises CardClicked when the user clicks anywhere on it. Host
    /// (TermsAndConditionsView) handles selection highlighting via
    /// SetSelected.
    /// </summary>
    public class TermsCardControl : Panel
    {
        public event EventHandler? CardClicked;

        public int TermsId { get; }

        private readonly Label _titleLabel;
        private readonly Label _versionLabel;
        private readonly Label _effectiveLabel;
        private bool _selected;

        public TermsCardControl(TermsSummaryDto terms)
        {
            TermsId = terms.TermsId;

            BackColor = BrandPalette.SurfacePrimary;
            BorderStyle = BorderStyle.FixedSingle;
            Cursor = Cursors.Hand;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = terms.Title,
                Dock = DockStyle.Top,
                Height = 26,
                Font = BrandPalette.BrandFontBold(10.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            _versionLabel = new Label
            {
                Text = $"Version {terms.Version}",
                Dock = DockStyle.Top,
                Height = 18,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
            };

            _effectiveLabel = new Label
            {
                Text = $"Effective {terms.EffectiveFrom:MMM d, yyyy}",
                Dock = DockStyle.Top,
                Height = 16,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            Controls.Add(_effectiveLabel);
            Controls.Add(_versionLabel);
            Controls.Add(_titleLabel);

            // Wire click on the panel AND every child so a click anywhere
            // inside the card selects it.
            Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);
            _titleLabel.Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);
            _versionLabel.Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);
            _effectiveLabel.Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the selected/unselected visual state. Called by the host view
        /// after every selection change.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _selected = selected;

            BackColor = selected
                ? BrandPalette.RowSelected
                : BrandPalette.SurfacePrimary;

            _titleLabel.ForeColor = selected
                ? BrandPalette.Brand
                : BrandPalette.NeutralText;

            Invalidate();
        }

        /// <summary>
        /// Updates the version string displayed on the card. Called by the
        /// host view after a successful save bumps the version server-side.
        /// </summary>
        public void SetVersion(string version)
        {
            _versionLabel.Text = $"Version {version}";
        }
    }
}