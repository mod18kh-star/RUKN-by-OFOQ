namespace OFOQ.Market.Domain.Identity;

public readonly record struct UserMfaId(
    Guid Value)
{
    public static UserMfaId New()
    {
        return new UserMfaId(
            Guid.NewGuid());
    }

    public static UserMfaId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "User MFA ID cannot be empty.",
                nameof(value));
        }

        return new UserMfaId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}