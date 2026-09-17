using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Fulfillment;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Fulfillment;

public sealed class ShippingMethodTests
{
    [Fact]
    public void FreeShipping_MustBeZero()
    {
        Assert.Throws<ArgumentException>(() => ShippingMethod.Create(TenantId.New(), "free", "Free", ShippingMethodType.Free, 10m, CurrencyCode.Create("SAR"), null, null, null, 0, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Pickup_RequiresLocation()
    {
        Assert.Throws<ArgumentException>(() => ShippingMethod.Create(TenantId.New(), "pickup", "Pickup", ShippingMethodType.Pickup, 0m, CurrencyCode.Create("SAR"), null, null, null, 0, DateTimeOffset.UtcNow));
    }
}
