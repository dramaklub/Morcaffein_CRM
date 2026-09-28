namespace CRM_Morcaffein.api.DTOs;

public class DecidePointAdjustmentDto
{
    // The admin/approver making the decision.
    public int DecidedBy { get; set; }

    // For approve: optional notes.
    // For reject: required rejection reason.
    public string? Notes { get; set; }
}