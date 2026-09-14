using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Security;

public sealed class PlatformMerchantVerificationReviewAuthorizationTests
{
    [Fact]
    public async Task ReviewPolicy_UnauthenticatedPrincipal_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var principal =
            new ClaimsPrincipal(
                new ClaimsIdentity());

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_PlatformAdministratorWithPwdAndMfa_Succeeds()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        await GrantPlatformRoleAsync(
            factory,
            user,
            PlatformRole.PlatformAdministrator);

        var principal =
            CreatePrincipal(
                user,
                "pwd",
                "mfa");

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.True(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_ComplianceReviewerWithPwdAndMfa_Succeeds()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        await GrantPlatformRoleAsync(
            factory,
            user,
            PlatformRole.ComplianceReviewer);

        var principal =
            CreatePrincipal(
                user,
                "pwd",
                "mfa");

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.True(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_PasswordOnly_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        await GrantPlatformRoleAsync(
            factory,
            user,
            PlatformRole.PlatformAdministrator);

        var principal =
            CreatePrincipal(
                user,
                "pwd");

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_MfaWithoutPassword_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        await GrantPlatformRoleAsync(
            factory,
            user,
            PlatformRole.PlatformAdministrator);

        var principal =
            CreatePrincipal(
                user,
                "mfa");

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_ActiveUserWithoutPlatformRole_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        var principal =
            CreatePrincipal(
                user,
                "pwd",
                "mfa");

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_ForgedJwtPlatformRoleWithoutDatabaseAssignment_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        var principal =
            CreatePrincipal(
                user,
                "pwd",
                "mfa");

        var identity =
            Assert.IsType<ClaimsIdentity>(
                principal.Identity);

        identity.AddClaim(
            new Claim(
                ClaimTypes.Role,
                nameof(
                    PlatformRole.PlatformAdministrator)));

        identity.AddClaim(
            new Claim(
                "role",
                nameof(
                    PlatformRole.PlatformAdministrator)));

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_TenantOwnerWithoutPlatformRole_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        var tenant =
            Tenant.Create(
                "Platform Authorization Store",
                $"platform-auth-{Guid.NewGuid():N}"[..30],
                DateTimeOffset.UtcNow,
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
                DateTimeOffset.UtcNow,
                user.Id.Value);

        var membershipRepository =
            factory.Services
                .GetRequiredService<
                    ITenantMembershipRepository>();

        await membershipRepository.AddAsync(
            membership);

        var principal =
            CreatePrincipal(
                user,
                "pwd",
                "mfa");

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_RevokedPlatformRoleWithExistingPrincipal_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        var assignment =
            await GrantPlatformRoleAsync(
                factory,
                user,
                PlatformRole.ComplianceReviewer);

        /*
         * Simulate a JWT issued while the role was still active.
         */
        var principal =
            CreatePrincipal(
                user,
                "pwd",
                "mfa");

        assignment.Delete(
            DateTimeOffset.UtcNow,
            user.Id.Value);

        /*
         * Authorization must re-read the live platform role state.
         * The already-issued principal must not preserve access.
         */
        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_SuspendedUserWithActivePlatformRole_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        await GrantPlatformRoleAsync(
            factory,
            user,
            PlatformRole.PlatformAdministrator);

        var principal =
            CreatePrincipal(
                user,
                "pwd",
                "mfa");

        user.Suspend(
            DateTimeOffset.UtcNow,
            user.Id.Value);

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_DisabledUserWithActivePlatformRole_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        await GrantPlatformRoleAsync(
            factory,
            user,
            PlatformRole.PlatformAdministrator);

        var principal =
            CreatePrincipal(
                user,
                "pwd",
                "mfa");

        user.Disable(
            DateTimeOffset.UtcNow,
            user.Id.Value);

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_DeletedUserWithActivePlatformRole_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var user =
            await CreateUserAsync(
                factory);

        await GrantPlatformRoleAsync(
            factory,
            user,
            PlatformRole.PlatformAdministrator);

        var principal =
            CreatePrincipal(
                user,
                "pwd",
                "mfa");

        user.Delete(
            DateTimeOffset.UtcNow,
            user.Id.Value);

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    [Fact]
    public async Task ReviewPolicy_InvalidSubjectClaim_Fails()
    {
        await using var factory =
            new MarketApiFactory();

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    "not-a-guid"),

                new Claim(
                    "amr",
                    "pwd"),

                new Claim(
                    "amr",
                    "mfa")
            };

        var principal =
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims,
                    authenticationType:
                        "PlatformAuthorizationTest"));

        var result =
            await AuthorizeAsync(
                factory,
                principal);

        Assert.False(
            result.Succeeded);
    }

    private static async Task<User>
        CreateUserAsync(
            MarketApiFactory factory)
    {
        var user =
            User.Create(
                $"platform-review-{Guid.NewGuid():N}@example.com",
                "integration-test-password-hash",
                DateTimeOffset.UtcNow);

        var userRepository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        await userRepository.AddAsync(
            user);

        return user;
    }

    private static async Task<PlatformUserRoleAssignment>
        GrantPlatformRoleAsync(
            MarketApiFactory factory,
            User user,
            PlatformRole role)
    {
        var assignment =
            PlatformUserRoleAssignment.Create(
                user.Id,
                role,
                DateTimeOffset.UtcNow,
                user.Id.Value);

        var repository =
            factory.Services
                .GetRequiredService<
                    IPlatformUserRoleAssignmentRepository>();

        await repository.AddAsync(
            assignment);

        return assignment;
    }

    private static ClaimsPrincipal CreatePrincipal(
        User user,
        params string[] authenticationMethods)
    {
        var claims =
            new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.Value.ToString()),

                new(
                    JwtRegisteredClaimNames.Email,
                    user.Email.Value)
            };

        foreach (var authenticationMethod in
                 authenticationMethods)
        {
            claims.Add(
                new Claim(
                    "amr",
                    authenticationMethod));
        }

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                authenticationType:
                    "PlatformAuthorizationTest"));
    }

    private static async Task<AuthorizationResult>
        AuthorizeAsync(
            MarketApiFactory factory,
            ClaimsPrincipal principal)
    {
        using var scope =
            factory.Services
                .CreateScope();

        var authorizationService =
            scope.ServiceProvider
                .GetRequiredService<
                    IAuthorizationService>();

        return await authorizationService
            .AuthorizeAsync(
                principal,
                resource:
                    null,
                AuthorizationPolicies
                    .PlatformMerchantVerificationReview);
    }
}
