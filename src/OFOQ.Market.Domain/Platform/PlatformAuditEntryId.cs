namespace OFOQ.Market.Domain.Platform;

public readonly record struct PlatformAuditEntryId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;

    public static PlatformAuditEntryId New() =>
        new(Guid.NewGuid());

    public static PlatformAuditEntryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Platform audit entry ID cannot be empty.",
                nameof(value));
        }

        return new PlatformAuditEntryId(value);
    }

    public override string ToString() => Value.ToString();
}
