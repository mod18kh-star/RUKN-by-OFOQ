namespace OFOQ.Market.Application.Identity.RegisterUser;

public sealed class InvalidPasswordException :
    Exception
{
    public InvalidPasswordException(
        string message)
        : base(message)
    {
    }
}