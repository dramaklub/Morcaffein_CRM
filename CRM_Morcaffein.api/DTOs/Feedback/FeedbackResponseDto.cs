namespace CRM_Morcaffein.api.DTOs;

public class FeedbackResponseDto
{
    public int TicketId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int LoggedBy { get; set; }
    public string LoggedByName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Category { get; set; } = string.Empty;
    public string StaffNotes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DateLogged { get; set; }
    public int? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }
}