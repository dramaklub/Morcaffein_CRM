namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// Summary row for the T&C list. Body is NOT included — the list view
/// shows cards with title/version, and the full body is fetched per-card
/// when the user clicks one.
/// </summary>
public class TermsSummaryDto
{
    public int TermsId { get; set; }
    public int? PlanId { get; set; }
    public string? PlanName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Full T&C row, including the body text.
/// </summary>
public class TermsDetailDto
{
    public int TermsId { get; set; }
    public int? PlanId { get; set; }
    public string? PlanName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}