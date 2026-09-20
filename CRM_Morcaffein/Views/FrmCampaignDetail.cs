using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.domain.entities;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// Modal for viewing, creating, and editing a campaign. Three modes:
    ///
    ///   Create  — opened by "+ New Campaign". Empty form, Create / Cancel buttons.
    ///   View    — opened by clicking a card. Read-only summary + Edit / Activate /
    ///             Deactivate / Close buttons.
    ///   Edit    — entered from View by clicking Edit. Same fields as Create, but
    ///             pre-populated and saving via the update endpoint.
    ///
    /// The modal is code-only (no Designer.cs), matching FrmCreateEmployee and
    /// FrmItemCustomizer.
    /// </summary>
    public class FrmCampaignDetail : Form
    {
        private enum Mode { Create, View, Edit }

        private readonly CampaignController _controller;
        private readonly CampaignResponseDto? _existing;

        private Mode _mode;

        // Fields
        private TextBox _nameTxtBx = null!;
        private ComboBox _categoryCombo = null!;
        private ComboBox _discountTypeCombo = null!;
        private Label _discountValueLabel = null!;
        private NumericUpDown _discountValueNud = null!;
        private Label _requiredProductLabel = null!;
        private ComboBox _requiredProductCombo = null!;
        private Label _rewardProductLabel = null!;
        private ComboBox _rewardProductCombo = null!;

        // Pending B1T1 selections — applied once the async product load populates the
        // two combos above (they can't be selected before their Items exist).
        private int? _pendingRequiredProductId;
        private int? _pendingRewardProductId;
        private ComboBox _targetTierCombo = null!;
        private NumericUpDown _minPurchaseNud = null!;
        private CheckBox _hasMinPurchaseChk = null!;
        private NumericUpDown _maxDiscountNud = null!;
        private CheckBox _hasMaxDiscountChk = null!;
        private NumericUpDown _maxRedemptionsNud = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;

        private Label _errorLabel = null!;

        // Mode-dependent controls
        private Panel _formPanel = null!;         // holds the editable fields
        private Panel _viewPanel = null!;         // holds the read-only summary
        private Panel _actionRow = null!;         // holds the buttons

        private Button _primaryBtn = null!;       // Create or Save
        private Button _editBtn = null!;          // visible in View
        private Button _activateBtn = null!;      // visible in View
        private Button _deactivateBtn = null!;    // visible in View
        private Button _cancelBtn = null!;        // visible in Create/Edit (as Cancel)
        private Button _closeBtn = null!;         // visible in View

        /// <summary>True if a mutation (create/edit/activate/deactivate) was performed.</summary>
        public bool ChangedSomething { get; private set; }

        private FrmCampaignDetail(CampaignController controller, CampaignResponseDto? existing, Mode initialMode)
        {
            _controller = controller;
            _existing = existing;
            _mode = initialMode;

            Text = initialMode switch
            {
                Mode.Create => "New Campaign",
                Mode.Edit => "Edit Campaign",
                _ => "Campaign Detail",
            };

            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(600, 720);
            BackColor = BrandPalette.SurfaceCard;
            Font = BrandPalette.BrandFontRegular(9.5f);
            Padding = new Padding(24);

            BuildLayout();

            if (initialMode == Mode.Create)
                PopulateEmptyForm();
            else
                PopulateFromExisting();

            ApplyMode();
            UpdateB1T1FieldsVisibility();
            _ = LoadProductsAsync();

            // Re-run button positioning once the form is shown. ApplyMode and BuildLayout
            // run before docking settles, so their button positions may be off. Load fires
            // after ClientSize is final, guaranteeing a correct layout.
            Load += (s, e) =>
            {
                PositionActionButtons(_actionRow);
                if (_mode == Mode.View) RefreshViewPanel();
            };
        }
        

        // =====================================================================
        // FACTORY METHODS — how the caller opens the modal
        // =====================================================================

        public static FrmCampaignDetail ForCreate(CampaignController controller)
            => new(controller, existing: null, Mode.Create);

        public static FrmCampaignDetail ForView(CampaignController controller, CampaignResponseDto campaign)
            => new(controller, campaign, Mode.View);

        // =====================================================================
        // LAYOUT
        // =====================================================================

        private void BuildLayout()
        {
            // ---- Title ----
            var titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
                Text = _mode == Mode.Create ? "Create Campaign" : "Campaign Details",
                BackColor = Color.Transparent,
            };

            var subtitleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Text = _mode == Mode.Create
                    ? "Fill in the campaign details."
                    : "Review the campaign details.",
            };

            // ---- View panel (read-only summary) ----
            _viewPanel = BuildViewPanel();

            // ---- Form panel (editable fields) ----
            _formPanel = BuildFormPanel();

            // ---- Error label ----
            _errorLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = BrandPalette.SurfaceCard,

            };

            // ---- Action row (buttons) ----
            _actionRow = BuildActionRow();

            // Compose. The Fill panels go in first so they have the LOWEST z-order in
            // their dock band; the error label and action row go in after so they paint
            // on top of the panel's bottom edge. This is what gives the panel a clean cut
            // with a visible tan strip above the buttons, instead of the panel extending
            // underneath them.
            Controls.Add(_viewPanel);
            Controls.Add(_formPanel);
            Controls.Add(_errorLabel);
            Controls.Add(_actionRow);
            Controls.Add(subtitleLabel);
            Controls.Add(titleLabel);
            PositionActionButtons(_actionRow);
        }

        private Panel BuildViewPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                Padding = new Padding(18, 14, 18, 14),
                Margin = new Padding(0, 0, 0, 12),
                Visible = false,
            };

            // Populated in ApplyMode() by switching the label texts.
            return panel;
        }

        private Panel BuildFormPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Visible = false,
                BackColor = BrandPalette.SurfacePrimary,
                Margin = new Padding(0, 0, 0, 12),
            };

            const int labelH = 18;
            const int fieldH = 30;
            const int rowH = 58;
            const int fieldW = 464;

            int y = 0;

            void AddLabel(string text)
            {
                panel.Controls.Add(new Label
                {
                    Text = text,
                    Location = new Point(0, y),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontRegular(9f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                });
            }

            // Name
            AddLabel("Promo name *");
            _nameTxtBx = new TextBox
            {
                Location = new Point(0, y + labelH),
                Size = new Size(fieldW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            panel.Controls.Add(_nameTxtBx);
            y += rowH;

            // Category
            AddLabel("Category *");
            _categoryCombo = new ComboBox
            {
                Location = new Point(0, y + labelH),
                Size = new Size(fieldW, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _categoryCombo.Items.AddRange(new object[]
            {
                PromoCategories.Seasonal,
                PromoCategories.Loyalty,
                PromoCategories.Birthday,
                PromoCategories.Inactive,
            });
            _categoryCombo.SelectedIndex = 0;
            panel.Controls.Add(_categoryCombo);
            y += rowH;

            // Discount type
            AddLabel("Discount type *");
            _discountTypeCombo = new ComboBox
            {
                Location = new Point(0, y + labelH),
                Size = new Size(220, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _discountTypeCombo.Items.AddRange(new object[] { "Percentage", "FixedAmount", "B1T1" });
            _discountTypeCombo.SelectedIndex = 0;
            _discountTypeCombo.SelectedIndexChanged += (s, e) => UpdateB1T1FieldsVisibility();
            panel.Controls.Add(_discountTypeCombo);

            // Discount value (to the right of type) — hidden for B1T1, which has no
            // fixed discount value: the free item's own price IS the discount.
            _discountValueLabel = new Label
            {
                Text = "Discount value *",
                Location = new Point(244, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            panel.Controls.Add(_discountValueLabel);
            _discountValueNud = new NumericUpDown
            {
                Location = new Point(244, y + labelH),
                Size = new Size(220, fieldH),
                DecimalPlaces = 2,
                Minimum = 0.01m,
                Maximum = 100000m,
                Value = 10m,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            panel.Controls.Add(_discountValueNud);
            y += rowH;

            // B1T1 product pickers — only meaningful when Discount type = B1T1, but the
            // row's vertical space is always reserved so nothing below it has to reflow
            // when the two controls show/hide.
            _requiredProductLabel = new Label
            {
                Text = "Buy this product *",
                Location = new Point(0, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            panel.Controls.Add(_requiredProductLabel);
            _requiredProductCombo = new ComboBox
            {
                Location = new Point(0, y + labelH),
                Size = new Size(220, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "ProductName",
                ValueMember = "ProductId",
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            panel.Controls.Add(_requiredProductCombo);

            _rewardProductLabel = new Label
            {
                Text = "Get this product free *",
                Location = new Point(244, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            panel.Controls.Add(_rewardProductLabel);
            _rewardProductCombo = new ComboBox
            {
                Location = new Point(244, y + labelH),
                Size = new Size(220, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "ProductName",
                ValueMember = "ProductId",
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            panel.Controls.Add(_rewardProductCombo);
            y += rowH;

            // Target tier
            AddLabel("Target tier *");
            _targetTierCombo = new ComboBox
            {
                Location = new Point(0, y + labelH),
                Size = new Size(fieldW, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _targetTierCombo.Items.AddRange(new object[] { "All", "Regular", "Silver", "Gold" });
            _targetTierCombo.SelectedIndex = 0;
            panel.Controls.Add(_targetTierCombo);
            y += rowH;

            // Min purchase (checkbox + value)
            _hasMinPurchaseChk = new CheckBox
            {
                Text = "Minimum purchase amount",
                Location = new Point(0, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _minPurchaseNud = new NumericUpDown
            {
                Location = new Point(0, y + labelH + 4),
                Size = new Size(220, fieldH),
                DecimalPlaces = 2,
                Minimum = 0m,
                Maximum = 100000m,
                Enabled = false,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _hasMinPurchaseChk.CheckedChanged += (s, e) => _minPurchaseNud.Enabled = _hasMinPurchaseChk.Checked;
            panel.Controls.Add(_hasMinPurchaseChk);
            panel.Controls.Add(_minPurchaseNud);

            // Max discount (checkbox + value) — right column
            _hasMaxDiscountChk = new CheckBox
            {
                Text = "Maximum discount cap",
                Location = new Point(244, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _maxDiscountNud = new NumericUpDown
            {
                Location = new Point(244, y + labelH + 4),
                Size = new Size(220, fieldH),
                DecimalPlaces = 2,
                Minimum = 0m,
                Maximum = 100000m,
                Enabled = false,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _hasMaxDiscountChk.CheckedChanged += (s, e) => _maxDiscountNud.Enabled = _hasMaxDiscountChk.Checked;
            panel.Controls.Add(_hasMaxDiscountChk);
            panel.Controls.Add(_maxDiscountNud);
            y += rowH + 12;

            // Max redemptions per customer
            AddLabel("Max redemptions per customer *");
            _maxRedemptionsNud = new NumericUpDown
            {
                Location = new Point(0, y + labelH),
                Size = new Size(220, fieldH),
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            panel.Controls.Add(_maxRedemptionsNud);
            y += rowH;

            // Start date
            AddLabel("Start date *");
            _startDatePicker = new DateTimePicker
            {
                Location = new Point(0, y + labelH),
                Size = new Size(220, fieldH),
                Format = DateTimePickerFormat.Short,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Value = DateTime.Today,
            };
            panel.Controls.Add(_startDatePicker);

            // End date
            panel.Controls.Add(new Label
            {
                Text = "End date *",
                Location = new Point(244, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            });
            _endDatePicker = new DateTimePicker
            {
                Location = new Point(244, y + labelH),
                Size = new Size(220, fieldH),
                Format = DateTimePickerFormat.Short,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Value = DateTime.Today.AddMonths(1),
            };
            panel.Controls.Add(_endDatePicker);
            y += rowH;

            panel.AutoScrollMinSize = new Size(0, y + 20);
            return panel;
        }

        private Panel BuildActionRow()
        {
            var row = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.Transparent,
            };

            // Create primary button (Create or Save)
            _primaryBtn = new Button
            {
                Text = "Create Campaign",
                Size = new Size(160, 36),
                Location = new Point(0, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
            };
            _primaryBtn.FlatAppearance.BorderSize = 0;
            _primaryBtn.Click += PrimaryBtn_Click;

            // Edit (View mode)
            _editBtn = new Button
            {
                Text = "Edit",
                Size = new Size(100, 36),
                Location = new Point(0, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _editBtn.FlatAppearance.BorderSize = 0;
            _editBtn.Click += (s, e) => SwitchToEdit();

            // Activate (View mode)
            _activateBtn = new Button
            {
                Text = "Activate",
                Size = new Size(110, 36),
                Location = new Point(110, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _activateBtn.FlatAppearance.BorderSize = 0;
            _activateBtn.Click += (s, e) => ToggleActivation(activate: true);

            // Deactivate (View mode)
            _deactivateBtn = new Button
            {
                Text = "Deactivate",
                Size = new Size(110, 36),
                Location = new Point(230, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.StatusNegativeFill,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _deactivateBtn.FlatAppearance.BorderSize = 0;
            _deactivateBtn.Click += (s, e) => ToggleActivation(activate: false);

            // Cancel (Create/Edit mode)
            _cancelBtn = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 36),
                Location = new Point(0, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _cancelBtn.FlatAppearance.BorderSize = 0;
            _cancelBtn.Click += CancelBtn_Click;

            // Close (View mode)
            _closeBtn = new Button
            {
                Text = "Close",
                Size = new Size(100, 36),
                Location = new Point(0, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _closeBtn.FlatAppearance.BorderSize = 0;
            _closeBtn.Click += (s, e) => { DialogResult = ChangedSomething ? DialogResult.OK : DialogResult.Cancel; Close(); };
            
            ButtonStyler.TrackEnabledState(_closeBtn);
            ButtonStyler.TrackEnabledState(_cancelBtn);
            ButtonStyler.TrackEnabledState(_deactivateBtn);
            ButtonStyler.TrackEnabledState(_activateBtn);
            ButtonStyler.TrackEnabledState(_editBtn);
            ButtonStyler.TrackEnabledState(_primaryBtn);

            row.Controls.Add(_closeBtn);
            row.Controls.Add(_cancelBtn);
            row.Controls.Add(_deactivateBtn);
            row.Controls.Add(_activateBtn);
            row.Controls.Add(_editBtn);
            row.Controls.Add(_primaryBtn);

            return row;
        }

        /// <summary>
        /// Places the visible action buttons left-to-right at fixed offsets inside the
        /// row. Deliberately does NOT depend on the row's ClientSize — this runs during
        /// the form's Load event, before docking has settled, and earlier attempts to
        /// compute positions from ClientSize pushed buttons off the right edge (or behind
        /// each other) because the row reported a stale or zero width at that moment.
        ///
        /// Layout: buttons are laid out in visual order from x=16 onward, rightward.
        /// Whatever fits, fits; the row's Width is generous enough for every combination
        /// of visible buttons, which is why fixed offsets are safe here.
        ///
        /// Visual order (left → right):
        ///   Close / Cancel  |  Deactivate  |  Activate  |  Edit  |  Create/Save
        /// Destructive (Deactivate) sits well away from the confirm action (Create/Save)
        /// to avoid mis-clicks.
        /// </summary>
        private void PositionActionButtons(Panel row)
        {
            const int leftInset = 16;
            const int gap = 8;
            const int topY = 8;

            var ordered = new[]
 {
                _closeBtn,
                _cancelBtn,
                _editBtn,
                _activateBtn,
                _deactivateBtn,
                _primaryBtn,
            };

            var x = leftInset;

            foreach (var btn in ordered)
            {
                if (!btn.Visible) continue;

                btn.Left = x;
                btn.Top = topY;
                x += btn.Width + gap;
            }
        }

        // =====================================================================
        // MODE
        // =====================================================================

        private void ApplyMode()
        {
            // Reset visibility for all mode-dependent controls
            _primaryBtn.Visible = false;
            _editBtn.Visible = false;
            _activateBtn.Visible = false;
            _deactivateBtn.Visible = false;
            _cancelBtn.Visible = false;
            _closeBtn.Visible = false;
            _viewPanel.Visible = false;
            _formPanel.Visible = false;
            _errorLabel.Text = string.Empty;

            switch (_mode)
            {
                case Mode.Create:
                    Text = "New Campaign";
                    _formPanel.Visible = true;
                    _primaryBtn.Text = "Create Campaign";
                    _primaryBtn.Visible = true;
                    _cancelBtn.Visible = true;
                    break;

                case Mode.Edit:
                    Text = "Edit Campaign";
                    _formPanel.Visible = true;
                    _primaryBtn.Text = "Save Changes";
                    _primaryBtn.Visible = true;
                    _cancelBtn.Visible = true;
                    break;

                case Mode.View:
                    Text = "Campaign Details";
                    _viewPanel.Visible = true;
                    RefreshViewPanel();
                    _editBtn.Visible = true;
                    _closeBtn.Visible = true;

                    // Only one of Activate / Deactivate is ever visible, based on status.
                    var isActive = _existing?.Status?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true;
                    _activateBtn.Visible = !isActive;
                    _deactivateBtn.Visible = isActive;
                    break;
            }

            // Reflow button positions after visibility changes.
            PositionActionButtons(_actionRow);
        }

        private void SwitchToEdit()
        {
            _mode = Mode.Edit;
            PopulateFromExisting();
            ApplyMode();
        }

        // =====================================================================
        // DATA BINDING
        // =====================================================================

        /// <summary>
        /// Shows the product pickers and hides the discount-value field when the selected
        /// discount type is B1T1, and vice versa. Called on every discount-type change and
        /// once up front after the initial mode/population pass.
        /// </summary>
        private void UpdateB1T1FieldsVisibility()
        {
            var isB1T1 = _discountTypeCombo.SelectedItem as string == "B1T1";

            _discountValueLabel.Visible = !isB1T1;
            _discountValueNud.Visible = !isB1T1;

            _requiredProductLabel.Visible = isB1T1;
            _requiredProductCombo.Visible = isB1T1;
            _rewardProductLabel.Visible = isB1T1;
            _rewardProductCombo.Visible = isB1T1;
        }

        /// <summary>
        /// Loads the product catalog (shared cache — same one the POS uses) and fills
        /// both B1T1 combos. Runs async because the constructor can't await; any pending
        /// selection from PopulateFromExisting is applied once the items exist.
        /// </summary>
        private async Task LoadProductsAsync()
        {
            try
            {
                var catalog = await CRM_Morcaffein.winforms.Services.ProductCatalogCache.GetCatalogAsync();

                _requiredProductCombo.DataSource = catalog.Products.ToList();
                _rewardProductCombo.DataSource = catalog.Products.ToList();

                if (_pendingRequiredProductId.HasValue)
                    _requiredProductCombo.SelectedValue = _pendingRequiredProductId.Value;
                if (_pendingRewardProductId.HasValue)
                    _rewardProductCombo.SelectedValue = _pendingRewardProductId.Value;
            }
            catch (Exception ex)
            {
                _errorLabel.Text = $"Could not load products for B1T1 promos: {ex.Message}";
            }
        }

        private void PopulateEmptyForm()
        {
            _nameTxtBx.Text = string.Empty;
            _categoryCombo.SelectedIndex = 0;
            _discountTypeCombo.SelectedIndex = 0;
            _discountValueNud.Value = 10m;
            _pendingRequiredProductId = null;
            _pendingRewardProductId = null;
            _targetTierCombo.SelectedIndex = 0;
            _hasMinPurchaseChk.Checked = false;
            _minPurchaseNud.Value = 0m;
            _minPurchaseNud.Enabled = false;
            _hasMaxDiscountChk.Checked = false;
            _maxDiscountNud.Value = 0m;
            _maxDiscountNud.Enabled = false;
            _maxRedemptionsNud.Value = 1m;
            _startDatePicker.Value = DateTime.Today;
            _endDatePicker.Value = DateTime.Today.AddMonths(1);
        }

        private void PopulateFromExisting()
        {
            if (_existing is null) return;

            _nameTxtBx.Text = _existing.PromoName;

            var catIdx = _categoryCombo.Items.IndexOf(_existing.PromoCategory);
            _categoryCombo.SelectedIndex = catIdx >= 0 ? catIdx : 0;

            var typeIdx = _discountTypeCombo.Items.IndexOf(_existing.DiscountType);
            _discountTypeCombo.SelectedIndex = typeIdx >= 0 ? typeIdx : 0;

            _discountValueNud.Value = _existing.DiscountValue > 0 ? _existing.DiscountValue : 0.01m;

            // Stashed for LoadProductsAsync to apply once the combos have items — they're
            // empty at this point since the catalog fetch hasn't completed yet.
            _pendingRequiredProductId = _existing.RequiredProductId;
            _pendingRewardProductId = _existing.RewardProductId;

            var tierIdx = _targetTierCombo.Items.IndexOf(_existing.TargetTier);
            _targetTierCombo.SelectedIndex = tierIdx >= 0 ? tierIdx : 0;

            if (_existing.MinPurchaseAmount.HasValue)
            {
                _hasMinPurchaseChk.Checked = true;
                _minPurchaseNud.Value = _existing.MinPurchaseAmount.Value;
                _minPurchaseNud.Enabled = true;
            }
            else
            {
                _hasMinPurchaseChk.Checked = false;
                _minPurchaseNud.Value = 0m;
                _minPurchaseNud.Enabled = false;
            }

            if (_existing.MaxDiscountAmount.HasValue)
            {
                _hasMaxDiscountChk.Checked = true;
                _maxDiscountNud.Value = _existing.MaxDiscountAmount.Value;
                _maxDiscountNud.Enabled = true;
            }
            else
            {
                _hasMaxDiscountChk.Checked = false;
                _maxDiscountNud.Value = 0m;
                _maxDiscountNud.Enabled = false;
            }

            _maxRedemptionsNud.Value = _existing.MaxRedemptionsPerCustomer;
            _startDatePicker.Value = _existing.StartDate;
            _endDatePicker.Value = _existing.EndDate;
        }

        private void RefreshViewPanel()
        {
            if (_existing is null) return;

            _viewPanel.Controls.Clear();

            const int labelX = 18;
            const int valueX = 180;
            int y = 14;

            void AddRow(string label, string value, Font? valueFont = null, Color? valueColor = null)
            {
                _viewPanel.Controls.Add(new Label
                {
                    Text = label,
                    Location = new Point(labelX, y),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontRegular(9f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                });

                _viewPanel.Controls.Add(new Label
                {
                    Text = value,
                    Location = new Point(valueX, y),
                    AutoSize = true,
                    Font = valueFont ?? BrandPalette.BrandFontRegular(9.5f),
                    ForeColor = valueColor ?? BrandPalette.NeutralText,
                });

                y += 26;
            }

            AddRow("Promo name", _existing.PromoName, BrandPalette.BrandFontBold(11f));
            AddRow("Category", _existing.PromoCategory);
            AddRow("Discount", FormatDiscount(_existing.DiscountType, _existing.DiscountValue),
                BrandPalette.BrandFontBold(11f), BrandPalette.Brand);

            if (_existing.DiscountType.Equals("B1T1", StringComparison.OrdinalIgnoreCase))
            {
                AddRow("Buy", _existing.RequiredProductName ?? $"Product #{_existing.RequiredProductId}");
                AddRow("Get free", _existing.RewardProductName ?? $"Product #{_existing.RewardProductId}");
            }

            AddRow("Target tier", _existing.TargetTier);
            AddRow("Min purchase", _existing.MinPurchaseAmount.HasValue ? $"₱{_existing.MinPurchaseAmount.Value:0.00}" : "—");
            AddRow("Max discount", _existing.MaxDiscountAmount.HasValue ? $"₱{_existing.MaxDiscountAmount.Value:0.00}" : "—");
            AddRow("Max redemptions", $"{_existing.MaxRedemptionsPerCustomer} per customer");
            AddRow("Start", _existing.StartDate.ToString("MMMM d, yyyy"));
            AddRow("End", _existing.EndDate.ToString("MMMM d, yyyy"));

            var statusColor = _existing.Status?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true
                ? BrandPalette.StatusPositiveText
                : BrandPalette.NeutralTextMuted;
            AddRow("Status", _existing.Status ?? "—", BrandPalette.BrandFontBold(9.5f), statusColor);
        }

        private static string FormatDiscount(string type, decimal value) => type switch
        {
            _ when type.Equals("Percentage", StringComparison.OrdinalIgnoreCase) => $"{value:0.##}% off",
            _ when type.Equals("B1T1", StringComparison.OrdinalIgnoreCase) => "Buy 1, Take 1",
            _ => $"₱{value:0.##} off",
        };

        // =====================================================================
        // ACTIONS
        // =====================================================================

        private async void PrimaryBtn_Click(object? sender, EventArgs e)
        {
            _errorLabel.Text = string.Empty;

            // Client-side validation before hitting the API (matches server rules).
            if (string.IsNullOrWhiteSpace(_nameTxtBx.Text))
            {
                _errorLabel.Text = "Promo name is required.";
                return;
            }

            if (_discountTypeCombo.SelectedItem is not string discountType)
            {
                _errorLabel.Text = "Discount type is required.";
                return;
            }

            var isB1T1 = discountType.Equals("B1T1", StringComparison.OrdinalIgnoreCase);
            int? requiredProductId = null;
            int? rewardProductId = null;
            decimal discountValue;

            if (isB1T1)
            {
                requiredProductId = _requiredProductCombo.SelectedValue as int?;
                rewardProductId = _rewardProductCombo.SelectedValue as int?;

                if (requiredProductId is null || rewardProductId is null)
                {
                    _errorLabel.Text = "Choose both the required product and the free product for this B1T1 promo.";
                    return;
                }

                discountValue = 0m;
            }
            else
            {
                discountValue = _discountValueNud.Value;
                if (discountValue <= 0)
                {
                    _errorLabel.Text = "Discount value must be greater than zero.";
                    return;
                }

                if (discountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase) && discountValue > 100)
                {
                    _errorLabel.Text = "Percentage discount cannot exceed 100.";
                    return;
                }
            }

            if (_endDatePicker.Value <= _startDatePicker.Value)
            {
                _errorLabel.Text = "End date must be after start date.";
                return;
            }

            var employeeId = EmployeeSession.Current?.EmployeeId
                ?? throw new InvalidOperationException("No employee session.");

            try
            {
                _primaryBtn.Enabled = false;

                var minPurchase = _hasMinPurchaseChk.Checked ? _minPurchaseNud.Value : (decimal?)null;
                var maxDiscount = _hasMaxDiscountChk.Checked ? _maxDiscountNud.Value : (decimal?)null;

                if (_mode == Mode.Create)
                {
                    await _controller.CreateAsync(
                        _nameTxtBx.Text, _categoryCombo.SelectedItem as string ?? PromoCategories.Seasonal,
                        discountType, discountValue,
                        _targetTierCombo.SelectedItem as string ?? "All",
                        minPurchase, maxDiscount,
                        (int)_maxRedemptionsNud.Value,
                        _startDatePicker.Value.Date, _endDatePicker.Value.Date,
                        employeeId,
                        requiredProductId, rewardProductId);

                    ChangedSomething = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else // Mode.Edit
                {
                    if (_existing is null) return;

                    await _controller.UpdateAsync(
                        _existing.CampaignId,
                        _nameTxtBx.Text, _categoryCombo.SelectedItem as string ?? PromoCategories.Seasonal,
                        discountType, discountValue,
                        _targetTierCombo.SelectedItem as string ?? "All",
                        minPurchase, maxDiscount,
                        (int)_maxRedemptionsNud.Value,
                        _startDatePicker.Value.Date, _endDatePicker.Value.Date,
                        employeeId,
                        requiredProductId, rewardProductId);

                    ChangedSomething = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                _errorLabel.Text = ex.Message;
            }
            finally
            {
                _primaryBtn.Enabled = true;
            }
        }

        private void CancelBtn_Click(object? sender, EventArgs e)
        {
            // Cancel from Create: close without saving.
            // Cancel from Edit: go back to View (not close) — user was looking at a campaign.
            if (_mode == Mode.Edit && _existing is not null)
            {
                _mode = Mode.View;
                ApplyMode();
            }
            else
            {
                DialogResult = ChangedSomething ? DialogResult.OK : DialogResult.Cancel;
                Close();
            }
        }

        private async void ToggleActivation(bool activate)
        {
            if (_existing is null) return;

            var verb = activate ? "activate" : "deactivate";
            var confirm = MessageBox.Show(this,
                $"Are you sure you want to {verb} \"{_existing.PromoName}\"?",
                activate ? "Activate Campaign" : "Deactivate Campaign",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _activateBtn.Enabled = false;
                _deactivateBtn.Enabled = false;

                var employeeId = EmployeeSession.Current?.EmployeeId
                    ?? throw new InvalidOperationException("No employee session.");

                if (activate)
                    await _controller.ActivateAsync(_existing.CampaignId, employeeId);
                else
                    await _controller.DeactivateAsync(_existing.CampaignId, employeeId);

                ChangedSomething = true;

                // Close the modal — the caller will reload the grid, so the card
                // reflects the new status. Keeping the modal open with a stale view
                // is more confusing than just closing.
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _errorLabel.Text = ex.Message;
                _activateBtn.Enabled = true;
                _deactivateBtn.Enabled = true;
            }
        }
    }
}