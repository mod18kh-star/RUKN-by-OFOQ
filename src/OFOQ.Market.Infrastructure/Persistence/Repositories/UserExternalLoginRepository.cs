using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

public sealed class UserExternalLoginRepository : IUserExternalLoginRepository
{
    private readonly MarketDbContext _dbContext;

    public UserExternalLoginRepository(MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserExternalLogin?> GetByProviderSubjectAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserExternalLogins.FirstOrDefaultAsync(
            item => item.Provider == provider && item.Subject == subject,
            cancellationToken);
    }

    public Task<UserExternalLogin?> GetByUserAndProviderAsync(
        UserId userId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserExternalLogins.FirstOrDefaultAsync(
            item => item.UserId == userId && item.Provider == provider,
            cancellationToken);
    }

    public async Task AddAsync(
        UserExternalLogin login,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(login);
        await _dbContext.UserExternalLogins.AddAsync(login, cancellationToken);
    }
}
