namespace OFOQ.Market.Infrastructure.Notifications;

public sealed class EmailDeliveryOptions
{
    public string? Host { get; init; }

    public int Port { get; init; } = 587;

    public bool EnableSsl { get; init; } = true;

    public string? UserName { get; init; }

    public string? Password { get; init; }

    public string FromAddress { get; init; } =
        "no-reply@localhost";

    public string FromName { get; init; } =
        "RUKN";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(FromAddress);

    public void ValidateForProduction()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Production email delivery is not configured. Configure Email:Smtp:Host and Email:Smtp:FromAddress.");
        }

        if (Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "Email SMTP port is invalid.");
        }
    }
}
