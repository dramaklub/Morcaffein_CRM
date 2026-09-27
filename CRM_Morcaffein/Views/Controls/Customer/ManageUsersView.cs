using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services;
using CRM_Morcaffein.winforms.Services.Api;
using CRM_Morcaffein.winforms.Views;
using CRM_Morcaffein.winforms.Views.Controls;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Admin tab: manage employee accounts. Searchable grid + left drawer for the
    /// selected employee (details + Edit / Reset Password / Deactivate actions).
    ///
    /// Admin role is NOT shown in the role dropdown — only Barista and Manager. Admins
    /// cannot create other Admins, edit Admins, or deactivate Admins (server enforces
    /// the same rules).
    ///
    /// Branch assignment: Baristas must be pinned to a branch; Managers may be
    /// assigned a specific branch OR left as "All Branches" (null). The dropdown
    /// toggles between "Branch *" and "Branch (optional)" and adds/removes the
    /// "All Branches" entry when the Role changes.
    /// </summary>
    public class ManageUsersView : UserControl
    {
        private readonly EmployeeService _controller;
        private readonly BindingSource _employeesBindingSource = new();

        private EmployeeResponseDto? _selectedEmployee;
        private int _page = 1;
        private const int PageSize = 25;
        private string _lastSearchSignature = string.Empty;

        // Layout
        private Panel cardHeaderPanel = null!;
        private TextBox searchTxtBx = null!;
        private Button newEmployeeBtn = null!;
        private DataGridView employeesGrid = null!;
        private Panel drawerPanel = null!;
        private Label drawerEmptyLabel = null!;
        private Panel drawerContentPanel = null!;
        private FlowLayoutPanel drawerFlow = null!;

        private Panel combinedBox = null!;
        private Label nameLabel = null!;
        private Label roleLabel = null!;
        private Label contactLabel = null!;
        private Label lastLoginLabel = null!;
        private Label statusLabel = null!;
        private Button editBtn = null!;
        private Button resetPasswordBtn = null!;
        private Button deactivateBtn = null!;
        private PaginationControl pagination = null!;

        // Inline edit state — the drawer's combinedBox swaps between a "view" layout
        // (profile + three buttons) and an "edit" layout (profile header + editable fields
        // + Save/Cancel). These fields hold the editable rows.
        private Panel editPanel = null!;
        private TextBox editFirstNameTxtBx = null!;
        private TextBox editLastNameTxtBx = null!;
        private TextBox editContactTxtBx = null!;
        private TextBox editEmailTxtBx = null!;
        private ComboBox editRoleCombo = null!;
        private Label editBranchLabel = null!;
        private ComboBox editBranchCombo = null!;
        private TextBox editUsernameTxtBx = null!;
        private Label editErrorLabel = null!;
        private Button editSaveBtn = null!;
        private Button editCancelBtn = null!;

        private Panel resetPanel = null!;
        private TextBox resetNewPasswordTxtBx = null!;
        private TextBox resetConfirmPasswordTxtBx = null!;
        private Label resetErrorLabel = null!;
        private Button resetSaveBtn = null!;
        private Button resetCancelBtn = null!;
        private DrawerMode _mode = DrawerMode.View;

        // Branch list cache — loaded once, refreshed on demand
        private readonly List<BranchItem> _branchesCache = new();
        private bool _branchesLoaded;

        private enum DrawerMode
        {
            View,
            Edit,
            Reset,
        }

        public ManageUsersView()
        {
            _controller = new EmployeeService();
            BuildLayout();
            ConfigureGridColumns();
            Load += async (s, e) => await ReloadAsync();
        }

        // =====================================================================
        // LAYOUT (mirrors CustomerManagementView's card + drawer)
        // =====================================================================

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;
            BackColor = BrandPalette.SurfaceSecondary;

            var bodyPanel = new Panel { Dock = DockStyle.Fill, BackColor = BrandPalette.SurfaceSecondary };

            // ---------- LEFT DRAWER ----------
            drawerPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 380,
                BackColor = BrandPalette.SurfaceCard,
            };

            drawerEmptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Text = "Select an employee to view details.",
            };

            drawerContentPanel = new Panel { Dock = DockStyle.Fill, Visible = false, AutoScroll = true };

            drawerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20),
                Width = 380,
            };

            // ---------- SINGLE COMBINED BOX: profile + divider + actions ----------
            combinedBox = new Panel
            {
                Size = new Size(340, 340),
                BackColor = BrandPalette.SurfaceCard,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0),
            };

            nameLabel = new Label
            {
                Location = new Point(16, 16),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(13f),
                ForeColor = BrandPalette.NeutralText,
            };
            roleLabel = new Label
            {
                Location = new Point(16, 46),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            contactLabel = new Label
            {
                Location = new Point(16, 72),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralText,
            };
            lastLoginLabel = new Label
            {
                Location = new Point(16, 98),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            statusLabel = new Label
            {
                Location = new Point(16, 124),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(9.5f),
                ForeColor = BrandPalette.NeutralText,
            };

            var dividerLine = new Panel
            {
                Location = new Point(16, 156),
                Size = new Size(308, 1),
                BackColor = BrandPalette.Border,
            };

            editBtn = MakeActionButton("Edit Information", 176);
            editBtn.Click += (s, e) => EnterEditState();

            resetPasswordBtn = MakeActionButton("Reset Password", 218);
            resetPasswordBtn.Click += (s, e) => EnterResetState();

            deactivateBtn = MakeActionButton("Deactivate Employee", 260);
            deactivateBtn.ForeColor = BrandPalette.StatusNegativeFill;
            deactivateBtn.Click += (s, e) => DeactivateSelected();

            ButtonStyler.TrackEnabledState(editBtn);
            ButtonStyler.TrackEnabledState(resetPasswordBtn);
            ButtonStyler.TrackEnabledState(deactivateBtn);

            editPanel = BuildEditPanel();
            editPanel.Visible = false;

            resetPanel = BuildResetPanel();
            resetPanel.Visible = false;

            combinedBox.Controls.Add(resetPanel);
            combinedBox.Controls.Add(editPanel);
            combinedBox.Controls.Add(deactivateBtn);
            combinedBox.Controls.Add(resetPasswordBtn);
            combinedBox.Controls.Add(editBtn);
            combinedBox.Controls.Add(dividerLine);
            combinedBox.Controls.Add(statusLabel);
            combinedBox.Controls.Add(lastLoginLabel);
            combinedBox.Controls.Add(contactLabel);
            combinedBox.Controls.Add(roleLabel);
            combinedBox.Controls.Add(nameLabel);

            drawerFlow.Controls.Add(combinedBox);
            drawerContentPanel.Controls.Add(drawerFlow);

            drawerPanel.Controls.Add(drawerEmptyLabel);
            drawerPanel.Controls.Add(drawerContentPanel);

            // ---------- RIGHT SIDE: card with grid ----------
            var cardOuter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
                Padding = new Padding(24),
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
            };

            cardHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = BrandPalette.SurfacePrimary };

            searchTxtBx = new TextBox
            {
                Location = new Point(16, 16),
                Size = new Size(300, 28),
                PlaceholderText = "Search by name, contact, username, or ID",
                Font = BrandPalette.BrandFontRegular(10f),
            };
            searchTxtBx.TextChanged += async (s, e) => await ReloadAsync();

            newEmployeeBtn = new Button
            {
                Text = "+ New Employee",
                Size = new Size(150, 32),
                Location = new Point(16, 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            newEmployeeBtn.FlatAppearance.BorderSize = 0;
            newEmployeeBtn.Click += (s, e) => OpenCreateModal();

            var divider = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = BrandPalette.Border };

            cardHeaderPanel.Controls.Add(newEmployeeBtn);
            cardHeaderPanel.Controls.Add(searchTxtBx);
            cardHeaderPanel.Controls.Add(divider);
            cardHeaderPanel.Resize += (s, e) =>
            {
                newEmployeeBtn.Left = cardHeaderPanel.ClientSize.Width - newEmployeeBtn.Width - 16;
                newEmployeeBtn.Top = 14;
            };

            pagination = new PaginationControl();
            pagination.PageChanged += async (s, page) =>
            {
                _page = page;
                await ReloadAsync(keepPage: true);
            };

            employeesGrid = new DataGridView { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
            ConfigureGrid(employeesGrid);
            employeesGrid.CellFormatting += EmployeesGrid_CellFormatting;
            employeesGrid.SelectionChanged += async (s, e) =>
            {
                _selectedEmployee = employeesGrid.CurrentRow?.DataBoundItem as EmployeeResponseDto;

                if (_mode != DrawerMode.View)
                    ExitToViewState();

                RefreshDrawer();
            };

            card.Controls.Add(employeesGrid);
            card.Controls.Add(pagination);
            card.Controls.Add(cardHeaderPanel);
            cardOuter.Controls.Add(card);

            bodyPanel.Controls.Add(cardOuter);
            bodyPanel.Controls.Add(drawerPanel);
            Controls.Add(bodyPanel);

            newEmployeeBtn.Left = 300;
        }

        private void EmployeesGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (employeesGrid.Rows[e.RowIndex].DataBoundItem is not EmployeeResponseDto emp)
                return;

            if (employeesGrid.Columns[e.ColumnIndex].Name == nameof(EmployeeResponseDto.IsActive))
            {
                e.Value = emp.IsActive ? "Active" : "Inactive";
                e.FormattingApplied = true;
            }

            if (!emp.IsActive)
            {
                var greyText = Color.FromArgb(255, 130, 130, 130);
                var greyBackground = Color.FromArgb(255, 240, 240, 240);

                e.CellStyle.ForeColor = greyText;
                e.CellStyle.BackColor = greyBackground;
                e.CellStyle.SelectionForeColor = greyText;
                e.CellStyle.SelectionBackColor = greyBackground;
            }
        }

        /// <summary>
        /// The inline edit form. Fields are prefilled in EnterEditState() from the
        /// currently selected employee. Layout is absolute-positioned inside a fixed
        /// panel whose height is computed at the end.
        /// </summary>
        private Panel BuildEditPanel()
        {
            const int rowHeight = 56;
            const int labelHeight = 18;
            const int fieldHeight = 28;
            const int fieldWidth = 308;

            var panel = new Panel
            {
                Location = new Point(14, 176),
                Width = 312,
                Height = 7 * rowHeight + 80,   // 7 rows (added Branch) + Save/Cancel + error
                BackColor = BrandPalette.SurfaceCard,
            };

            int y = 0;

            TextBox MakeRow(string labelText, bool digitsOnly = false)
            {
                var label = new Label
                {
                    Text = labelText,
                    Location = new Point(0, y),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontRegular(9f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                };
                var txt = new TextBox
                {
                    Location = new Point(0, y + labelHeight),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = BrandPalette.BrandFontRegular(9.5f),
                    BorderStyle = BorderStyle.FixedSingle,
                };
                if (digitsOnly)
                {
                    txt.KeyPress += (s, e) =>
                    {
                        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                            e.Handled = true;
                    };
                }
                panel.Controls.Add(txt);
                panel.Controls.Add(label);
                y += rowHeight;
                return txt;
            }

            editFirstNameTxtBx = MakeRow("First name *");
            editLastNameTxtBx = MakeRow("Last name *");
            editContactTxtBx = MakeRow("Contact number * (10-11 digits)", digitsOnly: true);
            editEmailTxtBx = MakeRow("Email (optional)");

            // ---- Role row ----
            var roleLabel = new Label
            {
                Text = "Role *",
                Location = new Point(0, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            editRoleCombo = new ComboBox
            {
                Location = new Point(0, y + labelHeight),
                Size = new Size(fieldWidth, fieldHeight),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            editRoleCombo.Items.AddRange(new object[] { "Barista", "Manager" });
            editRoleCombo.SelectedIndexChanged += (s, e) => UpdateEditBranchDropdownState();
            panel.Controls.Add(editRoleCombo);
            panel.Controls.Add(roleLabel);
            y += rowHeight;

            // ---- Branch row ----
            editBranchLabel = new Label
            {
                Text = "Branch *",
                Location = new Point(0, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            editBranchCombo = new ComboBox
            {
                Location = new Point(0, y + labelHeight),
                Size = new Size(fieldWidth, fieldHeight),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            panel.Controls.Add(editBranchCombo);
            panel.Controls.Add(editBranchLabel);
            y += rowHeight;

            editUsernameTxtBx = MakeRow("Username * (min 3 chars)");

            // Error label
            editErrorLabel = new Label
            {
                Location = new Point(0, y),
                Size = new Size(fieldWidth, 24),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            panel.Controls.Add(editErrorLabel);
            y += 28;

            // Save / Cancel row
            editSaveBtn = new Button
            {
                Text = "Save Changes",
                Location = new Point(0, y),
                Size = new Size(150, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
            };
            editSaveBtn.FlatAppearance.BorderSize = 0;
            editSaveBtn.Click += EditSaveBtn_Click;

            editCancelBtn = new Button
            {
                Text = "Cancel",
                Location = new Point(158, y),
                Size = new Size(150, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            editCancelBtn.FlatAppearance.BorderSize = 0;
            editCancelBtn.Click += (s, e) => ExitToViewState();

            panel.Controls.Add(editSaveBtn);
            panel.Controls.Add(editCancelBtn);

            panel.Height = y + 32;
            return panel;
        }

        /// <summary>
        /// Loads the tenant's branch list once, caches it, and refreshes the edit
        /// dropdown. Called before entering edit state.
        /// </summary>
        private void EnsureBranchesLoaded()
        {
            if (_branchesLoaded) return;
            _branchesLoaded = true;

            _ = Task.Run(async () =>
            {
                try
                {
                    var api = new BranchApi();
                    var branches = await api.GetBranchesAsync();

                    _branchesCache.Clear();
                    foreach (var b in branches)
                        _branchesCache.Add(new BranchItem(b.BranchId, b.BranchName));

                    if (IsHandleCreated)
                        BeginInvoke(new Action(UpdateEditBranchDropdownState));
                }
                catch
                {
                    // Silently ignore — dropdown stays empty and user gets a
                    // clear server-side error if they try to save without a branch.
                }
            });
        }

        /// <summary>
        /// Rebuilds the branch dropdown based on the currently-selected role.
        /// Baristas: no "All Branches" entry. Managers: "All Branches" is first.
        /// Preserves previous selection if it survives the rebuild.
        /// </summary>
        private void UpdateEditBranchDropdownState()
        {
            if (editBranchCombo is null || editBranchLabel is null || editRoleCombo is null)
                return;

            var role = editRoleCombo.SelectedItem as string ?? "Barista";
            var isBarista = role.Equals("Barista", StringComparison.OrdinalIgnoreCase);

            editBranchLabel.Text = isBarista ? "Branch *" : "Branch (optional)";

            var previousSelection = editBranchCombo.SelectedItem as BranchItem;

            editBranchCombo.Items.Clear();

            if (!isBarista)
                editBranchCombo.Items.Add(new BranchItem(null, "All Branches"));

            foreach (var b in _branchesCache)
                editBranchCombo.Items.Add(b);

            // Preserve previous selection if possible
            if (previousSelection is not null)
            {
                for (int i = 0; i < editBranchCombo.Items.Count; i++)
                {
                    if (editBranchCombo.Items[i] is BranchItem bi &&
                        bi.BranchId == previousSelection.BranchId)
                    {
                        editBranchCombo.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (editBranchCombo.Items.Count > 0)
                editBranchCombo.SelectedIndex = 0;
        }

        private sealed class BranchItem
        {
            public int? BranchId { get; }
            public string Display { get; }
            public BranchItem(int? id, string display) { BranchId = id; Display = display; }
            public override string ToString() => Display;
        }

        /// <summary>
        /// Switches combinedBox from "view" to "edit" state and prefills every field
        /// from the selected employee.
        /// </summary>
        private void EnterEditState()
        {
            if (_selectedEmployee is null) return;

            var emp = _selectedEmployee;

            editFirstNameTxtBx.Text = emp.FirstName;
            editLastNameTxtBx.Text = emp.LastName;
            editContactTxtBx.Text = emp.ContactNumber;
            editEmailTxtBx.Text = emp.Email ?? string.Empty;
            editUsernameTxtBx.Text = emp.Username ?? string.Empty;

            var roleIndex = editRoleCombo.Items.IndexOf(emp.Role);
            editRoleCombo.SelectedIndex = roleIndex >= 0 ? roleIndex : 0;

            // Load branches (once per view) and rebuild the dropdown for the current role.
            EnsureBranchesLoaded();
            UpdateEditBranchDropdownState();

            // Preselect the employee's current branch. For null (all-branch Manager),
            // picks the "All Branches" entry — which only exists if role is Manager.
            for (int i = 0; i < editBranchCombo.Items.Count; i++)
            {
                if (editBranchCombo.Items[i] is BranchItem bi && bi.BranchId == emp.BranchId)
                {
                    editBranchCombo.SelectedIndex = i;
                    break;
                }
            }

            editErrorLabel.Text = string.Empty;

            editBtn.Visible = false;
            resetPasswordBtn.Visible = false;
            deactivateBtn.Visible = false;
            editPanel.Visible = true;
            resetPanel.Visible = false;

            combinedBox.Height = 176 + editPanel.Height + 14;

            _mode = DrawerMode.Edit;
        }

        /// <summary>
        /// Reverts combinedBox to "view" state. Called on Cancel and after a successful
        /// Save from either the edit panel or the reset panel.
        /// </summary>
        private void ExitToViewState()
        {
            editPanel.Visible = false;
            resetPanel.Visible = false;
            editBtn.Visible = true;
            resetPasswordBtn.Visible = true;
            deactivateBtn.Visible = true;

            combinedBox.Height = 340;
            _mode = DrawerMode.View;
        }

        private async void EditSaveBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedEmployee is null) return;

            editErrorLabel.Text = string.Empty;

            // Client-side validation — mirrors the server rules.
            if (string.IsNullOrWhiteSpace(editFirstNameTxtBx.Text) ||
                string.IsNullOrWhiteSpace(editLastNameTxtBx.Text))
            {
                editErrorLabel.Text = "First and last name are required.";
                return;
            }

            var cleanedPhone = new string(editContactTxtBx.Text.Where(char.IsDigit).ToArray());
            if (cleanedPhone.Length < 10 || cleanedPhone.Length > 11)
            {
                editErrorLabel.Text = "Contact number must be 10-11 digits.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(editEmailTxtBx.Text) &&
                (!editEmailTxtBx.Text.Contains('@') || !editEmailTxtBx.Text.Contains('.')))
            {
                editErrorLabel.Text = "Invalid email format.";
                return;
            }

            var newUsername = editUsernameTxtBx.Text.Trim();
            if (newUsername.Length < 3)
            {
                editErrorLabel.Text = "Username must be at least 3 characters.";
                return;
            }

            // ---- Branch validation ----
            var newRole = editRoleCombo.SelectedItem as string ?? "Barista";
            var isBarista = newRole.Equals("Barista", StringComparison.OrdinalIgnoreCase);
            int? branchId = (editBranchCombo.SelectedItem as BranchItem)?.BranchId;

            if (isBarista && branchId is null)
            {
                editErrorLabel.Text = "Baristas must be assigned to a branch.";
                return;
            }

            try
            {
                editSaveBtn.Enabled = false;

                await _controller.UpdateAsync(
                    _selectedEmployee.EmployeeId,
                    editFirstNameTxtBx.Text,
                    editLastNameTxtBx.Text,
                    editContactTxtBx.Text,
                    string.IsNullOrWhiteSpace(editEmailTxtBx.Text) ? null : editEmailTxtBx.Text,
                    newRole,
                    newUsername,
                    branchId);

                ExitToViewState();
                Toast.Show(this, $"Saved changes to {_selectedEmployee.FullName}.", ToastKind.Success);
                await ReloadAsync(keepPage: true);
            }
            catch (Exception ex)
            {
                editErrorLabel.Text = ex.Message;
            }
            finally
            {
                editSaveBtn.Enabled = true;
            }
        }

        /// <summary>
        /// The inline reset-password form.
        /// </summary>
        private Panel BuildResetPanel()
        {
            const int rowHeight = 56;
            const int labelHeight = 18;
            const int fieldHeight = 28;
            const int fieldWidth = 308;

            var panel = new Panel
            {
                Location = new Point(14, 176),
                Width = 312,
                Height = 2 * rowHeight + 80,
                BackColor = BrandPalette.SurfaceCard,
            };

            int y = 0;

            void AddLabel(string text, int yPos)
            {
                var label = new Label
                {
                    Text = text,
                    Location = new Point(0, yPos),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontRegular(9f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                };
                panel.Controls.Add(label);
            }

            TextBox MakePwdBox()
            {
                var txt = new TextBox
                {
                    Location = new Point(0, y + labelHeight),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = BrandPalette.BrandFontRegular(9.5f),
                    BorderStyle = BorderStyle.FixedSingle,
                    UseSystemPasswordChar = true,
                };
                panel.Controls.Add(txt);
                y += rowHeight;
                return txt;
            }

            AddLabel("New password * (min 4 chars)", y);
            resetNewPasswordTxtBx = MakePwdBox();

            AddLabel("Confirm new password *", y);
            resetConfirmPasswordTxtBx = MakePwdBox();

            resetErrorLabel = new Label
            {
                Location = new Point(0, y),
                Size = new Size(fieldWidth, 24),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            panel.Controls.Add(resetErrorLabel);
            y += 28;

            resetSaveBtn = new Button
            {
                Text = "Reset Password",
                Location = new Point(0, y),
                Size = new Size(150, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
            };
            resetSaveBtn.FlatAppearance.BorderSize = 0;
            resetSaveBtn.Click += ResetSaveBtn_Click;

            resetCancelBtn = new Button
            {
                Text = "Cancel",
                Location = new Point(158, y),
                Size = new Size(150, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            resetCancelBtn.FlatAppearance.BorderSize = 0;
            resetCancelBtn.Click += (s, e) => ExitToViewState();

            panel.Controls.Add(resetSaveBtn);
            panel.Controls.Add(resetCancelBtn);

            panel.Height = y + 32;
            return panel;
        }

        private void EnterResetState()
        {
            if (_selectedEmployee is null) return;

            resetNewPasswordTxtBx.Text = string.Empty;
            resetConfirmPasswordTxtBx.Text = string.Empty;
            resetErrorLabel.Text = string.Empty;

            editPanel.Visible = false;
            resetPanel.Visible = true;
            editBtn.Visible = false;
            resetPasswordBtn.Visible = false;
            deactivateBtn.Visible = false;

            combinedBox.Height = 176 + resetPanel.Height + 14;
            _mode = DrawerMode.Reset;
        }

        private async void ResetSaveBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedEmployee is null) return;

            resetErrorLabel.Text = string.Empty;

            var pwd = resetNewPasswordTxtBx.Text;
            var confirm = resetConfirmPasswordTxtBx.Text;

            if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 4)
            {
                resetErrorLabel.Text = "Password must be at least 4 characters.";
                return;
            }
            if (pwd != confirm)
            {
                resetErrorLabel.Text = "Passwords do not match.";
                return;
            }

            try
            {
                resetSaveBtn.Enabled = false;
                await _controller.ResetPasswordAsync(_selectedEmployee.EmployeeId, pwd);

                ExitToViewState();
                Toast.Show(this, $"Password reset for {_selectedEmployee.FullName}.", ToastKind.Success);
            }
            catch (Exception ex)
            {
                resetErrorLabel.Text = ex.Message;
            }
            finally
            {
                resetSaveBtn.Enabled = true;
            }
        }

        private static Button MakeActionButton(string text, int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(14, y),
                Size = new Size(312, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // =====================================================================
        // GRID
        // =====================================================================

        private void ConfigureGridColumns()
        {
            employeesGrid.Columns.Clear();
            employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(EmployeeResponseDto.EmployeeId),
                DataPropertyName = nameof(EmployeeResponseDto.EmployeeId),
                HeaderText = "ID",
                Width = 60,
            });
            employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(EmployeeResponseDto.FullName),
                DataPropertyName = nameof(EmployeeResponseDto.FullName),
                HeaderText = "Name",
                Width = 180,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            });
            employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(EmployeeResponseDto.Role),
                DataPropertyName = nameof(EmployeeResponseDto.Role),
                HeaderText = "Role",
                Width = 90,
            });
            employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(EmployeeResponseDto.ContactNumber),
                DataPropertyName = nameof(EmployeeResponseDto.ContactNumber),
                HeaderText = "Contact",
                Width = 120,
            });
            employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(EmployeeResponseDto.Username),
                DataPropertyName = nameof(EmployeeResponseDto.Username),
                HeaderText = "Username",
                Width = 140,
            });
            employeesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(EmployeeResponseDto.IsActive),
                DataPropertyName = nameof(EmployeeResponseDto.IsActive),
                HeaderText = "Active",
                Width = 60,
            });
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            grid.BackgroundColor = BrandPalette.SurfacePrimary;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoGenerateColumns = false;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = BrandPalette.Border;
            grid.Font = BrandPalette.BrandFontRegular(9.5f);
            grid.ColumnHeadersDefaultCellStyle.BackColor = BrandPalette.HeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = BrandPalette.HeaderText;
            grid.ColumnHeadersDefaultCellStyle.Font = BrandPalette.BrandFontBold(9.5f);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = BrandPalette.HeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = BrandPalette.HeaderText;
            grid.ColumnHeadersHeight = 36;
            grid.DefaultCellStyle.SelectionBackColor = BrandPalette.RowSelected;
            grid.DefaultCellStyle.SelectionForeColor = BrandPalette.NeutralText;
            grid.DefaultCellStyle.BackColor = BrandPalette.SurfacePrimary;
            grid.DefaultCellStyle.ForeColor = BrandPalette.NeutralText;
            grid.RowTemplate.Height = 30;
            grid.ScrollBars = ScrollBars.Vertical;
        }

        // =====================================================================
        // LOADING
        // =====================================================================

        private async Task ReloadAsync(bool keepPage = false)
        {
            if (!keepPage)
            {
                _page = 1;
                pagination.ResetToFirstPage();
            }

            PagedResponse<EmployeeResponseDto> response;
            try
            {
                response = await _controller.SearchAsync(searchTxtBx.Text, _page, PageSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Error loading employees");
                return;
            }

            _employeesBindingSource.DataSource = response.Items;
            employeesGrid.DataSource = _employeesBindingSource;
            pagination.UpdatePageInfo(response.TotalCount, response.Page, response.PageSize);

            employeesGrid.ClearSelection();
            try { employeesGrid.CurrentCell = null; } catch { }
            _selectedEmployee = null;
            RefreshDrawer();
        }

        // =====================================================================
        // DRAWER
        // =====================================================================

        private void RefreshDrawer()
        {
            var emp = _selectedEmployee;
            if (emp is null)
            {
                drawerEmptyLabel.Visible = true;
                drawerContentPanel.Visible = false;
                return;
            }

            drawerEmptyLabel.Visible = false;
            drawerContentPanel.Visible = true;

            nameLabel.Text = emp.FullName;
            roleLabel.Text = $"{emp.Role}";
            contactLabel.Text = string.IsNullOrWhiteSpace(emp.ContactNumber) ? "—" : emp.ContactNumber;
            lastLoginLabel.Text = emp.LastLoginDate.HasValue
                ? $"Last login: {emp.LastLoginDate.Value:yyyy-MM-dd HH:mm}"
                : "Never logged in";
            statusLabel.Text = emp.IsActive ? "Active" : "Inactive";
            statusLabel.ForeColor = emp.IsActive ? BrandPalette.StatusPositiveText : BrandPalette.StatusNegativeFill;

            var isAdminRow = emp.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                 emp.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
            var isInactive = !emp.IsActive;

            editBtn.Enabled = !isAdminRow && !isInactive;
            resetPasswordBtn.Enabled = !isAdminRow && !isInactive;
            deactivateBtn.Enabled = !isAdminRow;

            deactivateBtn.Text = isInactive ? "Reactivate Employee" : "Deactivate Employee";
            deactivateBtn.ForeColor = isInactive
                ? BrandPalette.NeutralText
                : BrandPalette.StatusNegativeFill;
        }

        // =====================================================================
        // ACTIONS
        // =====================================================================

        private void OpenCreateModal()
        {
            using var form = new FrmCreateEmployee(_controller);
            if (form.ShowDialog(FindForm()) == DialogResult.OK)
            {
                _ = ReloadAsync();
            }
        }

        private async void DeactivateSelected()
        {
            if (_selectedEmployee is null) return;

            var emp = _selectedEmployee;
            var activating = !emp.IsActive;

            var confirm = MessageBox.Show(FindForm(),
                activating
                    ? $"Reactivate {emp.FullName}? They will be able to log in again."
                    : $"Deactivate {emp.FullName}? They will no longer be able to log in.",
                activating ? "Reactivate Employee" : "Deactivate Employee",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                if (activating)
                    await _controller.ReactivateAsync(emp.EmployeeId);
                else
                    await _controller.DeactivateAsync(emp.EmployeeId);

                await ReloadAsync(keepPage: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, activating ? "Reactivate failed" : "Deactivate failed");
            }
        }
    }
}