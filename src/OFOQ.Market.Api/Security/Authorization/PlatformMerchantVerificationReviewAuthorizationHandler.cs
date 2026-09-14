using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Security.Authorization;

public sealed class PlatformMerchantVerificationReviewAuthorizationHandler :
    AuthorizationHandler<PlatformMerchantVerificationReviewRequirement>
{
    private static readonly PlatformRole[] AllowedRoles =
    [
        PlatformRole.PlatformAdministrator,
        PlatformRole.ComplianceReviewer
    ];

    private readonly IUserRepository
        _userRepository;

    private readonly IPlatformUserRoleAssignmentRepository
        _platformRoleRepository;

    public PlatformMerchantVerificationReviewAuthorizationHandler(
        IUserRepository userRepository,
        IPlatformUserRoleAssignmentRepository platformRoleRepository)
    {
        _userRepository =
            userRepository;

        _platformRoleRepository =
            platformRoleRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformMerchantVerificationReviewRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        /*
         * Merchant verification review is a privileged
         * platform-level operation.
         *
         * A valid authenticated session alone is not enough.
         * Password + MFA assurance are both required.
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
                "pwd") ||
            !authenticationMethods.Contains(
                "mfa"))
        {
            return;
        }

        /*
         * The authenticated user identity comes from the JWT
         * subject claim. Platform roles themselves are deliberately
         * not trusted from JWT claims.
         */
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

        var cancellationToken =
            context.Resource is HttpContext httpContext
                ? httpContext.RequestAborted
                : CancellationToken.None;

        /*
         * Always re-read the current user state from persistence.
         *
         * This prevents a previously-issued JWT from keeping
         * privileged access after the platform account has been
         * suspended, disabled, or deleted.
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
         * Platform authorization is intentionally global and is
         * completely independent from TenantMembership roles.
         *
         * A store Owner/Admin does not gain compliance permissions.
         *
         * Role assignments are checked live so revocation takes
         * effect immediately without waiting for JWT expiration.
         */
        var hasRequiredPlatformRole =
            await _platformRoleRepository
                .HasAnyRoleAsync(
                    userId,
                    AllowedRoles,
                    cancellationToken);

        if (!hasRequiredPlatformRole)
        {
            return;
        }

        context.Succeed(
            requirement);
    }
}
