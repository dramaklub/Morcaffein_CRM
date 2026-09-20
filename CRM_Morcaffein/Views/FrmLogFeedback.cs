using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// Modal for logging a new feedback ticket. Step 1 finds/picks the customer (its own
    /// search — this modal doesn't depend on anything being pre-selected by the caller);
    /// step 2 fills in the rating/category/notes.
    ///
    /// MVC: calls FeedbackController methods directly and keeps its own plain fields for
    /// in-progress input. All validation is server-side.
    /// </summary>
    public partial class FrmLogFeedback : Form
    {
        private readonly FeedbackController _controller;
        private readonly int _loggedByEmployeeId;

        private CustomerResponseDto? _selectedCustomer;
        private int _rating;
        private string _category = string.Empty;

        public FeedbackResponseDto? CreatedFeedback { get; private set; }

        public FrmLogFeedback(FeedbackController controller, int loggedByEmployeeId)
        {
            _controller = controller;
            _loggedByEmployeeId = loggedByEmployeeId;
            InitializeComponent();

            // Explicit tab order — visual top-to-bottom.
            // Step 1: search -> results -> Next -> Cancel
            // Step 2: stars (mouse only for now) -> category -> notes -> Back -> Submit
            customerSearchTxtBx.TabIndex = 0;
            customerResultsListBox.TabIndex = 1;
            nextBtn.TabIndex = 2;
            cancelBtn.TabIndex = 3;

            starRating.TabIndex = 4;
            categoryCombo.TabIndex = 5;
            staffNotesTxtBx.TabIndex = 6;
            backBtn.TabIndex = 7;
            submitBtn.TabIndex = 8;

            customerSearchTxtBx.TextChanged += CustomerSearchTxtBx_TextChanged;
            customerResultsListBox.DisplayMember = nameof(CustomerResponseDto.FullName);
            customerResultsListBox.SelectedIndexChanged += (s, e) =>
                nextBtn.Enabled = customerResultsListBox.SelectedItem != null;

            starRating.RatingChanged += (s, e) => { _rating = starRating.Rating; UpdateSubmitEnabled(); };
            categoryCombo.SelectedIndexChanged += (s, e) => { _category = categoryCombo.SelectedItem as string ?? string.Empty; UpdateSubmitEnabled(); };

            nextBtn.Click += NextBtn_Click;
            backBtn.Click += (s, e) => ShowPickCustomerStep();
            submitBtn.Click += SubmitBtn_Click;

            ButtonStyler.TrackEnabledState(nextBtn);
            ButtonStyler.TrackEnabledState(submitBtn);

            ShowPickCustomerStep();
            CustomerSearchTxtBx_TextChanged(this, EventArgs.Empty); // populate the initial (unfiltered) results list
        }

        private async void CustomerSearchTxtBx_TextChanged(object? sender, EventArgs e)
        {
            try
            {
                var results = await _controller.SearchCustomersAsync(customerSearchTxtBx.Text);
                customerResultsListBox.DataSource = results;
            }
            catch
            {
                customerResultsListBox.DataSource = null;
            }
        }

        private void NextBtn_Click(object? sender, EventArgs e)
        {
            if (customerResultsListBox.SelectedItem is not CustomerResponseDto customer)
                return;

            _selectedCustomer = customer;
            customerLabel.Text = $"For: {customer.FullName}";
            ShowDetailsStep();
            UpdateSubmitEnabled();
        }

        private void UpdateSubmitEnabled()
        {
            submitBtn.Enabled = _selectedCustomer != null && _rating is >= 1 and <= 5 && !string.IsNullOrWhiteSpace(_category);
        }

        private async void SubmitBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedCustomer is null) return;

            try
            {
                errorLabel.Text = string.Empty;
                submitBtn.Enabled = false;

                CreatedFeedback = await _controller.LogFeedbackAsync(
                    _selectedCustomer.CustomerId,
                    _loggedByEmployeeId,
                    _rating,
                    _category,
                    staffNotesTxtBx.Text);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                errorLabel.Text = ex.Message;
            }
            finally
            {
                submitBtn.Enabled = true;
            }
        }

        private void ShowPickCustomerStep()
        {
            customerPickPanel.Visible = true;
            detailsPanel.Visible = false;
            cancelBtn.Visible = true;
            nextBtn.Visible = true;
            backBtn.Visible = false;
            submitBtn.Visible = false;
            stepSubtitleLabel.Text = "Step 1 of 2 — Find Customer";
        }

        private void ShowDetailsStep()
        {
            customerPickPanel.Visible = false;
            detailsPanel.Visible = true;
            cancelBtn.Visible = false; // step 2 offers Back instead — Cancel would just duplicate it
            nextBtn.Visible = false;
            backBtn.Visible = true;
            submitBtn.Visible = true;
            stepSubtitleLabel.Text = "Step 2 of 2 — Feedback Details";
        }
    }
}