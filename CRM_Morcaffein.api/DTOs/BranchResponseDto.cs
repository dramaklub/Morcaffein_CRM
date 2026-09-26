namespace CRM_Morcaffein.api.DTOs;

public class BranchResponseDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}