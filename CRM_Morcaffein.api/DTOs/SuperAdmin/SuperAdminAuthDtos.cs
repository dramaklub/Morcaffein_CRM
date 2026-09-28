namespace CRM_Morcaffein.api.DTOs;

public class SuperAdminLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SuperAdminLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int SuperAdminId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}