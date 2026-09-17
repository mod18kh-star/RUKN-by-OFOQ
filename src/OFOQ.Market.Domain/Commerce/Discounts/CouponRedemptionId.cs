namespace OFOQ.Market.Domain.Commerce.Discounts;
public readonly record struct CouponRedemptionId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CouponRedemptionId New() => new(Guid.NewGuid());
    public static CouponRedemptionId From(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Coupon redemption ID cannot be empty.", nameof(value));
        return new(value);
    }
}
