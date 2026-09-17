using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Sessions;

public sealed record AuthenticationSessionResult(
    UserSessionId SessionId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed record AuthenticationSessionSummary(
    UserSessionId SessionId,
    UserSessionAuthenticationLevel AuthenticationLevel,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? IpAddress,
    string? UserAgent,
    bool IsCurrent);
