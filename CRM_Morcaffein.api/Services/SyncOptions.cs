namespace CRM_Morcaffein.infrastructure.services
{
    /// <summary>
    /// Configuration for the background sync worker. Bound from the "Sync"
    /// section of appsettings.json.
    ///
    ///   Enabled          — master on/off. When false, the worker still
    ///                      runs but does nothing each tick. Useful for
    ///                      demoing offline behavior without pulling the
    ///                      network cable.
    ///   IntervalSeconds  — how often the worker ticks, in seconds. 20 is
    ///                      the demo default; production would use 30-60.
    /// </summary>
    public class SyncOptions
    {
        public bool Enabled { get; set; } = true;
        public int IntervalSeconds { get; set; } = 20;
    }
}