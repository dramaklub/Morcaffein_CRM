namespace CRM_Morcaffein.api.DTOs;

public class CampaignResponseDto
{
    public int CampaignId { get; set; }
    public string PromoName { get; set; } = string.Empty;
    public string PromoCategory { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public string TargetTier { get; set; } = "All";
    public decimal? MinPurchaseAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int MaxRedemptionsPerCustomer { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;

    public int? BranchId { get; set; }
    public string? BranchName { get; set; }   // resolved server-side; "All Branches" if null

    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }

    // B1T1 targeting — set only when DiscountType is "B1T1". Names are resolved
    // server-side so the client doesn't need a second product lookup to display them.
    public int? RequiredProductId { get; set; }
    public string? RequiredProductName { get; set; }
    public int? RewardProductId { get; set; }
    public string? RewardProductName { get; set; }

    // Who created the campaign. CreatedByName is resolved from the Creator nav property
    // so the client doesn't have to do a second lookup.
    public int MadeBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    // Decision attribution — populated once the campaign leaves "Pending".
    public int? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }
}