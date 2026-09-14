using System.Text;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Storefront;

public sealed class StorefrontQueryService
{
    private readonly IStorefrontQueryRepository
        _repository;

    public StorefrontQueryService(
        IStorefrontQueryRepository repository)
    {
        _repository =
            repository;
    }

    public Task<StorefrontInfoResult?> GetStoreAsync(
        string storeSlug,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetStoreAsync(
            TenantSlug.Create(
                storeSlug),
            cancellationToken);
    }

    public async Task<IReadOnlyList<StorefrontCategoryResult>?>
        GetCategoriesAsync(
            string storeSlug,
            CancellationToken cancellationToken = default)
    {
        var store =
            await GetStoreAsync(
                storeSlug,
                cancellationToken);

        if (store is null)
        {
            return null;
        }

        return await _repository.GetCategoriesAsync(
            store.TenantId,
            cancellationToken);
    }

    public async Task<StorefrontProductPageResult?>
        GetProductsAsync(
            string storeSlug,
            string? search,
            string? categorySlug,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        if (page < 1 ||
            page > 100000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "Page must be between 1 and 100000.");
        }

        if (pageSize < 1 ||
            pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be between 1 and 100.");
        }

        search =
            NormalizeSearch(
                search);

        categorySlug =
            NormalizeOptionalSlug(
                categorySlug,
                120);

        var store =
            await GetStoreAsync(
                storeSlug,
                cancellationToken);

        if (store is null)
        {
            return null;
        }

        return await _repository.GetProductsAsync(
            store.TenantId,
            search,
            categorySlug,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<StorefrontProductLookupResult>
        GetProductAsync(
            string storeSlug,
            string productSlug,
            CancellationToken cancellationToken = default)
    {
        var store =
            await GetStoreAsync(
                storeSlug,
                cancellationToken);

        if (store is null)
        {
            return new StorefrontProductLookupResult(
                false,
                null);
        }

        var normalizedProductSlug =
            NormalizeRequiredSlug(
                productSlug,
                160);

        var product =
            await _repository.GetProductBySlugAsync(
                store.TenantId,
                normalizedProductSlug,
                cancellationToken);

        return new StorefrontProductLookupResult(
            true,
            product);
    }

    private static string? NormalizeSearch(
        string? search)
    {
        if (string.IsNullOrWhiteSpace(
                search))
        {
            return null;
        }

        var normalized =
            search.Trim();

        if (normalized.Length >
            100)
        {
            throw new ArgumentException(
                "Search cannot exceed 100 characters.",
                nameof(search));
        }

        return normalized;
    }

    private static string? NormalizeOptionalSlug(
        string? slug,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(
                slug))
        {
            return null;
        }

        return NormalizeRequiredSlug(
            slug,
            maxLength);
    }

    private static string NormalizeRequiredSlug(
        string slug,
        int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            slug);

        var normalized =
            slug
                .Trim()
                .Normalize(
                    NormalizationForm.FormKC)
                .ToLowerInvariant();

        if (normalized.Length >
            maxLength)
        {
            throw new ArgumentException(
                $"Slug cannot exceed {maxLength} characters.",
                nameof(slug));
        }

        return normalized;
    }
}