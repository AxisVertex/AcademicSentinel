namespace AcademicSentinel.Server.Services;

public interface IEmailSender
{
    Task SendPasswordResetCodeAsync(string toEmail, string code);
}
