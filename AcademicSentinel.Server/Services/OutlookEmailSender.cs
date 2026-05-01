using System.Net;
using System.Net.Mail;

namespace AcademicSentinel.Server.Services;

public class OutlookEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public OutlookEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetCodeAsync(string toEmail, string code)
    {
        var configuredHost = _configuration["Email:SmtpHost"];
        var from = _configuration["Email:From"] ?? string.Empty;
        var username = _configuration["Email:Username"] ?? string.Empty;
        var password = _configuration["Email:Password"] ?? string.Empty;
        var portValue = _configuration["Email:SmtpPort"];

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Email settings are not configured. Set Email:From, Email:Username and Email:Password via User Secrets or environment variables.");
        }

        var host = string.IsNullOrWhiteSpace(configuredHost)
            ? ResolveSmtpHostFromEmail(username)
            : configuredHost;

        int port = 587;
        if (!string.IsNullOrWhiteSpace(portValue) && int.TryParse(portValue, out var parsedPort))
        {
            port = parsedPort;
        }

        using var message = new MailMessage(from, toEmail)
        {
            Subject = "AcademicSentinel Password Reset Code",
            Body = $"Your AcademicSentinel verification code is: {code}\n\nThis code will expire in 10 minutes.",
            IsBodyHtml = false
        };

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password)
        };

        try
        {
            await smtp.SendMailAsync(message);
        }
        catch (SmtpException smtpEx)
        {
            throw new InvalidOperationException(
                $"SMTP authentication failed for user '{username}' on host '{host}:{port}'. " +
                $"Ensure you're using a Gmail App Password, not your regular password. " +
                $"Details: {smtpEx.Message}", smtpEx);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to send email via SMTP. Host: {host}:{port}, From: {from}, To: {toEmail}. " +
                $"Error: {ex.Message}", ex);
        }
    }

    private static string ResolveSmtpHostFromEmail(string email)
    {
        var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant() ?? string.Empty;

        return domain switch
        {
            "gmail.com" => "smtp.gmail.com",
            "outlook.com" or "hotmail.com" or "live.com" or "office365.com" => "smtp.office365.com",
            _ => "smtp.office365.com"
        };
    }
}
