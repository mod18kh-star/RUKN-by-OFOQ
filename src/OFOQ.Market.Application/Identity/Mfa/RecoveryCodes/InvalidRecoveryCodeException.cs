namespace OFOQ.Market.Application.Identity.Mfa.RecoveryCodes;

public sealed class InvalidRecoveryCodeException :
    Exception
{
    public InvalidRecoveryCodeException()
        : base(
            "The recovery code is invalid.")
    {
    }
}