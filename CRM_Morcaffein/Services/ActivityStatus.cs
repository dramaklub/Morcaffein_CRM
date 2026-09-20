namespace CRM_Morcaffein.winforms.Services
{
    /// <summary>
    /// Single source of truth for "is this customer inactive?" and for rendering
    /// human-readable relative times next to LastVisitDate.
    ///
    /// The 14-day threshold is used by TransactionController (promo eligibility: the
    /// "We Miss You" campaign targets customers inactive for more than 14 days) and by
    /// the Customers view (the Status line in the drawer). Both reference
    /// InactiveThresholdDays so the number can never drift between the two.
    /// </summary>
    public static class ActivityStatus
    {
        /// <summary>
        /// A customer is inactive once more than this many whole days have passed since
        /// their last visit.
        /// </summary>
        public const int InactiveThresholdDays = 14;

        /// <summary>
        /// True when lastVisitUtc is strictly more than InactiveThresholdDays days ago.
        /// </summary>
        public static bool IsInactive(DateTime lastVisitUtc) =>
            (DateTime.UtcNow - lastVisitUtc).TotalDays > InactiveThresholdDays;

        /// <summary>
        /// "Active" or "Inactive" as plain text — no badge, no color.
        /// </summary>
        public static string ToDisplayString(DateTime lastVisitUtc) =>
            IsInactive(lastVisitUtc) ? "Inactive" : "Active";

        /// <summary>
        /// Human-readable relative time, e.g. "just now", "5 minutes ago", "3 hours ago",
        /// "2 days ago", "6 weeks ago", "4 months ago", "over a year ago".
        ///
        /// Never returns an empty string. A default(DateTime) (year 0001) is treated as
        /// "never" rather than a nonsense negative duration.
        /// </summary>
        public static string FormatRelative(DateTime utcTimestamp)
        {
            if (utcTimestamp == default)
                return "never";

            var delta = DateTime.UtcNow - utcTimestamp;

            if (delta.TotalSeconds < 60)
                return "just now";

            if (delta.TotalMinutes < 60)
            {
                var m = (int)delta.TotalMinutes;
                return m == 1 ? "1 minute ago" : $"{m} minutes ago";
            }

            if (delta.TotalHours < 24)
            {
                var h = (int)delta.TotalHours;
                return h == 1 ? "1 hour ago" : $"{h} hours ago";
            }

            if (delta.TotalDays < 7)
            {
                var d = (int)delta.TotalDays;
                return d == 1 ? "1 day ago" : $"{d} days ago";
            }

            if (delta.TotalDays < 30)
            {
                var w = (int)(delta.TotalDays / 7);
                return w == 1 ? "1 week ago" : $"{w} weeks ago";
            }

            if (delta.TotalDays < 365)
            {
                var mo = (int)(delta.TotalDays / 30);
                return mo == 1 ? "1 month ago" : $"{mo} months ago";
            }

            var y = (int)(delta.TotalDays / 365);
            return y == 1 ? "over a year ago" : $"{y} years ago";
        }
    }
}