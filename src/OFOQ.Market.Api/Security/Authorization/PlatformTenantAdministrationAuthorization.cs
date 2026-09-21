using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Api.Security;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Api.Security.Authorization;

public static class PlatformTenantAdministrationHeaders
{
    public const string Enabled =
        "X-OFOQ-Platform-Administration";
}

public sealed class PlatformTenantAdministrationAccessEvaluator
{
    private readonly ICurrentTenant
        _currentTenant;

    private readonly MarketDbContext
        _dbContext;

    private readonly IHostEnvironment
        _environment;

    private readonly IConfiguration
        _configuration;

    public PlatformTenantAdministrationAccessEvaluator(
        ICurrentTenant currentTenant,
        MarketDbContext dbContext,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _currentTenant =
            currentTenant;

        _dbContext =
            dbContext;

        _environment =
            environment;

        _configuration =
            configuration;
    }

    public async Task<bool> CanManageAsync(
        AuthorizationHandlerContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (context.Resource is not HttpContext httpContext)
        {
            return false;
        }

        /*
         * Platform tenant administration must be explicitly
         * requested by the Platform UI.
         */
        var explicitlyRequested =
            httpContext.Request.Headers.TryGetValue(
                PlatformTenantAdministrationHeaders.Enabled,
                out var headerValues) &&
            headerValues.Any(
                value =>
                    string.Equals(
                        value,
                        "1",
                        StringComparison.Ordinal));

        if (!explicitlyRequested)
        {
            return false;
        }

        /*
         * The tenant remains bound to the route-resolved tenant
         * context. The administration header never carries a
         * tenant identifier.
         */
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            return false;
        }

        var authenticationMethods =
            context.User
                .FindAll("amr")
                .Select(
                    claim =>
                        claim.Value)
                .ToHashSet(
                    StringComparer.Ordinal);

        if (!authenticationMethods.Contains("pwd"))
        {
            return false;
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
            return false;
        }

        var userId =
            UserId.From(
                userGuid);

        /*
         * Validate the live account instead of trusting only
         * claims contained in the access token.
         */
        var liveUser =
            await _dbContext
                .Set<User>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    user =>
                        user.Id == userId,
                    httpContext.RequestAborted);

        if (liveUser is null ||
            liveUser.IsDeleted ||
            liveUser.Status != UserStatus.Active)
        {
            return false;
        }

        /*
         * Use the same MFA behavior as Platform Administration:
         * production requires MFA; Development may use the
         * configured bootstrap bypass only while appropriate.
         */
        var sessionHasMfa =
            authenticationMethods.Contains("mfa");

        var developmentMfaBypass =
            DevelopmentSecurity.IsMfaBypassEnabled(
                _environment,
                _configuration);

        if (!sessionHasMfa &&
            !developmentMfaBypass)
        {
            if (!_environment.IsDevelopment())
            {
                return false;
            }

            var mfaIsEnabled =
                await _dbContext
                    .Set<UserMfa>()
                    .AsNoTracking()
                    .AnyAsync(
                        mfa =>
                            mfa.UserId == userId &&
                            mfa.Status ==
                                UserMfaStatus.Enabled,
                        httpContext.RequestAborted);

            if (mfaIsEnabled)
            {
                return false;
            }
        }

        /*
         * Never trust a platform role claim alone.
         * Read the live Platform Administrator assignment.
         */
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
                        !assignment.IsDeleted,
                    httpContext.RequestAborted);

        if (!isPlatformAdministrator)
        {
            return false;
        }

        var tenantId =
            _currentTenant.TenantId.Value;

        var tenant =
            await _dbContext
                .Set<Tenant>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == tenantId,
                    httpContext.RequestAborted);

        if (tenant is null ||
            tenant.IsDeleted)
        {
            return false;
        }

        /*
         * Operational Back Office administration is available
         * only for Draft and Active stores.
         */
        if (tenant.Status is not
            (TenantStatus.Draft or TenantStatus.Active))
        {
            return false;
        }

        return true;
    }
}

public sealed class PlatformTenantBackOfficeAuthorizationHandler :
    AuthorizationHandler<TenantBackOfficeRequirement>
{
    private readonly PlatformTenantAdministrationAccessEvaluator
        _evaluator;

    public PlatformTenantBackOfficeAuthorizationHandler(
        PlatformTenantAdministrationAccessEvaluator evaluator)
    {
        _evaluator =
            evaluator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantBackOfficeRequirement requirement)
    {
        if (await _evaluator.CanManageAsync(context))
        {
            context.Succeed(
                requirement);
        }
    }
}

public sealed class PlatformTenantCommerceAdministrationAuthorizationHandler :
    AuthorizationHandler<TenantCommerceAdministrationRequirement>
{
    private readonly PlatformTenantAdministrationAccessEvaluator
        _evaluator;

    public PlatformTenantCommerceAdministrationAuthorizationHandler(
        PlatformTenantAdministrationAccessEvaluator evaluator)
    {
        _evaluator =
            evaluator;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantCommerceAdministrationRequirement requirement)
    {
        if (await _evaluator.CanManageAsync(context))
        {
            context.Succeed(
                requirement);
        }
    }
}