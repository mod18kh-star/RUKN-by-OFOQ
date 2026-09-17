using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IUserTrustedDeviceRepository
{
    Task<UserTrustedDevice?> GetByIdAsync(
        UserTrustedDeviceId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserTrustedDevice>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserTrustedDevice device,
        CancellationToken cancellationToken = default);
}
