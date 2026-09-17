using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Catalog;

public sealed class ProductImageEndpointsTests
{
    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task Images_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            Tenant.Create(
                "Image Store",
                $"image-store-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        await factory.Services
            .GetRequiredService<
                ITenantRepository>()
            .AddAsync(
                tenant);

        var response =
            await client.GetAsync(
                $"/api/tenants/{tenant.Id.Value}/backoffice/products/{Guid.NewGuid()}/images");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task PutImages_SavesPrimaryAndAdditionalImage()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var product =
            SeedProduct(
                factory,
                setup.Tenant);

        var response =
            await client.PutAsJsonAsync(
                Url(
                    setup.Tenant,
                    product),
                new SetProductImagesRequest(
                    new[]
                    {
                        new SetProductImageRequest(
                            "https://example.com/main.jpg",
                            "Main",
                            true),

                        new SetProductImageRequest(
                            "https://example.com/second.jpg",
                            "Second",
                            false)
                    }));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ProductImagesResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            2,
            result.Images.Count);

        Assert.True(
            result.Images[0].IsPrimary);

        Assert.Equal(
            0,
            result.Images[0].SortOrder);

        Assert.False(
            result.Images[1].IsPrimary);

        Assert.Equal(
            1,
            result.Images[1].SortOrder);

        var getResponse =
            await client.GetAsync(
                Url(
                    setup.Tenant,
                    product));

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task PutImages_OneImage_SavesPrimaryImage()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var product =
            SeedProduct(
                factory,
                setup.Tenant);

        var response =
            await client.PutAsJsonAsync(
                Url(
                    setup.Tenant,
                    product),
                new SetProductImagesRequest(
                    new[]
                    {
                        new SetProductImageRequest(
                            "https://example.com/only.jpg",
                            null,
                            true)
                    }));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task PutImages_TwoPrimaryImages_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var product =
            SeedProduct(
                factory,
                setup.Tenant);

        var response =
            await client.PutAsJsonAsync(
                Url(
                    setup.Tenant,
                    product),
                new SetProductImagesRequest(
                    new[]
                    {
                        new SetProductImageRequest(
                            "https://example.com/a.jpg",
                            null,
                            true),

                        new SetProductImageRequest(
                            "https://example.com/b.jpg",
                            null,
                            true)
                    }));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static Product SeedProduct(
        MarketApiFactory factory,
        Tenant tenant)
    {
        var product =
            Product.Create(
                tenant.Id,
                "Image Test Product",
                $"image-product-{Guid.NewGuid():N}",
                Money.Create(
                    100m,
                    "USD"),
                DateTimeOffset.UtcNow);

        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryProductStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                product);
        }

        return product;
    }

    private static async Task<TestSetup> CreateSetupAsync(
        MarketApiFactory factory,
        HttpClient client)
    {
        var registration =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    $"images-{Guid.NewGuid():N}@example.com",
                    Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registration.StatusCode);

        var registered =
            await registration.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            registered);

        var user =
            await factory.Services
                .GetRequiredService<
                    IUserRepository>()
                .GetByIdAsync(
                    UserId.From(
                        registered.UserId));

        Assert.NotNull(
            user);

        var tenant =
            Tenant.Create(
                "Product Image Store",
                $"product-images-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow,
                user.Id.Value);

        await factory.Services
            .GetRequiredService<
                ITenantRepository>()
            .AddAsync(
                tenant);

        await factory.Services
            .GetRequiredService<
                ITenantMembershipRepository>()
            .AddAsync(
                TenantMembership.Create(
                    tenant.Id,
                    user.Id,
                    TenantRole.Owner,
                    DateTimeOffset.UtcNow,
                    user.Id.Value));

        var token =
            factory.Services
                .GetRequiredService<
                    IAccessTokenService>()
                .Create(
                    user.Id,
                    user.Email.Value,
                    DateTimeOffset.UtcNow,
                    AccessTokenAuthenticationLevel.MultiFactor);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.Token);

        return new TestSetup(
            tenant);
    }

    private static string Url(
        Tenant tenant,
        Product product) =>
        $"/api/tenants/{tenant.Id.Value}/backoffice/products/{product.Id.Value}/images";

    private sealed record TestSetup(
        Tenant Tenant);
}