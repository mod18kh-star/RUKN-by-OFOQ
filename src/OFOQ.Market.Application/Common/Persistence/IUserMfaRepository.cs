using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IUserMfaRepository
{
    Task<UserMfa?> GetByIdAsync(
        UserMfaId userMfaId,
        CancellationToken cancellationToken = default);

    Task<UserMfa?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserMfa userMfa,
        CancellationToken cancellationToken = default);
}