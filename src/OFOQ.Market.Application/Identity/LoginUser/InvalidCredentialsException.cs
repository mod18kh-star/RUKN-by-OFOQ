namespace OFOQ.Market.Application.Identity.LoginUser;

public sealed class InvalidCredentialsException :
    Exception
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}