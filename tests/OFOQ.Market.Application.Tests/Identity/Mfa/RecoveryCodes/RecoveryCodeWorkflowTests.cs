using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Consume;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Generate;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Regenerate;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Tests.Identity.Mfa.RecoveryCodes;

public sealed class RecoveryCodeWorkflowTests
{
    private static readonly DateTimeOffset FixedNow =
        new(
            2026,
            9,
            4,
            0,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task Generate_CreatesEightHashedCodes_AndReturnsPlainCodes()
    {
        var userId =
            UserId.New();

        var mfa =
            CreateEnabledMfa(
                userId);

        var mfaRepository =
            new FakeUserMfaRepository(
                mfa);

        var recoveryRepository =
            new FakeRecoveryCodeRepository();

        var recoveryService =
            new FakeRecoveryCodeService();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new GenerateRecoveryCodesHandler(
                mfaRepository,
                recoveryRepository,
                recoveryService,
                unitOfWork,
                new FixedTimeProvider(
                    FixedNow));

        var result =
            await handler.HandleAsync(
                new GenerateRecoveryCodesCommand(
                    userId));

        Assert.Equal(
            8,
            result.Codes.Count);

        Assert.Equal(
            8,
            recoveryRepository.Items.Count);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);

        foreach (var recoveryCode in
                 recoveryRepository.Items)
        {
            Assert.StartsWith(
                "HASH::",
                recoveryCode.CodeHash);

            Assert.DoesNotContain(
                result.Codes,
                plainCode =>
                    plainCode ==
                    recoveryCode.CodeHash);
        }

        Assert.All(
            result.Codes,
            plainCode =>
                Assert.DoesNotContain(
                    recoveryRepository.Items,
                    stored =>
                        stored.CodeHash ==
                        plainCode));
    }

    [Fact]
    public async Task Generate_Rejects_WhenCodesAlreadyExist()
    {
        var userId =
            UserId.New();

        var mfa =
            CreateEnabledMfa(
                userId);

        var existing =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "HASH::EXISTING",
                FixedNow.AddMinutes(-5));

        var recoveryRepository =
            new FakeRecoveryCodeRepository(
                existing);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new GenerateRecoveryCodesHandler(
                new FakeUserMfaRepository(
                    mfa),
                recoveryRepository,
                new FakeRecoveryCodeService(),
                unitOfWork,
                new FixedTimeProvider(
                    FixedNow));

