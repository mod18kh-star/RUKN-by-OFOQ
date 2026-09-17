using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.CurrentUserContext;

public sealed class GetCurrentUserContextHandler
{
    private readonly IUserRepository
        _userRepository;

    private readonly IUserMfaRepository
        _userMfaRepository;

    private readonly IPlatformUserRoleAssignmentRepository
        _platformRoleRepository;

    private readonly ITenantMembershipRepository
        _tenantMembershipRepository;

    public GetCurrentUserContextHandler(
        IUserRepository userRepository,
        IUserMfaRepository userMfaRepository,
        IPlatformUserRoleAssignmentRepository platformRoleRepository,
        ITenantMembershipRepository tenantMembershipRepository)
    {
        _userRepository =
            userRepository;

        _userMfaRepository =
            userMfaRepository;

        _platformRoleRepository =
            platformRoleRepository;

        _tenantMembershipRepository =
            tenantMembershipRepository;
    }

    public async Task<GetCurrentUserContextResult> HandleAsync(
        GetCurrentUserContextQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        var user =
            await _userRepository
                .GetByIdAsync(
                    query.UserId,
                    cancellationToken);

        if (user is null ||
            user.IsDeleted ||
            user.Status !=
                UserStatus.Active)
        {
            throw new CurrentUserUnavailableException();
        }

        var mfa =
            await _userMfaRepository
                .GetByUserIdAsync(
                    user.Id,
                    cancellationToken);

        var platformRoles =
            await _platformRoleRepository
                .GetActiveByUserIdAsync(
                    user.Id,
                    cancellationToken);

        var memberships =
            await _tenantMembershipRepository
                .GetByUserIdAsync(
                    user.Id,
                    cancellationToken);

        return new GetCurrentUserContextResult(
            user.Id,
            user.Email.Value,
            user.Status,
            user.EmailVerifiedAtUtc.HasValue,
            mfa?.Status ==
                UserMfaStatus.Enabled,
            platformRoles
                .Where(
                    assignment =>
                        !assignment.IsDeleted)
                .Select(
                    assignment =>
                        assignment.Role)
                .Distinct()
                .OrderBy(
                    role =>
                        role)
                .ToArray(),
            memberships.Any(
                membership =>
                    !membership.IsDeleted),
            user.FullName,
            user.PhoneNumber);
    }
}
