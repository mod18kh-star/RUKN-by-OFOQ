using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class ProductContentBlock :
    Entity<ProductContentBlockId>,
    ITenantDataScoped,
    IAuditable
{
    private ProductContentBlock()
    {
    }

    private ProductContentBlock(
        ProductContentBlockId id,
        TenantId tenantId,
        ProductId productId,
        ProductContentBlockType type,
        string? title,
        string? body,
        string? mediaUrl,
        int sortOrder,
        bool isVisible,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        ProductId =
            productId;

        ApplyContent(
            type,
            title,
            body,
            mediaUrl,
            sortOrder,
            isVisible);

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public ProductId ProductId { get; private set; }

    public ProductContentBlockType Type { get; private set; }

    public string? Title { get; private set; }

    public string? Body { get; private set; }

    public string? MediaUrl { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsVisible { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static ProductContentBlock Create(
        TenantId tenantId,
        ProductId productId,
        ProductContentBlockType type,
        string? title,
        string? body,
        string? mediaUrl,
        int sortOrder,
        bool isVisible,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (productId.IsEmpty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(productId));
        }

        return new ProductContentBlock(
            ProductContentBlockId.New(),
            tenantId,
            productId,
            type,
            title,
            body,
            mediaUrl,
            sortOrder,
            isVisible,
            createdAtUtc,
            createdByUserId);
    }

    public void Update(
        ProductContentBlockType type,
        string? title,
        string? body,
        string? mediaUrl,
        int sortOrder,
        bool isVisible,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        ApplyContent(
            type,
            title,
            body,
            mediaUrl,
            sortOrder,
            isVisible);

        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }

    private void ApplyContent(
        ProductContentBlockType type,
        string? title,
        string? body,
        string? mediaUrl,
        int sortOrder,
        bool isVisible)
    {
        if (!Enum.IsDefined(
                type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "Unsupported product content block type.");
        }

        if (sortOrder <
            0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Product content block sort order cannot be negative.");
        }

        var normalizedTitle =
            NormalizeOptional(
                title,
                160,
                nameof(title));

        var normalizedBody =
            NormalizeOptional(
                body,
                12000,
                nameof(body));

        var normalizedMediaUrl =
            NormalizeMediaUrl(
                mediaUrl);

        if (normalizedBody is null &&
            normalizedMediaUrl is null)
        {
            throw new ArgumentException(
                "A product content block requires text content or a media URL.");
        }

        if (RequiresMedia(
                type) &&
            normalizedMediaUrl is null)
        {
            throw new ArgumentException(
                $"{type} product content blocks require a media URL.");
        }

        Type =
            type;

        Title =
            normalizedTitle;

        Body =
            normalizedBody;

        MediaUrl =
            normalizedMediaUrl;

        SortOrder =
            sortOrder;

        IsVisible =
            isVisible;
    }

    private static bool RequiresMedia(
        ProductContentBlockType type)
    {
        return type is
            ProductContentBlockType.SizeGuide or
            ProductContentBlockType.Image or
            ProductContentBlockType.Video or
            ProductContentBlockType.Pdf;
    }

    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        var normalized =
            value.Trim();

        if (normalized.Length >
            maximumLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeMediaUrl(
        string? mediaUrl)
    {
        var normalized =
            NormalizeOptional(
                mediaUrl,
                2048,
                nameof(mediaUrl));

        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme !=
                Uri.UriSchemeHttp &&
             uri.Scheme !=
                Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "Product content media URL must be an absolute HTTP or HTTPS URL.",
                nameof(mediaUrl));
        }

        return normalized;
    }
}
