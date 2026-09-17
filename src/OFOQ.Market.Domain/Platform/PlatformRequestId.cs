namespace OFOQ.Market.Domain.Platform;

public readonly record struct PlatformRequestId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;

    public static PlatformRequestId New() =>
        new(Guid.NewGuid());

    public static PlatformRequestId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Platform request ID cannot be empty.",
                nameof(value));
        }

        return new PlatformRequestId(value);
    }
}
