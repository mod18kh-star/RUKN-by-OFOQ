using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Identity.Mfa;
using OFOQ.Market.Application.Identity.Mfa.ConfirmEnrollment;
using OFOQ.Market.Application.Identity.Mfa.StartEnrollment;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Tests.Identity.Mfa;

public sealed class MfaEnrollmentHandlerTests
{
    private static readonly DateTimeOffset FixedNow =
        new(
            2026,
            9,
            3,
            19,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task StartEnrollment_CreatesPendingMfa_WithProtectedSecret()
    {
        var user =
            CreateUser();

        var userRepository =
            new FakeUserRepository(user);

        var mfaRepository =
            new FakeUserMfaRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateStartHandler(
                userRepository,
                mfaRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new StartMfaEnrollmentCommand(
                    user.Id));

        Assert.Equal(
            "RAW-TOTP-SECRET",
            result.ManualEntryKey);

        Assert.Equal(
            "otpauth://totp/OFOQ:user@example.com",
            result.ProvisioningUri);

        Assert.NotNull(
            mfaRepository.AddedMfa);

        Assert.Equal(
            UserMfaStatus.PendingEnrollment,
            mfaRepository.AddedMfa.Status);

        Assert.Equal(
            "PROTECTED::RAW-TOTP-SECRET",
            mfaRepository.AddedMfa.ProtectedSecret);

        Assert.NotEqual(
            "RAW-TOTP-SECRET",
            mfaRepository.AddedMfa.ProtectedSecret);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task StartEnrollment_RestartsPendingEnrollment_WithNewProtectedSecret()
    {
        var user =
            CreateUser();

        var existingMfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED::OLD-SECRET",
                FixedNow.AddMinutes(-5));

        var userRepository =
            new FakeUserRepository(user);

        var mfaRepository =
            new FakeUserMfaRepository(
                existingMfa);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateStartHandler(
                userRepository,
                mfaRepository,
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new StartMfaEnrollmentCommand(
                    user.Id));

        Assert.Equal(
            "RAW-TOTP-SECRET",
            result.ManualEntryKey);

        Assert.Equal(
            "PROTECTED::RAW-TOTP-SECRET",
            existingMfa.ProtectedSecret);

        Assert.Equal(
            UserMfaStatus.PendingEnrollment,
            existingMfa.Status);

        Assert.Null(
            existingMfa.LastAcceptedTimeStep);

        Assert.Null(
            mfaRepository.AddedMfa);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task StartEnrollment_Rejects_WhenMfaIsAlreadyEnabled()
    {
        var user =
            CreateUser();

        var existingMfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED::OLD-SECRET",
                FixedNow.AddMinutes(-10));

        existingMfa.ConfirmEnrollment(
            100,
            FixedNow.AddMinutes(-9));

        var userRepository =
            new FakeUserRepository(user);

        var mfaRepository =
            new FakeUserMfaRepository(
                existingMfa);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateStartHandler(
                userRepository,
                mfaRepository,
                unitOfWork);

        await Assert.ThrowsAsync<
            MfaAlreadyEnabledException>(
                () =>
                    handler.HandleAsync(
                        new StartMfaEnrollmentCommand(
                            user.Id)));

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task StartEnrollment_Rejects_WhenUserDoesNotExist()
    {
        var userRepository =
            new FakeUserRepository(
                null);

        var mfaRepository =
            new FakeUserMfaRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateStartHandler(
                userRepository,
                mfaRepository,
                unitOfWork);

        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () =>
                    handler.HandleAsync(
                        new StartMfaEnrollmentCommand(
                            UserId.New())));

        Assert.Null(
            mfaRepository.AddedMfa);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ConfirmEnrollment_EnablesMfa_WhenCodeIsValid()
    {
        var user =
            CreateUser();

        var mfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED::RAW-TOTP-SECRET",
                FixedNow.AddMinutes(-1));

        var mfaRepository =
            new FakeUserMfaRepository(
                mfa);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateConfirmHandler(
                mfaRepository,
                new FakeTotpService(
                    isValid: true,
                    timeStep: 123456),
                unitOfWork);

        await handler.HandleAsync(
            new ConfirmMfaEnrollmentCommand(
                user.Id,
                "123456"));

        Assert.Equal(
            UserMfaStatus.Enabled,
            mfa.Status);

        Assert.Equal(
            123456,
            mfa.LastAcceptedTimeStep);

        Assert.Equal(
            FixedNow,
            mfa.EnabledAtUtc);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ConfirmEnrollment_DoesNotEnableMfa_WhenCodeIsInvalid()
    {
        var user =
            CreateUser();

        var mfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED::RAW-TOTP-SECRET",
                FixedNow.AddMinutes(-1));

        var mfaRepository =
            new FakeUserMfaRepository(
                mfa);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateConfirmHandler(
                mfaRepository,
                new FakeTotpService(
                    isValid: false,
                    timeStep: null),
                unitOfWork);

        await Assert.ThrowsAsync<
            InvalidMfaCodeException>(
                () =>
                    handler.HandleAsync(
                        new ConfirmMfaEnrollmentCommand(
                            user.Id,
                            "000000")));

        Assert.Equal(
            UserMfaStatus.PendingEnrollment,
            mfa.Status);

        Assert.Null(
            mfa.EnabledAtUtc);

        Assert.Null(
            mfa.LastAcceptedTimeStep);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ConfirmEnrollment_Rejects_WhenEnrollmentDoesNotExist()
    {
        var mfaRepository =
            new FakeUserMfaRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateConfirmHandler(
                mfaRepository,
                new FakeTotpService(
                    isValid: true,
                    timeStep: 123456),
                unitOfWork);

        await Assert.ThrowsAsync<
            InvalidMfaCodeException>(
                () =>
                    handler.HandleAsync(
                        new ConfirmMfaEnrollmentCommand(
                            UserId.New(),
                            "123456")));

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ConfirmEnrollment_UsesUnprotectedSecret_ForVerification()
    {
        var user =
            CreateUser();

        var mfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED::RAW-TOTP-SECRET",
                FixedNow.AddMinutes(-1));

        var mfaRepository =
            new FakeUserMfaRepository(
                mfa);

        var totpService =
            new FakeTotpService(
                isValid: true,
                timeStep: 123456);

        var handler =
            CreateConfirmHandler(
                mfaRepository,
                totpService,
                new FakeUnitOfWork());

        await handler.HandleAsync(
            new ConfirmMfaEnrollmentCommand(
                user.Id,
                "123456"));

        Assert.Equal(
            "RAW-TOTP-SECRET",
            totpService.LastVerifiedSecret);

        Assert.Equal(
            "123456",
            totpService.LastVerifiedCode);
    }

    private static StartMfaEnrollmentHandler CreateStartHandler(
        IUserRepository userRepository,
        IUserMfaRepository mfaRepository,
        IUnitOfWork unitOfWork)
    {
        return new StartMfaEnrollmentHandler(
            userRepository,
            mfaRepository,
            new FakeTotpService(
                isValid: true,
                timeStep: 123456),
            new FakeMfaSecretProtector(),
            unitOfWork,
            new FixedTimeProvider(
                FixedNow));
    }

    private static ConfirmMfaEnrollmentHandler CreateConfirmHandler(
        IUserMfaRepository mfaRepository,
        ITotpService totpService,
        IUnitOfWork unitOfWork)
    {
        return new ConfirmMfaEnrollmentHandler(
            mfaRepository,
            totpService,
            new FakeMfaSecretProtector(),
            unitOfWork,
            new FixedTimeProvider(
                FixedNow));
    }

    private static User CreateUser()
    {
        return User.Create(
            "user@example.com",
            "HASHED-PASSWORD",
            FixedNow.AddHours(-1));
    }

    private sealed class FakeUserRepository :
        IUserRepository
    {
        private readonly User? _user;

        public FakeUserRepository(
            User? user)
        {
            _user = user;
        }

        public Task<User?> GetByIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            var result =
                _user?.Id == userId
                    ? _user
                    : null;

            return Task.FromResult(
                result);
        }

        public Task<User?> GetByEmailAsync(
            EmailAddress email,
            CancellationToken cancellationToken = default)
        {
            var result =
                _user?.Email == email
                    ? _user
                    : null;

            return Task.FromResult(
                result);
        }

        public Task<bool> EmailExistsAsync(
            EmailAddress email,
            UserId? excludingUserId = null,
            CancellationToken cancellationToken = default)
        {
            var exists =
                _user is not null &&
                _user.Email == email &&
                (!excludingUserId.HasValue ||
                 _user.Id != excludingUserId.Value);

            return Task.FromResult(
                exists);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeUserMfaRepository :
        IUserMfaRepository
    {
        private readonly UserMfa? _existingMfa;

        public FakeUserMfaRepository(
            UserMfa? existingMfa = null)
        {
            _existingMfa =
                existingMfa;
        }

        public UserMfa? AddedMfa { get; private set; }

        public Task<UserMfa?> GetByIdAsync(
            UserMfaId userMfaId,
            CancellationToken cancellationToken = default)
        {
            var result =
                _existingMfa?.Id == userMfaId
                    ? _existingMfa
                    : AddedMfa?.Id == userMfaId
                        ? AddedMfa
                        : null;

            return Task.FromResult(
                result);
        }

        public Task<UserMfa?> GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            var result =
                _existingMfa?.UserId == userId
                    ? _existingMfa
                    : AddedMfa?.UserId == userId
                        ? AddedMfa
                        : null;

            return Task.FromResult(
                result);
        }

        public Task<bool> ExistsForUserAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            var exists =
                _existingMfa?.UserId == userId ||
                AddedMfa?.UserId == userId;

            return Task.FromResult(
                exists);
        }

        public Task AddAsync(
            UserMfa userMfa,
            CancellationToken cancellationToken = default)
        {
            AddedMfa =
                userMfa;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeTotpService :
        ITotpService
    {
        private readonly bool _isValid;
        private readonly long? _timeStep;

        public FakeTotpService(
            bool isValid,
            long? timeStep)
        {
            _isValid =
                isValid;

            _timeStep =
                timeStep;
        }

        public string? LastVerifiedSecret { get; private set; }

        public string? LastVerifiedCode { get; private set; }

        public MfaEnrollmentData CreateEnrollment(
            string accountName,
            string issuer)
        {
            return new MfaEnrollmentData(
                "RAW-TOTP-SECRET",
                "otpauth://totp/OFOQ:user@example.com");
        }

        public TotpVerificationResult Verify(
            string secret,
            string code,
            DateTimeOffset nowUtc)
        {
            LastVerifiedSecret =
                secret;

            LastVerifiedCode =
                code;

            return new TotpVerificationResult(
                _isValid,
                _timeStep);
        }
    }

    private sealed class FakeMfaSecretProtector :
        IMfaSecretProtector
    {
        private const string Prefix =
            "PROTECTED::";

        public string Protect(
            string secret)
        {
            return Prefix +
                secret;
        }

        public string Unprotect(
            string protectedSecret)
        {
            if (!protectedSecret.StartsWith(
                    Prefix,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Protected secret is invalid.");
            }

            return protectedSecret[
                Prefix.Length..];
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

            return Task.FromResult(1);
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