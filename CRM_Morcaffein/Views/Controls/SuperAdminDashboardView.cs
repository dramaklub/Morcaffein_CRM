using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// SuperAdmin dashboard — platform-wide overview. Read-only: three
    /// KPI pods (total companies, active subscriptions, MRR), a small bar
    /// chart of companies-per-plan, and a list of the most recent
    /// subscription assignments.
    ///
    /// Data comes from one combined endpoint (GET /api/superadmin/dashboard).
    /// No pagination, no filters, no drill-down — this is a glance, not a
    /// work surface.
    /// </summary>
    public class SuperAdminDashboardView : UserControl
    {
        private readonly SuperAdminApi _api;

        // Header actions
        private Button _exportBtn = null!;

        // KPIs
        private Label _companiesValueLabel = null!;
        private Label _subscriptionsValueLabel = null!;
        private Label _mrrValueLabel = null!;

        // Chart
        private Panel _chartHost = null!;

        // Recent subscriptions
        private DataGridView _recentGrid = null!;
        private Label _recentEmptyLabel = null!;

        // Loading state
        private Panel _loadingOverlay = null!;

        public SuperAdminDashboardView()
        {
            _api = new SuperAdminApi();
            BuildLayout();
            Load += async (s, e) => await ReloadAsync();
        }

        // =====================================================================
        // LAYOUT
        // =====================================================================

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;
            BackColor = BrandPalette.SurfaceSecondary;

            // Scroll host so the whole dashboard can scroll on a small window.
            var scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BrandPalette.SurfaceSecondary,
                Padding = new Padding(24),
            };

            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
            };

            // ---- Title row (title + export button) ----
            var headerRow = new Panel
            {
                Width = 900,
                Height = 62,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
            };
            headerRow.Resize += (s, e) => SyncFlowChildWidth(stack, headerRow);

            var titleLabel = new Label
            {
                Text = "Platform Overview",
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(16f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                Location = new Point(0, 0),
            };
            headerRow.Controls.Add(titleLabel);

            var subtitleLabel = new Label
            {
                Text = "Snapshot of all tenants, plans, and revenue.",
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Location = new Point(0, 28),
            };
            headerRow.Controls.Add(subtitleLabel);

            _exportBtn = new Button
            {
                Text = "Export PDF",
                Size = new Size(120, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _exportBtn.FlatAppearance.BorderSize = 0;
            _exportBtn.Click += ExportBtn_Click;
            headerRow.Controls.Add(_exportBtn);

            // Position the export button on the right edge of the header.
            void PositionExportButton()
            {
                if (_exportBtn is null || headerRow is null) return;
                _exportBtn.Location = new Point(
                    Math.Max(0, headerRow.ClientSize.Width - _exportBtn.Width),
                    4);
            }
            headerRow.Resize += (s, e) => PositionExportButton();
            PositionExportButton();

            stack.Controls.Add(headerRow);

            // ---- KPI strip ----
            var kpiRow = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 1,
                Width = 900,
                Height = 110,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 20),
            };
            kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            _companiesValueLabel = AddKpiPod(kpiRow, 0, "Total Companies", "—");
            _subscriptionsValueLabel = AddKpiPod(kpiRow, 1, "Active Subscriptions", "—");
            _mrrValueLabel = AddKpiPod(kpiRow, 2, "Monthly Recurring Revenue", "—");

            kpiRow.Resize += (s, e) => SyncFlowChildWidth(stack, kpiRow);
            stack.Controls.Add(kpiRow);

            // ---- Companies per plan ----
            var chartSection = new Panel
            {
                Width = 900,
                Height = 260,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 20),
            };
            chartSection.Paint += ChartSection_Paint;
            _chartHost = chartSection;
            chartSection.Resize += (s, e) => SyncFlowChildWidth(stack, chartSection);
            stack.Controls.Add(chartSection);

            // ---- Recent subscriptions ----
            var recentHeaderLabel = new Label
            {
                Text = "Recent Subscriptions",
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
            };
            stack.Controls.Add(recentHeaderLabel);

            var recentCard = new Panel
            {
                Width = 900,
                Height = 320,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 20),
            };
            recentCard.Resize += (s, e) => SyncFlowChildWidth(stack, recentCard);

            _recentGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
            };
            ConfigureGrid(_recentGrid);
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CompanyName",
                HeaderText = "Company",
                FillWeight = 30,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = BrandPalette.BrandFontBold(9.5f),
                    ForeColor = BrandPalette.Brand,
                },
            });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PlanName",
                HeaderText = "Plan",
                FillWeight = 20,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PlanMonthlyPrice",
                HeaderText = "Monthly",
                FillWeight = 15,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "₱#,##0",
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                },
            });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StartedAt",
                HeaderText = "Started",
                FillWeight = 20,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "yyyy-MM-dd HH:mm",
                },
            });
            _recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TermsAcceptedBy",
                HeaderText = "Agreed by",
                FillWeight = 15,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });

            _recentEmptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = BrandPalette.SurfacePrimary,
                Text = "No subscription activity yet.",
                Visible = false,
            };

            recentCard.Controls.Add(_recentGrid);
            recentCard.Controls.Add(_recentEmptyLabel);

            stack.Controls.Add(recentCard);

            // Compose
            scrollHost.Controls.Add(stack);
            Controls.Add(scrollHost);

            // Loading overlay — covers the whole view until the first fetch returns.
            _loadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
            };
            var loadingLabel = new Label
            {
                Text = "Loading dashboard…",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _loadingOverlay.Controls.Add(loadingLabel);
            Controls.Add(_loadingOverlay);
            _loadingOverlay.BringToFront();

            // Keep the flow children at a sane fixed width relative to the host.
            scrollHost.Resize += (s, e) =>
            {
                var w = Math.Max(720, scrollHost.ClientSize.Width - 48);
                stack.Width = w;
                foreach (Control c in stack.Controls)
                {
                    if (c is TableLayoutPanel || (c is Panel p && p.Height > 150))
                        c.Width = w;
                }
            };
        }

        /// <summary>
        /// Adds a KPI pod (title + big value) to the given cell of the row.
        /// Returns the value label so the caller can update it after loading.
        /// </summary>
        private static Label AddKpiPod(TableLayoutPanel row, int column, string title, string initialValue)
        {
            var pod = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3),
            };

            var titleLabel = new Label
            {
                Text = title,
                Location = new Point(16, 16),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            var valueLabel = new Label
            {
                Text = initialValue,
                Location = new Point(16, 44),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(20f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
            };

            pod.Controls.Add(valueLabel);
            pod.Controls.Add(titleLabel);
            row.Controls.Add(pod, column, 0);
            return valueLabel;
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
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = BrandPalette.Border;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
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
            grid.RowTemplate.Height = 32;
            grid.ScrollBars = ScrollBars.Vertical;
        }

        /// <summary>
        /// Keeps a flow child's width in sync with the flow panel. WinForms
        /// FlowLayoutPanel with WrapContents=false doesn't auto-size children,
        /// so this has to be done manually on each Resize of the flow.
        /// </summary>
        private static void SyncFlowChildWidth(FlowLayoutPanel flow, Control child)
        {
            if (flow.ClientSize.Width <= 0) return;
            child.Width = Math.Max(300, flow.ClientSize.Width - 4);
        }

        // =====================================================================
        // EXPORT
        // =====================================================================

        private async void ExportBtn_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"Morcaffein-Platform-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf",
                Title = "Save platform report",
                OverwritePrompt = true,
            };

            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                _exportBtn.Enabled = false;
                _exportBtn.Text = "Generating…";

                var report = new Services.ReportService();
                var bytes = await report.GenerateSuperAdminReportAsync();

                await File.WriteAllBytesAsync(dlg.FileName, bytes);

                Toast.Show(this, $"Report saved to {Path.GetFileName(dlg.FileName)}.", ToastKind.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Export failed");
            }
            finally
            {
                _exportBtn.Enabled = true;
                _exportBtn.Text = "Export PDF";
            }
        }

        // =====================================================================
        // LOAD
        // =====================================================================

        // =====================================================================
        // LOAD
        // =====================================================================

        private async Task ReloadAsync()
        {
            SuperAdminDashboardDto data;
            try
            {
                data = await _api.GetDashboardAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Error loading dashboard");
                _loadingOverlay.Visible = false;
                return;
            }

            // ---- KPIs ----
            _companiesValueLabel.Text = data.Summary.TotalCompanies.ToString("N0");
            _subscriptionsValueLabel.Text = data.Summary.ActiveSubscriptions.ToString("N0");
            _mrrValueLabel.Text = $"₱{data.Summary.MonthlyRecurringRevenue:N0}";

            // ---- Chart ----
            _chartBars = data.CompaniesPerPlan;
            _chartHost.Invalidate();

            // ---- Recent list ----
            if (data.RecentSubscriptions.Count == 0)
            {
                _recentGrid.Visible = false;
                _recentEmptyLabel.Visible = true;
            }
            else
            {
                _recentGrid.Visible = true;
                _recentEmptyLabel.Visible = false;

                _recentGrid.Rows.Clear();
                foreach (var s in data.RecentSubscriptions)
                {
                    _recentGrid.Rows.Add(
                        s.CompanyName,
                        s.PlanName,
                        s.PlanMonthlyPrice,
                        s.StartedAt.ToLocalTime(),
                        s.TermsAcceptedBy ?? "—");
                }
                _recentGrid.ClearSelection();
            }

            _loadingOverlay.Visible = false;
        }

        // =====================================================================
        // CHART PAINTING
        // =====================================================================

        private List<CompaniesPerPlanPoint> _chartBars = new();

        /// <summary>
        /// Renders the companies-per-plan bar chart directly on the panel. No
        /// WinForms Chart control — a handful of bars doesn't need one, and
        /// self-painting keeps the visual language consistent with the tenant
        /// dashboard cards.
        ///
        /// Bars are ordered as the API returns them: Basic, Standard, Full,
        /// then "No Plan" (which has PlanId = null). Colors:
        ///   Basic    → mid-tone
        ///   Standard → mid-tone darker
        ///   Full     → brand
        ///   No Plan  → muted grey (call-to-action: these tenants need a plan)
        /// </summary>
        private void ChartSection_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var panel = (Panel)sender;
            var w = panel.ClientSize.Width;
            var h = panel.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            // Title
            TextRenderer.DrawText(g, "Companies per Plan",
                BrandPalette.BrandFontBold(11f),
                new Point(20, 16),
                BrandPalette.NeutralText);

            // Chart area (leaves room for title at top and axis labels at bottom)
            const int topPad = 56;
            const int bottomPad = 44;
            const int sidePad = 40;

            var chartRect = new Rectangle(
                sidePad, topPad,
                w - sidePad * 2,
                h - topPad - bottomPad);

            if (chartRect.Height <= 0 || chartRect.Width <= 0) return;

            if (_chartBars.Count == 0)
            {
                TextRenderer.DrawText(g, "Loading…",
                    BrandPalette.BrandFontRegular(9.5f),
                    new Point(sidePad, chartRect.Y + chartRect.Height / 2),
                    BrandPalette.NeutralTextMuted);
                return;
            }

            var maxCount = Math.Max(1, _chartBars.Max(b => b.CompanyCount));

            // Baseline
            using (var axisPen = new Pen(BrandPalette.Border, 1))
            {
                g.DrawLine(axisPen,
                    chartRect.Left, chartRect.Bottom,
                    chartRect.Right, chartRect.Bottom);
            }

            // Bar geometry
            var slotCount = _chartBars.Count;
            var slotWidth = chartRect.Width / (float)slotCount;
            var barWidth = Math.Min(80, slotWidth * 0.55f);

            for (int i = 0; i < slotCount; i++)
            {
                var bar = _chartBars[i];
                var slotCenter = chartRect.Left + slotWidth * (i + 0.5f);
                var barLeft = (int)(slotCenter - barWidth / 2);

                var barHeight = bar.CompanyCount == 0
                    ? 0
                    : (int)((chartRect.Height - 8) * (bar.CompanyCount / (float)maxCount));

                var barRect = new Rectangle(
                    barLeft,
                    chartRect.Bottom - barHeight,
                    (int)barWidth,
                    barHeight);

                var fill = bar.PlanId switch
                {
                    null => Color.FromArgb(220, 180, 180, 180),   // "No Plan" — muted grey
                    1 => BrandPalette.Tint50,                   // Basic
                    2 => BrandPalette.Tone60,                   // Standard
                    _ => BrandPalette.Brand,                    // Full and beyond
                };

                if (barHeight > 0)
                {
                    using var brush = new SolidBrush(fill);
                    g.FillRectangle(brush, barRect);
                }
                else
                {
                    // Zero-value bars get a thin 2px "empty" marker at the baseline
                    // so the axis slot is visibly accounted for.
                    using var emptyBrush = new SolidBrush(BrandPalette.Border);
                    g.FillRectangle(emptyBrush,
                        new Rectangle(barLeft, chartRect.Bottom - 2, (int)barWidth, 2));
                }

                // Count above the bar (or above the baseline for zero)
                var countText = bar.CompanyCount.ToString("N0");
                var countY = bar.CompanyCount == 0
                    ? chartRect.Bottom - 24
                    : barRect.Top - 20;

                var countFont = BrandPalette.BrandFontBold(11f);
                var countSize = g.MeasureString(countText, countFont);
                g.DrawString(countText, countFont,
                    new SolidBrush(BrandPalette.NeutralText),
                    slotCenter - countSize.Width / 2,
                    countY);

                // Plan label below the axis
                var label = bar.PlanName;
                var labelFont = BrandPalette.BrandFontRegular(9.5f);
                var labelSize = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont,
                    new SolidBrush(bar.PlanId is null ? BrandPalette.NeutralTextMuted : BrandPalette.NeutralText),
                    slotCenter - labelSize.Width / 2,
                    chartRect.Bottom + 8);
            }
        }
    }
}