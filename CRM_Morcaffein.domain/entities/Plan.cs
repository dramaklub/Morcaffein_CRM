using System;

namespace CRM_Morcaffein.domain.entities;

/// <summary>
/// A subscription tier. SuperAdmin creates and edits these. Companies reference
/// one via Company.CurrentPlanId — that's what determines which modules the
/// tenant's Admin sees.
///
/// ModulesCsv is a comma-separated list of module keys. Deliberately not a
/// separate table — for three plans and a fixed set of modules, a join table
/// is overkill, and the client wants the list in one string anyway.
/// </summary>
public class Plan
{
    public int PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>"DataCollection,Transactions,Feedback,Retention,BI,BranchManagement"</summary>
    public string ModulesCsv { get; set; } = string.Empty;

    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSynced { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}