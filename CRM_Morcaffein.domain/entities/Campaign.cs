using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM_Morcaffein.domain.entities;

public class Campaign : BaseEntity
{
    public int CampaignId { get; set; }
    public string PromoName { get; set; } = string.Empty;
    public string PromoCategory { get; set; } = string.Empty;

    public string DiscountType { get; set; } = "Percentage";

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }

    public string TargetTier { get; set; } = "All";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinPurchaseAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxDiscountAmount { get; set; }

    public int MaxRedemptionsPerCustomer { get; set; } = 1;

    public int? RequiredProductId { get; set; }
    public int? RewardProductId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int MadeBy { get; set; }

    /// <summary>
    /// Why this campaign exists — free text from the Manager at creation/edit time.
    /// Required on new campaigns so the Admin has justification to review during
    /// approval. Distinct from DecisionNotes, which is the Admin's response.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// The customer-facing message that goes into the retention email when
    /// this campaign is approved. Distinct from Reason (internal, why the
    /// Manager created it) — this is the text the customer actually reads.
    /// Nullable at the DB level so pre-existing campaigns survive the
    /// migration; required by the API on create/update going forward.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The branch this campaign targets.
    ///   null  = company-wide (redeemable at every branch)
    ///   value = scoped to that specific branch
    /// Pinned Managers can only create campaigns for their own branch
    /// (never null). All-branch Managers can create either.
    /// </summary>
    public int? BranchId { get; set; }
    public virtual Branch? Branch { get; set; }

    public string Status { get; set; } = "Active";
    public int? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }

    public virtual Employee? Creator { get; set; }
    public virtual Employee? Decider { get; set; }
    public virtual Product? RequiredProduct { get; set; }
    public virtual Product? RewardProduct { get; set; }
    public virtual ICollection<CampaignRedeem> CampaignRedeems { get; set; } = new List<CampaignRedeem>();
}
