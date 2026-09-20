namespace CRM_Morcaffein.api.DTOs;

public class EmployeeResponseDto
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Credential info — null if the employee has no login yet.
    public string? Username { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool HasCredential { get; set; }
}