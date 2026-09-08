using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Security.Authorization;

public sealed class TenantCommerceAdministrationAuthorizationHandler :
    AuthorizationHandler<TenantCommerceAdministrationRequirement>
{
    private readonly IUserRepository
        _userRepository;

    private readonly ITenantRepository
        _tenantRepository;

    private readonly ITenantMembershipRepository
        _membershipRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public TenantCommerceAdministrationAuthorizationHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITenantMembershipRepository membershipRepository,
        ICurrentTenant currentTenant)
    {
        _userRepository =
            userRepository;

        _tenantRepository =
            tenantRepository;

        _membershipRepository =
            membershipRepository;

        _currentTenant =
            currentTenant;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantCommerceAdministrationRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

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
                "pwd") ||
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

        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
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

        var user =
            await _userRepository.GetByIdAsync(
                userId,
                cancellationToken);

        if (user is null ||
            user.IsDeleted ||
            user.Status != UserStatus.Active)
        {
            return;
        }

        var tenant =
            await _tenantRepository.GetByIdAsync(
                tenantId,
                cancellationToken);

        if (tenant is null ||
            tenant.IsDeleted)
        {
            return;
        }

        if (tenant.Status is not
            (TenantStatus.Draft or TenantStatus.Active))
        {
            return;
        }

        var membership =
            await _membershipRepository.GetByTenantAndUserAsync(
                tenantId,
                userId,
                cancellationToken);

        if (membership is null ||
            membership.IsDeleted)
        {
            return;
        }

        var allowedRole =
            membership.Role is
                TenantRole.Owner or
                TenantRole.Admin;

        if (!allowedRole)
        {
            return;
        }

        context.Succeed(
            requirement);
    }
}