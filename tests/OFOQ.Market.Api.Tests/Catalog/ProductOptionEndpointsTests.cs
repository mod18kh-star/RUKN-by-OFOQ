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
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Catalog;

public sealed class ProductOptionEndpointsTests
{
    private const string Email =
        "product-options-owner@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task CreateOption_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var productId =
            Guid.NewGuid();

        var response =
            await client.PostAsJsonAsync(
                $"/api/tenants/{Guid.NewGuid()}/backoffice/products/{productId}/options",
                new CreateProductOptionRequest(
                    "Color"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateOption_PasswordOnlyOwner_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        SetAccessToken(
            factory,
            client,
            setup.User,
            AccessTokenAuthenticationLevel.PasswordOnly);

        var response =
            await client.PostAsJsonAsync(
                OptionsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateProductOptionRequest(
                    "Color"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateOption_MfaOwner_ReturnsCreated()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var response =
            await client.PostAsJsonAsync(
                OptionsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateProductOptionRequest(
                    "  Color  ",
                    0));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var option =
            await response.Content
                .ReadFromJsonAsync<
                    ProductOptionResponse>();

        Assert.NotNull(
            option);

        Assert.NotEqual(
            Guid.Empty,
            option.OptionId);

        Assert.Equal(
            "Color",
            option.Name);

        Assert.Equal(
            0,
            option.SortOrder);

        Assert.Empty(
            option.Values);
    }

    [Fact]
    public async Task CreateOption_DuplicateName_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var first =
            await client.PostAsJsonAsync(
                OptionsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateProductOptionRequest(
                    "Color"));

        Assert.Equal(
            HttpStatusCode.Created,
            first.StatusCode);

        var second =
            await client.PostAsJsonAsync(
                OptionsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateProductOptionRequest(
                    "COLOR"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);

        Assert.Equal(
            "product_option_name_already_exists",
            await ReadErrorCodeAsync(
                second));
    }

    [Fact]
    public async Task CreateValue_AddsValueToCorrectOption()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var option =
            await CreateOptionAsync(
                client,
                setup.Tenant.Id,
                setup.Product.ProductId,
                "Color");

        var response =
            await client.PostAsJsonAsync(
                ValuesUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId,
                    option.OptionId),
                new CreateProductOptionValueRequest(
                    "  Black  ",
                    0));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var value =
            await response.Content
                .ReadFromJsonAsync<
                    ProductOptionValueResponse>();

        Assert.NotNull(
            value);

        Assert.Equal(
            "Black",
            value.Value);

        Assert.Equal(
            0,
            value.SortOrder);
    }

    [Fact]
    public async Task CreateValue_DuplicateValue_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var option =
            await CreateOptionAsync(
                client,
                setup.Tenant.Id,
                setup.Product.ProductId,
                "Size");

        var first =
            await client.PostAsJsonAsync(
                ValuesUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId,
                    option.OptionId),
                new CreateProductOptionValueRequest(
                    "Large"));

        Assert.Equal(
            HttpStatusCode.Created,
            first.StatusCode);

        var second =
            await client.PostAsJsonAsync(
                ValuesUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId,
                    option.OptionId),
                new CreateProductOptionValueRequest(
                    "LARGE"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);

