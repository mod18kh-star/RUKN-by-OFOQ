namespace OFOQ.Market.Domain.Identity;

public readonly record struct UserSessionId(
    Guid Value)
{
    public static UserSessionId New()
    {
        return new UserSessionId(
            Guid.NewGuid());
    }

    public static UserSessionId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "User session ID cannot be empty.",
                nameof(value));
        }

        return new UserSessionId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}
