using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRM_Morcaffein.domain.entities;

namespace CRM_Morcaffein.api.Controllers
{
    /// <summary>
    /// Dashboard analytics endpoints. Every endpoint here pre-aggregates on the
    /// server — the client never sees raw rows, only chart-ready shapes.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/tenant/{companyId:int}/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly ITenantDbContextFactory _tenantFactory;

        private const int GoldThreshold = 500;
        private const int SilverThreshold = 200;

        public DashboardController(ITenantDbContextFactory tenantFactory)
        {
            _tenantFactory = tenantFactory;
        }

        // GET api/tenant/{companyId}/dashboard/loyalty-tier-progression?months=6
        [HttpGet("loyalty-tier-progression")]
        public async Task<ActionResult<LoyaltyTierProgressionDto>> GetLoyaltyTierProgression(
            int companyId,
            [FromQuery] int months = 6)
        {
            if (months < 1) months = 1;
            if (months > 24) months = 24;

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var now = DateTime.UtcNow;
            var result = new LoyaltyTierProgressionDto();

            for (var i = months - 1; i >= 0; i--)
            {
                var monthEnd = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddMonths(-i + 1).AddTicks(-1);
                var monthStart = new DateTime(monthEnd.Year, monthEnd.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var eligibleCustomerIds = await tenant.Customers
                    .AsNoTracking()
                    .Where(c => c.CreatedAt <= monthEnd)
                    .Select(c => c.CustomerId)
                    .ToListAsync();

                var runningTotals = await tenant.PointLedgers
                    .AsNoTracking()
                    .Where(pl => pl.Timestamp <= monthEnd)
                    .GroupBy(pl => pl.CustomerId)
                    .Select(g => new { CustomerId = g.Key, Total = g.Sum(x => x.PointsChange) })
                    .ToDictionaryAsync(x => x.CustomerId, x => x.Total);

                int regular = 0, silver = 0, gold = 0;
                foreach (var customerId in eligibleCustomerIds)
                {
                    var total = runningTotals.TryGetValue(customerId, out var t) ? t : 0;
                    if (total >= GoldThreshold) gold++;
                    else if (total >= SilverThreshold) silver++;
                    else regular++;
                }

                result.Months.Add(new LoyaltyTierMonthPoint
                {
                    MonthLabel = monthStart.ToString("yyyy-MM"),
                    RegularCount = regular,
                    SilverCount = silver,
                    GoldCount = gold,
                });
            }

            return Ok(result);
        }

        // Fixed UTC+8 (Philippines Standard Time — no DST, so a constant offset is
        // correct year-round). Order.OrderDate is stored as DateTime.UtcNow; the
        // heatmap needs to bucket by the hour/day-of-week the sale actually
        // happened in the shop's local time, not raw UTC. If this app ever serves
        // shops outside PH, this needs to become a per-tenant setting instead of a
        // hardcoded constant.
        private static readonly TimeSpan PhilippinesUtcOffset = TimeSpan.FromHours(8);

        // GET api/tenant/{companyId}/dashboard/peak-hours
        // Static transaction velocity map (by design unaffected by range selector)
        [HttpGet("peak-hours")]
        public async Task<ActionResult<PeakHoursDto>> GetPeakHours(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var orders = await tenant.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Completed")
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync();

            var cells = orders
                .Select(o => new { LocalDate = o.OrderDate + PhilippinesUtcOffset, o.TotalAmount })
                .GroupBy(o => new { Day = (int)o.LocalDate.DayOfWeek, o.LocalDate.Hour })
                .Select(g => new PeakHoursCell
                {
                    DayOfWeek = g.Key.Day,
                    Hour = g.Key.Hour,
                    TransactionCount = g.Count(),
                    Revenue = g.Sum(x => x.TotalAmount),
                })
                .ToList();

            return Ok(new PeakHoursDto { Cells = cells });
        }

        // ---- Chart 1: Age Demographics vs. Spending Power (Shared) ----

        [HttpGet("age-demographics")]
        public async Task<ActionResult<AgeDemographicsAovDto>> GetAgeDemographics(
             int companyId,
             [FromQuery] int days = 30,
             [FromQuery] int? branchId = null)
        {
            if (days < 1) days = 1;
            if (days > 365) days = 365;

            branchId = await ResolveEffectiveBranchIdAsync(companyId, branchId);

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // The customer set stays company-wide — flipping branches shouldn't
            // change who's in each age bracket. Only the spend figures scope to
            // the branch, since that's what varies by location.
            var customers = await tenant.Customers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.CustomerId,
                    c.Birthday,
                    TotalSpend = c.Orders
                        .Where(o => o.Status == "Completed"
                                 && o.OrderDate >= startDate
                                 && (!branchId.HasValue || o.BranchId == branchId.Value))
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                    OrderCount = c.Orders
                        .Count(o => o.Status == "Completed"
                                 && o.OrderDate >= startDate
                                 && (!branchId.HasValue || o.BranchId == branchId.Value))
                })
                .ToListAsync();

            var totalMembers = customers.Count;
            var today = DateTime.UtcNow;

