namespace OFOQ.Market.Application.Identity.LoginUser;

public sealed record LoginUserCommand(
    string Email,
    string Password);