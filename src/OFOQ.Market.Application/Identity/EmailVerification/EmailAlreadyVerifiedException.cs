namespace OFOQ.Market.Application.Identity.EmailVerification;

public sealed class EmailAlreadyVerifiedException :
    Exception
{
    public EmailAlreadyVerifiedException()
        : base(
            "The email address is already verified.")
    {
    }
}
