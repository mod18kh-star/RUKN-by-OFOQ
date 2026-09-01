using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Contracts.Tenancy;

namespace OFOQ.Market.Api.Tests.Tenancy;

public sealed class TenantEndpointsTests
{
    [Fact]
    public async Task PostTenant_ReturnsCreated()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var request =
            new CreateTenantRequest(
                "Turks Store",
                "TURKS");

        var response =
            await client.PostAsJsonAsync(
                "/api/tenants",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<CreateTenantResponse>();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.TenantId);
        Assert.Equal("Turks Store", result.Name);
        Assert.Equal("turks", result.Slug);
        Assert.Equal("Draft", result.Status);
    }

    [Fact]
    public async Task PostTenant_ReturnsConflict_WhenSlugAlreadyExists()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/tenants",
            new CreateTenantRequest(
                "First Store",
                "turks"));

        var response =
            await client.PostAsJsonAsync(
                "/api/tenants",
                new CreateTenantRequest(
                    "Second Store",
                    "turks"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        using var json =
            JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

        Assert.Equal(
            "tenant_slug_already_exists",
            json.RootElement
                .GetProperty("code")
                .GetString());
    }

    [Fact]
    public async Task GetTenant_ReturnsOk_WhenTenantExists()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/tenants",
                new CreateTenantRequest(
                    "Turks Store",
                    "turks"));

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created =
            await createResponse.Content
                .ReadFromJsonAsync<CreateTenantResponse>();

        Assert.NotNull(created);

        var response =
            await client.GetAsync(
                $"/api/tenants/{created.TenantId}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<GetTenantByIdResponse>();

        Assert.NotNull(result);
        Assert.Equal(created.TenantId, result.TenantId);
        Assert.Equal("Turks Store", result.Name);
        Assert.Equal("turks", result.Slug);
        Assert.Equal("Draft", result.Status);
    }

    [Fact]
    public async Task GetTenant_ReturnsNotFound_WhenTenantDoesNotExist()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                $"/api/tenants/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetTenant_ReturnsBadRequest_WhenTenantIdIsInvalid()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/tenants/not-a-guid");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}