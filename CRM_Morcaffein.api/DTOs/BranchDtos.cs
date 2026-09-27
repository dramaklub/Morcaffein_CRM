namespace CRM_Morcaffein.api.DTOs;

public class CreateBranchRequest
{
    public string BranchName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class UpdateBranchRequest
{
    public string BranchName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}