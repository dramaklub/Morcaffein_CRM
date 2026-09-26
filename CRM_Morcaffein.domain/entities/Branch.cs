namespace CRM_Morcaffein.domain.entities;

/// <summary>
/// A physical branch (location) of a tenant's coffee shop. Tenant C (full
/// subscription) has branch management; other tiers have a single implicit
/// branch that everything is scoped to.
///
/// Employees reference a branch via nullable FK (Admins see all branches).
/// Orders, Campaigns, and Feedbacks reference a branch via non-nullable FK
/// (those events physically happen at a branch).
/// Customers are company-wide — no branch FK.
/// </summary>
public class Branch : BaseEntity
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}