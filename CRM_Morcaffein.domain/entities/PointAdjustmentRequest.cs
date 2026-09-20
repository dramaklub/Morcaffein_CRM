using System;

namespace CRM_Morcaffein.domain.entities;

/// <summary>
/// A manager-initiated request to manually add or deduct a customer's loyalty points,
/// pending admin approval.
/// </summary>
public class PointAdjustmentRequest : BaseEntity
{
    public int PointAdjustmentRequestId { get; set; }
    public int CustomerId { get; set; }
    public int PointsChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
    public int? ApprovedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual Employee? Requester { get; set; }
    public virtual Employee? Approver { get; set; }
}
