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
    }
}
