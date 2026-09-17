namespace OFOQ.Market.Contracts.Identity;

public sealed record UserSessionResponse(
    Guid SessionId,
    string AuthenticationLevel,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? IpAddress,
    string? UserAgent,
    bool IsCurrent);

public sealed record RevokeOtherSessionsResponse(
    int RevokedCount);
