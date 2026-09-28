namespace CRM_Morcaffein.api.DTOs;

public class PointLedgerResponseDto
{
    public int PointLedgerId { get; set; }
    public int CustomerId { get; set; }
    public int PointsChange { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public int? PerformedBy { get; set; }
    public DateTime Timestamp { get; set; }
}