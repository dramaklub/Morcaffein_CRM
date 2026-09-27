namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// Response for GET /api/superadmin/dashboard. One combined payload —
/// the SuperAdmin console renders it as a single screen, so three
/// round trips would be wasteful.
/// </summary>
public class SuperAdminDashboardDto
{
    public SuperAdminDashboardSummary Summary { get; set; } = new();
    public List<CompaniesPerPlanPoint> CompaniesPerPlan { get; set; } = new();
    public List<RecentSubscriptionPoint> RecentSubscriptions { get; set; } = new();
}

public class SuperAdminDashboardSummary
{
    /// <summary>Total companies on the platform, active or not.</summary>
    public int TotalCompanies { get; set; }

    /// <summary>Companies with a non-null CurrentPlanId.</summary>
    public int ActiveSubscriptions { get; set; }

    /// <summary>Sum of the current plans' MonthlyPrice across active subscriptions.</summary>
    public decimal MonthlyRecurringRevenue { get; set; }
}

public class CompaniesPerPlanPoint
{
    /// <summary>PlanId, or null for the "No Plan" bucket.</summary>
    public int? PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int CompanyCount { get; set; }
}

public class RecentSubscriptionPoint
{
    public int SubscriptionId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal PlanMonthlyPrice { get; set; }
    public DateTime StartedAt { get; set; }
    public string? TermsAcceptedBy { get; set; }
}