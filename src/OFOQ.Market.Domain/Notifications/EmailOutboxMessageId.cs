namespace OFOQ.Market.Domain.Notifications;

public readonly record struct EmailOutboxMessageId(Guid Value)
{
    public static EmailOutboxMessageId New()
        => new(Guid.NewGuid());

    public static EmailOutboxMessageId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Email outbox message ID cannot be empty.",
                nameof(value));

        return new EmailOutboxMessageId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;
}
