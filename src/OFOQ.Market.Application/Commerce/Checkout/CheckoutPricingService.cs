using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Commerce.Fulfillment;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Checkout;

public sealed record CheckoutPricingItem(ProductId ProductId, decimal LineTotal);
public sealed record CheckoutPricingContext(
    decimal ShippingAmount,
    decimal DiscountAmount,
    ShippingMethodId? ShippingMethodId,
    string? ShippingMethodName,
    string? ShippingMethodType,
    CustomerAddressId? ShippingAddressId,
    string? RecipientName,
    string? RecipientPhone,
    string? CountryCode,
    string? Region,
    string? City,
    string? PostalCode,
    string? AddressLine1,
    string? AddressLine2,
    string? CouponCode,
    DiscountCouponId? CouponId)
{
    public static CheckoutPricingContext Empty { get; } = new(0m,0m,null,null,null,null,null,null,null,null,null,null,null,null,null,null);
}

public sealed class CheckoutPricingService
{
    private readonly ICurrentTenant _tenant;
    private readonly ICustomerAddressRepository _addresses;
    private readonly IShippingMethodRepository _shipping;
    private readonly IDiscountCouponLockRepository _couponLocks;
    private readonly ICouponRedemptionRepository _redemptions;
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly TimeProvider _time;

    public CheckoutPricingService(ICurrentTenant tenant, ICustomerAddressRepository addresses, IShippingMethodRepository shipping, IDiscountCouponLockRepository couponLocks, ICouponRedemptionRepository redemptions, IProductRepository products, ICategoryRepository categories, TimeProvider time)
    { _tenant=tenant; _addresses=addresses; _shipping=shipping; _couponLocks=couponLocks; _redemptions=redemptions; _products=products; _categories=categories; _time=time; }

    public async Task<CheckoutPricingContext> ResolveAsync(UserId customerUserId, CustomerAddressId? addressId, ShippingMethodId? shippingMethodId, string? couponCode, IReadOnlyList<CheckoutPricingItem> items, CurrencyCode currency, CancellationToken ct = default)
    {
        EnsureTenant();
        var subtotal = decimal.Round(items.Sum(x=>x.LineTotal),2,MidpointRounding.AwayFromZero);
        CustomerAddress? address = null;
        ShippingMethod? method = null;
        decimal shippingAmount = 0m;

        if (addressId.HasValue)
        {
            address = await _addresses.GetByIdForUserAsync(addressId.Value, customerUserId, ct);
            if (address is null || !address.IsActive) throw new CheckoutPricingException("checkout_address_invalid","The selected shipping address is not available.");
        }

        if (shippingMethodId.HasValue)
        {
            method = await _shipping.GetByIdAsync(shippingMethodId.Value, ct);
            if (method is null || !method.IsAvailableFor(subtotal,currency)) throw new CheckoutPricingException("checkout_shipping_method_unavailable","The selected shipping method is not available for this order.");
            if (method.Type != ShippingMethodType.Pickup && address is null) throw new CheckoutPricingException("checkout_address_required","A shipping address is required for the selected shipping method.");
            shippingAmount = method.ResolveCharge();
        }

        decimal discountAmount = 0m;
        DiscountCoupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var normalized = couponCode.Trim().ToUpperInvariant();
            coupon = await _couponLocks.GetByCodeForUpdateAsync(normalized,ct);
            if (coupon is null || !coupon.IsActiveAt(_time.GetUtcNow())) throw new CheckoutPricingException("checkout_coupon_unavailable","Coupon is invalid, disabled or outside its active date range.");
            if (coupon.Currency != currency) throw new CheckoutPricingException("checkout_coupon_currency_mismatch","Coupon currency does not match the order currency.");
            if (coupon.MinimumOrderAmount.HasValue && subtotal < coupon.MinimumOrderAmount.Value) throw new CheckoutPricingException("checkout_coupon_minimum_not_met","Order minimum for this coupon has not been reached.");
            if (coupon.MaximumTotalUses.HasValue && await _redemptions.CountForCouponAsync(coupon.Id,ct) >= coupon.MaximumTotalUses.Value) throw new CheckoutPricingException("checkout_coupon_usage_limit_reached","Coupon usage limit has been reached.");
            if (coupon.MaximumUsesPerCustomer.HasValue && await _redemptions.CountForCouponAndCustomerAsync(coupon.Id,customerUserId,ct) >= coupon.MaximumUsesPerCustomer.Value) throw new CheckoutPricingException("checkout_coupon_customer_limit_reached","This customer has reached the coupon usage limit.");
            var eligible = await ResolveEligibleAmountAsync(coupon,items,ct);
            discountAmount = coupon.CalculateDiscount(eligible);
            if (discountAmount <= 0m) throw new CheckoutPricingException("checkout_coupon_not_applicable","Coupon does not apply to any item in this order.");
        }

