namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// Body for the Approve / Reject endpoints on campaigns. Mirrors the shape of
/// DecidePointAdjustmentDto — DecidedBy is the Admin making the decision, Notes
/// carries an optional approval note or a required rejection reason.
/// </summary>
public class DecideCampaignRequest
{
    public int DecidedBy { get; set; }

    /// <summary>Optional on approve; required on reject.</summary>
    public string? Notes { get; set; }
}