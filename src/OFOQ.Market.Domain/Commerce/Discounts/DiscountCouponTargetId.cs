namespace OFOQ.Market.Domain.Commerce.Discounts;

public readonly record struct DiscountCouponTargetId(Guid Value)
{
    public bool IsEmpty => Value == Guid.Empty;
    public static DiscountCouponTargetId New() => new(Guid.NewGuid());
    public static DiscountCouponTargetId From(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Discount coupon target ID cannot be empty.", nameof(value));
        return new(value);
    }
}
