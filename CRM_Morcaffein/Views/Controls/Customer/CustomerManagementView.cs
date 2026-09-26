using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// The Customers tab. Searchable grid + profile drawer. Point-adjustment UI is
    /// gated by EmployeeSession role — the API enforces the same rules server-side.
    /// </summary>
    public partial class CustomerManagementView : UserControl
    {
        private readonly CustomerController _controller;
        private readonly BindingSource _customersBindingSource = new();
        private PaginationControl pagination = null!;

        // Status filter dropdown — built in code, inserted into cardHeaderPanel below the
        // search row. Mirrors the Feedback status filter and the TransactionView category
        // dropdown. Default is "Active"; switching it changes the API query.
        private Panel _activeFilterRow = null!;
        private ComboBox _activeFilterCombo = null!;

        private const string ActiveFilterActiveOption = "Active";
        private const string ActiveFilterInactiveOption = "Inactive";
        private const string ActiveFilterAllOption = "All";

        // Additive drawer elements — built in code so the designer file stays untouched.
        // _statusValueLabel lives INSIDE profileBoxPanel (added to its Controls, not to
        // drawerFlow) so it reads as part of the profile block. _editCustomerBtn and
        // _customerEditPanel are inserted into drawerFlow right after profileBoxPanel.
        private Label _statusValueLabel = null!;
        private Button _editCustomerBtn = null!;
        private Button _deactivateCustomerBtn = null!;

        private Panel _customerEditPanel = null!;
        private TextBox _editFirstNameTxtBx = null!;
        private TextBox _editLastNameTxtBx = null!;
        private TextBox _editContactTxtBx = null!;
        private TextBox _editEmailTxtBx = null!;
        private DateTimePicker _editBirthdayPicker = null!;
        private CheckBox _editHasBirthdayChk = null!;
        private Label _editErrorLabel = null!;
        private Button _editSaveBtn = null!;
        private Button _editCancelBtn = null!;
        private bool _isEditingCustomer;

        private CustomerResponseDto? _selectedCustomer;
        private PointAdjustmentResponseDto? _selectedRequest;

        public CustomerManagementView()
        {
            _controller = new CustomerController();
            InitializeComponent();

            ConfigureCustomersGridColumns();
            ConfigureOrderHistoryGridColumns();
            ConfigureLedgerGridColumns();
            ConfigureAdjustmentRequestsGridColumns();

            pagination.PageChanged += async (s, page) => await ReloadCustomersAsync(page);

            customersGrid.DataSource = _customersBindingSource;
            customersGrid.SelectionChanged += CustomersGrid_SelectionChanged;
            customersGrid.CellFormatting += CustomersGrid_CellFormatting;

            searchTxtBx.TextChanged += (s, e) => ReloadCustomers();
            newCustomerBtn.Click += (s, e) => OpenOnboardingModal();

            cardHeaderPanel.Resize += (s, e) => PositionActionButton();
            PositionActionButton();

            BuildActiveFilterRow();

            // Only Baristas register new customers. Managers act on existing ones
            // (edit, approve point adjustments) and don't originate records. The API
            // enforces this too — CreateCustomer is reachable by any authenticated
            // employee today, so this is a UI-level restriction only for now.
            newCustomerBtn.Visible =
                EmployeeSession.Current?.Employee?.Role?.Equals("Barista", StringComparison.OrdinalIgnoreCase) == true;

            ButtonStyler.TrackEnabledState(submitRequestBtn);
            ButtonStyler.TrackEnabledState(approveBtn);
            ButtonStyler.TrackEnabledState(rejectBtn);

            adjustmentRequestsGrid.SelectionChanged += AdjustmentRequestsGrid_SelectionChanged;

            requestAmountNud.ValueChanged += (s, e) => UpdateSubmitRequestEnabled();
            requestReasonTxtBx.TextChanged += (s, e) => UpdateSubmitRequestEnabled();
            submitRequestBtn.Click += SubmitRequestBtn_Click;

            rejectionNotesTxtBx.TextChanged += (s, e) => UpdateApprovalControlsEnabled();
            approveBtn.Click += ApproveBtn_Click;
            rejectBtn.Click += RejectBtn_Click;

            Load += async (s, e) => await ReloadCustomersAsync();

            BuildDrawerAdditions();
        }

        // Barista files requests; nobody else does.
        private bool CanRequestAdjustment =>
            EmployeeSession.Current?.Employee?.Role?.Equals("Barista", StringComparison.OrdinalIgnoreCase) == true;

        // Manager decides. Not Admin, not SuperAdmin, not Barista.
        private bool CanApprove =>
            EmployeeSession.Current?.Employee?.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true;

        // Manager edits customer info inline. Barista cannot edit. Admin is not present in this view at all.

        private bool CanEditCustomer => CanApprove;
        private void PositionActionButton()
        {
            newCustomerBtn.Left = cardHeaderPanel.ClientSize.Width - newCustomerBtn.Width - 16;
            newCustomerBtn.Top = 14;
        }
        /// <summary>
        /// Builds the "Filter by Status:" row and inserts it into the card header, below
        /// the search box. Same shape as the Feedback status filter: label, then dropdown,
        /// positions computed from the label's measured width. Default is Active.
        ///
        /// The card header was 60px tall for search + button; this needs another ~40px
        /// below the search row, so the header grows.
        /// </summary>
        private void BuildActiveFilterRow()
        {
            // Grow the header so the new row fits below the search box.
            // Search box lives at y=16..44; filter row starts at y=52.
            cardHeaderPanel.Height = 92;

            _activeFilterRow = new Panel
            {
                Location = new Point(16, 52),
                Size = new Size(360, 30),
                BackColor = BrandPalette.SurfacePrimary,
            };

            var label = new Label
            {
                Text = "Filter by Status:",
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            _activeFilterCombo = new ComboBox
            {
                Size = new Size(180, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };

            _activeFilterCombo.Items.Add(ActiveFilterActiveOption);
            _activeFilterCombo.Items.Add(ActiveFilterInactiveOption);
            _activeFilterCombo.Items.Add(ActiveFilterAllOption);

            // Default to Active — matches the API's default and keeps Baristas from ever
            // seeing deactivated customers.
            _activeFilterCombo.SelectedItem = ActiveFilterActiveOption;

            // Position: label first, combo after, measured so they can't collide.
            var labelWidth = TextRenderer.MeasureText(label.Text, label.Font).Width;
            label.Left = 0;
            label.Top = (30 - label.Height) / 2;

            _activeFilterCombo.Left = labelWidth + 12;
            _activeFilterCombo.Top = (30 - _activeFilterCombo.Height) / 2;

            _activeFilterCombo.SelectedIndexChanged += async (s, e) =>
            {
                // Filter change is always a fresh page-1 query — same as a search change.
                await ReloadCustomersAsync();
            };

            _activeFilterRow.Controls.Add(_activeFilterCombo);
            _activeFilterRow.Controls.Add(label);

            cardHeaderPanel.Controls.Add(_activeFilterRow);
            _activeFilterRow.BringToFront();
        }

        /// <summary>
        /// The API value for the currently-selected filter:
        ///   "Active"   -> "active"
        ///   "Inactive" -> "inactive"
        ///   "All"      -> "all"
        /// Null (never happens with the fixed list, but defensive) -> "active".
        /// </summary>
        private string SelectedActiveFilter =>
            (_activeFilterCombo?.SelectedItem as string) switch
            {
                ActiveFilterInactiveOption => "inactive",
                ActiveFilterAllOption => "all",
                _ => "active",
            };

        // =====================================================================
        // DRAWER ADDITIONS (Status line + Edit Customer + inline edit panel)
        // =====================================================================

        /// <summary>
        /// Builds the three runtime-only drawer elements and places them:
        ///   - _statusValueLabel -> inside profileBoxPanel, as a fifth absolutely-positioned
        ///     line under contactLabel. Grows profileBoxPanel by 26px to fit it.
        ///   - _editCustomerBtn  -> inserted into drawerFlow at index 1 (right after
        ///     profileBoxPanel, before the "Order History" header).
        ///   - _customerEditPanel -> inserted into drawerFlow at index 2 (right below the
        ///     edit button). Hidden by default.
        ///
        /// Deliberately does NOT touch the designer file. Everything here is additive.
        /// </summary>
        private void BuildDrawerAdditions()
        {
            // ---- Status line, inside the profile box ----
            profileBoxPanel.Height += 26;   // make room for the fifth line

            _statusValueLabel = new Label
            {
                Location = new Point(14, 110),          // just below contactLabel at y=86
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(9f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
            };
            profileBoxPanel.Controls.Add(_statusValueLabel);

            // ---- Edit Customer button, inserted right after the profile box ----
            _editCustomerBtn = new Button
            {
                Text = "Edit Customer",
                Size = new Size(340, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0, 0, 0, 12),
            };
            _editCustomerBtn.FlatAppearance.BorderSize = 0;
            _editCustomerBtn.Click += (s, e) => EnterCustomerEditState();

            // ---- Deactivate Customer button, stacked directly below Edit ----
            _deactivateCustomerBtn = new Button
            {
                Text = "Deactivate Customer",
                Size = new Size(340, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.StatusNegativeFill,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0, 0, 0, 12),
            };
            _deactivateCustomerBtn.FlatAppearance.BorderSize = 0;
            _deactivateCustomerBtn.Click += DeactivateCustomerBtn_Click;

            // ---- Inline edit panel, inserted below the two buttons ----
            _customerEditPanel = BuildCustomerEditPanel();
            _customerEditPanel.Visible = false;

            // Insert into drawerFlow. profileBoxPanel is index 0; we want
            //   [profileBoxPanel, editCustomerBtn, deactivateCustomerBtn, customerEditPanel, orderHistoryHeader, ...]
            drawerFlow.Controls.Add(_editCustomerBtn);
            drawerFlow.Controls.Add(_deactivateCustomerBtn);
            drawerFlow.Controls.Add(_customerEditPanel);
            drawerFlow.Controls.SetChildIndex(_editCustomerBtn, 1);
            drawerFlow.Controls.SetChildIndex(_deactivateCustomerBtn, 2);
            drawerFlow.Controls.SetChildIndex(_customerEditPanel, 3);
        }

        private Panel BuildCustomerEditPanel()
        {
            const int rowHeight = 56;
            const int labelHeight = 18;
            const int fieldHeight = 28;
            const int fieldWidth = 300;

            var panel = new Panel
            {
                Width = 340,
                BackColor = BrandPalette.SurfaceCard,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 16),
            };

            int y = 14;   // inset from the panel's border

            void AddLabel(string text)
            {
                var label = new Label
                {
                    Text = text,
                    Location = new Point(14, y),
                    AutoSize = true,
                    Font = BrandPalette.BrandFontRegular(9f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                    BackColor = Color.Transparent,
                };
                panel.Controls.Add(label);
            }

            TextBox MakeRow(string labelText, bool digitsOnly = false)
            {
                AddLabel(labelText);
                var txt = new TextBox
                {
                    Location = new Point(14, y + labelHeight),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = BrandPalette.BrandFontRegular(9.5f),
                    BorderStyle = BorderStyle.FixedSingle,
                };
                if (digitsOnly)
                {
                    txt.KeyPress += (s, e) =>
                    {
                        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                            e.Handled = true;
                    };
                }
                panel.Controls.Add(txt);
                y += rowHeight;
                return txt;
            }

            _editFirstNameTxtBx = MakeRow("First name *");
            _editLastNameTxtBx = MakeRow("Last name *");
            _editContactTxtBx = MakeRow("Contact number * (10-11 digits)", digitsOnly: true);
            _editEmailTxtBx = MakeRow("Email (optional)");

            // Birthday row — label, then checkbox on its own line, then the picker below it.
            // Each element gets its own y step so nothing overlaps.
            AddLabel("Birthday");
            y += labelHeight + 4;                       // label baseline + small gap

            _editHasBirthdayChk = new CheckBox
            {
                Text = "Set birthday",
                Location = new Point(14, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };
            panel.Controls.Add(_editHasBirthdayChk);
            y += 26;                                    // checkbox row height

            _editBirthdayPicker = new DateTimePicker
            {
                Location = new Point(14, y),
                Size = new Size(fieldWidth, fieldHeight),
                Format = DateTimePickerFormat.Short,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Enabled = false,
            };
            _editHasBirthdayChk.CheckedChanged += (s, e) =>
                _editBirthdayPicker.Enabled = _editHasBirthdayChk.Checked;
            panel.Controls.Add(_editBirthdayPicker);
            y += fieldHeight + 12;                      // picker height + gap before the error label

            _editErrorLabel = new Label
            {
                Location = new Point(14, y),
                Size = new Size(fieldWidth, 24),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };
            panel.Controls.Add(_editErrorLabel);
            y += 28;

            _editSaveBtn = new Button
            {
                Text = "Save Changes",
                Location = new Point(14, y),
                Size = new Size(150, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
            };
            _editSaveBtn.FlatAppearance.BorderSize = 0;
            _editSaveBtn.Click += CustomerEditSaveBtn_Click;

            _editCancelBtn = new Button
            {
                Text = "Cancel",
                Location = new Point(172, y),
                Size = new Size(150, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _editCancelBtn.FlatAppearance.BorderSize = 0;
            _editCancelBtn.Click += (s, e) => ExitCustomerEditState();

            panel.Controls.Add(_editSaveBtn);
            panel.Controls.Add(_editCancelBtn);

            panel.Height = y + 32 + 14;   // + button height + bottom inset
            return panel;
        }

        private void EnterCustomerEditState()
        {
            if (_selectedCustomer is null) return;

            _editFirstNameTxtBx.Text = _selectedCustomer.FirstName;
            _editLastNameTxtBx.Text = _selectedCustomer.LastName;
            _editContactTxtBx.Text = _selectedCustomer.ContactNumber;
            _editEmailTxtBx.Text = _selectedCustomer.Email ?? string.Empty;

            if (_selectedCustomer.Birthday.HasValue)
            {
                _editHasBirthdayChk.Checked = true;
                _editBirthdayPicker.Value = _selectedCustomer.Birthday.Value;
                _editBirthdayPicker.Enabled = true;
            }
            else
            {
                _editHasBirthdayChk.Checked = false;
                _editBirthdayPicker.Enabled = false;
            }

            _editErrorLabel.Text = string.Empty;
            _editCustomerBtn.Visible = false;
            _deactivateCustomerBtn.Visible = false;
            _customerEditPanel.Visible = true;
            _isEditingCustomer = true;
        }

        private void ExitCustomerEditState()
        {
            _customerEditPanel.Visible = false;
            _editCustomerBtn.Visible = CanEditCustomer;
            _deactivateCustomerBtn.Visible = CanEditCustomer;
            _isEditingCustomer = false;
        }

        private async void DeactivateCustomerBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedCustomer is null) return;

            var customer = _selectedCustomer;
            var reactivating = !customer.IsActive;

            var confirmMessage = reactivating
                ? $"Reactivate {customer.FullName}? They will appear in the list."
                : $"Deactivate {customer.FullName}? They will no longer appear in the list, ";

            var confirmTitle = reactivating ? "Reactivate Customer" : "Deactivate Customer";

            var confirm = MessageBox.Show(
                FindForm(),
                confirmMessage,
                confirmTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _deactivateCustomerBtn.Enabled = false;

                if (reactivating)
                {
                    await _controller.ReactivateCustomerAsync(customer.CustomerId);
                    Toast.Show(this, $"Reactivated {customer.FullName}.", ToastKind.Success);
                }
                else
                {
                    await _controller.DeactivateCustomerAsync(customer.CustomerId);
                    Toast.Show(this, $"Deactivated {customer.FullName}.", ToastKind.Success);
                }

                // Reload. If we're in the Active filter and just deactivated, the customer
                // drops out of the grid. If we're in the Inactive filter and just reactivated,
                // the same. If we're in All, the row stays but its state flips.
                await ReloadCustomersAsync();

                // Try to keep the customer selected if they're still visible under the
                // current filter. If they filtered themselves out, selection clears and the
                // drawer reverts to the empty state.
                SelectCustomerRowById(customer.CustomerId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Operation failed");
            }
            finally
            {
                _deactivateCustomerBtn.Enabled = true;
            }
        }


        private async void CustomerEditSaveBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedCustomer is null) return;

            _editErrorLabel.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(_editFirstNameTxtBx.Text) ||
                string.IsNullOrWhiteSpace(_editLastNameTxtBx.Text))
            {
                _editErrorLabel.Text = "First and last name are required.";
                return;
            }

            var cleanedPhone = new string(_editContactTxtBx.Text.Where(char.IsDigit).ToArray());
            if (cleanedPhone.Length < 10 || cleanedPhone.Length > 11)
            {
                _editErrorLabel.Text = "Contact number must be 10-11 digits.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(_editEmailTxtBx.Text) &&
                (!_editEmailTxtBx.Text.Contains('@') || !_editEmailTxtBx.Text.Contains('.')))
            {
                _editErrorLabel.Text = "Invalid email format.";
                return;
            }

            try
            {
                _editSaveBtn.Enabled = false;

                var birthday = _editHasBirthdayChk.Checked
                    ? _editBirthdayPicker.Value.Date
                    : (DateTime?)null;

                await _controller.UpdateCustomerAsync(
                    _selectedCustomer.CustomerId,
                    _editFirstNameTxtBx.Text,
                    _editLastNameTxtBx.Text,
                    _editContactTxtBx.Text,
                    string.IsNullOrWhiteSpace(_editEmailTxtBx.Text) ? null : _editEmailTxtBx.Text,
                    birthday);

                ExitCustomerEditState();

                Toast.Show(this, $"Saved changes to {_selectedCustomer.FullName}.", ToastKind.Success);

                var affectedCustomerId = _selectedCustomer.CustomerId;   // local
                await ReloadCustomersAsync();
                SelectCustomerRowById(affectedCustomerId);
            }
            catch (Exception ex)
            {
                _editErrorLabel.Text = ex.Message;
            }
            finally
            {
                _editSaveBtn.Enabled = true;
            }
        }

        private async Task ReloadCustomersAsync(int? page = null)
        {
            var targetPage = page ?? 1;

            if (page is null)
                pagination.ResetToFirstPage();

            PagedResponse<CustomerResponseDto> response;
            try
            {
                response = await _controller.SearchAsync(
                    searchTxtBx.Text, targetPage, 25, SelectedActiveFilter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Error loading customers");
                return;
            }

            _customersBindingSource.DataSource = response.Items;

            pagination.UpdatePageInfo(response.TotalCount, response.Page, response.PageSize);

            customersGrid.ClearSelection();
            try { customersGrid.CurrentCell = null; } catch { /* not always allowed; ignore */ }

            // Page changes (and search changes) reset the drawer.
            _selectedCustomer = null;
            await RefreshDrawerAsync();
        }

        private async void ReloadCustomers()
        {
            try { await ReloadCustomersAsync(); }
            catch (Exception ex) { MessageBox.Show(FindForm(), ex.Message, "Error"); }
        }
        /// <summary>
        /// Mutes rows for inactive customers with a neutral grey treatment (not brand-tinted)
        /// so they visibly read as "off" against the white grid. Same treatment as inactive
        /// employees in Manage Users.
        /// </summary>
        private void CustomersGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (customersGrid.Rows[e.RowIndex].DataBoundItem is not CustomerResponseDto customer)
                return;

            if (!customer.IsActive)
            {
                var greyText = Color.FromArgb(255, 130, 130, 130);
                var greyBackground = Color.FromArgb(255, 240, 240, 240);

                e.CellStyle.ForeColor = greyText;
                e.CellStyle.BackColor = greyBackground;
                e.CellStyle.SelectionForeColor = greyText;
                e.CellStyle.SelectionBackColor = greyBackground;
            }
        }

        private async void CustomersGrid_SelectionChanged(object? sender, EventArgs e)
        {
            _selectedCustomer = customersGrid.CurrentRow?.DataBoundItem as CustomerResponseDto;
            await RefreshDrawerAsync();
        }

        private void AdjustmentRequestsGrid_SelectionChanged(object? sender, EventArgs e)
        {
            _selectedRequest = adjustmentRequestsGrid.CurrentRow?.DataBoundItem as PointAdjustmentResponseDto;
            UpdateApprovalControlsEnabled();
        }

        private void UpdateSubmitRequestEnabled()
        {
            submitRequestBtn.Enabled = CanRequestAdjustment &&
                requestAmountNud.Value != 0 &&
                !string.IsNullOrWhiteSpace(requestReasonTxtBx.Text);
        }

        private void UpdateApprovalControlsEnabled()
        {
            approvalHintLabel.Text = _selectedRequest is { Status: "Pending" }
                ? $"Selected: {(_selectedRequest.PointsChange > 0 ? "+" : string.Empty)}{_selectedRequest.PointsChange} pts — {_selectedRequest.Reason}"
                : "Select a pending request above.";

            var canActOnSelection = CanApprove &&
                _selectedRequest is { Status: "Pending" } &&
                _selectedRequest.RequestedBy != EmployeeSession.Current?.EmployeeId;

            approveBtn.Enabled = canActOnSelection;
            rejectBtn.Enabled = canActOnSelection && !string.IsNullOrWhiteSpace(rejectionNotesTxtBx.Text);
        }

        private async void SubmitRequestBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedCustomer is null) return;

            try
            {
                await _controller.RequestAdjustmentAsync(
                    _selectedCustomer.CustomerId, (int)requestAmountNud.Value,
                    requestReasonTxtBx.Text, EmployeeSession.Current!.EmployeeId);

                requestStatusLabel.Text = "Submitted — waiting on admin approval.";
                requestAmountNud.Value = 0;
                requestReasonTxtBx.Text = string.Empty;
                await RefreshDrawerAsync();
            }
            catch (Exception ex)
            {
                requestStatusLabel.Text = ex.Message;
            }
        }

        private async void ApproveBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest is null || _selectedCustomer is null) return;

            try
            {
                await _controller.ApproveAdjustmentAsync(
                    _selectedRequest.PointAdjustmentRequestId,
                    EmployeeSession.Current!.EmployeeId);

                approvalStatusLabel.Text = "Approved — points applied.";
                rejectionNotesTxtBx.Text = string.Empty;

                var affectedCustomerId = _selectedCustomer.CustomerId;
                var approvedPoints = _selectedRequest.PointsChange;
                var approvedCustomerName = _selectedCustomer.FullName;

                await ReloadCustomersAsync();
                SelectCustomerRowById(affectedCustomerId);

                var sign = approvedPoints > 0 ? "+" : string.Empty;
                Toast.Show(this, $"Approved {sign}{approvedPoints} pts for {approvedCustomerName}.", ToastKind.Success);
            }
            catch (Exception ex)
            {
                approvalStatusLabel.Text = ex.Message;
            }
        }

        private async void RejectBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedRequest is null) return;

            try
            {
                await _controller.RejectAdjustmentAsync(
                    _selectedRequest.PointAdjustmentRequestId,
                    EmployeeSession.Current!.EmployeeId,
                    rejectionNotesTxtBx.Text);

                approvalStatusLabel.Text = "Rejected.";
                rejectionNotesTxtBx.Text = string.Empty;

                var rejectedPoints = _selectedRequest.PointsChange;
                var rejectedCustomerName = _selectedCustomer?.FullName ?? "customer";

                await RefreshDrawerAsync();

                var sign = rejectedPoints > 0 ? "+" : string.Empty;
                Toast.Show(this, $"Rejected {sign}{rejectedPoints} pts for {rejectedCustomerName}.", ToastKind.Info);
            }
            catch (Exception ex)
            {
                approvalStatusLabel.Text = ex.Message;
            }
        }

        private async void OpenOnboardingModal()
        {
            using var form = new FrmCustomerOnboarding(_controller);

            if (form.ShowDialog(FindForm()) == DialogResult.OK && form.CreatedCustomer != null)
            {
                await ReloadCustomersAsync();
                SelectCustomerRowById(form.CreatedCustomer.CustomerId);
            }
        }

        private async Task RefreshDrawerAsync()
        {
            var customer = _selectedCustomer;
            if (customer is null)
            {
                // Clear any lingering edit state so the next selection starts fresh.
                if (_isEditingCustomer)
                    ExitCustomerEditState();

                drawerEmptyLabel.Visible = true;
                drawerContentPanel.Visible = false;
                return;
            }

            drawerEmptyLabel.Visible = false;
            drawerContentPanel.Visible = true;

            // When the customer is inactive, the drawer becomes read-only: profile and
            // history remain visible, but every interactive element is hidden except the
            // Reactivate button. Matches how Manage Users locks down a deactivated employee.
            var customerIsActive = customer.IsActive;

            requestSectionPanel.Visible = CanRequestAdjustment && customerIsActive;
            approvalSectionPanel.Visible = CanApprove && customerIsActive;

            // If the user was mid-edit and the drawer is being repopulated (row change,
            // page change, save), abandon the edit silently rather than carrying stale
            // text into a different customer.
            if (_isEditingCustomer)
                ExitCustomerEditState();

            // Edit is only available on an active customer.
            _editCustomerBtn.Visible = CanEditCustomer && customerIsActive;

            // Deactivate/Reactivate stays visible for Managers regardless of state —
            // for an inactive customer it's the one actionable button.
            _deactivateCustomerBtn.Visible = CanEditCustomer;

            _deactivateCustomerBtn.Text = customerIsActive
                ? "Deactivate Customer"
                : "Reactivate Customer";
            _deactivateCustomerBtn.ForeColor = customerIsActive
                ? BrandPalette.StatusNegativeFill
                : BrandPalette.NeutralText;

            customerNameLabel.Text = customer.FullName;
            tierBadgeLabel.Text = $"{customer.Tier} Tier";
            pointsBalanceLabel.Text = $"{customer.PointsBalance:N0} pts";
            contactLabel.Text = string.IsNullOrWhiteSpace(customer.Email)
                ? customer.ContactNumber
                : $"{customer.ContactNumber}   •   {customer.Email}";

            // Status line — plain text, computed from the 14-day threshold in ActivityStatus.
            _statusValueLabel.Text =
                $"Status: {ActivityStatus.ToDisplayString(customer.LastVisitDate)}   •   " +
                $"Last visit: {ActivityStatus.FormatRelative(customer.LastVisitDate)}";

            try
            {
                var orders = await _controller.GetOrderHistoryAsync(customer.CustomerId);
                orderHistoryGrid.DataSource = null;
                orderHistoryGrid.DataSource = orders;

                var ledger = await _controller.GetLedgerAsync(customer.CustomerId);
                ledgerGrid.DataSource = null;
                ledgerGrid.DataSource = ledger;

                var pending = await _controller.GetPendingAdjustmentsAsync(customer.CustomerId);

                // Baristas see only their own pending requests — they filed them, and
                // they can't act on anyone else's. Managers see every pending request for
                // the customer because they're the ones deciding.
                if (EmployeeSession.Current?.Employee?.Role?.Equals("Barista", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var me = EmployeeSession.Current?.EmployeeId;
                    pending = pending.Where(r => r.RequestedBy == me).ToList();
                }

                _selectedRequest = null;
                adjustmentRequestsGrid.DataSource = null;
                adjustmentRequestsGrid.DataSource = pending;
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Error loading drawer");
            }

            requestAmountNud.Value = 0;
            requestReasonTxtBx.Text = string.Empty;
            requestStatusLabel.Text = string.Empty;
            UpdateSubmitRequestEnabled();

            rejectionNotesTxtBx.Text = string.Empty;
            approvalStatusLabel.Text = string.Empty;
            approvalHintLabel.Text = "Select a pending request above.";
            approveBtn.Enabled = false;
            rejectBtn.Enabled = false;
        }

        private void SelectCustomerRowById(int customerId)
        {
            foreach (DataGridViewRow row in customersGrid.Rows)
            {
                if (row.DataBoundItem is CustomerResponseDto c && c.CustomerId == customerId)
                {
                    customersGrid.ClearSelection();
                    row.Selected = true;
                    customersGrid.CurrentCell = row.Cells[0];
                    _selectedCustomer = c;
                    _ = RefreshDrawerAsync();
                    break;
                }
            }
        }

        private void ConfigureCustomersGridColumns()
        {
            customersGrid.Columns.Clear();
            customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(CustomerResponseDto.CustomerId),
                DataPropertyName = nameof(CustomerResponseDto.CustomerId),
                HeaderText = "ID",
                Width = 60,
            });
            customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(CustomerResponseDto.FullName),
                DataPropertyName = nameof(CustomerResponseDto.FullName),
                HeaderText = "Name",
                Width = 180,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            });
            customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(CustomerResponseDto.ContactNumber),
                DataPropertyName = nameof(CustomerResponseDto.ContactNumber),
                HeaderText = "Contact",
                Width = 120,
            });
            customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(CustomerResponseDto.PointsBalance),
                DataPropertyName = nameof(CustomerResponseDto.PointsBalance),
                HeaderText = "Points",
                Width = 80,
            });
            customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(CustomerResponseDto.Tier),
                DataPropertyName = nameof(CustomerResponseDto.Tier),
                HeaderText = "Tier",
                Width = 90,
            });
            customersGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(CustomerResponseDto.LastVisitDate),
                DataPropertyName = nameof(CustomerResponseDto.LastVisitDate),
                HeaderText = "Last Visit",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
            });
        }

        private void ConfigureOrderHistoryGridColumns()
        {
            orderHistoryGrid.Columns.Clear();
            orderHistoryGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(OrderHistoryDto.OrderId),
                DataPropertyName = nameof(OrderHistoryDto.OrderId),
                HeaderText = "Order #",
                Width = 60,
            });
            orderHistoryGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(OrderHistoryDto.OrderDate),
                DataPropertyName = nameof(OrderHistoryDto.OrderDate),
                HeaderText = "Date",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
            });
            orderHistoryGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(OrderHistoryDto.TotalAmount),
                DataPropertyName = nameof(OrderHistoryDto.TotalAmount),
                HeaderText = "Total",
                Width = 90,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "₱#,##0.00", Alignment = DataGridViewContentAlignment.MiddleRight },
            });
        }

        private void ConfigureLedgerGridColumns()
        {
            ledgerGrid.Columns.Clear();
            ledgerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(PointLedgerResponseDto.Timestamp),
                DataPropertyName = nameof(PointLedgerResponseDto.Timestamp),
                HeaderText = "Date",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
            });
            ledgerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(PointLedgerResponseDto.Type),
                DataPropertyName = nameof(PointLedgerResponseDto.Type),
                HeaderText = "Type",
                Width = 80,
            });
            ledgerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(PointLedgerResponseDto.PointsChange),
                DataPropertyName = nameof(PointLedgerResponseDto.PointsChange),
                HeaderText = "Pts",
                Width = 60,
            });
            ledgerGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(PointLedgerResponseDto.Reason),
                DataPropertyName = nameof(PointLedgerResponseDto.Reason),
                HeaderText = "Reason",
                Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            });
        }

        private void ConfigureAdjustmentRequestsGridColumns()
        {
            adjustmentRequestsGrid.Columns.Clear();
            adjustmentRequestsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(PointAdjustmentResponseDto.RequestedAt),
                DataPropertyName = nameof(PointAdjustmentResponseDto.RequestedAt),
                HeaderText = "Date",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
            });
            adjustmentRequestsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(PointAdjustmentResponseDto.PointsChange),
                DataPropertyName = nameof(PointAdjustmentResponseDto.PointsChange),
                HeaderText = "Pts",
                Width = 70,
            });
            adjustmentRequestsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(PointAdjustmentResponseDto.Status),
                DataPropertyName = nameof(PointAdjustmentResponseDto.Status),
                HeaderText = "Status",
                Width = 90,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            });
        }
    }
}