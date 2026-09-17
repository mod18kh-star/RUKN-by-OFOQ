using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Commerce.Fulfillment;

namespace OFOQ.Market.Domain.Commerce.Orders;

public sealed partial class Order
{
    private decimal _shippingAmount;
    private decimal _discountAmount;
    private ShippingMethodId? _shippingMethodId;
    private CustomerAddressId? _shippingAddressId;

    public decimal ShippingAmount => _shippingAmount;
    public decimal DiscountAmount => _discountAmount;
    public ShippingMethodId? ShippingMethodId => _shippingMethodId;
    public CustomerAddressId? ShippingAddressId => _shippingAddressId;
    public string? ShippingMethodName { get; private set; }
    public string? ShippingMethodType { get; private set; }
    public string? ShippingRecipientName { get; private set; }
    public string? ShippingRecipientPhone { get; private set; }
    public string? ShippingCountryCode { get; private set; }
    public string? ShippingRegion { get; private set; }
    public string? ShippingCity { get; private set; }
    public string? ShippingPostalCode { get; private set; }
    public string? ShippingAddressLine1 { get; private set; }
    public string? ShippingAddressLine2 { get; private set; }
    public string? AppliedCouponCode { get; private set; }

    public void ApplyCheckoutContext(decimal shippingAmount, decimal discountAmount, ShippingMethodId? shippingMethodId, string? shippingMethodName, string? shippingMethodType, CustomerAddressId? shippingAddressId, string? recipientName, string? recipientPhone, string? countryCode, string? region, string? city, string? postalCode, string? addressLine1, string? addressLine2, string? couponCode, DateTimeOffset updatedAtUtc, Guid? updatedByUserId = null)
    {
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Checkout pricing can only be applied to a pending order.");
        if (shippingAmount < 0m || discountAmount < 0m) throw new ArgumentOutOfRangeException("Shipping and discount amounts cannot be negative.");
        var subtotal = _items.Sum(x=>x.LineTotal);
        if (discountAmount > subtotal) throw new ArgumentException("Discount amount cannot exceed order subtotal.");
        _shippingAmount = decimal.Round(shippingAmount,2,MidpointRounding.AwayFromZero);
        _discountAmount = decimal.Round(discountAmount,2,MidpointRounding.AwayFromZero);
        _shippingMethodId = shippingMethodId;
        _shippingAddressId = shippingAddressId;
        ShippingMethodName = Trim(shippingMethodName,160);
        ShippingMethodType = Trim(shippingMethodType,40);
        ShippingRecipientName = Trim(recipientName,160);
        ShippingRecipientPhone = Trim(recipientPhone,40);
        ShippingCountryCode = Trim(countryCode,2);
        ShippingRegion = Trim(region,120);
        ShippingCity = Trim(city,120);
        ShippingPostalCode = Trim(postalCode,32);
        ShippingAddressLine1 = Trim(addressLine1,240);
        ShippingAddressLine2 = Trim(addressLine2,240);
        AppliedCouponCode = Trim(couponCode,60)?.ToUpperInvariant();
        MarkUpdated(updatedAtUtc,updatedByUserId);
    }

    private static string? Trim(string? value,int max)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        var n=value.Trim(); if(n.Length>max)throw new ArgumentException($"Snapshot value cannot exceed {max} characters."); return n;
    }
}
