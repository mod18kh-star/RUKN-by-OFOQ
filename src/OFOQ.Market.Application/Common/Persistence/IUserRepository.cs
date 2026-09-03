using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        EmailAddress email,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        EmailAddress email,
        UserId? excludingUserId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);
}