using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CRM_Morcaffein.api.Services
{
    /// <summary>
    /// MailKit-based SMTP sender. Reads config from appsettings "MailKit"
    /// section. When Enabled is false, logs "would have sent" and returns
    /// success without touching the network — useful for development and
    /// for a demo where you don't want to actually send.
    ///
    /// All failures are caught and returned as (false, errorMessage). Callers
    /// decide whether to surface the error. This service never throws.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly bool _enabled;
        private readonly string _host;
        private readonly int _port;
        private readonly bool _useStartTls;
        private readonly string _username;
        private readonly string _password;
        private readonly string _fromName;
        private readonly string _fromAddress;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _logger = logger;

            var section = config.GetSection("MailKit");
            _enabled = bool.TryParse(section["Enabled"], out var e) && e;
            _host = section["Host"] ?? "";
            _port = int.TryParse(section["Port"], out var p) ? p : 587;
            _useStartTls = !bool.TryParse(section["UseStartTls"], out var tls) || tls;   // default true
            _username = section["Username"] ?? "";
            _password = section["Password"] ?? "";
            _fromName = section["FromName"] ?? "Morcaffein";
            _fromAddress = section["FromAddress"] ?? "";
        }

        public async Task<(bool Success, string? Error)> SendAsync(
            string toAddress,
            string subject,
            string plainTextBody,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
                return (false, "Recipient address is empty.");

            if (!_enabled)
            {
                _logger.LogInformation(
                    "[MailKit disabled] Would have sent to {To} — Subject: {Subject}",
                    toAddress, subject);
                return (true, null);
            }

            if (string.IsNullOrWhiteSpace(_host) || string.IsNullOrWhiteSpace(_fromAddress))
                return (false, "MailKit configuration is incomplete (Host or FromAddress missing).");

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromAddress));
                message.To.Add(MailboxAddress.Parse(toAddress));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = plainTextBody };

                using var client = new SmtpClient();

                var socketOptions = _useStartTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.SslOnConnect;

                await client.ConnectAsync(_host, _port, socketOptions, ct);

                // Only authenticate if credentials are provided. Some
                // relay-only servers (internal school SMTP) allow unauth
                // sends from a trusted subnet.
                if (!string.IsNullOrWhiteSpace(_username))
                    await client.AuthenticateAsync(_username, _password, ct);

                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);

                _logger.LogInformation(
                    "Email sent to {To} — Subject: {Subject}",
                    toAddress, subject);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to send email to {To} — Subject: {Subject}",
                    toAddress, subject);

                return (false, ex.Message);
            }
        }
    }
}