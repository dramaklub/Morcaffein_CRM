using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Services
{
    /// <summary>
    /// Controller for the Transactions/POS tab (MVC). One instance represents a single
    /// in-progress sale for one customer. Owns every business rule for the cart/promo
    /// flow — cart-item pricing, discount calculation, promo eligibility, and the
    /// stacked PWD/Senior discount (standard PH method).
    ///
    /// Cart is CLIENT-SIDE: the in-progress Order is built and mutated in memory here.
    /// Only CompleteSaleAsync hits the network — it POSTs the whole cart to the API,
    /// which recomputes all totals server-side and persists everything.
    /// </summary>
    public class TransactionService
    {
        private readonly OrderApi _orderApi;
        private readonly IReadOnlyList<Product> _allProducts;
        private readonly IReadOnlyList<string> _categories;
        private readonly IReadOnlyList<ProductModifier> _modifiers;
        private readonly IReadOnlyList<Campaign> _activeCampaigns;

        public Order Order { get; private set; }
        public Customer Customer { get; private set; }

        public IReadOnlyList<string> Categories => _categories;

        public decimal SubTotal => Order.SubTotal;
        public decimal TotalDiscount => Order.TotalDiscount;
        public decimal PwdSeniorDiscountAmount => Order.PwdSeniorDiscountAmount;
        public decimal TotalAmount => Order.TotalAmount;
        public bool HasItems => Order.OrderItems.Count > 0;
        public bool PwdSeniorApplied => Order.HasPwdSeniorDiscount;
        public IReadOnlyList<OrderItem> CartItems => Order.OrderItems.ToList();

        public int? CompletedOrderId { get; private set; }
        public int CompletedPointsEarned { get; private set; }
        public string CompletedCustomerName { get; set; } = string.Empty;

        /// <summary>
        /// The full server response from the completed sale. Populated on
        /// successful CompleteSaleAsync. Used by the receipt modal — it carries
        /// the line items, discount breakdown, and post-sale points balance.
        /// Null before the first sale or after a failed sale.
        /// </summary>
        public OrderResponseDto? CompletedOrder { get; private set; }

        public TransactionService(
            IReadOnlyList<Product> allProducts,
            IReadOnlyList<string> categories,
            IReadOnlyList<ProductModifier> modifiers,
            IReadOnlyList<Campaign> activeCampaigns,
            Customer customer,
            int createdByEmployeeId)
        {
            _orderApi = new OrderApi();
            _allProducts = allProducts;
            _categories = categories;
            _modifiers = modifiers;
            _activeCampaigns = activeCampaigns;
            Customer = customer;

            Order = new Order
            {
                CustomerId = customer.CustomerId,
                CreatedBy = createdByEmployeeId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
            };
        }

        public void SwitchCustomer(Customer newCustomer)
        {
            Order.CustomerId = newCustomer.CustomerId;
            Customer = newCustomer;
        }

        public IReadOnlyList<Product> GetProducts(string category, string searchText)
        {
            IEnumerable<Product> products = string.IsNullOrEmpty(category)
                ? _allProducts
                : _allProducts.Where(p => p.ProductCategory.Equals(category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(searchText))
                products = products.Where(p => p.ProductName.Contains(searchText, StringComparison.OrdinalIgnoreCase));

            return products.ToList();
        }

        public IReadOnlyList<ProductModifier> GetModifiers() => _modifiers;

        public void AddItemToCart(Product product, ProductSize size, int quantity, IEnumerable<ProductModifier> modifiers)
        {
            EnsurePending();
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            var existing = Order.OrderItems.FirstOrDefault(i => i.ProductSizeId == size.ProductSizeId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                RecalculateItemPricing(existing);
            }
            else
            {
                var item = new OrderItem
                {
                    ProductId = product.ProductId,
                    ProductSizeId = size.ProductSizeId,
                    SizeNameSnapshot = size.SizeName,
                    BaseSizePrice = size.Price,
                    Quantity = quantity,
                    Product = product,
                    ProductSize = size,
                };

                foreach (var modifier in modifiers)
                {
                    item.OrderItemModifiers.Add(new OrderItemModifier
                    {
                        ProductModifierId = modifier.ProductModifierId,
                        ProductModifierNameSnapshot = modifier.Name,
                        UnitPrice = modifier.Price,
                        ProductModifier = modifier,
                    });
                }

                RecalculateItemPricing(item);
                Order.OrderItems.Add(item);
            }

            RecalculateOrderTotals();
        }

        public void UpdateCartItem(OrderItem item, ProductSize newSize, int newQuantity, IEnumerable<ProductModifier> newModifiers)
        {
            EnsurePending();
            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            if (item.ProductSizeId == newSize.ProductSizeId)
            {
                item.Quantity = newQuantity;
                item.OrderItemModifiers.Clear();
                foreach (var modifier in newModifiers)
                {
                    item.OrderItemModifiers.Add(new OrderItemModifier
                    {
                        ProductModifierId = modifier.ProductModifierId,
                        ProductModifierNameSnapshot = modifier.Name,
                        UnitPrice = modifier.Price,
                        ProductModifier = modifier,
                    });
                }
                RecalculateItemPricing(item);
                RecalculateOrderTotals();
            }
            else
            {
                Order.OrderItems.Remove(item);
                AddItemToCart(item.Product!, newSize, newQuantity, newModifiers);
            }
        }

        public void RemoveItem(OrderItem item)
        {
            Order.OrderItems.Remove(item);
            RecalculateOrderTotals();
        }

        public IReadOnlyList<Campaign> GetEligiblePromos() =>
            _activeCampaigns.Where(c => CanRedeem(c, Customer, Order)).ToList();

        public void ApplyPromo(Campaign campaign)
        {
            EnsurePending();

            // Only one promo at a time — clear any existing before adding the new one.
            Order.CampaignRedeems.Clear();

            var discount = ComputeDiscountFor(campaign);
            Order.CampaignRedeems.Add(new CampaignRedeem
            {
                CampaignId = campaign.CampaignId,
                DiscountUsed = discount,
                RedeemDate = DateTime.UtcNow,
                Campaign = campaign,
            });
            RecalculateOrderTotals();
        }

        public void RemovePromo(Campaign campaign)
        {
            var redeem = Order.CampaignRedeems.FirstOrDefault(r => r.CampaignId == campaign.CampaignId);
            if (redeem is null) return;

            Order.CampaignRedeems.Remove(redeem);
            RecalculateOrderTotals();
        }

        public void ClearPromos()
        {
            Order.CampaignRedeems.Clear();
            RecalculateOrderTotals();
        }

        /// <summary>
        /// Toggles the PWD/Senior discount. Stacks on top of any applied promo using the
        /// standard PH method: 20% off the VAT-exempt amount of the post-promo net.
        /// Recalculates totals immediately so the Payables panel updates on the spot.
        /// </summary>
        public void SetPwdSeniorDiscount(bool applied)
        {
            EnsurePending();
            Order.HasPwdSeniorDiscount = applied;
            RecalculateOrderTotals();
        }

        /// <summary>
        /// Submits the cart. A server-side rejection (stale/invalid promo, a size that
        /// no longer exists, etc.) is a normal, expected outcome here — not an exception.
        /// On failure, any applied promo is cleared (it's what the server just said is
        /// invalid for this cart) so the UI doesn't keep advertising a discount that
        /// won't actually apply on retry.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> CompleteSaleAsync()
        {
            EnsurePending();
            if (Order.OrderItems.Count == 0)
                throw new InvalidOperationException("Order cannot be empty.");

            var request = new CompleteOrderRequest
            {
                CustomerId = Order.CustomerId,
                CreatedBy = Order.CreatedBy,
                CampaignId = Order.CampaignRedeems.FirstOrDefault()?.CampaignId,
                HasPwdSeniorDiscount = Order.HasPwdSeniorDiscount,
                BranchId = ResolveCurrentBranchId(),
                Lines = Order.OrderItems.Select(i => new OrderLineDto
                {
                    ProductId = i.ProductId,
                    ProductSizeId = i.ProductSizeId,
                    Quantity = i.Quantity,
                    ModifierIds = i.OrderItemModifiers.Select(m => m.ProductModifierId).ToList(),
                }).ToList(),
            };

            var result = await _orderApi.CompleteOrderAsync(request);

            if (!result.Success)
            {
                if (Order.CampaignRedeems.Count > 0)
                {
                    Order.CampaignRedeems.Clear();
                    RecalculateOrderTotals();
                }
                return (false, result.ErrorMessage);
            }

            var response = result.Order!;

            CompletedOrder = response;
            CompletedOrderId = response.OrderId;
            CompletedPointsEarned = response.PointsEarned;
            CompletedCustomerName = response.CustomerName;

            Order.Status = "Completed";
            Order.OrderId = response.OrderId;
            Order.SubTotal = response.SubTotal;
            Order.TotalDiscount = response.TotalDiscount;
            Order.TotalAmount = response.TotalAmount;
            Order.PointsEarned = response.PointsEarned;

            return (true, null);
        }

        // =====================================================================
        // BUSINESS RULES
        // =====================================================================

        private void EnsurePending()
        {
            if (!Order.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Order cannot be modified in '{Order.Status}' state.");
        }

        private static void RecalculateItemPricing(OrderItem item)
        {
            item.UnitPrice = item.BaseSizePrice + item.OrderItemModifiers.Sum(m => m.UnitPrice);
            item.SubTotal = item.Quantity * item.UnitPrice;
        }

        private void RecalculateOrderTotals()
        {
            Order.SubTotal = Order.OrderItems.Sum(i => i.SubTotal);

            // --- Promo discount (single promo, applied to SubTotal) ---
            decimal promoDiscount = 0m;
            foreach (var redeem in Order.CampaignRedeems)
            {
                var campaign = redeem.Campaign ?? _activeCampaigns.FirstOrDefault(c => c.CampaignId == redeem.CampaignId);
                var discount = campaign != null ? ComputeDiscountFor(campaign) : redeem.DiscountUsed;
                redeem.DiscountUsed = discount;
                promoDiscount += discount;
            }
            Order.TotalDiscount = promoDiscount;

            // --- PWD/Senior (stacked, standard PH method) ---
            var afterPromo = Math.Max(0m, Order.SubTotal - Order.TotalDiscount);

            if (Order.HasPwdSeniorDiscount)
            {
                var netOfVat = afterPromo / 1.12m;
                var pwdDiscount = decimal.Round(netOfVat * 0.20m, 2);

                Order.PwdSeniorDiscountAmount = pwdDiscount;
                Order.TotalAmount = decimal.Round(netOfVat - pwdDiscount, 2);
            }
            else
            {
                Order.PwdSeniorDiscountAmount = 0m;
                Order.TotalAmount = afterPromo;
            }
        }

        private static bool IsCurrentlyActive(Campaign campaign)
        {
            var now = DateTime.UtcNow;
            return campaign.IsActive && now >= campaign.StartDate && now <= campaign.EndDate;
        }

        private static bool CanRedeem(Campaign campaign, Customer customer, Order order)
        {
            if (!IsCurrentlyActive(campaign)) return false;

            if (campaign.MinPurchaseAmount.HasValue && order.SubTotal < campaign.MinPurchaseAmount.Value)
                return false;

            if (!campaign.TargetTier.Equals(PromoCategories.AllTiers, StringComparison.OrdinalIgnoreCase) &&
    !PointsToTier(customer.PointsBalance).Equals(campaign.TargetTier, StringComparison.OrdinalIgnoreCase))
                return false;

            if (campaign.PromoCategory.Equals(PromoCategories.Birthday, StringComparison.OrdinalIgnoreCase) && !IsBirthdayToday(customer))
                return false;

            if (campaign.PromoCategory.Equals(PromoCategories.Inactive, StringComparison.OrdinalIgnoreCase) && !IsInactive(customer))
                return false;

            var previousRedemptions = campaign.CampaignRedeems.Count(r => r.Order != null && r.Order.CustomerId == customer.CustomerId);
            return previousRedemptions < campaign.MaxRedemptionsPerCustomer;
        }

        private static string PointsToTier(int pointsBalance) => pointsBalance switch
        {
            >= 500 => "Gold",
            >= 200 => "Silver",
            _ => "Regular",
        };

        private static bool IsBirthdayToday(Customer customer) =>
            customer.Birthday?.Month == DateTime.UtcNow.Month && customer.Birthday?.Day == DateTime.UtcNow.Day;

        private static bool IsInactive(Customer customer) => (DateTime.UtcNow - customer.LastVisitDate).Days > ActivityStatus.InactiveThresholdDays;

        /// <summary>
        /// Routes to the right discount calc for the campaign's DiscountType. B1T1 needs
        /// the actual cart contents (it discounts one unit of a specific reward product),
        /// so it can't use the generic percentage/fixed-amount-of-subtotal math.
        /// </summary>
        private decimal ComputeDiscountFor(Campaign campaign) =>
            IsB1T1(campaign.DiscountType)
                ? CalculateB1T1Discount(campaign, Order)
                : CalculatePromoDiscount(campaign, Order.SubTotal);

        private static bool IsB1T1(string discountType) =>
            discountType.Equals("B1T1", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Mirrors the server's B1T1 redemption check in OrderController.CompleteOrder:
        /// both the required and reward products must be in the cart, and exactly one
        /// unit of the reward product's base size price is discounted (add-on modifiers
        /// on that cup are still paid for — only the base cup is free).
        /// </summary>
        private static decimal CalculateB1T1Discount(Campaign campaign, Order order)
        {
            if (!campaign.RequiredProductId.HasValue || !campaign.RewardProductId.HasValue)
                return 0m;

            var hasRequired = order.OrderItems.Any(i => i.ProductId == campaign.RequiredProductId.Value);
            if (!hasRequired) return 0m;

            var rewardItem = order.OrderItems.FirstOrDefault(i => i.ProductId == campaign.RewardProductId.Value);
            return rewardItem?.BaseSizePrice ?? 0m;
        }

        /// <summary>
        /// The branch this sale belongs to. For Baristas it's their assigned branch;
        /// for Managers/Admins it's whatever they picked in the topbar selector.
        /// Falls back to 1 if neither is set — shouldn't happen in practice, but the
        /// DB requires a non-null BranchId on Orders.
        /// </summary>
        private static int ResolveCurrentBranchId()
        {
            var session = Session.EmployeeSession.Current;
            return session?.CurrentBranchId
                ?? session?.Employee.BranchId
                ?? 1;
        }

        private static decimal CalculatePromoDiscount(Campaign campaign, decimal orderTotal)
        {
            if (orderTotal <= 0) return 0m;
            if (campaign.MinPurchaseAmount.HasValue && orderTotal < campaign.MinPurchaseAmount.Value) return 0m;

            decimal discount;
            if (campaign.DiscountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
            {
                discount = orderTotal * (campaign.DiscountValue / 100m);
                if (campaign.MaxDiscountAmount.HasValue && discount > campaign.MaxDiscountAmount.Value)
                    discount = campaign.MaxDiscountAmount.Value;
            }
            else
            {
                discount = campaign.DiscountValue;
            }

            return discount > orderTotal ? orderTotal : discount;
        }
    }
}