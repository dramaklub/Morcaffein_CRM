namespace CRM_Morcaffein.api.DTOs;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}