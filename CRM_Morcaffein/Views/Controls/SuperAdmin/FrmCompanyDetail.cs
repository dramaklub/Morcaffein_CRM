using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Single modal that shows a company's details and current subscription, and
    /// also hosts the plan-assignment UI as an in-place mode swap (View / Assign).
    /// No sub-modal — the "Change Plan" button just toggles which content panel
    /// is visible inside this same form.
    ///
    /// Layout is fixed at 640×620. Both modes live in a content host that starts
    /// at y=96; the Close button stays pinned bottom-right regardless of mode.
    /// </summary>
    public class FrmCompanyDetail : Form
    {
        private enum Mode { View, Assign }

        private readonly SuperAdminApi _api;
        private readonly SuperAdminCompanyDto _company;

        // ---- Fixed chrome ----
        private Panel _contentHost = null!;
        private Button _closeBtn = null!;

        // ---- View mode widgets ----
        private Panel _viewPanel = null!;
        private Panel _viewScrollHost = null!;
        private Label _companyNameLabel = null!;
        private Label _companyCodeLabel = null!;
        private Label _companyStatusLabel = null!;
        private Panel _currentCard = null!;
        private Label _currentPlanLabel = null!;
        private Label _currentPriceLabel = null!;
        private Label _currentModulesLabel = null!;
        private Label _currentSinceLabel = null!;
        private Label _currentAgreedByLabel = null!;
        private Button _changePlanBtn = null!;
        private DataGridView _historyGrid = null!;
        private Label _historyEmptyLabel = null!;

        // Admin account section (added between the subscription card and the
        // history grid). Populated from GET /companies/{id}/admin.
        private Panel _adminCard = null!;
        private Label _adminNameLabel = null!;
        private Label _adminUsernameLabel = null!;
        private Label _adminContactLabel = null!;
        private Label _adminStatusLabel = null!;
        private Button _adminActionBtn = null!;
        private SuperAdminAdminAccountDto? _adminAccount;

        // ---- Assign mode widgets ----
        private Panel _assignPanel = null!;
        private Label _assignCompanyLabel = null!;
        private FlowLayoutPanel _planOptionsHost = null!;
        private readonly List<RadioButton> _planRadios = new();
        private readonly List<int> _planIdForRadio = new();
        private TextBox _agreedByTxtBx = null!;
        private Label _assignErrorLabel = null!;
        private Button _assignBtn = null!;
        private Button _assignCancelBtn = null!;

        // ---- Data ----
        private List<SuperAdminPlanDto> _plans = new();
        private List<SubscriptionResponseDto> _subscriptions = new();
        private Mode _mode = Mode.View;

        // ---- Layout constants ----
        private const int OuterPad = 24;
        private const int ContentTop = 96;
        private const int GapSmall = 12;
        private const int GapMedium = 20;

        public FrmCompanyDetail(SuperAdminApi api, SuperAdminCompanyDto company)
        {
            _api = api;
            _company = company;

            Text = $"Company — {company.CompanyName}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(680, 720);
            BackColor = BrandPalette.SurfaceCard;
            Font = BrandPalette.BrandFontRegular(9.5f);

            BuildChrome();
            BuildViewPanel();
            BuildAssignPanel();

            ApplyMode();

            Load += async (s, e) => await ReloadAsync();
        }

        // =====================================================================
        // CHROME — outer padding host + Close button
        // =====================================================================

        // =====================================================================
        // CHROME — content host with padding + bottom bar with Close button
        // =====================================================================

        private void BuildChrome()
        {
            // Bottom bar first (docks under everything else)
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                BackColor = BrandPalette.SurfaceCard,
                Padding = new Padding(OuterPad, 12, OuterPad, 12),
            };

            _closeBtn = new Button
            {
                Text = "Close",
                Size = new Size(120, 34),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _closeBtn.FlatAppearance.BorderSize = 0;
            _closeBtn.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            bottomBar.Controls.Add(_closeBtn);

            // Content host fills the rest, and its Padding pushes every child
            // in by OuterPad on all sides. Every mode panel docks Fill inside
            // it, so they automatically respect the same inset — no per-panel
            // absolute-position math, no double-counted offsets.
            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceCard,
                Padding = new Padding(OuterPad),
            };

            Controls.Add(_contentHost);
            Controls.Add(bottomBar);

            CancelButton = _closeBtn;
        }

        // =====================================================================
        // VIEW MODE PANEL — docked Fill inside _contentHost
        // =====================================================================

        private void BuildViewPanel()
        {
            // The view panel itself is tall — company header, subscription card,
            // admin account card, subscription history grid. On small windows it
            // overflows. Put it inside an AutoScroll host so the user can reach
            // the bottom. _viewPanel is NOT docked Fill inside the host — it uses
            // Dock = Top with explicit height so the host's scrollbar appears
            // when the content is taller than the visible area.
            _viewPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 900,   // generous; will be shorter than the host on most windows
                BackColor = BrandPalette.SurfaceCard,
            };

            // Wrap it in a scrollable host.
            var viewScrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BrandPalette.SurfaceCard,
            };
            viewScrollHost.Controls.Add(_viewPanel);
            _viewScrollHost = viewScrollHost;

            // Layout strategy: a fixed header strip at the top, a fixed "current
            // subscription" block below it, then a history grid that fills the
            // remaining space. All paddings are handled by the panel's own
            // inner margins rather than absolute positions.

            const int cardPad = 16;

            // ---- Top strip: company name + code + status ----
            var topStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.Transparent,
            };

            _companyNameLabel = new Label
            {
                Text = _company.CompanyName,
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(16f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };
            _companyCodeLabel = new Label
            {
                Text = $"Code: {_company.CompanyCode}",
                Location = new Point(0, 30),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };
            _companyStatusLabel = new Label
            {
                Text = _company.IsActive ? "Active" : "Inactive",
                Location = new Point(200, 30),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(9f),
                ForeColor = _company.IsActive
                    ? BrandPalette.StatusPositiveText
                    : BrandPalette.StatusNegativeFill,
                BackColor = Color.Transparent,
            };
            topStrip.Controls.Add(_companyNameLabel);
            topStrip.Controls.Add(_companyCodeLabel);
            topStrip.Controls.Add(_companyStatusLabel);

            // ---- Current subscription section ----
            var currentSection = new Panel
            {
                Dock = DockStyle.Top,
                Height = 220,
                BackColor = Color.Transparent,
            };

            var currentHeader = new Label
            {
                Text = "Current Subscription",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };
            currentSection.Controls.Add(currentHeader);

            _currentCard = new Panel
            {
                Location = new Point(0, 26),
                Size = new Size(100, 176),   // width set in Resize
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            _currentPlanLabel = new Label
            {
                Location = new Point(cardPad, cardPad),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(13f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
            };

            _currentPriceLabel = new Label
            {
                Location = new Point(0, cardPad + 2),
                Size = new Size(200, 20),
                Font = BrandPalette.BrandFontBold(10f),
                ForeColor = BrandPalette.NeutralText,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };

            _currentModulesLabel = new Label
            {
                Location = new Point(cardPad, cardPad + 30),
                Size = new Size(100, 40),
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            _currentSinceLabel = new Label
            {
                Location = new Point(cardPad, cardPad + 78),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            _currentAgreedByLabel = new Label
            {
                Location = new Point(cardPad, cardPad + 98),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            _changePlanBtn = new Button
            {
                Location = new Point(cardPad, cardPad + 124),
                Size = new Size(140, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _changePlanBtn.FlatAppearance.BorderSize = 0;
            _changePlanBtn.Click += (s, e) => SwitchToAssignMode();

            _currentCard.Controls.Add(_currentPlanLabel);
            _currentCard.Controls.Add(_currentPriceLabel);
            _currentCard.Controls.Add(_currentModulesLabel);
            _currentCard.Controls.Add(_currentSinceLabel);
            _currentCard.Controls.Add(_currentAgreedByLabel);
            _currentCard.Controls.Add(_changePlanBtn);

            currentSection.Controls.Add(_currentCard);

            // ---- Admin Account section (fixed height, docked Top) ----
            var adminSection = new Panel
            {
                Dock = DockStyle.Top,
                Height = 200,
                BackColor = Color.Transparent,
            };

            var adminHeader = new Label
            {
                Text = "Admin Account",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };
            adminSection.Controls.Add(adminHeader);

            _adminCard = new Panel
            {
                Location = new Point(0, 26),
                Size = new Size(100, 160),   // width set on SizeChanged
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            adminSection.Controls.Add(_adminCard);

            const int admPad = 16;

            _adminNameLabel = new Label
            {
                Location = new Point(admPad, admPad),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(12f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                Text = "—",
            };

            _adminUsernameLabel = new Label
            {
                Location = new Point(admPad, admPad + 26),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
                Text = "—",
            };

            _adminContactLabel = new Label
            {
                Location = new Point(admPad, admPad + 48),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Text = "—",
            };

            _adminStatusLabel = new Label
            {
                Location = new Point(admPad, admPad + 70),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Text = "—",
            };

            _adminActionBtn = new Button
            {
                Location = new Point(admPad, admPad + 104),
                Size = new Size(180, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Text = "Edit Credentials",
            };
            _adminActionBtn.FlatAppearance.BorderSize = 0;
            _adminActionBtn.Click += AdminActionBtn_Click;

            _adminCard.Controls.Add(_adminActionBtn);
            _adminCard.Controls.Add(_adminStatusLabel);
            _adminCard.Controls.Add(_adminContactLabel);
            _adminCard.Controls.Add(_adminUsernameLabel);
            _adminCard.Controls.Add(_adminNameLabel);

            // Keep the card stretched to the section's width.
            adminSection.SizeChanged += (s, e) =>
            {
                _adminCard.Width = adminSection.Width;
            };

            // ---- History section (fills remaining space) ----
            var historySection = new Panel
            {
                Dock = DockStyle.Top,
                Height = 320,   // fits ~8 rows at 32px + header; scrollbar appears inside the grid if needed
                BackColor = Color.Transparent,
            };

            var historyHeader = new Label
            {
                Text = "Subscription History",
                Dock = DockStyle.Top,
                Height = 26,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            _historyGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = BrandPalette.Border,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                Font = BrandPalette.BrandFontRegular(9f),
                ColumnHeadersHeight = 32,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowTemplate = { Height = 28 },
            };

            _historyGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BrandPalette.HeaderBackground,
                ForeColor = BrandPalette.HeaderText,
                Font = BrandPalette.BrandFontBold(9f),
                SelectionBackColor = BrandPalette.HeaderBackground,
                SelectionForeColor = BrandPalette.HeaderText,
                Padding = new Padding(8, 4, 8, 4),
            };
            _historyGrid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BrandPalette.SurfacePrimary,
                ForeColor = BrandPalette.NeutralText,
                Padding = new Padding(8, 4, 8, 4),
                SelectionBackColor = BrandPalette.RowSelected,
                SelectionForeColor = BrandPalette.NeutralText,
            };

            _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PlanName",
                HeaderText = "Plan",
                FillWeight = 20,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = BrandPalette.BrandFontBold(9f),
                    ForeColor = BrandPalette.Brand,
                },
            });
            _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Period",
                HeaderText = "Period",
                FillWeight = 46,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Price",
                HeaderText = "Price",
                FillWeight = 16,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                },
            });
            _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AgreedBy",
                HeaderText = "Agreed by",
                FillWeight = 24,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });

            _historyEmptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Loading…",
                Visible = false,
            };

            historySection.Controls.Add(_historyGrid);
            historySection.Controls.Add(_historyEmptyLabel);
            historySection.Controls.Add(historyHeader);

            // WinForms docks Top sections in reverse order of Add — so add
            // bottom-most first, topmost last. This produces: topStrip at the
            // very top, then currentSection, then adminSection, then
            // historySection at the bottom. Sum their heights + a small tail
            // padding to set _viewPanel.Height explicitly (AutoSize = false
            // inside the scroll host works more reliably with a fixed height).
            _viewPanel.Controls.Add(historySection);
            _viewPanel.Controls.Add(adminSection);
            _viewPanel.Controls.Add(currentSection);
            _viewPanel.Controls.Add(topStrip);

            _viewPanel.Height = topStrip.Height
                              + currentSection.Height
                              + adminSection.Height
                              + historySection.Height
                              + 16;   // tail padding

            // Fix the widths of the two controls that need to track the card's
            // width (Label sizes are anchored instead of Absolute; but the card
            // itself needs an explicit width since it's inside a Top-docked
            // section that doesn't auto-size its children).
            _currentCard.SizeChanged += (s, e) =>
            {
                _currentPriceLabel.Left = _currentCard.Width - _currentPriceLabel.Width - cardPad;
                _currentModulesLabel.Width = _currentCard.Width - cardPad * 2;
            };
            currentSection.SizeChanged += (s, e) =>
            {
                _currentCard.Width = currentSection.Width;
                _currentPriceLabel.Left = _currentCard.Width - _currentPriceLabel.Width - cardPad;
                _currentModulesLabel.Width = _currentCard.Width - cardPad * 2;
            };

            _contentHost.Controls.Add(_viewScrollHost);
        }

        // =====================================================================
        // ASSIGN MODE PANEL — docked Fill inside _contentHost
        // =====================================================================

        private void BuildAssignPanel()
        {
            _assignPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceCard,
            };

            // ---- Bottom bar for the two action buttons (Cancel / Assign) ----
            var assignBtnBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.Transparent,
            };

            _assignCancelBtn = new Button
            {
                Text = "Cancel",
                Size = new Size(120, 34),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
            };
            _assignCancelBtn.FlatAppearance.BorderSize = 0;
            _assignCancelBtn.Click += (s, e) => SwitchToViewMode();

            _assignBtn = new Button
            {
                Text = "Assign Plan",
                Size = new Size(140, 34),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0),
            };
            _assignBtn.FlatAppearance.BorderSize = 0;
            _assignBtn.Click += AssignBtn_Click;

            // WinForms docks in reverse-add order — add Cancel first, then Assign,
            // so Assign ends up to the LEFT of Cancel.
            assignBtnBar.Controls.Add(_assignCancelBtn);
            assignBtnBar.Controls.Add(_assignBtn);

            // ---- Top strip: heading + subtitle ----
            var topStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.Transparent,
            };

            var heading = new Label
            {
                Text = "Change Plan",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(16f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };
            _assignCompanyLabel = new Label
            {
                Text = $"Choose a new subscription plan for {_company.CompanyName}.",
                Location = new Point(0, 30),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };
            topStrip.Controls.Add(heading);
            topStrip.Controls.Add(_assignCompanyLabel);

            // ---- Bottom-of-content: agreed-by row + error label ----
            var agreedBySection = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 76,
                BackColor = Color.Transparent,
            };

            var agreedByLabel = new Label
            {
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Text = "Agreed by *",
            };

            _agreedByTxtBx = new TextBox
            {
                Location = new Point(0, 20),
                Size = new Size(100, 26),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = ApiConfig.SuperAdminName ?? "Platform Operator",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            _assignErrorLabel = new Label
            {
                Location = new Point(0, 50),
                Size = new Size(100, 20),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            agreedBySection.Controls.Add(agreedByLabel);
            agreedBySection.Controls.Add(_agreedByTxtBx);
            agreedBySection.Controls.Add(_assignErrorLabel);

            agreedBySection.SizeChanged += (s, e) =>
            {
                _agreedByTxtBx.Width = agreedBySection.Width;
                _assignErrorLabel.Width = agreedBySection.Width;
            };

            // ---- Plan options host (fills the middle) ----
            _planOptionsHost = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0),
            };

            // Compose: Fill first, then Bottom, then Top
            _assignPanel.Controls.Add(_planOptionsHost);
            _assignPanel.Controls.Add(agreedBySection);
            _assignPanel.Controls.Add(assignBtnBar);
            _assignPanel.Controls.Add(topStrip);

            _contentHost.Controls.Add(_assignPanel);
        }

        // =====================================================================
        // MODE SWITCHING
        // =====================================================================

        private void ApplyMode()
        {
            _contentHost.SuspendLayout();
            try
            {
                // No Clear here — both panels are permanent children of
                // _contentHost (added in BuildViewPanel and BuildAssignPanel).
                // Clearing removed them from the tree and the visibility
                // toggle below couldn't bring them back, which is why the
                // modal appeared empty.
                if (_mode == Mode.View)
                {
                    _viewScrollHost.Visible = true;
                    _assignPanel.Visible = false;
                    Text = $"Company — {_company.CompanyName}";
                }
                else
                {
                    _viewScrollHost.Visible = false;
                    _assignPanel.Visible = true;
                    Text = $"Change Plan — {_company.CompanyName}";
                }
            }
            finally
            {
                _contentHost.ResumeLayout(performLayout: true);
                _contentHost.PerformLayout();
            }
        }

        private void SwitchToAssignMode()
        {
            if (_plans.Count == 0)
            {
                MessageBox.Show(FindForm(),
                    "No plans available. Create plans before assigning.",
                    "Change Plan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BuildPlanOptions();
            _assignErrorLabel.Text = string.Empty;
            _mode = Mode.Assign;
            ApplyMode();
        }

        private void SwitchToViewMode()
        {
            _mode = Mode.View;
            ApplyMode();
        }

        // =====================================================================
        // PLAN PICKER — rebuilt every time Assign mode is entered
        // =====================================================================

        private void BuildPlanOptions()
        {
            _planOptionsHost.Controls.Clear();
            _planRadios.Clear();
            _planIdForRadio.Clear();

            var current = _subscriptions.FirstOrDefault(s => s.EndedAt is null);
            var currentPlanId = current?.PlanId;

            foreach (var plan in _plans)
            {
                var option = BuildPlanOption(plan, preChecked: plan.PlanId == currentPlanId);
                _planOptionsHost.Controls.Add(option);
            }

            if (_planRadios.Count > 0 && !_planRadios.Any(r => r.Checked))
                _planRadios[0].Checked = true;

            // Ensure the option widths match the host's real (post-layout) width.
            _planOptionsHost.Resize -= PlanOptionsHost_Resize;
            _planOptionsHost.Resize += PlanOptionsHost_Resize;
            PlanOptionsHost_Resize(this, EventArgs.Empty);
        }

        private void PlanOptionsHost_Resize(object? sender, EventArgs e)
        {
            // Available width = host width minus scrollbar allowance.
            var w = Math.Max(200, _planOptionsHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);

            foreach (Control c in _planOptionsHost.Controls)
            {
                c.Width = w;
                // Also adjust the inner labels that were sized against the old width
                foreach (Control child in c.Controls)
                {
                    if (child is Label lbl && lbl.Name == "planPriceLabel")
                        lbl.Left = c.Width - lbl.Width - 14;
                    if (child is Label modLbl && modLbl.Name == "planModulesLabel")
                        modLbl.Width = c.Width - 56;
                }
            }
        }

        private Panel BuildPlanOption(SuperAdminPlanDto plan, bool preChecked)
        {
            var option = new Panel
            {
                Width = 500,   // will be overridden by PlanOptionsHost_Resize
                Height = 90,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
            };

            var radio = new RadioButton
            {
                Location = new Point(14, 14),
                Size = new Size(20, 20),
                Checked = preChecked,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            _planRadios.Add(radio);
            _planIdForRadio.Add(plan.PlanId);

            var nameLabel = new Label
            {
                Name = "planNameLabel",
                Text = plan.Name,
                Location = new Point(42, 14),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(12f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };

            var priceLabel = new Label
            {
                Name = "planPriceLabel",
                Text = $"₱{plan.MonthlyPrice:N0} / month",
                Location = new Point(300, 16),
                Size = new Size(140, 20),
                Font = BrandPalette.BrandFontBold(10f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Cursor = Cursors.Hand,
            };

            var modulesLabel = new Label
            {
                Name = "planModulesLabel",
                Text = HumanizeModules(plan.ModulesCsv),
                Location = new Point(42, 40),
                Size = new Size(440, 40),
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };

            void SelectThis()
            {
                for (int i = 0; i < _planRadios.Count; i++)
                    _planRadios[i].Checked = (_planRadios[i] == radio);
            }

            option.Click += (s, e) => SelectThis();
            nameLabel.Click += (s, e) => SelectThis();
            priceLabel.Click += (s, e) => SelectThis();
            modulesLabel.Click += (s, e) => SelectThis();
            radio.Click += (s, e) => SelectThis();

            option.Controls.Add(radio);
            option.Controls.Add(nameLabel);
            option.Controls.Add(priceLabel);
            option.Controls.Add(modulesLabel);
            return option;
        }

        // =====================================================================
        // DATA LOAD
        // =====================================================================

        private async Task ReloadAsync()
        {
            try
            {
                var subsTask = _api.GetSubscriptionsAsync(_company.CompanyId);
                var plansTask = _api.GetPlansAsync();
                var adminTask = _api.GetAdminAsync(_company.CompanyId);

                await Task.WhenAll(subsTask, plansTask, adminTask);

                _subscriptions = await subsTask;
                _plans = await plansTask;
                _adminAccount = await adminTask;

                RefreshCurrentBlock();
                RefreshAdminSection();
                RefreshHistory();
            }
            catch (Exception ex)
            {
                _historyEmptyLabel.Text = $"Could not load: {ex.Message}";
                _historyEmptyLabel.Visible = true;
                _historyGrid.Visible = false;
            }
        }

        private void RefreshCurrentBlock()
        {
            var current = _subscriptions.FirstOrDefault(s => s.EndedAt is null);

            if (current is null)
            {
                _currentPlanLabel.Text = "No active plan";
                _currentPriceLabel.Text = string.Empty;
                _currentModulesLabel.Text = "This company has no subscription assigned yet.";
                _currentSinceLabel.Text = string.Empty;
                _currentAgreedByLabel.Text = string.Empty;
                _changePlanBtn.Text = "Assign Plan";
                return;
            }

            _currentPlanLabel.Text = current.PlanName;
            _currentPriceLabel.Text = $"₱{current.PlanMonthlyPrice:N0} / month";
            _currentModulesLabel.Text = HumanizeModules(current.PlanModulesCsv);
            _currentSinceLabel.Text = $"Since {current.StartedAt:MMMM d, yyyy}";
            _currentAgreedByLabel.Text = current.TermsAcceptedAt.HasValue
                ? $"T&C agreed by {current.TermsAcceptedBy} on {current.TermsAcceptedAt.Value:MMMM d, yyyy}"
                : string.Empty;
            _changePlanBtn.Text = "Change Plan";
        }

        private void RefreshHistory()
        {
            if (_subscriptions.Count == 0)
            {
                _historyGrid.Visible = false;
                _historyEmptyLabel.Text = "No subscription history yet.";
                _historyEmptyLabel.Visible = true;
                return;
            }

            _historyGrid.Visible = true;
            _historyEmptyLabel.Visible = false;

            _historyGrid.Rows.Clear();
            foreach (var s in _subscriptions)
            {
                var period = s.EndedAt.HasValue
                    ? $"{s.StartedAt:MMM d, yyyy} – {s.EndedAt.Value:MMM d, yyyy}"
                    : $"{s.StartedAt:MMM d, yyyy} – present";

                _historyGrid.Rows.Add(
                    s.PlanName,
                    period,
                    $"₱{s.PlanMonthlyPrice:N0}/mo",
                    s.TermsAcceptedBy ?? "—");
            }

            _historyGrid.ClearSelection();
        }

        private void RefreshAdminSection()
        {
            if (_adminAccount is null || !_adminAccount.HasAdmin)
            {
                _adminNameLabel.Text = "No Admin assigned";
                _adminNameLabel.ForeColor = BrandPalette.NeutralTextMuted;
                _adminUsernameLabel.Text = "—";
                _adminContactLabel.Text = "This company has no login credentials yet.";
                _adminStatusLabel.Text = "";
                _adminActionBtn.Text = "Create Admin Account";
                _adminActionBtn.BackColor = BrandPalette.ButtonPrimaryFill;
                _adminActionBtn.ForeColor = BrandPalette.ButtonPrimaryText;
                return;
            }

            _adminNameLabel.Text = _adminAccount.FullName ?? "—";
            _adminNameLabel.ForeColor = BrandPalette.NeutralText;
            _adminUsernameLabel.Text = $"Username: {_adminAccount.Username ?? "—"}";

            var contact = _adminAccount.ContactNumber ?? "—";
            var email = string.IsNullOrWhiteSpace(_adminAccount.Email) ? "" : $"   ·   {_adminAccount.Email}";
            _adminContactLabel.Text = contact + email;

            var lockNote = _adminAccount.IsLocked == true
                ? " · LOCKED"
                : "";
            _adminStatusLabel.Text =
                (_adminAccount.IsActive == true ? "Active" : "Inactive") + lockNote +
                (_adminAccount.LastLoginDate.HasValue
                    ? $"   ·   Last login: {_adminAccount.LastLoginDate.Value:yyyy-MM-dd HH:mm}"
                    : "   ·   Never logged in");
            _adminStatusLabel.ForeColor = _adminAccount.IsActive == true
                ? BrandPalette.StatusPositiveText
                : BrandPalette.StatusNegativeFill;

            _adminActionBtn.Text = "Edit Credentials";
            _adminActionBtn.BackColor = BrandPalette.ButtonSecondaryFill;
            _adminActionBtn.ForeColor = BrandPalette.ButtonSecondaryText;
        }

        private async void AdminActionBtn_Click(object? sender, EventArgs e)
        {
            using var modal = _adminAccount?.HasAdmin == true
                ? FrmAdminAccountEdit.ForEdit(_api, _company.CompanyId, _company.CompanyName, _adminAccount)
                : FrmAdminAccountEdit.ForCreate(_api, _company.CompanyId, _company.CompanyName);

            if (modal.ShowDialog(FindForm()) == DialogResult.OK && modal.Result is not null)
            {
                _adminAccount = modal.Result;
                RefreshAdminSection();
                Toast.Show(this,
                    _adminAccount.HasAdmin
                        ? "Admin account saved."
                        : "Admin account created.",
                    ToastKind.Success);
            }
        }

        // =====================================================================
        // ASSIGN ACTION
        // =====================================================================

        private async void AssignBtn_Click(object? sender, EventArgs e)
        {
            _assignErrorLabel.Text = string.Empty;

            int checkedIndex = _planRadios.FindIndex(r => r.Checked);
            if (checkedIndex < 0)
            {
                _assignErrorLabel.Text = "Please choose a plan.";
                return;
            }

            var planId = _planIdForRadio[checkedIndex];

            var agreedBy = _agreedByTxtBx.Text.Trim();
            if (string.IsNullOrWhiteSpace(agreedBy))
            {
                _assignErrorLabel.Text = "Who accepted the T&C is required.";
                return;
            }

            try
            {
                _assignBtn.Enabled = false;
                _assignBtn.Text = "Assigning…";

                await _api.AssignPlanAsync(_company.CompanyId, planId, agreedBy);

                // Refresh local data, then return to View mode.
                await ReloadAsync();
                _mode = Mode.View;
                ApplyMode();
            }
            catch (Exception ex)
            {
                _assignErrorLabel.Text = ex.Message;
            }
            finally
            {
                _assignBtn.Enabled = true;
                _assignBtn.Text = "Assign Plan";
            }
        }

        // =====================================================================
        // UTIL
        // =====================================================================

        private static string HumanizeModules(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return "—";

            var names = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(key => key switch
                {
                    "DataCollection" => "Data Collection",
                    "Transactions" => "Transactions",
                    "Feedback" => "Feedback",
                    "Retention" => "Retention",
                    "BI" => "Business Intelligence",
                    "BranchManagement" => "Branch Management",
                    _ => key,
                });

            return string.Join("  ·  ", names);
        }
    }
}