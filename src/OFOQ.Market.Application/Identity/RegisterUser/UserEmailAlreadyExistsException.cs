namespace OFOQ.Market.Application.Identity.RegisterUser;

public sealed class UserEmailAlreadyExistsException :
    Exception
{
    public UserEmailAlreadyExistsException(
        string email)
        : base(
            $"A user with email '{email}' already exists.")
    {
    }
}