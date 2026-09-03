using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.LoginUser;

public sealed record LoginUserResult(
    UserId UserId,
    string Email,
    bool RequiresMfa,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    string? MfaChallengeToken,
    DateTimeOffset? MfaChallengeExpiresAtUtc);