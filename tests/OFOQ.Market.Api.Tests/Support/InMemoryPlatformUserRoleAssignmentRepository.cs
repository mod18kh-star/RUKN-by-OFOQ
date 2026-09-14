using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryPlatformUserRoleAssignmentRepository :
    IPlatformUserRoleAssignmentRepository
{
    private readonly object
        _sync = new();

    private readonly List<PlatformUserRoleAssignment>
        _items = [];

    public Task<IReadOnlyList<PlatformUserRoleAssignment>>
        GetActiveByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<PlatformUserRoleAssignment> result =
                _items
                    .Where(
                        assignment =>
                            !assignment.IsDeleted &&
                            assignment.UserId == userId)
                    .OrderBy(
                        assignment =>
                            assignment.Role)
                    .ToArray();

            return Task.FromResult(
                result);
        }
    }

    public Task<PlatformUserRoleAssignment?>
        GetActiveByUserAndRoleAsync(
            UserId userId,
            PlatformRole role,
            CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var result =
                _items.SingleOrDefault(
                    assignment =>
                        !assignment.IsDeleted &&
                        assignment.UserId == userId &&
                        assignment.Role == role);

            return Task.FromResult(
                result);
        }
    }

    public Task<bool>
        HasAnyRoleAsync(
            UserId userId,
            IReadOnlyCollection<PlatformRole> roles,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            roles);

        lock (_sync)
        {
            var result =
                _items.Any(
                    assignment =>
                        !assignment.IsDeleted &&
                        assignment.UserId == userId &&
                        roles.Contains(
                            assignment.Role));

            return Task.FromResult(
                result);
        }
    }

    public Task AddAsync(
        PlatformUserRoleAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            assignment);

        lock (_sync)
        {
            _items.Add(
                assignment);
        }

        return Task.CompletedTask;
    }
}
