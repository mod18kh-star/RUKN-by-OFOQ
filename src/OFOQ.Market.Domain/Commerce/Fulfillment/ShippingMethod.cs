using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Fulfillment;

public sealed class ShippingMethod :
    Entity<ShippingMethodId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaxCodeLength = 60;
    public const int MaxNameLength = 160;

    private ShippingMethod() { }

    private ShippingMethod(ShippingMethodId id, TenantId tenantId, string code, string name, ShippingMethodType type, decimal price, CurrencyCode currency, decimal? minimumOrderAmount, decimal? maximumOrderAmount, FulfillmentLocationId? pickupLocationId, int sortOrder, DateTimeOffset at, Guid? by)
        : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        TenantId = tenantId;
        Apply(code, name, type, price, currency, minimumOrderAmount, maximumOrderAmount, pickupLocationId, sortOrder);
        IsEnabled = true;
        CreatedAtUtc = at;
        CreatedByUserId = by;
    }

    public TenantId TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ShippingMethodType Type { get; private set; }
    public decimal Price { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public decimal? MaximumOrderAmount { get; private set; }
    public FulfillmentLocationId? PickupLocationId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static ShippingMethod Create(TenantId tenantId, string code, string name, ShippingMethodType type, decimal price, CurrencyCode currency, decimal? min, decimal? max, FulfillmentLocationId? pickupLocationId, int sortOrder, DateTimeOffset at, Guid? by = null)
        => new(ShippingMethodId.New(), tenantId, code, name, type, price, currency, min, max, pickupLocationId, sortOrder, at, by);

    public void Update(string code, string name, ShippingMethodType type, decimal price, CurrencyCode currency, decimal? min, decimal? max, FulfillmentLocationId? pickupLocationId, int sortOrder, bool isEnabled, DateTimeOffset at, Guid? by = null)
    {
        Apply(code, name, type, price, currency, min, max, pickupLocationId, sortOrder);
        IsEnabled = isEnabled;
        UpdatedAtUtc = at;
        UpdatedByUserId = by;
    }

    public bool IsAvailableFor(decimal orderAmount, CurrencyCode orderCurrency)
    {
        if (!IsEnabled || Currency != orderCurrency) return false;
        if (MinimumOrderAmount.HasValue && orderAmount < MinimumOrderAmount.Value) return false;
        if (MaximumOrderAmount.HasValue && orderAmount > MaximumOrderAmount.Value) return false;
        return true;
    }

    public decimal ResolveCharge() => Type == ShippingMethodType.Free ? 0m : Price;

    private void Apply(string code, string name, ShippingMethodType type, decimal price, CurrencyCode currency, decimal? min, decimal? max, FulfillmentLocationId? pickupLocationId, int sortOrder)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (currency.IsEmpty) throw new ArgumentException("Currency is required.", nameof(currency));
        if (price < 0m) throw new ArgumentOutOfRangeException(nameof(price));
        if (min is < 0m || max is < 0m) throw new ArgumentOutOfRangeException("Order thresholds cannot be negative.");
        if (min.HasValue && max.HasValue && min.Value > max.Value) throw new ArgumentException("Minimum order amount cannot exceed maximum order amount.");
        if (sortOrder < 0) throw new ArgumentOutOfRangeException(nameof(sortOrder));
        if (type == ShippingMethodType.Pickup && !pickupLocationId.HasValue) throw new ArgumentException("Pickup shipping methods require a pickup location.");
        if (type != ShippingMethodType.Pickup && pickupLocationId.HasValue) throw new ArgumentException("Only pickup shipping methods can reference a pickup location.");
        if (type == ShippingMethodType.Free && price != 0m) throw new ArgumentException("Free shipping must have a zero price.");

        Code = Required(code, MaxCodeLength, "Shipping method code").ToLowerInvariant();
        Name = Required(name, MaxNameLength, "Shipping method name");
        Type = type;
        Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
        MinimumOrderAmount = min;
        MaximumOrderAmount = max;
        PickupLocationId = pickupLocationId;
        SortOrder = sortOrder;
    }

    private static string Required(string value, int max, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var n = value.Trim();
        if (n.Length > max) throw new ArgumentException($"{name} cannot exceed {max} characters.");
        return n;
    }
}
