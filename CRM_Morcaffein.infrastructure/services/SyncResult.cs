namespace CRM_Morcaffein.infrastructure.services
{
    /// <summary>
    /// Outcome of one tenant's sync pass. One entry per table that had work
    /// to do this tick. Empty list = nothing to push (the common case).
    /// </summary>
    public class TenantSyncResult
    {
        public int CompanyId { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public int TotalRowsSynced { get; set; }
        public int TotalRowsSkipped { get; set; }
        public List<TableSyncCount> Tables { get; set; } = new();
    }

    public class TableSyncCount
    {
        public string TableName { get; set; } = string.Empty;
        public int Inserted { get; set; }
        public int Skipped { get; set; }
    }

    public class MasterSyncResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public int TotalRowsSynced { get; set; }
        public int TotalRowsSkipped { get; set; }
        public List<TableSyncCount> Tables { get; set; } = new();
    }
}