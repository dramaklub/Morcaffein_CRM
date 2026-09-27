using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Views.Controls;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// Staff console shell: a right-docked nav strip and a content area that swaps in
    /// whichever tab is active. The nav items shown depend on the logged-in employee's
    /// role:
    ///   Barista → Transactions, Customers, Feedback
    ///   Manager → Dashboard, Campaign, Customers, Feedback
    ///   Admin   → Dashboard, Approvals, Manage Users
    ///
    /// Customers and Feedback reuse the same views across roles that have them.
    /// Placeholder tabs (Dashboard, Campaign, Approvals, Manage Users) use PlaceholderView.
    /// </summary>
    public partial class FrmMain : Form
    {
        public bool ClosedViaLogout { get; private set; }

        private const string IconsFolder = "Resources/Icons";
        private const string BrandingFolder = "Resources/Branding";

        public FrmMain()
        {
            InitializeComponent();

            var employee = EmployeeSession.Current!.Employee;
            staffNameLabel.Text = employee.FullName;
            staffRoleLabel.Text = employee.Role;
            avatarPanel.Paint += AvatarPanel_Paint;

            // Top bar brand mark — load the icon PNG into the panel as a background.
            LoadBrandMarkInto(brandMarkPanel);

            BuildNavButtons(employee.Role);

            logoutBtn.Click += LogoutBtn_Click;
        }

        private async void FrmMain_Load(object sender, EventArgs e)
        {
            await LoadBranchesAsync();

            if (sidebarPanel.Controls.OfType<Button>().Any())
            {
                var firstNav = sidebarPanel.Controls.OfType<Button>().First();
                firstNav.PerformClick();
            }
        }

        /// <summary>
        /// Fetches the branch list and populates the topbar selector. Called once at
        /// startup after login. Baristas get the selector hidden — they're auto-pinned
        /// to their own branch. Managers and Admins can switch, including an "All Branches"
        /// option.
        /// </summary>
        private async Task LoadBranchesAsync()
        {
            var role = EmployeeSession.Current?.Employee?.Role ?? "";
            var hasBranching = EmployeeSession.Current?.HasModule("Branching") == true;

            // No Branching entitlement → no selector, and force-session to
            // the caller's own branch (or fall back to branch 1) so their API
            // calls are still scoped even though they can't switch.
            if (!hasBranching)
            {
                branchSelectorPanel.Visible = false;
                EmployeeSession.Current!.CurrentBranchId =
                    EmployeeSession.Current.Employee.BranchId ?? 1;
                return;
            }

            // Baristas are pinned to their own branch and don't get the selector.
            if (role.Equals("Barista", StringComparison.OrdinalIgnoreCase))
            {
                branchSelectorPanel.Visible = false;
                EmployeeSession.Current!.CurrentBranchId = EmployeeSession.Current.Employee.BranchId;
                return;
            }

            try
            {
                var api = new CRM_Morcaffein.winforms.Services.Api.BranchApi();
                var branches = await api.GetBranchesAsync();

                branchSelectorCombo.Items.Clear();
                branchSelectorCombo.Items.Add(new BranchItem(null, "All Branches"));
                foreach (var b in branches)
                    branchSelectorCombo.Items.Add(new BranchItem(b.BranchId, b.BranchName));

                branchSelectorCombo.DisplayMember = "Display";
                branchSelectorCombo.SelectedIndex = 0;   // default = All Branches

                branchSelectorCombo.SelectedIndexChanged += BranchSelectorCombo_SelectedIndexChanged;
            }
            catch (Exception ex)
            {
                // If branches fail to load, hide the selector and default to all branches.
                branchSelectorPanel.Visible = false;
                EmployeeSession.Current!.CurrentBranchId = null;
                System.Diagnostics.Debug.WriteLine($"Branch load failed: {ex.Message}");
            }
        }

        private void BranchSelectorCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (branchSelectorCombo.SelectedItem is not BranchItem item)
                return;

            EmployeeSession.Current!.CurrentBranchId = item.BranchId;

            // Reload the currently-active view so it picks up the new branch.
            // We don't cache the active view factory, so re-click the active nav button.
            var activeNav = sidebarPanel.Controls.OfType<Button>()
                .FirstOrDefault(b =>
                    b.BackColor == BrandPalette.NavActiveBackground);
            activeNav?.PerformClick();
        }

        /// <summary>
        /// Small wrapper so the dropdown can hold both "All Branches" (BranchId = null)
        /// and specific branches (BranchId = N) without nullable binding pain.
        /// </summary>
        private sealed class BranchItem
        {
            public int? BranchId { get; }
            public string Display { get; }
            public BranchItem(int? id, string display) { BranchId = id; Display = display; }
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
                    return;
                }
            }
        }

        // =====================================================================
        // NAV CONSTRUCTION
        // =====================================================================

        private void BuildNavButtons(string role)
        {
            var isManager = role.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                            role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                            role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
            var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                          role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

            var session = EmployeeSession.Current;

            // Every nav item is added through a small helper that checks
            // whether the tenant's plan includes the relevant module. If
            // entitlements never loaded (session.Entitlements is null) or
            // the plan doesn't include the module, HasModule returns false
            // and the item just doesn't render.
            var navItems = new List<(string Text, string IconFile, Action onClick)>();

            void AddIfEntitled(string text, string iconFile, string moduleKey, Func<UserControl> factory)
            {
                if (session?.HasModule(moduleKey) != true) return;
                navItems.Add((text, iconFile, () => ShowView(factory())));
            }

            void AddAlways(string text, string iconFile, Func<UserControl> factory)
            {
                navItems.Add((text, iconFile, () => ShowView(factory())));
            }

            if (isAdmin)
            {
                // Admin-only nav. Dashboard is gated by Dashboard entitlement
                // (present in all tiers). Approvals is gated by Actions —
                // no campaigns to approve without Retention/Actions.
                // Manage Users is fundamental and always present.
                AddIfEntitled("Dashboard", "Dashboard", "Dashboard", () =>
                {
                    var dashboardView = new AdminDashboardView();

                    dashboardView.NavigateToApprovalsRequested += (s, e) =>
                    {
                        var approvalsBtn = sidebarPanel.Controls.OfType<Button>()
                            .FirstOrDefault(b => b.Text == "Approvals");
                        approvalsBtn?.PerformClick();
                    };

                    dashboardView.NavigateToManageUsersRequested += (s, e) =>
                    {
                        var usersBtn = sidebarPanel.Controls.OfType<Button>()
                            .FirstOrDefault(b => b.Text == "Manage Users");
                        usersBtn?.PerformClick();
                    };

                    return dashboardView;
                });

                if (session?.HasModule("Actions") == true)
                {
                    AddAlways("Approvals", "Approvals", () => new ApprovalsView());
                }

                // Branch management — gated by the Branching module. Only
                // Full-tier tenants have it. Placed before Manage Users so
                // the two "organizational setup" surfaces sit together.
                AddIfEntitled("Branches", "Customers", "Branching", () => new BranchManagementView());

                AddAlways("Manage Users", "User Mgmt", () => new ManageUsersView());
            }
            else if (isManager)
            {
                AddIfEntitled("Dashboard", "Dashboard", "Dashboard", () =>
                {
                    var dashboardView = new ManagerDashboardView();
                    dashboardView.NavigateToFeedbackRequested += (s, e) =>
                    {
                        var feedbackBtn = sidebarPanel.Controls.OfType<Button>()
                            .FirstOrDefault(b => b.Text == "Feedback");
                        feedbackBtn?.PerformClick();
                    };
                    return dashboardView;
                });
                AddIfEntitled("Campaign", "Campaign", "Actions", () => new CampaignManagementView());
                AddIfEntitled("Customers", "Customers", "DataCollection", () => new CustomerManagementView());
                AddIfEntitled("Feedback", "Feedback", "Feedback", () => new FeedbackManagementView());
            }
            else
            {
                AddIfEntitled("Transactions", "Cart", "Transactions", () => new TransactionView());
                AddIfEntitled("Customers", "Customers", "DataCollection", () => new CustomerManagementView());
                AddIfEntitled("Feedback", "Feedback", "Feedback", () => new FeedbackManagementView());
            }

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
                    topBarTabLabel.Text = "/ " + capturedText;
                    capturedOnClick();
                };

                sidebarPanel.Controls.Add(btn);
                y += buttonHeight + gap;
            }

            // If no nav items rendered at all (tenant has no plan, or plan
            // includes nothing useful for this role), show an explanatory
            // message in the content area.
            if (navItems.Count == 0)
            {
                contentPanel.Controls.Clear();
                var msg = new Label
                {
                    Text = "No subscription — contact the platform operator.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = BrandPalette.BrandFontRegular(11f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                };
                contentPanel.Controls.Add(msg);
            }
        }

        /// <summary>
        /// Builds a nav button that renders [icon] [label] left-aligned.
        ///
        /// WinForms does not expose a "gap between image and text" property, so we
        /// compose the button's content manually: the Image is placed via ImageAlign
        /// (MiddleLeft), and the text is offset from the left edge with Padding.Left.
        /// The icon itself sits at the far left because ImageAlign puts it before the
        /// padded text region.
        /// </summary>
        private static Button CreateNavButton(string text, string iconFile, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                // TextImageRelation is what actually sequences [icon] [text] with a gap.
                TextImageRelation = TextImageRelation.ImageBeforeText,
                TextAlign = ContentAlignment.MiddleLeft,
                ImageAlign = ContentAlignment.MiddleLeft,
                // A modest left inset for the whole icon+text pair; the internal gap between
                // them is handled by WinForms once TextImageRelation is set.
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
            foreach (var button in sidebarPanel.Controls.OfType<Button>())
            {
                button.BackColor = button == active ? BrandPalette.NavActiveBackground : BrandPalette.NavBackground;
                button.ForeColor = button == active ? BrandPalette.NavActiveText : BrandPalette.NavText;
                button.Font = button == active ? BrandPalette.BrandFontBold(10f) : BrandPalette.BrandFontRegular(10f);
            }
        }

        // =====================================================================
        // ICON / IMAGE LOADING
        // =====================================================================

        private static Image? LoadIcon(string filenameWithoutExtension)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, IconsFolder, filenameWithoutExtension + ".png");
                if (!File.Exists(path))
                    return null;

                // Load from bytes to avoid GDI+ path caching and to release the file
                // handle immediately (so the file isn't locked while the app runs).
                var bytes = File.ReadAllBytes(path);
                using var ms = new MemoryStream(bytes);
                using var temp = Image.FromStream(ms);
                return new Bitmap(temp);
            }
            catch
            {
                return null;   // missing/corrupt file → no icon
            }
        }

        private static void LoadBrandMarkInto(Panel panel)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, BrandingFolder, "Top Bar Icon.png");
                if (!File.Exists(path))
                    return;

                var bytes = File.ReadAllBytes(path);
                using var ms = new MemoryStream(bytes);
                using var temp = Image.FromStream(ms);
                panel.BackgroundImage = new Bitmap(temp);
                panel.BackgroundImageLayout = ImageLayout.Zoom;
            }
            catch
            {
                // Silently skip — panel just renders empty if the brand mark isn't found.
            }
        }

        private void ShowView(UserControl view)
        {
            contentPanel.Controls.Clear();
            view.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(view);
        }

        private void LogoutBtn_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(this,
                $"Log out of {EmployeeSession.Current?.DisplayName}'s session?",
                "Log Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            ClosedViaLogout = true;
            EmployeeSession.End();
            Close();
        }

        private void AvatarPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var brush = new SolidBrush(BrandPalette.Brand);
            g.FillEllipse(brush, 0, 0, avatarPanel.Width - 1, avatarPanel.Height - 1);

            var initials = GetInitials(EmployeeSession.Current?.DisplayName);
            using var font = BrandPalette.BrandFontBold(11f);
            var textSize = g.MeasureString(initials, font);
            g.DrawString(initials, font, Brushes.White,
                (avatarPanel.Width - textSize.Width) / 2, (avatarPanel.Height - textSize.Height) / 2);
        }

        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "?";

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant(),
            };
        }
    }
}