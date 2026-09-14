using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Catalog;

public sealed class ProductAttributeValuePersistenceTests
{
    private readonly IntegrationTestDatabase
        _database =
            IntegrationTestDatabase.Create();

    [Fact]
    public async Task AttributeValue_RoundTrip_IsTenantScoped_AndKeyIsUnique()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenantA =
            Tenant.Create(
                "Attribute Store A",
                $"attribute-store-a-{Guid.NewGuid():N}",
                now);

        var tenantB =
            Tenant.Create(
                "Attribute Store B",
                $"attribute-store-b-{Guid.NewGuid():N}",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.AddRange(
                tenantA,
                tenantB);

            await setupContext.SaveChangesAsync();
        }

        var product =
            Product.Create(
                tenantA.Id,
                "Test Phone",
                $"test-phone-{Guid.NewGuid():N}",
                Money.Create(
                    1000m,
                    "USD"),
                now);

        await using (var productContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            productContext.Products.Add(
                product);

            await productContext.SaveChangesAsync();
        }

        await using (var contextA =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            var repository =
                new ProductAttributeValueRepository(
                    contextA);

            await repository.AddAsync(
                ProductAttributeValue.Create(
                    tenantA.Id,
                    product.Id,
                    "brand",
                    "Apple",
                    now));

            await contextA.SaveChangesAsync();
        }

        await using (var verificationContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            var repository =
                new ProductAttributeValueRepository(
                    verificationContext);

            var values =
                await repository.GetByProductIdAsync(
                    product.Id);

            var saved =
                Assert.Single(
                    values);

            Assert.Equal(
                tenantA.Id,
                saved.TenantId);

            Assert.Equal(
                product.Id,
                saved.ProductId);

            Assert.Equal(
                "brand",
                saved.Key);

            Assert.Equal(
                "Apple",
                saved.Value);
        }

        await using (var tenantBContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            var repository =
                new ProductAttributeValueRepository(
                    tenantBContext);

            var values =
                await repository.GetByProductIdAsync(
                    product.Id);

            Assert.Empty(
                values);
        }

        await using var duplicateContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        duplicateContext.ProductAttributeValues.Add(
            ProductAttributeValue.Create(
                tenantA.Id,
                product.Id,
                " BRAND ",
                "Samsung",
                now.AddMinutes(1)));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                duplicateContext.SaveChangesAsync());
    }
}