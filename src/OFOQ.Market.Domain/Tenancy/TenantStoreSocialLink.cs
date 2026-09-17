using System.Text;
using OFOQ.Market.Domain.Common;

namespace OFOQ.Market.Domain.Tenancy;

public sealed class TenantStoreSocialLink :
    Entity<TenantStoreSocialLinkId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxPlatformCodeLength = 40;
    public const int MaxLabelLength = 80;
    public const int MaxUrlLength = 2048;

    private TenantStoreSocialLink()
    {
    }

    private TenantStoreSocialLink(
        TenantStoreSocialLinkId id,
        TenantId tenantId,
        string platformCode,
        string? label,
        string url,
        int sortOrder,
        bool isVisible,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        TenantId =
            tenantId;

        PlatformCode =
            NormalizePlatformCode(
                platformCode);

        Label =
            NormalizeOptional(
                label,
                MaxLabelLength,
                "Social-link label");

        Url =
            NormalizeUrl(
                url);

        SortOrder =
            NormalizeSortOrder(
                sortOrder);

        IsVisible =
            isVisible;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public string PlatformCode { get; private set; } =
        string.Empty;

    public string? Label { get; private set; }

    public string Url { get; private set; } =
        string.Empty;

    public int SortOrder { get; private set; }

    public bool IsVisible { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantStoreSocialLink Create(
        TenantId tenantId,
        string platformCode,
        string? label,
        string url,
        int sortOrder,
        bool isVisible,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantStoreSocialLink(
            TenantStoreSocialLinkId.New(),
            tenantId,
            platformCode,
            label,
            url,
            sortOrder,
            isVisible,
            createdAtUtc,
            createdByUserId);
    }

    private static string NormalizePlatformCode(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                "Social platform code is required.",
                nameof(value));
        }

        var input =
            value.Trim()
                .ToLowerInvariant();

        var builder =
            new StringBuilder(
                input.Length);

        foreach (var character in input)
        {
            if (char.IsLetterOrDigit(
                    character))
            {
                builder.Append(
                    character);

                continue;
            }

            if (character is '-' or '_' or ' ')
            {
                if (builder.Length > 0 &&
                    builder[^1] != '-')
                {
                    builder.Append('-');
                }

                continue;
            }

            throw new ArgumentException(
                "Social platform code can only contain letters, digits, spaces, hyphens, and underscores.",
                nameof(value));
        }

        var normalized =
            builder
                .ToString()
                .Trim('-');

        if (normalized.Length == 0 ||
            normalized.Length >
                MaxPlatformCodeLength)
        {
            throw new ArgumentException(
                $"Social platform code must contain between 1 and {MaxPlatformCodeLength} characters.",
                nameof(value));
        }

        return normalized;
    }

    private static string NormalizeUrl(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                "Social-link URL is required.",
                nameof(value));
        }

        var normalized =
            value.Trim();

        if (normalized.Length >
            MaxUrlLength)
        {
            throw new ArgumentException(
                $"Social-link URL cannot exceed {MaxUrlLength} characters.",
                nameof(value));
        }

        if (!Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException(
                "Social-link URL must be an absolute HTTP or HTTPS URL.",
                nameof(value));
        }

        return uri.AbsoluteUri;
    }

    private static int NormalizeSortOrder(
        int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Social-link sort order cannot be negative.");
        }

        return sortOrder;
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        var normalized =
            value.Trim();

        if (normalized.Length >
            maxLength)
        {
            throw new ArgumentException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
