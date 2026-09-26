using CRM_Morcaffein.Controllers;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.Session;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// The Transactions/POS tab.
    ///
    /// Layout (top to bottom):
    ///   Row 1  — customer search box + Go, profile summary to its right (full width)
    ///   Row 2  — drinks catalog (left, fills) + Order / Payables / Log Sale (right, 380px)
    ///
    /// The catalog is a wrapper-Panel (AutoScroll) hosting a TableLayoutPanel with the
    /// product cards. The wrapper scrolls; the grid is absolutely positioned inside it and
    /// its height is set explicitly per row count — this is the only reliable way to get
    /// AutoScroll to fire on a WinForms grid.
    ///
    /// Customer selection gates the middle+right area via a non-docked overlay panel that
    /// tracks the Row 2 container's bounds.
    /// </summary>
    public class TransactionView : UserControl
    {
        // ---------------------------------------------------------------------
        // Layout constants
        // ---------------------------------------------------------------------
        private const int DrinksColumnCount = 5;
        private const int DrinksRowHeight = 216;
        private const int CustomerAreaHeight = 96;
        private const int RightColumnWidth = 380;
        private const string AllCategoriesOption = "All Categories";
        private const decimal VatRate = 0.12m;

        // ---------------------------------------------------------------------
        // State
        // ---------------------------------------------------------------------
        private TransactionController? _controller;
        private string _selectedCategory = string.Empty;
        private string _lastProductFilterSignature = string.Empty;

        private IReadOnlyList<Product>? _cachedProducts;
        private IReadOnlyList<string>? _cachedCategories;
        private IReadOnlyList<ProductModifier>? _cachedModifiers;

        private System.Windows.Forms.Timer _productSearchDebounce = null!;

        // ---------------------------------------------------------------------
        // Row 1: customer area
        // ---------------------------------------------------------------------
        private TextBox _customerSearchTxtBx = null!;
        private Label _customerSearchErrorLabel = null!;
        private Panel _customerProfilePanel = null!;
        private Label _customerProfileNameLabel = null!;
        private Label _customerProfileDetailLabel = null!;

        // ---------------------------------------------------------------------
        // Row 2, left: drinks
        // ---------------------------------------------------------------------
        private TextBox _productSearchTxtBx = null!;
        private ComboBox _categoryCombo = null!;
        private Panel _productsScrollHost = null!;
        private TableLayoutPanel _productsGrid = null!;

        // ---------------------------------------------------------------------
        // Row 2, right: order / payables / log sale
        // ---------------------------------------------------------------------
        private FlowLayoutPanel _cartLinesPanel = null!;
        private Label _cartEmptyLabel = null!;
        private Label _subTotalValueLabel = null!;
        private Label _discountValueLabel = null!;
        private Label _vatValueLabel = null!;
        private Label _pwdSeniorValueLabel = null!;
        private CheckBox _pwdSeniorChk = null!;
        private Label _totalValueLabel = null!;
        private Label _cartStatusLabel = null!;
        private Button _promoMenuBtn = null!;
        private Button _logSaleBtn = null!;

        // ---------------------------------------------------------------------
        // Overlay (blocks Row 2 until a customer is searched)
        // ---------------------------------------------------------------------
        private Panel _blockOverlayPanel = null!;

        // =====================================================================
        // CONSTRUCTION
        // =====================================================================

        public TransactionView()
        {
            BuildLayout();

            typeof(Control)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(_productsGrid, true);

            RefreshSummary();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _productSearchDebounce?.Dispose();

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
            };

            // Row 1 — customer area, full width, docked top.
            var customerArea = BuildCustomerArea();

            // Row 2 — drinks + order/payables, fills the rest.
            var lowerRow = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
            };

            var drinksArea = BuildDrinksArea();
            var rightColumn = BuildOrderColumn();

            lowerRow.Controls.Add(drinksArea);
            lowerRow.Controls.Add(rightColumn);

            // Overlay — absolutely positioned over lowerRow, brought to front. Covers
            // Row 2 only; the customer search box in Row 1 stays usable.
            _blockOverlayPanel = new Panel
            {
                BackColor = BrandPalette.SurfaceCard,
                Visible = true,
            };

            var overlayMessage = new Label
            {
                Text = "Search for a customer to begin.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(12f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };
            _blockOverlayPanel.Controls.Add(overlayMessage);

            lowerRow.Controls.Add(_blockOverlayPanel);
            _blockOverlayPanel.BringToFront();

            void FitOverlay()
            {
                _blockOverlayPanel.Bounds = new Rectangle(
                    0, 0,
                    lowerRow.ClientSize.Width,
                    lowerRow.ClientSize.Height);
            }
            lowerRow.Resize += (s, e) => FitOverlay();
            FitOverlay();

            tableCardPanel.Controls.Add(lowerRow);
            tableCardPanel.Controls.Add(customerArea);

            cardOuterPanel.Controls.Add(tableCardPanel);
            Controls.Add(cardOuterPanel);
        }

        // =====================================================================
        // ROW 1 — CUSTOMER AREA
        // =====================================================================

        private Panel BuildCustomerArea()
        {
            var area = new Panel
            {
                Dock = DockStyle.Top,
                Height = CustomerAreaHeight,
                BackColor = BrandPalette.SurfaceCard,
                Padding = new Padding(12),
            };

            // --- Search row (left) ---
            // Fixed height + absolute-positioned children. Dock.Fill on the textbox made
            // it stretch to the panel's full height while the button drew at its own
            // intrinsic height, so they could never align. Pinning both to the same
            // y/height is the only reliable way.
            const int searchRowHeight = 34;
            const int searchButtonWidth = 80;
            const int searchRowWidth = 360;

            var searchRow = new Panel
            {
                Dock = DockStyle.Left,
                Width = searchRowWidth,
                Height = searchRowHeight,
            };

            _customerSearchTxtBx = new TextBox
            {
                Location = new Point(0, 0),
                Width = searchRowWidth - searchButtonWidth,
                Height = searchRowHeight,
                PlaceholderText = "Customer ID or phone",
                Font = BrandPalette.BrandFontRegular(10.5f),
                BorderStyle = BorderStyle.FixedSingle,
            };

            _customerSearchTxtBx.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    RunCustomerSearch();
                    e.SuppressKeyPress = true;
                }
            };

            _customerSearchTxtBx.KeyPress += (s, e) =>
            {
                // Numeric only — the POS search accepts Customer ID or phone.
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    e.Handled = true;
            };

            var searchBtn = new Button
            {
                Text = "Search",
                Location = new Point(searchRowWidth - searchButtonWidth, 0),
                Width = searchButtonWidth,
                Height = searchRowHeight,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9f),
                Cursor = Cursors.Hand,
                Padding = new Padding(0),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            searchBtn.FlatAppearance.BorderSize = 0;
            searchBtn.Click += (s, e) => RunCustomerSearch();

            searchRow.Controls.Add(_customerSearchTxtBx);
            searchRow.Controls.Add(searchBtn);

            // --- Profile (right of search) ---
            // Transparent — no background, no border. Only the content reads as the
            // "highlight": the name is brand brown, the detail line muted.
            _customerProfilePanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 320,
                Visible = false,
                BackColor = Color.Transparent,
                Padding = new Padding(24, 4, 8, 4),
            };

            _customerProfileNameLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = BrandPalette.BrandFontBold(12f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
            };

            _customerProfileDetailLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            _customerProfilePanel.Controls.Add(_customerProfileDetailLabel);
            _customerProfilePanel.Controls.Add(_customerProfileNameLabel);

            // --- Error label (shares the profile slot) ---
            _customerSearchErrorLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0),
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.StatusNegativeFill,
                BackColor = Color.Transparent,
            };

            // Add order: Fill-docked error label first, then the Left-docked profile panel
            // and search row. Only one of profile/error is visible at a time, so they never
            // fight for the same space in practice.
            area.Controls.Add(_customerSearchErrorLabel);
            area.Controls.Add(_customerProfilePanel);
            area.Controls.Add(searchRow);

            return area;
        }

        // =====================================================================
        // ROW 2, LEFT — DRINKS
        // =====================================================================

        private Panel BuildDrinksArea()
        {
            var area = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BrandPalette.SurfacePrimary,
            };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(16, 14, 16, 10),
            };

            // --- Search row ---
            var searchRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(0, 2, 0, 2),
            };

            _productSearchTxtBx = new TextBox
            {
                Dock = DockStyle.Fill,
                PlaceholderText = "Search drinks by name",
                Font = BrandPalette.BrandFontRegular(11f),
                BorderStyle = BorderStyle.FixedSingle,
            };

            // Debounce — only refresh after the user pauses typing. Rebuilding a 40-card
            // TableLayoutPanel on every keystroke was the source of the visible lag.
            _productSearchDebounce = new System.Windows.Forms.Timer { Interval = 200 };
            _productSearchDebounce.Tick += (s, e) =>
            {
                _productSearchDebounce.Stop();
                RefreshProductsPanel();
            };

            _productSearchTxtBx.TextChanged += (s, e) =>
            {
                _productSearchDebounce.Stop();
                _productSearchDebounce.Start();
            };

            searchRow.Controls.Add(_productSearchTxtBx);

            // --- Sort row ---
            var sortRow = new Panel { Dock = DockStyle.Top, Height = 34 };

            var drinksLabel = new Label
            {
                Text = "Drinks",
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(13f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var sortLabel = new Label
            {
                Text = "Sort by Category:",
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            _categoryCombo = new ComboBox
            {
                Size = new Size(180, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = BrandPalette.BrandFontRegular(9.5f),
            };
            _categoryCombo.SelectedIndexChanged += (s, e) =>
            {
                if (_categoryCombo.SelectedItem is not string category) return;
                _selectedCategory = category == AllCategoriesOption ? string.Empty : category;
                RefreshProductsPanel();
            };

            var drinksWidth = TextRenderer.MeasureText(drinksLabel.Text, drinksLabel.Font).Width;
            var sortLabelWidth = TextRenderer.MeasureText(sortLabel.Text, sortLabel.Font).Width;

            drinksLabel.Left = 0;
            drinksLabel.Top = 0;

            sortLabel.Left = drinksWidth + 24;
            sortLabel.Top = (sortRow.Height - sortLabel.Height) / 2 + 2;

            _categoryCombo.Left = drinksWidth + 24 + sortLabelWidth + 14;
            _categoryCombo.Top = (sortRow.Height - _categoryCombo.Height) / 2;

            sortRow.Controls.Add(drinksLabel);
            sortRow.Controls.Add(sortLabel);
            sortRow.Controls.Add(_categoryCombo);

            var divider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BrandPalette.Border,
            };

            header.Controls.Add(sortRow);
            header.Controls.Add(searchRow);
            header.Controls.Add(divider);

            // --- Scroll host + grid ---
            // The wrapper Panel owns AutoScroll. The grid is ABSOLUTELY positioned inside
            // it (no Dock), and its width/height are set explicitly — a Dock.Top/Fill child
            // of an AutoScroll panel gets its size re-managed by the dock engine, which is
            // why the scrollbar never appeared on the previous attempts.
            _productsScrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _productsGrid = new TableLayoutPanel
            {
                Location = new Point(0, 0),
                ColumnCount = DrinksColumnCount,
                RowCount = 0,
                AutoScroll = false,
                Padding = new Padding(12),
                BackColor = BrandPalette.SurfacePrimary,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            };

            for (int i = 0; i < DrinksColumnCount; i++)
                _productsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / DrinksColumnCount));

            _productsScrollHost.Controls.Add(_productsGrid);
            _productsScrollHost.Resize += (s, e) => SyncProductsGridWidth();

            area.Controls.Add(_productsScrollHost);
            area.Controls.Add(header);

            return area;
        }

        private void SyncProductsGridWidth()
        {
            if (_productsScrollHost is null || _productsGrid is null)
                return;

            var hostWidth = _productsScrollHost.ClientSize.Width;
            if (hostWidth <= 0)
                return;

            _productsGrid.Width = hostWidth;
        }

        // =====================================================================
        // ROW 2, RIGHT — ORDER / PAYABLES / LOG SALE
        // =====================================================================

        private Panel BuildOrderColumn()
        {
            var column = new Panel
            {
                Dock = DockStyle.Right,
                Width = RightColumnWidth,
                BackColor = BrandPalette.SurfacePrimary,
                Padding = new Padding(10),
            };

            var orderBox = BuildOrderBox();
            var payablesBox = BuildPayablesBox();
            var logSaleBox = BuildLogSaleBox();

            column.Controls.Add(orderBox);
            column.Controls.Add(payablesBox);
            column.Controls.Add(logSaleBox);

            return column;
        }

        private Panel BuildOrderBox()
        {
            var box = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = BrandPalette.SurfacePrimary,
            };

            var header = new Label
            {
                Text = "Order",
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(10, 6, 0, 0),
                Font = BrandPalette.BrandFontBold(10f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BrandPalette.Border,
            };

            _cartLinesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8, 6, 8, 6),
            };

            _cartEmptyLabel = new Label
            {
                Text = "No items yet.\nTap a drink to add it.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };
            _cartLinesPanel.Controls.Add(_cartEmptyLabel);

            box.Controls.Add(_cartLinesPanel);
            box.Controls.Add(divider);
            box.Controls.Add(header);

            return box;
        }

        private Panel BuildPayablesBox()
        {
            var box = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 312,
                Margin = new Padding(10, 0, 10, 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = BrandPalette.SurfacePrimary,
                Padding = new Padding(14, 12, 14, 12),
            };

            var header = new Label
            {
                Text = "Payables",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            _cartStatusLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                BackColor = Color.Transparent,
            };

            var summaryTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
            };
            summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            summaryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            for (int i = 0; i < 5; i++)
                summaryTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));

            Label MakeCaption(string text) => new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };

            Label MakeValue() => new()
            {
                Text = "₱0.00",
                Dock = DockStyle.Fill,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
            };

            _subTotalValueLabel = MakeValue();
            _discountValueLabel = MakeValue();
            _vatValueLabel = MakeValue();
            _pwdSeniorValueLabel = MakeValue();
            _totalValueLabel = MakeValue();
            _totalValueLabel.ForeColor = BrandPalette.Brand;
            _totalValueLabel.Font = BrandPalette.BrandFontBold(15f);

            _pwdSeniorChk = new CheckBox
            {
                Text = "PWD/Senior Disc.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Cursor = Cursors.Hand,
                Padding = new Padding(0),
                Margin = new Padding(0),
            };
            _pwdSeniorChk.CheckedChanged += (s, e) =>
            {
                if (_controller is null)
                {
                    _pwdSeniorChk.Checked = false;
                    return;
                }

                try
                {
                    _controller.SetPwdSeniorDiscount(_pwdSeniorChk.Checked);
                    RefreshSummary();
                }
                catch (Exception ex)
                {
                    ShowCartStatus(ex.Message);
                    _pwdSeniorChk.Checked = false;
                }
            };

            summaryTable.Controls.Add(MakeCaption("Subtotal"), 0, 0);
            summaryTable.Controls.Add(_subTotalValueLabel, 1, 0);
            summaryTable.Controls.Add(MakeCaption("Discount"), 0, 1);
            summaryTable.Controls.Add(_discountValueLabel, 1, 1);
            summaryTable.Controls.Add(MakeCaption("VAT (12%)"), 0, 2);
            summaryTable.Controls.Add(_vatValueLabel, 1, 2);
            summaryTable.Controls.Add(_pwdSeniorChk, 0, 3);
            summaryTable.Controls.Add(_pwdSeniorValueLabel, 1, 3);
            summaryTable.Controls.Add(MakeCaption("Total"), 0, 4);
            summaryTable.Controls.Add(_totalValueLabel, 1, 4);

            // Promo button — full-width row at the bottom of Payables.
            var promoRow = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                Padding = new Padding(0, 8, 0, 0),
            };

            _promoMenuBtn = new Button
            {
                Text = "Select a Promo ▾",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9f),
                Cursor = Cursors.Hand,
                Enabled = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
            };
            _promoMenuBtn.FlatAppearance.BorderSize = 0;
            _promoMenuBtn.Click += (s, e) => ShowPromoMenu();
            ButtonStyler.TrackEnabledState(_promoMenuBtn);

            promoRow.Controls.Add(_promoMenuBtn);

            box.Controls.Add(summaryTable);
            box.Controls.Add(_cartStatusLabel);
            box.Controls.Add(header);
            box.Controls.Add(promoRow);

            return box;
        }

        private Panel BuildLogSaleBox()
        {
            var box = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Margin = new Padding(10, 0, 10, 10),
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _logSaleBtn = new Button
            {
                Text = "Log Sale",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(10.5f),
                Enabled = false,
            };
            _logSaleBtn.FlatAppearance.BorderSize = 0;
            _logSaleBtn.Click += (s, e) => CompleteSale();
            ButtonStyler.TrackEnabledState(_logSaleBtn);

            box.Controls.Add(_logSaleBtn);
            return box;
        }

        // =====================================================================
        // CATALOG LOADING
        // =====================================================================

        private async Task EnsureCatalogLoadedAsync()
        {
            if (_cachedProducts is not null) return;

            var catalog = await ProductCatalogCache.GetCatalogAsync();

            _cachedProducts = catalog.Products.Select(MapToProductEntity).ToList();
            _cachedCategories = catalog.Categories;
            _cachedModifiers = catalog.Modifiers.Select(MapToModifierEntity).ToList();
        }

        private static Product MapToProductEntity(ProductResponseDto dto)
        {
            var product = new Product
            {
                ProductId = dto.ProductId,
                ProductCode = dto.ProductCode,
                ProductName = dto.ProductName,
                ProductCategory = dto.ProductCategory,
                BasePrice = dto.BasePrice,
                ImageUrl = dto.ImageUrl,
                LocalImagePath = dto.LocalImagePath,
                IsAvailable = dto.IsAvailable,
            };

            foreach (var sizeDto in dto.Sizes)
            {
                product.ProductSizes.Add(new ProductSize
                {
                    ProductSizeId = sizeDto.ProductSizeId,
                    ProductId = dto.ProductId,
                    SizeName = sizeDto.SizeName,
                    Price = sizeDto.Price,
                });
            }

            return product;
        }

        private static ProductModifier MapToModifierEntity(ProductModifierResponseDto dto) => new()
        {
            ProductModifierId = dto.ProductModifierId,
            Name = dto.Name,
            Category = dto.Category,
            Price = dto.Price,
        };

        private static Campaign MapToCampaignEntity(CampaignResponseDto dto) => new()
        {
            CampaignId = dto.CampaignId,
            PromoName = dto.PromoName,
            PromoCategory = dto.PromoCategory,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            TargetTier = dto.TargetTier,
            MinPurchaseAmount = dto.MinPurchaseAmount,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            MaxRedemptionsPerCustomer = dto.MaxRedemptionsPerCustomer,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
        };

        // =====================================================================
        // CUSTOMER SELECTION
        // =====================================================================

        private async void RunCustomerSearch()
        {
            var query = _customerSearchTxtBx.Text.Trim();
            if (string.IsNullOrEmpty(query))
                return;

            try
            {
                var customerApi = new CustomerApi();
                var match = await customerApi.FindCustomerAsync(query);

                if (match is null)
                {
                    _customerSearchErrorLabel.Text = $"No customer found for \"{query}\".";
                    _customerProfilePanel.Visible = false;
                    _blockOverlayPanel.Visible = true;
                    return;
                }

                _customerSearchErrorLabel.Text = string.Empty;
                ShowCustomerProfile(match);
                await SelectCustomerAsync(match);
            }
            catch (Exception ex)
            {
                _customerSearchErrorLabel.Text = ex.Message;
                _customerProfilePanel.Visible = false;
                _blockOverlayPanel.Visible = true;
            }
        }

        private void ShowCustomerProfile(CustomerResponseDto customer)
        {
            _customerProfileNameLabel.Text = customer.FullName;
            _customerProfileDetailLabel.Text = $"{customer.Tier} Tier   •   {customer.PointsBalance} pts\n{customer.ContactNumber}";
            _customerProfilePanel.Visible = true;
        }

        private async Task SelectCustomerAsync(CustomerResponseDto dto)
        {
            var employeeId = EmployeeSession.Current?.EmployeeId
                ?? throw new InvalidOperationException("No employee session — the Transaction view requires a logged-in employee.");

            if (_controller is not null && _controller.CartItems.Count > 0)
            {
                var currentCustomerName = _controller.Customer.FullName;
                var confirm = MessageBox.Show(
                    FindForm(),
                    $"Discard the in-progress order for {currentCustomerName} and start a new one for {dto.FullName}?",
                    "Start New Transaction",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                {
                    _customerSearchTxtBx.Text = _controller.Customer.CustomerId.ToString();
                    return;
                }
            }

            try
            {
                await EnsureCatalogLoadedAsync();

                var entity = new Customer
                {
                    CustomerId = dto.CustomerId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    ContactNumber = dto.ContactNumber,
                    Email = dto.Email,
                    Birthday = dto.Birthday,
                    LastVisitDate = dto.LastVisitDate,
                    PointsBalance = dto.PointsBalance,
                    IsActive = dto.IsActive,
                };

                var campaignDtos = await CampaignCache.GetActiveCampaignsAsync();
                var campaigns = campaignDtos.Select(MapToCampaignEntity).ToList();

                _controller = new TransactionController(
                    _cachedProducts!,
                    _cachedCategories!,
                    _cachedModifiers!,
                    campaigns,
                    entity,
                    employeeId);

                _blockOverlayPanel.Visible = false;

                BuildCategoryOptions();
                RefreshProductsPanel();
                RefreshCartLines();
                RefreshSummary();
            }
            catch (Exception ex)
            {
                _customerSearchErrorLabel.Text = ex.Message;
                _customerProfilePanel.Visible = false;
                _blockOverlayPanel.Visible = true;
            }
        }

        // =====================================================================
        // PROMO
        // =====================================================================

        private void ShowPromoMenu()
        {
            if (_controller is null)
                return;

            var menu = new ContextMenuStrip
            {
                // Caps the menu's growth so a long promo list never renders past the
                // bottom of the screen — ToolStripDropDownMenu (which ContextMenuStrip
                // derives from) shows built-in scroll arrows once content exceeds this,
                // instead of the menu just keeping growing downward.
                MaximumSize = new Size(320, 320),
            };

            if (_controller.Order.CampaignRedeems.Count > 0)
            {
                var clear = menu.Items.Add("Clear applied promo");
                clear.Font = BrandPalette.BrandFontBold(9f);
                clear.Click += (s, e) =>
                {
                    _controller.ClearPromos();
                    RefreshSummary();
                    RefreshPromoButton();
                };
                menu.Items.Add(new ToolStripSeparator());
            }

            var eligiblePromos = _controller.GetEligiblePromos();
            if (eligiblePromos.Count == 0)
            {
                var none = menu.Items.Add("No promos available for this order");
                none.Enabled = false;
            }
            else
            {
                foreach (var campaign in eligiblePromos)
                {
                    var alreadyApplied = _controller.Order.CampaignRedeems
                        .Any(r => r.CampaignId == campaign.CampaignId);

                    var label = campaign.DiscountType switch
                    {
                        "Percentage" => $"{campaign.PromoName} — {campaign.DiscountValue:0.##}% off",
                        "B1T1" => $"{campaign.PromoName} — Buy 1, Take 1",
                        _ => $"{campaign.PromoName} — ₱{campaign.DiscountValue:0.##} off",
                    };
                    if (alreadyApplied)
                        label += "  (applied)";

                    var item = menu.Items.Add(label);
                    item.Enabled = !alreadyApplied;
                    item.Click += (s, e) =>
                    {
                        try
                        {
                            _controller.ApplyPromo(campaign);
                            RefreshSummary();
                            RefreshPromoButton();
                        }
                        catch (Exception ex)
                        {
                            Toast.Show(this, ex.Message, ToastKind.Error);
                        }
                    };
                }
            }

            // Button sits in the Payables box with summary rows above and Log Sale below —
            // open downward so the summary stays visible.
            menu.Show(_promoMenuBtn, new Point(0, _promoMenuBtn.Height));
        }

        private void RefreshPromoButton()
        {
            if (_controller is null)
            {
                _promoMenuBtn.Text = "Select a Promo ▾";
                _promoMenuBtn.Enabled = false;
                return;
            }

            var applied = _controller.Order.CampaignRedeems.FirstOrDefault();

            if (applied is null)
            {
                _promoMenuBtn.Text = "Select a Promo ▾";
            }
            else
            {
                var campaign = _controller.GetEligiblePromos()
                                   .FirstOrDefault(c => c.CampaignId == applied.CampaignId);

                var name = campaign?.PromoName ?? $"Promo #{applied.CampaignId}";
                _promoMenuBtn.Text = $"{name} ▾";
            }

            _promoMenuBtn.Enabled = true;
        }

        private void RefreshSummary()
        {
            RefreshPromoButton();

            if (_controller is null)
            {
                _subTotalValueLabel.Text = "₱0.00";
                _discountValueLabel.Text = "₱0.00";
                _vatValueLabel.Text = "₱0.00";
                _pwdSeniorValueLabel.Text = "₱0.00";
                _totalValueLabel.Text = "₱0.00";
                _logSaleBtn.Enabled = false;
                _pwdSeniorChk.Enabled = false;
                return;
            }

            _pwdSeniorChk.Enabled = true;

            var subTotal = _controller.SubTotal;
            var afterPromo = subTotal - _controller.TotalDiscount;
            var vatAmount = afterPromo > 0
                ? decimal.Round((afterPromo / 1.12m) * VatRate, 2)
                : 0m;

            _subTotalValueLabel.Text = $"₱{subTotal:0.00}";
            _discountValueLabel.Text = $"₱{_controller.TotalDiscount:0.00}";
            _vatValueLabel.Text = $"₱{vatAmount:0.00}";
            _pwdSeniorValueLabel.Text = $"₱{_controller.PwdSeniorDiscountAmount:0.00}";
            _totalValueLabel.Text = $"₱{_controller.TotalAmount:0.00}";
            _logSaleBtn.Enabled = _controller.HasItems;
        }

        // =====================================================================
        // DRINKS GRID
        // =====================================================================

        private void BuildCategoryOptions()
        {
            if (_cachedCategories is null) return;

            _categoryCombo.Items.Clear();
            _categoryCombo.Items.Add(AllCategoriesOption);
            foreach (var category in _cachedCategories)
                _categoryCombo.Items.Add(category);
            _categoryCombo.SelectedIndex = 0;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);

        private const int WM_SETREDRAW = 0x000B;

        private static void SuspendRedraw(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, false, 0);
        }

        private static void ResumeRedraw(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, true, 0);
            control.Refresh();
        }

        private void RefreshProductsPanel()
        {
            var signature = $"{_selectedCategory}|{_productSearchTxtBx.Text}";
            if (signature == _lastProductFilterSignature && _productsGrid.Controls.Count > 0)
                return;

            _lastProductFilterSignature = signature;

            _productsGrid.SuspendLayout();
            SuspendRedraw(_productsGrid);
            try
            {
                _productsGrid.Controls.Clear();
                _productsGrid.RowStyles.Clear();
                _productsGrid.RowCount = 0;

                if (_controller is null)
                {
                    SyncProductsGridWidth();
                    _productsGrid.Height = _productsScrollHost.ClientSize.Height;
                    return;
                }

                var products = _controller.GetProducts(_selectedCategory, _productSearchTxtBx.Text);

                // Fixed-height product rows plus a Percent(100) spacer row at the end. The
                // spacer absorbs leftover vertical space; without it, TableLayoutPanel
                // expands the LAST row to fill the panel, which stretches partial rows.
                var productRows = (products.Count + DrinksColumnCount - 1) / DrinksColumnCount;

                for (int r = 0; r < productRows; r++)
                    _productsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, DrinksRowHeight));
                _productsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                _productsGrid.RowCount = productRows + 1;

                for (int i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    var card = new ProductCardControl(product)
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(4),
                    };
                    card.CardClicked += (s, e) => OpenCustomizerForNewItem(product);
                    card.SizeQuickAdded += (s, size) => QuickAddToCart(product, size);

                    var row = i / DrinksColumnCount;
                    var col = i % DrinksColumnCount;
                    _productsGrid.Controls.Add(card, col, row);
                }

                // Explicit grid size — the wrapper's AutoScroll compares this against its
                // own client height and shows a scrollbar when the grid is taller.
                SyncProductsGridWidth();
                var gridHeight = productRows * DrinksRowHeight
                    + _productsGrid.Padding.Top + _productsGrid.Padding.Bottom;
                _productsGrid.Height = Math.Max(gridHeight, _productsScrollHost.ClientSize.Height);
            }
            finally
            {
                ResumeRedraw(_productsGrid);
                _productsGrid.ResumeLayout(performLayout: true);
                _productsScrollHost.PerformLayout();
            }
        }

        // =====================================================================
        // CART
        // =====================================================================

        private void QuickAddToCart(Product product, ProductSize size)
        {
            if (_controller is null)
            {
                ShowCartStatus("Pick a customer on the left first.");
                return;
            }

            try
            {
                _controller.AddItemToCart(product, size, 1, Enumerable.Empty<ProductModifier>());
                RefreshCartLines();
                RefreshSummary();
            }
            catch (Exception ex)
            {
                Toast.Show(this, ex.Message, ToastKind.Error);
            }
        }

        private void OpenCustomizerForNewItem(Product product)
        {
            if (_controller is null)
            {
                ShowCartStatus("Pick a customer on the left first.");
                return;
            }

            using var form = new FrmItemCustomizer(product, _controller.GetModifiers());

            if (form.ShowDialog(FindForm()) == DialogResult.OK)
            {
                try
                {
                    _controller.AddItemToCart(product, form.SelectedSize, form.Quantity, form.SelectedModifiers);
                    RefreshCartLines();
                    RefreshSummary();
                }
                catch (Exception ex)
                {
                    Toast.Show(this, ex.Message, ToastKind.Error);
                }
            }
        }

        private void RefreshCartLines()
        {
            _cartLinesPanel.Controls.Clear();

            if (_controller is null || _controller.CartItems.Count == 0)
            {
                _cartLinesPanel.Controls.Add(_cartEmptyLabel);
                return;
            }

            var lineWidth = Math.Max(200,
                _cartLinesPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);

            foreach (var item in _controller.CartItems)
            {
                var line = new CartLineControl(item) { Width = lineWidth };
                line.QuantityIncreased += (s, e) => ChangeQuantity(item, +1);
                line.QuantityDecreased += (s, e) => ChangeQuantity(item, -1);
                line.RemoveClicked += (s, e) => RemoveFromCart(item);
                _cartLinesPanel.Controls.Add(line);
            }
        }

        private void RemoveFromCart(OrderItem item)
        {
            if (_controller is null) return;

            _controller.RemoveItem(item);
            RefreshCartLines();
            RefreshSummary();
        }

        private void ChangeQuantity(OrderItem item, int delta)
        {
            if (_controller is null || item.Product is null) return;

            var newQuantity = item.Quantity + delta;
            if (newQuantity <= 0)
            {
                RemoveFromCart(item);
                return;
            }

            var size = item.Product.ProductSizes.First(s => s.ProductSizeId == item.ProductSizeId);
            var modifiers = _controller.GetModifiers()
                .Where(m => item.OrderItemModifiers.Any(om => om.ProductModifierId == m.ProductModifierId));

            try
            {
                _controller.UpdateCartItem(item, size, newQuantity, modifiers);
                RefreshCartLines();
                RefreshSummary();
            }
            catch (Exception ex)
            {
                Toast.Show(this, ex.Message, ToastKind.Error);
            }
        }

        private void ShowCartStatus(string message) => _cartStatusLabel.Text = message;

        private async void CompleteSale()
        {
            if (_controller is null) return;

            _logSaleBtn.Enabled = false;
            _cartStatusLabel.Text = "Saving order...";

            (bool Success, string? ErrorMessage) result;
            try
            {
                result = await _controller.CompleteSaleAsync();
            }
            catch (Exception ex)
            {
                // Anything that lands here is a genuinely unexpected failure (network
                // unreachable, a programmer-guard tripping) — not a normal server
                // rejection, which CompleteSaleAsync now reports without throwing.
                _cartStatusLabel.Text = string.Empty;
                Toast.Show(this, ex.Message, ToastKind.Error);
                _logSaleBtn.Enabled = _controller?.HasItems ?? false;
                return;
            }

            _cartStatusLabel.Text = string.Empty;

            if (!result.Success)
            {
                Toast.Show(this, result.ErrorMessage ?? "Could not complete the sale.", ToastKind.Error);
                _logSaleBtn.Enabled = _controller.HasItems;
                RefreshSummary();
                RefreshPromoButton();
                return;
            }

            MessageBox.Show(FindForm(),
                $"Sale completed. {_controller.CompletedPointsEarned} points awarded to {_controller.CompletedCustomerName}.",
                "Sale Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _controller = null;
            _customerProfilePanel.Visible = false;
            _customerSearchErrorLabel.Text = string.Empty;
            _customerSearchTxtBx.Text = string.Empty;
            _cartStatusLabel.Text = string.Empty;
            _blockOverlayPanel.Visible = true;

            _pwdSeniorChk.Checked = false;

            _lastProductFilterSignature = string.Empty;
            RefreshProductsPanel();
            RefreshCartLines();
            RefreshSummary();
        }
    }
}