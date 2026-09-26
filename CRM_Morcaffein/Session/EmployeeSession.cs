using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.domain.entities;

namespace CRM_Morcaffein.Session
{
    /// <summary>
    /// Nothing like this existed anywhere in the codebase yet. Order.CreatedBy,
    /// Feedback.LoggedBy, and Campaign.MadeBy/ApprovedBy all need "who is currently
    /// logged in" — this is the single source of truth for that, set once by FrmLogin
    /// on a successful Credential.VerifyPassword(...), and read by every downstream
    /// ViewModel via the static Current instance for the lifetime of the app process.
    /// </summary>
    public sealed class EmployeeSession
    {
        public static EmployeeSession? Current { get; private set; }

        public Employee Employee { get; }

        private EmployeeSession(Employee employee)
        {
            Employee = employee;
        }

        public static EmployeeSession Start(Employee employee)
        {
            Current = new EmployeeSession(employee);
            return Current;
        }

        public static void End() => Current = null;

        public int EmployeeId => Employee.EmployeeId;
        public string DisplayName => Employee.FullName;

        /// <summary>
        /// The tenant's entitlements (module list) for the current session.
        /// Populated once at login by FrmLogin. Null until that runs, or if
        /// the login flow failed to fetch them.
        /// </summary>
        public EntitlementsResponseDto? Entitlements { get; set; }

        /// <summary>
        /// True if the tenant's plan includes the given module key.
        /// Module keys: "Transactions", "DataCollection", "Feedback",
        /// "Dashboard", "BusinessIntelligence", "Actions", "Branching".
        ///
        /// If Entitlements is null (not loaded yet, or failed to load),
        /// returns false — conservative default, so a failed fetch doesn't
        /// accidentally grant full access.
        /// </summary>
        public bool HasModule(string moduleKey)
        {
            if (Entitlements?.Modules is null) return false;
            return Entitlements.Modules.Contains(moduleKey, StringComparer.OrdinalIgnoreCase);
        }
        /// <summary>
        /// The branch the current user is looking at. Set by the topbar selector in
        /// FrmMain. null = "All Branches" (company-wide view). Baristas are pinned to
        /// their own branch and never see the selector.
        /// </summary>
        public int? CurrentBranchId { get; set; }
    }
}
