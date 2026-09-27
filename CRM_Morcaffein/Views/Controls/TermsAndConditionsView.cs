using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// SuperAdmin tab: view Terms & Conditions documents. One card per plan
    /// on the left; the selected document's body shows in a scrollable panel
    /// on the right.
    ///
    /// Read-only. Editing the T&C content is a future feature — for now this
    /// is reference material for the platform operator (and a place to show
    /// which version is currently effective).
    ///
    /// Mirrors the Branch Management / Manage Users "cards + detail panel"
    /// shape, but the detail panel here is a plain scrolling text area,
    /// not a form.
    /// </summary>
    public class TermsAndConditionsView : UserControl
    {
        private readonly SuperAdminApi _api;

        // Layout
        private Panel _cardsColumnPanel = null!;
        private Panel _detailPanel = null!;
        private Label _detailTitleLabel = null!;
        private Label _detailMetaLabel = null!;
        private RichTextBox _detailBodyBox = null!;
        private Label _detailEmptyLabel = null!;

        // State
        private List<TermsSummaryDto> _terms = new();
        private List<TermsCardControl> _cards = new();
        private int? _selectedTermsId;

        public TermsAndConditionsView()
        {
            _api = new SuperAdminApi();
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

            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
                Padding = new Padding(24),
            };

            // ---------- Left cards column ----------
            _cardsColumnPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 320,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 20, 0),
            };

            var cardsHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
            };

            _cardsColumnPanel.Controls.Add(cardsHost);
            _cardsColumnPanel.Tag = cardsHost;   // stash the scroll host for later

            // ---------- Right detail panel ----------
            _detailPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(28),
            };

            _detailTitleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                Text = "",
            };

            _detailMetaLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Text = "",
            };

            // Spacer below the meta line.
            var detailSpacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 12,
                BackColor = Color.Transparent,
            };

            // The body text. RichTextBox so it scrolls natively and shows
            // multi-line plain text with proper wrapping. Read-only — this
            // is a viewer, not an editor.
            _detailBodyBox = new RichTextBox
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

            _detailEmptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = BrandPalette.SurfacePrimary,
                Text = "Loading…",
                Visible = true,
            };

            _detailPanel.Controls.Add(_detailBodyBox);
            _detailPanel.Controls.Add(_detailEmptyLabel);
            _detailPanel.Controls.Add(detailSpacer);
            _detailPanel.Controls.Add(_detailMetaLabel);
            _detailPanel.Controls.Add(_detailTitleLabel);

            bodyPanel.Controls.Add(_detailPanel);
            bodyPanel.Controls.Add(_cardsColumnPanel);

            Controls.Add(bodyPanel);
        }

        // =====================================================================
        // DATA LOADING
        // =====================================================================

        private async Task ReloadAsync()
        {
            try
            {
                _terms = await _api.GetTermsListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Error loading Terms & Conditions");
                return;
            }

            PopulateCards();

            // Auto-select the first card so the detail panel isn't empty on load.
            if (_terms.Count > 0)
                await SelectTermsAsync(_terms[0].TermsId);
            else
            {
                _detailEmptyLabel.Text = "No Terms & Conditions documents have been configured yet.";
                _detailEmptyLabel.Visible = true;
                _detailBodyBox.Visible = false;
                _detailTitleLabel.Text = "";
                _detailMetaLabel.Text = "";
            }
        }

        private void PopulateCards()
        {
            var cardsHost = (Panel)_cardsColumnPanel.Tag!;
            cardsHost.Controls.Clear();
            _cards.Clear();

            foreach (var t in _terms)
            {
                var card = new TermsCardControl(t)
                {
                    Dock = DockStyle.Top,
                    Height = 96,
                    Margin = new Padding(0, 0, 0, 12),
                };

                var capturedTermsId = t.TermsId;
                card.CardClicked += async (s, e) => await SelectTermsAsync(capturedTermsId);

                _cards.Add(card);
                cardsHost.Controls.Add(card);
            }

            // Reverse z-order so the first card sits at the top of the
            // scrolling stack (docked Top controls stack bottom-up).
            for (int i = cardsHost.Controls.Count - 1; i >= 0; i--)
                cardsHost.Controls.SetChildIndex(cardsHost.Controls[i], cardsHost.Controls.Count - 1 - i);

            HighlightSelectedCard();
        }

        private async Task SelectTermsAsync(int termsId)
        {
            _selectedTermsId = termsId;
            HighlightSelectedCard();

            // Immediately show "Loading…" so we don't render stale content
            // while the detail fetch is in flight.
            _detailTitleLabel.Text = "";
            _detailMetaLabel.Text = "";
            _detailBodyBox.Text = "";
            _detailBodyBox.Visible = false;
            _detailEmptyLabel.Text = "Loading…";
            _detailEmptyLabel.Visible = true;

            TermsDetailDto detail;
            try
            {
                detail = await _api.GetTermsDetailAsync(termsId);
            }
            catch (Exception ex)
            {
                _detailEmptyLabel.Text = $"Could not load: {ex.Message}";
                _detailEmptyLabel.Visible = true;
                _detailBodyBox.Visible = false;
                return;
            }

            _detailTitleLabel.Text = detail.Title;
            _detailMetaLabel.Text =
                $"Version {detail.Version}   ·   Effective {detail.EffectiveFrom:MMMM d, yyyy}" +
                (string.IsNullOrWhiteSpace(detail.PlanName) ? "" : $"   ·   Applies to the {detail.PlanName} plan");

            _detailBodyBox.Text = detail.Body;
            _detailBodyBox.SelectionStart = 0;
            _detailBodyBox.SelectionLength = 0;

            _detailBodyBox.Visible = true;
            _detailEmptyLabel.Visible = false;
        }

        private void HighlightSelectedCard()
        {
            foreach (var card in _cards)
                card.SetSelected(card.TermsId == _selectedTermsId);
        }
    }
}