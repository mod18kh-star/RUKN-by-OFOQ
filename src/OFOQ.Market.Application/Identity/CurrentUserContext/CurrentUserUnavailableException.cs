namespace OFOQ.Market.Application.Identity.CurrentUserContext;

public sealed class CurrentUserUnavailableException :
    Exception
{
    public CurrentUserUnavailableException()
        : base(
            "The authenticated user is no longer available.")
    {
    }
}
