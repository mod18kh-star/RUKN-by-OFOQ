using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using OFOQ.Market.Api.Security;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Security.Authorization;

public sealed class TenantBackOfficeAuthorizationHandler :
    AuthorizationHandler<TenantBackOfficeRequirement>
{
    private readonly IUserRepository
        _userRepository;

    private readonly ITenantRepository
        _tenantRepository;

    private readonly ITenantMembershipRepository
        _membershipRepository;

    private readonly ICurrentTenant
        _currentTenant;

    private readonly IHostEnvironment
        _environment;

    private readonly IConfiguration
        _configuration;

    public TenantBackOfficeAuthorizationHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITenantMembershipRepository membershipRepository,
        ICurrentTenant currentTenant,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _userRepository =
            userRepository;

        _tenantRepository =
            tenantRepository;

        _membershipRepository =
            membershipRepository;

        _currentTenant =
            currentTenant;

        _environment =
            environment;

        _configuration =
            configuration;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantBackOfficeRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        /*
         * Privileged Tenant Back Office access requires
         * password + MFA assurance.
         */
        var authenticationMethods =
            context.User
                .FindAll(
                    "amr")
                .Select(
                    claim =>
                        claim.Value)
                .ToHashSet(
                    StringComparer.Ordinal);

        if (!authenticationMethods.Contains(
                "pwd"))
        {
            return;
        }

        var developmentMfaBypass =
            DevelopmentSecurity
                .IsMfaBypassEnabled(
                    _environment,
                    _configuration);

        if (!developmentMfaBypass &&
            !authenticationMethods.Contains(
                "mfa"))
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

        /*
         * Tenant scope is established centrally from the
         * matched route by TenantRouteContextMiddleware.
         */
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue)
        {
            return;
        }

        var userId =
            UserId.From(
                userGuid);

        var tenantId =
            _currentTenant.TenantId.Value;

        var cancellationToken =
            context.Resource is HttpContext httpContext
                ? httpContext.RequestAborted
                : CancellationToken.None;

        /*
         * Never trust an old JWT for current privileged state.
         */
        var user =
            await _userRepository
                .GetByIdAsync(
                    userId,
                    cancellationToken);

        if (user is null ||
            user.IsDeleted ||
            user.Status != UserStatus.Active)
        {
            return;
        }

        /*
         * Tenant is resolved from the route scope only.
         * No request-body TenantId participates in authorization.
         */
        var tenant =
            await _tenantRepository
                .GetByIdAsync(
                    tenantId,
                    cancellationToken);

        if (tenant is null ||
            tenant.IsDeleted)
        {
            return;
        }

        /*
         * Draft:
         * allowed so the store can be configured.
         *
         * Active:
         * fully operational.
         *
         * Suspended:
         * operational Back Office access denied.
         */
        if (tenant.Status is not
            (TenantStatus.Draft or TenantStatus.Active))
        {
            return;
        }

        /*
         * Membership is read LIVE and for the exact current
         * tenant only.
         */
        var membership =
            await _membershipRepository
                .GetByTenantAndUserAsync(
                    tenantId,
                    userId,
                    cancellationToken);

        if (membership is null ||
            membership.IsDeleted)
        {
            return;
        }

        /*
         * Explicit role allow-list.
         *
         * Future roles do not receive access automatically.
         */
        var allowedRole =
            membership.Role is
                TenantRole.Owner or
                TenantRole.Admin or
                TenantRole.Manager or
                TenantRole.Staff;

        if (!allowedRole)
        {
            return;
        }

        context.Succeed(
            requirement);
    }
}