using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Content;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Reviews;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class StorefrontContentQueryRepository : IStorefrontContentQueryRepository
{
    private readonly MarketDbContext _db;

    public StorefrontContentQueryRepository(MarketDbContext db) => _db = db;

    public async Task<StorefrontContentPageResult?> GetPublishedPageAsync(
        string storeSlug,
        string pageSlug,
        CancellationToken cancellationToken = default)
    {
        var slug = TenantSlug.Create(storeSlug);
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Slug == slug && !x.IsDeleted && x.Status == TenantStatus.Active,
                cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        return await _db.ContentPages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenant.Id &&
                x.Slug == pageSlug.Trim().ToLowerInvariant() &&
                x.IsPublished)
            .Select(x => new StorefrontContentPageResult(
                x.Id.Value,
                x.Title,
                x.Slug,
                x.Body,
                x.SeoTitle,
                x.SeoDescription,
                x.PublishedAtUtc,
                x.Kind == ContentPageKind.Reviews
                    ? "Reviews"
                    : x.Kind == ContentPageKind.Statistics
                        ? "Statistics"
                        : "Standard",
                x.HeroImageUrl))
            .SingleOrDefaultAsync(cancellationToken);
    }


    public async Task<StorefrontPageStatisticsResult?> GetPublishedPageStatisticsAsync(
        string storeSlug,
        string pageSlug,
        CancellationToken cancellationToken = default)
    {
        var slug = TenantSlug.Create(storeSlug);
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Slug == slug && !x.IsDeleted && x.Status == TenantStatus.Active,
                cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        var normalizedPageSlug = pageSlug.Trim().ToLowerInvariant();
        var page = await _db.ContentPages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenant.Id &&
                x.Slug == normalizedPageSlug &&
                x.IsPublished &&
                x.Kind == ContentPageKind.Statistics)
            .Select(x => new
            {
                x.ShowCustomerCount,
                x.ShowCompletedOrderCount,
                x.ShowUnitsSold,
                x.ShowAverageRating,
                x.ShowReviewCount,
                x.ShowCountryCount
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (page is null)
        {
            return null;
        }

        var completedOrders = _db.Orders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(order =>
                order.TenantId == tenant.Id &&
                (order.FulfillmentStatus == OrderFulfillmentStatus.Delivered ||
                 order.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled));

        long? completedOrderCount = page.ShowCompletedOrderCount
            ? await completedOrders.LongCountAsync(cancellationToken)
            : null;

        long? customerCount = page.ShowCustomerCount
            ? await completedOrders
                .Select(order => EF.Property<Guid>(order, "_customerUserId"))
                .Distinct()
                .LongCountAsync(cancellationToken)
            : null;

        long? countryCount = page.ShowCountryCount
            ? await completedOrders
                .Where(order => order.ShippingCountryCode != null && order.ShippingCountryCode != string.Empty)
                .Select(order => order.ShippingCountryCode!)
                .Distinct()
                .LongCountAsync(cancellationToken)
            : null;

        long? unitsSold = null;
        if (page.ShowUnitsSold)
        {
            unitsSold = await _db.OrderItems
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(item =>
                    item.TenantId == tenant.Id &&
                    _db.Orders
                        .IgnoreQueryFilters()
                        .Any(order =>
                            order.Id == item.OrderId &&
                            order.TenantId == tenant.Id &&
                            (order.FulfillmentStatus == OrderFulfillmentStatus.Delivered ||
                             order.FulfillmentStatus == OrderFulfillmentStatus.Fulfilled)))
                .SumAsync(item => (long)item.Quantity, cancellationToken);
        }

        var publishedReviews = _db.ProductReviews
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(review =>
                review.TenantId == tenant.Id &&
                review.Status == ProductReviewStatus.Published);

        var needsReviewAggregate = page.ShowAverageRating || page.ShowReviewCount;
        var publishedReviewCount = needsReviewAggregate
            ? await publishedReviews.CountAsync(cancellationToken)
            : 0;

        decimal? averageRating = null;
        if (page.ShowAverageRating)
        {
            averageRating = publishedReviewCount == 0
                ? 0m
                : decimal.Round(
                    await publishedReviews.AverageAsync(review => (decimal)review.Rating, cancellationToken),
                    2);
        }

        int? reviewCount = page.ShowReviewCount
            ? publishedReviewCount
            : null;

        return new StorefrontPageStatisticsResult(
            customerCount,
            completedOrderCount,
            unitsSold,
            averageRating,
            reviewCount,
            countryCount);
    }

    public async Task<IReadOnlyList<StorefrontNavigationItemResult>?> GetNavigationAsync(
        string storeSlug,
        NavigationLocation location,
        CancellationToken cancellationToken = default)
    {
        var slug = TenantSlug.Create(storeSlug);
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Slug == slug && !x.IsDeleted && x.Status == TenantStatus.Active,
                cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        var items = await _db.NavigationItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenant.Id &&
                x.Location == location &&
                x.IsVisible)
            .OrderBy(x => x.ParentItemId.HasValue)
            .ThenBy(x => x.SortOrder)
            .ToArrayAsync(cancellationToken);

        var results = new List<StorefrontNavigationItemResult>(items.Length);

        foreach (var item in items)
        {
            var href = await ResolveHrefAsync(
                tenant.Id,
                storeSlug,
                item,
                cancellationToken);

            // A stale navigation item must never expose a dead internal link.
            if (href is null)
            {
                continue;
            }

            results.Add(new StorefrontNavigationItemResult(
                item.Id.Value,
                item.Location.ToString(),
                item.TargetType.ToString(),
                item.Label,
                item.TargetId,
                item.ExternalUrl,
                item.ParentItemId?.Value,
                item.SortOrder,
                href));
        }

        return results;
    }

    private async Task<string?> ResolveHrefAsync(
        TenantId tenantId,
        string storeSlug,
        NavigationItem item,
        CancellationToken cancellationToken)
    {
        if (item.TargetType == NavigationTargetType.External)
        {
            return item.ExternalUrl;
        }

        if (!item.TargetId.HasValue || item.TargetId.Value == Guid.Empty)
        {
            return null;
        }

        var encodedStoreSlug = Uri.EscapeDataString(storeSlug);

        if (item.TargetType == NavigationTargetType.Page)
        {
            var pageId = ContentPageId.From(item.TargetId.Value);
            var pageSlug = await _db.ContentPages
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.Id == pageId &&
                    x.IsPublished)
                .Select(x => x.Slug)
                .SingleOrDefaultAsync(cancellationToken);

            return pageSlug is null
                ? null
                : $"/store/{encodedStoreSlug}/pages/{Uri.EscapeDataString(pageSlug)}";
        }

        if (item.TargetType == NavigationTargetType.Category)
        {
            var categoryId = CategoryId.From(item.TargetId.Value);
            var categorySlug = await _db.Categories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.Id == categoryId &&
                    x.IsVisible)
                .Select(x => x.Slug)
                .SingleOrDefaultAsync(cancellationToken);

            return categorySlug is null
                ? null
                : $"/store/{encodedStoreSlug}/categories/{Uri.EscapeDataString(categorySlug)}";
        }

        if (item.TargetType == NavigationTargetType.Product)
        {
            var productId = ProductId.From(item.TargetId.Value);
            var productSlug = await _db.Products
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.Id == productId &&
                    !x.IsDeleted &&
                    x.Status == ProductStatus.Published &&
                    x.IsVisible)
                .Select(x => x.Slug)
                .SingleOrDefaultAsync(cancellationToken);

            return productSlug is null
                ? null
                : $"/store/{encodedStoreSlug}/products/{Uri.EscapeDataString(productSlug)}";
        }

        return null;
    }
}
