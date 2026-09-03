namespace OFOQ.Market.Contracts.Identity;

public sealed record VerifyMfaTotpResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);