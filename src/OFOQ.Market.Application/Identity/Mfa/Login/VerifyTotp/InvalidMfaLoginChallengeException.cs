namespace OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;

public sealed class InvalidMfaLoginChallengeException :
    Exception
{
    public InvalidMfaLoginChallengeException()
        : base(
            "The MFA challenge or verification code is invalid.")
    {
    }
}