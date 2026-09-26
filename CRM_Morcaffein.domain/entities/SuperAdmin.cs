using System;

namespace CRM_Morcaffein.domain.entities;

/// <summary>
/// A SuperAdmin account — the developer-side operator of the SaaS platform.
/// Does NOT belong to any tenant. Logs into the same app but a different shell
/// (FrmSuperAdminMain) with its own nav (Companies, Plans, Subscriptions).
///
/// Deliberately a separate table from the tenant-scoped Credential table —
/// SuperAdmins aren't employees of any company, and their login should never
/// be resolvable via a tenant URL.
/// </summary>
public class SuperAdmin
{
    public int SuperAdminId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}