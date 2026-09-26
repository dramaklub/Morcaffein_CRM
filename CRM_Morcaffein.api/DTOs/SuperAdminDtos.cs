namespace CRM_Morcaffein.api.DTOs;

// ---- Company listing for the SuperAdmin console ----

public class SuperAdminCompanyDto
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Resolved from Company.CurrentPlan (null if no plan assigned yet)
    public int? CurrentPlanId { get; set; }
    public string? CurrentPlanName { get; set; }
}

// ---- Plan listing ----

public class SuperAdminPlanDto
{
    public int PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModulesCsv { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; }
}

// ---- SuperAdmin: plan assignment ----

public class AssignPlanRequest
{
    /// <summary>The plan to assign. Must exist and be active.</summary>
    public int PlanId { get; set; }

    /// <summary>
    /// Who agreed to the T&C. For the demo, this is the SuperAdmin's name —
    /// they register the shop owner's account on their behalf.
    /// </summary>
    public string TermsAcceptedBy { get; set; } = string.Empty;
}

// ---- SuperAdmin: subscription history ----

public class SubscriptionResponseDto
{
    public int SubscriptionId { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanModulesCsv { get; set; } = string.Empty;
    public decimal PlanMonthlyPrice { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? TermsAcceptedBy { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }
}