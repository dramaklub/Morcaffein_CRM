namespace CRM_Morcaffein.api.Services
{
    /// <summary>
    /// Abstraction over outbound email. One method — everything the API
    /// needs is "send this subject + body to this address."
    ///
    /// Implementations:
    ///   SmtpEmailService  — real MailKit SMTP (production shape)
    ///
    /// The interface exists so tests / future sandbox implementations can
    /// swap in without touching the callers (CampaignController).
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a plain-text email. Returns (success, errorMessage).
        /// Never throws — failures are reported via the tuple so the caller
        /// (e.g. campaign approval) can decide whether to surface the error
        /// or continue silently.
        /// </summary>
        Task<(bool Success, string? Error)> SendAsync(
            string toAddress,
            string subject,
            string plainTextBody,
            CancellationToken ct = default);
    }
}