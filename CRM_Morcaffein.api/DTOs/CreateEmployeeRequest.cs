namespace CRM_Morcaffein.api.DTOs;

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = "Barista";

    // Credential fields — required (every employee gets a login).
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}