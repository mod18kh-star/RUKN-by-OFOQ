using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IUserExternalLoginRepository
{
    Task<UserExternalLogin?> GetByProviderSubjectAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken = default);

    Task<UserExternalLogin?> GetByUserAndProviderAsync(
        UserId userId,
        string provider,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserExternalLogin login,
        CancellationToken cancellationToken = default);
}
