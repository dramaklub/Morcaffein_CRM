namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// Response from POST /campaigns/{id}/approve. Wraps the campaign DTO plus
/// the outcome of the retention-email send that runs after the campaign
/// goes Active. EmailSent is false on SMTP failure — the approve still
/// succeeded, and EmailError carries the reason for display.
/// </summary>
public class ApproveCampaignResponse
{
    public CampaignResponseDto Campaign { get; set; } = new();

    /// <summary>True if the email was sent (or if MailKit is disabled and
    /// the send was skipped as a no-op).</summary>
    public bool EmailSent { get; set; }

    /// <summary>Populated when EmailSent is false. Null otherwise.</summary>
    public string? EmailError { get; set; }
}