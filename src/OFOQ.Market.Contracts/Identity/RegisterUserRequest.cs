namespace OFOQ.Market.Contracts.Identity;

public sealed record RegisterUserRequest(
    string Email,
    string Password);