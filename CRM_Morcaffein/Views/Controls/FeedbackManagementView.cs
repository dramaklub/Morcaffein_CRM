using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// The Feedback tab: ticket grid + read-only drawer. Staff log and view; deciding
    /// (resolve/dismiss/award) is a manager concern on a future surface.
    ///
    /// Staff can also CANCEL their own pending tickets (soft-delete, only the employee
    /// who logged it). Cancelling hides the ticket from the grid.
    ///
    /// API-backed: all ticket data comes from the API. No mock.
    /// </summary>
    public partial class FeedbackManagementView : UserControl
    {
        private readonly FeedbackController _controller;
        private readonly BindingSource _ticketsBindingSource = new();

        private FeedbackResponseDto? _selectedTicket;
        private Button _cancelTicketBtn = null!;

        // Status filter dropdown — built in code, inserted into cardHeaderPanel below the
        // existing search row. Mirrors the TransactionView "Sort by Category" pattern.
        private Panel _statusFilterRow = null!;
        private ComboBox _statusFilterCombo = null!;

        private const string StatusAllOption = "All Statuses";
        private static readonly string[] StatusOptions = { "Pending", "Resolved", "Cancelled" };

        private Panel _decisionPanel = null!;
        private NumericUpDown _bonusPointsNud = null!;
        private TextBox _decisionNotesTxtBx = null!;
        private Button _resolveBtn = null!;
        private Label _decisionErrorLabel = null!;

        private const int MaxFeedbackBonusPoints = 50;

        public FeedbackManagementView()
        {
            _controller = new FeedbackController();
            InitializeComponent();

            ConfigureTicketsGridColumns();

            pagination.PageChanged += async (s, page) => await ReloadTicketsAsync(page);

            ticketsGrid.DataSource = _ticketsBindingSource;
            ticketsGrid.SelectionChanged += TicketsGrid_SelectionChanged;

            searchTxtBx.TextChanged += (s, e) => ReloadTickets();
            logFeedbackBtn.Click += (s, e) => OpenLogFeedbackModal();

            cardHeaderPanel.Resize += (s, e) => PositionActionButton();
            PositionActionButton();

            BuildDecisionPanel();
            BuildCancelButton();
            BuildStatusFilterRow();

            // Only Baristas log feedback. Managers decide on it. The API enforces this
            // server-side too (FeedbackController.LogFeedback rejects non-Baristas).
            logFeedbackBtn.Visible =
                EmployeeSession.Current?.Employee?.Role?.Equals("Barista", StringComparison.OrdinalIgnoreCase) == true;

            Load += async (s, e) => await ReloadTicketsAsync();
        }
        /// <summary>
        /// Builds the Manager-only decision panel and inserts it into drawerFlow. Visible
        /// only when the current employee is a Manager AND the selected ticket is Pending;
        /// RefreshDrawer toggles visibility.
        ///
        /// One button — "Resolve Ticket". BonusPoints == 0 means "resolved, no gesture"
        /// (the dismiss case); > 0 awards that many points to the customer. Notes optional.
        /// </summary>
        private void BuildDecisionPanel()
        {
            _decisionPanel = new Panel
            {
                Size = new Size(420, 200),
                BackColor = BrandPalette.SurfaceCard,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Margin = new Padding(0, 16, 0, 16),
            };

            const int inset = 14;

            var header = new Label
            {
                Text = "Resolve Ticket",
                Location = new Point(inset, 12),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(9.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var hint = new Label
            {
                Text = "0 points = resolved without a gesture. Up to 50 for a recovery.",
                Location = new Point(inset, 34),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(8f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            var pointsLabel = new Label
            {
                Text = "Bonus points",
                Location = new Point(inset, 60),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            _bonusPointsNud = new NumericUpDown
            {
                Location = new Point(inset, 80),
                Size = new Size(100, 26),
                Minimum = 0,
                Maximum = MaxFeedbackBonusPoints,
                Value = 0,
                Font = BrandPalette.BrandFontRegular(9.5f),
                TextAlign = HorizontalAlignment.Center,
            };

            _decisionNotesTxtBx = new TextBox
            {
                Location = new Point(inset, 112),
                Size = new Size(392, 26),
                PlaceholderText = "Notes (optional)",
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };

            _resolveBtn = new Button
            {
                Text = "Resolve Ticket",
                Location = new Point(inset, 146),
                Size = new Size(392, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
            };
            _resolveBtn.FlatAppearance.BorderSize = 0;
            _resolveBtn.Click += ResolveBtn_Click;

            _decisionErrorLabel = new Label
            {
                Location = new Point(inset, 182),
                Size = new Size(392, 20),
                AutoSize = false,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                BackColor = Color.Transparent,
                Visible = false,
            };

            _decisionPanel.Controls.Add(_decisionErrorLabel);
            _decisionPanel.Controls.Add(_resolveBtn);
            _decisionPanel.Controls.Add(_decisionNotesTxtBx);
            _decisionPanel.Controls.Add(_bonusPointsNud);
            _decisionPanel.Controls.Add(pointsLabel);
            _decisionPanel.Controls.Add(hint);
            _decisionPanel.Controls.Add(header);

            drawerFlow.Controls.Add(_decisionPanel);
        }


        /// <summary>
        /// Adds the "Cancel Ticket" button to the drawer's bottom. Built here in code
        /// rather than the Designer so the drawer layout doesn't need to be touched.
        /// </summary>
        private void BuildCancelButton()
        {
            _cancelTicketBtn = new Button
            {
                Text = "Cancel Ticket",
                Width = 420,               // matches the width of the review box in the flow
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Visible = false,
                Margin = new Padding(0, 16, 0, 0),   // gap above the button
            };
            _cancelTicketBtn.FlatAppearance.BorderSize = 0;
            _cancelTicketBtn.Click += CancelTicketBtn_Click;

            // Add to the drawer's flow panel so it appears right after the review content,
            // near the ticket details — not pinned to the bottom of the whole drawer.
            drawerFlow.Controls.Add(_cancelTicketBtn);
        }

        private void PositionActionButton()
        {
            logFeedbackBtn.Left = cardHeaderPanel.ClientSize.Width - logFeedbackBtn.Width - 16;
            logFeedbackBtn.Top = 14;
        }

        /// <summary>
        /// Builds the status filter row and inserts it into the card header, below the
        /// existing search box. Mirrors the TransactionView "Sort by Category" pattern:
        /// a small label, then a dropdown. The card header was 60px tall for search + button;
        /// this needs another ~40px below the search row, so we grow the header.
        ///
        /// Changing the selection re-queries the API with the new status filter applied.
        /// Default selection is "Pending" — resolved tickets are hidden until the user
        /// explicitly picks another option.
        /// </summary>
        private void BuildStatusFilterRow()
        {
            // Grow the header so the new row has space below the search box.
            // Search box lives at y=16..44; filter row starts at y=52.
            cardHeaderPanel.Height = 92;

            _statusFilterRow = new Panel
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

            _statusFilterCombo = new ComboBox
            {
                Size = new Size(180, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };

            // "All Statuses" first, then the explicit statuses. Same shape as the
            // TransactionView category dropdown.
            _statusFilterCombo.Items.Add(StatusAllOption);
            foreach (var s in StatusOptions)
                _statusFilterCombo.Items.Add(s);

            // Default to Pending — the pending queue is the Manager's working set.
            _statusFilterCombo.SelectedItem = "Pending";

            // Position: label first, combo after, computed from the label's measured width
            // so they can't collide at any DPI.
            var labelWidth = TextRenderer.MeasureText(label.Text, label.Font).Width;
            label.Left = 0;
            label.Top = (30 - label.Height) / 2;

            _statusFilterCombo.Left = labelWidth + 12;
            _statusFilterCombo.Top = (30 - _statusFilterCombo.Height) / 2;

            _statusFilterCombo.SelectedIndexChanged += async (s, e) =>
            {
                // A filter change is always a fresh page-1 query — same as a search change.
                await ReloadTicketsAsync();
            };

            _statusFilterRow.Controls.Add(_statusFilterCombo);
            _statusFilterRow.Controls.Add(label);

            cardHeaderPanel.Controls.Add(_statusFilterRow);
            _statusFilterRow.BringToFront();
        }

        /// <summary>
        /// The currently-selected status filter, or null when "All Statuses" is picked.
        /// Null means "no status filter" — the API returns every status.
        /// </summary>
        private string? SelectedStatusFilter =>
            _statusFilterCombo?.SelectedItem is string s && s != StatusAllOption
                ? s
                : null;

        private async Task ReloadTicketsAsync(int? page = null)
        {
            var targetPage = page ?? 1;

            // A null page (search change or initial load) always resets to page 1.
            if (page is null)
                pagination.ResetToFirstPage();

            PagedResponse<FeedbackResponseDto> response;
            try
            {
                response = await _controller.GetTicketsAsync(
                    searchTxtBx.Text, targetPage, 25, SelectedStatusFilter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Error loading tickets");
                return;
            }

            _ticketsBindingSource.DataSource = response.Items;

            pagination.UpdatePageInfo(response.TotalCount, response.Page, response.PageSize);

            ticketsGrid.ClearSelection();
            try { ticketsGrid.CurrentCell = null; } catch { /* not always allowed; ignore */ }

            // Page changes (and search changes) reset the drawer.
            _selectedTicket = null;
            RefreshDrawer();
        }

        private async void ReloadTickets()
        {
            try { await ReloadTicketsAsync(); }
            catch (Exception ex) { MessageBox.Show(FindForm(), ex.Message, "Error loading tickets"); }
        }

        private void TicketsGrid_SelectionChanged(object? sender, EventArgs e)
        {
            _selectedTicket = ticketsGrid.CurrentRow?.DataBoundItem as FeedbackResponseDto;
            RefreshDrawer();
        }

        private async void OpenLogFeedbackModal()
        {
            using var form = new FrmLogFeedback(_controller, EmployeeSession.Current!.EmployeeId);

            if (form.ShowDialog(FindForm()) == DialogResult.OK && form.CreatedFeedback != null)
            {
                await ReloadTicketsAsync();
                SelectTicketRowById(form.CreatedFeedback.TicketId);
            }
        }

        private void RefreshDrawer()
        {
            var ticket = _selectedTicket;
            if (ticket is null)
            {
                drawerEmptyLabel.Visible = true;
                drawerContentPanel.Visible = false;
                _cancelTicketBtn.Visible = false;
                _decisionPanel.Visible = false;
                return;
            }

            drawerEmptyLabel.Visible = false;
            drawerContentPanel.Visible = true;

            ticketCustomerLabel.Text = ticket.CustomerName;
            ticketRatingLabel.Text = new string('★', ticket.Rating) + new string('☆', 5 - ticket.Rating);
            ticketCategoryLabel.Text = $"Category: {ticket.Category}";
            ticketStatusLabel.Text = $"Status: {ticket.Status}";
            ticketDateLabel.Text = $"Logged {ticket.DateLogged:yyyy-MM-dd HH:mm} by {ticket.LoggedByName}";
            ticketNotesLabel.Text = string.IsNullOrWhiteSpace(ticket.StaffNotes) ? "(no notes)" : ticket.StaffNotes;

            // Cancel button is visible only when:
            //   - ticket status is Pending (not already resolved/dismissed/cancelled)
            //   - the currently logged-in employee is the one who logged the ticket
            var currentEmployeeId = EmployeeSession.Current?.EmployeeId;
            _cancelTicketBtn.Visible =
                ticket.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) &&
                ticket.LoggedBy == currentEmployeeId;

            // Decision panel is Manager-only and only for Pending tickets.
            var isManager = EmployeeSession.Current?.Employee?.Role?
                .Equals("Manager", StringComparison.OrdinalIgnoreCase) == true;

            _decisionPanel.Visible =
                isManager &&
                ticket.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);

            // Reset panel state on every drawer refresh.
            _bonusPointsNud.Value = 0;
            _decisionNotesTxtBx.Text = string.Empty;
            _decisionErrorLabel.Text = string.Empty;
            _decisionErrorLabel.Visible = false;
        }
        private async void ResolveBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedTicket is null) return;

            _decisionErrorLabel.Text = string.Empty;
            _decisionErrorLabel.Visible = false;

            var bonus = (int)_bonusPointsNud.Value;
            var notes = string.IsNullOrWhiteSpace(_decisionNotesTxtBx.Text)
                ? null
                : _decisionNotesTxtBx.Text.Trim();

            try
            {
                _resolveBtn.Enabled = false;

                var employeeId = EmployeeSession.Current?.EmployeeId
                    ?? throw new InvalidOperationException("No employee session.");

                var ticketId = _selectedTicket.TicketId;
                var customerName = _selectedTicket.CustomerName;

                await _controller.ResolveAsync(ticketId, employeeId, bonus, notes);

                // Compose the toast message based on whether a gesture was awarded.
                var message = bonus > 0
                    ? $"Resolved ticket #{ticketId} for {customerName} — +{bonus} pts."
                    : $"Resolved ticket #{ticketId} for {customerName}.";

                Toast.Show(this, message, ToastKind.Success);

                await ReloadTicketsAsync();
            }
            catch (Exception ex)
            {
                _decisionErrorLabel.Text = ex.Message;
                _decisionErrorLabel.Visible = true;
            }
            finally
            {
                _resolveBtn.Enabled = true;
            }
        }

        private async void CancelTicketBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedTicket is null) return;

            var confirm = MessageBox.Show(
                FindForm(),
                $"Are you sure you want to cancel ticket #{_selectedTicket.TicketId}?",
                "Cancel Ticket",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                var employeeId = EmployeeSession.Current?.EmployeeId
                    ?? throw new InvalidOperationException("No employee session.");

                var ticketId = _selectedTicket.TicketId;
                await _controller.CancelTicketAsync(ticketId, employeeId);

                MessageBox.Show(FindForm(),
                    $"Ticket #{ticketId} has been cancelled.",
                    "Ticket Cancelled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await ReloadTicketsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Cancel failed");
            }
        }

        private void SelectTicketRowById(int ticketId)
        {
            foreach (DataGridViewRow row in ticketsGrid.Rows)
            {
                if (row.DataBoundItem is FeedbackResponseDto t && t.TicketId == ticketId)
                {
                    ticketsGrid.ClearSelection();
                    row.Selected = true;
                    ticketsGrid.CurrentCell = row.Cells[0];
                    _selectedTicket = t;
                    RefreshDrawer();
                    break;
                }
            }
        }

        private void ConfigureTicketsGridColumns()
        {
            ticketsGrid.Columns.Clear();
            ticketsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(FeedbackResponseDto.TicketId),
                DataPropertyName = nameof(FeedbackResponseDto.TicketId),
                HeaderText = "Ticket #",
                Width = 70,
            });
            ticketsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(FeedbackResponseDto.CustomerName),
                DataPropertyName = nameof(FeedbackResponseDto.CustomerName),
                HeaderText = "Customer",
                Width = 160,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            });
            ticketsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(FeedbackResponseDto.Rating),
                DataPropertyName = nameof(FeedbackResponseDto.Rating),
                HeaderText = "Rating",
                Width = 60,
            });
            ticketsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(FeedbackResponseDto.Category),
                DataPropertyName = nameof(FeedbackResponseDto.Category),
                HeaderText = "Category",
                Width = 90,
            });
            ticketsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(FeedbackResponseDto.Status),
                DataPropertyName = nameof(FeedbackResponseDto.Status),
                HeaderText = "Status",
                Width = 90,
            });
            ticketsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(FeedbackResponseDto.DateLogged),
                DataPropertyName = nameof(FeedbackResponseDto.DateLogged),
                HeaderText = "Logged",
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" },
            });
        }
    }
}