namespace CRM_Morcaffein.api.DTOs;

public class LogFeedbackRequest
{
    public int CustomerId { get; set; }
    public int LoggedBy { get; set; }
    public int Rating { get; set; }
    public string Category { get; set; } = string.Empty;
    public string StaffNotes { get; set; } = string.Empty;
}