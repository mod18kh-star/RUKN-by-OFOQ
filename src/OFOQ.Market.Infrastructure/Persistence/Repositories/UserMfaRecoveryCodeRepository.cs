using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class UserMfaRecoveryCodeRepository :
    IUserMfaRecoveryCodeRepository
{
    private readonly MarketDbContext _dbContext;

    public UserMfaRecoveryCodeRepository(
        MarketDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<UserMfaRecoveryCode?> GetByIdAsync(
        UserMfaRecoveryCodeId recoveryCodeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserMfaRecoveryCodes
            .SingleOrDefaultAsync(
                recoveryCode =>
                    recoveryCode.Id ==
                    recoveryCodeId,
                cancellationToken);
    }

    public Task<UserMfaRecoveryCode?> GetByHashAsync(
        UserMfaId userMfaId,
        string codeHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            codeHash);

        return _dbContext.UserMfaRecoveryCodes
            .SingleOrDefaultAsync(
                recoveryCode =>
                    recoveryCode.UserMfaId ==
                        userMfaId &&
                    recoveryCode.CodeHash ==
                        codeHash,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserMfaRecoveryCode>>
        GetUnusedByUserMfaIdAsync(
            UserMfaId userMfaId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .UserMfaRecoveryCodes
            .Where(
                recoveryCode =>
                    recoveryCode.UserMfaId ==
                        userMfaId &&
                    recoveryCode.UsedAtUtc ==
                        null)
            .ToListAsync(
                cancellationToken);
    }

    public Task<bool> AnyForUserMfaAsync(
        UserMfaId userMfaId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext
            .UserMfaRecoveryCodes
            .AnyAsync(
                recoveryCode =>
                    recoveryCode.UserMfaId ==
                    userMfaId,
                cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<UserMfaRecoveryCode> recoveryCodes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            recoveryCodes);

        await _dbContext
            .UserMfaRecoveryCodes
            .AddRangeAsync(
                recoveryCodes,
                cancellationToken);
    }

    public void RemoveRange(
        IEnumerable<UserMfaRecoveryCode> recoveryCodes)
    {
        ArgumentNullException.ThrowIfNull(
            recoveryCodes);

        _dbContext
            .UserMfaRecoveryCodes
            .RemoveRange(
                recoveryCodes);
    }

    public async Task<bool> TryConsumeByHashAsync(
        UserMfaId userMfaId,
        string codeHash,
        DateTimeOffset usedAtUtc,
        Guid? updatedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            codeHash);

        var affectedRows =
            await _dbContext
                .UserMfaRecoveryCodes
                .Where(
                    recoveryCode =>
                        recoveryCode.UserMfaId ==
                            userMfaId &&
                        recoveryCode.CodeHash ==
                            codeHash &&
                        recoveryCode.UsedAtUtc ==
                            null)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                recoveryCode =>
                                    recoveryCode.UsedAtUtc,
                                usedAtUtc)
                            .SetProperty(
                                recoveryCode =>
                                    recoveryCode.UpdatedAtUtc,
                                usedAtUtc)
                            .SetProperty(
                                recoveryCode =>
                                    recoveryCode.UpdatedByUserId,
                                updatedByUserId),
                    cancellationToken);

        return affectedRows == 1;
    }
}