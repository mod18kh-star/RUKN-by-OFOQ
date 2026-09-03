namespace OFOQ.Market.Domain.Identity;

public readonly record struct UserId(Guid Value)
{
    public static UserId New()
        => new(Guid.NewGuid());

    public static UserId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(value));

        return new UserId(value);
    }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString()
        => Value.ToString();
}