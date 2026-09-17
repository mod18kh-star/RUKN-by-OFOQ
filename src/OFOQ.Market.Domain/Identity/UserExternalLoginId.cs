namespace OFOQ.Market.Domain.Identity;

public readonly record struct UserExternalLoginId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;

    public static UserExternalLoginId New() => new(Guid.NewGuid());

    public static UserExternalLoginId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("External login ID cannot be empty.", nameof(value));

        return new UserExternalLoginId(value);
    }
}
