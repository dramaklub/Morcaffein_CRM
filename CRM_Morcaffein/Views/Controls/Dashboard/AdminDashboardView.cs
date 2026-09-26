using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.Session;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Branch Admin's Bento Grid Dashboard View — the "business as a whole" surface.
    ///
    /// It answers strategic questions, not operational ones:
    ///   - How much did we make?               → Total Revenue pod
    ///   - How much is each member worth?      → Member LTV pod (+ at-risk sub-line)
    ///   - Is anything waiting on me?          → Pending Approvals pod (click → Approvals tab)
    ///   - When are we busy?                   → Peak Hours heatmap
    ///   - Are we too dependent on discounts?  → Revenue vs Discounts chart
    ///   - Are members coming back?            → New vs Returning chart
    ///   - Is governance healthy?              → Audit Log Summary strip
    ///
    /// Cards that live on the Manager's operational dashboard and are deliberately
    /// NOT duplicated here: Age Demographics, Loyalty Tier Progression, Campaign
    /// Lift, and the Customer Rating pod. Top Products is a planned future Manager
    /// card, not Admin.
    /// </summary>
    public class AdminDashboardView : UserControl
    {
        /// <summary>
        /// Raised when the Admin clicks a card that should navigate to another tab
        /// (currently: the Pending Approvals pod, which should open the Approvals tab).
        /// FrmMain subscribes to this and performs the actual navigation.
        /// </summary>
        public event EventHandler? NavigateToApprovalsRequested;

        /// <summary>
        /// Raised when the Basic-tier "Employees" pod is clicked. FrmMain
        /// subscribes and navigates to the Manage Users tab.
        /// </summary>
        public event EventHandler? NavigateToManageUsersRequested;


        private readonly DashboardController _controller;

        private DashboardDateRangeFilterControl _dateFilter = null!;
        private Button _exportBtn = null!;

        // KPI Pods (2 or 3 depending on tier)
        private KpiSummaryPodControl _revenuePod = null!;
        private KpiSummaryPodControl _ltvPod = null!;
        private KpiSummaryPodControl _pendingApprovalsPod = null!;
        private KpiSummaryPodControl _employeesPod = null!;

        // Charts
        private VisitFrequencyControl _visitFreqCard = null!; // Row 2 (full width)
        private RevenueVsDiscountsControl _revenueVsDiscountsCard = null!;
        private NewVsReturningControl _newVsReturningCard = null!;
        private AuditLogSummaryControl _auditSummaryCard = null!;

        // Basic-tier-only cards (shown when the tenant has no BusinessIntelligence)
        private RecentEmployeesControl _recentEmployeesCard = null!;
        private RecentFeedbackControl _recentFeedbackCard = null!;

        // Cached DTOs for pod drill-down modals
        private PosDiagnosticsDto? _latestPos;
        private CustomerLtvChurnDto? _latestLtv;

        private Panel _contentHost = null!;
        private FlowLayoutPanel _bentoContainer = null!;

        public AdminDashboardView()
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
            Dock = DockStyle.Fill;
            BackColor = BrandPalette.SurfaceSecondary;

            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BrandPalette.SurfaceSecondary,
                Padding = new Padding(16, 12, 16, 16),
            };

            // FlowLayoutPanel(TopDown) — same structural pattern as Manager's,
            // no TableLayoutPanel row-style ambiguity. Rows added in visual order.
            _bentoContainer = new FlowLayoutPanel
            {
                Location = new Point(16, 12),
                Width = 900,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
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

            // ---- KPI Pods (2 or 3 columns depending on tier) ----
            // Standard/Full: Revenue, LTV, Pending Approvals.
            // Basic:         Revenue, Employees (navigates to Manage Users).
            //
            // LTV and Pending Approvals are both BI-adjacent — LTV is a
            // strategic metric, and there are no campaigns to approve
            // without the Actions module. Basic tenants get Employees as
            // a second pod, which shows headcount and links to Manage Users.
            var hasBIForPods = EmployeeSession.Current?.HasModule("BusinessIntelligence") == true;

            var podCount = hasBIForPods ? 3 : 2;
            var kpiRow = MakeBentoRow(podCount, 96, bottomGap: 18);

            _revenuePod = new KpiSummaryPodControl("Total Revenue", "₱0")
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
            };
            _revenuePod.CardClicked += (s, e) => ShowRevenueModal();
            kpiRow.Controls.Add(_revenuePod, 0, 0);

            if (hasBIForPods)
            {
                _ltvPod = new KpiSummaryPodControl("Member Lifetime Value", "₱0")
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                };
                _ltvPod.CardClicked += (s, e) => ShowLtvModal();
                kpiRow.Controls.Add(_ltvPod, 1, 0);

                _pendingApprovalsPod = new KpiSummaryPodControl("Pending Approvals", "0")
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                };
                _pendingApprovalsPod.CardClicked += (s, e)
                    => NavigateToApprovalsRequested?.Invoke(this, EventArgs.Empty);
                kpiRow.Controls.Add(_pendingApprovalsPod, 2, 0);
            }
            else
            {
                _employeesPod = new KpiSummaryPodControl("Employees", "0")
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                };
                _employeesPod.CardClicked += (s, e)
                    => NavigateToManageUsersRequested?.Invoke(this, EventArgs.Empty);
                kpiRow.Controls.Add(_employeesPod, 1, 0);
            }

            // ---- Row 1 (full width): Audit Log Summary ----
            // Moved up from the bottom — governance visibility shouldn't require
            // scrolling past the strategic charts.
            var auditRow = MakeBentoRow(1, 160, bottomGap: 14);
            _auditSummaryCard = new AuditLogSummaryControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                MinimumSize = new Size(100, 100),
            };
            auditRow.Controls.Add(_auditSummaryCard, 0, 0);

            // ---- Row 2 (2-up): Revenue vs Discounts + New vs Returning ----
            var row2 = MakeBentoRow(2, 300, bottomGap: 14);
            custRowStyles(row2, 50f, 50f);

            _revenueVsDiscountsCard = new RevenueVsDiscountsControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                MinimumSize = new Size(100, 100),
            };
            _newVsReturningCard = new NewVsReturningControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                MinimumSize = new Size(100, 100),
            };
            row2.Controls.Add(_revenueVsDiscountsCard, 0, 0);
            row2.Controls.Add(_newVsReturningCard, 1, 0);

            // ---- Row 3 (full width): Customer Visit Frequency ----
            var visitRow = MakeBentoRow(1, 260, bottomGap: 4);
            _visitFreqCard = new VisitFrequencyControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                MinimumSize = new Size(100, 100),
            };
            visitRow.Controls.Add(_visitFreqCard, 0, 0);

            // Assemble — FlowLayoutPanel(TopDown) stacks in exactly this order.
            // BusinessIntelligence gates the analytical rows (Revenue vs
            // Discounts, New vs Returning, Visit Frequency). Basic tenants
            // get the filter bar + KPI pods + Audit strip only.
            // ---- Basic-tier-only row: Recent Employees + Recent Feedback ----
            // Shown when the tenant has no BusinessIntelligence module. Fills
            // the visual gap where the BI charts would be, and matches what
            // a Basic Admin's job actually is: manage people, handle feedback.
            var basicRow = MakeBentoRow(2, 280, bottomGap: 14);
            custRowStyles(basicRow, 50f, 50f);

            _recentEmployeesCard = new RecentEmployeesControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                MinimumSize = new Size(100, 100),
            };
            _recentEmployeesCard.RowClicked += (s, employeeId)
                => NavigateToManageUsersRequested?.Invoke(this, EventArgs.Empty);

            _recentFeedbackCard = new RecentFeedbackControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                MinimumSize = new Size(100, 100),
            };
            // Recent Feedback is display-only on the Admin dashboard — the
            // Feedback queue is a Manager work surface, not an Admin one.
            // The card gives oversight; the Manager operates the queue.

            basicRow.Controls.Add(_recentEmployeesCard, 0, 0);
            basicRow.Controls.Add(_recentFeedbackCard, 1, 0);

            var session = EmployeeSession.Current;

            _bentoContainer.Controls.Add(filterRow);
            _bentoContainer.Controls.Add(kpiRow);

            if (session?.HasModule("BusinessIntelligence") == true)
            {
                // Full/Standard: BI rows.
                _bentoContainer.Controls.Add(auditRow);
                _bentoContainer.Controls.Add(row2);
                _bentoContainer.Controls.Add(visitRow);
            }
            else
            {
                // Basic: the two-list row instead.
                _bentoContainer.Controls.Add(basicRow);
            }

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
            var row = new TableLayoutPanel
            {
                Height = height,
                ColumnCount = columns,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, bottomGap),
                BackColor = Color.Transparent,
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
                    DateRangePreset.Today => 1,
                    DateRangePreset.SevenDays => 7,
                    _ => 30,
                };

                var branchId = EmployeeSession.Current?.CurrentBranchId;
                var hasBI = EmployeeSession.Current?.HasModule("BusinessIntelligence") == true;

                // ---- KPI pods: always shown for Admin ----

                _latestPos = await _controller.GetPosDiagnosticsAsync(days, branchId);
                _revenuePod.UpdateData($"₱{_latestPos.NetSales:N0}");

                if (hasBI)
                {
                    _latestLtv = await _controller.GetCustomerLtvChurnAsync(days);
                    _ltvPod?.UpdateData($"₱{_latestLtv.OverallAverageLtv:N0}");
                }

                var audit = await _controller.GetAuditSummaryAsync(days);
                _pendingApprovalsPod?.UpdateData($"{audit.PendingCampaignApprovals:N0}");
                _employeesPod?.UpdateData($"{audit.ActiveEmployeeCount:N0}");
                _auditSummaryCard.BindData(audit);

                // Basic-tier cards — only present when the tenant has no BI.
                if (!hasBI)
                {
                    _recentEmployeesCard.BindData(await _controller.GetRecentEmployeesAsync(5));
                    _recentFeedbackCard.BindData(await _controller.GetRecentFeedbackAsync(5, branchId));
                }

                // ---- BI-gated charts ----
                if (hasBI)
                {
                    var discountDays = days switch
                    {
                        1 => 30,
                        7 => 30,
                        _ => 90,
                    };
                    _revenueVsDiscountsCard.BindData(await _controller.GetRevenueVsDiscountsAsync(discountDays));
                    _newVsReturningCard.BindData(await _controller.GetNewVsReturningAsync(6));
                    _visitFreqCard.BindData(await _controller.GetVisitFrequencyAsync());
                }
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

        private void ShowLtvModal()
        {
            if (_latestLtv == null) return;
            using var modal = new CustomerLtvDetailModal(_latestLtv);
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

            // Step 1 — ask which report.
            using var picker = new ExportReportTypeModal(
                "Business summary: revenue vs discounts, member LTV, retention, audit log.");
            if (picker.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            var isLedger = picker.SelectedReport == ReportKind.TransactionLedger;

            // Step 2 — SaveFileDialog, filename varies by report kind.
            var defaultName = isLedger
                ? $"Morcaffein-Ledger-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf"
                : $"Morcaffein-Business-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf";

            using var dlg = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = defaultName,
                Title = "Save report",
                OverwritePrompt = true,
            };

            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            // Step 3 — generate the chosen report.
            try
            {
                _exportBtn.Enabled = false;
                _exportBtn.Text = "Generating…";

                var report = new ReportController();
                var bytes = isLedger
                    ? await report.GenerateTransactionLedgerAsync(days)
                    : await report.GenerateAdminReportAsync(days);

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