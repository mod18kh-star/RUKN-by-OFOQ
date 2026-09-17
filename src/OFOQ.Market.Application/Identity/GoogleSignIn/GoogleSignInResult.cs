using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.GoogleSignIn;

public sealed record GoogleSignInResult(
    UserId UserId,
    string Email,
    bool RequiresMfa,
    string? MfaChallengeToken,
    DateTimeOffset? MfaChallengeExpiresAtUtc);
