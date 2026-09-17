namespace OFOQ.Market.Domain.Notifications;

public readonly record struct TenantNotificationPreferencesId(Guid Value)
{
    public static TenantNotificationPreferencesId New()
        => new(Guid.NewGuid());

    public static TenantNotificationPreferencesId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Notification preferences ID cannot be empty.",
                nameof(value));

        return new TenantNotificationPreferencesId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;
}
