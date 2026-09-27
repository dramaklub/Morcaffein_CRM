namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// The Admin account assigned to a company — the primary login the
/// SuperAdmin provisions. One per company; the tenant's own Manage Users
/// surface manages everyone else.
///
/// HasAdmin=false means the company has no Admin yet, and the SuperAdmin
/// console should offer to create one.
/// </summary>
public class SuperAdminAdminAccountDto
{
    public bool HasAdmin { get; set; }
    public int? EmployeeId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? ContactNumber { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsLocked { get; set; }
    public DateTime? LastLoginDate { get; set; }
}

/// <summary>
/// Create request for a company's first Admin. All fields required except
/// email. The tenant's Credentials + Employees rows are created along with
/// a master CompanyUser routing row (so the account can log in immediately).
/// </summary>
public class CreateCompanyAdminRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
}

/// <summary>
/// Update request for an existing Admin. Both fields are optional; null or
/// blank means "leave unchanged." Sending a non-empty NewPassword resets
/// the password, clears lockout, and zeroes FailedLoginAttempts.
/// </summary>
public class UpdateCompanyAdminRequest
{
    public string? Username { get; set; }
    public string? NewPassword { get; set; }
}