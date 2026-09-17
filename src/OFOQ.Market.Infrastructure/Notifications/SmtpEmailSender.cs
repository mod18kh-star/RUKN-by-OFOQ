using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using OFOQ.Market.Application.Common.Notifications;

namespace OFOQ.Market.Infrastructure.Notifications;

public sealed class SmtpEmailSender :
    IEmailSender
{
    private readonly EmailDeliveryOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        EmailDeliveryOptions options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string textBody,
        string? htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogInformation(
                "Email delivery is not configured. Development email to {Email}: {Subject}",
                toEmail,
                subject);

            return;
        }

        using var message =
            new MailMessage
            {
                From =
                    new MailAddress(
                        _options.FromAddress,
                        _options.FromName),
                Subject = subject,
                Body = textBody,
                IsBodyHtml = false
            };

        message.To.Add(
            new MailAddress(toEmail));

        if (!string.IsNullOrWhiteSpace(htmlBody))
        {
            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    textBody,
                    null,
                    "text/plain"));

            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    htmlBody,
                    null,
                    "text/html"));
        }

        using var client =
            new SmtpClient(
                _options.Host!,
                _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod =
                    SmtpDeliveryMethod.Network,
                UseDefaultCredentials =
                    false
            };

        if (!string.IsNullOrWhiteSpace(
                _options.UserName))
        {
            client.Credentials =
                new NetworkCredential(
                    _options.UserName,
                    _options.Password);
        }

        cancellationToken.ThrowIfCancellationRequested();

        await client.SendMailAsync(
            message);
    }
}
