using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Security;

public sealed class PublicUploadEndpointSecurityTests
{
    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task ProductAsset_RejectsSpoofedPng()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateAuthenticatedTenantAsync(
                factory,
                client);

        using var form =
            CreateFileForm(
                "attack.png",
                "image/png",
                "<html><script>alert(1)</script></html>"u8.ToArray());

        var response =
            await client.PostAsync(
                $"/api/tenants/{tenant.Id.Value}/backoffice/products/assets",
                form);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ContentAsset_RejectsSpoofedPng()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateAuthenticatedTenantAsync(
                factory,
                client);

        using var form =
            CreateFileForm(
                "attack.png",
                "image/png",
                "<html><script>alert(1)</script></html>"u8.ToArray());

        var response =
            await client.PostAsync(
                $"/api/tenants/{tenant.Id.Value}/backoffice/pages/asset",
                form);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task StorefrontAsset_RejectsSvg()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var tenant =
            await CreateAuthenticatedTenantAsync(
                factory,
                client);

        using var form =
            CreateFileForm(
                "attack.svg",
                "image/svg+xml",
                "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray(),
                "logo");

        var response =
            await client.PostAsync(
                $"/api/tenants/{tenant.Id.Value}/backoffice/storefront-presentation/asset",
                form);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static MultipartFormDataContent CreateFileForm(
        string fileName,
        string contentType,
        byte[] content,
        string? slot = null)
    {
        var form =
            new MultipartFormDataContent();

        var file =
            new ByteArrayContent(
                content);

        file.Headers.ContentType =
            new MediaTypeHeaderValue(
                contentType);

        form.Add(
            file,
            "file",
            fileName);

        if (!string.IsNullOrWhiteSpace(
                slot))
        {
            form.Add(
                new StringContent(
                    slot),
                "slot");
        }

        return form;
    }

    private static async Task<Tenant> CreateAuthenticatedTenantAsync(
        MarketApiFactory factory,
        HttpClient client)
    {
        var registration =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    $"upload-security-{Guid.NewGuid():N}@example.com",
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
                "Upload Security Store",
                $"upload-security-{Guid.NewGuid():N}",
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
                    ITestAccessTokenService>()
                .Create(
                    user.Id,
                    user.Email.Value,
                    DateTimeOffset.UtcNow,
                    AccessTokenAuthenticationLevel.MultiFactor);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.Token);

        return tenant;
    }
}