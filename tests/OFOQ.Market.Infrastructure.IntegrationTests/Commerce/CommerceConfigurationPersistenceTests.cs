using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class CommerceConfigurationPersistenceTests
{
    [Fact]
    public void Model_ContainsTenantCommerceVerticalMapping()
    {
        using var dbContext =
            CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(
                    TenantCommerceVertical));

        Assert.NotNull(
            entityType);

        Assert.Equal(
            "commerce_tenant_verticals",
            entityType.GetTableName());

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.GetDatabaseName() ==
                "ux_commerce_verticals_tenant_vertical");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.GetDatabaseName() ==
                "ux_commerce_verticals_tenant_primary");

        Assert.NotEmpty(
            entityType.GetDeclaredQueryFilters());
    }

    [Fact]
    public void Model_ContainsCapabilityOverrideMapping()
    {
        using var dbContext =
            CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(
                    TenantCommerceCapabilityOverride));

        Assert.NotNull(
            entityType);

        Assert.Equal(
            "commerce_tenant_capability_overrides",
            entityType.GetTableName());

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.GetDatabaseName() ==
                "ux_commerce_cap_overrides_tenant_capability");

        Assert.NotEmpty(
            entityType.GetDeclaredQueryFilters());
    }

    [Fact]
    public void Model_UsesRestrictTenantRelationships()
    {
        using var dbContext =
            CreateDbContext();

        var verticalType =
            dbContext.Model.FindEntityType(
                typeof(
                    TenantCommerceVertical));

        var capabilityOverrideType =
            dbContext.Model.FindEntityType(
                typeof(
                    TenantCommerceCapabilityOverride));

        Assert.NotNull(
            verticalType);

        Assert.NotNull(
            capabilityOverrideType);

        Assert.Contains(
            verticalType.GetForeignKeys(),
            foreignKey =>
                foreignKey.DeleteBehavior ==
                DeleteBehavior.Restrict);

        Assert.Contains(
            capabilityOverrideType.GetForeignKeys(),
            foreignKey =>
                foreignKey.DeleteBehavior ==
                DeleteBehavior.Restrict);
    }

    private static MarketDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<MarketDbContext>()
                .UseNpgsql(
                    "Host=127.0.0.1;Port=5432;Database=unused;Username=unused;Password=unused")
                .Options;

        return new MarketDbContext(
            options,
            new TestCurrentTenant(
                TenantId.New()));
    }
}