using System;

namespace CRM_Morcaffein.domain.entities;

public class Credential : BaseEntity
{
    public int CredentialId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public int FailedLoginAttempts { get; set; }
    public bool IsLocked { get; set; }

    public virtual Employee? Employee { get; set; }
}
