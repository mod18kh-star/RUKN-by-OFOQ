using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Configuration;

public sealed class CommerceConfigurationDomainTests
{
    [Fact]
    public void Catalog_Apparel_ProvidesRetailCapabilities()
    {
        var definition =
            CommerceVerticalCatalog.Get(
                CommerceVerticalType.Apparel);

        Assert.Contains(
            CommerceCapabilityType.PhysicalStock,
            definition.DefaultCapabilities);

        Assert.Contains(
            CommerceCapabilityType.Variants,
            definition.DefaultCapabilities);

        Assert.Contains(
            CommerceCapabilityType.MultiWarehouse,
            definition.DefaultCapabilities);
    }

    [Fact]
    public void Catalog_RealEstate_DoesNotUsePhysicalStock()
    {
        var definition =
            CommerceVerticalCatalog.Get(
                CommerceVerticalType.RealEstate);

        Assert.Contains(
            CommerceCapabilityType.Listings,
            definition.DefaultCapabilities);

        Assert.Contains(
            CommerceCapabilityType.Maps,
            definition.DefaultCapabilities);

        Assert.DoesNotContain(
            CommerceCapabilityType.PhysicalStock,
            definition.DefaultCapabilities);
    }

    [Fact]
    public void Resolver_CombinesCapabilitiesFromMultipleVerticals()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenantId =
            TenantId.New();

        var apparel =
            TenantCommerceVertical.Create(
                tenantId,
                CommerceVerticalType.Apparel,
                true,
                now);

        var perfumes =
            TenantCommerceVertical.Create(
                tenantId,
                CommerceVerticalType.Perfumes,
                false,
                now);

        var result =
            CommerceCapabilityResolver.Resolve(
                new[]
                {
                    apparel,
                    perfumes
                },
                Array.Empty<TenantCommerceCapabilityOverride>());

        Assert.Contains(
            CommerceCapabilityType.PhysicalStock,
            result);

        Assert.Contains(
            CommerceCapabilityType.Variants,
            result);

        Assert.Contains(
            CommerceCapabilityType.Bundles,
            result);
    }

    [Fact]
    public void Resolver_DisabledVertical_DoesNotContributeCapabilities()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenantId =
            TenantId.New();

        var apparel =
            TenantCommerceVertical.Create(
                tenantId,
                CommerceVerticalType.Apparel,
                true,
                now);

        var realEstate =
            TenantCommerceVertical.Create(
                tenantId,
                CommerceVerticalType.RealEstate,
                false,
                now);

        realEstate.Disable(
            now.AddMinutes(1));

        var result =
            CommerceCapabilityResolver.Resolve(
                new[]
                {
                    apparel,
                    realEstate
                },
                Array.Empty<TenantCommerceCapabilityOverride>());

        Assert.Contains(
            CommerceCapabilityType.PhysicalStock,
            result);

        Assert.DoesNotContain(
            CommerceCapabilityType.Listings,
            result);
    }

    [Fact]
    public void Resolver_OverrideCanDisableDefaultCapability()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenantId =
            TenantId.New();

        var apparel =
            TenantCommerceVertical.Create(
                tenantId,
                CommerceVerticalType.Apparel,
                true,
                now);

        var capabilityOverride =
            TenantCommerceCapabilityOverride.Create(
                tenantId,
                CommerceCapabilityType.MultiWarehouse,
                false,
                now);

        var result =
            CommerceCapabilityResolver.Resolve(
                new[]
                {
                    apparel
                },
                new[]
                {
                    capabilityOverride
                });

        Assert.DoesNotContain(
            CommerceCapabilityType.MultiWarehouse,
            result);
    }

    [Fact]
    public void Resolver_OverrideCanEnableAdditionalCapability()
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenantId =
            TenantId.New();

        var apparel =
            TenantCommerceVertical.Create(
                tenantId,
                CommerceVerticalType.Apparel,
                true,
                now);

        var capabilityOverride =
            TenantCommerceCapabilityOverride.Create(
                tenantId,
                CommerceCapabilityType.CustomConfiguration,
                true,
                now);

        var result =
            CommerceCapabilityResolver.Resolve(
                new[]
                {
                    apparel
                },
                new[]
                {
                    capabilityOverride
                });

        Assert.Contains(
            CommerceCapabilityType.CustomConfiguration,
            result);
    }

    [Fact]
    public void PrimaryVertical_CannotBeDisabled()
    {
        var vertical =
            TenantCommerceVertical.Create(
                TenantId.New(),
                CommerceVerticalType.Apparel,
                true,
                DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () =>
                vertical.Disable(
                    DateTimeOffset.UtcNow.AddMinutes(1)));
    }
}