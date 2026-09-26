using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Modal dialog for Promos Redeemed KPI pod — campaign redemption breakdown.
    /// </summary>
    public class PromosRedeemedDetailModal : Form
    {
        public PromosRedeemedDetailModal(CampaignRoiDto data)
        {
            Text            = "Promotion Redemptions";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ClientSize      = new Size(640, 480);
            BackColor       = BrandPalette.SurfacePrimary;
            Font            = BrandPalette.BrandFontRegular(9.5f);
            Padding         = new Padding(22);

            BuildLayout(data);
        }

        private void BuildLayout(CampaignRoiDto data)
        {
            var totalRedemptions = data.Campaigns.Sum(c => c.TotalRedemptions);
            var totalDiscounts = data.Campaigns.Sum(c => c.TotalDiscountCost);

            // ── Top Header Panel ──
            var headerPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 58,
                BackColor = BrandPalette.SurfacePrimary,
            };

            var titleLabel = new Label
            {
                Text      = "Promotions & Campaign Redemptions",
                Location  = new Point(0, 0),
                Size      = new Size(596, 26),
                Font      = BrandPalette.BrandFontBold(13.5f),
                ForeColor = BrandPalette.NeutralText,
            };

            var subtitleLabel = new Label
            {
                Text      = $"Total Redemptions: {totalRedemptions:N0}  ·  Total Savings Given: ₱{totalDiscounts:N2}",
                Location  = new Point(0, 26),
                Size      = new Size(596, 20),
                Font      = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            var divider = new Panel
            {
                Location  = new Point(0, 48),
                Size      = new Size(596, 1),
                BackColor = BrandPalette.Border,
            };

            headerPanel.Controls.Add(divider);
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(titleLabel);

            // ── Bottom Button Row ──
            var btnRow = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 44,
                BackColor = BrandPalette.SurfacePrimary,
            };
            var closeBtn = new Button
            {
                Text      = "Close",
                Width     = 110,
                Height    = 36,
                Dock      = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font      = BrandPalette.BrandFontBold(9.5f),
                Cursor    = Cursors.Hand,
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => Close();
            btnRow.Controls.Add(closeBtn);

            // ── Center Grid ──
            var gridPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(0, 10, 0, 10),
                BackColor = BrandPalette.SurfacePrimary,
            };

            var grid = new DataGridView
            {
                Dock                          = DockStyle.Fill,
                ReadOnly                      = true,
                AllowUserToAddRows            = false,
                AllowUserToDeleteRows         = false,
                AllowUserToResizeRows         = false,
                AllowUserToOrderColumns       = false,
                EnableHeadersVisualStyles     = false,
                RowHeadersVisible             = false,
                BackgroundColor               = BrandPalette.SurfacePrimary,
                BorderStyle                   = BorderStyle.None,
                GridColor                     = BrandPalette.Border,
                CellBorderStyle               = DataGridViewCellBorderStyle.SingleHorizontal,
                SelectionMode                 = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode           = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight           = 32,
                ColumnHeadersHeightSizeMode   = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = BrandPalette.SurfaceSecondary,
                    ForeColor          = BrandPalette.NeutralTextMuted,
                    Font               = BrandPalette.BrandFontBold(8.5f),
                    Padding            = new Padding(8, 4, 8, 4),
                    SelectionBackColor = BrandPalette.SurfaceSecondary,
                    SelectionForeColor = BrandPalette.NeutralTextMuted,
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = BrandPalette.SurfacePrimary,
                    ForeColor          = BrandPalette.NeutralText,
                    Padding            = new Padding(8, 6, 8, 6),
                    SelectionBackColor = BrandPalette.Tint90,
                    SelectionForeColor = BrandPalette.NeutralText,
                },
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Promo Campaign", FillWeight = 40, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText       = "Redeemed",
                FillWeight       = 20,
                SortMode         = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText       = "Discount Given",
                FillWeight       = 20,
                SortMode         = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    ForeColor = BrandPalette.StatusNegativeFill
                }
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText       = "Revenue Driven",
                FillWeight       = 20,
                SortMode         = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font      = BrandPalette.BrandFontBold(9.5f),
                    ForeColor = BrandPalette.Brand,
                }
            });

            if (data.Campaigns.Count == 0)
            {
                grid.Rows.Add("No campaign redemptions recorded in this period.", "0", "₱0.00", "₱0.00");
            }
            else
            {
                foreach (var c in data.Campaigns)
                {
                    grid.Rows.Add(
                        c.PromoName,
                        $"{c.TotalRedemptions} times",
                        $"₱{c.TotalDiscountCost:N2}",
                        $"₱{c.RevenueGenerated:N2}"
                    );
                }
            }

            gridPanel.Controls.Add(grid);

            Controls.Add(gridPanel);
            Controls.Add(btnRow);
            Controls.Add(headerPanel);

            CancelButton = closeBtn;
        }
    }
}