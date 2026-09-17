namespace OFOQ.Market.Domain.Commerce.Discounts;

public readonly record struct DiscountCouponId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static DiscountCouponId New() => new(Guid.NewGuid());
    public static DiscountCouponId From(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Discount coupon ID cannot be empty.", nameof(value));
        return new(value);
    }
}
