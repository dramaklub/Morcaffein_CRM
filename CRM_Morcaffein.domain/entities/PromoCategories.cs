namespace CRM_Morcaffein.domain.entities;

/// <summary>
/// The set of promo-category strings the system understands. Referenced by
/// TransactionController (promo eligibility rules) and by the campaign create form
/// (so the dropdown items come from one place). Previously these were raw string
/// literals scattered across the codebase.
///
/// Not every category has special gating logic — "Seasonal" and "Loyalty" are
/// simply descriptive; "Birthday" and "Inactive" trigger eligibility checks.
/// </summary>
public static class PromoCategories
{
    /// <summary>General discount with no gating beyond dates/tier/spend.</summary>
    public const string Seasonal = "Seasonal";

    /// <summary>Birthday-today eligibility gate.</summary>
    public const string Birthday = "Birthday";

    /// <summary>Inactive-14-days eligibility gate.</summary>
    public const string Inactive = "Inactive";

    /// <summary>Tier-based loyalty promo.</summary>
    public const string Loyalty = "Loyalty";

    /// <summary>Wildcard tier value — "this promo applies to any tier."</summary>
    public const string AllTiers = "All";
}