using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Views.Controls;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    partial class FeedbackManagementView
    {
        private PaginationControl pagination;
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
        private Button logFeedbackBtn;
        private DataGridView ticketsGrid;
        private Panel drawerPanel;
        private Label drawerEmptyLabel;
        private Panel drawerContentPanel;
        private FlowLayoutPanel drawerFlow;
        private Label ticketCustomerLabel;
        private Panel reviewBoxPanel;
        private Label reviewHeaderLabel;
        private Label ticketRatingLabel;
        private Label ticketCategoryLabel;
        private Label ticketStatusLabel;
        private Label ticketDateLabel;
        private Label ticketNotesLabel;

        private void InitializeComponent()
        {
            bodyPanel = new Panel();
            cardOuterPanel = new Panel();
            tableCardPanel = new Panel();
            cardHeaderPanel = new Panel();
            cardDividerPanel = new Panel();
            searchTxtBx = new TextBox();
            logFeedbackBtn = new Button();
            ticketsGrid = new DataGridView();
            pagination = new PaginationControl();
            drawerPanel = new Panel();
            drawerEmptyLabel = new Label();
            drawerContentPanel = new Panel();
            drawerFlow = new FlowLayoutPanel();
            ticketCustomerLabel = new Label();
            reviewBoxPanel = new Panel();
            reviewHeaderLabel = new Label();
            ticketRatingLabel = new Label();
            ticketCategoryLabel = new Label();
            ticketStatusLabel = new Label();
            ticketDateLabel = new Label();
            ticketNotesLabel = new Label();

            ((System.ComponentModel.ISupportInitialize)ticketsGrid).BeginInit();
            SuspendLayout();

            // bodyPanel — the page; drawerPanel sits edge-to-edge on the left, the table is
            // a padded/boxed card in the remaining space, matching the Customers tab.
            bodyPanel.Dock = DockStyle.Fill;
            bodyPanel.BackColor = BrandPalette.SurfaceSecondary;

            cardOuterPanel.Dock = DockStyle.Fill;
            cardOuterPanel.BackColor = BrandPalette.SurfaceSecondary;
            cardOuterPanel.Padding = new Padding(24);

            tableCardPanel.Dock = DockStyle.Fill;
            tableCardPanel.BackColor = BrandPalette.SurfacePrimary;
            tableCardPanel.BorderStyle = BorderStyle.FixedSingle;

            cardHeaderPanel.Dock = DockStyle.Top;
            cardHeaderPanel.Height = 60;
            cardHeaderPanel.BackColor = BrandPalette.SurfacePrimary;

            // searchTxtBx — top-left of the card; filters the ticket grid live
            searchTxtBx.Location = new Point(16, 16);
            searchTxtBx.Size = new Size(300, 28);
            searchTxtBx.PlaceholderText = "Search by customer, category, or status";
            searchTxtBx.Font = BrandPalette.BrandFontRegular(10f);

            // logFeedbackBtn — top-right of the card; always clickable (the modal it opens
            // has its own customer picker, so nothing needs to be pre-selected first)
            logFeedbackBtn.Text = "+ Log Feedback";
            logFeedbackBtn.Size = new Size(150, 32);
            logFeedbackBtn.Location = new Point(16, 14);
            logFeedbackBtn.FlatStyle = FlatStyle.Flat;
            logFeedbackBtn.FlatAppearance.BorderSize = 0;
            logFeedbackBtn.BackColor = BrandPalette.ButtonPrimaryFill;
            logFeedbackBtn.ForeColor = BrandPalette.ButtonPrimaryText;
            logFeedbackBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            logFeedbackBtn.Cursor = Cursors.Hand;

            cardDividerPanel.Dock = DockStyle.Bottom;
            cardDividerPanel.Height = 1;
            cardDividerPanel.BackColor = BrandPalette.Border;

            cardHeaderPanel.Controls.Add(logFeedbackBtn);
            cardHeaderPanel.Controls.Add(searchTxtBx);
            cardHeaderPanel.Controls.Add(cardDividerPanel);

            // ticketsGrid
            ticketsGrid.Dock = DockStyle.Fill;
            ConfigureGrid(ticketsGrid, headerSize: 9.5f, rowHeight: 30);
            ticketsGrid.BorderStyle = BorderStyle.None;

            tableCardPanel.Controls.Add(ticketsGrid);
            tableCardPanel.Controls.Add(cardHeaderPanel);
            tableCardPanel.Controls.Add(pagination);
            cardOuterPanel.Controls.Add(tableCardPanel);

            // drawerPanel — left of the card, matching the Customers tab's layout
            drawerPanel.Dock = DockStyle.Left;
            drawerPanel.Width = 460;
            drawerPanel.BackColor = BrandPalette.SurfaceCard;

            drawerEmptyLabel.Dock = DockStyle.Fill;
            drawerEmptyLabel.TextAlign = ContentAlignment.MiddleCenter;
            drawerEmptyLabel.ForeColor = BrandPalette.NeutralTextMuted;
            drawerEmptyLabel.Font = BrandPalette.BrandFontRegular(10f);
            drawerEmptyLabel.Text = "Select a ticket to view details.";

            drawerContentPanel.Dock = DockStyle.Fill;
            drawerContentPanel.Visible = false;
            drawerContentPanel.AutoScroll = true;

            drawerFlow.Dock = DockStyle.Top;
            drawerFlow.FlowDirection = FlowDirection.TopDown;
            drawerFlow.WrapContents = false;
            drawerFlow.AutoSize = true;
            drawerFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            drawerFlow.Padding = new Padding(20);
            drawerFlow.Width = 460;

            ticketCustomerLabel.AutoSize = true;
            ticketCustomerLabel.Font = BrandPalette.BrandFontBold(13f);
            ticketCustomerLabel.ForeColor = BrandPalette.NeutralText;
            ticketCustomerLabel.Margin = new Padding(0, 0, 0, 6);

            // reviewBoxPanel — boxes the review itself (rating/category/status/date/notes)
            // off from everything else in the drawer, so it reads as its own unit rather than
            // a run of loose labels.
            reviewBoxPanel.Size = new Size(420, 210);
            reviewBoxPanel.BackColor = BrandPalette.SurfaceCard;
            reviewBoxPanel.BorderStyle = BorderStyle.FixedSingle;
            reviewBoxPanel.Margin = new Padding(0, 0, 0, 16);

            reviewHeaderLabel.AutoSize = true;
            reviewHeaderLabel.Font = BrandPalette.BrandFontBold(9f);
            reviewHeaderLabel.ForeColor = BrandPalette.NeutralTextMuted;
            reviewHeaderLabel.Location = new Point(14, 10);
            reviewHeaderLabel.Text = "Customer Review";

            ticketRatingLabel.AutoSize = true;
            ticketRatingLabel.Font = BrandPalette.BrandFontRegular(12f);
            ticketRatingLabel.ForeColor = BrandPalette.Brand;
            ticketRatingLabel.Location = new Point(14, 32);

            ticketCategoryLabel.AutoSize = true;
            ticketCategoryLabel.Font = BrandPalette.BrandFontRegular(9.5f);
            ticketCategoryLabel.ForeColor = BrandPalette.NeutralText;
            ticketCategoryLabel.Location = new Point(14, 58);

            ticketStatusLabel.AutoSize = true;
            ticketStatusLabel.Font = BrandPalette.BrandFontBold(9.5f);
            ticketStatusLabel.ForeColor = BrandPalette.NeutralTextMuted;
            ticketStatusLabel.Location = new Point(14, 78);

            ticketDateLabel.AutoSize = true;
            ticketDateLabel.Font = BrandPalette.BrandFontRegular(8.5f);
            ticketDateLabel.ForeColor = BrandPalette.NeutralTextMuted;
            ticketDateLabel.Location = new Point(14, 98);

            ticketNotesLabel.AutoSize = true;
            ticketNotesLabel.MaximumSize = new Size(392, 0);
            ticketNotesLabel.Font = BrandPalette.BrandFontRegular(9f);
            ticketNotesLabel.ForeColor = BrandPalette.NeutralText;
            ticketNotesLabel.Location = new Point(14, 120);

            reviewBoxPanel.Controls.Add(ticketNotesLabel);
            reviewBoxPanel.Controls.Add(ticketDateLabel);
            reviewBoxPanel.Controls.Add(ticketStatusLabel);
            reviewBoxPanel.Controls.Add(ticketCategoryLabel);
            reviewBoxPanel.Controls.Add(ticketRatingLabel);
            reviewBoxPanel.Controls.Add(reviewHeaderLabel);

            // Drawer flow now holds only the customer name and the review box — the staff
            // console doesn't decide anything on a ticket, so there's no decision panel.
            drawerFlow.Controls.Add(ticketCustomerLabel);
            drawerFlow.Controls.Add(reviewBoxPanel);

            drawerContentPanel.Controls.Add(drawerFlow);

            drawerPanel.Controls.Add(drawerEmptyLabel);
            drawerPanel.Controls.Add(drawerContentPanel);

            // bodyPanel: Fill control (cardOuterPanel) added first, edge-docked (drawerPanel) added after.
            bodyPanel.Controls.Add(cardOuterPanel);
            bodyPanel.Controls.Add(drawerPanel);

            Controls.Add(bodyPanel);
            Size = new Size(1200, 700);
            BackColor = BrandPalette.SurfaceSecondary;

            ((System.ComponentModel.ISupportInitialize)ticketsGrid).EndInit();
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