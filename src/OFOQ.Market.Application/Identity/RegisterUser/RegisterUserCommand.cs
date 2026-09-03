namespace OFOQ.Market.Application.Identity.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password);