using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services;
using CRM_Morcaffein.winforms.Views;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Admin tab: manage the tenant's branches. Searchable grid + left drawer
    /// for the selected branch.
    ///
    /// Admin-only. Reached via the "Branches" nav item in FrmMain, which is
    /// itself gated by the Branching entitlement — so a tenant on Basic or
    /// Standard will never see this view. The API also enforces Admin-only
    /// on all write endpoints.
    ///
    /// This view is deliberately not wired for non-Admin roles: even if
    /// someone navigated here directly, the Create/Edit/Deactivate buttons
    /// check EmployeeSession role and disable themselves.
    /// </summary>
    public class BranchManagementView : UserControl
    {
        private readonly BranchService _service;
        private readonly BindingSource _branchesBindingSource = new();

        private BranchResponseDto? _selectedBranch;

        // Layout — mirrors ManageUsersView
        private Panel _cardHeaderPanel = null!;
        private TextBox _searchTxtBx = null!;
        private Button _newBranchBtn = null!;
        private DataGridView _grid = null!;
        private Panel _drawerPanel = null!;
        private Label _drawerEmptyLabel = null!;
        private Panel _drawerContentPanel = null!;
        private FlowLayoutPanel _drawerFlow = null!;
        private Panel _combinedBox = null!;
        private Label _nameLabel = null!;
        private Label _addressLabel = null!;
        private Label _idLabel = null!;
        private Label _statusLabel = null!;
        private Button _editBtn = null!;
        private Button _deactivateBtn = null!;

        private List<BranchResponseDto> _allBranches = new();

        public BranchManagementView()
        {
            _service = new BranchService();
            BuildLayout();
            ConfigureGridColumns();
            Load += async (s, e) => await ReloadAsync();
        }

        // =====================================================================
        // LAYOUT
        // =====================================================================

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;
            BackColor = BrandPalette.SurfaceSecondary;

            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
            };

            // ---------- LEFT DRAWER ----------
            _drawerPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 380,
                BackColor = BrandPalette.SurfaceCard,
            };

            _drawerEmptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Text = "Select a branch to view details.",
            };

            _drawerContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                AutoScroll = true,
            };

            _drawerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20),
                Width = 380,
            };

            // ---------- Combined box: profile + divider + actions ----------
            _combinedBox = new Panel
            {
                Size = new Size(340, 300),
                BackColor = BrandPalette.SurfaceCard,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0),
            };

            _nameLabel = new Label
            {
                Location = new Point(16, 16),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(13f),
                ForeColor = BrandPalette.NeutralText,
            };
            _idLabel = new Label
            {
                Location = new Point(16, 46),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _addressLabel = new Label
            {
                Location = new Point(16, 72),
                Size = new Size(308, 44),
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralText,
            };
            _statusLabel = new Label
            {
                Location = new Point(16, 124),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(9.5f),
                ForeColor = BrandPalette.NeutralText,
            };

            var dividerLine = new Panel
            {
                Location = new Point(16, 156),
                Size = new Size(308, 1),
                BackColor = BrandPalette.Border,
            };

            _editBtn = MakeActionButton("Edit Branch", 176);
            _editBtn.Click += (s, e) => EditSelected();

            _deactivateBtn = MakeActionButton("Deactivate Branch", 218);
            _deactivateBtn.ForeColor = BrandPalette.StatusNegativeFill;
            _deactivateBtn.Click += (s, e) => DeactivateSelected();

            _combinedBox.Controls.Add(_deactivateBtn);
            _combinedBox.Controls.Add(_editBtn);
            _combinedBox.Controls.Add(dividerLine);
            _combinedBox.Controls.Add(_statusLabel);
            _combinedBox.Controls.Add(_addressLabel);
            _combinedBox.Controls.Add(_idLabel);
            _combinedBox.Controls.Add(_nameLabel);

            _drawerFlow.Controls.Add(_combinedBox);
            _drawerContentPanel.Controls.Add(_drawerFlow);

            _drawerPanel.Controls.Add(_drawerEmptyLabel);
            _drawerPanel.Controls.Add(_drawerContentPanel);

            // ---------- RIGHT SIDE: card with grid ----------
            var cardOuter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
                Padding = new Padding(24),
            };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
            };

            _cardHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _searchTxtBx = new TextBox
            {
                Location = new Point(16, 16),
                Size = new Size(300, 28),
                PlaceholderText = "Search by name or address",
                Font = BrandPalette.BrandFontRegular(10f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            _searchTxtBx.TextChanged += (s, e) => ApplyFilter();

            _newBranchBtn = new Button
            {
                Text = "+ New Branch",
                Size = new Size(150, 32),
                Location = new Point(16, 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _newBranchBtn.FlatAppearance.BorderSize = 0;
            _newBranchBtn.Click += (s, e) => CreateNew();

            var divider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BrandPalette.Border,
            };

            _cardHeaderPanel.Controls.Add(_newBranchBtn);
            _cardHeaderPanel.Controls.Add(_searchTxtBx);
            _cardHeaderPanel.Controls.Add(divider);

            _cardHeaderPanel.Resize += (s, e) =>
            {
                _newBranchBtn.Left = _cardHeaderPanel.ClientSize.Width - _newBranchBtn.Width - 16;
                _newBranchBtn.Top = 14;
            };

            // ---------- Grid ----------
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
            };
            ConfigureGrid(_grid);
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.SelectionChanged += (s, e) =>
            {
                _selectedBranch = _grid.CurrentRow?.DataBoundItem as BranchResponseDto;
                RefreshDrawer();
            };

            card.Controls.Add(_grid);
            card.Controls.Add(_cardHeaderPanel);
            cardOuter.Controls.Add(card);

            bodyPanel.Controls.Add(cardOuter);
            bodyPanel.Controls.Add(_drawerPanel);
            Controls.Add(bodyPanel);

            // Hide the + New Branch button for non-Admins (shouldn't be
            // reachable given the nav gating, but defense in depth).
            var isAdmin = EmployeeSession.Current?.Employee?.Role?
                .Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
            _newBranchBtn.Visible = isAdmin;
        }

        private static Button MakeActionButton(string text, int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(14, y),
                Size = new Size(312, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // =====================================================================
        // GRID
        // =====================================================================

        private void ConfigureGridColumns()
        {
            _grid.Columns.Clear();
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(BranchResponseDto.BranchId),
                DataPropertyName = nameof(BranchResponseDto.BranchId),
                HeaderText = "ID",
                Width = 60,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(BranchResponseDto.BranchName),
                DataPropertyName = nameof(BranchResponseDto.BranchName),
                HeaderText = "Branch Name",
                Width = 200,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(BranchResponseDto.Address),
                DataPropertyName = nameof(BranchResponseDto.Address),
                HeaderText = "Address",
                Width = 300,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(BranchResponseDto.IsActive),
                DataPropertyName = nameof(BranchResponseDto.IsActive),
                HeaderText = "Active",
                Width = 70,
            });
        }

        private static void ConfigureGrid(DataGridView grid)
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
            grid.Font = BrandPalette.BrandFontRegular(9.5f);
            grid.ColumnHeadersDefaultCellStyle.BackColor = BrandPalette.HeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = BrandPalette.HeaderText;
            grid.ColumnHeadersDefaultCellStyle.Font = BrandPalette.BrandFontBold(9.5f);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = BrandPalette.HeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = BrandPalette.HeaderText;
            grid.ColumnHeadersHeight = 36;
            grid.DefaultCellStyle.SelectionBackColor = BrandPalette.RowSelected;
            grid.DefaultCellStyle.SelectionForeColor = BrandPalette.NeutralText;
            grid.DefaultCellStyle.BackColor = BrandPalette.SurfacePrimary;
            grid.DefaultCellStyle.ForeColor = BrandPalette.NeutralText;
            grid.RowTemplate.Height = 30;
            grid.ScrollBars = ScrollBars.Vertical;
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_grid.Rows[e.RowIndex].DataBoundItem is not BranchResponseDto branch)
                return;

            // IsActive bool → "Yes" / "No"
            if (_grid.Columns[e.ColumnIndex].Name == nameof(BranchResponseDto.IsActive))
            {
                e.Value = branch.IsActive ? "Active" : "Inactive";
                e.FormattingApplied = true;
            }

            // Inactive rows render greyed out, matching the pattern in
            // ManageUsersView and CustomerManagementView.
            if (!branch.IsActive)
            {
                var greyText = Color.FromArgb(255, 130, 130, 130);
                var greyBackground = Color.FromArgb(255, 240, 240, 240);

                e.CellStyle.ForeColor = greyText;
                e.CellStyle.BackColor = greyBackground;
                e.CellStyle.SelectionForeColor = greyText;
                e.CellStyle.SelectionBackColor = greyBackground;
            }
        }

        // =====================================================================
        // LOADING
        // =====================================================================

        private async Task ReloadAsync()
        {
            try
            {
                // Admin view — use GetAllAsync so inactive branches show up
                // and can be reactivated. Non-admins never reach this view.
                _allBranches = await _service.GetAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Error loading branches");
                return;
            }

            ApplyFilter();
        }

        /// <summary>
        /// Client-side search filter. Matches branch name or address
        /// (case-insensitive substring).
        /// </summary>
        private void ApplyFilter()
        {
            var term = _searchTxtBx.Text?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(term)
                ? _allBranches
                : _allBranches.Where(b =>
                    (b.BranchName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (b.Address?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                  .ToList();

            _branchesBindingSource.DataSource = filtered;
            _grid.DataSource = _branchesBindingSource;

            _grid.ClearSelection();
            try { _grid.CurrentCell = null; } catch { }

            _selectedBranch = null;
            RefreshDrawer();
        }

        // =====================================================================
        // DRAWER
        // =====================================================================

        private void RefreshDrawer()
        {
            var branch = _selectedBranch;
            if (branch is null)
            {
                _drawerEmptyLabel.Visible = true;
                _drawerContentPanel.Visible = false;
                return;
            }

            _drawerEmptyLabel.Visible = false;
            _drawerContentPanel.Visible = true;

            _nameLabel.Text = branch.BranchName;
            _idLabel.Text = $"Branch #{branch.BranchId}";
            _addressLabel.Text = string.IsNullOrWhiteSpace(branch.Address)
                ? "(no address)"
                : branch.Address;
            _statusLabel.Text = branch.IsActive ? "Active" : "Inactive";
            _statusLabel.ForeColor = branch.IsActive
                ? BrandPalette.StatusPositiveText
                : BrandPalette.StatusNegativeFill;

            // Deactivate button doubles as reactivate. Text and color flip
            // with the branch's state, matching the ManageUsersView pattern.
            _deactivateBtn.Text = branch.IsActive
                ? "Deactivate Branch"
                : "Reactivate Branch";
            _deactivateBtn.ForeColor = branch.IsActive
                ? BrandPalette.StatusNegativeFill
                : BrandPalette.NeutralText;
        }

        // =====================================================================
        // ACTIONS
        // =====================================================================

        private async void CreateNew()
        {
            using var modal = FrmBranchDetail.ForCreate(_service);
            if (modal.ShowDialog(FindForm()) == DialogResult.OK && modal.Result is not null)
            {
                await ReloadAsync();
                SelectBranchRowById(modal.Result.BranchId);
            }
        }

        private async void EditSelected()
        {
            if (_selectedBranch is null) return;

            using var modal = FrmBranchDetail.ForEdit(_service, _selectedBranch);
            if (modal.ShowDialog(FindForm()) == DialogResult.OK && modal.Result is not null)
            {
                var id = modal.Result.BranchId;
                await ReloadAsync();
                SelectBranchRowById(id);
            }
        }

        private async void DeactivateSelected()
        {
            if (_selectedBranch is null) return;

            var branch = _selectedBranch;
            var activating = !branch.IsActive;

            var confirm = MessageBox.Show(
                FindForm(),
                activating
                    ? $"Reactivate {branch.BranchName}? It will appear in dropdowns and the topbar selector again."
                    : $"Deactivate {branch.BranchName}? It will be hidden from dropdowns and the topbar selector, but historical orders, campaigns, and feedback remain attached.",
                activating ? "Reactivate Branch" : "Deactivate Branch",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                _deactivateBtn.Enabled = false;

                if (activating)
                    await _service.ReactivateAsync(branch.BranchId);
                else
                    await _service.DeactivateAsync(branch.BranchId);

                Toast.Show(this,
                    activating
                        ? $"Reactivated {branch.BranchName}."
                        : $"Deactivated {branch.BranchName}.",
                    ToastKind.Success);

                await ReloadAsync();
                SelectBranchRowById(branch.BranchId);
            }
            catch (Exception ex)
            {
                // Server-side guards surface here — e.g. "Cannot deactivate the
                // only active branch" or "N active employees are still assigned".
                MessageBox.Show(FindForm(), ex.Message,
                    activating ? "Reactivate failed" : "Deactivate failed");
            }
            finally
            {
                _deactivateBtn.Enabled = true;
            }
        }

        private void SelectBranchRowById(int branchId)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.DataBoundItem is BranchResponseDto b && b.BranchId == branchId)
                {
                    _grid.ClearSelection();
                    row.Selected = true;
                    _grid.CurrentCell = row.Cells[0];
                    _selectedBranch = b;
                    RefreshDrawer();
                    break;
                }
            }
        }
    }
}