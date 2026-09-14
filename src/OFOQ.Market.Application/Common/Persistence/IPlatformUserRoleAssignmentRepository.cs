using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IPlatformUserRoleAssignmentRepository
{
    Task<IReadOnlyList<PlatformUserRoleAssignment>>
        GetActiveByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default);

    Task<PlatformUserRoleAssignment?>
        GetActiveByUserAndRoleAsync(
            UserId userId,
            PlatformRole role,
            CancellationToken cancellationToken = default);

    Task<bool>
        HasAnyRoleAsync(
            UserId userId,
            IReadOnlyCollection<PlatformRole> roles,
            CancellationToken cancellationToken = default);

    Task AddAsync(
        PlatformUserRoleAssignment assignment,
        CancellationToken cancellationToken = default);
}
