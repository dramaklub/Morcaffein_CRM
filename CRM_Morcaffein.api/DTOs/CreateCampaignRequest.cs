namespace CRM_Morcaffein.api.DTOs;

public class CreateCampaignRequest
{
    public string PromoName { get; set; } = string.Empty;
    public string PromoCategory { get; set; } = string.Empty;

    /// <summary>"Percentage", "FixedAmount", or "B1T1".</summary>
    public string DiscountType { get; set; } = "Percentage";

    public decimal DiscountValue { get; set; }

    /// <summary>"All", "Regular", "Silver", or "Gold".</summary>
    public string TargetTier { get; set; } = "All";

    public decimal? MinPurchaseAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    public int MaxRedemptionsPerCustomer { get; set; } = 1;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>Required for DiscountType "B1T1" — the product that must be in the cart.</summary>
    public int? RequiredProductId { get; set; }

    /// <summary>Required for DiscountType "B1T1" — the product given free.</summary>
    public int? RewardProductId { get; set; }

    /// <summary>The Manager creating or updating the campaign.</summary>
    public int MadeBy { get; set; }
}

public class UpdateCampaignRequest
{
    public string PromoName { get; set; } = string.Empty;
    public string PromoCategory { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public string TargetTier { get; set; } = "All";
    public decimal? MinPurchaseAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public int MaxRedemptionsPerCustomer { get; set; } = 1;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? RequiredProductId { get; set; }
    public int? RewardProductId { get; set; }

    /// <summary>The Manager making the edit. Used only for a role check, not stored.</summary>
    public int MadeBy { get; set; }
}