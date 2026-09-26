using System;

namespace CRM_Morcaffein.domain.entities;

/// <summary>
/// A record of a company's subscription to a plan. Every time the SuperAdmin
/// assigns a plan to a company — whether it's the first assignment, an upgrade,
/// a downgrade, or a renewal — a Subscription row is created.
///
/// The Company.CurrentPlanId field is the fast-path lookup (which plan is
/// active right now). The Subscription table is the history: what plan was
/// active from when to when, and who agreed to it.
///
/// "Active" is a computed concept: exactly one Subscription per company should
/// have EndedAt == null at any given time. That row is the current subscription.
/// When a new plan is assigned:
///   - the previous active row's EndedAt is stamped with now
///   - a new row is inserted with StartedAt = now and EndedAt = null
/// </summary>
public class Subscription
{
    public int SubscriptionId { get; set; }

    /// <summary>The company this subscription belongs to.</summary>
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>The plan the company is subscribed to during this period.</summary>
    public int PlanId { get; set; }
    public Plan? Plan { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null while this is the current subscription. Set when superseded.</summary>
    public DateTime? EndedAt { get; set; }  

    /// <summary>
    /// Who agreed to the T&C when this subscription started. For the demo,
    /// this is the SuperAdmin's name (they register the shop owner's account
    /// on the shop's behalf). In a real deployment it would be the shop
    /// owner's name/signature, captured separately.
    /// </summary>
    public string? TermsAcceptedBy { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}