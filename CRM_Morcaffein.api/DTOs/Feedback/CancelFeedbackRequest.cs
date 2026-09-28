namespace CRM_Morcaffein.api.DTOs;

public class CancelFeedbackRequest
{
    public int CancelledBy { get; set; }
    public string? Reason { get; set; }
}