using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Discounts;

namespace OFOQ.Market.Application.Commerce.Discounts;

public sealed record DiscountCouponResult(Guid Id, string Code, string Name, string Type, decimal Value, string Scope, string Currency, bool IncludeDescendantCategories, decimal? MinimumOrderAmount, int? MaximumTotalUses, int? MaximumUsesPerCustomer, DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc, bool IsEnabled, IReadOnlyList<Guid> ProductIds, IReadOnlyList<Guid> CategoryIds);

public sealed class CouponAdministrationService
{
    private readonly ICurrentTenant _tenant;
    private readonly IDiscountCouponRepository _coupons;
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _time;
    public CouponAdministrationService(ICurrentTenant tenant, IDiscountCouponRepository coupons, IProductRepository products, ICategoryRepository categories, IUnitOfWork uow, TimeProvider time)
    { _tenant = tenant; _coupons = coupons; _products = products; _categories = categories; _uow = uow; _time = time; }

    public async Task<IReadOnlyList<DiscountCouponResult>> GetAllAsync(CancellationToken ct = default) => (await _coupons.GetAllAsync(ct)).OrderByDescending(x => x.CreatedAtUtc).Select(Map).ToArray();

    public async Task<DiscountCouponResult> CreateAsync(string code, string name, DiscountCouponType type, decimal value, DiscountCouponScope scope, string currency, bool includeDescendants, decimal? minOrder, int? maxUses, int? perCustomer, DateTimeOffset? startsAt, DateTimeOffset? endsAt, IReadOnlyList<Guid> productIds, IReadOnlyList<Guid> categoryIds, Guid actor, CancellationToken ct = default)
    {
        EnsureTenant();
        var normalized = NormalizeCode(code);
        if (await _coupons.CodeExistsAsync(normalized, cancellationToken: ct)) throw new ArgumentException("Coupon code already exists.");
        var coupon = DiscountCoupon.Create(_tenant.TenantId!.Value, code, name, type, value, scope, CurrencyCode.Create(currency), includeDescendants, minOrder, maxUses, perCustomer, startsAt, endsAt, _time.GetUtcNow(), actor);
        await ValidateAndApplyTargetsAsync(coupon, productIds, categoryIds, ct);
        await _coupons.AddAsync(coupon, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(coupon);
    }

    public async Task<DiscountCouponResult?> UpdateAsync(DiscountCouponId id, string code, string name, DiscountCouponType type, decimal value, DiscountCouponScope scope, string currency, bool includeDescendants, decimal? minOrder, int? maxUses, int? perCustomer, DateTimeOffset? startsAt, DateTimeOffset? endsAt, bool enabled, IReadOnlyList<Guid> productIds, IReadOnlyList<Guid> categoryIds, Guid actor, CancellationToken ct = default)
    {
        EnsureTenant();
        var coupon = await _coupons.GetByIdAsync(id, ct); if (coupon is null) return null;
        var normalized = NormalizeCode(code);
        if (await _coupons.CodeExistsAsync(normalized, id, ct)) throw new ArgumentException("Coupon code already exists.");
        coupon.Update(code, name, type, value, scope, CurrencyCode.Create(currency), includeDescendants, minOrder, maxUses, perCustomer, startsAt, endsAt, enabled, _time.GetUtcNow(), actor);
        await ValidateAndApplyTargetsAsync(coupon, productIds, categoryIds, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(coupon);
    }

    private async Task ValidateAndApplyTargetsAsync(DiscountCoupon coupon, IReadOnlyList<Guid> productIds, IReadOnlyList<Guid> categoryIds, CancellationToken ct)
    {
        productIds ??= [];
        categoryIds ??= [];
        if (coupon.Scope == DiscountCouponScope.Products)
        {
            if (productIds.Count == 0) throw new ArgumentException("Product-scoped coupons require at least one product.");
            foreach (var raw in productIds.Distinct()) if (raw == Guid.Empty || await _products.GetByIdAsync(ProductId.From(raw), ct) is null) throw new ArgumentException("One or more coupon products were not found in this store.");
            coupon.ReplaceProductTargets(productIds.Distinct().Select(ProductId.From)); coupon.ReplaceCategoryTargets([]);
        }
        else if (coupon.Scope == DiscountCouponScope.Categories)
        {
            if (categoryIds.Count == 0) throw new ArgumentException("Category-scoped coupons require at least one category.");
            foreach (var raw in categoryIds.Distinct()) if (raw == Guid.Empty || await _categories.GetByIdAsync(CategoryId.From(raw), ct) is null) throw new ArgumentException("One or more coupon categories were not found in this store.");
            coupon.ReplaceCategoryTargets(categoryIds.Distinct().Select(CategoryId.From)); coupon.ReplaceProductTargets([]);
        }
        else { coupon.ReplaceProductTargets([]); coupon.ReplaceCategoryTargets([]); }
    }

    private void EnsureTenant() { if (!_tenant.IsAvailable || !_tenant.TenantId.HasValue) throw new TenantScopeViolationException("Tenant context is required."); }
    private static string NormalizeCode(string code) { ArgumentException.ThrowIfNullOrWhiteSpace(code); return code.Trim().ToUpperInvariant(); }
    private static DiscountCouponResult Map(DiscountCoupon x) => new(x.Id.Value, x.Code, x.Name, x.Type.ToString(), x.Value, x.Scope.ToString(), x.Currency.Value, x.IncludeDescendantCategories, x.MinimumOrderAmount, x.MaximumTotalUses, x.MaximumUsesPerCustomer, x.StartsAtUtc, x.EndsAtUtc, x.IsEnabled, x.ProductTargets.Select(t => t.ProductId.Value).ToArray(), x.CategoryTargets.Select(t => t.CategoryId.Value).ToArray());
}
