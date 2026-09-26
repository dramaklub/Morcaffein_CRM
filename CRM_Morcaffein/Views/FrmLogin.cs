using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// Login screen. Two-pane layout: brand panel on the left (BLANK — fill in Figma),
    /// login form on the right. On success, stores the JWT in ApiConfig.AuthToken,
    /// seeds EmployeeSession, and closes with DialogResult.OK.
    /// </summary>
    public class FrmLogin : Form
    {
        private readonly SuperAdminAuthApi _superAdminApi = new();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _brandBackground?.Dispose();
                _brandBackground = null;
            }
            base.Dispose(disposing);
        }

        private Image? _brandBackground;
        private TextBox _usernameTxtBx = null!;
        private TextBox _passwordTxtBx = null!;
        private Button _loginBtn = null!;
        private Label _errorLabel = null!;

        // --- Brand pane dimensions for Figma handoff ---
        // The left pane is a Panel, Dock = Left, Width = 300px.
        // Its height = the form's client height = 440px.
        // So the design canvas for the brand pane is 300 x 440.
        private const int BrandPaneWidth = 300;
        private const int FormClientWidth = 760;
        private const int FormClientHeight = 440;

        public FrmLogin()
        {
            Text = "Morcaffein CRM — Sign In";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(FormClientWidth, FormClientHeight);
            BackColor = BrandPalette.SurfacePrimary;
            Font = BrandPalette.BrandFontRegular(9.5f);

            BuildLayout();
        }

        private void BuildLayout()
        {
            // ---------- LEFT PANE: brand (BLANK) ----------
            // Sized 300 x 440. Fill this in Figma, then we drop the designed
            // controls back in here. For now it's just a colored surface.
            var brandPane = new Panel
            {
                Dock = DockStyle.Left,
                Width = BrandPaneWidth,
                BackColor = BrandPalette.Brand,   // fallback if the image fails to load
            };

            // Load the branding background image. If it's missing, the pane just
            // keeps the solid brand color above — no crash.
            try
            {
                var imagePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Login Background.png");
                if (File.Exists(imagePath))
                    _brandBackground = Image.FromFile(imagePath);
            }
            catch
            {
                _brandBackground = null;   // image is optional
            }

            brandPane.Paint += (s, e) =>
            {
                if (_brandBackground is null) return;

                var g = e.Graphics;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(_brandBackground, new Rectangle(0, 0, brandPane.Width, brandPane.Height));
            };

            // ---------- RIGHT PANE: login form ----------
            var formPane = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                Padding = new Padding(60, 80, 60, 60),
            };

            var headingLabel = new Label
            {
                Text = "Morcaffein",
                Location = new Point(60, 70),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(26f),
                ForeColor = BrandPalette.Brand,
            };

            var hintLabel = new Label
            {
                Text = "Customer Retention & Loyalty System",
                Location = new Point(60, 112),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            var usernameLabel = new Label
            {
                Text = "Username",
                Location = new Point(60, 172),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _usernameTxtBx = new TextBox
            {
                Location = new Point(60, 192),
                Size = new Size(340, 28),
                Font = BrandPalette.BrandFontRegular(10f),
                BorderStyle = BorderStyle.FixedSingle,
            };

            var passwordLabel = new Label
            {
                Text = "Password",
                Location = new Point(60, 236),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _passwordTxtBx = new TextBox
            {
                Location = new Point(60, 256),
                Size = new Size(340, 28),
                Font = BrandPalette.BrandFontRegular(10f),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true,
            };

            _errorLabel = new Label
            {
                Location = new Point(60, 294),
                Size = new Size(340, 32),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _loginBtn = new Button
            {
                Text = "Sign In",
                Location = new Point(60, 330),
                Size = new Size(340, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(11f),
                Cursor = Cursors.Hand,
            };
            _loginBtn.FlatAppearance.BorderSize = 0;
            _loginBtn.Click += LoginBtn_Click;
            ButtonStyler.TrackEnabledState(_loginBtn);

            formPane.Controls.Add(_loginBtn);
            formPane.Controls.Add(_errorLabel);
            formPane.Controls.Add(_passwordTxtBx);
            formPane.Controls.Add(passwordLabel);
            formPane.Controls.Add(_usernameTxtBx);
            formPane.Controls.Add(usernameLabel);
            formPane.Controls.Add(hintLabel);
            formPane.Controls.Add(headingLabel);
            // Explicit tab order — visual top-to-bottom. The controls were added in reverse
            // order for correct layout, so the default TabIndex values are backwards.
            _usernameTxtBx.TabIndex = 0;
            _passwordTxtBx.TabIndex = 1;
            _loginBtn.TabIndex = 2;

            // ---------- COMPOSE ----------
            // Fill added first, then Left — so the brand pane docks to the left and
            // the form pane fills the rest.
            Controls.Add(formPane);
            Controls.Add(brandPane);

            // Enter key on either textbox submits
            _usernameTxtBx.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { LoginBtn_Click(this, EventArgs.Empty); e.SuppressKeyPress = true; }
            };
            _passwordTxtBx.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { LoginBtn_Click(this, EventArgs.Empty); e.SuppressKeyPress = true; }
            };
        }

        private async void LoginBtn_Click(object? sender, EventArgs e)
        {
            var username = _usernameTxtBx.Text.Trim();
            var password = _passwordTxtBx.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    this,
                    "Please enter both username and password.",
                    "Sign In",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _loginBtn.Enabled = false;
                _loginBtn.Text = "Signing in...";
                _errorLabel.Text = string.Empty;

                // Step 1 — try the tenant (regular staff) login first.
                var authApi = new AuthApi();
                var result = await authApi.LoginAsync(username, password);

                if (result.Success)
                {
                    // Tenant login succeeded — regular employee path.
                    var response = result.Data!;
                    ApiConfig.AuthToken = response.Token;
                    ApiConfig.IsSuperAdminSession = false;

                    var employee = new Employee
                    {
                        EmployeeId = response.EmployeeId,
                        FirstName = ExtractFirstName(response.FullName),
                        LastName = ExtractLastName(response.FullName),
                        Role = response.Role,
                        BranchId = response.BranchId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    EmployeeSession.Start(employee);

                    // Fetch entitlements before opening the shell so the nav is
                    // fully built the first time FrmMain paints. If this fails,
                    // the session still works — HasModule just returns false
                    // for everything, and the app behaves like a no-plan tenant.
                    try
                    {
                        var meApi = new MeApi();
                        EmployeeSession.Current!.Entitlements = await meApi.GetEntitlementsAsync();
                    }
                    catch
                    {
                        // Silent — failed entitlements fetch shouldn't block login.
                        // The nav will be empty, which is the correct "no plan" state.
                    }

                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                // Step 2 — tenant login failed. Try SuperAdmin login.
                // Same username/password; different endpoint, no tenant context.
                var superResult = await _superAdminApi.LoginAsync(username, password);

                if (superResult.Success)
                {
                    ApiConfig.AuthToken = superResult.Token;
                    ApiConfig.IsSuperAdminSession = true;

                    // SuperAdmins aren't employees — no EmployeeSession.Start().
                    // The shell reads SuperAdminName from ApiConfig instead.
                    ApiConfig.SuperAdminName = superResult.FullName;

                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                // Step 3 — both failed. Show the tenant login's error message,
                // since that's the more likely path a user is trying.
                MessageBox.Show(
                    this,
                    result.ErrorMessage ?? "Login failed.",
                    "Sign In",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                _passwordTxtBx.Text = string.Empty;
                _passwordTxtBx.Focus();
            }
            catch (Exception ex)
            {
                // Truly unexpected — still a popup, never a raw crash dialog.
                MessageBox.Show(
                    this,
                    $"An unexpected error occurred:\n\n{ex.Message}",
                    "Unexpected Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _loginBtn.Enabled = true;
                _loginBtn.Text = "Log In";
            }
        }

        private static string ExtractFirstName(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : fullName;
        }

        private static string ExtractLastName(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : string.Empty;
        }
    }
}