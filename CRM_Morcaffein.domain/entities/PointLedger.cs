using System;

namespace CRM_Morcaffein.domain.entities;

public class PointLedger : BaseEntity
{
    public int PointLedgerId { get; set; }
    public int CustomerId { get; set; }
    public int PointsChange { get; set; } // Positive for earned/adjusted, negative for redeemed
    public string Type { get; set; } = string.Empty; // "Earned", "Redeemed", "Adjustment", "Expired"
    public string Reason { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public int? PerformedBy { get; set; } // Employee who performed/approved this change, when applicable
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public virtual Customer? Customer { get; set; }
    public virtual Order? Order { get; set; }
    public virtual Employee? PerformedByEmployee { get; set; }
}
