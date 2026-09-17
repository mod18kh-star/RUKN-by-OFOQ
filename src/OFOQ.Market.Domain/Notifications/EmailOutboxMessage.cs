using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Notifications;

public sealed class EmailOutboxMessage :
    Entity<EmailOutboxMessageId>
{
    private EmailOutboxMessage()
    {
    }

    private EmailOutboxMessage(
        EmailOutboxMessageId id,
        Guid? tenantId,
        string toEmail,
        string subject,
        string textBody,
        string? htmlBody,
        string kind,
        string dedupeKey,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        ToEmail = NormalizeRequired(toEmail, 254, nameof(toEmail));
        Subject = NormalizeRequired(subject, 240, nameof(subject));
        TextBody = NormalizeRequired(textBody, 12000, nameof(textBody));
        HtmlBody = NormalizeOptional(htmlBody, 24000);
        Kind = NormalizeRequired(kind, 80, nameof(kind));
        DedupeKey = NormalizeRequired(dedupeKey, 220, nameof(dedupeKey));
        State = EmailOutboxState.Pending;
        Attempts = 0;
        NextAttemptAtUtc = createdAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid? TenantId { get; private set; }

    public string ToEmail { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string TextBody { get; private set; } = string.Empty;

    public string? HtmlBody { get; private set; }

    public string Kind { get; private set; } = string.Empty;

    public string DedupeKey { get; private set; } = string.Empty;

    public EmailOutboxState State { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public static EmailOutboxMessage Create(
        Guid? tenantId,
        string toEmail,
        string subject,
        string textBody,
        string? htmlBody,
        string kind,
        string dedupeKey,
        DateTimeOffset createdAtUtc)
        => new(
            EmailOutboxMessageId.New(),
            tenantId,
            toEmail,
            subject,
            textBody,
            htmlBody,
            kind,
            dedupeKey,
            createdAtUtc);

    public void MarkSent(DateTimeOffset sentAtUtc)
    {
        State = EmailOutboxState.Sent;
        SentAtUtc = sentAtUtc;
        LeaseExpiresAtUtc = null;
        LastError = null;
    }

    public void MarkFailed(
        string error,
        DateTimeOffset nextAttemptAtUtc)
    {
        State = EmailOutboxState.Failed;
        LeaseExpiresAtUtc = null;
        var normalizedError =
            string.IsNullOrWhiteSpace(error)
                ? "Email delivery failed."
                : error.Trim();

        LastError =
            normalizedError.Length <= 2000
                ? normalizedError
                : normalizedError[..2000];

        NextAttemptAtUtc = nextAttemptAtUtc;
    }

    private static string NormalizeRequired(
        string value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Value is required.",
                parameterName);

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                nameof(value));

        return normalized;
    }
}
