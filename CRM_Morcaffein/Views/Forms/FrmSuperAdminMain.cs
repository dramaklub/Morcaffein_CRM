using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;
using CRM_Morcaffein.winforms.Views.Controls;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// SuperAdmin shell. Same visual language as FrmMain, but with a
    /// SuperAdmin-only nav (Companies / Plans / Subscriptions) and no branch
    /// selector (SuperAdmins have no branch context).
    ///
    /// Opened by Program.cs when ApiConfig.IsSuperAdminSession is true.
    /// Logout closes with ClosedViaLogout = true so the outer loop returns
    /// to FrmLogin.
    /// </summary>
    public class FrmSuperAdminMain : Form
    {
        public bool ClosedViaLogout { get; private set; }

        private const string IconsFolder = "Resources/Icons";
        private const string BrandingFolder = "Resources/Branding";

        // ---- Chrome ----
        private Panel _sidebarPanel = null!;
        private Panel _contentPanel = null!;
        private Panel _footerPanel = null!;
        private Panel _avatarPanel = null!;
        private Label _staffNameLabel = null!;
        private Label _staffRoleLabel = null!;
        private Button _logoutBtn = null!;
        private Panel _topBarPanel = null!;
        private Panel _brandMarkPanel = null!;
        private Label _topBarBrandLabel = null!;
        private Label _topBarTabLabel = null!;

        public FrmSuperAdminMain()
        {
            Text = "Morcaffein CRM — SuperAdmin Console";
            WindowState = FormWindowState.Maximized;
            ClientSize = new Size(1374, 688);
            Font = BrandPalette.BrandFontRegular(9f);
            BackColor = BrandPalette.SurfaceSecondary;

            BuildLayout();
            BuildNavButtons();

            Load += FrmSuperAdminMain_Load;
        }

        private void BuildLayout()
        {
            // ---- Sidebar (right-docked) ----
            _sidebarPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 220,
                BackColor = BrandPalette.NavBackground,
            };

            // ---- Footer inside the sidebar ----
            _footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 118,
                BackColor = BrandPalette.NavFooterBackground,
                Padding = new Padding(18, 14, 18, 14),
            };

            _avatarPanel = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(18, 14),
                BackColor = Color.Transparent,
            };
            _avatarPanel.Paint += AvatarPanel_Paint;

            _staffNameLabel = new Label
            {
                Text = ApiConfig.SuperAdminName ?? "SuperAdmin",
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(9.5f),
                ForeColor = BrandPalette.NeutralText,
                Location = new Point(66, 18),
            };

            _staffRoleLabel = new Label
            {
                Text = "SuperAdmin",
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Location = new Point(66, 38),
            };

            _logoutBtn = new Button
            {
                Text = "Log Out",
                Size = new Size(184, 32),
                Location = new Point(18, 66),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.White,
                ForeColor = BrandPalette.NeutralText,
                Font = BrandPalette.BrandFontRegular(9f),
                Cursor = Cursors.Hand,
            };
            _logoutBtn.FlatAppearance.BorderSize = 1;
            _logoutBtn.FlatAppearance.BorderColor = BrandPalette.Border;
            _logoutBtn.Click += LogoutBtn_Click;

            _footerPanel.Controls.Add(_logoutBtn);
            _footerPanel.Controls.Add(_staffRoleLabel);
            _footerPanel.Controls.Add(_staffNameLabel);
            _footerPanel.Controls.Add(_avatarPanel);

            _sidebarPanel.Controls.Add(_footerPanel);

            // ---- Content panel ----
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
            };

            // ---- Topbar ----
            _topBarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = BrandPalette.TopBarBackground,
            };

            var topBarBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BrandPalette.TopBarBorder,
            };

            _brandMarkPanel = new Panel
            {
                Size = new Size(32, 32),
                Location = new Point(22, 14),
                BackColor = Color.Transparent,
            };
            LoadBrandMarkInto(_brandMarkPanel);

            _topBarBrandLabel = new Label
            {
                Text = "Morcaffein",
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(13.5f),
                ForeColor = BrandPalette.Brand,
                Location = new Point(64, 19),
            };

            _topBarTabLabel = new Label
            {
                Text = "/ SuperAdmin",
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(11f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Location = new Point(180, 22),
            };

            _topBarPanel.Controls.Add(_topBarTabLabel);
            _topBarPanel.Controls.Add(_topBarBrandLabel);
            _topBarPanel.Controls.Add(_brandMarkPanel);
            _topBarPanel.Controls.Add(topBarBorder);

            // Compose. Fill first, then edges, then topbar last so it claims
            // the full width above the sidebar.
            Controls.Add(_contentPanel);
            Controls.Add(_sidebarPanel);
            Controls.Add(_topBarPanel);
        }

        private void BuildNavButtons()
        {
            var navItems = new List<(string Text, string IconFile, Action onClick)>
            {
                ("Dashboard",            "Dashboard", () => ShowView(new SuperAdminDashboardView())),
                ("Companies",            "Customers", () => ShowView(new SuperAdminCompaniesView())),
                ("Terms",   "Approvals", () => ShowView(new TermsAndConditionsView())),
            };

            int y = 20;
            const int buttonWidth = 184;
            const int buttonHeight = 46;
            const int gap = 12;

            foreach (var (text, iconFile, onClick) in navItems)
            {
                var btn = CreateNavButton(text, iconFile, buttonWidth, buttonHeight);
                btn.Location = new Point(18, y);

                var capturedText = text;
                var capturedOnClick = onClick;
                btn.Click += (s, e) =>
                {
                    SetActiveNav(btn);
                    _topBarTabLabel.Text = "/ SuperAdmin / " + capturedText;
                    capturedOnClick();
                };

                _sidebarPanel.Controls.Add(btn);
                y += buttonHeight + gap;
            }
        }

        private static Button CreateNavButton(string text, string iconFile, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                TextAlign = ContentAlignment.MiddleLeft,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Font = BrandPalette.BrandFontRegular(10f),
                Cursor = Cursors.Hand,
                BackColor = BrandPalette.NavBackground,
                ForeColor = BrandPalette.NavText,
                Image = LoadIcon(iconFile),
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = BrandPalette.NavHoverBackground;
            return btn;
        }

        private void SetActiveNav(Button active)
        {
            foreach (var button in _sidebarPanel.Controls.OfType<Button>())
            {
                button.BackColor = button == active ? BrandPalette.NavActiveBackground : BrandPalette.NavBackground;
                button.ForeColor = button == active ? BrandPalette.NavActiveText : BrandPalette.NavText;
                button.Font = button == active ? BrandPalette.BrandFontBold(10f) : BrandPalette.BrandFontRegular(10f);
            }
        }

        private void FrmSuperAdminMain_Load(object? sender, EventArgs e)
        {
            if (_sidebarPanel.Controls.OfType<Button>().Any())
            {
                var firstNav = _sidebarPanel.Controls.OfType<Button>().First();
                firstNav.PerformClick();
            }
        }

        private void ShowView(UserControl view)
        {
            _contentPanel.Controls.Clear();
            view.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(view);
        }

        private void LogoutBtn_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(this,
                $"Log out of {ApiConfig.SuperAdminName ?? "this session"}?",
                "Log Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            ClosedViaLogout = true;
            ApiConfig.ClearAuthToken();
            Close();
        }

        private void AvatarPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var brush = new SolidBrush(BrandPalette.Brand);
            g.FillEllipse(brush, 0, 0, _avatarPanel.Width - 1, _avatarPanel.Height - 1);

            var initials = GetInitials(ApiConfig.SuperAdminName);
            using var font = BrandPalette.BrandFontBold(11f);
            var textSize = g.MeasureString(initials, font);
            g.DrawString(initials, font, Brushes.White,
                (_avatarPanel.Width - textSize.Width) / 2, (_avatarPanel.Height - textSize.Height) / 2);
        }

        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "SA";

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "SA",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant(),
            };
        }

        private static Image? LoadIcon(string filenameWithoutExtension)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, IconsFolder, filenameWithoutExtension + ".png");
                if (!File.Exists(path)) return null;

                var bytes = File.ReadAllBytes(path);
                using var ms = new MemoryStream(bytes);
                using var temp = Image.FromStream(ms);
                return new Bitmap(temp);
            }
            catch
            {
                return null;
            }
        }

        private static void LoadBrandMarkInto(Panel panel)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, BrandingFolder, "Top Bar Icon.png");
                if (!File.Exists(path)) return;

                var bytes = File.ReadAllBytes(path);
                using var ms = new MemoryStream(bytes);
                using var temp = Image.FromStream(ms);
                panel.BackgroundImage = new Bitmap(temp);
                panel.BackgroundImageLayout = ImageLayout.Zoom;
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.UserClosing && !ClosedViaLogout)
            {
                var confirm = MessageBox.Show(this,
                    "Are you sure you want to exit? Any unsaved progress will be lost.",
                    "Exit Morcaffein",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}