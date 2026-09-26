using System;

namespace CRM_Morcaffein.domain.entities;

public class Feedback : BaseEntity
{
    public int TicketId { get; set; }
    public int CustomerId { get; set; }
    public int LoggedBy { get; set; }
    public int Rating { get; set; } // CSAT score 1 to 5
    public string Category { get; set; } = string.Empty;
    public string StaffNotes { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // "Pending", "Resolved", "Dismissed"
    public DateTime DateLogged { get; set; } = DateTime.UtcNow;

    // Decision attribution — populated only once Status leaves "Pending".
    public int? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }

    public int BranchId { get; set; }
    public virtual Branch? Branch { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual Employee? LoggedByEmployee { get; set; }
    public virtual Employee? DecidedByEmployee { get; set; }
}
