using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace lms_api.Services;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}

public interface IEmailService
{
    Task SendInviteEmailAsync(string email, string courseTitle, string inviteToken);
    Task SendCompletionEmailAsync(string email, string userName, string courseTitle);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _settings = configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
    }

    public async Task SendInviteEmailAsync(string email, string courseTitle, string inviteToken)
    {
        string frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
        string registrationLink = $"{frontendUrl}/register?token={inviteToken}";

        MimeMessage message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = $"You've been invited to: {courseTitle}";

        BodyBuilder bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
                    <h2 style=""color: #2563eb;"">You're Invited!</h2>
                    <p>You have been invited to complete the following course:</p>
                    <h3 style=""color: #1e40af;"">{System.Net.WebUtility.HtmlEncode(courseTitle)}</h3>
                    <p>Click the button below to create your account and get started:</p>
                    <a href=""{registrationLink}""
                       style=""display: inline-block; background-color: #2563eb; color: white;
                               padding: 12px 24px; text-decoration: none; border-radius: 6px;
                               font-weight: bold; margin: 16px 0;"">
                        Accept Invitation
                    </a>
                    <p style=""color: #6b7280; font-size: 14px;"">
                        This invitation link will expire in 7 days.<br/>
                        If you cannot click the button, copy and paste this link:<br/>
                        <a href=""{registrationLink}"">{registrationLink}</a>
                    </p>
                </div>",
            TextBody = $"You've been invited to complete '{courseTitle}'.\n\nRegister here: {registrationLink}\n\nThis link expires in 7 days."
        };

        message.Body = bodyBuilder.ToMessageBody();

        await SendAsync(message);
    }

    public async Task SendCompletionEmailAsync(string email, string userName, string courseTitle)
    {
        MimeMessage message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = $"Certificate of Completion: {courseTitle}";

        BodyBuilder bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
                    <div style=""background: linear-gradient(135deg, #1e40af, #2563eb); padding: 32px; text-align: center; border-radius: 8px 8px 0 0;"">
                        <h1 style=""color: white; margin: 0;"">Certificate of Completion</h1>
                    </div>
                    <div style=""background: #f8fafc; padding: 32px; border: 1px solid #e2e8f0; border-top: none; border-radius: 0 0 8px 8px;"">
                        <p style=""font-size: 18px; color: #374151;"">Congratulations, <strong>{System.Net.WebUtility.HtmlEncode(userName)}</strong>!</p>
                        <p style=""color: #6b7280;"">You have successfully completed:</p>
                        <h2 style=""color: #1e40af; border-bottom: 2px solid #2563eb; padding-bottom: 8px;"">
                            {System.Net.WebUtility.HtmlEncode(courseTitle)}
                        </h2>
                        <p style=""color: #6b7280; font-size: 14px;"">
                            Completed on: {DateTime.UtcNow:MMMM dd, yyyy}
                        </p>
                        <p style=""color: #374151;"">
                            This certificate confirms that you have watched the required content and
                            passed the assessment for this course.
                        </p>
                    </div>
                </div>",
            TextBody = $"Congratulations {userName}! You have successfully completed '{courseTitle}' on {DateTime.UtcNow:MMMM dd, yyyy}."
        };

        message.Body = bodyBuilder.ToMessageBody();

        await SendAsync(message);
    }

    private async Task SendAsync(MimeMessage message)
    {
        if (string.IsNullOrEmpty(_settings.Host) || string.IsNullOrEmpty(_settings.Username))
        {
            _logger.LogWarning("Email settings not configured. Skipping email send to {To}", message.To);
            return;
        }

        try
        {
            using SmtpClient client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation("Email sent to {To} with subject '{Subject}'", message.To, message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", message.To);
            // Don't throw — email failure should not break the request
        }
    }
}
