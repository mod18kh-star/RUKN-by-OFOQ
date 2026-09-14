using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Catalog;

public sealed class ProductImagePersistenceTests
{
    private readonly IntegrationTestDatabase
        _database =
            IntegrationTestDatabase.Create();

    [Fact]
    public async Task ProductImages_RoundTrip_OrderAndPrimaryConstraintWork()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Image Store",
                $"image-store-{Guid.NewGuid():N}",
                now);

        await using (var context =
                     _database.CreateContext())
        {
            context.Tenants.Add(
                tenant);

            await context.SaveChangesAsync();
        }

        var product =
            Product.Create(
                tenant.Id,
                "Image Product",
                "image-product",
                Money.Create(
                    100m,
                    "USD"),
                now);

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            context.Products.Add(
                product);

            context.ProductImages.AddRange(
                ProductImage.Create(
                    tenant.Id,
                    product.Id,
                    "https://example.com/primary.jpg",
                    "Primary",
                    0,
                    true,
                    now),

                ProductImage.Create(
                    tenant.Id,
                    product.Id,
                    "https://example.com/secondary.jpg",
                    "Secondary",
                    1,
                    false,
                    now));

            await context.SaveChangesAsync();
        }

        await using (var context =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            var repository =
                new ProductImageRepository(
                    context);

            var images =
                await repository.GetByProductIdAsync(
                    product.Id);

            Assert.Equal(
                2,
                images.Count);

            Assert.True(
                images[0].IsPrimary);

            Assert.Equal(
                0,
                images[0].SortOrder);

            Assert.False(
                images[1].IsPrimary);

            Assert.Equal(
                1,
                images[1].SortOrder);
        }

        await using var duplicatePrimaryContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        duplicatePrimaryContext.ProductImages.Add(
            ProductImage.Create(
                tenant.Id,
                product.Id,
                "https://example.com/another-primary.jpg",
                null,
                2,
                true,
                now));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                duplicatePrimaryContext.SaveChangesAsync());
    }
}