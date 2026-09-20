namespace CRM_Morcaffein.api.DTOs;

public class PointAdjustmentResponseDto
{
    public int PointAdjustmentRequestId { get; set; }
    public int CustomerId { get; set; }
    public int PointsChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ApprovedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNotes { get; set; }
}