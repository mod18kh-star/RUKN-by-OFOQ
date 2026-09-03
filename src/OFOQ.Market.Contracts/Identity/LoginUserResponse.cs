namespace OFOQ.Market.Contracts.Identity;

public sealed record LoginUserResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);