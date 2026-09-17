namespace OFOQ.Market.Application.Identity.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string? FullName = null,
    string? PhoneNumber = null);
