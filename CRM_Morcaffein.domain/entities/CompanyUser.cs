using System;

namespace CRM_Morcaffein.domain.entities;

/// <summary>
/// Routing record mapping a username to the tenant DB whose Credentials
/// table holds the real password hash. Lives in the master DB.
///
/// Login is POST /api/auth/login with just username+password, no tenant
/// in the URL — the server needs to know which tenant DB to query before
/// it can verify anything, and this table is the answer. Password hashes
/// stay tenant-side, so there's one source of truth for the secret.
/// </summary>
public class CompanyUser
{
    public int CompanyUserId { get; set; }

    /// <summary>The tenant this username resolves to.</summary>
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>
    /// Globally unique across all tenants — two companies can't both have
    /// a user named "renz.arriola". The seeder offsets its RNG by companyId,
    /// so this naturally holds today. If per-tenant uniqueness is ever
    /// needed, change the index to (CompanyId, Username) and add tenant
    /// selection to the login flow.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}