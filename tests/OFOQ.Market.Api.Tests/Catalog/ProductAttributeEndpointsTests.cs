using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Catalog;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Catalog;

public sealed class ProductAttributeEndpointsTests
{
    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task Attributes_WithoutAccessToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            Tenant.Create(
                "Attributes Store",
                $"attributes-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        var response =
            await client.GetAsync(
                AttributesUrl(
                    tenant.Id,
                    ProductId.New()));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAttributes_WithoutPrimaryVertical_ReturnsConflict()
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
                setup.Tenant,
                setup.User);

        var response =
            await client.GetAsync(
                AttributesUrl(
                    setup.Tenant.Id,
                    product.Id));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Equal(
            "commerce_vertical_not_configured",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task GetAttributes_ReturnsPrimaryVerticalSchema()
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
                setup.Tenant,
                setup.User);

        SeedPrimaryVertical(
            factory,
            setup.Tenant,
            setup.User,
            CommerceVerticalType.MobilePhones);

        var response =
            await client.GetAsync(
                AttributesUrl(
                    setup.Tenant.Id,
                    product.Id));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ProductAttributesResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            product.Id.Value,
            result.ProductId);

        Assert.Equal(
            nameof(
                CommerceVerticalType.MobilePhones),
            result.Vertical);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.VerticalCode));

        Assert.Contains(
            result.Attributes,
            attribute =>
                attribute.Key ==
                    "brand" &&
                attribute.Value is null);

        Assert.Contains(
            result.Attributes,
            attribute =>
                attribute.Key ==
                    "storage-gb" &&
                attribute.ValueType ==
                    "Integer");

        Assert.Contains(
            result.Attributes,
            attribute =>
                attribute.Key ==
                    "dual-sim" &&
                attribute.ValueType ==
                    "Boolean");
    }

    [Fact]
    public async Task SetAttributes_NormalizesAndPersistsValues()
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
                setup.Tenant,
                setup.User);

        SeedPrimaryVertical(
            factory,
            setup.Tenant,
            setup.User,
            CommerceVerticalType.MobilePhones);

        var request =
            new SetProductAttributesRequest(
                new[]
                {
                    new SetProductAttributeValueRequest(
                        "brand",
                        " Apple "),

                    new SetProductAttributeValueRequest(
                        "storage-gb",
                        "0256"),

                    new SetProductAttributeValueRequest(
                        "ram-gb",
                        "08"),

                    new SetProductAttributeValueRequest(
                        "dual-sim",
                        "TRUE"),

                    new SetProductAttributeValueRequest(
                        "color",
                        " Black ")
                });

        var response =
            await client.PutAsJsonAsync(
                AttributesUrl(
                    setup.Tenant.Id,
                    product.Id),
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ProductAttributesResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            "Apple",
            GetValue(
                result,
                "brand"));

        Assert.Equal(
            "256",
            GetValue(
                result,
                "storage-gb"));

        Assert.Equal(
            "8",
            GetValue(
                result,
                "ram-gb"));

        Assert.Equal(
            "true",
            GetValue(
                result,
                "dual-sim"));

        Assert.Equal(
            "Black",
            GetValue(
                result,
                "color"));

        var getResponse =
            await client.GetAsync(
                AttributesUrl(
                    setup.Tenant.Id,
                    product.Id));

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var roundTrip =
            await getResponse.Content
                .ReadFromJsonAsync<
                    ProductAttributesResponse>();

        Assert.NotNull(
            roundTrip);

        Assert.Equal(
            "256",
            GetValue(
                roundTrip,
                "storage-gb"));
    }

    [Fact]
    public async Task SetAttributes_InvalidValue_ReturnsBadRequest()
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
                setup.Tenant,
                setup.User);

        SeedPrimaryVertical(
            factory,
            setup.Tenant,
            setup.User,
            CommerceVerticalType.MobilePhones);

        var response =
            await client.PutAsJsonAsync(
                AttributesUrl(
                    setup.Tenant.Id,
                    product.Id),
                new SetProductAttributesRequest(
                    new[]
                    {
                        new SetProductAttributeValueRequest(
                            "storage-gb",
                            "many")
                    }));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "product_attributes_invalid",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task SetAttributes_UnsupportedField_ReturnsBadRequest()
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
                setup.Tenant,
                setup.User);

        SeedPrimaryVertical(
            factory,
            setup.Tenant,
            setup.User,
            CommerceVerticalType.MobilePhones);

        var response =
            await client.PutAsJsonAsync(
                AttributesUrl(
                    setup.Tenant.Id,
                    product.Id),
                new SetProductAttributesRequest(
                    new[]
                    {
                        new SetProductAttributeValueRequest(
                            "shoe-size",
                            "42")
                    }));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "product_attributes_invalid",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task Attributes_CrossTenantProduct_ReturnsNotFound()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory,
                client);

        var tenantA =
            await CreateTenantAsync(
                factory,
                user,
                "Attribute Store A",
                $"attribute-a-{Guid.NewGuid():N}");

        var tenantB =
            await CreateTenantAsync(
                factory,
                user,
                "Attribute Store B",
                $"attribute-b-{Guid.NewGuid():N}");

        SetAccessToken(
            factory,
            client,
            user);

        var product =
            SeedProduct(
                factory,
                tenantA,
                user);

        SeedPrimaryVertical(
            factory,
            tenantB,
            user,
            CommerceVerticalType.MobilePhones);

        var response =
            await client.GetAsync(
                AttributesUrl(
                    tenantB.Id,
                    product.Id));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task SetAttributes_NullValues_ReturnsBadRequest()
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
                setup.Tenant,
                setup.User);

        SeedPrimaryVertical(
            factory,
            setup.Tenant,
            setup.User,
            CommerceVerticalType.MobilePhones);

        using var content =
            JsonContent.Create(
                new
                {
                    values =
                        (object?)null
                });

        var response =
            await client.PutAsync(
                AttributesUrl(
                    setup.Tenant.Id,
                    product.Id),
                content);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "product_attributes_invalid",
            await ReadErrorCodeAsync(
                response));
    }

    private static async Task<TestSetup>
        CreateSetupAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var user =
            await CreateUserAsync(
                factory,
                client);

        var tenant =
            await CreateTenantAsync(
                factory,
                user,
                "Product Attribute Store",
                $"product-attributes-{Guid.NewGuid():N}");

        SetAccessToken(
            factory,
            client,
            user);

        return new TestSetup(
            user,
            tenant);
    }

    private static async Task<User>
        CreateUserAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var registrationResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    $"attributes-{Guid.NewGuid():N}@example.com",
                    Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registrationResponse.StatusCode);

        var registration =
            await registrationResponse.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            registration);

        var userRepository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        var user =
            await userRepository.GetByIdAsync(
                UserId.From(
                    registration.UserId));

        Assert.NotNull(
            user);

        return user;
    }

    private static async Task<Tenant>
        CreateTenantAsync(
            MarketApiFactory factory,
            User user,
            string name,
            string slug)
    {
        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                name,
                slug,
                now,
                user.Id.Value);

        var tenantRepository =
            factory.Services
                .GetRequiredService<
                    ITenantRepository>();

        await tenantRepository.AddAsync(
            tenant);

        var membership =
            TenantMembership.Create(
                tenant.Id,
                user.Id,
                TenantRole.Owner,
                now,
                user.Id.Value);

        var membershipRepository =
            factory.Services
                .GetRequiredService<
                    ITenantMembershipRepository>();

        await membershipRepository.AddAsync(
            membership);

        return tenant;
    }

    private static Product SeedProduct(
        MarketApiFactory factory,
        Tenant tenant,
        User user)
    {
        var product =
            Product.Create(
                tenant.Id,
                "Test Phone",
                $"test-phone-{Guid.NewGuid():N}",
                Money.Create(
                    999m,
                    "USD"),
                DateTimeOffset.UtcNow,
                createdByUserId:
                    user.Id.Value);

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

    private static void SeedPrimaryVertical(
        MarketApiFactory factory,
        Tenant tenant,
        User user,
        CommerceVerticalType verticalType)
    {
        var vertical =
            TenantCommerceVertical.Create(
                tenant.Id,
                verticalType,
                isPrimary: true,
                DateTimeOffset.UtcNow,
                user.Id.Value);

        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryTenantCommerceVerticalStore>();

        lock (store.SyncRoot)
        {
            store.Items.Add(
                vertical);
        }
    }

    private static void SetAccessToken(
        MarketApiFactory factory,
        HttpClient client,
        User user)
    {
        var accessTokenService =
            factory.Services
                .GetRequiredService<
                    IAccessTokenService>();

        var accessToken =
            accessTokenService.Create(
                user.Id,
                user.Email.Value,
                DateTimeOffset.UtcNow,
                AccessTokenAuthenticationLevel.MultiFactor);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken.Token);
    }

    private static string? GetValue(
        ProductAttributesResponse response,
        string key)
    {
        return response.Attributes
            .Single(
                attribute =>
                    attribute.Key ==
                    key)
            .Value;
    }

    private static string AttributesUrl(
        TenantId tenantId,
        ProductId productId)
    {
        return
            $"/api/tenants/{tenantId.Value}" +
            $"/backoffice/products/{productId.Value}/attributes";
    }

    private static async Task<string?>
        ReadErrorCodeAsync(
            HttpResponseMessage response)
    {
        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return json.RootElement
            .GetProperty(
                "code")
            .GetString();
    }

    private sealed record TestSetup(
        User User,
        Tenant Tenant);
}