namespace OFOQ.Market.Domain.Identity;

public readonly record struct UserMfaRecoveryCodeId(
    Guid Value)
{
    public static UserMfaRecoveryCodeId New()
    {
        return new UserMfaRecoveryCodeId(
            Guid.NewGuid());
    }

    public static UserMfaRecoveryCodeId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "MFA recovery code ID cannot be empty.",
                nameof(value));
        }

        return new UserMfaRecoveryCodeId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}