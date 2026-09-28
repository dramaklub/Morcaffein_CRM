using System.Collections.Concurrent;

namespace CRM_Morcaffein.infrastructure.services
{
    /// <summary>
    /// In-memory snapshot of the last sync pass per company, plus the last
    /// master sync. Written by the worker after each tick, read by
    /// SyncController for the topbar indicator.
    ///
    /// Deliberately not persisted — this is process-local state that's
    /// rebuilt on each API boot. If the API restarts, the indicator shows
    /// "unknown" until the first tick.
    /// </summary>
    public class SyncStatus
    {
        public bool IsOnline { get; set; } = true;
        public DateTime? LastSyncedAt { get; set; }
        public string? LastError { get; set; }
        public int PendingCount { get; set; }
        public string? CloudHost { get; set; }
    }

    /// <summary>
    /// Singleton service holding the last sync state for every company.
    /// Thread-safe — the worker writes, HTTP requests read concurrently.
    /// </summary>
    public class SyncStatusStore
    {
        private readonly ConcurrentDictionary<int, SyncStatus> _byCompany = new();
        private SyncStatus _master = new();

        public SyncStatus ForCompany(int companyId)
            => _byCompany.GetOrAdd(companyId, _ => new SyncStatus());

        public void UpdateCompany(int companyId, Action<SyncStatus> mutate)
        {
            var status = ForCompany(companyId);
            lock (status)
            {
                mutate(status);
            }
        }

        public SyncStatus Master => _master;

        public void UpdateMaster(Action<SyncStatus> mutate)
        {
            lock (_master)
            {
                mutate(_master);
            }
        }
    }
}