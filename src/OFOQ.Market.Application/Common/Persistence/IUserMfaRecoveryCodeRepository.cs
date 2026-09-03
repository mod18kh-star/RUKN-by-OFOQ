using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Persistence;

public interface IUserMfaRecoveryCodeRepository
{
    Task<UserMfaRecoveryCode?> GetByIdAsync(
        UserMfaRecoveryCodeId recoveryCodeId,
        CancellationToken cancellationToken = default);

    Task<UserMfaRecoveryCode?> GetByHashAsync(
        UserMfaId userMfaId,
        string codeHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserMfaRecoveryCode>> GetUnusedByUserMfaIdAsync(
        UserMfaId userMfaId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<UserMfaRecoveryCode> recoveryCodes,
        CancellationToken cancellationToken = default);

    void RemoveRange(
        IEnumerable<UserMfaRecoveryCode> recoveryCodes);
}