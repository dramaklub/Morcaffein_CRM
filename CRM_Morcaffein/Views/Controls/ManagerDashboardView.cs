using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.Theme;


namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Store Manager's Bento Grid Dashboard View.
    /// Layout: KPI pods, date filter, heatmap (full width), Age Demographics +
    /// Loyalty Tier side by side, then Campaign Lift (full width). Barista Sales
    /// and POS Diagnostics chart cards were deliberately dropped from this view —
    /// their underlying data (PosDiagnosticsDto) is still fetched where the KPI
    /// pods and drill-down modals need it, just not rendered as standalone cards.
    /// Campaign Lift stays — it's one of the core shared charts, not a dropped one.
    /// </summary>
    public class ManagerDashboardView : UserControl
    {
        private readonly DashboardController _controller;

        private DashboardDateRangeFilterControl _dateFilter = null!;
        private Button _exportBtn = null!;

        // KPI Pods (Top Strip)
        private KpiSummaryPodControl _revenuePod = null!;
        private KpiSummaryPodControl _ordersPod = null!;
        private KpiSummaryPodControl _promosPod = null!;

        // Charts
        private PeakHoursHeatmapControl       _peakHoursCard    = null!; // Row 1 (full width)
        private AgeDemographicsControl _demographicsCard = null!; // Row 2 Left
        private TopProductsControl _topProductsCard = null!; // Row 2 Right
        private ActiveCampaignLiftControl _campaignLiftCard = null!; // Row 3 Left
        private FeedbackRatingDistributionControl _csatCard = null!; // Row 3 Right

        // Cached DTOs for KPI modals
        private PosDiagnosticsDto?             _latestPos;
        private List<RecentOrderSummaryDto>    _latestRecentOrders = new();
        private CampaignRoiDto?                _latestCampaignRoi;
        private FeedbackRatingDistributionDto? _latestCsat;
        private decimal                        _latestAov;

        private Panel           _contentHost    = null!;
        private FlowLayoutPanel _bentoContainer = null!;

        public ManagerDashboardView()
        {
            _controller = new DashboardController();
            MinimumSize = new Size(600, 400);

            SuspendLayout();
            BuildBentoLayout();
            ResumeLayout(false);

            Load += async (s, e) => { SyncWidth(); await ReloadAsync(); };
        }

        private void BuildBentoLayout()
        {
            Dock      = DockStyle.Fill;
            BackColor = BrandPalette.SurfaceSecondary;

            // No in-view header — FrmMain's top bar already shows "/ Dashboard" as
            // the active tab label, so a second "Store Manager Dashboard" title
            // here was pure redundancy.

            // ---- Content Host ----
            _contentHost = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = BrandPalette.SurfaceSecondary,
                Padding    = new Padding(16, 12, 16, 16),
            };

            // A plain top-down FlowLayoutPanel, not a TableLayoutPanel — it stacks
            // children in the exact order they're added, with no row-styling
            // ambiguity. (The previous TableLayoutPanel version declared 6 rows but
            // never added RowStyles for them, which combined with AutoSize and every
            // "row" being its own Dock=Top panel is exactly the kind of setup that
            // makes TableLayoutPanel misjudge a row's position — that's what was
            // pushing the date filter bar above the KPI row instead of below it.)
            _bentoContainer = new FlowLayoutPanel
            {
                Location      = new Point(16, 12),
                Width         = 900,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = Color.Transparent,
            };

            // ---- Date Filter Bar (above the KPI pods) ----
            var filterRow = new Panel
            {
                Height = 38,
                Margin = new Padding(0, 0, 0, 14),
                BackColor = Color.Transparent,
                Padding = new Padding(3, 2, 3, 2),
            };

            _exportBtn = new Button
            {
                Text = "Export PDF",
                Width = 110,
                Height = 30,
                Dock = DockStyle.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 2),
            };
            _exportBtn.FlatAppearance.BorderSize = 0;
            _exportBtn.Click += ExportBtn_Click;
            ButtonStyler.TrackEnabledState(_exportBtn);

            _dateFilter = new DashboardDateRangeFilterControl
            {
                Dock = DockStyle.Right,
            };
            _dateFilter.RangeChanged += async (s, e) => await ReloadAsync();

            filterRow.Controls.Add(_dateFilter);
            filterRow.Controls.Add(_exportBtn);

            // ---- KPI Pods (4 Equal Columns) ---- taller + more room below so the
            // pods aren't crowded by the chart row right under them.
            var kpiRow = MakeBentoRow(3, 96, bottomGap: 18);
            _revenuePod = new KpiSummaryPodControl("Total Revenue", "₱0") { Dock = DockStyle.Fill, Margin = new Padding(3) };
            _ordersPod = new KpiSummaryPodControl("Avg. per Sale", "₱0") { Dock = DockStyle.Fill, Margin = new Padding(3) };
            _promosPod = new KpiSummaryPodControl("Promos Used", "0") { Dock = DockStyle.Fill, Margin = new Padding(3) };

            _revenuePod.CardClicked += (s, e) => ShowRevenueModal();
            _ordersPod.CardClicked += (s, e) => ShowSalesPerOrderModal();
            _promosPod.CardClicked += (s, e) => ShowPromosModal();

            kpiRow.Controls.Add(_revenuePod, 0, 0);
            kpiRow.Controls.Add(_ordersPod, 1, 0);
            kpiRow.Controls.Add(_promosPod, 2, 0);

            // ---- Row 1 (1 chart): Peak Hours Transaction Velocity (Full Width) ----
            // Taller than before so the heatmap actually fills the card instead of
            // leaving dead space above/below it.
            var peakRow = MakeBentoRow(1, 280, bottomGap: 14);
            _peakHoursCard = new PeakHoursHeatmapControl
            {
                Dock        = DockStyle.Fill,
                Margin      = new Padding(3),
                MinimumSize = new Size(100, 100)
            };
            peakRow.Controls.Add(_peakHoursCard, 0, 0);

            // ---- Row 2 (2 charts): Age Demographics (50%) + Loyalty Tier (50%) ----
            var row2 = MakeBentoRow(2, 300, bottomGap: 14);
            custRowStyles(row2, 50f, 50f);
            _demographicsCard = new AgeDemographicsControl { Dock = DockStyle.Fill, Margin = new Padding(3), MinimumSize = new Size(100, 100) };
            _topProductsCard = new TopProductsControl { Dock = DockStyle.Fill, Margin = new Padding(3), MinimumSize = new Size(100, 100) };
            row2.Controls.Add(_demographicsCard, 0, 0);
            row2.Controls.Add(_topProductsCard, 1, 0);

            // ---- Row 3 (2 charts): Campaign Lift (60%) + Customer Ratings (40%) ----
            var row3 = MakeBentoRow(2, 300, bottomGap: 4);
            custRowStyles(row3, 60f, 40f);
            _campaignLiftCard = new ActiveCampaignLiftControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                MinimumSize = new Size(100, 100)
            };
            _csatCard = new FeedbackRatingDistributionControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                MinimumSize = new Size(100, 100)
            };
            row3.Controls.Add(_campaignLiftCard, 0, 0);
            row3.Controls.Add(_csatCard, 1, 0);

            // Assemble — FlowLayoutPanel (TopDown) stacks these in exactly this
            // order, top to bottom. No cell coordinates needed.
            _bentoContainer.Controls.Add(filterRow); // Above the KPI pods
            _bentoContainer.Controls.Add(kpiRow);
            _bentoContainer.Controls.Add(peakRow);
            _bentoContainer.Controls.Add(row2);
            _bentoContainer.Controls.Add(row3);

            _contentHost.Controls.Add(_bentoContainer);
            _contentHost.Resize += (s, e) => SyncWidth();

            Controls.Add(_contentHost);
        }

        private static void custRowStyles(TableLayoutPanel row, float col1, float col2)
        {
            row.ColumnStyles.Clear();
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, col1));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, col2));
        }

        private static TableLayoutPanel MakeBentoRow(int columns, int height, int bottomGap)
        {
            // No Dock here — these rows are flowed children of _bentoContainer now
            // (a FlowLayoutPanel), and FlowLayoutPanel positions children by flow
            // order using their own Width/Height/Margin, not Dock. Dock on a flowed
            // child fights the flow engine's sizing instead of cooperating with it.
            var row = new TableLayoutPanel
            {
                Height      = height,
                ColumnCount = columns,
                RowCount    = 1,
                Margin      = new Padding(0, 0, 0, bottomGap),
                BackColor   = Color.Transparent,
            };
            for (int i = 0; i < columns; i++)
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
            return row;
        }

        private void SyncWidth()
        {
            if (_contentHost == null || _bentoContainer == null) return;
            var w = Math.Max(700,
                _contentHost.ClientSize.Width
                - _contentHost.Padding.Horizontal
                - SystemInformation.VerticalScrollBarWidth);
            _bentoContainer.Width = w;
            foreach (Control row in _bentoContainer.Controls)
                row.Width = w;
        }

        private async Task ReloadAsync()
        {
            try
            {
                var days = _dateFilter.SelectedPreset switch
                {
                    DateRangePreset.Today      => 1,
                    DateRangePreset.SevenDays  => 7,
                    _                          => 30,
                };

              

                // 1. Static Velocity Heatmap (Not affected by range filter)
                _peakHoursCard.BindData(await _controller.GetPeakHoursAsync());

                // 2. Date-filtered metrics — PosDiagnosticsDto has no standalone chart
                // card in this view anymore, but the KPI pods and Revenue/Sales-per-
                // Order modals still derive their numbers from it.
                _latestPos = await _controller.GetPosDiagnosticsAsync(days);

                _latestAov = _latestPos.TotalTransactions > 0 ? _latestPos.NetSales / _latestPos.TotalTransactions : 0m;
                // KPI values are shown in whole pesos — cents are visual noise at this size,
                // and the exact figure is a click away in the drill-down modal.
                _revenuePod.UpdateData($"₱{_latestPos.NetSales:N0}");
                _ordersPod.UpdateData($"₱{_latestAov:N0}");
                _promosPod.UpdateData($"{_latestPos.CampaignTransactionCount:N0}");

                _latestCsat = await _controller.GetFeedbackRatingsAsync(days);
                _csatCard.BindData(_latestCsat);

                _latestRecentOrders = await _controller.GetRecentOrdersAsync(days, 15);
                // Feeds the Promos Redeemed KPI pod's drill-down modal. The Campaign
                // Lift chart card below is a separate endpoint/DTO (daily revenue vs
                // redemption count), not this one.
                _latestCampaignRoi = await _controller.GetCampaignRoiAsync(days);

                _demographicsCard.BindData(await _controller.GetAgeDemographicsAsync(days));
                _topProductsCard.BindData(await _controller.GetTopProductsAsync(days, 5));
                _campaignLiftCard.BindData(await _controller.GetCampaignLiftAsync(days));
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Dashboard error");
            }
        }

        private void ShowRevenueModal()
        {
            if (_latestPos == null) return;
            using var modal = new RevenueDetailModal(_latestPos);
            modal.ShowDialog(FindForm());
        }

        private void ShowSalesPerOrderModal()
        {
            using var modal = new SalesPerOrderDetailModal(_latestRecentOrders, _latestAov);
            modal.ShowDialog(FindForm());
        }

        private void ShowPromosModal()
        {
            if (_latestCampaignRoi == null) return;
            using var modal = new PromosRedeemedDetailModal(_latestCampaignRoi);
            modal.ShowDialog(FindForm());
        }

        private async void ExportBtn_Click(object? sender, EventArgs e)
        {
            var days = _dateFilter.SelectedPreset switch
            {
                DateRangePreset.Today => 1,
                DateRangePreset.SevenDays => 7,
                _ => 30,
            };

            var defaultName = $"Morcaffein-Operations-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf";

            using var dlg = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = defaultName,
                Title = "Save report",
                OverwritePrompt = true,
            };

            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                _exportBtn.Enabled = false;
                _exportBtn.Text = "Generating…";

                var report = new ReportController();
                var bytes = await report.GenerateManagerReportAsync(days);
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
    }
}