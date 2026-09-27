using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Modal for creating or editing a company's Admin account (the primary
    /// login the SuperAdmin provisions).
    ///
    /// Two modes:
    ///   Create — no Admin exists. All fields editable.
    ///   Edit   — Admin exists. Name/contact/email are read-only (they
    ///            belong to the tenant's own Manage Users surface). Only
    ///            username and password are editable, and password is
    ///            optional — leave it blank to keep the current one.
    ///
    /// Cross-DB writes happen server-side; this modal just calls the API
    /// and surfaces errors.
    /// </summary>
    public class FrmAdminAccountEdit : Form
    {
        private enum Mode { Create, Edit }

        private readonly SuperAdminApi _api;
        private readonly int _companyId;
        private readonly string _companyName;
        private readonly SuperAdminAdminAccountDto? _existing;
        private readonly Mode _mode;

        // Create-mode fields
        private TextBox _firstNameTxtBx = null!;
        private TextBox _lastNameTxtBx = null!;
        private TextBox _contactTxtBx = null!;
        private TextBox _emailTxtBx = null!;

        // Both modes
        private TextBox _usernameTxtBx = null!;
        private TextBox _passwordTxtBx = null!;
        private TextBox _confirmTxtBx = null!;

        private Label _errorLabel = null!;
        private Button _primaryBtn = null!;

        private const int FormWidth = 540;
        private const int PAD = 40;
        private const int FieldW = 412;

        public SuperAdminAdminAccountDto? Result { get; private set; }

        private FrmAdminAccountEdit(
            SuperAdminApi api,
            int companyId,
            string companyName,
            Mode mode,
            SuperAdminAdminAccountDto? existing)
        {
            _api = api;
            _companyId = companyId;
            _companyName = companyName;
            _mode = mode;
            _existing = existing;

            Text = mode == Mode.Create ? "Create Admin Account" : "Edit Admin Credentials";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(FormWidth, mode == Mode.Create ? 640 : 480);
            BackColor = BrandPalette.SurfaceCard;
            Font = BrandPalette.BrandFontRegular(9.5f);
            Padding = new Padding(24);

            BuildLayout();

            if (_mode == Mode.Edit && _existing is not null)
                PopulateFromExisting();
        }

        public static FrmAdminAccountEdit ForCreate(
            SuperAdminApi api, int companyId, string companyName)
            => new(api, companyId, companyName, Mode.Create, null);

        public static FrmAdminAccountEdit ForEdit(
            SuperAdminApi api, int companyId, string companyName, SuperAdminAdminAccountDto existing)
            => new(api, companyId, companyName, Mode.Edit, existing);

        private void BuildLayout()
        {
            var titleLabel = new Label
            {
                Text = _mode == Mode.Create ? "Create Admin Account" : "Edit Admin Credentials",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var subtitleLabel = new Label
            {
                Text = _mode == Mode.Create
                    ? $"Set up the primary Admin login for {_companyName}."
                    : $"Update the login for {_companyName}'s Admin account.",
                Location = new Point(0, 26),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            var formHost = new Panel
            {
                Location = new Point(0, 66),
                Size = new Size(FormWidth - 48, ClientSize.Height - 66 - 56 - 24),
                BackColor = BrandPalette.SurfaceCard,
            };

            const int labelH = 18;
            const int fieldH = 30;
            const int rowH = 66;
            int y = 0;

            void AddLabel(string text, int x = PAD)
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

            TextBox AddField(string labelText, bool password = false)
            {
                AddLabel(labelText);
                var txt = new TextBox
                {
                    Location = new Point(PAD, y + labelH),
                    Size = new Size(FieldW, fieldH),
                    Font = BrandPalette.BrandFontRegular(9.5f),
                    BorderStyle = BorderStyle.FixedSingle,
                    UseSystemPasswordChar = password,
                };
                formHost.Controls.Add(txt);
                y += rowH;
                return txt;
            }

            if (_mode == Mode.Create)
            {
                // Full-detail capture — the SuperAdmin is provisioning the
                // employee record from scratch.
                _firstNameTxtBx = AddField("First name *");
                _lastNameTxtBx = AddField("Last name *");
                _contactTxtBx = AddField("Contact number * (10-11 digits)");

                // Digits-only enforcement on contact.
                _contactTxtBx.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                        e.Handled = true;
                };

                _emailTxtBx = AddField("Email (optional)");
            }
            else
            {
                // Read-only header block showing the current Admin's details.
                var infoLabel = new Label
                {
                    Location = new Point(PAD, 0),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontBold(10f),
                    ForeColor = BrandPalette.Brand,
                    BackColor = Color.Transparent,
                    Text = _existing?.FullName ?? "—",
                };
                formHost.Controls.Add(infoLabel);

                var contactLine = new Label
                {
                    Location = new Point(PAD, 22),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontRegular(9f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                    BackColor = Color.Transparent,
                    Text = (_existing?.ContactNumber ?? "—") +
                           (string.IsNullOrWhiteSpace(_existing?.Email) ? "" : $"   ·   {_existing!.Email}"),
                };
                formHost.Controls.Add(contactLine);

                var roleLine = new Label
                {
                    Location = new Point(PAD, 42),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontRegular(8.5f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                    BackColor = Color.Transparent,
                    Text = "Admin · read-only (employee details are managed by the tenant)",
                };
                formHost.Controls.Add(roleLine);

                y = 76;   // jump past the header block
            }

            _usernameTxtBx = AddField("Username * (min 3 chars)");

            _passwordTxtBx = AddField(
                _mode == Mode.Create
                    ? "Password * (min 4 chars)"
                    : "New password (leave blank to keep current)");

            _confirmTxtBx = AddField(
                _mode == Mode.Create
                    ? "Confirm password *"
                    : "Confirm new password");

            _errorLabel = new Label
            {
                Location = new Point(PAD, y),
                Size = new Size(FieldW, 26),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };
            formHost.Controls.Add(_errorLabel);

            // ---- Button row ----
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

            _primaryBtn = new Button
            {
                Text = _mode == Mode.Create ? "Create Account" : "Save Changes",
                Size = new Size(160, 36),
                Location = new Point(btnRow.Width - 120 - 8 - 160, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _primaryBtn.FlatAppearance.BorderSize = 0;
            _primaryBtn.Click += PrimaryBtn_Click;

            btnRow.Controls.Add(_primaryBtn);
            btnRow.Controls.Add(cancelBtn);

            Controls.Add(formHost);
            Controls.Add(titleLabel);
            Controls.Add(subtitleLabel);
            Controls.Add(btnRow);

            AcceptButton = _primaryBtn;
            CancelButton = cancelBtn;
        }

        private void PopulateFromExisting()
        {
            if (_existing is null) return;
            _usernameTxtBx.Text = _existing.Username ?? string.Empty;
        }

        private async void PrimaryBtn_Click(object? sender, EventArgs e)
        {
            _errorLabel.Text = string.Empty;

            var username = _usernameTxtBx.Text.Trim();
            var password = _passwordTxtBx.Text;
            var confirm = _confirmTxtBx.Text;

            if (username.Length < 3)
            {
                _errorLabel.Text = "Username must be at least 3 characters.";
                return;
            }

            if (_mode == Mode.Create)
            {
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

                if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
                {
                    _errorLabel.Text = "Password must be at least 4 characters.";
                    return;
                }

                if (password != confirm)
                {
                    _errorLabel.Text = "Passwords do not match.";
                    return;
                }

                try
                {
                    _primaryBtn.Enabled = false;
                    _primaryBtn.Text = "Creating…";

                    var request = new CreateCompanyAdminRequest
                    {
                        Username = username,
                        Password = password,
                        FirstName = _firstNameTxtBx.Text.Trim(),
                        LastName = _lastNameTxtBx.Text.Trim(),
                        ContactNumber = _contactTxtBx.Text.Trim(),
                        Email = string.IsNullOrWhiteSpace(_emailTxtBx.Text) ? null : _emailTxtBx.Text.Trim(),
                    };

                    Result = await _api.CreateAdminAsync(_companyId, request);

                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    _errorLabel.Text = ExtractApiMessage(ex.Message);
                }
                finally
                {
                    _primaryBtn.Enabled = true;
                    _primaryBtn.Text = "Create Account";
                }
            }
            else
            {
                // ---- Edit mode ----
                if (!string.IsNullOrEmpty(password))
                {
                    if (password.Length < 4)
                    {
                        _errorLabel.Text = "Password must be at least 4 characters.";
                        return;
                    }
                    if (password != confirm)
                    {
                        _errorLabel.Text = "Passwords do not match.";
                        return;
                    }
                }

                var usernameChanged =
                    !string.Equals(username, _existing?.Username, StringComparison.Ordinal);

                if (!usernameChanged && string.IsNullOrEmpty(password))
                {
                    _errorLabel.Text = "Nothing to update.";
                    return;
                }

                try
                {
                    _primaryBtn.Enabled = false;
                    _primaryBtn.Text = "Saving…";

                    var request = new UpdateCompanyAdminRequest
                    {
                        Username = usernameChanged ? username : null,
                        NewPassword = string.IsNullOrEmpty(password) ? null : password,
                    };

                    Result = await _api.UpdateAdminAsync(_companyId, request);

                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    _errorLabel.Text = ExtractApiMessage(ex.Message);
                }
                finally
                {
                    _primaryBtn.Enabled = true;
                    _primaryBtn.Text = "Save Changes";
                }
            }
        }

        /// <summary>
        /// Pulls the readable sentence out of the API's JSON error body
        /// ({"message":"..."}), same helper pattern used elsewhere.
        /// </summary>
        private static string ExtractApiMessage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Request failed.";

            const string marker = "\"message\":\"";
            var idx = raw.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return raw;

            var start = idx + marker.Length;
            var end = raw.IndexOf('"', start);
            if (end < 0) return raw;

            return raw.Substring(start, end - start);
        }
    }
}