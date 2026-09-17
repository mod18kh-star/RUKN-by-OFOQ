using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Api.Security;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Api.Security.Authorization;

public static class PlatformAdministrationAuthorization
{
    public const string PolicyName =
        "platform-administration";

    public static IServiceCollection AddPlatformAdministrationAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    PolicyName,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.AddRequirements(
                            PlatformAdministrationRequirement.Instance);
                    });
            });

        services.AddScoped<
            IAuthorizationHandler,
            PlatformAdministrationAuthorizationHandler>();

        return services;
    }
}

public sealed class PlatformAdministrationRequirement :
    IAuthorizationRequirement
{
    public static PlatformAdministrationRequirement Instance { get; } =
        new();

    private PlatformAdministrationRequirement()
    {
    }
}

public sealed class PlatformAdministrationAuthorizationHandler :
    AuthorizationHandler<PlatformAdministrationRequirement>
{
    private readonly MarketDbContext
        _dbContext;

    private readonly IHostEnvironment
        _environment;

    private readonly IConfiguration
        _configuration;

    public PlatformAdministrationAuthorizationHandler(
        MarketDbContext dbContext,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _dbContext =
            dbContext;

        _environment =
            environment;

        _configuration =
            configuration;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAdministrationRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var authenticationMethods =
            context.User
                .FindAll("amr")
                .Select(
                    claim =>
                        claim.Value)
                .ToHashSet(
                    StringComparer.Ordinal);

        /*
         * A password-authenticated session is always required.
         *
         * Production always requires MFA for Platform Administration.
         * Development permits password-only only while MFA has not yet
         * been enrolled, so the first local administrator can bootstrap
         * an authenticator. Once MFA is enabled, Development requires it
         * too and can no longer silently fall back to password-only.
         */
        if (!authenticationMethods.Contains(
                "pwd"))
        {
            return;
        }

        var subject =
            context.User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        if (!Guid.TryParse(
                subject,
                out var userGuid) ||
            userGuid == Guid.Empty)
        {
            return;
        }

        var userId =
            UserId.From(
                userGuid);

        var liveUser =
            await _dbContext
                .Set<User>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user =>
                        user.Id == userId);

        if (liveUser is null ||
            liveUser.IsDeleted ||
            liveUser.Status !=
                UserStatus.Active)
        {
            return;
        }

        var sessionHasMfa =
            authenticationMethods.Contains(
                "mfa");

        var developmentMfaBypass =
            DevelopmentSecurity
                .IsMfaBypassEnabled(
                    _environment,
                    _configuration);

        if (!sessionHasMfa &&
            !developmentMfaBypass)
        {
            if (!_environment.IsDevelopment())
            {
                return;
            }

            var mfaIsEnabled =
                await _dbContext
                    .Set<UserMfa>()
                    .AsNoTracking()
                    .AnyAsync(
                        mfa =>
                            mfa.UserId == userId &&
                            mfa.Status ==
                                UserMfaStatus.Enabled);

            if (mfaIsEnabled)
            {
                return;
            }
        }

        var isPlatformAdministrator =
            await _dbContext
                .Set<PlatformUserRoleAssignment>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(
                    assignment =>
                        assignment.UserId == userId &&
                        assignment.Role ==
                            PlatformRole.PlatformAdministrator &&
                        !assignment.IsDeleted);

        if (!isPlatformAdministrator)
        {
            return;
        }

        context.Succeed(
            requirement);
    }
}