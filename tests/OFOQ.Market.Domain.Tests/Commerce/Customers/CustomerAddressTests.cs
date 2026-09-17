using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Customers;

public sealed class CustomerAddressTests
{
    [Fact]
    public void Create_NormalizesCountryAndAllowsDefault()
    {
        var x = CustomerAddress.Create(TenantId.New(), UserId.New(), " Home ", " Customer ", "+966500000000", "sa", null, "Riyadh", null, "Street 1", null, true, DateTimeOffset.UtcNow);
        Assert.Equal("SA", x.CountryCode);
        Assert.True(x.IsDefault);
    }

    [Fact]
    public void Deactivate_ClearsDefault()
    {
        var x = CustomerAddress.Create(TenantId.New(), UserId.New(), "Home", "Customer", "0500000000", "SA", null, "Riyadh", null, "Street 1", null, true, DateTimeOffset.UtcNow);
        x.Deactivate(DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.False(x.IsDefault);
        Assert.False(x.IsActive);
    }
}
