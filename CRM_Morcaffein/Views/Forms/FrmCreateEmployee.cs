using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// Modal for creating a new employee + their login credential in one step.
    /// Admin can only assign Barista or Manager roles — the server enforces the same
    /// rule, but we also omit "Admin" from the dropdown here.
    ///
    /// Layout: single absolute-positioned form panel, same pattern as
    /// FrmCampaignDetail. Fields share the same left edge (PAD) so labels sit
    /// directly above their inputs. The two-column rows use PAD and COL2 anchors.
    ///
    /// Branch assignment: Baristas must be pinned to a branch; Managers may be
    /// assigned a specific branch OR left as "All Branches" (null). The dropdown
    /// toggles between "Branch *" (required) and "Branch (optional)" when the
    /// role changes, and adds/removes the "All Branches" entry accordingly.
    /// </summary>
    public class FrmCreateEmployee : Form
    {
        private readonly EmployeeService _controller;

        private TextBox _firstNameTxtBx = null!;
        private TextBox _lastNameTxtBx = null!;
        private TextBox _contactTxtBx = null!;
        private TextBox _emailTxtBx = null!;
        private ComboBox _roleCombo = null!;
        private Label _branchLabel = null!;
        private ComboBox _branchCombo = null!;
        private TextBox _usernameTxtBx = null!;
        private TextBox _passwordTxtBx = null!;
        private TextBox _confirmTxtBx = null!;
        private Label _errorLabel = null!;

        // Modal width and the field width that goes with it.
        private const int FormWidth = 520;
        private const int PAD = 40;
        private const int FieldW = 392;
        private const int HalfW = 188;
        private const int Col2 = PAD + HalfW + 16;

        // Cached branch list. Loaded async at construction; the dropdown is
        // rebuilt from this whenever the role changes so we never have to
        // re-fetch from the API.
        private readonly List<BranchItem> _cachedBranches = new();

        public FrmCreateEmployee(EmployeeService controller)
        {
            _controller = controller;

            Text = "New Employee";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            // Content: title + subtitle + 8 rows + error + button row.
            ClientSize = new Size(FormWidth, 660);
            BackColor = BrandPalette.SurfaceCard;
            Font = BrandPalette.BrandFontRegular(9.5f);
            Padding = new Padding(24);

            BuildLayout();

            // Fire-and-forget branch load. The dropdown is empty until the API
            // responds; usually milliseconds, but the user might open it before.
            _ = LoadBranchesAsync();
        }

        private void BuildLayout()
        {
            var titleLabel = new Label
            {
                Text = "Create Employee",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var subtitleLabel = new Label
            {
                Text = "Fill in the employee's details and assign them a login.",
                Location = new Point(0, 26),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            var formHost = new Panel
            {
                Location = new Point(0, 66),
                Size = new Size(FormWidth - 48, 570),
                BackColor = BrandPalette.SurfaceCard,
            };

            const int labelH = 18;
            const int fieldH = 30;
            const int rowH = 58;
            int y = 0;

            void AddLabel(string text, int x)
            {
                formHost.Controls.Add(new Label
                {
                    Text = text,
                    Location = new Point(x, y),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontRegular(9f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                    BackColor = Color.Transparent,
                });
            }

            // ---- First name / Last name (2-up) ----
            AddLabel("First name *", PAD);
            _firstNameTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(HalfW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            formHost.Controls.Add(_firstNameTxtBx);

            AddLabel("Last name *", Col2);
            _lastNameTxtBx = new TextBox
            {
                Location = new Point(Col2, y + labelH),
                Size = new Size(HalfW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            formHost.Controls.Add(_lastNameTxtBx);
            y += rowH;

            // ---- Contact number ----
            AddLabel("Contact number * (10-11 digits)", PAD);
            _contactTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(FieldW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            _contactTxtBx.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            };
            formHost.Controls.Add(_contactTxtBx);
            y += rowH;

            // ---- Email ----
            AddLabel("Email (optional)", PAD);
            _emailTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(FieldW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            formHost.Controls.Add(_emailTxtBx);
            y += rowH;

            // ---- Role ----
            AddLabel("Role *", PAD);
            _roleCombo = new ComboBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(FieldW, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _roleCombo.Items.AddRange(new object[] { "Barista", "Manager" });
            _roleCombo.SelectedIndex = 0;
            _roleCombo.SelectedIndexChanged += (s, e) => UpdateBranchDropdownState();
            formHost.Controls.Add(_roleCombo);
            y += rowH;

            _branchLabel = new Label
            {
                Text = "Branch *",
                Location = new Point(PAD, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };
            formHost.Controls.Add(_branchLabel);

            _branchCombo = new ComboBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(FieldW, fieldH),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            formHost.Controls.Add(_branchCombo);

            var hasBranching = EmployeeSession.Current?.HasModule("Branching") == true;
            if (!hasBranching)
            {
                _branchLabel.Visible = false;
                _branchCombo.Visible = false;
            }

            y += rowH;

            // ---- Username ----
            AddLabel("Username * (min 3 chars)", PAD);
            _usernameTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(FieldW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            formHost.Controls.Add(_usernameTxtBx);
            y += rowH;

            // ---- Password / Confirm ----
            AddLabel("Password * (min 4 chars)", PAD);
            _passwordTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(HalfW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true,
            };
            formHost.Controls.Add(_passwordTxtBx);

            AddLabel("Confirm password *", Col2);
            _confirmTxtBx = new TextBox
            {
                Location = new Point(Col2, y + labelH),
                Size = new Size(HalfW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true,
            };
            formHost.Controls.Add(_confirmTxtBx);
            y += rowH;

            // ---- Error label ----
            _errorLabel = new Label
            {
                Location = new Point(PAD, y + 4),
                Size = new Size(FieldW, 26),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };
            formHost.Controls.Add(_errorLabel);

            // ---- Action row ----
            var btnRow = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.Transparent,
            };

            var cancelBtn = new Button
            {
                Text = "Cancel",
                Size = new Size(120, 36),
                Location = new Point(btnRow.Width - 120, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var createBtn = new Button
            {
                Text = "Create Employee",
                Size = new Size(160, 36),
                Location = new Point(btnRow.Width - 120 - 8 - 160, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            createBtn.FlatAppearance.BorderSize = 0;
            createBtn.Click += CreateBtn_Click;

            btnRow.Controls.Add(createBtn);
            btnRow.Controls.Add(cancelBtn);

            formHost.Size = new Size(FormWidth - 48, ClientSize.Height - 66 - 56 - 24);

            Controls.Add(formHost);
            Controls.Add(titleLabel);
            Controls.Add(subtitleLabel);
            Controls.Add(btnRow);

            _firstNameTxtBx.TabIndex = 0;
            _lastNameTxtBx.TabIndex = 1;
            _contactTxtBx.TabIndex = 2;
            _emailTxtBx.TabIndex = 3;
            _roleCombo.TabIndex = 4;
            _branchCombo.TabIndex = 5;
            _usernameTxtBx.TabIndex = 6;
            _passwordTxtBx.TabIndex = 7;
            _confirmTxtBx.TabIndex = 8;
            createBtn.TabIndex = 9;
            cancelBtn.TabIndex = 10;

            AcceptButton = createBtn;
            CancelButton = cancelBtn;
        }

        // =====================================================================
        // BRANCH HANDLING
        // =====================================================================

        private async Task LoadBranchesAsync()
        {
            try
            {
                var api = new BranchApi();
                var branches = await api.GetBranchesAsync();

                _cachedBranches.Clear();
                foreach (var b in branches)
                    _cachedBranches.Add(new BranchItem(b.BranchId, b.BranchName));

                UpdateBranchDropdownState();
            }
            catch (Exception ex)
            {
                _errorLabel.Text = $"Could not load branches: {ex.Message}";
            }
        }

        /// <summary>
        /// Rebuilds the branch dropdown based on the currently-selected role.
        /// Baristas: no "All Branches" entry, must pick a real branch.
        /// Managers: "All Branches" is available as the first item.
        /// Preserves any previously-selected branch if it still exists in the list.
        /// </summary>
        private void UpdateBranchDropdownState()
        {
            if (_branchCombo is null || _branchLabel is null || _roleCombo is null)
                return;

            var role = _roleCombo.SelectedItem as string ?? "Barista";
            var isBarista = role.Equals("Barista", StringComparison.OrdinalIgnoreCase);

            _branchLabel.Text = isBarista ? "Branch *" : "Branch (optional)";

            var previousSelection = _branchCombo.SelectedItem as BranchItem;

            _branchCombo.Items.Clear();

            if (!isBarista)
                _branchCombo.Items.Add(new BranchItem(null, "All Branches"));

            foreach (var b in _cachedBranches)
                _branchCombo.Items.Add(b);

            // Preserve previous selection if possible
            if (previousSelection is not null)
            {
                for (int i = 0; i < _branchCombo.Items.Count; i++)
                {
                    if (_branchCombo.Items[i] is BranchItem bi &&
                        bi.BranchId == previousSelection.BranchId)
                    {
                        _branchCombo.SelectedIndex = i;
                        return;
                    }
                }
            }

            // Default: for Barista pick the first real branch; for Manager pick "All Branches"
            if (_branchCombo.Items.Count > 0)
                _branchCombo.SelectedIndex = 0;
        }

        private sealed class BranchItem
        {
            public int? BranchId { get; }
            public string Display { get; }
            public BranchItem(int? id, string display) { BranchId = id; Display = display; }
            public override string ToString() => Display;
        }

        // =====================================================================
        // SAVE
        // =====================================================================

        private async void CreateBtn_Click(object? sender, EventArgs e)
        {
            _errorLabel.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(_firstNameTxtBx.Text) ||
                string.IsNullOrWhiteSpace(_lastNameTxtBx.Text))
            {
                _errorLabel.Text = "First and last name are required.";
                return;
            }

            var cleanedPhone = new string(_contactTxtBx.Text.Where(char.IsDigit).ToArray());
            if (cleanedPhone.Length < 10 || cleanedPhone.Length > 11)
            {
                _errorLabel.Text = "Contact number must be 10-11 digits.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(_emailTxtBx.Text) &&
                (!_emailTxtBx.Text.Contains('@') || !_emailTxtBx.Text.Contains('.')))
            {
                _errorLabel.Text = "Invalid email format.";
                return;
            }

            // ---- Branch resolution ----
            var selectedRole = _roleCombo.SelectedItem as string ?? "Barista";
            var isBarista = selectedRole.Equals("Barista", StringComparison.OrdinalIgnoreCase);
            var hasBranching = EmployeeSession.Current?.HasModule("Branching") == true;

            int? branchId;
            if (hasBranching)
            {
                branchId = (_branchCombo.SelectedItem as BranchItem)?.BranchId;

                if (isBarista && branchId is null)
                {
                    _errorLabel.Text = "Baristas must be assigned to a branch.";
                    return;
                }
            }
            else
            {
                // No Branching module: single-branch tenant. Default everyone
                // to Branch 1 so the FK requirement downstream is met.
                branchId = 1;
            }

            if (_usernameTxtBx.Text.Trim().Length < 3)
            {
                _errorLabel.Text = "Username must be at least 3 characters.";
                return;
            }

            if (_passwordTxtBx.Text.Length < 4)
            {
                _errorLabel.Text = "Password must be at least 4 characters.";
                return;
            }

            if (_passwordTxtBx.Text != _confirmTxtBx.Text)
            {
                _errorLabel.Text = "Passwords do not match.";
                return;
            }

            try
            {
                await _controller.CreateAsync(
                    _firstNameTxtBx.Text, _lastNameTxtBx.Text,
                    _contactTxtBx.Text,
                    string.IsNullOrWhiteSpace(_emailTxtBx.Text) ? null : _emailTxtBx.Text,
                    selectedRole,
                    _usernameTxtBx.Text,
                    _passwordTxtBx.Text,
                    branchId);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _errorLabel.Text = ex.Message;
            }
        }
    }
}