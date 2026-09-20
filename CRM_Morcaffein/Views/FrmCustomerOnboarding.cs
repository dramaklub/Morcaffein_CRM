using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// Modal registration form, two steps. Caller reads CreatedCustomer after ShowDialog()
    /// returns OK. MVC: no business logic here — validation is done on the API.
    /// </summary>
    public partial class FrmCustomerOnboarding : Form
    {
        private readonly CustomerController _controller;
        private DateTime? _birthday;

        public CustomerResponseDto? CreatedCustomer { get; private set; }

        public FrmCustomerOnboarding(CustomerController controller)
        {
            _controller = controller;
            InitializeComponent();

            // Explicit tab order — visual top-to-bottom, left-to-right.
            // Step 1 order: first name -> last name -> phone -> email -> birthday -> Cancel -> Next.
            // Step 2 order: Back -> Confirm (Next and Cancel are hidden on step 2).
            firstNameTxtBx.TabIndex = 0;
            lastNameTxtBx.TabIndex = 1;
            phoneTxtBx.TabIndex = 2;
            emailTxtBx.TabIndex = 3;
            hasBirthdayChk.TabIndex = 4;
            birthdayPicker.TabIndex = 5;
            nextBtn.TabIndex = 6;
            cancelBtn.TabIndex = 7;
            backBtn.TabIndex = 8;
            confirmBtn.TabIndex = 9;

            hasBirthdayChk.CheckedChanged += (s, e) =>
            {
                birthdayPicker.Enabled = hasBirthdayChk.Checked;
                _birthday = hasBirthdayChk.Checked ? birthdayPicker.Value.Date : null;
            };
            birthdayPicker.ValueChanged += (s, e) =>
            {
                if (hasBirthdayChk.Checked)
                    _birthday = birthdayPicker.Value.Date;
            };
            phoneTxtBx.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            };

            nextBtn.Click += NextBtn_Click;
            backBtn.Click += (s, e) => ShowDetailsStep();
            confirmBtn.Click += ConfirmBtn_ClickAsync;

            ShowDetailsStep();
        }

        private bool CanSave() =>
            !string.IsNullOrWhiteSpace(firstNameTxtBx.Text) &&
            !string.IsNullOrWhiteSpace(lastNameTxtBx.Text) &&
            !string.IsNullOrWhiteSpace(phoneTxtBx.Text);

        private void NextBtn_Click(object? sender, EventArgs e)
        {
            if (!CanSave())
            {
                errorLabel.Text = "Please fill in first name, last name, and phone number.";
                return;
            }

            // Phone: 10-11 digits only.
            var phone = new string(phoneTxtBx.Text.Where(char.IsDigit).ToArray());
            if (phone.Length < 10 || phone.Length > 11)
            {
                errorLabel.Text = "Phone number must be 10-11 digits.";
                return;
            }

            // Email is optional, but if provided it must look like an email.
            var email = emailTxtBx.Text.Trim();
            if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            {
                errorLabel.Text = "Please enter a valid email address (e.g. name@example.com).";
                return;
            }

            errorLabel.Text = string.Empty;
            ShowConfirmStep();
        }

        private static bool IsValidEmail(string email)
        {
            // Lightweight check: something@something.something with no whitespace.
            // The server does a stricter check (contains '@' and '.'); this is the UX-level
            // gate so the user sees the error before submitting.
            var atIdx = email.IndexOf('@');
            if (atIdx <= 0) return false;                        // must have @, and not at position 0
            if (atIdx != email.LastIndexOf('@')) return false;   // only one @

            var dotIdx = email.IndexOf('.', atIdx + 1);
            if (dotIdx < 0 || dotIdx == atIdx + 1) return false; // must have . after @, not immediately
            if (dotIdx == email.Length - 1) return false;        // must have something after the .

            return !email.Any(char.IsWhiteSpace);
        }

        private async void ConfirmBtn_ClickAsync(object? sender, EventArgs e)
        {
            try
            {
                errorLabel.Text = string.Empty;
                confirmBtn.Enabled = false;
                CreatedCustomer = await _controller.RegisterAsync(
                    firstNameTxtBx.Text, lastNameTxtBx.Text, phoneTxtBx.Text,
                    string.IsNullOrWhiteSpace(emailTxtBx.Text) ? null : emailTxtBx.Text,
                    _birthday);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ShowDetailsStep();
                errorLabel.Text = ex.Message;
            }
            finally
            {
                confirmBtn.Enabled = true;
            }
        }

        private void ShowDetailsStep()
        {
            detailsPanel.Visible = true;
            confirmPanel.Visible = false;
            nextBtn.Visible = true;
            backBtn.Visible = false;
            confirmBtn.Visible = false;
            stepSubtitleLabel.Text = "Step 1 of 2 — Details";
        }

        private void ShowConfirmStep()
        {
            nameValueLabel.Text = $"Name: {firstNameTxtBx.Text} {lastNameTxtBx.Text}";
            phoneValueLabel.Text = $"Phone: {phoneTxtBx.Text}";
            emailValueLabel.Text = $"Email: {(string.IsNullOrWhiteSpace(emailTxtBx.Text) ? "Not provided" : emailTxtBx.Text)}";
            birthdayValueLabel.Text = $"Birthday: {(_birthday.HasValue ? _birthday.Value.ToString("MMMM d, yyyy") : "Not provided")}";

            detailsPanel.Visible = false;
            confirmPanel.Visible = true;
            nextBtn.Visible = false;
            backBtn.Visible = true;
            confirmBtn.Visible = true;
            stepSubtitleLabel.Text = "Step 2 of 2 — Confirm";
        }
    }
}