            string GetBracket(DateTime? bday)
            {
                if (!bday.HasValue) return "Unknown";
                var age = today.Year - bday.Value.Year;
                if (bday.Value.Date > today.AddYears(-age)) age--;

                if (age < 18) return "<18";
                if (age <= 24) return "18-24";
                if (age <= 34) return "25-34";
                return "35+";
            }

            var bracketsOrder = new[] { "<18", "18-24", "25-34", "35+", "Unknown" };

            var grouped = customers
                .GroupBy(c => GetBracket(c.Birthday))
                .ToDictionary(g => g.Key, g => new
                {
                    Count = g.Count(),
                    TotalSpend = g.Sum(x => x.TotalSpend),
                    TotalOrders = g.Sum(x => x.OrderCount)
                });

            var result = new AgeDemographicsAovDto();

            foreach (var b in bracketsOrder)
            {
                if (grouped.TryGetValue(b, out var data))
                {
                    var aov = data.TotalOrders > 0 ? data.TotalSpend / data.TotalOrders : 0m;
                    var pct = totalMembers > 0 ? (decimal)data.Count / totalMembers * 100m : 0m;

                    result.Brackets.Add(new AgeBracketPoint
                    {
                        BracketLabel = b,
                        ActiveMemberCount = data.Count,
                        PercentageOfTotal = decimal.Round(pct, 1),
                        AverageOrderValue = decimal.Round(aov, 2)
                    });
                }
                else
                {
                    result.Brackets.Add(new AgeBracketPoint
                    {
                        BracketLabel = b,
                        ActiveMemberCount = 0,
                        PercentageOfTotal = 0m,
                        AverageOrderValue = 0m
                    });
                }
            }

