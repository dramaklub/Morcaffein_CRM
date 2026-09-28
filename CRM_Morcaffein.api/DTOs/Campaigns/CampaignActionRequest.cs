namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// Body for campaign action endpoints (activate/deactivate). Carries only the actor's
/// employee ID so the server can verify the caller is a Manager.
/// </summary>
public class CampaignActionRequest
{
    public int PerformedBy { get; set; }
}