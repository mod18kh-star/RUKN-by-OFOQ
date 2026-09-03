namespace OFOQ.Market.Application.Identity.Mfa;

public sealed class MfaAlreadyEnabledException :
    Exception
{
    public MfaAlreadyEnabledException()
        : base("MFA is already enabled.")
    {
    }
}