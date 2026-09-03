namespace OFOQ.Market.Contracts.Identity;

public sealed record LoginUserResponse(
    Guid UserId,
    string Email,
    bool RequiresMfa,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    string? MfaChallengeToken,
    DateTimeOffset? MfaChallengeExpiresAtUtc);