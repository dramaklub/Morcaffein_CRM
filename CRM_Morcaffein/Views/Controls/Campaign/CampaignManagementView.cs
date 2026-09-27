using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// The Manager's Campaign tab. A grid of campaign cards — no drawer, no table.
    /// Clicking a card opens the detail modal (FrmCampaignDetail), which handles editing
    /// and the activate/deactivate actions.
    ///
    /// This view is Manager-only. Baristas never see the tab (FrmMain doesn't include it
    /// for them). Admin also doesn't see it — their Campaign oversight goes through the
    /// Approvals tab, not here.
    /// </summary>
    public class CampaignManagementView : UserControl
    {
        private const int CampaignColumnCount = 4;
        private const int CardWidth = 260;
        private const int CardHeight = 200;
        private const int CardMargin = 8;
        private const int RowHeight = CardHeight + CardMargin * 2;   // 216

        private readonly CampaignService _controller;

        // Header
        private Panel _cardHeaderPanel = null!;
        private TextBox _searchTxtBx = null!;
        private Button _newCampaignBtn = null!;

        // Status filter row — sits below the search box inside the card header.
        // Purely client-side: filters the already-loaded _allCampaigns list.
        private Panel _statusFilterRow = null!;
        private ComboBox _statusFilterCombo = null!;
        private const string StatusFilterAllOption = "All Statuses";
        private static readonly string[] StatusFilterOptions =
            { "Active", "Pending", "Rejected", "Inactive" };

        // Card grid (mirrors TransactionView's drinks area)
        private Panel _cardsScrollHost = null!;
        private TableLayoutPanel _cardsGrid = null!;
        private Label _emptyStateLabel = null!;

        // Debounce for the search box — avoids one API call per keystroke.
        private System.Windows.Forms.Timer _searchDebounce = null!;

        // In-memory list of everything fetched; search filters it client-side.
        private List<CampaignResponseDto> _allCampaigns = new();

        // Filter signature so we don't rebuild the grid when nothing changed.
        private string _lastFilterSignature = string.Empty;

        public CampaignManagementView()
        {
            _controller = new CampaignService();
            BuildLayout();
            Load += async (s, e) => await ReloadAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _searchDebounce?.Dispose();
            base.Dispose(disposing);
        }

        // =====================================================================
        // LAYOUT
        // =====================================================================

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;
            BackColor = BrandPalette.SurfaceSecondary;

            var cardOuterPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
                Padding = new Padding(24),
            };

            var tableCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
            };

            // ---- Header: search + New Campaign ----
            _cardHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _searchTxtBx = new TextBox
            {
                Location = new Point(16, 16),
                Size = new Size(300, 28),
                PlaceholderText = "Search by name or category",
                Font = BrandPalette.BrandFontRegular(10f),
            };

            // Debounce: rebuild the grid only after the user pauses typing.
            _searchDebounce = new System.Windows.Forms.Timer { Interval = 200 };
            _searchDebounce.Tick += (s, e) =>
            {
                _searchDebounce.Stop();
                RefreshCardsPanel();
            };

            _searchTxtBx.TextChanged += (s, e) =>
            {
                _searchDebounce.Stop();
                _searchDebounce.Start();
            };

            _newCampaignBtn = new Button
            {
                Text = "+ New Campaign",
                Size = new Size(170, 32),
                Location = new Point(16, 14),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _newCampaignBtn.FlatAppearance.BorderSize = 0;
            _newCampaignBtn.Click += (s, e) => OpenCreateModal();

            var headerDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BrandPalette.Border,
            };

            // ---- Status filter row ----
            BuildStatusFilterRow();

            _cardHeaderPanel.Controls.Add(_newCampaignBtn);
            _cardHeaderPanel.Controls.Add(_searchTxtBx);
            _cardHeaderPanel.Controls.Add(_statusFilterRow);
            _cardHeaderPanel.Controls.Add(headerDivider);

            // Reposition the button to hug the right edge whenever the header resizes.
            _cardHeaderPanel.Resize += (s, e) =>
            {
                _newCampaignBtn.Left = _cardHeaderPanel.ClientSize.Width - _newCampaignBtn.Width - 16;
                _newCampaignBtn.Top = 14;
            };

            // ---- Card grid ----
            // Same wrapper-Panel-with-AutoScroll + absolutely-positioned TableLayoutPanel
            // pattern as the drinks area in TransactionView. The wrapper scrolls; the grid
            // is Dock=None and its Width/Height are set explicitly.
            _cardsScrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _cardsGrid = new TableLayoutPanel
            {
                Location = new Point(0, 0),
                ColumnCount = CampaignColumnCount,
                RowCount = 0,
                AutoScroll = false,
                Padding = new Padding(16),
                BackColor = BrandPalette.SurfacePrimary,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            };

            for (int i = 0; i < CampaignColumnCount; i++)
                _cardsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / CampaignColumnCount));

            _emptyStateLabel = new Label
            {
                Text = "No campaigns yet. Click \"+ New Campaign\" to create one.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _cardsScrollHost.Controls.Add(_cardsGrid);
            _cardsScrollHost.Resize += (s, e) => SyncCardsGridWidth();

            tableCardPanel.Controls.Add(_cardsScrollHost);
            tableCardPanel.Controls.Add(_cardHeaderPanel);

            cardOuterPanel.Controls.Add(tableCardPanel);
            Controls.Add(cardOuterPanel);
        }

        /// <summary>
        /// Builds the "Filter by Status:" row that sits below the search box in the
        /// card header. Same shape as CustomerManagementView and FeedbackManagementView:
        /// a small muted label, then a dropdown, positions computed from the label's
        /// measured width so they can't collide at any DPI.
        ///
        /// Purely client-side — the dropdown's SelectedIndexChanged re-renders the
        /// already-loaded _allCampaigns list. No API round trip.
        /// </summary>
        private void BuildStatusFilterRow()
        {
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

            _statusFilterCombo.Items.Add(StatusFilterAllOption);
            foreach (var s in StatusFilterOptions)
                _statusFilterCombo.Items.Add(s);

            // Default to "All Statuses" — a Manager wants to see their whole history.
            _statusFilterCombo.SelectedItem = StatusFilterAllOption;

            var labelWidth = TextRenderer.MeasureText(label.Text, label.Font).Width;
            label.Left = 0;
            label.Top = (30 - label.Height) / 2;

            _statusFilterCombo.Left = labelWidth + 12;
            _statusFilterCombo.Top = (30 - _statusFilterCombo.Height) / 2;

            _statusFilterCombo.SelectedIndexChanged += (s, e) => RefreshCardsPanel();

            _statusFilterRow.Controls.Add(_statusFilterCombo);
            _statusFilterRow.Controls.Add(label);
        }

        /// <summary>
        /// The currently-selected status filter, or null when "All Statuses" is picked.
        /// Null means "no status filter" — every campaign is included.
        /// </summary>
        private string? SelectedStatusFilter =>
            _statusFilterCombo?.SelectedItem is string s && s != StatusFilterAllOption
                ? s
                : null;

        private void SyncCardsGridWidth()
        {
            if (_cardsScrollHost is null || _cardsGrid is null)
                return;

            var hostWidth = _cardsScrollHost.ClientSize.Width;
            if (hostWidth <= 0)
                return;

            _cardsGrid.Width = hostWidth;
        }

        // =====================================================================
        // DATA LOADING
        // =====================================================================

        private async Task ReloadAsync()
        {
            try
            {
                _allCampaigns = await _controller.GetAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Error loading campaigns");
                return;
            }

            // Force a rebuild regardless of the filter signature — the underlying data
            // changed, so even the same filter should produce a fresh render.
            _lastFilterSignature = string.Empty;
            RefreshCardsPanel();
        }

        // =====================================================================
        // CARD GRID
        // =====================================================================

        private void RefreshCardsPanel()
        {
            var searchTerm = _searchTxtBx.Text?.Trim() ?? string.Empty;
            var statusFilter = SelectedStatusFilter;
            var signature = $"campaigns|{searchTerm}|{statusFilter ?? "*"}";

            if (signature == _lastFilterSignature && _cardsGrid.Controls.Count > 0)
                return;

            _lastFilterSignature = signature;

            _cardsGrid.SuspendLayout();
            try
            {
                _cardsGrid.Controls.Clear();
                _cardsGrid.RowStyles.Clear();
                _cardsGrid.RowCount = 0;

                // Apply status filter first (drop non-matching statuses), then
                // the search term (name / category substring). Both filters
                // AND together — same convention as CustomerManagementView and
                // FeedbackManagementView.
                IEnumerable<CampaignResponseDto> filtered = _allCampaigns;

                if (statusFilter is not null)
                {
                    filtered = filtered.Where(c =>
                        string.Equals(c.Status, statusFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filtered = filtered.Where(c =>
                        c.PromoName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.PromoCategory.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                var filteredList = filtered.ToList();

                if (filteredList.Count == 0)
                {
                    _cardsGrid.RowCount = 1;
                    _cardsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                    _cardsGrid.Controls.Add(_emptyStateLabel, 0, 0);
                    _cardsGrid.SetColumnSpan(_emptyStateLabel, CampaignColumnCount);

                    SyncCardsGridWidth();
                    _cardsGrid.Height = Math.Max(
                        _cardsScrollHost.ClientSize.Height,
                        _cardsGrid.Padding.Top + _cardsGrid.Padding.Bottom + 60);
                    return;
                }

                var rows = (filteredList.Count + CampaignColumnCount - 1) / CampaignColumnCount;

                for (int r = 0; r < rows; r++)
                    _cardsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));

                // Spacer row — absorbs leftover vertical space so the last product row
                // doesn't stretch (same fix as TransactionView).
                _cardsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                _cardsGrid.RowCount = rows + 1;

                for (int i = 0; i < filteredList.Count; i++)
                {
                    var campaign = filteredList[i];
                    var card = new CampaignCardControl(campaign)
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(CardMargin),
                    };
                    card.CardClicked += (s, e) => OpenDetailModal(campaign);

                    var row = i / CampaignColumnCount;
                    var col = i % CampaignColumnCount;
                    _cardsGrid.Controls.Add(card, col, row);
                }

                SyncCardsGridWidth();
                var gridHeight = rows * RowHeight
                    + _cardsGrid.Padding.Top + _cardsGrid.Padding.Bottom;
                _cardsGrid.Height = Math.Max(gridHeight, _cardsScrollHost.ClientSize.Height);
            }
            finally
            {
                _cardsGrid.ResumeLayout(performLayout: true);
                _cardsScrollHost.PerformLayout();
            }
        }

        // =====================================================================
        // MODAL HOOKS
        // =====================================================================

        private async void OpenCreateModal()
        {
            using var form = FrmCampaignDetail.ForCreate(_controller);

            if (form.ShowDialog(FindForm()) == DialogResult.OK)
            {
                Toast.Show(this, "New campaign created.", ToastKind.Success);
                await ReloadAsync();
            }
        }

        private async void OpenDetailModal(CampaignResponseDto campaign)
        {
            using var form = FrmCampaignDetail.ForView(_controller, campaign);

            if (form.ShowDialog(FindForm()) == DialogResult.OK)
            {
                Toast.Show(this, $"Updated \"{campaign.PromoName}\".", ToastKind.Success);
                await ReloadAsync();
            }
        }
    }
}