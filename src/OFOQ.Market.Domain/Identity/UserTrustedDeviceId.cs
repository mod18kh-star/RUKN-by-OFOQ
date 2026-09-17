namespace OFOQ.Market.Domain.Identity;

public readonly record struct UserTrustedDeviceId(Guid Value)
{
    public static UserTrustedDeviceId New()
        => new(Guid.NewGuid());

    public static UserTrustedDeviceId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "Trusted device ID cannot be empty.",
                nameof(value));

        return new UserTrustedDeviceId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;
}
