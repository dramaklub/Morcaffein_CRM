using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// Modal for creating a new employee + their login credential in one step.
    /// Admin can only assign Barista or Manager roles — the server enforces the same
    /// rule, but we also omit "Admin" from the dropdown here.
    /// </summary>
    public class FrmCreateEmployee : Form
    {
        private readonly EmployeeController _controller;

        private TextBox _firstNameTxtBx = null!;
        private TextBox _lastNameTxtBx = null!;
        private TextBox _contactTxtBx = null!;
        private TextBox _emailTxtBx = null!;
        private ComboBox _roleCombo = null!;
        private TextBox _usernameTxtBx = null!;
        private TextBox _passwordTxtBx = null!;
        private TextBox _confirmTxtBx = null!;
        private Label _errorLabel = null!;

        public FrmCreateEmployee(EmployeeController controller)
        {
            _controller = controller;

            Text = "New Employee";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(500, 560);
            BackColor = BrandPalette.SurfacePrimary;
            Font = BrandPalette.BrandFontRegular(9.5f);
            Padding = new Padding(24);

            BuildLayout();
        }

        private void BuildLayout()
        {
            var titleLabel = new Label
            {
                Text = "Create Employee",
                Dock = DockStyle.Top,
                Height = 30,
                Font = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
            };

            var subtitleLabel = new Label
            {
                Text = "Fill in the employee's details and assign them a login.",
                Dock = DockStyle.Top,
                Height = 40,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            // Two-column row for first/last name
            var nameRow = new Panel { Dock = DockStyle.Top, Height = 68 };
            _firstNameTxtBx = MakeField(0, 0, 220);
            _lastNameTxtBx = MakeField(244, 0, 220);

            _contactTxtBx = MakeField(0, 0, 464);
            _contactTxtBx.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            };
            var contactPanel = WrapField("Contact number * (10-11 digits)", _contactTxtBx, 68);

            _emailTxtBx = MakeField(0, 0, 464);
            var emailPanel = WrapField("Email (optional)", _emailTxtBx, 68);

            // Role row
            var rolePanel = new Panel { Dock = DockStyle.Top, Height = 68 };
            var roleLabel = new Label
            {
                Text = "Role *",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _roleCombo = new ComboBox
            {
                Location = new Point(0, 20),
                Size = new Size(464, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _roleCombo.Items.AddRange(new object[] { "Barista", "Manager" });
            _roleCombo.SelectedIndex = 0;
            rolePanel.Controls.Add(_roleCombo);
            rolePanel.Controls.Add(roleLabel);

            _usernameTxtBx = MakeField(0, 0, 464);
            var usernamePanel = WrapField("Username * (min 3 chars)", _usernameTxtBx, 68);

            _passwordTxtBx = MakeField(0, 0, 464);
            _passwordTxtBx.UseSystemPasswordChar = true;
            var passwordPanel = WrapField("Password * (min 4 chars)", _passwordTxtBx, 68);

            _confirmTxtBx = MakeField(0, 0, 464);
            _confirmTxtBx.UseSystemPasswordChar = true;
            var confirmPanel = WrapField("Confirm password *", _confirmTxtBx, 68);

            _errorLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
            };

            var btnRow = new Panel { Dock = DockStyle.Bottom, Height = 44 };

            var cancelBtn = new Button
            {
                Text = "Cancel",
                Width = 120,
                Height = 36,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var createBtn = new Button
            {
                Text = "Create Employee",
                Width = 160,
                Height = 36,
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
            };
            createBtn.FlatAppearance.BorderSize = 0;
            createBtn.Click += CreateBtn_Click;

            btnRow.Controls.Add(cancelBtn);
            btnRow.Controls.Add(createBtn);

            // Build top-down. Order matters with Dock.Top: first added is bottom-most.
            // We want visual order: title, subtitle, first/last row, contact, email, role, username, password, confirm, error.
            var stack = new List<Control>
            {
                _errorLabel,
                confirmPanel,
                passwordPanel,
                usernamePanel,
                rolePanel,
                emailPanel,
                contactPanel,
                nameRow,
                subtitleLabel,
                titleLabel,
            };
            foreach (var c in stack)
                Controls.Add(c);
            Controls.Add(btnRow);

            // Tab order
            int tab = 0;
            _firstNameTxtBx.TabIndex = tab++;
            _lastNameTxtBx.TabIndex = tab++;
            _contactTxtBx.TabIndex = tab++;
            _emailTxtBx.TabIndex = tab++;
            _roleCombo.TabIndex = tab++;
            _usernameTxtBx.TabIndex = tab++;
            _passwordTxtBx.TabIndex = tab++;
            _confirmTxtBx.TabIndex = tab++;
            createBtn.TabIndex = tab++;
            cancelBtn.TabIndex = tab++;

            AcceptButton = createBtn;
            CancelButton = cancelBtn;
        }

        private static TextBox MakeField(int x, int y, int width)
        {
            return new TextBox
            {
                Location = new Point(x, y + 20),
                Size = new Size(width, 28),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
        }

        private static Panel WrapField(string labelText, TextBox field, int height)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = height };
            var label = new Label
            {
                Text = labelText,
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            panel.Controls.Add(field);
            panel.Controls.Add(label);
            return panel;
        }

        private async void CreateBtn_Click(object? sender, EventArgs e)
        {
            _errorLabel.Text = string.Empty;

            // Client-side validation matching server rules
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
                    _roleCombo.SelectedItem as string ?? "Barista",
                    _usernameTxtBx.Text,
                    _passwordTxtBx.Text);

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