        await Assert.ThrowsAsync<
            RecoveryCodesAlreadyGeneratedException>(
                () =>
                    handler.HandleAsync(
                        new GenerateRecoveryCodesCommand(
                            userId)));

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Generate_Rejects_WhenMfaIsNotEnabled()
    {
        var userId =
            UserId.New();

        var pendingMfa =
            UserMfa.BeginEnrollment(
                userId,
                "PROTECTED-SECRET",
                FixedNow.AddMinutes(-5));

        var handler =
            new GenerateRecoveryCodesHandler(
                new FakeUserMfaRepository(
                    pendingMfa),
                new FakeRecoveryCodeRepository(),
                new FakeRecoveryCodeService(),
                new FakeUnitOfWork(),
                new FixedTimeProvider(
                    FixedNow));

        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () =>
                    handler.HandleAsync(
                        new GenerateRecoveryCodesCommand(
                            userId)));
    }

    [Fact]
    public async Task Regenerate_RemovesUnusedOldCodes_AndCreatesEightNewCodes()
    {
        var userId =
            UserId.New();

        var mfa =
            CreateEnabledMfa(
                userId);

        var firstOldCode =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "HASH::OLD-1",
                FixedNow.AddDays(-1));

        var secondOldCode =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                "HASH::OLD-2",
                FixedNow.AddDays(-1));

        var recoveryRepository =
            new FakeRecoveryCodeRepository(
                firstOldCode,
                secondOldCode);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new RegenerateRecoveryCodesHandler(
                new FakeUserMfaRepository(
                    mfa),
                recoveryRepository,
                new FakeRecoveryCodeService(),
                unitOfWork,
                new FixedTimeProvider(
                    FixedNow));

        var result =
            await handler.HandleAsync(
                new RegenerateRecoveryCodesCommand(
                    userId));

        Assert.Equal(
            8,
            result.Codes.Count);

        Assert.Equal(
            8,
            recoveryRepository.Items.Count);

        Assert.DoesNotContain(
            recoveryRepository.Items,
            item =>
                item.CodeHash ==
                    "HASH::OLD-1" ||
                item.CodeHash ==
                    "HASH::OLD-2");

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Consume_ConsumesValidRecoveryCode()
    {
        var userId =
            UserId.New();

        var mfa =
            CreateEnabledMfa(
                userId);

        var service =
            new FakeRecoveryCodeService();

        var plainCode =
            "VALID-CODE";

        var stored =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                service.Hash(
                    plainCode),
                FixedNow.AddHours(-1));

        var recoveryRepository =
            new FakeRecoveryCodeRepository(
                stored);

        var handler =
            new ConsumeRecoveryCodeHandler(
                new FakeUserMfaRepository(
                    mfa),
                recoveryRepository,
                service,
                new FixedTimeProvider(
                    FixedNow));

        await handler.HandleAsync(
            new ConsumeRecoveryCodeCommand(
                userId,
                plainCode));

        Assert.True(
            stored.IsUsed);

        Assert.Equal(
            FixedNow,
            stored.UsedAtUtc);

        Assert.Equal(
            service.Hash(
                plainCode),
            recoveryRepository.LastConsumedHash);
    }

    [Fact]
    public async Task Consume_RejectsAlreadyUsedOrUnknownRecoveryCode()
    {
        var userId =
            UserId.New();

        var mfa =
            CreateEnabledMfa(
                userId);

        var service =
            new FakeRecoveryCodeService();

        var stored =
            UserMfaRecoveryCode.Create(
                mfa.Id,
                service.Hash(
                    "FIRST-CODE"),
                FixedNow.AddHours(-1));

        stored.MarkUsed(
            FixedNow.AddMinutes(-10));

        var handler =
            new ConsumeRecoveryCodeHandler(
                new FakeUserMfaRepository(
                    mfa),
                new FakeRecoveryCodeRepository(
                    stored),
                service,
                new FixedTimeProvider(
                    FixedNow));

        await Assert.ThrowsAsync<
            InvalidRecoveryCodeException>(
                () =>
                    handler.HandleAsync(
                        new ConsumeRecoveryCodeCommand(
                            userId,
                            "FIRST-CODE")));
    }

    [Fact]
    public async Task Consume_RejectsMalformedRecoveryCode_Generically()
    {
        var userId =
            UserId.New();

        var mfa =
            CreateEnabledMfa(
                userId);

        var recoveryRepository =
            new FakeRecoveryCodeRepository();

        var handler =
            new ConsumeRecoveryCodeHandler(
                new FakeUserMfaRepository(
                    mfa),
                recoveryRepository,
                new FakeRecoveryCodeService(),
                new FixedTimeProvider(
                    FixedNow));

        await Assert.ThrowsAsync<
            InvalidRecoveryCodeException>(
                () =>
                    handler.HandleAsync(
                        new ConsumeRecoveryCodeCommand(
                            userId,
                            "MALFORMED")));

        Assert.Null(
            recoveryRepository.LastConsumedHash);
    }

    [Fact]
    public async Task Consume_Rejects_WhenMfaIsNotEnabled()
    {
        var userId =
            UserId.New();

        var pendingMfa =
            UserMfa.BeginEnrollment(
                userId,
                "PROTECTED-SECRET",
                FixedNow.AddMinutes(-5));

        var handler =
            new ConsumeRecoveryCodeHandler(
                new FakeUserMfaRepository(
                    pendingMfa),
                new FakeRecoveryCodeRepository(),
                new FakeRecoveryCodeService(),
                new FixedTimeProvider(
                    FixedNow));

        await Assert.ThrowsAsync<
            InvalidRecoveryCodeException>(
                () =>
                    handler.HandleAsync(
                        new ConsumeRecoveryCodeCommand(
                            userId,
                            "VALID-CODE")));
    }

    private static UserMfa CreateEnabledMfa(
        UserId userId)
    {
        var mfa =
            UserMfa.BeginEnrollment(
                userId,
                "PROTECTED-SECRET",
                FixedNow.AddMinutes(-10));

        mfa.ConfirmEnrollment(
            100,
            FixedNow.AddMinutes(-9));

        return mfa;
    }

    private sealed class FakeUserMfaRepository :
        IUserMfaRepository
    {
        private readonly UserMfa? _mfa;

        public FakeUserMfaRepository(
            UserMfa? mfa)
        {
            _mfa =
                mfa;
        }

        public Task<UserMfa?> GetByIdAsync(
            UserMfaId userMfaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _mfa?.Id == userMfaId
                    ? _mfa
                    : null);
        }

        public Task<UserMfa?> GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _mfa?.UserId == userId
                    ? _mfa
                    : null);
        }

        public Task<bool> ExistsForUserAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _mfa?.UserId == userId);
        }

        public Task AddAsync(
            UserMfa userMfa,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeRecoveryCodeRepository :
        IUserMfaRecoveryCodeRepository
    {
        private readonly List<UserMfaRecoveryCode> _items;

        public FakeRecoveryCodeRepository(
            params UserMfaRecoveryCode[] items)
        {
            _items =
                items.ToList();
        }

        public IReadOnlyList<UserMfaRecoveryCode> Items =>
            _items;

        public string? LastConsumedHash { get; private set; }

        public Task<UserMfaRecoveryCode?> GetByIdAsync(
            UserMfaRecoveryCodeId recoveryCodeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _items.SingleOrDefault(
                    item =>
                        item.Id ==
                        recoveryCodeId));
        }

        public Task<UserMfaRecoveryCode?> GetByHashAsync(
            UserMfaId userMfaId,
            string codeHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _items.SingleOrDefault(
                    item =>
                        item.UserMfaId ==
                            userMfaId &&
                        item.CodeHash ==
                            codeHash));
        }

        public Task<IReadOnlyList<UserMfaRecoveryCode>>
            GetUnusedByUserMfaIdAsync(
                UserMfaId userMfaId,
                CancellationToken cancellationToken = default)
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

        public Task<bool> AnyForUserMfaAsync(
            UserMfaId userMfaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _items.Any(
                    item =>
                        item.UserMfaId ==
                        userMfaId));
        }

        public Task AddRangeAsync(
            IEnumerable<UserMfaRecoveryCode> recoveryCodes,
            CancellationToken cancellationToken = default)
        {
            _items.AddRange(
                recoveryCodes);

            return Task.CompletedTask;
        }

        public void RemoveRange(
            IEnumerable<UserMfaRecoveryCode> recoveryCodes)
        {
            foreach (var recoveryCode in
                     recoveryCodes.ToArray())
            {
                _items.Remove(
                    recoveryCode);
            }
        }

        public Task<bool> TryConsumeByHashAsync(
            UserMfaId userMfaId,
            string codeHash,
            DateTimeOffset usedAtUtc,
            Guid? updatedByUserId = null,
            CancellationToken cancellationToken = default)
        {
            LastConsumedHash =
                codeHash;

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

    private sealed class FakeRecoveryCodeService :
        IRecoveryCodeService
    {
        public IReadOnlyList<string> GenerateCodes(
            int count)
        {
            return Enumerable
                .Range(
                    1,
                    count)
                .Select(
                    index =>
                        $"RECOVERY-CODE-{index:D2}")
                .ToArray();
        }

        public string Hash(
            string code)
        {
            if (code ==
                "MALFORMED")
            {
                throw new ArgumentException(
                    "Invalid recovery code.");
            }

            return $"HASH::{code}";
        }

        public bool Verify(
            string code,
            string codeHash)
        {
            return string.Equals(
                Hash(code),
                codeHash,
                StringComparison.Ordinal);
        }
    }

    private sealed class FakeUnitOfWork :
        IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;

            return Task.FromResult(
                1);
        }
    }

    private sealed class FixedTimeProvider :
        TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}