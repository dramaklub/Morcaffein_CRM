using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views
{
    partial class FrmCustomerOnboarding
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

        // Step 1 — Details
        private Panel detailsPanel;
        private Label firstNameLabel;
        private TextBox firstNameTxtBx;
        private Label lastNameLabel;
        private TextBox lastNameTxtBx;
        private Label phoneLabel;
        private TextBox phoneTxtBx;
        private Label emailLabel;
        private TextBox emailTxtBx;
        private CheckBox hasBirthdayChk;
        private DateTimePicker birthdayPicker;
        private Label errorLabel;

        // Step 2 — Confirm
        private Panel confirmPanel;
        private Label summaryHeaderLabel;
        private Label nameValueLabel;
        private Label phoneValueLabel;
        private Label emailValueLabel;
        private Label birthdayValueLabel;

        // Shared action row
        private Button cancelBtn;
        private Button nextBtn;
        private Button backBtn;
        private Button confirmBtn;

        private void InitializeComponent()
        {
            titleLabel = new Label();
            stepSubtitleLabel = new Label();

            detailsPanel = new Panel();
            firstNameLabel = new Label();
            firstNameTxtBx = new TextBox();
            lastNameLabel = new Label();
            lastNameTxtBx = new TextBox();
            phoneLabel = new Label();
            phoneTxtBx = new TextBox();
            emailLabel = new Label();
            emailTxtBx = new TextBox();
            hasBirthdayChk = new CheckBox();
            birthdayPicker = new DateTimePicker();
            errorLabel = new Label();

            confirmPanel = new Panel();
            summaryHeaderLabel = new Label();
            nameValueLabel = new Label();
            phoneValueLabel = new Label();
            emailValueLabel = new Label();
            birthdayValueLabel = new Label();

            cancelBtn = new Button();
            nextBtn = new Button();
            backBtn = new Button();
            confirmBtn = new Button();

            SuspendLayout();

            // titleLabel / stepSubtitleLabel
            titleLabel.AutoSize = true;
            titleLabel.Font = BrandPalette.BrandFontBold(14f);
            titleLabel.ForeColor = BrandPalette.Brand;
            titleLabel.Location = new Point(24, 20);
            titleLabel.Text = "New Customer";

            stepSubtitleLabel.AutoSize = true;
            stepSubtitleLabel.Font = BrandPalette.BrandFontRegular(8.5f);
            stepSubtitleLabel.ForeColor = BrandPalette.NeutralTextMuted;
            stepSubtitleLabel.Location = new Point(24, 50);
            stepSubtitleLabel.Text = "Step 1 of 2 — Details";

            // ===== detailsPanel =====
            detailsPanel.Location = new Point(24, 76);
            detailsPanel.Size = new Size(392, 280);

            firstNameLabel.AutoSize = true;
            firstNameLabel.Font = BrandPalette.BrandFontRegular(9f);
            firstNameLabel.ForeColor = BrandPalette.NeutralTextMuted;
            firstNameLabel.Location = new Point(0, 0);
            firstNameLabel.Text = "First name *";

            firstNameTxtBx.Location = new Point(0, 20);
            firstNameTxtBx.Size = new Size(190, 26);
            firstNameTxtBx.Font = BrandPalette.BrandFontRegular(9.5f);
            firstNameTxtBx.BorderStyle = BorderStyle.FixedSingle;

            lastNameLabel.AutoSize = true;
            lastNameLabel.Font = BrandPalette.BrandFontRegular(9f);
            lastNameLabel.ForeColor = BrandPalette.NeutralTextMuted;
            lastNameLabel.Location = new Point(202, 0);
            lastNameLabel.Text = "Last name *";

            lastNameTxtBx.Location = new Point(202, 20);
            lastNameTxtBx.Size = new Size(190, 26);
            lastNameTxtBx.Font = BrandPalette.BrandFontRegular(9.5f);
            lastNameTxtBx.BorderStyle = BorderStyle.FixedSingle;

            phoneLabel.AutoSize = true;
            phoneLabel.Font = BrandPalette.BrandFontRegular(9f);
            phoneLabel.ForeColor = BrandPalette.NeutralTextMuted;
            phoneLabel.Location = new Point(0, 58);
            phoneLabel.Text = "Phone number * (min. 10 digits)";

            phoneTxtBx.Location = new Point(0, 78);
            phoneTxtBx.Size = new Size(392, 26);
            phoneTxtBx.Font = BrandPalette.BrandFontRegular(9.5f);
            phoneTxtBx.BorderStyle = BorderStyle.FixedSingle;

            emailLabel.AutoSize = true;
            emailLabel.Font = BrandPalette.BrandFontRegular(9f);
            emailLabel.ForeColor = BrandPalette.NeutralTextMuted;
            emailLabel.Location = new Point(0, 116);
            emailLabel.Text = "Email (optional)";

            emailTxtBx.Location = new Point(0, 136);
            emailTxtBx.Size = new Size(392, 26);
            emailTxtBx.Font = BrandPalette.BrandFontRegular(9.5f);
            emailTxtBx.BorderStyle = BorderStyle.FixedSingle;

            hasBirthdayChk.AutoSize = true;
            hasBirthdayChk.Font = BrandPalette.BrandFontRegular(9f);
            hasBirthdayChk.ForeColor = BrandPalette.NeutralTextMuted;
            hasBirthdayChk.Location = new Point(0, 174);
            hasBirthdayChk.Text = "Set Birthday";

            birthdayPicker.Location = new Point(0, 196);
            birthdayPicker.Size = new Size(392, 26);
            birthdayPicker.Font = BrandPalette.BrandFontRegular(9.5f);
            birthdayPicker.Format = DateTimePickerFormat.Long;
            birthdayPicker.MaxDate = DateTime.Today;
            birthdayPicker.Enabled = false;

            errorLabel.AutoSize = false;
            errorLabel.Size = new Size(392, 40);
            errorLabel.Location = new Point(0, 230);
            errorLabel.Font = BrandPalette.BrandFontRegular(8.5f);
            errorLabel.ForeColor = BrandPalette.StatusNegativeFill;

            detailsPanel.Controls.Add(errorLabel);
            detailsPanel.Controls.Add(birthdayPicker);
            detailsPanel.Controls.Add(hasBirthdayChk);
            detailsPanel.Controls.Add(emailTxtBx);
            detailsPanel.Controls.Add(emailLabel);
            detailsPanel.Controls.Add(phoneTxtBx);
            detailsPanel.Controls.Add(phoneLabel);
            detailsPanel.Controls.Add(lastNameTxtBx);
            detailsPanel.Controls.Add(lastNameLabel);
            detailsPanel.Controls.Add(firstNameTxtBx);
            detailsPanel.Controls.Add(firstNameLabel);

            // ===== confirmPanel =====
            confirmPanel.Location = new Point(24, 76);
            confirmPanel.Size = new Size(392, 280);
            confirmPanel.Visible = false;

            summaryHeaderLabel.AutoSize = true;
            summaryHeaderLabel.Font = BrandPalette.BrandFontRegular(9f);
            summaryHeaderLabel.ForeColor = BrandPalette.NeutralTextMuted;
            summaryHeaderLabel.Location = new Point(0, 0);
            summaryHeaderLabel.Text = "Please confirm these details before registering:";

            nameValueLabel.AutoSize = true;
            nameValueLabel.Font = BrandPalette.BrandFontBold(10f);
            nameValueLabel.ForeColor = BrandPalette.NeutralText;
            nameValueLabel.Location = new Point(0, 32);

            phoneValueLabel.AutoSize = true;
            phoneValueLabel.Font = BrandPalette.BrandFontRegular(9.5f);
            phoneValueLabel.ForeColor = BrandPalette.NeutralText;
            phoneValueLabel.Location = new Point(0, 58);

            emailValueLabel.AutoSize = true;
            emailValueLabel.Font = BrandPalette.BrandFontRegular(9.5f);
            emailValueLabel.ForeColor = BrandPalette.NeutralText;
            emailValueLabel.Location = new Point(0, 84);

            birthdayValueLabel.AutoSize = true;
            birthdayValueLabel.Font = BrandPalette.BrandFontRegular(9.5f);
            birthdayValueLabel.ForeColor = BrandPalette.NeutralText;
            birthdayValueLabel.Location = new Point(0, 110);

            confirmPanel.Controls.Add(birthdayValueLabel);
            confirmPanel.Controls.Add(emailValueLabel);
            confirmPanel.Controls.Add(phoneValueLabel);
            confirmPanel.Controls.Add(nameValueLabel);
            confirmPanel.Controls.Add(summaryHeaderLabel);

            // ===== shared action row =====
            cancelBtn.Text = "Cancel";
            cancelBtn.Size = new Size(100, 34);
            cancelBtn.Location = new Point(24, 372);
            cancelBtn.FlatStyle = FlatStyle.Flat;
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.BackColor = BrandPalette.ButtonSecondaryFill;
            cancelBtn.ForeColor = BrandPalette.ButtonSecondaryText;
            cancelBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            cancelBtn.Cursor = Cursors.Hand;
            cancelBtn.DialogResult = DialogResult.Cancel;

            nextBtn.Text = "Next";
            nextBtn.Size = new Size(100, 34);
            nextBtn.Location = new Point(316, 372);
            nextBtn.FlatStyle = FlatStyle.Flat;
            nextBtn.FlatAppearance.BorderSize = 0;
            nextBtn.BackColor = BrandPalette.ButtonPrimaryFill;
            nextBtn.ForeColor = BrandPalette.ButtonPrimaryText;
            nextBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            nextBtn.Cursor = Cursors.Hand;

            backBtn.Text = "Back";
            backBtn.Size = new Size(100, 34);
            backBtn.Location = new Point(176, 372);
            backBtn.FlatStyle = FlatStyle.Flat;
            backBtn.FlatAppearance.BorderSize = 0;
            backBtn.BackColor = BrandPalette.ButtonSecondaryFill;
            backBtn.ForeColor = BrandPalette.ButtonSecondaryText;
            backBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            backBtn.Cursor = Cursors.Hand;
            backBtn.Visible = false;

            confirmBtn.Text = "Confirm";
            confirmBtn.Size = new Size(132, 34);
            confirmBtn.Location = new Point(284, 372);
            confirmBtn.FlatStyle = FlatStyle.Flat;
            confirmBtn.FlatAppearance.BorderSize = 0;
            confirmBtn.BackColor = BrandPalette.ButtonPrimaryFill;
            confirmBtn.ForeColor = BrandPalette.ButtonPrimaryText;
            confirmBtn.Font = BrandPalette.BrandFontRegular(9.5f);
            confirmBtn.Cursor = Cursors.Hand;
            confirmBtn.Visible = false;

            // FrmCustomerOnboarding
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(440, 428);
            BackColor = BrandPalette.SurfacePrimary;
            Controls.Add(titleLabel);
            Controls.Add(stepSubtitleLabel);
            Controls.Add(detailsPanel);
            Controls.Add(confirmPanel);
            Controls.Add(cancelBtn);
            Controls.Add(nextBtn);
            Controls.Add(backBtn);
            Controls.Add(confirmBtn);
            Font = BrandPalette.BrandFontRegular(9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "New Customer";
            CancelButton = cancelBtn;
            ResumeLayout(false);
        }
    }
}
