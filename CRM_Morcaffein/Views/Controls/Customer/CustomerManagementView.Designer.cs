using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    partial class CustomerManagementView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private Panel bodyPanel;
        private Panel cardOuterPanel;
        private Panel tableCardPanel;
        private Panel cardHeaderPanel;
        private Panel cardDividerPanel;
        private TextBox searchTxtBx;
        private Button newCustomerBtn;
        private DataGridView customersGrid;
        private Panel drawerPanel;
        private Label drawerEmptyLabel;
        private Panel drawerContentPanel;
        private FlowLayoutPanel drawerFlow;

        private Panel profileBoxPanel;

        private Label customerNameLabel;
        private Label tierBadgeLabel;
        private Label pointsBalanceLabel;
        private Label contactLabel;
        private Label orderHistoryHeaderLabel;
        private DataGridView orderHistoryGrid;
        private Label ledgerHeaderLabel;
        private DataGridView ledgerGrid;
        private Label adjustmentRequestsHeaderLabel;
        private DataGridView adjustmentRequestsGrid;
        private Panel requestSectionPanel;
        private Label requestHeaderLabel;
        private NumericUpDown requestAmountNud;
        private TextBox requestReasonTxtBx;
        private Button submitRequestBtn;
        private Label requestStatusLabel;
        private Panel approvalSectionPanel;
        private Label approvalHeaderLabel;
        private Label approvalHintLabel;
        private TextBox rejectionNotesTxtBx;
        private Button approveBtn;
        private Button rejectBtn;
        private Label approvalStatusLabel;

        private void InitializeComponent()
        {
            bodyPanel = new Panel();
            cardOuterPanel = new Panel();
            tableCardPanel = new Panel();
            cardHeaderPanel = new Panel();
            cardDividerPanel = new Panel();
            searchTxtBx = new TextBox();
            newCustomerBtn = new Button();
            customersGrid = new DataGridView();
            pagination = new PaginationControl();
            drawerPanel = new Panel();
            drawerEmptyLabel = new Label();
            drawerContentPanel = new Panel();
            drawerFlow = new FlowLayoutPanel();
            profileBoxPanel = new Panel();
            customerNameLabel = new Label();
            tierBadgeLabel = new Label();
            pointsBalanceLabel = new Label();
            contactLabel = new Label();
            orderHistoryHeaderLabel = new Label();
            orderHistoryGrid = new DataGridView();
            ledgerHeaderLabel = new Label();
            ledgerGrid = new DataGridView();
            adjustmentRequestsHeaderLabel = new Label();
            adjustmentRequestsGrid = new DataGridView();
            requestSectionPanel = new Panel();
            requestHeaderLabel = new Label();
            requestAmountNud = new NumericUpDown();
            requestReasonTxtBx = new TextBox();
            submitRequestBtn = new Button();
            requestStatusLabel = new Label();
            approvalSectionPanel = new Panel();
            approvalHeaderLabel = new Label();
            approvalHintLabel = new Label();
            rejectionNotesTxtBx = new TextBox();
            approveBtn = new Button();
            rejectBtn = new Button();
            approvalStatusLabel = new Label();

            ((System.ComponentModel.ISupportInitialize)customersGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)orderHistoryGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ledgerGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)adjustmentRequestsGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)requestAmountNud).BeginInit();
            SuspendLayout();

            // bodyPanel — the page; drawerPanel sits edge-to-edge on the left, the table is
            // a padded/boxed card in the remaining space (not full-bleed).
            bodyPanel.Dock = DockStyle.Fill;
            bodyPanel.BackColor = BrandPalette.SurfaceSecondary;

            // cardOuterPanel — just provides the page margin around the table card
            cardOuterPanel.Dock = DockStyle.Fill;
            cardOuterPanel.BackColor = BrandPalette.SurfaceSecondary;
            cardOuterPanel.Padding = new Padding(24);

            // tableCardPanel — the boxed table itself
            tableCardPanel.Dock = DockStyle.Fill;
            tableCardPanel.BackColor = BrandPalette.SurfacePrimary;
            tableCardPanel.BorderStyle = BorderStyle.FixedSingle;

            // cardHeaderPanel — search top-left, primary action top-right, inside the card
            cardHeaderPanel.Dock = DockStyle.Top;
            cardHeaderPanel.Height = 60;
            cardHeaderPanel.BackColor = BrandPalette.SurfacePrimary;

            // searchTxtBx
            searchTxtBx.Location = new Point(16, 16);
            searchTxtBx.Size = new Size(300, 28);
            searchTxtBx.PlaceholderText = "Search by name, phone, or ID";
            searchTxtBx.Font = BrandPalette.BrandFontRegular(10f);

            // newCustomerBtn — repositioned to hug the card's right edge by
            // CustomerManagementView.PositionActionButton (anchors are unreliable here since
            // this card is built and sized entirely in code, not the visual designer).
            newCustomerBtn.Text = "+ New Customer";
            newCustomerBtn.Size = new Size(150, 32);
            newCustomerBtn.Location = new Point(16, 14);
            newCustomerBtn.FlatStyle = FlatStyle.Flat;
            newCustomerBtn.FlatAppearance.BorderSize = 0;
            newCustomerBtn.BackColor = BrandPalette.ButtonPrimaryFill;
            newCustomerBtn.ForeColor = BrandPalette.ButtonPrimaryText;
            newCustomerBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            newCustomerBtn.Cursor = Cursors.Hand;

            // cardDividerPanel — thin separator between the card's header row and the grid
            cardDividerPanel.Dock = DockStyle.Bottom;
            cardDividerPanel.Height = 1;
            cardDividerPanel.BackColor = BrandPalette.Border;

            cardHeaderPanel.Controls.Add(newCustomerBtn);
            cardHeaderPanel.Controls.Add(searchTxtBx);
            cardHeaderPanel.Controls.Add(cardDividerPanel);

            // customersGrid
            customersGrid.Dock = DockStyle.Fill;
            ConfigureGrid(customersGrid, headerSize: 9.5f, rowHeight: 30);
            customersGrid.BorderStyle = BorderStyle.None;

            // tableCardPanel composition: Fill (customersGrid) added first, Top (cardHeaderPanel) added after.
            tableCardPanel.Controls.Add(customersGrid);
            tableCardPanel.Controls.Add(cardHeaderPanel);
            tableCardPanel.Controls.Add(pagination);
            cardOuterPanel.Controls.Add(tableCardPanel);

            // drawerPanel — left of the card (details for whatever row is clicked appear here)
            drawerPanel.Dock = DockStyle.Left;
            drawerPanel.Width = 380;
            drawerPanel.BackColor = BrandPalette.SurfaceCard;

            // drawerEmptyLabel
            drawerEmptyLabel.Dock = DockStyle.Fill;
            drawerEmptyLabel.TextAlign = ContentAlignment.MiddleCenter;
            drawerEmptyLabel.ForeColor = BrandPalette.NeutralTextMuted;
            drawerEmptyLabel.Font = BrandPalette.BrandFontRegular(10f);
            drawerEmptyLabel.Text = "Select a customer to view details.";

            // drawerContentPanel
            drawerContentPanel.Dock = DockStyle.Fill;
            drawerContentPanel.Visible = false;
            drawerContentPanel.AutoScroll = true;

            // drawerFlow
            drawerFlow.Dock = DockStyle.Top;
            drawerFlow.FlowDirection = FlowDirection.TopDown;
            drawerFlow.WrapContents = false;
            drawerFlow.AutoSize = true;
            drawerFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            drawerFlow.Padding = new Padding(20);
            drawerFlow.Width = 380;

            // customerNameLabel
            customerNameLabel.AutoSize = true;
            customerNameLabel.Font = BrandPalette.BrandFontBold(13f);
            customerNameLabel.ForeColor = BrandPalette.NeutralText;
            customerNameLabel.Margin = new Padding(0, 0, 0, 6);

            // tierBadgeLabel — plain text, no color coding (tiers are labeled, not colored)
            tierBadgeLabel.AutoSize = true;
            tierBadgeLabel.ForeColor = BrandPalette.NeutralTextMuted;
            tierBadgeLabel.Font = BrandPalette.BrandFontBold(9f);
            tierBadgeLabel.Margin = new Padding(0, 0, 0, 8);

            // pointsBalanceLabel
            pointsBalanceLabel.AutoSize = true;
            pointsBalanceLabel.Font = BrandPalette.BrandFontRegular(10f);
            pointsBalanceLabel.ForeColor = BrandPalette.NeutralText;
            pointsBalanceLabel.Margin = new Padding(0, 0, 0, 4);

            // contactLabel
            contactLabel.AutoSize = true;
            contactLabel.Font = BrandPalette.BrandFontRegular(9f);
            contactLabel.ForeColor = BrandPalette.NeutralTextMuted;
            contactLabel.Margin = new Padding(0, 0, 0, 18);

            // orderHistoryHeaderLabel
            orderHistoryHeaderLabel.AutoSize = true;
            orderHistoryHeaderLabel.Font = BrandPalette.BrandFontBold(10f);
            orderHistoryHeaderLabel.ForeColor = BrandPalette.NeutralText;
            orderHistoryHeaderLabel.Text = "Order History";
            orderHistoryHeaderLabel.Margin = new Padding(0, 0, 0, 6);

            // orderHistoryGrid
            ConfigureGrid(orderHistoryGrid, headerSize: 8.5f, rowHeight: 22);
            orderHistoryGrid.BorderStyle = BorderStyle.FixedSingle;
            orderHistoryGrid.Size = new Size(340, 150);
            orderHistoryGrid.Margin = new Padding(0, 0, 0, 18);

            // ledgerHeaderLabel
            ledgerHeaderLabel.AutoSize = true;
            ledgerHeaderLabel.Font = BrandPalette.BrandFontBold(10f);
            ledgerHeaderLabel.ForeColor = BrandPalette.NeutralText;
            ledgerHeaderLabel.Text = "Points Ledger";
            ledgerHeaderLabel.Margin = new Padding(0, 0, 0, 6);

            // ledgerGrid
            ConfigureGrid(ledgerGrid, headerSize: 8.5f, rowHeight: 22);
            ledgerGrid.BorderStyle = BorderStyle.FixedSingle;
            ledgerGrid.Size = new Size(340, 150);
            ledgerGrid.Margin = new Padding(0, 0, 0, 18);

            // adjustmentRequestsHeaderLabel
            adjustmentRequestsHeaderLabel.AutoSize = true;
            adjustmentRequestsHeaderLabel.Font = BrandPalette.BrandFontBold(10f);
            adjustmentRequestsHeaderLabel.ForeColor = BrandPalette.NeutralText;
            adjustmentRequestsHeaderLabel.Text = "Points Adjustment Requests";
            adjustmentRequestsHeaderLabel.Margin = new Padding(0, 0, 0, 6);

            // adjustmentRequestsGrid
            ConfigureGrid(adjustmentRequestsGrid, headerSize: 8.5f, rowHeight: 22);
            adjustmentRequestsGrid.BorderStyle = BorderStyle.FixedSingle;
            adjustmentRequestsGrid.Size = new Size(340, 130);
            adjustmentRequestsGrid.Margin = new Padding(0, 0, 0, 14);

            // requestSectionPanel — Manager+ only: propose a new adjustment (Pending until an admin decides)
            requestSectionPanel.Size = new Size(340, 182);
            requestSectionPanel.Visible = false;
            requestSectionPanel.Margin = new Padding(0, 0, 0, 14);

            requestHeaderLabel.AutoSize = true;
            requestHeaderLabel.Font = BrandPalette.BrandFontBold(9.5f);
            requestHeaderLabel.ForeColor = BrandPalette.NeutralText;
            requestHeaderLabel.Location = new Point(0, 0);
            requestHeaderLabel.Text = "Request Point Adjustment";

            requestAmountNud.Minimum = -100000;
            requestAmountNud.Maximum = 100000;
            requestAmountNud.Location = new Point(0, 22);
            requestAmountNud.Size = new Size(340, 26);
            requestAmountNud.Font = BrandPalette.BrandFontRegular(9.5f);
            requestAmountNud.TextAlign = HorizontalAlignment.Center;

            requestReasonTxtBx.Location = new Point(0, 52);
            requestReasonTxtBx.Size = new Size(340, 60);           // taller: 26 → 60
            requestReasonTxtBx.Multiline = true;                    // multi-line
            requestReasonTxtBx.ScrollBars = ScrollBars.Vertical;    // scroll if content overflows
            requestReasonTxtBx.PlaceholderText = "Reason (required)";
            requestReasonTxtBx.Font = BrandPalette.BrandFontRegular(9.5f);

            submitRequestBtn.Text = "Submit for Approval";
            submitRequestBtn.Location = new Point(0, 116);
            submitRequestBtn.Size = new Size(340, 32);
            submitRequestBtn.FlatStyle = FlatStyle.Flat;
            submitRequestBtn.FlatAppearance.BorderSize = 0;
            submitRequestBtn.BackColor = BrandPalette.ButtonPrimaryFill;
            submitRequestBtn.ForeColor = BrandPalette.ButtonPrimaryText;
            submitRequestBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            submitRequestBtn.Cursor = Cursors.Hand;

            requestStatusLabel.AutoSize = false;
            requestStatusLabel.Location = new Point(0, 152);
            requestStatusLabel.Size = new Size(340, 28);
            requestStatusLabel.Font = BrandPalette.BrandFontRegular(8.5f);
            requestStatusLabel.ForeColor = BrandPalette.NeutralTextMuted;

            requestSectionPanel.Controls.Add(requestStatusLabel);
            requestSectionPanel.Controls.Add(submitRequestBtn);
            requestSectionPanel.Controls.Add(requestReasonTxtBx);
            requestSectionPanel.Controls.Add(requestAmountNud);
            requestSectionPanel.Controls.Add(requestHeaderLabel);

            // approvalSectionPanel — Admin+ only: decide on the request selected in the grid above
            approvalSectionPanel.Size = new Size(340, 160);
            approvalSectionPanel.Visible = false;

            approvalHeaderLabel.AutoSize = true;
            approvalHeaderLabel.Font = BrandPalette.BrandFontBold(9.5f);
            approvalHeaderLabel.ForeColor = BrandPalette.NeutralText;
            approvalHeaderLabel.Location = new Point(0, 0);
            approvalHeaderLabel.Text = "Approve / Reject Selected Request";

            approvalHintLabel.AutoSize = true;
            approvalHintLabel.Font = BrandPalette.BrandFontRegular(8f);
            approvalHintLabel.ForeColor = BrandPalette.NeutralTextMuted;
            approvalHintLabel.Location = new Point(0, 20);
            approvalHintLabel.Text = "Select a pending request above.";

            rejectionNotesTxtBx.Location = new Point(0, 42);
            rejectionNotesTxtBx.Size = new Size(340, 26);
            rejectionNotesTxtBx.PlaceholderText = "Rejection reason (required to reject)";
            rejectionNotesTxtBx.Font = BrandPalette.BrandFontRegular(9.5f);

            approveBtn.Text = "Approve";
            approveBtn.Location = new Point(0, 72);
            approveBtn.Size = new Size(164, 32);
            approveBtn.FlatStyle = FlatStyle.Flat;
            approveBtn.FlatAppearance.BorderSize = 0;
            approveBtn.BackColor = BrandPalette.ButtonPrimaryFill;
            approveBtn.ForeColor = BrandPalette.ButtonPrimaryText;
            approveBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            approveBtn.Cursor = Cursors.Hand;

            rejectBtn.Text = "Reject";
            rejectBtn.Location = new Point(176, 72);
            rejectBtn.Size = new Size(164, 32);
            rejectBtn.FlatStyle = FlatStyle.Flat;
            rejectBtn.FlatAppearance.BorderSize = 0;
            rejectBtn.BackColor = BrandPalette.ButtonSecondaryFill;
            rejectBtn.ForeColor = BrandPalette.ButtonSecondaryText;
            rejectBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            rejectBtn.Cursor = Cursors.Hand;

            approvalStatusLabel.AutoSize = false;
            approvalStatusLabel.Location = new Point(0, 108);
            approvalStatusLabel.Size = new Size(340, 28);
            approvalStatusLabel.Font = BrandPalette.BrandFontRegular(8.5f);
            approvalStatusLabel.ForeColor = BrandPalette.NeutralTextMuted;

            approvalSectionPanel.Controls.Add(approvalStatusLabel);
            approvalSectionPanel.Controls.Add(rejectBtn);
            approvalSectionPanel.Controls.Add(approveBtn);
            approvalSectionPanel.Controls.Add(rejectionNotesTxtBx);
            approvalSectionPanel.Controls.Add(approvalHintLabel);
            approvalSectionPanel.Controls.Add(approvalHeaderLabel);

            // profileBoxPanel — boxes the profile block (name/tier/points/contact) off from the
            // history/ledger grids below, matching how the Feedback drawer boxes its review block.
            // Children are absolutely positioned inside it (a plain Panel ignores Padding for
            // absolutely-positioned children), so the 14px inset is applied per-control.
            profileBoxPanel.Size = new Size(340, 128);
            profileBoxPanel.BackColor = BrandPalette.SurfaceCard;
            profileBoxPanel.BorderStyle = BorderStyle.FixedSingle;
            profileBoxPanel.Margin = new Padding(0, 0, 0, 16);

            customerNameLabel.Location = new Point(14, 12);
            customerNameLabel.Margin = new Padding(0);       // no longer flow-positioned

            tierBadgeLabel.Location = new Point(14, 38);
            tierBadgeLabel.Margin = new Padding(0);

            pointsBalanceLabel.Location = new Point(14, 62);
            pointsBalanceLabel.Margin = new Padding(0);

            contactLabel.Location = new Point(14, 86);
            contactLabel.Margin = new Padding(0);

            profileBoxPanel.Controls.Add(contactLabel);
            profileBoxPanel.Controls.Add(pointsBalanceLabel);
            profileBoxPanel.Controls.Add(tierBadgeLabel);
            profileBoxPanel.Controls.Add(customerNameLabel);

            drawerFlow.Controls.Add(profileBoxPanel);
            drawerFlow.Controls.Add(orderHistoryHeaderLabel);
            drawerFlow.Controls.Add(orderHistoryGrid);
            // ... rest unchanged ...
            drawerFlow.Controls.Add(orderHistoryGrid);
            drawerFlow.Controls.Add(ledgerHeaderLabel);
            drawerFlow.Controls.Add(ledgerGrid);
            drawerFlow.Controls.Add(adjustmentRequestsHeaderLabel);
            drawerFlow.Controls.Add(adjustmentRequestsGrid);
            drawerFlow.Controls.Add(requestSectionPanel);
            drawerFlow.Controls.Add(approvalSectionPanel);

            drawerContentPanel.Controls.Add(drawerFlow);

            // Fill (drawerEmptyLabel) added first, then drawerContentPanel on top of it —
            // both Dock=Fill; only one is ever Visible at a time (see RefreshDrawer).
            drawerPanel.Controls.Add(drawerEmptyLabel);
            drawerPanel.Controls.Add(drawerContentPanel);

            // bodyPanel: Fill control (cardOuterPanel) added first, edge-docked (drawerPanel) added after —
            // WinForms docks in reverse z-order, so Fill must be lowest z / added first.
            bodyPanel.Controls.Add(cardOuterPanel);
            bodyPanel.Controls.Add(drawerPanel);

            // Root
            Controls.Add(bodyPanel);
            Size = new Size(1200, 700);
            BackColor = BrandPalette.SurfaceSecondary;

            ((System.ComponentModel.ISupportInitialize)customersGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)orderHistoryGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)ledgerGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)adjustmentRequestsGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)requestAmountNud).EndInit();
            ResumeLayout(false);
        }

        private static void ConfigureGrid(DataGridView grid, float headerSize, int rowHeight)
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
            grid.ShowCellToolTips = false;
            grid.GridColor = BrandPalette.Border;
            grid.Font = BrandPalette.BrandFontRegular(headerSize);
            grid.ColumnHeadersDefaultCellStyle.BackColor = BrandPalette.HeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = BrandPalette.HeaderText;
            grid.ColumnHeadersDefaultCellStyle.Font = BrandPalette.BrandFontBold(headerSize);
            // Clicking a header to sort selects that header cell — without this, WinForms
            // paints that selection using the system Highlight color (blue) regardless of
            // the rest of the theme.
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = BrandPalette.HeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = BrandPalette.HeaderText;
            grid.ColumnHeadersHeight = rowHeight + 6;
            grid.DefaultCellStyle.SelectionBackColor = BrandPalette.RowSelected;
            grid.DefaultCellStyle.SelectionForeColor = BrandPalette.NeutralText;
            grid.DefaultCellStyle.BackColor = BrandPalette.SurfacePrimary;
            grid.DefaultCellStyle.ForeColor = BrandPalette.NeutralText;
            grid.RowTemplate.Height = rowHeight;
            grid.ScrollBars = ScrollBars.Vertical;
        }
    }
}
