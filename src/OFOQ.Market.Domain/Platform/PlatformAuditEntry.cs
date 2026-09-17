using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Platform;

public sealed class PlatformAuditEntry : Entity<PlatformAuditEntryId>
{
    private PlatformAuditEntry()
    {
    }

    private PlatformAuditEntry(
        PlatformAuditEntryId id,
        UserId actorUserId,
        TenantId tenantId,
        string action,
        string reason,
        string? oldValueJson,
        string? newValueJson,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredAtUtc)
        : base(id)
    {
        ActorUserId = actorUserId;
        TenantId = tenantId;
        Action = action;
        Reason = reason;
        OldValueJson = oldValueJson;
        NewValueJson = newValueJson;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredAtUtc = occurredAtUtc;
    }

    public UserId ActorUserId { get; private set; }

    public TenantId TenantId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public string? OldValueJson { get; private set; }

    public string? NewValueJson { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static PlatformAuditEntry Create(
        UserId actorUserId,
        TenantId tenantId,
        string action,
        string reason,
        string? oldValueJson,
        string? newValueJson,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredAtUtc)
    {
        if (actorUserId.IsEmpty)
        {
            throw new ArgumentException(
                "Actor user ID cannot be empty.",
                nameof(actorUserId));
        }

        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        var normalizedAction = NormalizeRequired(
            action,
            nameof(action),
            120);

        var normalizedReason = NormalizeRequired(
            reason,
            nameof(reason),
            500);

        return new PlatformAuditEntry(
            PlatformAuditEntryId.New(),
            actorUserId,
            tenantId,
            normalizedAction,
            normalizedReason,
            NormalizeOptionalStrict(oldValueJson, nameof(oldValueJson), 12_000),
            NormalizeOptionalStrict(newValueJson, nameof(newValueJson), 12_000),
            NormalizeOptionalTruncated(ipAddress, 64),
            NormalizeOptionalTruncated(userAgent, 512),
            occurredAtUtc);
    }

    private static string NormalizeRequired(
        string value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalStrict(
        string? value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalTruncated(
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
