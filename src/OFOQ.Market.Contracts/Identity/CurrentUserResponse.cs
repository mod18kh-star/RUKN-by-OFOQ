namespace OFOQ.Market.Contracts.Identity;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email);