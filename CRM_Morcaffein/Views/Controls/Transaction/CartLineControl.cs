using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// One row in the Order box: "Drink Name (Size) +Mods … ₱SubTotal [- 2x +] ✕" — a compact
    /// inline quantity stepper instead of a data-grid row, matching a modern POS's order list.
    ///
    /// The name label wraps to two lines when needed, so "Vanilla Matcha (Cold)" no longer
    /// clips to "Vanilla Matc…". Row height is 60px (up from 44) to fit the second line.
    ///
    /// Absolute-positioned children recomputed on every OnResize. Deliberately NOT Dock-based
    /// and NOT TableLayoutPanel — the same reasoning as the previous version.
    ///
    /// The ✕ is a Label, not a Button — FlatStyle buttons enforce a minimum width larger
    /// than 28px, which would overlap the + stepper.
    /// </summary>
    public class CartLineControl : Panel
    {
        public OrderItem Item { get; }

        public event EventHandler? QuantityIncreased;
        public event EventHandler? QuantityDecreased;
        public event EventHandler? RemoveClicked;

        private const int RowHeight = 60;             // was 44 — taller for two-line names
        private const int StepperButtonSize = 30;
        private const int QtyLabelWidth = 34;
        private const int StepperClusterWidth = StepperButtonSize + QtyLabelWidth + StepperButtonSize; // 94
        private const int GapBeforeRemove = 10;
        private const int RemoveWidth = 28;
        private const int PriceWidth = 76;
        private const int RightClusterWidth = StepperClusterWidth + GapBeforeRemove + RemoveWidth;     // 132

        private readonly Label _nameLabel = null!;
        private readonly Label _priceLabel = null!;
        private readonly Panel _rightCluster = null!;
        private readonly FlowLayoutPanel _stepper = null!;
        private readonly Label _removeLabel = null!;

        public CartLineControl(OrderItem item)
        {
            Item = item;

            Height = RowHeight;
            Margin = new Padding(0, 0, 0, 3);
            BackColor = BrandPalette.SurfacePrimary;

            var modifiersSuffix = item.OrderItemModifiers.Count == 0
                ? string.Empty
                : $"  +{string.Join(", ", item.OrderItemModifiers.Select(m => m.ProductModifierNameSnapshot))}";

            _nameLabel = new Label
            {
                Text = $"{item.Product?.ProductName ?? "—"} ({item.SizeNameSnapshot}){modifiersSuffix}",

                // Now wraps to two lines. AutoEllipsis is removed — that was the
                // setting forcing "Vanilla Matc..." on a single line. The row is
                // 60px tall and the label is vertically centred, so two lines sit
                // comfortably inside.
                AutoSize = false,
                Height = RowHeight - 4,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _priceLabel = new Label
            {
                Text = $"₱{item.SubTotal:0.00}",
                AutoSize = false,
                Width = PriceWidth,
                Height = RowHeight,
                TextAlign = ContentAlignment.MiddleRight,
                Font = BrandPalette.BrandFontBold(9.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _rightCluster = new Panel
            {
                Width = RightClusterWidth,
                Height = RowHeight,
                BackColor = BrandPalette.SurfacePrimary,
            };

            _stepper = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Width = StepperClusterWidth,
                Height = RowHeight,
                Padding = new Padding(0, (RowHeight - StepperButtonSize) / 2, 0, 0),
            };

            var minusBtn = MakeStepperButton("-");
            minusBtn.Click += (s, e) => QuantityDecreased?.Invoke(this, EventArgs.Empty);

            var qtyLabel = new Label
            {
                Text = $"{item.Quantity}x",
                Width = QtyLabelWidth,
                Height = StepperButtonSize,
                Margin = new Padding(0),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontBold(10f),
                ForeColor = BrandPalette.NeutralText,
            };

            var plusBtn = MakeStepperButton("+");
            plusBtn.Click += (s, e) => QuantityIncreased?.Invoke(this, EventArgs.Empty);

            _stepper.Controls.Add(minusBtn);
            _stepper.Controls.Add(qtyLabel);
            _stepper.Controls.Add(plusBtn);

            _removeLabel = new Label
            {
                Text = "✕",
                Width = RemoveWidth,
                Height = RowHeight,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(12f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = BrandPalette.SurfacePrimary,
                Cursor = Cursors.Hand,
            };
            _removeLabel.Click += (s, e) => RemoveClicked?.Invoke(this, EventArgs.Empty);

            Controls.Add(_nameLabel);
            Controls.Add(_priceLabel);
            Controls.Add(_rightCluster);
            _rightCluster.Controls.Add(_stepper);
            _rightCluster.Controls.Add(_removeLabel);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            if (_nameLabel is null || _priceLabel is null || _rightCluster is null ||
                _stepper is null || _removeLabel is null)
                return;

            var width = ClientSize.Width;
            if (width <= 0)
                return;

            _rightCluster.Location = new Point(width - RightClusterWidth, 0);
            _removeLabel.Location = new Point(RightClusterWidth - RemoveWidth, 0);
            _stepper.Location = new Point(0, 0);
            _priceLabel.Location = new Point(_rightCluster.Left - PriceWidth, 0);

            // Name label stretches from the left edge to just before the price
            // label, with 4px of clearance. Now that it wraps, the height matches
            // the row so two lines fit centred.
            var nameWidth = Math.Max(0, _priceLabel.Left - 6);
            _nameLabel.Location = new Point(2, 0);
            _nameLabel.Width = nameWidth;
            _nameLabel.Height = RowHeight;
        }

        private static Button MakeStepperButton(string text) => new()
        {
            Text = text,
            Width = StepperButtonSize,
            Height = StepperButtonSize,
            Margin = new Padding(0),
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandPalette.ButtonSecondaryFill,
            ForeColor = BrandPalette.ButtonSecondaryText,
            Font = BrandPalette.BrandFontBold(10f),
            Cursor = Cursors.Hand,
        };
    }
}