using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Modal for Total Revenue KPI — breakdown and order metrics.
    /// </summary>
    public class RevenueDetailModal : Form
    {
        public RevenueDetailModal(PosDiagnosticsDto data)
        {
            Text            = "Revenue Details";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ClientSize      = new Size(560, 520);
            BackColor       = BrandPalette.SurfacePrimary;
            Font            = BrandPalette.BrandFontRegular(9.5f);
            Padding         = new Padding(22);

            BuildLayout(data);
        }

        private void BuildLayout(PosDiagnosticsDto data)
        {
            var aov = data.TotalTransactions > 0 ? data.NetSales / data.TotalTransactions : 0m;
            var discountTotal = data.TotalCampaignDiscounts + data.TotalPwdSeniorDiscounts;
            var discountPct = data.GrossSales > 0 ? discountTotal / data.GrossSales * 100m : 0m;

            // ── Top Header Panel ──
            var headerPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 112,
                BackColor = BrandPalette.SurfacePrimary,
            };

            var titleLabel = new Label
            {
                Text      = "Revenue Breakdown",
                Location  = new Point(0, 0),
                Size      = new Size(516, 26),
                Font      = BrandPalette.BrandFontBold(13.5f),
                ForeColor = BrandPalette.NeutralText,
            };

            var subtitleLabel = new Label
            {
                Text      = $"Net ₱{data.NetSales:N0} from {data.TotalTransactions:N0} orders · AOV ₱{aov:N2} · Discounts {discountPct:0.0}% of gross",
                Location  = new Point(0, 26),
                Size      = new Size(516, 20),
                Font      = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            var divider = new Panel
            {
                Location  = new Point(0, 48),
                Size      = new Size(516, 1),
                BackColor = BrandPalette.Border,
            };

            var chips = new FlowLayoutPanel
            {
                Location      = new Point(0, 56),
                Size          = new Size(516, 48),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = BrandPalette.SurfacePrimary,
            };
            chips.Controls.Add(MakeChip("Gross Sales", $"₱{data.GrossSales:N0}"));
            chips.Controls.Add(MakeChip("Campaign Disc.", $"−₱{data.TotalCampaignDiscounts:N0}", isNegative: true));
            chips.Controls.Add(MakeChip("PWD/Senior", $"−₱{data.TotalPwdSeniorDiscounts:N0}", isNegative: true));
            chips.Controls.Add(MakeChip("Net Revenue", $"₱{data.NetSales:N0}", isHighlight: true));

            headerPanel.Controls.Add(chips);
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
            var closeBtn = MakeCloseButton();
            closeBtn.Click += (s, e) => Close();
            btnRow.Controls.Add(closeBtn);

            // ── Center Detail Grid ──
            var gridPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(0, 10, 0, 10),
                BackColor = BrandPalette.SurfacePrimary,
            };

            var grid = MakeGrid();
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Line Item", FillWeight = 58, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(MakeAmountColumn("Amount", 42));

            void Add(string label, decimal amount, bool neg = false, bool bold = false)
            {
                var i = grid.Rows.Add(label, $"₱{amount:N2}");
                if (neg) grid.Rows[i].DefaultCellStyle.ForeColor = BrandPalette.StatusNegativeFill;
                if (bold)
                {
                    grid.Rows[i].DefaultCellStyle.Font = BrandPalette.BrandFontBold(10f);
                    grid.Rows[i].DefaultCellStyle.BackColor = BrandPalette.SurfaceSecondary;
                    grid.Rows[i].DefaultCellStyle.ForeColor = BrandPalette.Brand;
                }
            }

            Add("Gross Sales", data.GrossSales);
            Add("Campaign Promo Discounts", data.TotalCampaignDiscounts, neg: true);
            Add("PWD / Senior Discounts", data.TotalPwdSeniorDiscounts, neg: true);
            Add("Net Revenue", data.NetSales, bold: true);
            grid.Rows.Add("", "");
            grid.Rows.Add("Completed Orders", $"{data.TotalTransactions:N0}");
            grid.Rows.Add("Campaign-tagged Orders", $"{data.CampaignTransactionCount:N0}");
            grid.Rows.Add("PWD/Senior Orders", $"{data.PwdSeniorTransactionCount:N0}");
            grid.Rows.Add("Average Sales per Order", $"₱{aov:N2}");

            gridPanel.Controls.Add(grid);

            // Compose cleanly without dock collision
            Controls.Add(gridPanel);
            Controls.Add(btnRow);
            Controls.Add(headerPanel);

            CancelButton = closeBtn;
        }

        private static Panel MakeChip(string caption, string value, bool isNegative = false, bool isHighlight = false)
        {
            var p = new Panel
            {
                Width     = 122,
                Height    = 42,
                Margin    = new Padding(0, 0, 8, 0),
                BackColor = isHighlight ? BrandPalette.Tint90 : BrandPalette.SurfaceSecondary,
            };

            p.Controls.Add(new Label
            {
                Text      = caption,
                Location  = new Point(8, 4),
                Size      = new Size(p.Width - 16, 14),
                Font      = BrandPalette.BrandFontRegular(7.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleLeft,
            });

            p.Controls.Add(new Label
            {
                Text      = value,
                Location  = new Point(8, 18),
                Size      = new Size(p.Width - 16, 18),
                Font      = BrandPalette.BrandFontBold(9.5f),
                ForeColor = isNegative ? BrandPalette.StatusNegativeFill : (isHighlight ? BrandPalette.Brand : BrandPalette.NeutralText),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleLeft,
            });
            return p;
        }

        private static Button MakeCloseButton() => new()
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

        private static DataGridView MakeGrid() => new()
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

        private static DataGridViewTextBoxColumn MakeAmountColumn(string header, float weight) => new()
        {
            HeaderText       = header,
            FillWeight       = weight,
            SortMode         = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment          = DataGridViewContentAlignment.MiddleRight,
                Font               = BrandPalette.BrandFontBold(9.5f),
                ForeColor          = BrandPalette.Brand,
                SelectionBackColor = BrandPalette.Tint90,
                SelectionForeColor = BrandPalette.Brand,
            }
        };
    }
}