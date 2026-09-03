namespace OFOQ.Market.Application.Identity.Mfa.Login.VerifyRecovery;

public sealed class InvalidMfaRecoveryVerificationException :
    Exception
{
    public InvalidMfaRecoveryVerificationException()
        : base(
            "The MFA challenge or recovery code is invalid.")
    {
    }
}