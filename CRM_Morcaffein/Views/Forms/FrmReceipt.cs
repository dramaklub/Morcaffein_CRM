using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// Read-only receipt shown after a sale completes. Renders from the
    /// OrderResponseDto returned by POST /orders plus the cashier-supplied
    /// amount paid (which the API doesn't know about).
    ///
    /// No print button — this demo has no receipt printer. The modal is
    /// purely a visual confirmation of the completed sale. Close returns
    /// the cashier to a cleared cart.
    /// </summary>
    public class FrmReceipt : Form
    {
        public FrmReceipt(OrderResponseDto order, decimal amountPaid)
        {
            Text = $"Receipt — Order #{order.OrderId}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(440, 760);
            BackColor = BrandPalette.SurfaceCard;
            Font = BrandPalette.BrandFontRegular(9.5f);
            Padding = new Padding(24);

            BuildLayout(order, amountPaid);
        }

        private void BuildLayout(OrderResponseDto order, decimal amountPaid)
        {
            var change = amountPaid - order.TotalAmount;

            // ---- Header ----
            var shopLabel = new Label
            {
                Text = "MORCAFFEIN",
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontBold(16f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
            };

            var branchLabel = new Label
            {
                Text = order.BranchName ?? "—",
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            var orderInfoLabel = new Label
            {
                Text = $"Order #{order.OrderId}   ·   {order.OrderDate.ToLocalTime():MMM d, yyyy HH:mm}",
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            var cashierLabel = new Label
            {
                Text = $"Cashier: {order.CreatedByName}",
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
            };

            var topDivider = MakeDivider();

            // ---- Customer line ----
            var customerLabel = new Label
            {
                Text = $"Customer: {order.CustomerName}",
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            // ---- Line items ----
            var itemsPanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 6),
            };

            var itemsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
            };

            foreach (var line in order.Lines)
            {
                itemsFlow.Controls.Add(BuildLineItemControl(line));
            }

            itemsPanel.Controls.Add(itemsFlow);

            // ---- Totals block ----
            var totals = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 12, 0, 12),   // a bit more vertical air
            };

            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            void AddTotalRow(string label, string value, Font? font = null, Color? color = null)
            {
                var row = totals.RowCount;
                totals.RowCount++;
                totals.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));

                totals.Controls.Add(new Label
                {
                    Text = label,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = font ?? BrandPalette.BrandFontRegular(9.5f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                    BackColor = Color.Transparent,
                }, 0, row);

                totals.Controls.Add(new Label
                {
                    Text = value,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    Font = font ?? BrandPalette.BrandFontRegular(9.5f),
                    ForeColor = color ?? BrandPalette.NeutralText,
                    BackColor = Color.Transparent,
                }, 1, row);
            }

            AddTotalRow("Subtotal", $"₱{order.SubTotal:0.00}");

            if (order.TotalDiscount > 0)
                AddTotalRow("Promo discount", $"−₱{order.TotalDiscount:0.00}");
            if (order.PwdSeniorDiscountAmount > 0)
                AddTotalRow("PWD/Senior discount", $"−₱{order.PwdSeniorDiscountAmount:0.00}");

            AddTotalRow("TOTAL", $"₱{order.TotalAmount:0.00}",
                BrandPalette.BrandFontBold(12f), BrandPalette.Brand);

            AddTotalRow("Amount paid", $"₱{amountPaid:0.00}");
            AddTotalRow("Change", $"₱{change:0.00}",
                BrandPalette.BrandFontBold(10f), BrandPalette.StatusPositiveText);

            // ---- Points block ----
            var pointsDivider = MakeDivider();

            var pointsEarnedLabel = new Label
            {
                Text = $"+ {order.PointsEarned} points earned",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontBold(10f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
            };

            var balanceLabel = new Label
            {
                Text = $"New balance: {order.NewPointsBalance:N0} pts",
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            var thanksLabel = new Label
            {
                Text = "Thank you! Come again.",
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            // ---- Scrollable content host (in case items overflow) ----
            var contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = BrandPalette.SurfaceCard,
            };

            // WinForms docks Top-docked children bottom-up — add the thanks line
            // first so it renders last (bottom).
            // Explicit spacer panels between sections. WinForms dock-stacking
            // doesn't honor Margin for visual gap — a docked panel's Margin is
            // absorbed by the dock engine. Real spacers are the reliable fix.
            static Panel Spacer(int height) => new()
            {
                Dock = DockStyle.Top,
                Height = height,
                BackColor = Color.Transparent,
            };

            // Docks top-to-bottom in reverse order of Add — add the bottom-most
            // control first. Every section gets a spacer above and below where
            // it makes sense, so the receipt breathes.
            contentHost.Controls.Add(thanksLabel);
            contentHost.Controls.Add(Spacer(32));       // above the thanks line
            contentHost.Controls.Add(balanceLabel);
            contentHost.Controls.Add(pointsEarnedLabel);
            contentHost.Controls.Add(Spacer(28));       // above the points block
            contentHost.Controls.Add(pointsDivider);
            contentHost.Controls.Add(Spacer(20));       // below the totals
            contentHost.Controls.Add(totals);
            contentHost.Controls.Add(Spacer(24));       // below the item list
            contentHost.Controls.Add(itemsPanel);
            contentHost.Controls.Add(Spacer(16));       // below the customer line
            contentHost.Controls.Add(customerLabel);
            contentHost.Controls.Add(Spacer(24));       // below the top divider
            contentHost.Controls.Add(topDivider);
            contentHost.Controls.Add(cashierLabel);
            contentHost.Controls.Add(orderInfoLabel);
            contentHost.Controls.Add(branchLabel);
            contentHost.Controls.Add(shopLabel);

            // ---- Close button ----
            var closeBtn = new Button
            {
                Text = "Close",
                Size = new Size(140, 40),
                Dock = DockStyle.Bottom,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(10f),
                Cursor = Cursors.Hand,
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            var btnBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = BrandPalette.SurfaceCard,
                Padding = new Padding(0, 12, 0, 4),
            };
            btnBar.Controls.Add(closeBtn);

            Controls.Add(contentHost);
            Controls.Add(btnBar);

            AcceptButton = closeBtn;
            CancelButton = closeBtn;
        }

        private static Panel MakeDivider() => new()
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = BrandPalette.Border,
            Margin = new Padding(0, 4, 0, 4),
        };

        /// <summary>
        /// One line item on the receipt: product name + size on the left,
        /// quantity x unit price and subtotal on the right, modifiers as a
        /// small indented sub-line below.
        /// </summary>
        private static Control BuildLineItemControl(OrderReceiptLineDto line)
        {
            var hasMods = line.Modifiers.Count > 0;
            var height = hasMods ? 46 : 28;

            var row = new Panel
            {
                Width = 380,
                Height = height,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 2),
            };

            var nameLabel = new Label
            {
                Text = $"{line.ProductName} ({line.SizeName})",
                Location = new Point(0, 0),
                Size = new Size(250, 22),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var qtyPriceLabel = new Label
            {
                Text = $"{line.Quantity} × ₱{line.UnitPrice:0.00}",
                Location = new Point(250, 0),
                Size = new Size(130, 22),
                TextAlign = ContentAlignment.MiddleRight,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            row.Controls.Add(qtyPriceLabel);
            row.Controls.Add(nameLabel);

            if (hasMods)
            {
                var modsText = string.Join(", ", line.Modifiers.Select(m => m.Name));
                var modsLabel = new Label
                {
                    Text = $"  + {modsText}",
                    Location = new Point(0, 22),
                    Size = new Size(380, 18),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = BrandPalette.BrandFontRegular(8f),
                    ForeColor = BrandPalette.NeutralTextMuted,
                    BackColor = Color.Transparent,
                };
                row.Controls.Add(modsLabel);
            }

            return row;
        }
    }
}