namespace CRM_Morcaffein.api.DTOs;

// ---- Chart 5: Loyalty Tier Progression ----

public class LoyaltyTierProgressionDto
{
    public List<LoyaltyTierMonthPoint> Months { get; set; } = new();
}

public class LoyaltyTierMonthPoint
{
    /// <summary>"2026-06" style label, oldest first.</summary>
    public string MonthLabel { get; set; } = string.Empty;
    public int RegularCount { get; set; }
    public int SilverCount { get; set; }
    public int GoldCount { get; set; }
}

// ---- Chart 4: Peak Hours Transaction Velocity ----

public class PeakHoursDto
{
    public List<PeakHoursCell> Cells { get; set; } = new();
}

public class PeakHoursCell
{
    /// <summary>0 (Sunday) - 6 (Saturday), matches System.DayOfWeek.</summary>
    public int DayOfWeek { get; set; }
    /// <summary>0-23, 24-hour clock.</summary>
    public int Hour { get; set; }
    public int TransactionCount { get; set; }
    public decimal Revenue { get; set; }
}

// ---- Chart 1: Age Demographics vs. Spending Power ----

public class AgeDemographicsAovDto
{
    public List<AgeBracketPoint> Brackets { get; set; } = new();
}

public class AgeBracketPoint
{
    /// <summary>"<18", "18-24", "25-34", "35+", "Unknown"</summary>
    public string BracketLabel { get; set; } = string.Empty;
    public int ActiveMemberCount { get; set; }
    public decimal PercentageOfTotal { get; set; }
    public decimal AverageOrderValue { get; set; }
}

// ---- Chart 2: Active Campaign Sales Lift & Redemptions ----

public class ActiveCampaignLiftDto
{
    public List<DailyCampaignLiftPoint> DailyPoints { get; set; } = new();
}

public class DailyCampaignLiftPoint
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public decimal DailyRevenue { get; set; }
    public int DailyRedemptions { get; set; }
}

// ---- Chart 3: Customer Satisfaction & Rating Distribution ----

public class FeedbackRatingDistributionDto
{
    public double AverageRating { get; set; }
    public int TotalFeedbacks { get; set; }
    public List<RatingDistributionPoint> RatingBreakdown { get; set; } = new();
}

public class RatingDistributionPoint
{
    /// <summary>1 to 5 Stars</summary>
    public int Rating { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

// ---- Chart 6: Shift / Barista Sales Performance (Manager-Only) ----

public class BaristaSalesPerformanceDto
{
    public List<BaristaPerformancePoint> Baristas { get; set; } = new();
}

public class BaristaPerformancePoint
{
    public int EmployeeId { get; set; }
    public string BaristaName { get; set; } = string.Empty;
    public int OrdersProcessed { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

// ---- Chart 7: Cash Register & POS Diagnostics (Manager-Only) ----

public class PosDiagnosticsDto
{
    public decimal GrossSales { get; set; }
    public decimal TotalCampaignDiscounts { get; set; }
    public decimal TotalPwdSeniorDiscounts { get; set; }
    public decimal NetSales { get; set; }
    public int TotalTransactions { get; set; }
    public int PwdSeniorTransactionCount { get; set; }
    public int CampaignTransactionCount { get; set; }
}

// ---- Chart 8: Campaign ROI & Profit Margin Erosion (Admin-Only) ----

public class CampaignRoiDto
{
    public List<CampaignRoiItem> Campaigns { get; set; } = new();
}

public class CampaignRoiItem
{
    public int CampaignId { get; set; }
    public string PromoName { get; set; } = string.Empty;
    public int TotalRedemptions { get; set; }
    public decimal TotalDiscountCost { get; set; }
    public decimal RevenueGenerated { get; set; }
    public decimal NetProfitImpact { get; set; }
}

// ---- Chart 9: Customer Lifetime Value & Churn Risk (Admin-Only) ----

public class CustomerLtvChurnDto
{
    public decimal OverallAverageLtv { get; set; }
    public decimal RegularTierAvgLtv { get; set; }
    public decimal SilverTierAvgLtv { get; set; }
    public decimal GoldTierAvgLtv { get; set; }

    /// <summary>Silver/Gold members inactive for more than 14 days. Shown as a separate line in the LTV detail modal.</summary>
    public int AtRiskHighTierCount { get; set; }

    /// <summary>All members (any tier) inactive for more than 14 days. Feeds the LTV KPI pod's secondary line on the Admin dashboard.</summary>
    public int AtRiskTotalCount { get; set; }

    /// <summary>Total active members considered for this report. Denominator for the at-risk percentage.</summary>
    public int ActiveMemberCount { get; set; }
}

// ---- Chart 10: Staff Overrides & Audit Logs (Admin-Only) ----

public class AuditLogSummaryDto
{
    public int TotalManualPointAdjustments { get; set; }
    public int ApprovedPointAdjustments { get; set; }
    public int RejectedPointAdjustments { get; set; }
    public int FeedbackBonusPointsAwarded { get; set; }
    public int PendingCampaignApprovals { get; set; }
}

// ---- Recent Orders (Sales per Order KPI Modal) ----

public class RecentOrderSummaryDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
}

// ---- Admin Dashboard: Revenue vs Discounts over time ----

public class RevenueVsDiscountsDto
{
    public List<RevenueVsDiscountsBucket> Buckets { get; set; } = new();
}

public class RevenueVsDiscountsBucket
{
    /// <summary>Human-readable label: "Sep 1" for daily/weekly buckets, "Sep 2026" for monthly.</summary>
    public string BucketLabel { get; set; } = string.Empty;

    /// <summary>Net revenue for the bucket — what customers actually paid, after all discounts.</summary>
    public decimal NetRevenue { get; set; }

    /// <summary>Total campaign/promo discount applied in the bucket.</summary>
    public decimal CampaignDiscount { get; set; }

    /// <summary>Total PWD/Senior discount applied in the bucket (kept separate — legal obligation, not a business choice).</summary>
    public decimal PwdSeniorDiscount { get; set; }
}

// ---- Admin Dashboard: New vs Returning Customers over time ----

public class NewVsReturningDto
{
    public List<NewVsReturningBucket> Buckets { get; set; } = new();
}

public class NewVsReturningBucket
{
    /// <summary>"Sep 2026" style label, oldest first.</summary>
    public string MonthLabel { get; set; } = string.Empty;

    /// <summary>Distinct customers whose FIRST-EVER completed order fell in this month.</summary>
    public int NewCustomerCount { get; set; }

    /// <summary>Distinct customers who ordered this month but had ordered before this month.</summary>
    public int ReturningCustomerCount { get; set; }
}

// ---- Manager Dashboard: Top Products by revenue ----

public class TopProductsResponseDto
{
    public List<TopProductItem> Products { get; set; } = new();
}

public class TopProductItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;

    /// <summary>Total units sold across all sizes in the period.</summary>
    public int UnitsSold { get; set; }

    /// <summary>Revenue attributable to this product in the period (sum of OrderItem.SubTotal).</summary>
    public decimal Revenue { get; set; }
}