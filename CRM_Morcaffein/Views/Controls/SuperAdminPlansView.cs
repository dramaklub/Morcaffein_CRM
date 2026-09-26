using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Read-only list of subscription plans. Columns: Name, Description,
    /// Monthly Price, Active. Modules are shown in a second line under each
    /// plan name inside the grid via the CellFormatting handler — the raw CSV
    /// is not pretty in a cell, but the modules list is what actually matters
    /// at a glance.
    /// </summary>
    public class SuperAdminPlansView : UserControl
    {
        private readonly SuperAdminApi _api;

        private Panel _cardHeaderPanel = null!;
        private Label _countLabel = null!;
        private DataGridView _grid = null!;
        private Label _emptyLabel = null!;
        private Panel _loadingPanel = null!;

        public SuperAdminPlansView()
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

            // ---- Header ----
            _cardHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = BrandPalette.SurfacePrimary,
            };

            var title = new Label
            {
                Text = "Subscription Plans",
                Location = new Point(20, 16),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(13f),
                ForeColor = BrandPalette.NeutralText,
            };

            _countLabel = new Label
            {
                Location = new Point(20, 42),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            var headerDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BrandPalette.Border,
            };

            _cardHeaderPanel.Controls.Add(title);
            _cardHeaderPanel.Controls.Add(_countLabel);
            _cardHeaderPanel.Controls.Add(headerDivider);

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
                RowTemplate = { Height = 34 },
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
                Name = nameof(SuperAdminPlanDto.Name),
                DataPropertyName = nameof(SuperAdminPlanDto.Name),
                HeaderText = "Plan",
                FillWeight = 15,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = BrandPalette.BrandFontBold(10f),
                    ForeColor = BrandPalette.Brand,
                    SelectionBackColor = BrandPalette.RowSelected,
                    SelectionForeColor = BrandPalette.Brand,
                },
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(SuperAdminPlanDto.Description),
                DataPropertyName = nameof(SuperAdminPlanDto.Description),
                HeaderText = "Description",
                FillWeight = 45,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ModulesDisplay",
                DataPropertyName = nameof(SuperAdminPlanDto.ModulesCsv),
                HeaderText = "Included Modules",
                FillWeight = 30,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(SuperAdminPlanDto.MonthlyPrice),
                DataPropertyName = nameof(SuperAdminPlanDto.MonthlyPrice),
                HeaderText = "Monthly",
                FillWeight = 12,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "₱#,##0",
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                },
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nameof(SuperAdminPlanDto.IsActive),
                DataPropertyName = nameof(SuperAdminPlanDto.IsActive),
                HeaderText = "Active",
                FillWeight = 8,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                },
            });

            // Cells that need to render differently than their raw value:
            //   - ModulesDisplay: "DataCollection,Transactions,Feedback" → "Data Collection · Transactions · Feedback"
            //   - IsActive: bool → "Active" / "Inactive"
            _grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (_grid.Rows[e.RowIndex].DataBoundItem is not SuperAdminPlanDto plan) return;

                if (_grid.Columns[e.ColumnIndex].Name == "ModulesDisplay")
                {
                    e.Value = HumanizeModules(plan.ModulesCsv);
                    e.FormattingApplied = true;
                }
                else if (_grid.Columns[e.ColumnIndex].Name == nameof(SuperAdminPlanDto.IsActive))
                {
                    e.Value = plan.IsActive ? "Yes" : "No";
                    e.FormattingApplied = true;
                }
            };

            // ---- Empty state ----
            _emptyLabel = new Label
            {
                Text = "No plans configured.",
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
                Text = "Loading plans…",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _loadingPanel.Controls.Add(loadingLabel);

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
                var plans = await _api.GetPlansAsync();

                _countLabel.Text = plans.Count == 1
                    ? "1 plan"
                    : $"{plans.Count} plans";

                if (plans.Count == 0)
                {
                    _loadingPanel.Visible = false;
                    _emptyLabel.Visible = true;
                    return;
                }

                _grid.DataSource = null;
                _grid.DataSource = plans;

                _loadingPanel.Visible = false;
                _emptyLabel.Visible = false;
                _grid.Visible = true;
                _grid.ClearSelection();
            }
            catch (Exception ex)
            {
                _loadingPanel.Visible = false;
                _emptyLabel.Text = $"Could not load plans: {ex.Message}";
                _emptyLabel.Visible = true;
            }
        }

        /// <summary>
        /// Converts the raw ModulesCsv ("DataCollection,Transactions,Feedback")
        /// into a friendlier form for display: "Data Collection · Transactions · Feedback".
        /// </summary>
        private static string HumanizeModules(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return "—";

            var names = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(key => key switch
                {
                    "DataCollection" => "Data Collection",
                    "Transactions" => "Transactions",
                    "Feedback" => "Feedback",
                    "Retention" => "Retention",
                    "BI" => "Business Intelligence",
                    "BranchManagement" => "Branch Management",
                    _ => key,
                });

            return string.Join("  ·  ", names);
        }
    }
}