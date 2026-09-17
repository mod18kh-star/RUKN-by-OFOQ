using System.Net;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class StorefrontImageRequirementTests
{
    [Fact]
    public async Task PublishedProduct_WithOnePrimaryImage_IsPublic()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Image Requirement Store",
                $"image-store-{Guid.NewGuid():N}",
                now);

        tenant.Activate(
            now);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        var product =
            Product.Create(
                tenant.Id,
                "Incomplete Product",
                "incomplete-product",
                Money.Create(
                    100m,
                    "USD"),
                now);

        product.Publish(
            now.AddSeconds(1));

        var productStore =
            factory.Services
                .GetRequiredService<
                    InMemoryProductStore>();

        lock (productStore.SyncRoot)
        {
            productStore.Items.Add(
                product);
        }

        var imageStore =
            factory.Services
                .GetRequiredService<
                    InMemoryProductImageStore>();

        lock (imageStore.SyncRoot)
        {
            imageStore.Items.Add(
                ProductImage.Create(
                    tenant.Id,
                    product.Id,
                    "https://images.example.test/only.jpg",
                    "Only image",
                    0,
                    true,
                    now));
        }

        var listResponse =
            await client.GetAsync(
                $"/api/storefront/{tenant.Slug.Value}/products");

        Assert.Equal(
            HttpStatusCode.OK,
            listResponse.StatusCode);

        var detailResponse =
            await client.GetAsync(
                $"/api/storefront/{tenant.Slug.Value}/products/incomplete-product");

        Assert.Equal(
            HttpStatusCode.OK,
            detailResponse.StatusCode);
    }
}