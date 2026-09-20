using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.api.Controllers
{
    [ApiController]
    [Route("api/tenant/{companyId:int}/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        public OrdersController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponseDto>> CompleteOrder(
            int companyId,
            CompleteOrderRequest request)
        {
            // ---- Request-level validation ----
            if (request.CustomerId <= 0)
                return BadRequest(new { message = "Customer is required." });
            if (request.CreatedBy <= 0)
                return BadRequest(new { message = "Created-by employee is required." });
            if (request.Lines is null || request.Lines.Count == 0)
                return BadRequest(new { message = "Order must have at least one line." });
            if (request.Lines.Any(l => l.Quantity <= 0))
                return BadRequest(new { message = "Line quantity must be greater than zero." });

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // ---- Verify customer and employee exist ----
            var customer = await tenant.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId);
            if (customer is null)
                return BadRequest(new { message = "Customer not found." });

            var employeeExists = await tenant.Employees
                .AnyAsync(e => e.EmployeeId == request.CreatedBy);
            if (!employeeExists)
                return BadRequest(new { message = "Employee not found." });

            // ---- Look up all products / sizes / modifiers referenced ----
            var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
            var sizeIds = request.Lines.Select(l => l.ProductSizeId).Distinct().ToList();
            var modifierIds = request.Lines.SelectMany(l => l.ModifierIds).Distinct().ToList();

            var products = await tenant.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            var sizes = await tenant.ProductSizes
                .Where(s => sizeIds.Contains(s.ProductSizeId))
                .ToListAsync();

            var modifiers = await tenant.ProductModifiers
                .Where(m => modifierIds.Contains(m.ProductModifierId))
                .ToListAsync();

            // ---- Build the Order entity, computing everything server-side ----
            var order = new Order
            {
                CustomerId = request.CustomerId,
                CreatedBy = request.CreatedBy,
                OrderDate = DateTime.UtcNow,
                Status = "Completed",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                HasPwdSeniorDiscount = request.HasPwdSeniorDiscount,
            };

            decimal subTotal = 0m;

            foreach (var line in request.Lines)
            {
                var product = products.FirstOrDefault(p => p.ProductId == line.ProductId);
                var size = sizes.FirstOrDefault(s => s.ProductSizeId == line.ProductSizeId);

                if (product is null)
                    return BadRequest(new { message = $"Product {line.ProductId} not found." });
                if (size is null)
                    return BadRequest(new { message = $"Product size {line.ProductSizeId} not found." });
                if (size.ProductId != product.ProductId)
                    return BadRequest(new { message = $"Size {size.ProductSizeId} does not belong to product {product.ProductId}." });

                var orderItem = new OrderItem
                {
                    ProductId = product.ProductId,
                    ProductSizeId = size.ProductSizeId,
                    SizeNameSnapshot = size.SizeName,
                    BaseSizePrice = size.Price,
                    Quantity = line.Quantity,
                };

                decimal lineUnitPrice = size.Price;

                foreach (var modId in line.ModifierIds)
                {
                    var modifier = modifiers.FirstOrDefault(m => m.ProductModifierId == modId);
                    if (modifier is null)
                        return BadRequest(new { message = $"Modifier {modId} not found." });

                    orderItem.OrderItemModifiers.Add(new OrderItemModifier
                    {
                        ProductModifierId = modifier.ProductModifierId,
                        ProductModifierNameSnapshot = modifier.Name,
                        UnitPrice = modifier.Price,
                    });

                    lineUnitPrice += modifier.Price;
                }

                orderItem.UnitPrice = lineUnitPrice;
                orderItem.SubTotal = lineUnitPrice * line.Quantity;

                order.OrderItems.Add(orderItem);
                subTotal += orderItem.SubTotal;
            }

            order.SubTotal = subTotal;

            // ---- Promo / B1T1 Validation & Calculation ----
            decimal promoDiscount = 0m;

            if (request.CampaignId.HasValue)
            {
                var campaign = await tenant.Campaigns
                    .FirstOrDefaultAsync(c => c.CampaignId == request.CampaignId.Value);

                if (campaign is null)
                    return BadRequest(new { message = $"Campaign {request.CampaignId.Value} not found." });
                if (!campaign.IsActive || DateTime.UtcNow < campaign.StartDate || DateTime.UtcNow > campaign.EndDate)
                    return BadRequest(new { message = "Campaign is not currently active." });

                // B1T1 Campaign Handling
                if (string.Equals(campaign.DiscountType, "B1T1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(campaign.DiscountType, "BuyOneGetOne", StringComparison.OrdinalIgnoreCase))
                {
                    // 1. Validate required purchase product exists in cart
                    if (campaign.RequiredProductId.HasValue &&
                        !request.Lines.Any(l => l.ProductId == campaign.RequiredProductId.Value))
                    {
                        return BadRequest(new { message = "Order does not contain the required item to qualify for this B1T1 promo." });
                    }

                    // 2. Validate free reward product exists in cart lines
                    var rewardLine = campaign.RewardProductId.HasValue
                        ? request.Lines.FirstOrDefault(l => l.ProductId == campaign.RewardProductId.Value)
                        : null;

                    if (rewardLine is null)
                    {
                        return BadRequest(new { message = "Order does not contain the required B1T1 free item." });
                    }

                    // 3. Discount exactly 1 unit of the reward item's base size price
                    var rewardSize = sizes.FirstOrDefault(s => s.ProductSizeId == rewardLine.ProductSizeId);
                    if (rewardSize is null)
                    {
                        return BadRequest(new { message = "Reward item size not found." });
                    }

                    // Single claim per order limit applied here (1 x Base Size Price)
                    promoDiscount = rewardSize.Price;
                }
                else
                {
                    // Percentage / Fixed Amount Cart Discounts
                    promoDiscount = CalculatePromoDiscount(campaign, order.SubTotal);
                }

                order.CampaignRedeems.Add(new CampaignRedeem
                {
                    CampaignId = campaign.CampaignId,
                    DiscountUsed = promoDiscount,
                    RedeemDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }

            order.TotalDiscount = promoDiscount;

            // ---- PWD/Senior (stacked, standard PH method) ----
            var afterPromo = Math.Max(0m, order.SubTotal - order.TotalDiscount);

            if (request.HasPwdSeniorDiscount)
            {
                var netOfVat = afterPromo / 1.12m;
                var pwdDiscount = decimal.Round(netOfVat * 0.20m, 2);

                order.PwdSeniorDiscountAmount = pwdDiscount;
                order.TotalAmount = decimal.Round(netOfVat - pwdDiscount, 2);
            }
            else
            {
                order.PwdSeniorDiscountAmount = 0m;
                order.TotalAmount = afterPromo;
            }

            // ---- Points: 1 per ₱10 net spend, on the FINAL total ----
            order.PointsEarned = (int)(order.TotalAmount / 10m);

            // ---- Save Order and Ledger ----
            tenant.Orders.Add(order);

            try
            {
                await tenant.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
            }

            // ---- Award points + write ledger row ----
            if (order.PointsEarned > 0)
            {
                customer.PointsBalance += order.PointsEarned;
                customer.LastVisitDate = DateTime.UtcNow;
                customer.UpdatedAt = DateTime.UtcNow;

                tenant.PointLedgers.Add(new PointLedger
                {
                    CustomerId = customer.CustomerId,
                    PointsChange = order.PointsEarned,
                    Type = "Earned",
                    Reason = $"Order #{order.OrderId} Spend",
                    OrderId = order.OrderId,
                    Timestamp = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });

                try
                {
                    await tenant.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    return Conflict(new { message = ex.InnerException?.Message ?? ex.Message });
                }
            }

            return Ok(new OrderResponseDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerName = (customer.FirstName + " " + customer.LastName).Trim(),
                CreatedBy = order.CreatedBy,
                SubTotal = order.SubTotal,
                TotalDiscount = order.TotalDiscount,
                TotalAmount = order.TotalAmount,
                PointsEarned = order.PointsEarned,
                OrderDate = order.OrderDate,
                Status = order.Status,
            });
        }

        private static decimal CalculatePromoDiscount(Campaign campaign, decimal orderTotal)
        {
            if (orderTotal <= 0) return 0m;
            if (campaign.MinPurchaseAmount.HasValue && orderTotal < campaign.MinPurchaseAmount.Value)
                return 0m;

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