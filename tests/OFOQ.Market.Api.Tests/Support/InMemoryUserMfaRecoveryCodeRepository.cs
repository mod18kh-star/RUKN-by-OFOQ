using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class InMemoryUserMfaRecoveryCodeRepository :
    IUserMfaRecoveryCodeRepository
{
    private readonly object _syncRoot =
        new();

    private readonly List<UserMfaRecoveryCode> _items =
        [];

    public IReadOnlyList<UserMfaRecoveryCode> Items
    {
        get
        {
            lock (_syncRoot)
            {
                return _items
                    .ToArray();
            }
        }
    }

    public Task<UserMfaRecoveryCode?> GetByIdAsync(
        UserMfaRecoveryCodeId recoveryCodeId,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            var recoveryCode =
                _items.SingleOrDefault(
                    item =>
                        item.Id ==
                        recoveryCodeId);

            return Task.FromResult(
                recoveryCode);
        }
    }

    public Task<UserMfaRecoveryCode?> GetByHashAsync(
        UserMfaId userMfaId,
        string codeHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            codeHash);

        lock (_syncRoot)
        {
            var recoveryCode =
                _items.SingleOrDefault(
                    item =>
                        item.UserMfaId ==
                            userMfaId &&
                        item.CodeHash ==
                            codeHash);

            return Task.FromResult(
                recoveryCode);
        }
    }

    public Task<IReadOnlyList<UserMfaRecoveryCode>>
        GetUnusedByUserMfaIdAsync(
            UserMfaId userMfaId,
            CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            IReadOnlyList<UserMfaRecoveryCode> result =
                _items
                    .Where(
                        item =>
                            item.UserMfaId ==
                                userMfaId &&
                            !item.IsUsed)
                    .ToArray();

            return Task.FromResult(
                result);
        }
    }

    public Task<bool> AnyForUserMfaAsync(
        UserMfaId userMfaId,
        CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(
                _items.Any(
                    item =>
                        item.UserMfaId ==
                        userMfaId));
        }
    }

    public Task AddRangeAsync(
        IEnumerable<UserMfaRecoveryCode> recoveryCodes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            recoveryCodes);

        lock (_syncRoot)
        {
            _items.AddRange(
                recoveryCodes);
        }

        return Task.CompletedTask;
    }

    public void RemoveRange(
        IEnumerable<UserMfaRecoveryCode> recoveryCodes)
    {
        ArgumentNullException.ThrowIfNull(
            recoveryCodes);

        lock (_syncRoot)
        {
            foreach (var recoveryCode in recoveryCodes)
            {
                _items.Remove(
                    recoveryCode);
            }
        }
    }

    public Task<bool> TryConsumeByHashAsync(
        UserMfaId userMfaId,
        string codeHash,
        DateTimeOffset usedAtUtc,
        Guid? updatedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            codeHash);

        lock (_syncRoot)
        {
            var recoveryCode =
                _items.SingleOrDefault(
                    item =>
                        item.UserMfaId ==
                            userMfaId &&
                        item.CodeHash ==
                            codeHash &&
                        !item.IsUsed);

            if (recoveryCode is null)
            {
                return Task.FromResult(
                    false);
            }

            recoveryCode.MarkUsed(
                usedAtUtc,
                updatedByUserId);

            return Task.FromResult(
                true);
        }
    }
}