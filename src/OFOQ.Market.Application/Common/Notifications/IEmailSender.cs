namespace OFOQ.Market.Application.Common.Notifications;

public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string textBody,
        string? htmlBody,
        CancellationToken cancellationToken = default);
}
