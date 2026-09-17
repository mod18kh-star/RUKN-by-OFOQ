using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Identity;

public sealed class UserTrustedDevice :
    Entity<UserTrustedDeviceId>
{
    private UserTrustedDevice()
    {
    }

    private UserTrustedDevice(
        UserTrustedDeviceId id,
        UserId userId,
        string tokenHash,
        string? userAgentHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        string? createdIpAddress)
        : base(id)
    {
        if (userId.IsEmpty)
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));

        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentException(
                "Trusted device expiration must be after creation.",
                nameof(expiresAtUtc));

        UserId = userId;
        TokenHash = NormalizeRequired(
            tokenHash,
            128,
            nameof(tokenHash));
        UserAgentHash = NormalizeOptional(
            userAgentHash,
            128);
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        LastUsedAtUtc = createdAtUtc;
        CreatedIpAddress = NormalizeOptional(
            createdIpAddress,
            64);
        LastIpAddress = CreatedIpAddress;
    }

    public UserId UserId { get; private set; }

    public string TokenHash { get; private set; } =
        string.Empty;

    public string? UserAgentHash { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastUsedAtUtc { get; private set; }

    public string? CreatedIpAddress { get; private set; }

    public string? LastIpAddress { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevocationReason { get; private set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public bool IsUsable(DateTimeOffset nowUtc)
        => !IsRevoked &&
           nowUtc < ExpiresAtUtc;

    public static UserTrustedDevice Create(
        UserTrustedDeviceId id,
        UserId userId,
        string tokenHash,
        string? userAgentHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc,
        string? createdIpAddress = null)
        => new(
            id,
            userId,
            tokenHash,
            userAgentHash,
            expiresAtUtc,
            createdAtUtc,
            createdIpAddress);

    public void Touch(
        DateTimeOffset nowUtc,
        string? ipAddress)
    {
        if (!IsUsable(nowUtc))
            throw new InvalidOperationException(
                "Trusted device is no longer usable.");

        LastUsedAtUtc = nowUtc;
        LastIpAddress = NormalizeOptional(
            ipAddress,
            64);
    }

    public void Revoke(
        string reason,
        DateTimeOffset nowUtc)
    {
        if (IsRevoked)
            return;

        RevokedAtUtc = nowUtc;
        RevocationReason =
            NormalizeRequired(
                reason,
                200,
                nameof(reason));
    }

    private static string NormalizeRequired(
        string value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Value is required.",
                parameterName);

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
