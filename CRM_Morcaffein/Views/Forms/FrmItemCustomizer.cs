using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views
{
    public enum CustomizerMode
    {
        NewItem,
        EditExisting,
    }

    /// <summary>
    /// Code-only modal (no paired Designer.cs — everything is built in the constructor
    /// below, same convention as Views/Controls/ProductCardControl.cs). Drop this file in
    /// alongside the other Forms; it needs no designer surface to work.
    ///
    /// Opened either from a drink card click (Mode == NewItem) or from clicking an existing
    /// cart line to edit it (Mode == EditExisting). Caller reads SelectedSize / Quantity /
    /// SelectedModifiers after ShowDialog() returns OK.
    ///
    /// MVC note: no ViewModel — this modal doesn't talk to any Model/Service at all (it's
    /// pure local price-preview arithmetic on the size/modifiers/quantity picked so far), so
    /// it just keeps its own plain fields instead of wrapping them in a bindable object.
    /// </summary>
    public class FrmItemCustomizer : Form
    {
        public Product Product { get; }
        public CustomizerMode Mode { get; }

        private readonly List<ProductSize> _sizes;
        private readonly Dictionary<ProductModifier, CheckBox> _modifierChecks = new();

        public ProductSize SelectedSize { get; private set; }
        public int Quantity { get; private set; } = 1;
        public IEnumerable<ProductModifier> SelectedModifiers =>
            _modifierChecks.Where(kv => kv.Value.Checked).Select(kv => kv.Key);

        private decimal CalculatedUnitPrice => SelectedSize.Price + SelectedModifiers.Sum(m => m.Price);
        private decimal CalculatedLineTotal => CalculatedUnitPrice * Quantity;

        private readonly Panel _sizePanel = new() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 4, 0, 10) };
        private readonly Panel _modifiersPanel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 320,
            Width = 900,// scrollable fixed height
            AutoScroll = true,
            Padding = new Padding(0, 4, 0, 10)
        };
        private readonly Label _priceLabel = new() { Dock = DockStyle.Top, Height = 34, TextAlign = ContentAlignment.MiddleRight };
        private readonly Label _qtyValueLabel = new() { Width = 36, TextAlign = ContentAlignment.MiddleCenter, Font = BrandPalette.BrandFontBold(11f) };
        private readonly Button _confirmBtn;

        /// <param name="editingItem">Pass the existing OrderItem to pre-populate for editing; null for a brand new line.</param>
        public FrmItemCustomizer(Product product, IReadOnlyList<ProductModifier> allModifiers, OrderItem? editingItem = null)
        {
            Product = product;
            _sizes = product.ProductSizes.ToList();
            if (_sizes.Count == 0)
                throw new InvalidOperationException($"{product.ProductName} has no sizes configured.");

            Mode = editingItem != null ? CustomizerMode.EditExisting : CustomizerMode.NewItem;
            SelectedSize = editingItem != null
                ? _sizes.FirstOrDefault(s => s.ProductSizeId == editingItem.ProductSizeId) ?? _sizes[0]
                : _sizes[0];
            Quantity = editingItem?.Quantity ?? 1;

            Text = Mode == CustomizerMode.NewItem ? "Customize Item" : "Edit Item";
            Font = BrandPalette.BrandFontRegular(9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(720, 720);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(18);

            var productNameLabel = new Label
            {
                Text = product.ProductName,
                Dock = DockStyle.Top,
                Height = 30,
                Font = BrandPalette.BrandFontBold(13f),
                ForeColor = BrandPalette.NeutralText,
            };

            var sizeHeader = SectionHeader("Size");
            BuildSizeOptions();

            var modifierHeader = SectionHeader("Modifiers");
            BuildModifierOptions(allModifiers, editingItem);

            var qtyHeader = SectionHeader("Quantity");
            var qtyRow = BuildQuantityStepper();

            _confirmBtn = new Button
            {
                Text = Mode == CustomizerMode.NewItem ? "Add to Cart" : "Update Item",
                Dock = DockStyle.Bottom,
                Height = 42,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(10.5f),
            };
            _confirmBtn.FlatAppearance.BorderSize = 0;
            _confirmBtn.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            var cancelBtn = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Bottom,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Margin = new Padding(0, 0, 0, 6),
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // Added bottom-up because these are Dock.Bottom/Top mixed; Controls order below
            // keeps the visual stack: name -> size -> modifiers -> qty -> price -> buttons.
            Controls.Add(_priceLabel);
            Controls.Add(qtyRow);
            Controls.Add(qtyHeader);
            Controls.Add(_modifiersPanel);
            Controls.Add(modifierHeader);
            Controls.Add(_sizePanel);
            Controls.Add(sizeHeader);
            Controls.Add(productNameLabel);
            Controls.Add(cancelBtn);
            Controls.Add(_confirmBtn);

            RefreshPriceLabel();
        }

        private static Label SectionHeader(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 22,
            Font = BrandPalette.BrandFontBold(9.5f),
            ForeColor = BrandPalette.NeutralTextMuted,
        };

        private void BuildSizeOptions()
        {
            foreach (var size in _sizes)
            {
                var radio = new RadioButton
                {
                    Text = $"{size.SizeName} — ₱{size.Price:0.##}",
                    Dock = DockStyle.Top,
                    Height = 26,
                    Checked = size.ProductSizeId == SelectedSize.ProductSizeId,
                    Font = BrandPalette.BrandFontRegular(9.5f),
                };
                radio.CheckedChanged += (s, e) =>
                {
                    if (radio.Checked)
                    {
                        SelectedSize = size;
                        RefreshPriceLabel();
                    }
                };
                _sizePanel.Controls.Add(radio);
            }
            // Panel.Controls.Add with Dock=Top stacks in reverse visual order (last added is
            // topmost) — reverse the child order so sizes read top-to-bottom as seeded.
            var children = _sizePanel.Controls.Cast<Control>().ToList();
            _sizePanel.Controls.Clear();
            for (int i = children.Count - 1; i >= 0; i--)
                _sizePanel.Controls.Add(children[i]);
        }

        private void BuildModifierOptions(IReadOnlyList<ProductModifier> allModifiers, OrderItem? editingItem)
        {
            foreach (var modifier in allModifiers)
            {
                var isPreSelected = editingItem != null &&
                    editingItem.OrderItemModifiers.Any(m => m.ProductModifierId == modifier.ProductModifierId);

                var check = new CheckBox
                {
                    Text = $"{modifier.Name} (+₱{modifier.Price:0.##})",
                    Dock = DockStyle.Top,
                    Height = 26,
                    Checked = isPreSelected,
                    Font = BrandPalette.BrandFontRegular(9.5f),
                };
                check.CheckedChanged += (s, e) => RefreshPriceLabel();
                _modifierChecks[modifier] = check;
                _modifiersPanel.Controls.Add(check);
            }
            var children = _modifiersPanel.Controls.Cast<Control>().ToList();
            _modifiersPanel.Controls.Clear();
            for (int i = children.Count - 1; i >= 0; i--)
                _modifiersPanel.Controls.Add(children[i]);
        }

        private Panel BuildQuantityStepper()
        {
            var row = new Panel { Dock = DockStyle.Top, Height = 40 };

            var minusBtn = MakeStepperButton("-");
            minusBtn.Left = 0;
            minusBtn.Click += (s, e) =>
            {
                Quantity = Math.Max(1, Quantity - 1);
                _qtyValueLabel.Text = Quantity.ToString();
                RefreshPriceLabel();
            };

            var plusBtn = MakeStepperButton("+");
            plusBtn.Left = minusBtn.Width + 44;
            plusBtn.Click += (s, e) =>
            {
                Quantity += 1;
                _qtyValueLabel.Text = Quantity.ToString();
                RefreshPriceLabel();
            };

            _qtyValueLabel.Left = minusBtn.Width + 4;
            _qtyValueLabel.Top = 4;
            _qtyValueLabel.Height = 32;
            _qtyValueLabel.Text = Quantity.ToString();

            row.Controls.Add(minusBtn);
            row.Controls.Add(_qtyValueLabel);
            row.Controls.Add(plusBtn);
            return row;
        }

        private static Button MakeStepperButton(string text) => new()
        {
            Text = text,
            Width = 32,
            Height = 32,
            Top = 4,
            FlatStyle = FlatStyle.Flat,
            BackColor = BrandPalette.ButtonSecondaryFill,
            ForeColor = BrandPalette.ButtonSecondaryText,
            Font = BrandPalette.BrandFontBold(12f),
        };

        private void RefreshPriceLabel()
        {
            _priceLabel.Text = $"Total: ₱{CalculatedLineTotal:0.00}";
            _priceLabel.Font = BrandPalette.BrandFontBold(13f);
            _priceLabel.ForeColor = BrandPalette.Brand;
        }
    }
}
