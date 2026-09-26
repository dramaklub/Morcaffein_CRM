using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views
{
    partial class FrmMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private Panel sidebarPanel;
        private Panel contentPanel;
        private Panel footerPanel;
        private Panel avatarPanel;
        private Label staffNameLabel;
        private Label staffRoleLabel;
        private Button logoutBtn;
        private Panel topBarPanel;
        private Panel topBarBorderPanel;
        private Panel brandMarkPanel;
        private Label topBarBrandLabel;
        private Label topBarTabLabel;
        private Panel branchSelectorPanel;
        private Label branchSelectorLabel;
        private ComboBox branchSelectorCombo;

        private void InitializeComponent()
        {
            sidebarPanel = new Panel();
            contentPanel = new Panel();
            footerPanel = new Panel();
            avatarPanel = new Panel();
            staffNameLabel = new Label();
            staffRoleLabel = new Label();
            logoutBtn = new Button();
            topBarPanel = new Panel();
            topBarBorderPanel = new Panel();
            brandMarkPanel = new Panel();
            topBarBrandLabel = new Label();
            topBarTabLabel = new Label();
            branchSelectorPanel = new Panel();
            branchSelectorLabel = new Label();
            branchSelectorCombo = new ComboBox();
            SuspendLayout();

            // sidebarPanel — right-docked, deliberately lighter than the rest of the UI.
            // Nav buttons are built DYNAMICALLY in FrmMain.cs based on the logged-in
            // employee's role — not hardcoded here.
            sidebarPanel.Dock = DockStyle.Right;
            sidebarPanel.Width = 220;
            sidebarPanel.BackColor = BrandPalette.NavBackground;

            // footerPanel — pinned to the bottom of the sidebar: avatar + name/role + logout.
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.Height = 118;
            footerPanel.BackColor = BrandPalette.NavFooterBackground;
            footerPanel.Padding = new Padding(18, 14, 18, 14);

            avatarPanel.Size = new Size(40, 40);
            avatarPanel.Location = new Point(18, 14);
            avatarPanel.BackColor = Color.Transparent;

            // Read-only — session info is set in FrmMain.cs from EmployeeSession.Current.
            staffNameLabel.AutoSize = true;
            staffNameLabel.Font = BrandPalette.BrandFontBold(9.5f);
            staffNameLabel.ForeColor = BrandPalette.NeutralText;
            staffNameLabel.Location = new Point(66, 18);

            staffRoleLabel.AutoSize = true;
            staffRoleLabel.Font = BrandPalette.BrandFontRegular(8.5f);
            staffRoleLabel.ForeColor = BrandPalette.NeutralTextMuted;
            staffRoleLabel.Location = new Point(66, 38);
            staffRoleLabel.Text = "Role";

            logoutBtn.Text = "Log Out";
            logoutBtn.Size = new Size(184, 32);
            logoutBtn.Location = new Point(18, 66);
            logoutBtn.FlatStyle = FlatStyle.Flat;
            logoutBtn.FlatAppearance.BorderSize = 1;
            logoutBtn.FlatAppearance.BorderColor = BrandPalette.Border;
            logoutBtn.BackColor = BrandPalette.White;
            logoutBtn.ForeColor = BrandPalette.NeutralText;
            logoutBtn.Font = BrandPalette.BrandFontRegular(9f);
            logoutBtn.Cursor = Cursors.Hand;

            footerPanel.Controls.Add(logoutBtn);
            footerPanel.Controls.Add(staffRoleLabel);
            footerPanel.Controls.Add(staffNameLabel);
            footerPanel.Controls.Add(avatarPanel);

            sidebarPanel.Controls.Add(footerPanel);

            // contentPanel
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = BrandPalette.SurfaceSecondary;

            // topBarPanel — thin brand strip (icon mark + name + active tab). Docked Top and
            // added LAST: WinForms docks in reverse z-order, so this claims the full form
            // width across the very top before sidebarPanel(Right) claims its column below it.
            topBarPanel.Dock = DockStyle.Top;
            topBarPanel.Height = 60;
            topBarPanel.BackColor = BrandPalette.TopBarBackground;

            topBarBorderPanel.Dock = DockStyle.Bottom;
            topBarBorderPanel.Height = 1;
            topBarBorderPanel.BackColor = BrandPalette.TopBarBorder;

            brandMarkPanel.Size = new Size(32, 32);
            brandMarkPanel.Location = new Point(22, 14);
            brandMarkPanel.BackColor = Color.Transparent;

            topBarBrandLabel.AutoSize = true;
            topBarBrandLabel.Font = BrandPalette.BrandFontBold(13.5f);
            topBarBrandLabel.ForeColor = BrandPalette.Brand;
            topBarBrandLabel.Location = new Point(64, 19);
            topBarBrandLabel.Text = "Morcaffein";

            topBarTabLabel.AutoSize = true;
            topBarTabLabel.Font = BrandPalette.BrandFontRegular(11f);
            topBarTabLabel.ForeColor = BrandPalette.NeutralTextMuted;
            topBarTabLabel.Location = new Point(180, 22);
            topBarTabLabel.Text = "/";

            // ---- Branch selector (right side of topbar) ----
            // A right-docked panel containing "Branch:" + a dropdown. Populated in
            // FrmMain.cs from the /branches endpoint after login. Baristas never see
            // this — the panel is hidden if the user's role doesn't allow branch switching.
            branchSelectorPanel.Dock = DockStyle.Right;
            branchSelectorPanel.Width = 240;
            branchSelectorPanel.BackColor = Color.Transparent;
            branchSelectorPanel.Padding = new Padding(0, 14, 22, 14);
            branchSelectorPanel.Location = new Point(320, 0);

            branchSelectorLabel.Text = "Branch:";
            branchSelectorLabel.AutoSize = true;
            branchSelectorLabel.Font = BrandPalette.BrandFontRegular(9f);
            branchSelectorLabel.ForeColor = BrandPalette.NeutralTextMuted;
            branchSelectorLabel.Location = new Point(0, 22);

            branchSelectorCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            branchSelectorCombo.Font = BrandPalette.BrandFontRegular(9.5f);
            branchSelectorCombo.Width = 170;
            branchSelectorCombo.Location = new Point(56, 18);

            branchSelectorPanel.Controls.Add(branchSelectorCombo);
            branchSelectorPanel.Controls.Add(branchSelectorLabel);

            topBarPanel.Controls.Add(branchSelectorPanel);
            topBarPanel.Controls.Add(topBarTabLabel);
            topBarPanel.Controls.Add(topBarBrandLabel);
            topBarPanel.Controls.Add(brandMarkPanel);
            topBarPanel.Controls.Add(topBarBorderPanel);

            // FrmMain — Fill (contentPanel) first, then edge-docked sidebarPanel, then
            // topBarPanel last so it overlaps above the sidebar.
            Controls.Add(contentPanel);
            Controls.Add(sidebarPanel);
            Controls.Add(topBarPanel);

            AutoScaleDimensions = new SizeF(8F, 18F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1374, 688);
            Font = new Font("Bahnschrift SemiLight", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Name = "FrmMain";
            Text = "Morcaffein CRM — Staff Console";
            WindowState = FormWindowState.Maximized;
            Load += FrmMain_Load;
            ResumeLayout(false);
        }
    }
}