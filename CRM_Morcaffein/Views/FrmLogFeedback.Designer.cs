using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Views.Controls;

namespace CRM_Morcaffein.winforms.Views
{
    partial class FrmLogFeedback
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private Label titleLabel;
        private Label stepSubtitleLabel;

        // Step 1 — pick customer
        private Panel customerPickPanel;
        private TextBox customerSearchTxtBx;
        private ListBox customerResultsListBox;

        // Step 2 — feedback details
        private Panel detailsPanel;
        private Label customerLabel;
        private Label ratingLabel;
        private StarRatingControl starRating;
        private Label categoryLabel;
        private ComboBox categoryCombo;
        private Label notesLabel;
        private TextBox staffNotesTxtBx;

        private Label errorLabel;
        private Button cancelBtn;
        private Button nextBtn;
        private Button backBtn;
        private Button submitBtn;

        private void InitializeComponent()
        {
            titleLabel = new Label();
            stepSubtitleLabel = new Label();

            customerPickPanel = new Panel();
            customerSearchTxtBx = new TextBox();
            customerResultsListBox = new ListBox();

            detailsPanel = new Panel();
            customerLabel = new Label();
            ratingLabel = new Label();
            starRating = new StarRatingControl();
            categoryLabel = new Label();
            categoryCombo = new ComboBox();
            notesLabel = new Label();
            staffNotesTxtBx = new TextBox();

            errorLabel = new Label();
            cancelBtn = new Button();
            nextBtn = new Button();
            backBtn = new Button();
            submitBtn = new Button();

            SuspendLayout();

            titleLabel.AutoSize = true;
            titleLabel.Font = BrandPalette.BrandFontBold(14f);
            titleLabel.ForeColor = BrandPalette.Brand;
            titleLabel.Location = new Point(24, 20);
            titleLabel.Text = "Log Feedback";

            stepSubtitleLabel.AutoSize = true;
            stepSubtitleLabel.Font = BrandPalette.BrandFontRegular(8.5f);
            stepSubtitleLabel.ForeColor = BrandPalette.NeutralTextMuted;
            stepSubtitleLabel.Location = new Point(24, 50);
            stepSubtitleLabel.Text = "Step 1 of 2 — Find Customer";

            // ===== Step 1: customerPickPanel =====
            customerPickPanel.Location = new Point(24, 76);
            customerPickPanel.Size = new Size(352, 280);

            customerSearchTxtBx.Location = new Point(0, 0);
            customerSearchTxtBx.Size = new Size(352, 28);
            customerSearchTxtBx.PlaceholderText = "Search by name, phone, or ID";
            customerSearchTxtBx.Font = BrandPalette.BrandFontRegular(9.5f);
            customerSearchTxtBx.BorderStyle = BorderStyle.FixedSingle;

            customerResultsListBox.Location = new Point(0, 34);
            customerResultsListBox.Size = new Size(352, 246);
            customerResultsListBox.Font = BrandPalette.BrandFontRegular(9.5f);
            customerResultsListBox.BorderStyle = BorderStyle.FixedSingle;
            customerResultsListBox.IntegralHeight = false;

            customerPickPanel.Controls.Add(customerResultsListBox);
            customerPickPanel.Controls.Add(customerSearchTxtBx);

            // ===== Step 2: detailsPanel =====
            detailsPanel.Location = new Point(24, 76);
            detailsPanel.Size = new Size(352, 280);
            detailsPanel.Visible = false;

            customerLabel.AutoSize = true;
            customerLabel.Font = BrandPalette.BrandFontRegular(9f);
            customerLabel.ForeColor = BrandPalette.NeutralTextMuted;
            customerLabel.Location = new Point(0, 0);
            customerLabel.Text = "For: —";

            ratingLabel.AutoSize = true;
            ratingLabel.Font = BrandPalette.BrandFontRegular(9f);
            ratingLabel.ForeColor = BrandPalette.NeutralTextMuted;
            ratingLabel.Location = new Point(0, 30);
            ratingLabel.Text = "Rating *";

            starRating.Location = new Point(0, 50);

            categoryLabel.AutoSize = true;
            categoryLabel.Font = BrandPalette.BrandFontRegular(9f);
            categoryLabel.ForeColor = BrandPalette.NeutralTextMuted;
            categoryLabel.Location = new Point(0, 90);
            categoryLabel.Text = "Category *";

            categoryCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            categoryCombo.Location = new Point(0, 110);
            categoryCombo.Size = new Size(352, 26);
            categoryCombo.Font = BrandPalette.BrandFontRegular(9.5f);
            categoryCombo.Items.AddRange(new object[] { "Taste", "Wait Time", "Service" });

            notesLabel.AutoSize = true;
            notesLabel.Font = BrandPalette.BrandFontRegular(9f);
            notesLabel.ForeColor = BrandPalette.NeutralTextMuted;
            notesLabel.Location = new Point(0, 148);
            notesLabel.Text = "Staff Notes";

            staffNotesTxtBx.Location = new Point(0, 168);
            staffNotesTxtBx.Size = new Size(352, 100);
            staffNotesTxtBx.Multiline = true;
            staffNotesTxtBx.ScrollBars = ScrollBars.Vertical;
            staffNotesTxtBx.Font = BrandPalette.BrandFontRegular(9.5f);
            staffNotesTxtBx.BorderStyle = BorderStyle.FixedSingle;

            detailsPanel.Controls.Add(staffNotesTxtBx);
            detailsPanel.Controls.Add(notesLabel);
            detailsPanel.Controls.Add(categoryCombo);
            detailsPanel.Controls.Add(categoryLabel);
            detailsPanel.Controls.Add(starRating);
            detailsPanel.Controls.Add(ratingLabel);
            detailsPanel.Controls.Add(customerLabel);

            // ===== shared bottom row =====
            errorLabel.AutoSize = false;
            errorLabel.Size = new Size(352, 32);
            errorLabel.Location = new Point(24, 360);
            errorLabel.Font = BrandPalette.BrandFontRegular(8.5f);
            errorLabel.ForeColor = BrandPalette.StatusNegativeFill;

            cancelBtn.Text = "Cancel";
            cancelBtn.Size = new Size(100, 34);
            cancelBtn.Location = new Point(24, 398);
            cancelBtn.FlatStyle = FlatStyle.Flat;
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.BackColor = BrandPalette.ButtonSecondaryFill;
            cancelBtn.ForeColor = BrandPalette.ButtonSecondaryText;
            cancelBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            cancelBtn.Cursor = Cursors.Hand;
            cancelBtn.DialogResult = DialogResult.Cancel;

            nextBtn.Text = "Next";
            nextBtn.Size = new Size(100, 34);
            nextBtn.Location = new Point(276, 398);
            nextBtn.FlatStyle = FlatStyle.Flat;
            nextBtn.FlatAppearance.BorderSize = 0;
            nextBtn.BackColor = BrandPalette.ButtonPrimaryFill;
            nextBtn.ForeColor = BrandPalette.ButtonPrimaryText;
            nextBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            nextBtn.Cursor = Cursors.Hand;
            nextBtn.Enabled = false;

            backBtn.Text = "Back";
            backBtn.Size = new Size(100, 34);
            backBtn.Location = new Point(24, 398); // takes cancelBtn's slot — cancel is hidden once step 2 is shown
            backBtn.FlatStyle = FlatStyle.Flat;
            backBtn.FlatAppearance.BorderSize = 0;
            backBtn.BackColor = BrandPalette.ButtonSecondaryFill;
            backBtn.ForeColor = BrandPalette.ButtonSecondaryText;
            backBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            backBtn.Cursor = Cursors.Hand;
            backBtn.Visible = false;

            submitBtn.Text = "Submit";
            submitBtn.Size = new Size(130, 34);
            submitBtn.Location = new Point(246, 398);
            submitBtn.FlatStyle = FlatStyle.Flat;
            submitBtn.FlatAppearance.BorderSize = 0;
            submitBtn.BackColor = BrandPalette.ButtonPrimaryFill;
            submitBtn.ForeColor = BrandPalette.ButtonPrimaryText;
            submitBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            submitBtn.Cursor = Cursors.Hand;
            submitBtn.Visible = false;
            submitBtn.Enabled = false;

            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(400, 456);
            BackColor = BrandPalette.SurfacePrimary;
            Controls.Add(titleLabel);
            Controls.Add(stepSubtitleLabel);
            Controls.Add(customerPickPanel);
            Controls.Add(detailsPanel);
            Controls.Add(errorLabel);
            Controls.Add(cancelBtn);
            Controls.Add(backBtn);
            Controls.Add(nextBtn);
            Controls.Add(submitBtn);
            Font = BrandPalette.BrandFontRegular(9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Log Feedback";
            CancelButton = cancelBtn;
            ResumeLayout(false);
        }
    }
}