        return new CheckoutPricingContext(
            shippingAmount,
            discountAmount,
            method?.Id,
            method?.Name,
            method?.Type.ToString(),
            address?.Id,
            address?.RecipientName,
            address?.Phone,
            address?.CountryCode,
            address?.Region,
            address?.City,
            address?.PostalCode,
            address?.Line1,
            address?.Line2,
            coupon?.Code,
            coupon?.Id);
    }

    public async Task RecordRedemptionAsync(Order order, CheckoutPricingContext context, UserId customerUserId, CancellationToken ct = default)
    {
        if (!context.CouponId.HasValue || context.DiscountAmount <= 0m) return;
        if (await _redemptions.ExistsForOrderAsync(order.Id,ct)) return;
        await _redemptions.AddAsync(CouponRedemption.Create(order.TenantId,context.CouponId.Value,order.Id,customerUserId,context.DiscountAmount,_time.GetUtcNow()),ct);
    }

    private async Task<decimal> ResolveEligibleAmountAsync(DiscountCoupon coupon, IReadOnlyList<CheckoutPricingItem> items, CancellationToken ct)
    {
        if (coupon.Scope == DiscountCouponScope.EntireStore) return items.Sum(x=>x.LineTotal);
        if (coupon.Scope == DiscountCouponScope.Products)
        {
            var targets = coupon.ProductTargets.Select(x=>x.ProductId).ToHashSet();
            return items.Where(x=>targets.Contains(x.ProductId)).Sum(x=>x.LineTotal);
        }
        var targetedCategories = coupon.CategoryTargets.Select(x=>x.CategoryId).ToHashSet();
        var categories = await _categories.GetAllAsync(ct);
        var parentById = categories.ToDictionary(x=>x.Id,x=>x.ParentCategoryId);
        decimal eligible = 0m;
        foreach (var item in items)
        {
            var product = await _products.GetByIdAsync(item.ProductId,ct);
            if (product?.CategoryId is not { } categoryId) continue;
            if (MatchesCategory(categoryId,targetedCategories,parentById,coupon.IncludeDescendantCategories)) eligible += item.LineTotal;
        }
        return eligible;
    }

    private static bool MatchesCategory(CategoryId categoryId, HashSet<CategoryId> targets, IReadOnlyDictionary<CategoryId,CategoryId?> parentById, bool includeDescendants)
    {
        if (targets.Contains(categoryId)) return true;
        if (!includeDescendants) return false;
        var current = categoryId;
        var visited = new HashSet<CategoryId>();
        while (visited.Add(current) && parentById.TryGetValue(current,out var parent) && parent.HasValue)
        {
            if (targets.Contains(parent.Value)) return true;
            current = parent.Value;
        }
        return false;
    }

    private void EnsureTenant(){ if(!_tenant.IsAvailable||!_tenant.TenantId.HasValue) throw new TenantScopeViolationException("Tenant context is required."); }
}
