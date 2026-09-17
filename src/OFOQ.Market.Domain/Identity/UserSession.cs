using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed class UserSession :
    Entity<UserSessionId>
{
    private UserSession(
        UserSessionId id,
        UserId userId,
        string refreshTokenHash,
        UserSessionAuthenticationLevel authenticationLevel,
        UserSessionAuthenticationMethod authenticationMethod,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        string? ipAddress,
        string? userAgent)
        : base(id)
    {
        UserId = userId;
        RefreshTokenHash = NormalizeRefreshTokenHash(refreshTokenHash);
        AuthenticationLevel = authenticationLevel;
        AuthenticationMethod = authenticationMethod;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        LastSeenAtUtc = createdAtUtc;
        LastRotatedAtUtc = createdAtUtc;
        CreatedIpAddress = NormalizeOptional(ipAddress, 64);
        LastIpAddress = CreatedIpAddress;
        UserAgent = NormalizeOptional(userAgent, 512);
    }

    private UserSession()
    {
    }

    public UserId UserId { get; private set; }

    public string RefreshTokenHash { get; private set; } =
        string.Empty;

    public UserSessionAuthenticationLevel AuthenticationLevel
    {
        get;
        private set;
    }

    public UserSessionAuthenticationMethod AuthenticationMethod
    {
        get;
        private set;
    }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastSeenAtUtc { get; private set; }

    public DateTimeOffset LastRotatedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevocationReason { get; private set; }

    public string? CreatedIpAddress { get; private set; }

    public string? LastIpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public static UserSession Create(
        UserId userId,
        string refreshTokenHash,
        UserSessionAuthenticationLevel authenticationLevel,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        string? ipAddress = null,
        string? userAgent = null,
        UserSessionAuthenticationMethod authenticationMethod = UserSessionAuthenticationMethod.Password)
    {
        return Create(
            UserSessionId.New(),
            userId,
            refreshTokenHash,
            authenticationLevel,
            expiresAtUtc,
            createdAtUtc,
            ipAddress,
            userAgent,
            authenticationMethod);
    }

    public static UserSession Create(
        UserSessionId sessionId,
        UserId userId,
        string refreshTokenHash,
        UserSessionAuthenticationLevel authenticationLevel,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        string? ipAddress = null,
        string? userAgent = null,
        UserSessionAuthenticationMethod authenticationMethod = UserSessionAuthenticationMethod.Password)
    {
        if (sessionId.IsEmpty)
        {
            throw new ArgumentException(
                "User session ID cannot be empty.",
                nameof(sessionId));
        }

        if (userId.IsEmpty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        if (!Enum.IsDefined(authenticationLevel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(authenticationLevel));
        }

        if (!Enum.IsDefined(authenticationMethod))
        {
            throw new ArgumentOutOfRangeException(
                nameof(authenticationMethod));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "Session expiration must be after creation time.",
                nameof(expiresAtUtc));
        }

        return new UserSession(
            sessionId,
            userId,
            refreshTokenHash,
            authenticationLevel,
            authenticationMethod,
            expiresAtUtc,
            createdAtUtc,
            ipAddress,
            userAgent);
    }

    public bool IsUsable(
        DateTimeOffset nowUtc)
    {
        return
            !IsRevoked &&
            nowUtc < ExpiresAtUtc;
    }

    public bool HasRefreshTokenHash(
        string refreshTokenHash)
    {
        return string.Equals(
            RefreshTokenHash,
            NormalizeRefreshTokenHash(refreshTokenHash),
            StringComparison.Ordinal);
    }

    public void RotateRefreshToken(
        string refreshTokenHash,
        DateTimeOffset nowUtc,
        string? ipAddress = null,
        string? userAgent = null)
    {
        EnsureUsable(nowUtc);

        RefreshTokenHash =
            NormalizeRefreshTokenHash(
                refreshTokenHash);

        LastSeenAtUtc = nowUtc;
        LastRotatedAtUtc = nowUtc;
        LastIpAddress = NormalizeOptional(ipAddress, 64);

        var normalizedUserAgent =
            NormalizeOptional(userAgent, 512);

        if (normalizedUserAgent is not null)
        {
            UserAgent = normalizedUserAgent;
        }
    }

    public void UpgradeToMultiFactor(
        string refreshTokenHash,
        DateTimeOffset nowUtc,
        string? ipAddress = null,
        string? userAgent = null)
    {
        EnsureUsable(nowUtc);

        AuthenticationLevel =
            UserSessionAuthenticationLevel.MultiFactor;

        RotateRefreshToken(
            refreshTokenHash,
            nowUtc,
            ipAddress,
            userAgent);
    }

    public void Revoke(
        string reason,
        DateTimeOffset nowUtc)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = nowUtc;
        RevocationReason =
            NormalizeRequired(
                reason,
                200,
                "Session revocation reason is required.");
    }

    private void EnsureUsable(
        DateTimeOffset nowUtc)
    {
        if (!IsUsable(nowUtc))
        {
            throw new InvalidOperationException(
                "The user session is no longer usable.");
        }
    }

    private static string NormalizeRefreshTokenHash(
        string refreshTokenHash)
    {
        return NormalizeRequired(
            refreshTokenHash,
            128,
            "Refresh token hash is required.");
    }

    private static string NormalizeRequired(
        string value,
        int maxLength,
        string requiredMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                requiredMessage,
                nameof(value));
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength];
        }

        return normalized;
    }
}
