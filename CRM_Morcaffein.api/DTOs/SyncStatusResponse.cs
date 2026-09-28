namespace CRM_Morcaffein.api.DTOs;

/// <summary>
/// Response for GET /api/tenant/{companyId}/sync/status. Read by the
/// client's topbar indicator to drive the green/yellow/red dot.
/// </summary>
public class SyncStatusResponse
{
    public int CompanyId { get; set; }

    /// <summary>
    /// True if the last tick reported no errors for this company (and for
    /// master). False means the topbar dot goes red.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Number of unsynced rows in the tenant DB. 0 = everything's been
    /// pushed. Greater than 0 = yellow dot (waiting to sync). -1 = count
    /// query failed on the worker (rare); treat as "unknown".
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>Null if no successful sync has run yet since API boot.</summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Null when the last attempt succeeded. Contains the inner exception
    /// message when it failed. The client uses this for the toast.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// The cloud host this company syncs to. Null if no cloud replica is
    /// configured. Shown in the indicator's tooltip.
    /// </summary>
    public string? CloudHost { get; set; }
}