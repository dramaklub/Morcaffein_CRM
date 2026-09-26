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

    /// <summary>
    /// Count of feedback tickets with Status = "Pending" — the Manager's
    /// actionable queue. Feeds the "Feedback" KPI pod on the Basic-tier
    /// Manager dashboard (which shows this count and navigates to the
    /// Feedback tab on click). Also useful as a general signal on the
    /// ratings card.
    /// </summary>
    public int PendingFeedbackCount { get; set; }

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

    /// <summary>
    /// Total active (non-deactivated) employees for this tenant. Feeds the
    /// "Employees" KPI pod on the Basic-tier Admin dashboard. Lives on the
    /// audit DTO for convenience — the audit endpoint already runs on every
    /// Admin dashboard load, so there's no extra round trip.
    /// </summary>
    public int ActiveEmployeeCount { get; set; }
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

// ---- Admin Dashboard: Customer Visit Frequency Distribution ----

public class VisitFrequencyDto
{
    public List<VisitFrequencyBucket> Buckets { get; set; } = new();

    /// <summary>Total distinct customers who placed at least one order.</summary>
    public int TotalCustomers { get; set; }
}

public class VisitFrequencyBucket
{
    /// <summary>Display label — "1 visit", "2–3 visits", etc.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Number of distinct customers whose order count falls in this bucket.</summary>
    public int CustomerCount { get; set; }
}

// ---- Transactions Ledger (raw row export for the PDF report) ----

public class TransactionLedgerDto
{
    public List<TransactionLedgerRow> Rows { get; set; } = new();

    /// <summary>Aggregate figures for the footer row of the report table.</summary>
    public decimal TotalSubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalPwdSenior { get; set; }
    public decimal TotalNet { get; set; }
    public int TotalPointsEarned { get; set; }
}

public class TransactionLedgerRow
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal PwdSeniorDiscount { get; set; }
    public decimal NetAmount { get; set; }
    public int PointsEarned { get; set; }
}

// ---- Basic-tier Admin Dashboard: Recent Employees card ----

public class RecentEmployeesDto
{
    public List<RecentEmployeeItem> Employees { get; set; } = new();
}

public class RecentEmployeeItem
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---- Basic-tier Admin Dashboard: Recent Feedback card ----

public class RecentFeedbackDto
{
    public List<RecentFeedbackItem> Feedbacks { get; set; } = new();
}

public class RecentFeedbackItem
{
    public int TicketId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DateLogged { get; set; }
}