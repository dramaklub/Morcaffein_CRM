namespace CRM_Morcaffein.api.DTOs;

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Same semantics as CreateEmployeeRequest.BranchId:
    ///   - Barista: required
    ///   - Manager: optional (null = all-branch)
    ///   - Admin:   must be null
    /// </summary>
    public int? BranchId { get; set; }

    public string? Username { get; set; }
}