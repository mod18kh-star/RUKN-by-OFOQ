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

public sealed class ProductVariantEndpointsTests
{
    private const string Email =
        "product-variants-owner@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task CreateVariant_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                VariantsUrl(
                    TenantId.New(),
                    Guid.NewGuid()),
                new CreateStructuredProductVariantRequest(
                    "Black / M",
                    "SHIRT-BLACK-M",
                    null,
                    true,
                    10,
                    2,
                    false,
                    [Guid.NewGuid()]));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateVariant_PasswordOnlyOwner_ReturnsForbidden()
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
                VariantsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateStructuredProductVariantRequest(
                    "Black / M",
                    "SHIRT-BLACK-M",
                    29.99m,
                    true,
                    17,
                    3,
                    false,
                    [
                        setup.Black.ValueId,
                        setup.Medium.ValueId
                    ]));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateVariant_MfaOwner_CreatesStructuredVariant()
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
                VariantsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateStructuredProductVariantRequest(
                    "Black / M",
                    "shirt-black-m",
                    29.99m,
                    true,
                    17,
                    3,
                    false,
                    [
                        setup.Black.ValueId,
                        setup.Medium.ValueId
                    ]));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var variant =
            await response.Content
                .ReadFromJsonAsync<
                    StructuredProductVariantResponse>();

        Assert.NotNull(
            variant);

        Assert.NotEqual(
            Guid.Empty,
            variant.VariantId);

        Assert.Equal(
            "SHIRT-BLACK-M",
            variant.Sku);

        Assert.Equal(
            29.99m,
            variant.PriceOverride);

        Assert.Equal(
            "USD",
            variant.PriceOverrideCurrency);

        Assert.True(
            variant.TrackInventory);

        Assert.Equal(
            17,
            variant.Quantity);

        Assert.Equal(
            3,
            variant.LowStockThreshold);

        Assert.True(
            variant.IsAvailableForSale);

        Assert.True(
            variant.IsEnabled);

        Assert.False(
            variant.IsDefault);

        Assert.Equal(
            2,
            variant.Selections.Count);

        Assert.Contains(
            variant.Selections,
            selection =>
                selection.OptionName == "Color" &&
                selection.Value == "Black");

        Assert.Contains(
            variant.Selections,
            selection =>
                selection.OptionName == "Size" &&
                selection.Value == "M");
    }

    [Fact]
    public async Task CreateVariant_DuplicateSku_ReturnsConflict()
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
            await CreateVariantAsync(
                client,
                setup,
                "Black / M",
                "SHIRT-BLACK-M",
                setup.Black.ValueId,
                setup.Medium.ValueId);

        Assert.Equal(
            HttpStatusCode.Created,
            first.StatusCode);

        var second =
            await client.PostAsJsonAsync(
                VariantsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateStructuredProductVariantRequest(
                    "White / L",
                    "SHIRT-BLACK-M",
                    null,
                    true,
                    5,
                    1,
                    false,
                    [
                        setup.White.ValueId,
                        setup.Large.ValueId
                    ]));

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);

        Assert.Equal(
            "product_sku_already_exists",
            await ReadErrorCodeAsync(
                second));
    }

    [Fact]
    public async Task CreateVariant_DuplicateCombination_ReturnsConflict()
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
            await CreateVariantAsync(
                client,
                setup,
                "Black / M",
                "SHIRT-BLACK-M",
                setup.Black.ValueId,
                setup.Medium.ValueId);

        Assert.Equal(
            HttpStatusCode.Created,
            first.StatusCode);

        var second =
            await CreateVariantAsync(
                client,
                setup,
                "Another Black / M",
                "SHIRT-BLACK-M-2",
                setup.Black.ValueId,
                setup.Medium.ValueId);

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);

        Assert.Equal(
            "product_variant_combination_already_exists",
            await ReadErrorCodeAsync(
                second));
    }

    [Fact]
    public async Task CreateVariant_MissingOneOption_ReturnsBadRequest()
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
                VariantsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateStructuredProductVariantRequest(
                    "Black",
                    "SHIRT-BLACK",
                    null,
                    true,
                    10,
                    2,
                    false,
                    [
                        setup.Black.ValueId
                    ]));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "validation_error",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task CreateVariant_TwoValuesFromSameOption_ReturnsBadRequest()
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
                VariantsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateStructuredProductVariantRequest(
                    "Invalid",
                    "INVALID-COLORS",
                    null,
                    true,
                    10,
                    2,
                    false,
                    [
                        setup.Black.ValueId,
                        setup.White.ValueId
                    ]));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "validation_error",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task CreateVariant_ValueFromAnotherProduct_ReturnsBadRequest()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var productTwo =
            await CreateProductAsync(
                client,
                setup.Tenant.Id,
                "Product Two",
                "product-two",
                "PRODUCT-TWO-BASE");

        var optionTwo =
            await CreateOptionAsync(
                client,
                setup.Tenant.Id,
                productTwo.ProductId,
                "Material");

        var cotton =
            await CreateValueAsync(
                client,
                setup.Tenant.Id,
                productTwo.ProductId,
                optionTwo.OptionId,
                "Cotton");

        var response =
            await client.PostAsJsonAsync(
                VariantsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId),
                new CreateStructuredProductVariantRequest(
                    "Invalid",
                    "INVALID-FOREIGN-VALUE",
                    null,
                    true,
                    10,
                    2,
                    false,
                    [
                        setup.Black.ValueId,
                        cotton.ValueId
                    ]));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "product_variant_option_value_not_found",
            await ReadErrorCodeAsync(
                response));
    }

    [Fact]
    public async Task GetVariants_ReturnsStructuredVariantWithSelections()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateSetupAsync(
                factory,
                client);

        var create =
            await CreateVariantAsync(
                client,
                setup,
                "Black / M",
                "SHIRT-BLACK-M",
                setup.Black.ValueId,
                setup.Medium.ValueId);

        Assert.Equal(
            HttpStatusCode.Created,
            create.StatusCode);

        var response =
            await client.GetAsync(
                VariantsUrl(
                    setup.Tenant.Id,
                    setup.Product.ProductId));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var variants =
            await response.Content
                .ReadFromJsonAsync<
                    StructuredProductVariantResponse[]>();

        Assert.NotNull(
            variants);

        var structured =
            Assert.Single(
                variants,
                variant =>
                    variant.Sku ==
                    "SHIRT-BLACK-M");

        Assert.Equal(
            2,
            structured.Selections.Count);

        Assert.Contains(
            structured.Selections,
            selection =>
                selection.OptionName == "Color" &&
                selection.Value == "Black");

        Assert.Contains(
            structured.Selections,
            selection =>
                selection.OptionName == "Size" &&
                selection.Value == "M");
    }

    [Fact]
    public async Task Variants_DoNotLeakAcrossTenants()
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
                "PRODUCT-A-BASE");

        var productB =
            await CreateProductAsync(
                client,
                tenantB.Id,
                "Product B",
                "product-b",
                "PRODUCT-B-BASE");

        var colorB =
            await CreateOptionAsync(
                client,
                tenantB.Id,
                productB.ProductId,
                "Color");

        var blackB =
            await CreateValueAsync(
                client,
                tenantB.Id,
                productB.ProductId,
                colorB.OptionId,
                "Black");

        var createB =
            await client.PostAsJsonAsync(
                VariantsUrl(
                    tenantB.Id,
                    productB.ProductId),
                new CreateStructuredProductVariantRequest(
                    "Black",
                    "PRODUCT-B-BLACK",
                    null,
                    true,
                    10,
                    2,
                    false,
                    [
                        blackB.ValueId
                    ]));

        Assert.Equal(
            HttpStatusCode.Created,
            createB.StatusCode);

        var foreignResponse =
            await client.GetAsync(
                VariantsUrl(
                    tenantA.Id,
                    productB.ProductId));

        Assert.Equal(
            HttpStatusCode.NotFound,
            foreignResponse.StatusCode);

        var ownResponse =
            await client.GetAsync(
                VariantsUrl(
                    tenantA.Id,
                    productA.ProductId));

        Assert.Equal(
            HttpStatusCode.OK,
            ownResponse.StatusCode);

        var variantsA =
            await ownResponse.Content
                .ReadFromJsonAsync<
                    StructuredProductVariantResponse[]>();

        Assert.NotNull(
            variantsA);

        Assert.DoesNotContain(
            variantsA,
            variant =>
                variant.Sku ==
                "PRODUCT-B-BLACK");
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
                "Variant Store",
                "variant-store");

        SetAccessToken(
            factory,
            client,
            user,
            AccessTokenAuthenticationLevel.MultiFactor);

        var product =
            await CreateProductAsync(
                client,
                tenant.Id);

        var color =
            await CreateOptionAsync(
                client,
                tenant.Id,
                product.ProductId,
                "Color",
                0);

        var size =
            await CreateOptionAsync(
                client,
                tenant.Id,
                product.ProductId,
                "Size",
                1);

        var black =
            await CreateValueAsync(
                client,
                tenant.Id,
                product.ProductId,
                color.OptionId,
                "Black",
                0);

        var white =
            await CreateValueAsync(
                client,
                tenant.Id,
                product.ProductId,
                color.OptionId,
                "White",
                1);

        var medium =
            await CreateValueAsync(
                client,
                tenant.Id,
                product.ProductId,
                size.OptionId,
                "M",
                0);

        var large =
            await CreateValueAsync(
                client,
                tenant.Id,
                product.ProductId,
                size.OptionId,
                "L",
                1);

        return new TestSetup(
            user,
            tenant,
            product,
            color,
            size,
            black,
            white,
            medium,
            large);
    }

    private static async Task<User>
        CreateUserAsync(
            MarketApiFactory factory,
            HttpClient client)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var registration =
            await response.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            registration);

        var repository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        var user =
            await repository.GetByIdAsync(
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

    private static async Task<ProductResponse>
        CreateProductAsync(
            HttpClient client,
            TenantId tenantId,
            string name = "T-Shirt",
            string slug = "t-shirt",
            string sku = "TSHIRT-BASE")
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
                    25m,
                    null,
                    "USD",
                    sku,
                    true,
                    100,
                    5,
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

    private static async Task<ProductOptionValueResponse>
        CreateValueAsync(
            HttpClient client,
            TenantId tenantId,
            Guid productId,
            Guid optionId,
            string value,
            int sortOrder = 0)
    {
        var response =
            await client.PostAsJsonAsync(
                ValuesUrl(
                    tenantId,
                    productId,
                    optionId),
                new CreateProductOptionValueRequest(
                    value,
                    sortOrder));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    ProductOptionValueResponse>();

        Assert.NotNull(
            result);

        return result;
    }

    private static Task<HttpResponseMessage>
        CreateVariantAsync(
            HttpClient client,
            TestSetup setup,
            string name,
            string sku,
            Guid firstValueId,
            Guid secondValueId)
    {
        return client.PostAsJsonAsync(
            VariantsUrl(
                setup.Tenant.Id,
                setup.Product.ProductId),
            new CreateStructuredProductVariantRequest(
                name,
                sku,
                null,
                true,
                10,
                2,
                false,
                [
                    firstValueId,
                    secondValueId
                ]));
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

    private static string VariantsUrl(
        TenantId tenantId,
        Guid productId)
    {
        return
            $"{ProductsUrl(tenantId)}/{productId}/variants";
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
        ProductResponse Product,
        ProductOptionResponse Color,
        ProductOptionResponse Size,
        ProductOptionValueResponse Black,
        ProductOptionValueResponse White,
        ProductOptionValueResponse Medium,
        ProductOptionValueResponse Large);
}