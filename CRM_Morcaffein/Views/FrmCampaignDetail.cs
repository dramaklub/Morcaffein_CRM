using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.winforms.Services;
using CRM_Morcaffein.winforms.Services.Api;

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
    /// Branch scoping: an all-branch Manager sees a Branch dropdown and can choose
    /// "All Branches" (company-wide campaign) or a specific branch. A pinned Manager
    /// doesn't see the dropdown — their campaign is always scoped to their own branch,
    /// enforced server-side too.
    ///
    /// The modal is code-only (no Designer.cs), matching FrmCreateEmployee.
    /// </summary>
    public class FrmCampaignDetail : Form
    {
        private enum Mode { Create, View, Edit }

        private readonly CampaignService _controller;
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

        // Branch selector — visible only for all-branch Managers.
        private Label _branchLabel = null!;
        private ComboBox _branchCombo = null!;
        private readonly List<BranchItem> _branchesCache = new();
        private bool _branchesLoaded;

        private NumericUpDown _minPurchaseNud = null!;
        private CheckBox _hasMinPurchaseChk = null!;
        private NumericUpDown _maxDiscountNud = null!;
        private CheckBox _hasMaxDiscountChk = null!;
        private NumericUpDown _maxRedemptionsNud = null!;
        private DateTimePicker _startDatePicker = null!;
        private DateTimePicker _endDatePicker = null!;
        private TextBox _reasonTxtBx = null!;
        private TextBox _descriptionTxtBx = null!;
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

        private FrmCampaignDetail(CampaignService controller, CampaignResponseDto? existing, Mode initialMode)
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
            ClientSize = new Size(600, 800);
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
            UpdateBranchFieldVisibility();

            _ = LoadProductsAsync();
            _ = LoadBranchesAsync();

            Load += (s, e) =>
            {
                PositionActionButtons(_actionRow);
                if (_mode == Mode.View) RefreshViewPanel();
            };
        }

        // =====================================================================
        // FACTORY METHODS — how the caller opens the modal
        // =====================================================================

        public static FrmCampaignDetail ForCreate(CampaignService controller)
            => new(controller, existing: null, Mode.Create);

        public static FrmCampaignDetail ForView(CampaignService controller, CampaignResponseDto campaign)
            => new(controller, campaign, Mode.View);

        // =====================================================================
        // LAYOUT
        // =====================================================================

        private void BuildLayout()
        {
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

            _viewPanel = BuildViewPanel();
            _formPanel = BuildFormPanel();

            _errorLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = BrandPalette.SurfaceCard,
            };

            _actionRow = BuildActionRow();

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
                BackColor = BrandPalette.SurfaceCard,
                Padding = new Padding(18, 14, 18, 14),
                Margin = new Padding(0, 0, 0, 12),
                Visible = false,
            };
            return panel;
        }

        private Panel BuildFormPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                Visible = false,
                BackColor = BrandPalette.SurfaceCard,
                Margin = new Padding(0, 0, 0, 12),
            };

            const int labelH = 18;
            const int fieldH = 30;
            const int rowH = 58;
            const int fieldW = 464;
            const int PAD = 40;
            const int COL2 = PAD + 244;

            int y = 0;

            void AddLabel(string text, int x = PAD)
            {
                panel.Controls.Add(new Label
                {
                    Text = text,
                    Location = new Point(x, y),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontRegular(9f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                });
            }

            // ---- Promo name ----
            AddLabel("Promo name *");
            _nameTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(fieldW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            panel.Controls.Add(_nameTxtBx);
            y += rowH;

            // ---- Category ----
            AddLabel("Category *");
            _categoryCombo = new ComboBox
            {
                Location = new Point(PAD, y + labelH),
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

            // ---- Discount type + value (2-up) ----
            AddLabel("Discount type *", PAD);
            _discountTypeCombo = new ComboBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(220, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _discountTypeCombo.Items.AddRange(new object[] { "Percentage", "FixedAmount", "B1T1" });
            _discountTypeCombo.SelectedIndex = 0;
            _discountTypeCombo.SelectedIndexChanged += (s, e) => UpdateB1T1FieldsVisibility();
            panel.Controls.Add(_discountTypeCombo);

            AddLabel("Discount value *", COL2);
            _discountValueLabel = panel.Controls.OfType<Label>()
                .Last(l => l.Text == "Discount value *");

            _discountValueNud = new NumericUpDown
            {
                Location = new Point(COL2, y + labelH),
                Size = new Size(220, fieldH),
                DecimalPlaces = 2,
                Minimum = 0.01m,
                Maximum = 100000m,
                Value = 10m,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            panel.Controls.Add(_discountValueNud);
            y += rowH;

            // ---- B1T1 product pickers (2-up, hidden unless Discount type = B1T1) ----
            _requiredProductLabel = new Label
            {
                Text = "Buy this product *",
                Location = new Point(PAD, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            panel.Controls.Add(_requiredProductLabel);
            _requiredProductCombo = new ComboBox
            {
                Location = new Point(PAD, y + labelH),
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
                Location = new Point(COL2, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            panel.Controls.Add(_rewardProductLabel);
            _rewardProductCombo = new ComboBox
            {
                Location = new Point(COL2, y + labelH),
                Size = new Size(220, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "ProductName",
                ValueMember = "ProductId",
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            panel.Controls.Add(_rewardProductCombo);
            y += rowH;

            // ---- Target tier ----
            AddLabel("Target tier *");
            _targetTierCombo = new ComboBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(fieldW, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _targetTierCombo.Items.AddRange(new object[] { "All", "Regular", "Silver", "Gold" });
            _targetTierCombo.SelectedIndex = 0;
            panel.Controls.Add(_targetTierCombo);
            y += rowH;

            // ---- Branch (all-branch Managers only) ----
            _branchLabel = new Label
            {
                Text = "Branch *",
                Location = new Point(PAD, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            panel.Controls.Add(_branchLabel);

            _branchCombo = new ComboBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(fieldW, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
                DisplayMember = "Display",
            };
            panel.Controls.Add(_branchCombo);
            y += rowH;

            // ---- Min purchase / Max discount (2-up checkboxes + nud) ----
            _hasMinPurchaseChk = new CheckBox
            {
                Text = "Minimum purchase amount",
                Location = new Point(PAD, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _minPurchaseNud = new NumericUpDown
            {
                Location = new Point(PAD, y + labelH + 4),
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

            _hasMaxDiscountChk = new CheckBox
            {
                Text = "Maximum discount cap",
                Location = new Point(COL2, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _maxDiscountNud = new NumericUpDown
            {
                Location = new Point(COL2, y + labelH + 4),
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

            // ---- Max redemptions ----
            AddLabel("Max redemptions per customer *");
            _maxRedemptionsNud = new NumericUpDown
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(220, fieldH),
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            panel.Controls.Add(_maxRedemptionsNud);
            y += rowH;

            // ---- Reason ----
            AddLabel("Reason *");
            _reasonTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(fieldW, 60),
                Multiline = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 250,
                PlaceholderText = "E.g. Clearing old matcha stock, Q3 revenue push, response to customer feedback",
            };
            panel.Controls.Add(_reasonTxtBx);
            y += 76;

            // ---- Description (customer-facing message) ----
            AddLabel("Message to customers * (max 500 chars)");
            _descriptionTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(fieldW, 60),
                Multiline = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 500,
                PlaceholderText = "What the customer will read in the email — warm, short, first-person from the shop.",
            };
            panel.Controls.Add(_descriptionTxtBx);
            y += 76;

            // ---- Start / End dates (2-up) ----
            AddLabel("Start date *", PAD);
            _startDatePicker = new DateTimePicker
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(220, fieldH),
                Format = DateTimePickerFormat.Short,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Value = DateTime.Today,
            };
            panel.Controls.Add(_startDatePicker);

            AddLabel("End date *", COL2);
            _endDatePicker = new DateTimePicker
            {
                Location = new Point(COL2, y + labelH),
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
        // BRANCH HANDLING
        // =====================================================================

        /// <summary>
        /// True if the current employee can pick a branch for their campaigns.
        /// An all-branch Manager (Employee.BranchId == null) can. A pinned
        /// Manager's campaigns are always their own branch — no choice offered.
        /// </summary>
        private bool CanChooseBranch =>
            EmployeeSession.Current?.Employee?.BranchId is null;

        /// <summary>
        /// Shows or hides the branch row depending on whether the current user
        /// can choose a branch. A pinned Manager's branch row is hidden entirely
        /// (rather than disabled) — there's nothing to explain.
        /// </summary>
        private void UpdateBranchFieldVisibility()
        {
            if (_branchLabel is null || _branchCombo is null) return;

            var canChoose = CanChooseBranch;
            _branchLabel.Visible = canChoose;
            _branchCombo.Visible = canChoose;
        }

        private async Task LoadBranchesAsync()
        {
            // Nothing to load if the user can't choose.
            if (!CanChooseBranch) return;

            try
            {
                var api = new BranchApi();
                var branches = await api.GetBranchesAsync();

                _branchesCache.Clear();
                // "All Branches" first — the company-wide option (BranchId = null).
                _branchesCache.Add(new BranchItem(null, "All Branches"));
                foreach (var b in branches)
                    _branchesCache.Add(new BranchItem(b.BranchId, b.BranchName));

                _branchCombo.Items.Clear();
                foreach (var b in _branchesCache)
                    _branchCombo.Items.Add(b);

                // Default or preserve selection. In Create mode, "All Branches" is
                // index 0. In View/Edit mode, the existing campaign's branch is
                // preselected by PopulateFromExisting, but that runs before the
                // async load, so we reapply here.
                if (_mode == Mode.Create)
                {
                    _branchCombo.SelectedIndex = 0;
                }
                else if (_existing is not null)
                {
                    SelectBranchById(_existing.BranchId);
                }
                else if (_branchCombo.Items.Count > 0)
                {
                    _branchCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _errorLabel.Text = $"Could not load branches: {ex.Message}";
            }
        }

        private void SelectBranchById(int? branchId)
        {
            for (int i = 0; i < _branchCombo.Items.Count; i++)
            {
                if (_branchCombo.Items[i] is BranchItem bi && bi.BranchId == branchId)
                {
                    _branchCombo.SelectedIndex = i;
                    return;
                }
            }
        }

        /// <summary>
        /// Wrapper so the combo can hold both "All Branches" (BranchId = null)
        /// and specific branches (BranchId = N) without nullable-binding pain.
        /// </summary>
        private sealed class BranchItem
        {
            public int? BranchId { get; }
            public string Display { get; }
            public BranchItem(int? id, string display) { BranchId = id; Display = display; }
            public override string ToString() => Display;
        }

        // =====================================================================
        // MODE
        // =====================================================================

        private void ApplyMode()
        {
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

                    var isActive = _existing?.Status?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true;
                    _activateBtn.Visible = !isActive;
                    _deactivateBtn.Visible = isActive;
                    break;
            }

            PositionActionButtons(_actionRow);
        }

        private void SwitchToEdit()
        {
            _mode = Mode.Edit;
            PopulateFromExisting();
            ApplyMode();
            UpdateBranchFieldVisibility();
        }

        // =====================================================================
        // DATA BINDING
        // =====================================================================

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
            _reasonTxtBx.Text = string.Empty;
            _descriptionTxtBx.Text = string.Empty;
            _categoryCombo.SelectedIndex = 0;
            _discountTypeCombo.SelectedIndex = 0;
            _discountValueNud.Value = 10m;
            _pendingRequiredProductId = null;
            _pendingRewardProductId = null;
            _targetTierCombo.SelectedIndex = 0;

            // Branch defaults to "All Branches" (index 0) once LoadBranchesAsync
            // populates the combo. If the user is pinned, the row is hidden and
            // the branch is resolved from their employee record at save time.

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
            _reasonTxtBx.Text = _existing.Reason ?? string.Empty;
            _descriptionTxtBx.Text = _existing.Description ?? string.Empty;

            var catIdx = _categoryCombo.Items.IndexOf(_existing.PromoCategory);
            _categoryCombo.SelectedIndex = catIdx >= 0 ? catIdx : 0;

            var typeIdx = _discountTypeCombo.Items.IndexOf(_existing.DiscountType);
            _discountTypeCombo.SelectedIndex = typeIdx >= 0 ? typeIdx : 0;

            _discountValueNud.Value = _existing.DiscountValue > 0 ? _existing.DiscountValue : 0.01m;

            _pendingRequiredProductId = _existing.RequiredProductId;
            _pendingRewardProductId = _existing.RewardProductId;

            var tierIdx = _targetTierCombo.Items.IndexOf(_existing.TargetTier);
            _targetTierCombo.SelectedIndex = tierIdx >= 0 ? tierIdx : 0;

            // Preselect the campaign's existing branch. If the combo hasn't been
            // populated yet (async load hasn't returned), LoadBranchesAsync will
            // apply this selection once items exist.
            if (CanChooseBranch && _branchCombo.Items.Count > 0)
                SelectBranchById(_existing.BranchId);

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
            AddRow("Reason", string.IsNullOrWhiteSpace(_existing.Reason) ? "—" : _existing.Reason);
            AddRow("Message", string.IsNullOrWhiteSpace(_existing.Description) ? "—" : _existing.Description);
            AddRow("Discount", FormatDiscount(_existing.DiscountType, _existing.DiscountValue),
                BrandPalette.BrandFontBold(11f), BrandPalette.Brand);

            if (_existing.DiscountType.Equals("B1T1", StringComparison.OrdinalIgnoreCase))
            {
                AddRow("Buy", _existing.RequiredProductName ?? $"Product #{_existing.RequiredProductId}");
                AddRow("Get free", _existing.RewardProductName ?? $"Product #{_existing.RewardProductId}");
            }

            AddRow("Target tier", _existing.TargetTier);

            // Branch row — resolved server-side ("All Branches" if null).
            var branchDisplay = string.IsNullOrWhiteSpace(_existing.BranchName)
                ? "All Branches"
                : _existing.BranchName;
            AddRow("Branch", branchDisplay,
                _existing.BranchId is null ? null : BrandPalette.BrandFontBold(9.5f),
                _existing.BranchId is null ? BrandPalette.NeutralTextMuted : (Color?)null);

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

            if (string.IsNullOrWhiteSpace(_nameTxtBx.Text))
            {
                _errorLabel.Text = "Promo name is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_reasonTxtBx.Text))
            {
                _errorLabel.Text = "A reason for this campaign is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_descriptionTxtBx.Text))
            {
                _errorLabel.Text = "A message for customers is required.";
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

            // ---- Branch resolution ----
            // All-branch Manager: read from the dropdown (null = company-wide).
            // Pinned Manager: use their own branch — dropdown is hidden.
            int? branchId;
            if (CanChooseBranch)
            {
                if (_branchCombo.SelectedItem is not BranchItem selectedBranch)
                {
                    _errorLabel.Text = "Please choose a branch (or All Branches).";
                    return;
                }
                branchId = selectedBranch.BranchId;
            }
            else
            {
                branchId = EmployeeSession.Current?.Employee?.BranchId;
                if (branchId is null)
                {
                    _errorLabel.Text = "Could not resolve your branch. Please log in again.";
                    return;
                }
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
                        _reasonTxtBx.Text,
                        _descriptionTxtBx.Text,
                        discountType, discountValue,
                        _targetTierCombo.SelectedItem as string ?? "All",
                        minPurchase, maxDiscount,
                        (int)_maxRedemptionsNud.Value,
                        _startDatePicker.Value.Date, _endDatePicker.Value.Date,
                        employeeId,
                        branchId,
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
                        _reasonTxtBx.Text,
                        _descriptionTxtBx.Text,
                        discountType, discountValue,
                        _targetTierCombo.SelectedItem as string ?? "All",
                        minPurchase, maxDiscount,
                        (int)_maxRedemptionsNud.Value,
                        _startDatePicker.Value.Date, _endDatePicker.Value.Date,
                        employeeId,
                        branchId,
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