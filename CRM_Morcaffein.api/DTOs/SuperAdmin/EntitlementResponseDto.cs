namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// What the current tenant's plan entitles them to. Returned by
/// GET /api/tenant/{companyId}/me/entitlements and cached client-side
/// after login. The client uses this to filter navigation and gate views;
/// the server ALSO checks this independently on gated endpoints, so hiding
/// a nav item client-side is a UX convenience, not a security boundary.
/// </summary>
public class EntitlementsResponseDto
{
    /// <summary>
    /// Modules the tenant's current plan includes. Possible values:
    /// "DataCollection", "Transactions", "Feedback", "Retention",
    /// "BI", "BranchManagement".
    /// </summary>
    public List<string> Modules { get; set; } = new();

    /// <summary>
    /// Null if the company has no plan assigned. Populated for convenience
    /// so the client can display "on the Full plan" somewhere if it wants.
    /// </summary>
    public int? PlanId { get; set; }
    public string? PlanName { get; set; }
}

/// <summary>
/// Response for GET /api/tenant/{companyId}/me/terms. Read-only — the
/// tenant's Admin can see the terms of their current plan plus the
/// acceptance metadata from the Subscription row.
///
/// HasTerms = false means the tenant has no plan assigned, or the plan
/// has no T&C document. The client shows an empty-state message rather
/// than a broken viewer.
/// </summary>
public class TenantTermsDto
{
    public bool HasTerms { get; set; }

    public int? PlanId { get; set; }
    public string? PlanName { get; set; }

    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? Version { get; set; }
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>Who accepted the T&C when this subscription started (usually the SuperAdmin on the tenant's behalf).</summary>
    public string? AcceptedBy { get; set; }
    public DateTime? AcceptedAt { get; set; }
}