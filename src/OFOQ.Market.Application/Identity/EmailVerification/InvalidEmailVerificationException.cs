namespace OFOQ.Market.Application.Identity.EmailVerification;

public sealed class InvalidEmailVerificationException :
    Exception
{
    public InvalidEmailVerificationException()
        : base(
            "The email verification request is invalid or expired.")
    {
    }
}