        Assert.Equal(
            "product_option_value_already_exists",
            await ReadErrorCodeAsync(
                second));
    }

    [Fact]
    public async Task GetOptions_ReturnsOptionsAndValuesInOrder()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var size =
            await CreateOptionAsync(
                client,
                setup.Tenant.Id,
                setup.Product.ProductId,
                "Size",
                1);

        var color =
            await CreateOptionAsync(
                client,
                setup.Tenant.Id,
                setup.Product.ProductId,
                "Color",
                0);

        await client.PostAsJsonAsync(
            ValuesUrl(
                setup.Tenant.Id,
                setup.Product.ProductId,
                color.OptionId),
            new CreateProductOptionValueRequest(
                "White",
                1));

        await client.PostAsJsonAsync(
            ValuesUrl(
                setup.Tenant.Id,
                setup.Product.ProductId,
                color.OptionId),
            new CreateProductOptionValueRequest(
                "Black",
                0));

        await client.PostAsJsonAsync(
            ValuesUrl(
                setup.Tenant.Id,
                setup.Product.ProductId,
                size.OptionId),
            new CreateProductOptionValueRequest(
                "M",
                0));

        var response =
            await client.GetAsync(
                OptionsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var options =
            await response.Content
                .ReadFromJsonAsync<
                    ProductOptionResponse[]>();

        Assert.NotNull(
            options);

        Assert.Equal(
            2,
            options.Length);

        Assert.Equal(
            "Color",
            options[0].Name);

        Assert.Equal(
            "Size",
            options[1].Name);

        Assert.Equal(
            2,
            options[0].Values.Count);

        Assert.Equal(
            "Black",
            options[0].Values[0].Value);

        Assert.Equal(
            "White",
            options[0].Values[1].Value);
    }

    [Fact]
    public async Task ProductOptions_DoNotLeakAcrossTenants()
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
                "Store A",
                "store-a");

        var tenantB =
            await CreateTenantAsync(
                factory,
                user,
                "Store B",
                "store-b");

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        var productA =
            await CreateProductAsync(
                client,
                tenantA.Id,
                "Product A",
                "product-a",
                "SKU-A");

        var productB =
            await CreateProductAsync(
                client,
                tenantB.Id,
                "Product B",
                "product-b",
                "SKU-B");

        var createOption =
            await client.PostAsJsonAsync(
                OptionsUrl(
                    tenantB.Id,
                    productB.ProductId),
                new CreateProductOptionRequest(
                    "Secret Option"));

        Assert.Equal(
            HttpStatusCode.Created,
            createOption.StatusCode);

        var response =
            await client.GetAsync(
                OptionsUrl(
                    tenantA.Id,
                    productB.ProductId));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var ownResponse =
            await client.GetAsync(
                OptionsUrl(
                    tenantA.Id,
                    productA.ProductId));

        Assert.Equal(
            HttpStatusCode.OK,
            ownResponse.StatusCode);

        var ownOptions =
            await ownResponse.Content
                .ReadFromJsonAsync<
                    ProductOptionResponse[]>();

        Assert.NotNull(
            ownOptions);

        Assert.Empty(
            ownOptions);
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
                "Options Store",
                "options-store");

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        var product =
            await CreateProductAsync(
                client,
                tenant.Id);

        return new TestSetup(
            user,
            tenant,
            product);
    }

    private static async Task<User>
        CreateUserAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var registration =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registration.StatusCode);

        var response =
            await registration.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            response);

        var repository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        var user =
            await repository.GetByIdAsync(
                UserId.From(
                    response.UserId));

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

    private static async Task<ProductResponse>
        CreateProductAsync(
            HttpClient client,
            TenantId tenantId,
            string name = "Product",
            string slug = "product",
            string sku = "TEST-SKU")
    {
        var response =
            await client.PostAsJsonAsync(
                ProductsUrl(
                    tenantId),
                new CreateProductRequest(
                    name,
                    slug,
                    null,
                    null,
                    100m,
                    null,
                    "USD",
                    sku,
                    true,
                    10,
                    2,
                    false));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var product =
            await response.Content
                .ReadFromJsonAsync<
                    ProductResponse>();

        Assert.NotNull(
            product);

        return product;
    }

    private static async Task<ProductOptionResponse>
        CreateOptionAsync(
            HttpClient client,
            TenantId tenantId,
            Guid productId,
            string name,
            int sortOrder = 0)
    {
        var response =
            await client.PostAsJsonAsync(
                OptionsUrl(
                    tenantId,
                    productId),
                new CreateProductOptionRequest(
                    name,
                    sortOrder));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var option =
            await response.Content
                .ReadFromJsonAsync<
                    ProductOptionResponse>();

        Assert.NotNull(
            option);

        return option;
    }

    private static void SetAccessToken(
        MarketApiFactory factory,
        HttpClient client,
        User user,
        AccessTokenAuthenticationLevel authenticationLevel)
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
                authenticationLevel);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken.Token);
    }

    private static string ProductsUrl(
        TenantId tenantId)
    {
        return
            $"/api/tenants/{tenantId.Value}/backoffice/products";
    }

    private static string OptionsUrl(
        TenantId tenantId,
        Guid productId)
    {
        return
            $"{ProductsUrl(tenantId)}/{productId}/options";
    }

    private static string ValuesUrl(
        TenantId tenantId,
        Guid productId,
        Guid optionId)
    {
        return
            $"{OptionsUrl(tenantId, productId)}/{optionId}/values";
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
            .GetProperty("code")
            .GetString();
    }

    private sealed record TestSetup(
        User User,
        Tenant Tenant,
        ProductResponse Product);
}