using OFOQ.Market.Application.Storefront;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IStorefrontQueryRepository
{
    Task<StorefrontInfoResult?> GetStoreAsync(
        TenantSlug storeSlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorefrontCategoryResult>> GetCategoriesAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);

    Task<StorefrontProductPageResult> GetProductsAsync(
        TenantId tenantId,
        string? search,
        string? categorySlug,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<StorefrontProductDetailResult?> GetProductBySlugAsync(
        TenantId tenantId,
        string productSlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CartProductDisplayResult>> GetCartProductsAsync(
        TenantId tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);
}

public sealed record CartVariantDisplayResult(
    Guid VariantId,
    string Name);

public sealed record CartProductDisplayResult(
    Guid ProductId,
    string Name,
    string Slug,
    string Currency,
    decimal CurrentPrice,
    decimal? CompareAtPrice,
    string? PrimaryImageUrl,
    string? PrimaryImageAltText,
    IReadOnlyList<CartVariantDisplayResult> Variants);