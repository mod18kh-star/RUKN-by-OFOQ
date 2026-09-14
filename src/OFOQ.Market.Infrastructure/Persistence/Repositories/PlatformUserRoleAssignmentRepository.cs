using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class PlatformUserRoleAssignmentRepository :
    IPlatformUserRoleAssignmentRepository
{
    private readonly MarketDbContext
        _dbContext;

    public PlatformUserRoleAssignmentRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<IReadOnlyList<PlatformUserRoleAssignment>>
        GetActiveByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .PlatformUserRoleAssignments
            .Where(
                assignment =>
                    assignment.UserId == userId)
            .OrderBy(
                assignment =>
                    assignment.Role)
            .ToListAsync(
                cancellationToken);
    }

    public Task<PlatformUserRoleAssignment?>
        GetActiveByUserAndRoleAsync(
            UserId userId,
            PlatformRole role,
            CancellationToken cancellationToken = default)
    {
        return _dbContext
            .PlatformUserRoleAssignments
            .SingleOrDefaultAsync(
                assignment =>
                    assignment.UserId == userId &&
                    assignment.Role == role,
                cancellationToken);
    }

    public Task<bool>
        HasAnyRoleAsync(
            UserId userId,
            IReadOnlyCollection<PlatformRole> roles,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            roles);

        if (roles.Count == 0)
        {
            return Task.FromResult(
                false);
        }

        return _dbContext
            .PlatformUserRoleAssignments
            .AnyAsync(
                assignment =>
                    assignment.UserId == userId &&
                    roles.Contains(
                        assignment.Role),
                cancellationToken);
    }

    public async Task AddAsync(
        PlatformUserRoleAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            assignment);

        await _dbContext
            .PlatformUserRoleAssignments
            .AddAsync(
                assignment,
                cancellationToken);
    }
}
