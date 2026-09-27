using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// The Admin's Approvals tab. Lists pending Manager-submitted campaigns as horizontal
    /// cards on the right; clicking one opens its detail in the left drawer, where the
    /// Admin can Approve or Reject it.
    ///
    /// Campaigns come from GET /campaigns/pending — the same endpoint the API exposes
    /// specifically for this surface. Approve flips them Active; Reject moves them to
    /// Rejected with a required reason.
    ///
    /// Admin-only. FrmMain wires this in place of the Approvals placeholder.
    /// </summary>
    public class ApprovalsView : UserControl
    {
        private readonly CampaignService _controller;

        // ---- Header ----
        private Panel _headerPanel = null!;
        private Label _titleLabel = null!;
        private Label _countLabel = null!;

        // ---- Cards area ----
        private Panel _cardsScrollHost = null!;
        private FlowLayoutPanel _cardsFlow = null!;
        private Label _emptyStateLabel = null!;

        // ---- Drawer ----
        private Panel _drawerPanel = null!;
        private Label _drawerEmptyLabel = null!;
        private Panel _drawerContentPanel = null!;
        private Panel _detailBoxPanel = null!;
        private Panel _decisionPanel = null!;

        // Drawer content controls
        private Label _campaignNameLabel = null!;
        private Label _campaignMetaLabel = null!;
        private Label _reasonCaptionLabel = null!;
        private Label _reasonValueLabel = null!;
        private Label _detailCategoryLabel = null!;
        private Label _detailDiscountLabel = null!;
        private Label _detailTierLabel = null!;
        private Label _detailMinPurchaseLabel = null!;
        private Label _detailMaxDiscountLabel = null!;
        private Label _detailMaxRedemptionsLabel = null!;
        private Label _detailStartLabel = null!;
        private Label _detailEndLabel = null!;
        private Label _detailCreatorLabel = null!;
        private Label _detailRequestedAtLabel = null!;

        private TextBox _decisionNotesTxtBx = null!;
        private Label _decisionErrorLabel = null!;
        private Button _approveBtn = null!;
        private Button _rejectBtn = null!;

        private CampaignResponseDto? _selectedCampaign;

        // ---- Card metrics (kept in one place so the layout math can't drift) ----
        private const int CardHeight = 72;
        private const int CardGap = 8;

        // ---- Drawer metrics ----
        private const int DrawerWidth = 420;
        private const int DetailBoxWidth = 340;
        private const int DetailBoxHeight = 470;
        private const int DecisionPanelWidth = 340;
        private const int DecisionPanelHeight = 200;
        private const int DrawerInsetX = 18;
        private const int DetailRowSpacing = 28;

        public ApprovalsView()
        {
            _controller = new CampaignService();
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

            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfaceSecondary,
            };

            // ---------- RIGHT: card with header + list of approval cards ----------
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

            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _titleLabel = new Label
            {
                Text = "Pending Campaign Approvals",
                Location = new Point(20, 18),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(13f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            _countLabel = new Label
            {
                Location = new Point(20, 44),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            var headerDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BrandPalette.Border,
            };

            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_countLabel);
            _headerPanel.Controls.Add(headerDivider);

            // ---------- Cards area ----------
            _cardsScrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BrandPalette.SurfacePrimary,
                Padding = new Padding(16, 12, 16, 12),
            };

            _cardsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,               // we control width and height explicitly
                BackColor = BrandPalette.SurfacePrimary,
                Padding = new Padding(0),
            };

            _emptyStateLabel = new Label
            {
                Text = "No pending approvals. Campaigns submitted by Managers appear here.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = BrandPalette.SurfacePrimary,
                Visible = false,
            };

            _cardsScrollHost.Controls.Add(_cardsFlow);
            _cardsScrollHost.Controls.Add(_emptyStateLabel);
            _cardsScrollHost.Resize += (s, e) => SyncCardsFlowWidth();

            card.Controls.Add(_cardsScrollHost);
            card.Controls.Add(_headerPanel);

            cardOuter.Controls.Add(card);

            // ---------- LEFT: drawer ----------
            _drawerPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = DrawerWidth,
                BackColor = BrandPalette.SurfaceCard,
            };

            _drawerEmptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(10f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Text = "Select a campaign to review.",
            };

            _drawerContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                AutoScroll = true,
            };

            var drawerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20),
                Width = DrawerWidth,
            };

            // ---- Campaign Details header (sits above the box, distinct from it) ----
            var detailHeaderLabel = new Label
            {
                Text = "Campaign Details",
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(10f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                Margin = new Padding(4, 0, 0, 8),
            };
            drawerFlow.Controls.Add(detailHeaderLabel);

            // ---- Detail box ----
            _detailBoxPanel = BuildDetailBox();

            drawerFlow.Controls.Add(_detailBoxPanel);

            // ---- Requested-at line ----
            _detailRequestedAtLabel = new Label
            {
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Margin = new Padding(4, 0, 0, 14),
            };
            drawerFlow.Controls.Add(_detailRequestedAtLabel);

            // ---- Decision panel ----
            _decisionPanel = BuildDecisionPanel();
            drawerFlow.Controls.Add(_decisionPanel);

            _drawerContentPanel.Controls.Add(drawerFlow);
            _drawerPanel.Controls.Add(_drawerEmptyLabel);
            _drawerPanel.Controls.Add(_drawerContentPanel);

            bodyPanel.Controls.Add(cardOuter);
            bodyPanel.Controls.Add(_drawerPanel);

            Controls.Add(bodyPanel);
        }

        private Panel BuildDetailBox()
        {
            var box = new Panel
            {
                Size = new Size(DetailBoxWidth, DetailBoxHeight),
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 16),
            };

            // Header is placed OUTSIDE this box (see BuildLayout's drawer flow) so the
            // box itself reads as one self-contained region without a nested header.
            _campaignNameLabel = new Label
            {
                Location = new Point(DrawerInsetX, 18),
                MaximumSize = new Size(DetailBoxWidth - DrawerInsetX * 2, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            _campaignMetaLabel = new Label
            {
                Location = new Point(DrawerInsetX, 48),
                MaximumSize = new Size(DetailBoxWidth - DrawerInsetX * 2, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
            };

            // Reason sits above the field list — it's the Manager's justification,
            // and the Admin should read it before anything else in the campaign.
            _reasonCaptionLabel = new Label
            {
                Text = "Reason",
                Location = new Point(DrawerInsetX, 76),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            _reasonValueLabel = new Label
            {
                Location = new Point(DrawerInsetX, 94),
                MaximumSize = new Size(DetailBoxWidth - DrawerInsetX * 2, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            // Field rows, each spaced DetailRowSpacing apart, starting below the meta line.
            var fieldTop = 154;
            _detailCategoryLabel = MakeDetailLabel(DrawerInsetX, fieldTop);
            _detailDiscountLabel = MakeDetailLabel(DrawerInsetX, fieldTop + DetailRowSpacing * 1);
            _detailTierLabel = MakeDetailLabel(DrawerInsetX, fieldTop + DetailRowSpacing * 2);
            _detailMinPurchaseLabel = MakeDetailLabel(DrawerInsetX, fieldTop + DetailRowSpacing * 3);
            _detailMaxDiscountLabel = MakeDetailLabel(DrawerInsetX, fieldTop + DetailRowSpacing * 4);
            _detailMaxRedemptionsLabel = MakeDetailLabel(DrawerInsetX, fieldTop + DetailRowSpacing * 5);
            _detailStartLabel = MakeDetailLabel(DrawerInsetX, fieldTop + DetailRowSpacing * 6);
            _detailEndLabel = MakeDetailLabel(DrawerInsetX, fieldTop + DetailRowSpacing * 7);
            _detailCreatorLabel = MakeDetailLabel(DrawerInsetX, fieldTop + DetailRowSpacing * 8);

            box.Controls.Add(_campaignNameLabel);
            box.Controls.Add(_campaignMetaLabel);
            box.Controls.Add(_reasonCaptionLabel);
            box.Controls.Add(_reasonValueLabel);
            box.Controls.Add(_detailCategoryLabel);
            box.Controls.Add(_detailDiscountLabel);
            box.Controls.Add(_detailTierLabel);
            box.Controls.Add(_detailMinPurchaseLabel);
            box.Controls.Add(_detailMaxDiscountLabel);
            box.Controls.Add(_detailMaxRedemptionsLabel);
            box.Controls.Add(_detailStartLabel);
            box.Controls.Add(_detailEndLabel);
            box.Controls.Add(_detailCreatorLabel);

            return box;
        }

        private Panel BuildDecisionPanel()
        {
            var panel = new Panel
            {
                Size = new Size(DecisionPanelWidth, DecisionPanelHeight),
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 16),
            };

            const int inset = 14;

            var header = new Label
            {
                Text = "Decision",
                Location = new Point(inset, 12),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(9.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var hint = new Label
            {
                Text = "Notes optional on approve; required on reject.",
                Location = new Point(inset, 34),
                Size = new Size(DecisionPanelWidth - inset * 2, 20),
                AutoSize = false,
                Font = BrandPalette.BrandFontRegular(8f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _decisionNotesTxtBx = new TextBox
            {
                Location = new Point(inset, 62),
                Size = new Size(DecisionPanelWidth - inset * 2, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Notes / rejection reason",
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };
            _decisionNotesTxtBx.TextChanged += (s, e) => UpdateRejectEnabled();

            _decisionErrorLabel = new Label
            {
                Location = new Point(inset, 128),
                Size = new Size(DecisionPanelWidth - inset * 2, 20),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Visible = false,
            };

            var buttonWidth = (DecisionPanelWidth - inset * 2 - 8) / 2;   // two buttons, 8px gap

            _approveBtn = new Button
            {
                Text = "Approve",
                Location = new Point(inset, 152),
                Size = new Size(buttonWidth, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
            };
            _approveBtn.FlatAppearance.BorderSize = 0;
            _approveBtn.Click += ApproveBtn_Click;

            _rejectBtn = new Button
            {
                Text = "Reject",
                Location = new Point(inset + buttonWidth + 8, 152),
                Size = new Size(buttonWidth, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.StatusNegativeFill,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            _rejectBtn.FlatAppearance.BorderSize = 0;
            _rejectBtn.Click += RejectBtn_Click;

            ButtonStyler.TrackEnabledState(_approveBtn);
            ButtonStyler.TrackEnabledState(_rejectBtn);

            panel.Controls.Add(_decisionErrorLabel);
            panel.Controls.Add(_approveBtn);
            panel.Controls.Add(_rejectBtn);
            panel.Controls.Add(_decisionNotesTxtBx);
            panel.Controls.Add(hint);
            panel.Controls.Add(header);

            return panel;
        }

        private static Label MakeDetailLabel(int x, int y) => new()
        {
            Location = new Point(x, y),
            AutoSize = true,
            Font = BrandPalette.BrandFontRegular(9.5f),
            ForeColor = BrandPalette.NeutralText,
            BackColor = Color.Transparent,
        };

        private void SyncCardsFlowWidth()
        {
            if (_cardsScrollHost is null || _cardsFlow is null)
                return;

            var hostWidth = _cardsScrollHost.ClientSize.Width;
            if (hostWidth <= 0)
                return;

            // Reserve for the vertical scrollbar if it appears.
            var usableWidth = Math.Max(200, hostWidth
                - _cardsScrollHost.Padding.Horizontal
                - SystemInformation.VerticalScrollBarWidth);

            _cardsFlow.Width = usableWidth;

            foreach (var c in _cardsFlow.Controls.OfType<ApprovalCardControl>())
            {
                c.Width = usableWidth;
                c.Height = CardHeight;
            }

            _cardsFlow.PerformLayout();
        }

        // =====================================================================
        // DATA
        // =====================================================================

        private async Task ReloadAsync()
        {
            List<CampaignResponseDto> pending;
            try
            {
                pending = await _controller.GetPendingAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Error loading pending approvals");
                return;
            }

            // Clear selection and drawer
            _selectedCampaign = null;
            RefreshDrawer();

            // The width we should give each card. Computed once, used both when creating
            // the cards and when resizing the flow panel afterwards — using the flow panel's
            // own Width here would give 0 on first load, which made the cards invisible.
            var cardWidth = Math.Max(200,
                _cardsScrollHost.ClientSize.Width
                - _cardsScrollHost.Padding.Horizontal
                - SystemInformation.VerticalScrollBarWidth);

            _cardsFlow.SuspendLayout();
            try
            {
                _cardsFlow.Controls.Clear();
                _cardsFlow.Width = cardWidth;

                if (pending.Count == 0)
                {
                    _emptyStateLabel.Visible = true;
                    _cardsFlow.Visible = false;
                    _cardsFlow.Height = 0;
                }
                else
                {
                    _emptyStateLabel.Visible = false;
                    _cardsFlow.Visible = true;

                    foreach (var campaign in pending)
                    {
                        var card = new ApprovalCardControl(campaign)
                        {
                            Width = cardWidth,
                        };
                        card.CardClicked += (s, e) => SelectCampaign(campaign);
                        _cardsFlow.Controls.Add(card);
                    }

                    _cardsFlow.Height = pending.Count * (CardHeight + CardGap) + 4;
                }
            }
            finally
            {
                _cardsFlow.ResumeLayout(performLayout: true);
            }

            _countLabel.Text = pending.Count == 1
                ? "1 campaign awaiting approval"
                : $"{pending.Count} campaigns awaiting approval";
        }

        private void SelectCampaign(CampaignResponseDto campaign)
        {
            _selectedCampaign = campaign;
            RefreshDrawer();
        }

        // =====================================================================
        // DRAWER
        // =====================================================================

        private void RefreshDrawer()
        {
            var campaign = _selectedCampaign;
            if (campaign is null)
            {
                _drawerEmptyLabel.Visible = true;
                _drawerContentPanel.Visible = false;
                return;
            }

            _drawerEmptyLabel.Visible = false;
            _drawerContentPanel.Visible = true;

            _campaignNameLabel.Text = campaign.PromoName;
            _campaignMetaLabel.Text = FormatDiscount(campaign.DiscountType, campaign.DiscountValue);
            _reasonValueLabel.Text = string.IsNullOrWhiteSpace(campaign.Reason)
                ? "(no reason provided)"
                : campaign.Reason;

            _detailCategoryLabel.Text = $"Category: {campaign.PromoCategory}";
            _detailDiscountLabel.Text = $"Discount: {FormatDiscount(campaign.DiscountType, campaign.DiscountValue)}";
            _detailTierLabel.Text = $"Target tier: {campaign.TargetTier}";
            _detailMinPurchaseLabel.Text = $"Min purchase: {(campaign.MinPurchaseAmount.HasValue ? $"₱{campaign.MinPurchaseAmount.Value:0.00}" : "—")}";
            _detailMaxDiscountLabel.Text = $"Max discount: {(campaign.MaxDiscountAmount.HasValue ? $"₱{campaign.MaxDiscountAmount.Value:0.00}" : "—")}";
            _detailMaxRedemptionsLabel.Text = $"Max redemptions: {campaign.MaxRedemptionsPerCustomer} per customer";
            _detailStartLabel.Text = $"Start: {campaign.StartDate:MMMM d, yyyy}";
            _detailEndLabel.Text = $"End: {campaign.EndDate:MMMM d, yyyy}";
            _detailCreatorLabel.Text = $"Requested by: {(string.IsNullOrWhiteSpace(campaign.CreatedByName) ? "—" : campaign.CreatedByName)}";

            _detailRequestedAtLabel.Text = campaign.DecidedAt.HasValue
                ? $"Decision recorded {campaign.DecidedAt.Value:yyyy-MM-dd HH:mm}"
                : "Awaiting decision";

            // Reset decision panel
            _decisionNotesTxtBx.Text = string.Empty;
            _decisionErrorLabel.Text = string.Empty;
            _decisionErrorLabel.Visible = false;

            var isPending = campaign.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase);
            _approveBtn.Enabled = isPending;
            _rejectBtn.Enabled = isPending && !string.IsNullOrWhiteSpace(_decisionNotesTxtBx.Text);
        }

        private void UpdateRejectEnabled()
        {
            var isPending = _selectedCampaign?.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) == true;
            _rejectBtn.Enabled = isPending && !string.IsNullOrWhiteSpace(_decisionNotesTxtBx.Text);
        }

        // =====================================================================
        // ACTIONS
        // =====================================================================

        private async void ApproveBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedCampaign is null) return;

            var campaign = _selectedCampaign;
            var notes = string.IsNullOrWhiteSpace(_decisionNotesTxtBx.Text)
                ? null
                : _decisionNotesTxtBx.Text.Trim();

            var confirm = MessageBox.Show(
                FindForm(),
                $"Approve \"{campaign.PromoName}\"? It will go live immediately.",
                "Approve Campaign",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _approveBtn.Enabled = false;
                _rejectBtn.Enabled = false;

                var adminId = EmployeeSession.Current?.EmployeeId
                    ?? throw new InvalidOperationException("No employee session.");

                var result = await _controller.ApproveAsync(campaign.CampaignId, adminId, notes);

                // The approve succeeds even if the retention-email send failed —
                // surface both outcomes so the Admin knows whether the customer
                // message went out.
                if (result.EmailSent)
                {
                    Toast.Show(this,
                        $"Approved \"{campaign.PromoName}\". Retention email sent.",
                        ToastKind.Success);
                }
                else
                {
                    Toast.Show(this,
                        $"Approved \"{campaign.PromoName}\", but the email failed: {result.EmailError}",
                        ToastKind.Info);
                }

                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _decisionErrorLabel.Text = ex.Message;
                _decisionErrorLabel.Visible = true;
            }
            finally
            {
                _approveBtn.Enabled = true;
            }
        }

        private async void RejectBtn_Click(object? sender, EventArgs e)
        {
            if (_selectedCampaign is null) return;

            var notes = _decisionNotesTxtBx.Text.Trim();
            if (string.IsNullOrWhiteSpace(notes))
            {
                _decisionErrorLabel.Text = "A rejection reason is required.";
                _decisionErrorLabel.Visible = true;
                return;
            }

            var campaign = _selectedCampaign;

            var confirm = MessageBox.Show(
                FindForm(),
                $"Reject \"{campaign.PromoName}\"? The Manager will see your reason and can edit and resubmit.",
                "Reject Campaign",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _approveBtn.Enabled = false;
                _rejectBtn.Enabled = false;

                var adminId = EmployeeSession.Current?.EmployeeId
                    ?? throw new InvalidOperationException("No employee session.");

                await _controller.RejectAsync(campaign.CampaignId, adminId, notes);

                Toast.Show(this, $"Rejected \"{campaign.PromoName}\".", ToastKind.Info);

                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _decisionErrorLabel.Text = ex.Message;
                _decisionErrorLabel.Visible = true;
            }
            finally
            {
                _approveBtn.Enabled = true;
            }
        }

        private static string FormatDiscount(string type, decimal value) =>
            type.Equals("Percentage", StringComparison.OrdinalIgnoreCase)
                ? $"{value:0.##}% off"
                : $"₱{value:0.##} off";
    }
}