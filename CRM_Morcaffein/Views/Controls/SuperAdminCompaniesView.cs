using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Read-only list of companies in the SuperAdmin console. Columns: ID,
    /// Company Code, Name, Plan, Active, Created. Search box filters by name
    /// or code, client-side. Writes (create company, assign plan) come later.
    /// </summary>
    public class SuperAdminCompaniesView : UserControl
    {
        private readonly SuperAdminApi _api;

        private Panel _cardHeaderPanel = null!;
        private TextBox _searchTxtBx = null!;
        private Label _countLabel = null!;
        private DataGridView _grid = null!;
        private Label _emptyLabel = null!;
        private Panel _loadingPanel = null!;

        private List<SuperAdminCompanyDto> _allCompanies = new();

        public SuperAdminCompaniesView()
        {
            _api = new SuperAdminApi();

            BuildLayout();
            Load += async (s, e) => await ReloadAsync();
        }

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;
            BackColor = BrandPalette.SurfaceSecondary;

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

            // ---- Header: title + search ----
            _cardHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = BrandPalette.SurfacePrimary,
            };

            var title = new Label
            {
                Text = "Companies",
                Location = new Point(20, 16),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(13f),
                ForeColor = BrandPalette.NeutralText,
            };

            _searchTxtBx = new TextBox
            {
                Location = new Point(0, 18),  // repositioned in the Resize handler
                Size = new Size(300, 28),
                PlaceholderText = "Search by company name or code",
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _searchTxtBx.TextChanged += (s, e) => RefreshGrid();

            _countLabel = new Label
            {
                Location = new Point(0, 22),  // repositioned in the Resize handler
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };

            var headerDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BrandPalette.Border,
            };

            _cardHeaderPanel.Controls.Add(title);
            _cardHeaderPanel.Controls.Add(_searchTxtBx);
            _cardHeaderPanel.Controls.Add(_countLabel);
            _cardHeaderPanel.Controls.Add(headerDivider);

            // Right-align the search box and count label. Search sits next to
            // the right edge with an 16px inset; count label sits immediately
            // to the left of the search box.
            void PositionHeaderControls()
            {
                const int rightInset = 16;
                const int gap = 16;

                _searchTxtBx.Left = _cardHeaderPanel.ClientSize.Width
                                     - _searchTxtBx.Width - rightInset;
                _searchTxtBx.Top = 18;

                _countLabel.Left = _searchTxtBx.Left - _countLabel.Width - gap;
                _countLabel.Top = 22;
            }

            _cardHeaderPanel.Resize += (s, e) => PositionHeaderControls();
            PositionHeaderControls();

            // ---- Grid ----
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.None,
                GridColor = BrandPalette.Border,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowTemplate = { Height = 32 },
                Visible = false,
            };

            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BrandPalette.HeaderBackground,
                ForeColor = BrandPalette.HeaderText,
                Font = BrandPalette.BrandFontBold(9.5f),
                SelectionBackColor = BrandPalette.HeaderBackground,
                SelectionForeColor = BrandPalette.HeaderText,
                Padding = new Padding(8, 4, 8, 4),
            };
            _grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BrandPalette.SurfacePrimary,
                ForeColor = BrandPalette.NeutralText,
                Padding = new Padding(8, 6, 8, 6),
                SelectionBackColor = BrandPalette.RowSelected,
                SelectionForeColor = BrandPalette.NeutralText,
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(SuperAdminCompanyDto.CompanyId),
                DataPropertyName = nameof(SuperAdminCompanyDto.CompanyId),
                HeaderText = "ID",
                FillWeight = 8,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(SuperAdminCompanyDto.CompanyCode),
                DataPropertyName = nameof(SuperAdminCompanyDto.CompanyCode),
                HeaderText = "Code",
                FillWeight = 14,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(SuperAdminCompanyDto.CompanyName),
                DataPropertyName = nameof(SuperAdminCompanyDto.CompanyName),
                HeaderText = "Company Name",
                FillWeight = 32,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CurrentPlanName",
                DataPropertyName = nameof(SuperAdminCompanyDto.CurrentPlanName),
                HeaderText = "Plan",
                FillWeight = 16,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(SuperAdminCompanyDto.IsActive),
                DataPropertyName = nameof(SuperAdminCompanyDto.IsActive),
                HeaderText = "Active",
                FillWeight = 10,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(SuperAdminCompanyDto.CreatedAt),
                DataPropertyName = nameof(SuperAdminCompanyDto.CreatedAt),
                HeaderText = "Created",
                FillWeight = 20,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
            });

            // No-plan-assigned rows get a muted dash instead of blank.
            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;

                if (_grid.Columns[e.ColumnIndex].Name == "CurrentPlanName" && e.Value is null)
                {
                    e.Value = "—";
                    e.FormattingApplied = true;
                }
            };

            // Double-click a row to open its detail modal.
            _grid.CellDoubleClick += async (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (_grid.Rows[e.RowIndex].DataBoundItem is not SuperAdminCompanyDto company) return;

                using var detail = new FrmCompanyDetail(_api, company);
                detail.ShowDialog(FindForm());

                // Always reload after close — the plan may have changed, which
                // means the grid's CurrentPlanName column is stale.
                await ReloadAsync();
            };

            // ---- Empty state ----
            _emptyLabel = new Label
            {
                Text = "No companies found.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = BrandPalette.SurfacePrimary,
                Visible = false,
            };

            // ---- Loading state ----
            _loadingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                Visible = true,
            };
            var loadingLabel = new Label
            {
                Text = "Loading companies…",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _loadingPanel.Controls.Add(loadingLabel);

            // Compose: Fill-docked children get added first so they're behind,
            // header docks on top.
            card.Controls.Add(_loadingPanel);
            card.Controls.Add(_emptyLabel);
            card.Controls.Add(_grid);
            card.Controls.Add(_cardHeaderPanel);

            cardOuter.Controls.Add(card);
            Controls.Add(cardOuter);
        }

        private async Task ReloadAsync()
        {
            _loadingPanel.Visible = true;
            _grid.Visible = false;
            _emptyLabel.Visible = false;

            try
            {
                _allCompanies = await _api.GetCompaniesAsync();
            }
            catch (Exception ex)
            {
                _loadingPanel.Visible = false;
                _emptyLabel.Text = $"Could not load companies: {ex.Message}";
                _emptyLabel.Visible = true;
                return;
            }

            _loadingPanel.Visible = false;
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            var term = _searchTxtBx.Text?.Trim() ?? string.Empty;

            var filtered = string.IsNullOrEmpty(term)
                ? _allCompanies
                : _allCompanies.Where(c =>
                    c.CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.CompanyCode.Contains(term, StringComparison.OrdinalIgnoreCase))
                  .ToList();

            _countLabel.Text = filtered.Count == 1
                ? "1 company"
                : $"{filtered.Count} companies";

            _grid.DataSource = null;
            _grid.DataSource = filtered;

            if (filtered.Count == 0)
            {
                _grid.Visible = false;
                _emptyLabel.Text = string.IsNullOrEmpty(term)
                    ? "No companies registered yet."
                    : $"No companies match \"{term}\".";
                _emptyLabel.Visible = true;
            }
            else
            {
                _emptyLabel.Visible = false;
                _grid.Visible = true;
                _grid.ClearSelection();
            }
        }
    }
}