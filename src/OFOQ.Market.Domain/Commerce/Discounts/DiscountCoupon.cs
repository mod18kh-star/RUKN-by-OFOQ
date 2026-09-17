using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Discounts;

public sealed class DiscountCoupon : AggregateRoot<DiscountCouponId>, ITenantDataScoped, IAuditable
{
    public const int MaxCodeLength = 60;
    public const int MaxNameLength = 160;
    private readonly List<DiscountCouponProductTarget> _productTargets = [];
    private readonly List<DiscountCouponCategoryTarget> _categoryTargets = [];
    private string _currencyCode = string.Empty;

    private DiscountCoupon() { }

    private DiscountCoupon(DiscountCouponId id, TenantId tenantId, string code, string name, DiscountCouponType type, decimal value, DiscountCouponScope scope, CurrencyCode currency, bool includeDescendantCategories, decimal? minimumOrderAmount, int? maximumTotalUses, int? maximumUsesPerCustomer, DateTimeOffset? startsAtUtc, DateTimeOffset? endsAtUtc, DateTimeOffset createdAtUtc, Guid? createdByUserId) : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        TenantId = tenantId;
        Apply(code, name, type, value, scope, currency, includeDescendantCategories, minimumOrderAmount, maximumTotalUses, maximumUsesPerCustomer, startsAtUtc, endsAtUtc);
        IsEnabled = true;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DiscountCouponType Type { get; private set; }
    public decimal Value { get; private set; }
    public DiscountCouponScope Scope { get; private set; }
    public CurrencyCode Currency => CurrencyCode.Create(_currencyCode);
    public bool IncludeDescendantCategories { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public int? MaximumTotalUses { get; private set; }
    public int? MaximumUsesPerCustomer { get; private set; }
    public DateTimeOffset? StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }
    public bool IsEnabled { get; private set; }
    public IReadOnlyCollection<DiscountCouponProductTarget> ProductTargets => _productTargets.AsReadOnly();
    public IReadOnlyCollection<DiscountCouponCategoryTarget> CategoryTargets => _categoryTargets.AsReadOnly();
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static DiscountCoupon Create(TenantId tenantId, string code, string name, DiscountCouponType type, decimal value, DiscountCouponScope scope, CurrencyCode currency, bool includeDescendantCategories, decimal? minimumOrderAmount, int? maximumTotalUses, int? maximumUsesPerCustomer, DateTimeOffset? startsAtUtc, DateTimeOffset? endsAtUtc, DateTimeOffset createdAtUtc, Guid? createdByUserId = null)
        => new(DiscountCouponId.New(), tenantId, code, name, type, value, scope, currency, includeDescendantCategories, minimumOrderAmount, maximumTotalUses, maximumUsesPerCustomer, startsAtUtc, endsAtUtc, createdAtUtc, createdByUserId);

    public void Update(string code, string name, DiscountCouponType type, decimal value, DiscountCouponScope scope, CurrencyCode currency, bool includeDescendantCategories, decimal? minimumOrderAmount, int? maximumTotalUses, int? maximumUsesPerCustomer, DateTimeOffset? startsAtUtc, DateTimeOffset? endsAtUtc, bool isEnabled, DateTimeOffset updatedAtUtc, Guid? updatedByUserId = null)
    {
        Apply(code, name, type, value, scope, currency, includeDescendantCategories, minimumOrderAmount, maximumTotalUses, maximumUsesPerCustomer, startsAtUtc, endsAtUtc);
        IsEnabled = isEnabled;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void ReplaceProductTargets(IEnumerable<ProductId> productIds)
    {
        var ids = productIds.Distinct().ToArray();
        if (Scope != DiscountCouponScope.Products && ids.Length > 0) throw new InvalidOperationException("Product targets are only valid for a product-scoped coupon.");
        _productTargets.Clear();
        foreach (var productId in ids) _productTargets.Add(DiscountCouponProductTarget.Create(TenantId, Id, productId));
    }

    public void ReplaceCategoryTargets(IEnumerable<CategoryId> categoryIds)
    {
        var ids = categoryIds.Distinct().ToArray();
        if (Scope != DiscountCouponScope.Categories && ids.Length > 0) throw new InvalidOperationException("Category targets are only valid for a category-scoped coupon.");
        _categoryTargets.Clear();
        foreach (var categoryId in ids) _categoryTargets.Add(DiscountCouponCategoryTarget.Create(TenantId, Id, categoryId));
    }

    public bool IsActiveAt(DateTimeOffset now) => IsEnabled && (!StartsAtUtc.HasValue || StartsAtUtc.Value <= now) && (!EndsAtUtc.HasValue || EndsAtUtc.Value >= now);

    public decimal CalculateDiscount(decimal eligibleAmount)
    {
        if (eligibleAmount <= 0m) return 0m;
        var discount = Type == DiscountCouponType.Percentage ? eligibleAmount * (Value / 100m) : Math.Min(Value, eligibleAmount);
        return decimal.Round(Math.Clamp(discount, 0m, eligibleAmount), 2, MidpointRounding.AwayFromZero);
    }

    private void Apply(string code, string name, DiscountCouponType type, decimal value, DiscountCouponScope scope, CurrencyCode currency, bool includeDescendantCategories, decimal? minimumOrderAmount, int? maximumTotalUses, int? maximumUsesPerCustomer, DateTimeOffset? startsAtUtc, DateTimeOffset? endsAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code); ArgumentException.ThrowIfNullOrWhiteSpace(name);
        code = code.Trim().ToUpperInvariant(); name = name.Trim();
        if (code.Length > MaxCodeLength || name.Length > MaxNameLength) throw new ArgumentException("Coupon code or name is too long.");
        if (!Enum.IsDefined(type) || !Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException("Unsupported coupon type or scope.");
        if (value <= 0m) throw new ArgumentOutOfRangeException(nameof(value));
        if (type == DiscountCouponType.Percentage && value > 100m) throw new ArgumentOutOfRangeException(nameof(value), "Percentage coupons cannot exceed 100%.");
        if (currency.IsEmpty) throw new ArgumentException("Coupon currency is required.", nameof(currency));
        if (minimumOrderAmount is < 0m) throw new ArgumentOutOfRangeException(nameof(minimumOrderAmount));
        if (maximumTotalUses is <= 0) throw new ArgumentOutOfRangeException(nameof(maximumTotalUses));
        if (maximumUsesPerCustomer is <= 0) throw new ArgumentOutOfRangeException(nameof(maximumUsesPerCustomer));
        if (startsAtUtc.HasValue && endsAtUtc.HasValue && startsAtUtc.Value >= endsAtUtc.Value) throw new ArgumentException("Coupon end time must be after start time.");
        Code = code; Name = name; Type = type; Value = decimal.Round(value, 2, MidpointRounding.AwayFromZero); Scope = scope; _currencyCode = currency.Value;
        IncludeDescendantCategories = scope == DiscountCouponScope.Categories && includeDescendantCategories;
        MinimumOrderAmount = minimumOrderAmount; MaximumTotalUses = maximumTotalUses; MaximumUsesPerCustomer = maximumUsesPerCustomer; StartsAtUtc = startsAtUtc; EndsAtUtc = endsAtUtc;
    }
}
