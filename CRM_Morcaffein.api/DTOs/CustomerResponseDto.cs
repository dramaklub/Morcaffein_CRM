namespace CRM_Morcaffein.api.DTOs;

public class CustomerResponseDto
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
    public DateTime LastVisitDate { get; set; }
    public int PointsBalance { get; set; }
    public string Tier { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}