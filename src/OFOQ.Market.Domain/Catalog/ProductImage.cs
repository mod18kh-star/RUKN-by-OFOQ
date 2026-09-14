using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class ProductImage :
    Entity<ProductImageId>,
    ITenantDataScoped
{
    private ProductImage()
    {
    }

    private ProductImage(
        ProductImageId id,
        TenantId tenantId,
        ProductId productId,
        string url,
        string? altText,
        int sortOrder,
        bool isPrimary,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        ProductId =
            productId;

        Url =
            NormalizeUrl(
                url);

        AltText =
            NormalizeAltText(
                altText);

        SortOrder =
            NormalizeSortOrder(
                sortOrder);

        IsPrimary =
            isPrimary;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public ProductId ProductId { get; private set; }

    public string Url { get; private set; } =
        string.Empty;

    public string? AltText { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public static ProductImage Create(
        TenantId tenantId,
        ProductId productId,
        string url,
        string? altText,
        int sortOrder,
        bool isPrimary,
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

        return new ProductImage(
            ProductImageId.New(),
            tenantId,
            productId,
            url,
            altText,
            sortOrder,
            isPrimary,
            createdAtUtc,
            createdByUserId);
    }

    private static string NormalizeUrl(
        string url)
    {
        if (string.IsNullOrWhiteSpace(
                url))
        {
            throw new ArgumentException(
                "Product image URL is required.",
                nameof(url));
        }

        var normalized =
            url.Trim();

        if (normalized.Length >
            2048)
        {
            throw new ArgumentException(
                "Product image URL cannot exceed 2048 characters.",
                nameof(url));
        }

        if (!Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "Product image URL must be an absolute HTTP or HTTPS URL.",
                nameof(url));
        }

        return normalized;
    }

    private static string? NormalizeAltText(
        string? altText)
    {
        if (string.IsNullOrWhiteSpace(
                altText))
        {
            return null;
        }

        var normalized =
            altText.Trim();

        if (normalized.Length >
            300)
        {
            throw new ArgumentException(
                "Product image alt text cannot exceed 300 characters.",
                nameof(altText));
        }

        return normalized;
    }

    private static int NormalizeSortOrder(
        int sortOrder)
    {
        if (sortOrder <
            0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Product image sort order cannot be negative.");
        }

        return sortOrder;
    }
}