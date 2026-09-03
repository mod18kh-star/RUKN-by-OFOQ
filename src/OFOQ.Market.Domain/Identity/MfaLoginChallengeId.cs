namespace OFOQ.Market.Domain.Identity;

public readonly record struct MfaLoginChallengeId(
    Guid Value)
{
    public static MfaLoginChallengeId New()
    {
        return new MfaLoginChallengeId(
            Guid.NewGuid());
    }

    public static MfaLoginChallengeId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "MFA login challenge ID cannot be empty.",
                nameof(value));
        }

        return new MfaLoginChallengeId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}