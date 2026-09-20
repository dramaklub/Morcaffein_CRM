namespace CRM_Morcaffein.api.DTOs;

public class ResolveFeedbackRequest
{
    /// <summary>The Manager making the decision. Server verifies the role.</summary>
    public int DecidedBy { get; set; }

    /// <summary>
    /// Bonus points awarded as part of the resolution. 0 means "resolved, no gesture"
    /// (the dismiss case). Maximum enforced server-side at 50.
    /// </summary>
    public int BonusPoints { get; set; }

    /// <summary>
    /// Optional note from the Manager. Goes into Feedback.DecisionNotes only; the
    /// PointLedger row (when BonusPoints > 0) uses a fixed auto-generated Reason so
    /// the points grid stays scannable.
    /// </summary>
    public string? Notes { get; set; }
}