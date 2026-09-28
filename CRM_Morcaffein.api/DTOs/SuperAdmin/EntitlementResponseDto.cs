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