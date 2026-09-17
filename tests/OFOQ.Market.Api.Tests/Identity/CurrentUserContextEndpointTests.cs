using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Identity;

public sealed class CurrentUserContextEndpointTests
{
    private const string Email =
        "platform@example.com";

    private const string Password =
        "StrongPassword123";

    [Fact]
    public async Task Me_ReturnsPersistedPlatformRoleAndSecurityState()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var registerResponse =
            await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var registration =
            await registerResponse.Content
                .ReadFromJsonAsync<
                    RegisterUserResponse>();

        Assert.NotNull(
            registration);

        var roleRepository =
            factory.Services
                .GetRequiredService<
                    IPlatformUserRoleAssignmentRepository>();

        await roleRepository.AddAsync(
            PlatformUserRoleAssignment.Create(
                UserId.From(
                    registration.UserId),
                PlatformRole.PlatformAdministrator,
                DateTimeOffset.UtcNow,
                registration.UserId));


        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginUserRequest(
                    Email,
                    Password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<
                    LoginUserResponse>();

        Assert.NotNull(
            login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        var meResponse =
            await client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            meResponse.StatusCode);

        var me =
            await meResponse.Content
                .ReadFromJsonAsync<
                    CurrentUserResponse>();

        Assert.NotNull(
            me);

        Assert.Equal(
            registration.UserId,
            me.UserId);

        Assert.Equal(
            Email,
            me.Email);

        Assert.Equal(
            "Active",
            me.Status);

        Assert.False(
            me.EmailVerified);

        Assert.False(
            me.MfaEnabled);

        Assert.False(
            me.SessionMfaVerified);

        Assert.Contains(
            "pwd",
            me.AuthenticationMethods);

        Assert.Contains(
            "PlatformAdministrator",
            me.PlatformRoles);

        Assert.False(
            me.HasTenantMemberships);
    }
}
