using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// SuperAdmin tab: view and edit Terms & Conditions documents. One card
    /// per plan on the left; the selected document's body shows in a
    /// scrollable panel on the right.
    ///
    /// Read-only by default. Click Edit to arm editing; Save commits (with
    /// an auto-bumped version), Cancel reverts. Explicit mode because T&C
    /// is legal text — accidental keystrokes shouldn't silently modify it.
    ///
    /// Save flips IsSynced = false on the server, so the master sync worker
    /// pushes the updated row to cloud within ~20 seconds.
    /// </summary>
    public class TermsAndConditionsView : UserControl
    {
        private readonly SuperAdminApi _api;

        // Layout — cards column
        private Panel _cardsColumnPanel = null!;

        // Layout — detail panel
        private Panel _detailPanel = null!;
        private Label _detailTitleLabel = null!;
        private Label _detailMetaLabel = null!;
        private RichTextBox _detailBodyBox = null!;
        private Label _detailEmptyLabel = null!;

        // Layout — top-of-detail action row (Edit/Save/Cancel buttons)
        private Panel _detailActionsPanel = null!;
        private Button _editBtn = null!;
        private Button _saveBtn = null!;
        private Button _cancelBtn = null!;

        // State
        private List<TermsSummaryDto> _terms = new();
        private List<TermsCardControl> _cards = new();
        private int? _selectedTermsId;
        private TermsDetailDto? _loadedDetail;   // current doc — used to revert on Cancel
        private string _loadedBodyOriginal = string.Empty;
        private bool _isEditing;

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
            _cardsColumnPanel.Tag = cardsHost;

            // ---------- Right detail panel ----------
            _detailPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(28),
            };

            // ---- Title row (title label + Edit/Save/Cancel buttons on the right) ----
            var titleRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.Transparent,
            };

            _detailTitleLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "",
            };

            _detailActionsPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 300,
                BackColor = Color.Transparent,
            };

            _editBtn = MakeActionButton("Edit", isPrimary: false);
            _editBtn.Click += (s, e) => EnterEditMode();

            _saveBtn = MakeActionButton("Save", isPrimary: true);
            _saveBtn.Click += async (s, e) => await SaveAsync();

            _cancelBtn = MakeActionButton("Cancel", isPrimary: false);
            _cancelBtn.Click += (s, e) => ExitEditMode(revert: true);

            // Position right-to-left: Save, Cancel, Edit
            _saveBtn.Left = 300 - _saveBtn.Width;
            _cancelBtn.Left = _saveBtn.Left - 8 - _cancelBtn.Width;
            _editBtn.Left = _cancelBtn.Left - 8 - _editBtn.Width;
            _saveBtn.Top = _cancelBtn.Top = _editBtn.Top = 3;

            _detailActionsPanel.Controls.Add(_saveBtn);
            _detailActionsPanel.Controls.Add(_cancelBtn);
            _detailActionsPanel.Controls.Add(_editBtn);

            titleRow.Controls.Add(_detailTitleLabel);
            titleRow.Controls.Add(_detailActionsPanel);

            _detailMetaLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Text = "",
            };

            var detailSpacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 12,
                BackColor = Color.Transparent,
            };

            // The body text. Read-only unless Edit mode is active.
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

            // Add order matters: Fill (body/empty) first, then Top-docked
            // in bottom-to-top order (spacer, meta, title row).
            _detailPanel.Controls.Add(_detailBodyBox);
            _detailPanel.Controls.Add(_detailEmptyLabel);
            _detailPanel.Controls.Add(detailSpacer);
            _detailPanel.Controls.Add(_detailMetaLabel);
            _detailPanel.Controls.Add(titleRow);

            bodyPanel.Controls.Add(_detailPanel);
            bodyPanel.Controls.Add(_cardsColumnPanel);

            Controls.Add(bodyPanel);

            UpdateActionButtonVisibility();
        }

        private static Button MakeActionButton(string text, bool isPrimary) => new()
        {
            Text = text,
            Size = new Size(90, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = isPrimary ? BrandPalette.ButtonPrimaryFill : BrandPalette.ButtonSecondaryFill,
            ForeColor = isPrimary ? BrandPalette.ButtonPrimaryText : BrandPalette.ButtonSecondaryText,
            Font = BrandPalette.BrandFontBold(9.5f),
            Cursor = Cursors.Hand,
        };

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

            if (_terms.Count > 0)
                await SelectTermsAsync(_terms[0].TermsId);
            else
            {
                _detailEmptyLabel.Text = "No Terms & Conditions documents have been configured yet.";
                _detailEmptyLabel.Visible = true;
                _detailBodyBox.Visible = false;
                _detailTitleLabel.Text = "";
                _detailMetaLabel.Text = "";
                UpdateActionButtonVisibility();
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

            for (int i = cardsHost.Controls.Count - 1; i >= 0; i--)
                cardsHost.Controls.SetChildIndex(cardsHost.Controls[i], cardsHost.Controls.Count - 1 - i);

            HighlightSelectedCard();
        }

        private async Task SelectTermsAsync(int termsId)
        {
            // Guard: don't switch while editing — the user might lose text.
            // If editing, ask first.
            if (_isEditing)
            {
                var confirm = MessageBox.Show(
                    FindForm(),
                    "Discard unsaved changes and switch to another document?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                    return;

                ExitEditMode(revert: true);
            }

            _selectedTermsId = termsId;
            HighlightSelectedCard();

            _detailTitleLabel.Text = "";
            _detailMetaLabel.Text = "";
            _detailBodyBox.Text = "";
            _detailBodyBox.Visible = false;
            _detailEmptyLabel.Text = "Loading…";
            _detailEmptyLabel.Visible = true;
            UpdateActionButtonVisibility();

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
                _loadedDetail = null;
                UpdateActionButtonVisibility();
                return;
            }

            _loadedDetail = detail;
            _loadedBodyOriginal = detail.Body;

            _detailTitleLabel.Text = detail.Title;
            _detailMetaLabel.Text =
                $"Version {detail.Version}   ·   Effective {detail.EffectiveFrom:MMMM d, yyyy}" +
                (string.IsNullOrWhiteSpace(detail.PlanName) ? "" : $"   ·   Applies to the {detail.PlanName} plan");

            _detailBodyBox.Text = detail.Body;
            _detailBodyBox.SelectionStart = 0;
            _detailBodyBox.SelectionLength = 0;

            _detailBodyBox.Visible = true;
            _detailEmptyLabel.Visible = false;

            UpdateActionButtonVisibility();
        }

        private void HighlightSelectedCard()
        {
            foreach (var card in _cards)
                card.SetSelected(card.TermsId == _selectedTermsId);
        }

        // =====================================================================
        // EDIT / SAVE / CANCEL
        // =====================================================================

        private void EnterEditMode()
        {
            if (_loadedDetail is null) return;

            _isEditing = true;
            _detailBodyBox.ReadOnly = false;
            _detailBodyBox.BackColor = Color.FromArgb(255, 252, 246);   // subtle warm tint
            _detailBodyBox.Focus();

            UpdateActionButtonVisibility();
        }

        private void ExitEditMode(bool revert)
        {
            if (revert)
                _detailBodyBox.Text = _loadedBodyOriginal;

            _isEditing = false;
            _detailBodyBox.ReadOnly = true;
            _detailBodyBox.BackColor = BrandPalette.SurfacePrimary;

            UpdateActionButtonVisibility();
        }

        private async Task SaveAsync()
        {
            if (_loadedDetail is null || _selectedTermsId is null) return;

            var newBody = _detailBodyBox.Text;
            if (string.IsNullOrWhiteSpace(newBody))
            {
                Toast.Show(this, "Body text cannot be empty.", ToastKind.Error);
                return;
            }

            if (string.Equals(newBody, _loadedBodyOriginal, StringComparison.Ordinal))
            {
                Toast.Show(this, "No changes to save.", ToastKind.Info);
                ExitEditMode(revert: false);
                return;
            }

            _saveBtn.Enabled = false;
            _saveBtn.Text = "Saving…";

            try
            {
                var updated = await _api.UpdateTermsAsync(_selectedTermsId.Value, newBody);

                // Refresh local state and UI from the server's response.
                _loadedDetail = updated;
                _loadedBodyOriginal = updated.Body;

                _detailTitleLabel.Text = updated.Title;
                _detailMetaLabel.Text =
                    $"Version {updated.Version}   ·   Effective {updated.EffectiveFrom:MMMM d, yyyy}" +
                    (string.IsNullOrWhiteSpace(updated.PlanName) ? "" : $"   ·   Applies to the {updated.PlanName} plan");

                _detailBodyBox.Text = updated.Body;

                // Update the card's version label — the left-side card shows the version.
                UpdateCardVersion(updated.TermsId, updated.Version);

                ExitEditMode(revert: false);

                Toast.Show(this, $"Saved. Version bumped to {updated.Version}.", ToastKind.Success);
            }
            catch (Exception ex)
            {
                Toast.Show(this, $"Save failed: {ex.Message}", ToastKind.Error);
            }
            finally
            {
                _saveBtn.Enabled = true;
                _saveBtn.Text = "Save";
            }
        }

        private void UpdateCardVersion(int termsId, string newVersion)
        {
            // The card control holds its own TermsSummaryDto. Rather than
            // walking the control tree, we rebuild the card display by
            // looking it up and calling a small setter we add to
            // TermsCardControl.
            var card = _cards.FirstOrDefault(c => c.TermsId == termsId);
            card?.SetVersion(newVersion);
        }

        private void UpdateActionButtonVisibility()
        {
            var hasDoc = _loadedDetail is not null;

            _editBtn.Visible = hasDoc && !_isEditing;
            _saveBtn.Visible = hasDoc && _isEditing;
            _cancelBtn.Visible = hasDoc && _isEditing;
        }
    }
}