            return Ok(result);
        }

        // ---- Chart 2: Active Campaign Sales Lift & Redemptions (Shared) ----

        [HttpGet("campaign-lift")]
        public async Task<ActionResult<ActiveCampaignLiftDto>> GetCampaignLift(
            int companyId,
            [FromQuery] int days = 14,
            [FromQuery] int? branchId = null)
        {
            if (days < 1) days = 1;
            if (days > 90) days = 90;

            branchId = await ResolveEffectiveBranchIdAsync(companyId, branchId);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            var orders = await ScopeOrdersToBranch(
                    tenant.Orders.AsNoTracking().Where(o => o.Status == "Completed" && o.OrderDate >= startDate),
                    branchId)
                .Select(o => new { Date = o.OrderDate.Date, o.TotalAmount })
                .ToListAsync();

            // CampaignRedeems don't have their own BranchId — they inherit the branch
            // of their parent Order. Filtering them by joining to Order.BranchId keeps
            // the "which branch's redemptions" question correct.
            var redemptions = await tenant.CampaignRedeems
                .AsNoTracking()
                .Where(cr => cr.RedeemDate >= startDate
                          && (!branchId.HasValue || (cr.Order != null && cr.Order.BranchId == branchId.Value)))
                .Select(cr => cr.RedeemDate.Date)
                .ToListAsync();

            var orderGroup = orders.GroupBy(o => o.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));
            var redemptionGroup = redemptions.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());

            var result = new ActiveCampaignLiftDto();

            for (var d = startDate; d <= DateTime.UtcNow.Date; d = d.AddDays(1))
            {
                var rev = orderGroup.TryGetValue(d, out var r) ? r : 0m;
                var red = redemptionGroup.TryGetValue(d, out var c) ? c : 0;

                result.DailyPoints.Add(new DailyCampaignLiftPoint
                {
                    Date = d,
                    DateLabel = d.ToString("MMM d"),
                    DailyRevenue = rev,
                    DailyRedemptions = red
                });
            }

            return Ok(result);
        }

        // ---- Chart 3: Customer Satisfaction & Rating Distribution (Shared) ----

        [HttpGet("feedback-ratings")]
        public async Task<ActionResult<FeedbackRatingDistributionDto>> GetFeedbackRatings(
             int companyId,
             [FromQuery] int days = 30,
             [FromQuery] int? branchId = null)
        {
            if (days < 1) days = 1;
            if (days > 365) days = 365;

            branchId = await ResolveEffectiveBranchIdAsync(companyId, branchId);

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var feedbackRows = await tenant.Feedbacks
                .AsNoTracking()
                .Where(f => f.IsActive
                         && f.DateLogged >= startDate
                         && (!branchId.HasValue || f.BranchId == branchId.Value))
                .Select(f => new { f.Rating, f.Status })
                .ToListAsync();

            var feedbacks = feedbackRows.Select(f => f.Rating).ToList();
            var total = feedbacks.Count;
            var avg = total > 0 ? feedbacks.Average() : 0.0;

            // Pending count is a lifetime figure, not scoped to the date range —
            // a ticket logged three months ago and still unresolved is *more*
            // urgent than one logged yesterday. Deliberately not filtered by
            // `startDate`.
            var pendingCount = await tenant.Feedbacks
                .AsNoTracking()
                .Where(f => f.IsActive
                         && f.Status == "Pending"
                         && (!branchId.HasValue || f.BranchId == branchId.Value))
                .CountAsync();

            var result = new FeedbackRatingDistributionDto
            {
                AverageRating = Math.Round(avg, 1),
                TotalFeedbacks = total,
                PendingFeedbackCount = pendingCount
            };

            for (int rating = 1; rating <= 5; rating++)
            {
                var count = feedbacks.Count(r => r == rating);
                var pct = total > 0 ? (decimal)count / total * 100m : 0m;

                result.RatingBreakdown.Add(new RatingDistributionPoint
                {
                    Rating = rating,
                    Count = count,
                    Percentage = decimal.Round(pct, 1)
                });
            }

            return Ok(result);
        }

        // ---- Chart 6: Shift / Barista Sales Performance (Manager Only) ----

        [HttpGet("barista-sales")]
        public async Task<ActionResult<BaristaSalesPerformanceDto>> GetBaristaSales(
            int companyId,
            [FromQuery] int days = 30)
        {
            if (!CallerIsManagerOrAdmin())
                return StatusCode(403, new { message = "Only managers and admins can view barista performance." });

            if (days < 1) days = 1;
            if (days > 365) days = 365;

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var sales = await tenant.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Completed" && o.OrderDate >= startDate)
                .GroupBy(o => o.CreatedBy)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    OrdersProcessed = g.Count(),
                    TotalRevenue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var employeeIds = sales.Select(s => s.EmployeeId).ToList();

            var employees = await tenant.Employees
                .AsNoTracking()
                .Where(e => employeeIds.Contains(e.EmployeeId))
                .ToDictionaryAsync(e => e.EmployeeId, e => (e.FirstName + " " + e.LastName).Trim());

            var result = new BaristaSalesPerformanceDto();

            foreach (var item in sales.OrderByDescending(s => s.TotalRevenue))
            {
                var name = employees.TryGetValue(item.EmployeeId, out var empName) ? empName : $"Staff #{item.EmployeeId}";
                var aov = item.OrdersProcessed > 0 ? item.TotalRevenue / item.OrdersProcessed : 0m;

                result.Baristas.Add(new BaristaPerformancePoint
                {
                    EmployeeId = item.EmployeeId,
                    BaristaName = name,
                    OrdersProcessed = item.OrdersProcessed,
                    TotalRevenue = item.TotalRevenue,
                    AverageOrderValue = decimal.Round(aov, 2)
                });
            }

            return Ok(result);
        }

        // ---- Chart 7: Cash Register & POS Diagnostics (Manager Only) ----

        [HttpGet("pos-diagnostics")]
        public async Task<ActionResult<PosDiagnosticsDto>> GetPosDiagnostics(
            int companyId,
            [FromQuery] int days = 30,
            [FromQuery] int? branchId = null)
        {
            if (!CallerIsManagerOrAdmin())
                return StatusCode(403, new { message = "Only managers and admins can view POS diagnostics." });

            branchId = await ResolveEffectiveBranchIdAsync(companyId, branchId);

            if (days < 1) days = 1;
            if (days > 365) days = 365;

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var completedOrders = await ScopeOrdersToBranch(
                    tenant.Orders.AsNoTracking().Where(o => o.Status == "Completed" && o.OrderDate >= startDate),
                    branchId)
                .ToListAsync();

            var gross = completedOrders.Sum(o => o.SubTotal);
            var campaignDiscounts = completedOrders.Sum(o => o.TotalDiscount);
            var pwdDiscounts = completedOrders.Sum(o => o.PwdSeniorDiscountAmount);
            var net = completedOrders.Sum(o => o.TotalAmount);

            var totalCount = completedOrders.Count;
            var pwdCount = completedOrders.Count(o => o.HasPwdSeniorDiscount);
            var campaignCount = completedOrders.Count(o => o.TotalDiscount > 0);

            return Ok(new PosDiagnosticsDto
            {
                GrossSales = gross,
                TotalCampaignDiscounts = campaignDiscounts,
                TotalPwdSeniorDiscounts = pwdDiscounts,
                NetSales = net,
                TotalTransactions = totalCount,
                PwdSeniorTransactionCount = pwdCount,
                CampaignTransactionCount = campaignCount
            });
        }

        // ---- Chart 8: Campaign ROI & Profit Margin Erosion (Admin Only) ----

        [HttpGet("campaign-roi")]
        public async Task<ActionResult<CampaignRoiDto>> GetCampaignRoi(
            int companyId,
            [FromQuery] int days = 30)
        {
            if (!CallerIsManagerOrAdmin())
                return StatusCode(403, new { message = "Only managers and admins can view campaign ROI reports." });

            if (days < 1) days = 1;
            if (days > 365) days = 365;

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var campaigns = await tenant.Campaigns
                .AsNoTracking()
                .Include(c => c.CampaignRedeems.Where(cr => cr.RedeemDate >= startDate))
                    .ThenInclude(cr => cr.Order)
                .OrderByDescending(c => c.CampaignId)
                .ToListAsync();

            var result = new CampaignRoiDto();

            foreach (var c in campaigns)
            {
                var redemptions = c.CampaignRedeems.Count;
                var discountCost = c.CampaignRedeems.Sum(cr => cr.DiscountUsed);
                var revenue = c.CampaignRedeems
                    .Where(cr => cr.Order != null && cr.Order.Status == "Completed")
                    .Sum(cr => cr.Order!.TotalAmount);

                result.Campaigns.Add(new CampaignRoiItem
                {
                    CampaignId = c.CampaignId,
                    PromoName = c.PromoName,
                    TotalRedemptions = redemptions,
                    TotalDiscountCost = discountCost,
                    RevenueGenerated = revenue,
                    NetProfitImpact = revenue - discountCost
                });
            }

            return Ok(result);
        }

        // ---- Chart 9: Customer Lifetime Value & Churn Risk (Admin Only) ----

        [HttpGet("customer-ltv-churn")]
        public async Task<ActionResult<CustomerLtvChurnDto>> GetCustomerLtvChurn(
            int companyId,
            [FromQuery] int days = 30)
        {
            if (!CallerIsAdmin())
                return StatusCode(403, new { message = "Only admins can view customer LTV reports." });

            if (days < 1) days = 1;
            if (days > 365) days = 365;

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var customers = await tenant.Customers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.CustomerId,
                    c.PointsBalance,
                    c.LastVisitDate,
                    TotalSpend = c.Orders.Where(o => o.Status == "Completed" && o.OrderDate >= startDate).Sum(o => (decimal?)o.TotalAmount) ?? 0m
                })
                .ToListAsync();

            var now = DateTime.UtcNow;
            var thresholdDays = 14;

            var overallAvg = customers.Count > 0 ? customers.Average(c => c.TotalSpend) : 0m;

            var regular = customers.Where(c => c.PointsBalance < 200).ToList();
            var silver = customers.Where(c => c.PointsBalance >= 200 && c.PointsBalance < 500).ToList();
            var gold = customers.Where(c => c.PointsBalance >= 500).ToList();

            var regAvg = regular.Count > 0 ? regular.Average(c => c.TotalSpend) : 0m;
            var silAvg = silver.Count > 0 ? silver.Average(c => c.TotalSpend) : 0m;
            var goldAvg = gold.Count > 0 ? gold.Average(c => c.TotalSpend) : 0m;

            var atRiskHighTierCount = customers.Count(c =>
                 c.PointsBalance >= 200 && (now - c.LastVisitDate).TotalDays > thresholdDays);

            // Total at-risk across all tiers — any member inactive for longer than the
            // threshold, regardless of their loyalty level. Feeds the Admin LTV pod's
            // sub-line, which is a whole-base signal rather than a VIP-only one.
            var atRiskTotalCount = customers.Count(c =>
                (now - c.LastVisitDate).TotalDays > thresholdDays);

            return Ok(new CustomerLtvChurnDto
            {
                OverallAverageLtv = decimal.Round(overallAvg, 2),
                RegularTierAvgLtv = decimal.Round(regAvg, 2),
                SilverTierAvgLtv = decimal.Round(silAvg, 2),
                GoldTierAvgLtv = decimal.Round(goldAvg, 2),
                AtRiskHighTierCount = atRiskHighTierCount,
                AtRiskTotalCount = atRiskTotalCount,
                ActiveMemberCount = customers.Count
            });
        }

        // ---- Chart 10: Staff Overrides & Audit Logs (Admin Only) ----

        [HttpGet("audit-summary")]
        public async Task<ActionResult<AuditLogSummaryDto>> GetAuditSummary(
            int companyId,
            [FromQuery] int days = 30)
        {
            if (!CallerIsAdmin())
                return StatusCode(403, new { message = "Only admins can view audit summary reports." });

            if (days < 1) days = 1;
            if (days > 365) days = 365;

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var adjustments = await tenant.PointAdjustmentRequests
                .AsNoTracking()
                .Where(a => a.RequestedAt >= startDate)
                .ToListAsync();

            var totalAdjustments = adjustments.Count;
            var approvedAdjustments = adjustments.Count(a => a.Status == "Approved");
            var rejectedAdjustments = adjustments.Count(a => a.Status == "Rejected");

            var feedbackBonusCount = await tenant.PointLedgers
                .AsNoTracking()
                .Where(pl => pl.Type == "FeedbackBonus" && pl.Timestamp >= startDate)
                .CountAsync();

            var pendingCampaigns = await tenant.Campaigns
                .AsNoTracking()
                .Where(c => c.Status == "Pending")
                .CountAsync();

            var activeEmployeeCount = await tenant.Employees
                .AsNoTracking()
                .CountAsync(e => e.IsActive);

            return Ok(new AuditLogSummaryDto
            {
                TotalManualPointAdjustments = totalAdjustments,
                ApprovedPointAdjustments = approvedAdjustments,
                RejectedPointAdjustments = rejectedAdjustments,
                FeedbackBonusPointsAwarded = feedbackBonusCount,
                PendingCampaignApprovals = pendingCampaigns,
                ActiveEmployeeCount = activeEmployeeCount
            });
        }

        // ---- Recent Orders (Sales per Order KPI Modal) ----

        [HttpGet("recent-orders")]
        public async Task<ActionResult<List<RecentOrderSummaryDto>>> GetRecentOrders(
             int companyId,
             [FromQuery] int days = 30,
             [FromQuery] int limit = 10,
             [FromQuery] int? branchId = null)
        {
            if (days < 1) days = 1;
            if (days > 365) days = 365;
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100;

            branchId = await ResolveEffectiveBranchIdAsync(companyId, branchId);

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var orders = await ScopeOrdersToBranch(
                    tenant.Orders
                        .AsNoTracking()
                        .Include(o => o.Customer)
                        .Include(o => o.OrderItems)
                        .Where(o => o.Status == "Completed" && o.OrderDate >= startDate),
                    branchId)
                .OrderByDescending(o => o.OrderDate)
                .Take(limit)
                .Select(o => new RecentOrderSummaryDto
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    CustomerName = o.Customer != null ? (o.Customer.FirstName + " " + o.Customer.LastName).Trim() : "Walk-in Customer",
                    ItemCount = o.OrderItems.Sum(i => i.Quantity),
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET api/tenant/{companyId}/dashboard/revenue-vs-discounts?days=90
        //
        // Returns, bucketed by week (for ≤60-day ranges) or by month (for >60-day ranges):
        //   - NetRevenue (what customers paid)
        //   - CampaignDiscount (business-chosen promo discounts)
        //   - PwdSeniorDiscount (legally-mandated discounts, kept separate)
        //
        // The Admin dashboard uses this to answer "are we becoming dependent on
        // discounts?" — if the discount lines grow relative to net revenue over time,
        // the business is leaning more on promotions to move product.
        //
        // PWD/Senior is shown separately because it's a legal obligation, not a
        // business decision. Bundling it with campaign discounts would misrepresent
        // the "how much of our revenue is our choice to discount" story.
        [HttpGet("revenue-vs-discounts")]
        public async Task<ActionResult<RevenueVsDiscountsDto>> GetRevenueVsDiscounts(
            int companyId,
            [FromQuery] int days = 90)
        {
            if (days < 7) days = 7;
            if (days > 730) days = 730;

            // Compute the window in shop-local time so bucket boundaries land on
            // local midnight, not 8am-local (which is what a midnight-UTC boundary
            // becomes for a PH shop). Since Order.OrderDate is stored in UTC, we
            // shift incoming order dates by the offset before bucketing, and the
            // window/bucket edges are computed in local time then compared against
            // shifted dates. Same treatment as GetPeakHours.
            var todayLocal = (DateTime.UtcNow + PhilippinesUtcOffset).Date;
            var startLocal = todayLocal.AddDays(-days + 1);

            // Bucket by week for shorter ranges, month for longer. This keeps the
            // chart's x-axis readable — 90 daily buckets would be unreadable; 13
            // weekly buckets is the right density.
            var bucketByMonth = days > 60;

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // Query using the UTC boundary that corresponds to startLocal (so EF
            // can push the filter down to SQL). Then shift every returned OrderDate
            // into local time for bucketing.
            var startUtc = startLocal - PhilippinesUtcOffset;

            var orders = (await tenant.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Completed" && o.OrderDate >= startUtc)
                .Select(o => new
                {
                    o.OrderDate,
                    o.TotalAmount,
                    o.TotalDiscount,
                    o.PwdSeniorDiscountAmount
                })
                .ToListAsync())
                .Select(o => new
                {
                    OrderDate = o.OrderDate + PhilippinesUtcOffset,   // local time
                    o.TotalAmount,
                    o.TotalDiscount,
                    o.PwdSeniorDiscountAmount
                })
                .ToList();

            var buckets = new List<RevenueVsDiscountsBucket>();

            if (bucketByMonth)
            {
                // Month buckets — walk from the month containing startDate to the
                // month containing today.
                var firstMonth = new DateTime(startLocal.Year, startLocal.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                var lastMonth = new DateTime(todayLocal.Year, todayLocal.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

                for (var m = firstMonth; m <= lastMonth; m = m.AddMonths(1))
                {
                    var monthEnd = m.AddMonths(1).AddTicks(-1);

                    var slice = orders.Where(o => o.OrderDate >= m && o.OrderDate <= monthEnd).ToList();

                    buckets.Add(new RevenueVsDiscountsBucket
                    {
                        BucketLabel = m.ToString("MMM yyyy"),
                        NetRevenue = slice.Sum(o => o.TotalAmount),
                        CampaignDiscount = slice.Sum(o => o.TotalDiscount),
                        PwdSeniorDiscount = slice.Sum(o => o.PwdSeniorDiscountAmount),
                    });
                }
            }
            else
            {
                // Week buckets — start each bucket on the startDate's day-of-week and
                // step 7 days. The final bucket may be a partial week ending today.
                var bucketStart = startLocal;
                while (bucketStart <= todayLocal)
                {
                    var bucketEnd = bucketStart.AddDays(6);
                    if (bucketEnd > todayLocal) bucketEnd = todayLocal;

                    var slice = orders
                        .Where(o => o.OrderDate.Date >= bucketStart && o.OrderDate.Date <= bucketEnd)
                        .ToList();

                    buckets.Add(new RevenueVsDiscountsBucket
                    {
                        BucketLabel = bucketStart.ToString("MMM d"),
                        NetRevenue = slice.Sum(o => o.TotalAmount),
                        CampaignDiscount = slice.Sum(o => o.TotalDiscount),
                        PwdSeniorDiscount = slice.Sum(o => o.PwdSeniorDiscountAmount),
                    });

                    bucketStart = bucketStart.AddDays(7);
                }
            }

            return Ok(new RevenueVsDiscountsDto { Buckets = buckets });
        }

        // GET api/tenant/{companyId}/dashboard/new-vs-returning?months=6
        //
        // For each month in the window, counts the distinct customers who placed a
        // completed order in that month, split into:
        //   - "new": the customer's first-ever completed order was IN that month
        //   - "returning": the customer had completed at least one order BEFORE
        //     that month started
        //
        // This is the strategic retention signal: if "new" grows while "returning"
        // stays flat, we're acquiring customers but not keeping them. If both grow,
        // the loyalty program is doing its job.
        //
        // Counts are DISTINCT CUSTOMERS, not orders — "how many people came back"
        // matters more than "how many visits happened."
        [HttpGet("new-vs-returning")]
        public async Task<ActionResult<NewVsReturningDto>> GetNewVsReturning(
            int companyId,
            [FromQuery] int months = 6)
        {
            if (months < 1) months = 1;
            if (months > 24) months = 24;

            var todayLocal = (DateTime.UtcNow + PhilippinesUtcOffset).Date;
            var firstMonthStartLocal = new DateTime(todayLocal.Year, todayLocal.Month, 1, 0, 0, 0, DateTimeKind.Unspecified)
                .AddMonths(-(months - 1));

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // Pull every (CustomerId, OrderDate) pair for completed orders from the
            // first month of the window onward. We do NOT need anything before
            // firstMonthStart for the ORDER dates — but we DO need to know, for each
            // customer, whether they had ordered before firstMonthStart. That's what
            // the next query handles.
            var firstMonthStartUtc = firstMonthStartLocal - PhilippinesUtcOffset;

            var ordersInWindow = (await tenant.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Completed" && o.OrderDate >= firstMonthStartUtc)
                .Select(o => new { o.CustomerId, o.OrderDate })
                .ToListAsync())
                .Select(o => new
                {
                    o.CustomerId,
                    OrderDate = o.OrderDate + PhilippinesUtcOffset,   // local wall-clock
                })
                .ToList();

            // For every customer who ordered within the window, find their absolute
            // first completed order date ever — a single grouped MIN across the whole
            // Orders table, restricted to the customers we care about. This is one
            // query, not one per customer.
            var customerIdsInWindow = ordersInWindow
                .Select(o => o.CustomerId)
                .Distinct()
                .ToList();

            var firstOrderDatesRaw = await tenant.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Completed"
                         && customerIdsInWindow.Contains(o.CustomerId))
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    FirstOrderDate = g.Min(x => x.OrderDate),
                })
                .ToDictionaryAsync(x => x.CustomerId, x => x.FirstOrderDate);

            // Shift every first-order-date into local wall-clock so the comparisons
            // against monthStart / monthEnd (both local) line up.
            var firstOrderDates = firstOrderDatesRaw
                .ToDictionary(kv => kv.Key, kv => kv.Value + PhilippinesUtcOffset);

            // Build the month buckets.
            var result = new NewVsReturningDto();

            for (var i = 0; i < months; i++)
            {
                var monthStart = firstMonthStartLocal.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                // Distinct customers who ordered IN this month.
                var customersThisMonth = ordersInWindow
                    .Where(o => o.OrderDate >= monthStart && o.OrderDate <= monthEnd)
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .ToList();

                // Of those, split: first-ever order was inside this month (= new),
                // vs had ordered before this month started (= returning).
                int newCount = 0;
                int returningCount = 0;

                foreach (var customerId in customersThisMonth)
                {
                    if (!firstOrderDates.TryGetValue(customerId, out var firstOrderDate))
                    {
                        // Shouldn't happen — customer appears in ordersInWindow so
                        // they'll be in the MIN query too — but be defensive.
                        newCount++;
                        continue;
                    }

                    if (firstOrderDate >= monthStart && firstOrderDate <= monthEnd)
                        newCount++;
                    else
                        returningCount++;
                }

                result.Buckets.Add(new NewVsReturningBucket
                {
                    MonthLabel = monthStart.ToString("MMM yyyy"),
                    NewCustomerCount = newCount,
                    ReturningCustomerCount = returningCount,
                });
            }

            return Ok(result);
        }

        // GET api/tenant/{companyId}/dashboard/top-products?days=30&limit=5
        //
        // Returns the top-N products by revenue for the trailing `days` window.
        // Ranks by revenue (what made us money), not units (what moved) — that's
        // the Manager's framing: "which drinks are pulling weight financially."
        // Units are returned alongside so the card can show both signals.
        //
        // Size variants (Regular/Large) are rolled up to their parent Product —
        // OrderItem rows are per-size, but the Manager wants "Iced Americano: 42
        // units", not a split across sizes.
        [HttpGet("top-products")]
        public async Task<ActionResult<TopProductsResponseDto>> GetTopProducts(
            int companyId,
            [FromQuery] int days = 30,
            [FromQuery] int limit = 5,
            [FromQuery] int? branchId = null)
        {
            if (days < 1) days = 1;
            if (days > 365) days = 365;
            if (limit < 1) limit = 1;
            if (limit > 20) limit = 20;

            branchId = await ResolveEffectiveBranchIdAsync(companyId, branchId);

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var rawRows = await tenant.OrderItems
                .AsNoTracking()
                .Where(oi => oi.Order != null
                          && oi.Order.Status == "Completed"
                          && oi.Order.OrderDate >= startDate
                          && (!branchId.HasValue || oi.Order.BranchId == branchId.Value))
                .Select(oi => new
                {
                    oi.ProductId,
                    oi.Quantity,
                    oi.BaseSizePrice,
                    ModifierTotal = oi.OrderItemModifiers.Sum(m => (decimal?)m.UnitPrice) ?? 0m,
                })
                .ToListAsync();

            var rows = rawRows
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    UnitsSold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * (x.BaseSizePrice + x.ModifierTotal)),
                })
                .OrderByDescending(x => x.Revenue)
                .Take(limit)
                .ToList();

            if (rows.Count == 0)
                return Ok(new TopProductsResponseDto());

            var productIds = rows.Select(r => r.ProductId).ToList();

            var products = await tenant.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.ProductId))
                .Select(p => new { p.ProductId, p.ProductName, p.ProductCategory })
                .ToDictionaryAsync(p => p.ProductId);

            var result = new TopProductsResponseDto();

            foreach (var r in rows)
            {
                products.TryGetValue(r.ProductId, out var meta);

                result.Products.Add(new TopProductItem
                {
                    ProductId = r.ProductId,
                    ProductName = meta?.ProductName ?? $"Product #{r.ProductId}",
                    ProductCategory = meta?.ProductCategory ?? "—",
                    UnitsSold = r.UnitsSold,
                    Revenue = r.Revenue,
                });
            }

            return Ok(result);
        }
        // GET api/tenant/{companyId}/dashboard/visit-frequency
        //
        // Groups all customers who have ever placed a completed order by how many
        // completed orders they have placed, into 5 buckets. Tells the Admin what
        // kind of business this is — one-time-visit heavy (weak retention) or
        // regular-heavy (strong retention).
        //
        // Not date-filtered: this is a lifetime-shape metric, not a period metric.
        // "How many times have our members visited us, ever" is the question.
        [HttpGet("visit-frequency")]
        public async Task<ActionResult<VisitFrequencyDto>> GetVisitFrequency(int companyId)
        {
            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            // Group completed orders by customer, count per customer.
            var counts = await tenant.Orders
                .AsNoTracking()
                .Where(o => o.Status == "Completed")
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToListAsync();

            int b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0;

            foreach (var c in counts)
            {
                switch (c.Count)
                {
                    case 1: b1++; break;
                    case <= 3: b2++; break;
                    case <= 7: b3++; break;
                    case <= 15: b4++; break;
                    default: b5++; break;
                }
            }

            var result = new VisitFrequencyDto
            {
                TotalCustomers = counts.Count,
                Buckets =
                {
                    new VisitFrequencyBucket { Label = "1 visit",     CustomerCount = b1 },
                    new VisitFrequencyBucket { Label = "2–3 visits",  CustomerCount = b2 },
                    new VisitFrequencyBucket { Label = "4–7 visits",  CustomerCount = b3 },
                    new VisitFrequencyBucket { Label = "8–15 visits", CustomerCount = b4 },
                    new VisitFrequencyBucket { Label = "16+ visits",  CustomerCount = b5 },
                }
            };

            return Ok(result);
        }

        // GET api/tenant/{companyId}/dashboard/transactions?days=30
        //
        // Returns every completed order in the window as a flat ledger — one row
        // per order with subtotal / discount / PWD / net / points. This is the
        // raw data behind every other dashboard chart, exposed for the PDF
        // transaction ledger report.
        //
        // Not paginated: the report needs the whole window to compute its own
        // footer totals. Cap is 1000 rows so the endpoint can't be abused.
        [HttpGet("transactions")]
        public async Task<ActionResult<TransactionLedgerDto>> GetTransactions(
            int companyId,
            [FromQuery] int days = 30,
            [FromQuery] int? branchId = null)
        {
            if (days < 1) days = 1;
            if (days > 365) days = 365;

            branchId = await ResolveEffectiveBranchIdAsync(companyId, branchId);

            var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var orders = await ScopeOrdersToBranch(
                    tenant.Orders
                        .AsNoTracking()
                        .Include(o => o.Customer)
                        .Include(o => o.OrderItems)
                        .Where(o => o.Status == "Completed" && o.OrderDate >= startDate),
                    branchId)
                .OrderByDescending(o => o.OrderDate)
                .Take(1000)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    CustomerName = o.Customer != null
                        ? (o.Customer.FirstName + " " + o.Customer.LastName).Trim()
                        : "Walk-in",
                    ItemCount = o.OrderItems.Sum(i => i.Quantity),
                    o.SubTotal,
                    o.TotalDiscount,
                    o.PwdSeniorDiscountAmount,
                    o.TotalAmount,
                    o.PointsEarned,
                })
                .ToListAsync();

            var result = new TransactionLedgerDto
            {
                TotalSubTotal = orders.Sum(o => o.SubTotal),
                TotalDiscount = orders.Sum(o => o.TotalDiscount),
                TotalPwdSenior = orders.Sum(o => o.PwdSeniorDiscountAmount),
                TotalNet = orders.Sum(o => o.TotalAmount),
                TotalPointsEarned = orders.Sum(o => o.PointsEarned),
            };

            result.Rows = orders.Select(o => new TransactionLedgerRow
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                CustomerName = o.CustomerName,
                ItemCount = o.ItemCount,
                SubTotal = o.SubTotal,
                TotalDiscount = o.TotalDiscount,
                PwdSeniorDiscount = o.PwdSeniorDiscountAmount,
                NetAmount = o.TotalAmount,
                PointsEarned = o.PointsEarned,
            }).ToList();

            return Ok(result);
        }

        // GET api/tenant/{companyId}/dashboard/recent-employees
        //
        // Last N employees by EmployeeId descending. Only used by the Basic-tier
        // Admin dashboard's "Recent Employees" card, which shows a mini roster
        // alongside the Employee count pod. Not branch-scoped — the Admin's
        // employee roster is company-wide by design.
        [HttpGet("recent-employees")]
        public async Task<ActionResult<RecentEmployeesDto>> GetRecentEmployees(
            int companyId,
            [FromQuery] int limit = 5)
        {
            if (limit < 1) limit = 5;
            if (limit > 20) limit = 20;

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var employees = await tenant.Employees
                .AsNoTracking()
                .Include(e => e.Branch)
                .OrderByDescending(e => e.EmployeeId)
                .Take(limit)
                .Select(e => new RecentEmployeeItem
                {
                    EmployeeId = e.EmployeeId,
                    FullName = (e.FirstName + " " + e.LastName).Trim(),
                    Role = e.Role,
                    BranchName = e.Branch != null ? e.Branch.BranchName : null,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt,
                })
                .ToListAsync();

            return Ok(new RecentEmployeesDto { Employees = employees });
        }

        // GET api/tenant/{companyId}/dashboard/recent-feedback
        //
        // Last N feedback tickets by DateLogged descending. Only used by the
        // Basic-tier Admin dashboard's "Recent Feedback" card. Branch-scoped
        // via the optional branchId, same as the Feedback tab itself.
        [HttpGet("recent-feedback")]
        public async Task<ActionResult<RecentFeedbackDto>> GetRecentFeedback(
            int companyId,
            [FromQuery] int limit = 5,
            [FromQuery] int? branchId = null)
        {
            if (limit < 1) limit = 5;
            if (limit > 20) limit = 20;

            branchId = await ResolveEffectiveBranchIdAsync(companyId, branchId);

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var feedbacks = await tenant.Feedbacks
                .AsNoTracking()
                .Include(f => f.Customer)
                .Where(f => f.IsActive
                         && (!branchId.HasValue || f.BranchId == branchId.Value))
                .OrderByDescending(f => f.DateLogged)
                .Take(limit)
                .Select(f => new RecentFeedbackItem
                {
                    TicketId = f.TicketId,
                    CustomerName = f.Customer != null
                        ? (f.Customer.FirstName + " " + f.Customer.LastName).Trim()
                        : "—",
                    Rating = f.Rating,
                    Category = f.Category,
                    Status = f.Status,
                    DateLogged = f.DateLogged,
                })
                .ToListAsync();

            return Ok(new RecentFeedbackDto { Feedbacks = feedbacks });
        }

        // ---- Helpers ----

        /// <summary>
        /// Resolves the branch a request should actually be scoped to.
        ///
        /// Rules:
        ///   - If the caller is a pinned employee (Employee.BranchId != null),
        ///     the server forces their own branch. Whatever the client sent
        ///     in ?branchId= is ignored.
        ///   - If the caller is all-branch (Admin, or Manager with
        ///     BranchId == null), the client-supplied value is honored.
        ///     null = "All Branches".
        ///
        /// This is the actual enforcement — the client hiding the selector
        /// is UX, not a security boundary.
        /// </summary>
        private async Task<int?> ResolveEffectiveBranchIdAsync(int companyId, int? requestedBranchId)
        {
            var empIdClaim = User.FindFirst("employeeId")?.Value;
            if (!int.TryParse(empIdClaim, out var employeeId))
                return requestedBranchId;   // shouldn't happen given [Authorize], but no-op if it does

            await using var tenant = await _tenantFactory.CreateAsync(companyId);

            var callerBranch = await tenant.Employees
                .AsNoTracking()
                .Where(e => e.EmployeeId == employeeId)
                .Select(e => e.BranchId)
                .FirstOrDefaultAsync();

            // Pinned caller → force their own branch, ignore the request.
            if (callerBranch.HasValue)
                return callerBranch.Value;

            // All-branch caller → honor whatever they sent (which may be null).
            return requestedBranchId;
        }

        private bool CallerIsManagerOrAdmin() =>
            User.IsInRole("Manager") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        private bool CallerIsAdmin() =>
            User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        /// <summary>
        /// Returns a filtered IQueryable of Orders, scoped to a branch if
        /// branchId has a value. When branchId is null, returns all orders —
        /// the "All Branches" case the topbar selector can produce.
        ///
        /// This is deliberately on IQueryable so EF can push the filter down
        /// to SQL rather than materializing rows first.
        /// </summary>
        private static IQueryable<CRM_Morcaffein.domain.entities.Order> ScopeOrdersToBranch(
            IQueryable<CRM_Morcaffein.domain.entities.Order> query,
            int? branchId)
        {
            return branchId.HasValue
                ? query.Where(o => o.BranchId == branchId.Value)
                : query;
        }
    }
}
