namespace OFOQ.Market.Domain.Identity;

public readonly record struct EmailVerificationChallengeId(
    Guid Value)
{
    public static EmailVerificationChallengeId New()
    {
        return new EmailVerificationChallengeId(
            Guid.NewGuid());
    }

    public static EmailVerificationChallengeId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Email verification challenge ID cannot be empty.",
                nameof(value));
        }

        return new EmailVerificationChallengeId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;

    public override string ToString()
    {
        return Value.ToString();
    }
}
