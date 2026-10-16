using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Tenant-facing read-only T&C view. Shows the T&C document for the
    /// tenant's current plan, plus who accepted it and when. Admin-only —
    /// the nav item is only added for the Admin role in FrmMain.
    ///
    /// Layout mirrors the SuperAdmin TermsAndConditionsView detail panel —
    /// a single card with title, meta line, and a scrollable body. But
    /// there's no card list on the left: the tenant has exactly one plan,
    /// so exactly one T&C document.
    ///
    /// Empty state (no plan assigned or plan has no T&C) shows a centered
    /// message rather than a broken viewer.
    /// </summary>
    public class TenantTermsView : UserControl
    {
        private readonly MeApi _api;

        private Panel _cardPanel = null!;
        private Label _titleLabel = null!;
        private Label _metaLabel = null!;
        private Label _acceptedLabel = null!;
        private RichTextBox _bodyBox = null!;
        private Label _emptyLabel = null!;
        private Panel _loadingOverlay = null!;

        public TenantTermsView()
        {
            _api = new MeApi();
            BuildLayout();
            Load += async (s, e) => await ReloadAsync();
        }

        // =====================================================================
        // LAYOUT
        // =====================================================================

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;
            BackColor = BrandPalette.SurfaceSecondary;

            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = BrandPalette.SurfaceSecondary,
            };

            _cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(28, 24, 28, 24),
                Visible = false,
            };

            // Title — T&C document title.
            _titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                Font = BrandPalette.BrandFontBold(16f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                Text = "",
                TextAlign = ContentAlignment.MiddleLeft,
            };

            // Meta line — "Version X.Y · Effective <date> · Applies to the <plan> plan"
            _metaLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Text = "",
            };

            // Accepted line — "Accepted by X on Y" (only shown if the data exists).
            _acceptedLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
                Text = "",
            };

            // Spacer between header and body.
            var spacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 16,
                BackColor = Color.Transparent,
            };

            // Body — read-only RichTextBox so it scrolls natively and wraps text.
            _bodyBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = BrandPalette.SurfacePrimary,
                ForeColor = BrandPalette.NeutralText,
                Font = BrandPalette.BrandFontRegular(10f),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true,
                DetectUrls = false,
                Text = "",
            };

            // Empty state — shown when HasTerms == false.
            _emptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(11f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = BrandPalette.SurfacePrimary,
                Text = "Loading…",
                Visible = false,
            };

            // Add order: Fill first, then docked Top bottom-up.
            _cardPanel.Controls.Add(_bodyBox);
            _cardPanel.Controls.Add(_emptyLabel);
            _cardPanel.Controls.Add(spacer);
            _cardPanel.Controls.Add(_acceptedLabel);
            _cardPanel.Controls.Add(_metaLabel);
            _cardPanel.Controls.Add(_titleLabel);

            contentHost.Controls.Add(_cardPanel);
            Controls.Add(contentHost);

            // Loading overlay — covers everything until the fetch returns.
            _loadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
            };
            var loadingLabel = new Label
            {
                Text = "Loading terms…",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _loadingOverlay.Controls.Add(loadingLabel);
            Controls.Add(_loadingOverlay);
            _loadingOverlay.BringToFront();
        }

        // =====================================================================
        // LOAD
        // =====================================================================

        private async Task ReloadAsync()
        {
            TenantTermsDto terms;
            try
            {
                terms = await _api.GetTermsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(),
                    $"Could not load terms: {ex.Message}",
                    "Terms & Conditions");
                _loadingOverlay.Visible = false;
                return;
            }

            if (!terms.HasTerms || string.IsNullOrWhiteSpace(terms.Body))
            {
                _cardPanel.Visible = true;
                _emptyLabel.Visible = true;
                _bodyBox.Visible = false;
                _titleLabel.Text = "";
                _metaLabel.Text = "";
                _acceptedLabel.Text = "";
                _emptyLabel.Text =
                    string.IsNullOrWhiteSpace(terms.PlanName)
                        ? "No subscription terms available.\n\nYour account does not currently have a plan assigned. Contact your platform operator."
                        : "No subscription terms available.\n\nThe plan assigned to your account does not have a published terms document.";
                _loadingOverlay.Visible = false;
                return;
            }

            _cardPanel.Visible = true;
            _bodyBox.Visible = true;
            _emptyLabel.Visible = false;

            _titleLabel.Text = terms.Title ?? "Terms & Conditions";

            var metaParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(terms.Version))
                metaParts.Add($"Version {terms.Version}");
            if (terms.EffectiveFrom.HasValue)
                metaParts.Add($"Effective {terms.EffectiveFrom.Value:MMMM d, yyyy}");
            if (!string.IsNullOrWhiteSpace(terms.PlanName))
                metaParts.Add($"Applies to the {terms.PlanName} plan");
            _metaLabel.Text = string.Join("   ·   ", metaParts);

            if (!string.IsNullOrWhiteSpace(terms.AcceptedBy))
            {
                var acceptedAtText = terms.AcceptedAt.HasValue
                    ? $" on {terms.AcceptedAt.Value.ToLocalTime():MMMM d, yyyy}"
                    : "";
                _acceptedLabel.Text = $"Accepted by {terms.AcceptedBy}{acceptedAtText}";
            }
            else
            {
                _acceptedLabel.Text = "";
            }

            _bodyBox.Text = terms.Body;
            _bodyBox.SelectionStart = 0;
            _bodyBox.SelectionLength = 0;

            _loadingOverlay.Visible = false;
        }
    }
}