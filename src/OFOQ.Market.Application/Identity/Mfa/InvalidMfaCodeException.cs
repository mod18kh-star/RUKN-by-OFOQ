namespace OFOQ.Market.Application.Identity.Mfa;

public sealed class InvalidMfaCodeException :
    Exception
{
    public InvalidMfaCodeException()
        : base("The MFA code is invalid.")
    {
    }
}