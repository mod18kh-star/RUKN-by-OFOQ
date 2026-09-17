namespace OFOQ.Market.Application.Identity.RegisterUser;

public sealed class InvalidRegistrationProfileException :
    Exception
{
    public InvalidRegistrationProfileException(
        string code,
        string message)
        : base(message)
    {
        Code =
            code;
    }

    public string Code { get; }
}
