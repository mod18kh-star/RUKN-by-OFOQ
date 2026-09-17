using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Platform;

public sealed class PlatformRequest :
    Entity<PlatformRequestId>
{
    private PlatformRequest()
    {
    }

    private PlatformRequest(
        PlatformRequestId id,
        TenantId tenantId,
        UserId requestedByUserId,
        PlatformRequestType type,
        string summary,
        string payloadJson,
        DateTimeOffset requestedAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        RequestedByUserId = requestedByUserId;
        Type = type;
        Summary = NormalizeSummary(summary);
        PayloadJson = NormalizePayload(payloadJson);
        Status = PlatformRequestStatus.Pending;
        RequestedAtUtc = requestedAtUtc;
    }

    public TenantId TenantId { get; private set; }

    public UserId RequestedByUserId { get; private set; }

    public PlatformRequestType Type { get; private set; }

    public PlatformRequestStatus Status { get; private set; }

    public string Summary { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public DateTimeOffset RequestedAtUtc { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public UserId? ReviewedByUserId { get; private set; }

    public string? ReviewReason { get; private set; }

    public static PlatformRequest Create(
        TenantId tenantId,
        UserId requestedByUserId,
        PlatformRequestType type,
        string summary,
        string payloadJson,
        DateTimeOffset requestedAtUtc)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (requestedByUserId.IsEmpty)
        {
            throw new ArgumentException(
                "Requesting user ID cannot be empty.",
                nameof(requestedByUserId));
        }

        return new PlatformRequest(
            PlatformRequestId.New(),
            tenantId,
            requestedByUserId,
            type,
            summary,
            payloadJson,
            requestedAtUtc);
    }

    public void Revise(
        string summary,
        string payloadJson,
        DateTimeOffset requestedAtUtc)
    {
        if (Status != PlatformRequestStatus.MoreInfoRequested)
        {
            throw new InvalidOperationException(
                "Only requests waiting for more information can be revised.");
        }

        Summary = NormalizeSummary(summary);
        PayloadJson = NormalizePayload(payloadJson);
        Status = PlatformRequestStatus.Pending;
        RequestedAtUtc = requestedAtUtc;
        ReviewedAtUtc = null;
        ReviewedByUserId = null;
        ReviewReason = null;
    }

    public void RequestMoreInfo(
        UserId reviewedByUserId,
        string reason,
        DateTimeOffset reviewedAtUtc)
    {
        EnsureReviewable();
        Status = PlatformRequestStatus.MoreInfoRequested;
        ReviewedByUserId = reviewedByUserId;
        ReviewReason = NormalizeReviewReason(reason);
        ReviewedAtUtc = reviewedAtUtc;
    }

    public void Approve(
        UserId reviewedByUserId,
        string reason,
        DateTimeOffset reviewedAtUtc)
    {
        EnsureReviewable();
        Status = PlatformRequestStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewReason = NormalizeReviewReason(reason);
        ReviewedAtUtc = reviewedAtUtc;
    }

    public void Reject(
        UserId reviewedByUserId,
        string reason,
        DateTimeOffset reviewedAtUtc)
    {
        EnsureReviewable();
        Status = PlatformRequestStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        ReviewReason = NormalizeReviewReason(reason);
        ReviewedAtUtc = reviewedAtUtc;
    }

    private void EnsureReviewable()
    {
        if (Status is PlatformRequestStatus.Approved or PlatformRequestStatus.Rejected)
        {
            throw new InvalidOperationException(
                "A completed request cannot be reviewed again.");
        }
    }

    private static string NormalizeSummary(string? summary)
    {
        var normalized = summary?.Trim() ?? string.Empty;

        if (normalized.Length is < 2 or > 240)
        {
            throw new ArgumentException(
                "Request summary must be between 2 and 240 characters.",
                nameof(summary));
        }

        return normalized;
    }

    private static string NormalizePayload(string? payloadJson)
    {
        var normalized = payloadJson?.Trim() ?? string.Empty;

        if (normalized.Length is < 2 or > 12000)
        {
            throw new ArgumentException(
                "Request payload is invalid.",
                nameof(payloadJson));
        }

        return normalized;
    }

    private static string NormalizeReviewReason(string? reason)
    {
        var normalized = reason?.Trim() ?? string.Empty;

        if (normalized.Length is < 2 or > 1000)
        {
            throw new ArgumentException(
                "Review reason must be between 2 and 1000 characters.",
                nameof(reason));
        }

        return normalized;
    }
}
