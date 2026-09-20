namespace CRM_Morcaffein.api.DTOs;

public class CreatePointAdjustmentDto
{
    public int CustomerId { get; set; }
    public int PointsChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedBy { get; set; }
}