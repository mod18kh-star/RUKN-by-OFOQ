using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Tests.Identity.Mfa.Login.VerifyTotp;

public sealed class VerifyMfaTotpHandlerTests
{
    private static readonly DateTimeOffset FixedNow =
        new(
            2026,
            9,
            4,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task ValidTotp_ConsumesChallengeAndReturnsAccessToken()
    {
        var user = CreateUser();
        var mfa = CreateEnabledMfa(user.Id);
        var challenge = CreateChallenge(user.Id);

        var unitOfWork =
            new FakeUnitOfWork();

        var accessTokenService =
            new FakeAccessTokenService();

        var handler =
            CreateHandler(
                user,
                mfa,
                challenge,
                new TotpVerificationResult(
                    true,
                    101),
                unitOfWork,
                accessTokenService);

        var result =
            await handler.HandleAsync(
                new VerifyMfaTotpCommand(
                    "RAW-CHALLENGE",
                    "123456"));

        Assert.True(
            challenge.IsConsumed);

        Assert.Equal(
            FixedNow,
            challenge.ConsumedAtUtc);

        Assert.Equal(
            101,
            mfa.LastAcceptedTimeStep);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);

        Assert.Equal(
            1,
            accessTokenService.CreateCount);

        Assert.Equal(
            "ACCESS-TOKEN",
            result.AccessToken);

        Assert.Equal(
            user.Id,
            result.UserId);

        Assert.Equal(
            user.Email.Value,
            result.Email);
    }

    [Fact]
    public async Task InvalidTotp_IncrementsFailedAttemptAndDoesNotIssueToken()
    {
        var user = CreateUser();
        var mfa = CreateEnabledMfa(user.Id);
        var challenge = CreateChallenge(user.Id);

        var unitOfWork =
            new FakeUnitOfWork();

        var accessTokenService =
            new FakeAccessTokenService();

        var handler =
            CreateHandler(
                user,
                mfa,
                challenge,
                new TotpVerificationResult(
                    false,
                    null),
                unitOfWork,
                accessTokenService);

        await Assert.ThrowsAsync<
            InvalidMfaLoginChallengeException>(
                () =>
                    handler.HandleAsync(
                        new VerifyMfaTotpCommand(
                            "RAW-CHALLENGE",
                            "000000")));

        Assert.Equal(
            1,
            challenge.FailedAttemptCount);

        Assert.False(
            challenge.IsConsumed);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);

        Assert.Equal(
            0,
            accessTokenService.CreateCount);
    }

    [Fact]
    public async Task ReplayedTotp_IsRejectedAndCountsAsFailedAttempt()
    {
        var user = CreateUser();
        var mfa = CreateEnabledMfa(user.Id);
        var challenge = CreateChallenge(user.Id);

        var unitOfWork =
            new FakeUnitOfWork();

        var accessTokenService =
            new FakeAccessTokenService();

        var handler =
            CreateHandler(
                user,
                mfa,
                challenge,
                new TotpVerificationResult(
                    true,
                    100),
                unitOfWork,
                accessTokenService);

        await Assert.ThrowsAsync<
            InvalidMfaLoginChallengeException>(
                () =>
                    handler.HandleAsync(
                        new VerifyMfaTotpCommand(
                            "RAW-CHALLENGE",
                            "123456")));

        Assert.Equal(
            100,
            mfa.LastAcceptedTimeStep);

        Assert.Equal(
            1,
            challenge.FailedAttemptCount);

        Assert.False(
            challenge.IsConsumed);

        Assert.Equal(
            0,
            accessTokenService.CreateCount);
    }

    [Fact]
    public async Task MalformedChallenge_IsRejectedWithoutDatabaseMutation()
    {
        var user = CreateUser();
        var mfa = CreateEnabledMfa(user.Id);
        var challenge = CreateChallenge(user.Id);

        var unitOfWork =
            new FakeUnitOfWork();

        var accessTokenService =
            new FakeAccessTokenService();

        var handler =
            CreateHandler(
                user,
                mfa,
                challenge,
                new TotpVerificationResult(
                    true,
                    101),
                unitOfWork,
                accessTokenService);

        await Assert.ThrowsAsync<
            InvalidMfaLoginChallengeException>(
                () =>
                    handler.HandleAsync(
                        new VerifyMfaTotpCommand(
                            "INVALID",
                            "123456")));

        Assert.False(
            challenge.IsConsumed);

        Assert.Equal(
            0,
            challenge.FailedAttemptCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);

        Assert.Equal(
            0,
            accessTokenService.CreateCount);
    }

    [Fact]
    public async Task ExpiredChallenge_IsRejectedWithoutIssuingToken()
    {
        var user = CreateUser();
        var mfa = CreateEnabledMfa(user.Id);

        var challenge =
            MfaLoginChallenge.Create(
                user.Id,
                "HASHED-CHALLENGE",
                FixedNow.AddMinutes(-1),
                FixedNow.AddMinutes(-6));

        var unitOfWork =
            new FakeUnitOfWork();

        var accessTokenService =
            new FakeAccessTokenService();

        var handler =
            CreateHandler(
                user,
                mfa,
                challenge,
                new TotpVerificationResult(
                    true,
                    101),
                unitOfWork,
                accessTokenService);

        await Assert.ThrowsAsync<
            InvalidMfaLoginChallengeException>(
                () =>
                    handler.HandleAsync(
                        new VerifyMfaTotpCommand(
                            "RAW-CHALLENGE",
                            "123456")));

        Assert.False(
            challenge.IsConsumed);

        Assert.Equal(
            0,
            accessTokenService.CreateCount);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task FifthInvalidAttempt_ExhaustsChallenge()
    {
        var user = CreateUser();
        var mfa = CreateEnabledMfa(user.Id);
        var challenge = CreateChallenge(user.Id);

        challenge.RegisterFailedAttempt(
            FixedNow.AddMinutes(-4));

        challenge.RegisterFailedAttempt(
            FixedNow.AddMinutes(-3));

        challenge.RegisterFailedAttempt(
            FixedNow.AddMinutes(-2));

        challenge.RegisterFailedAttempt(
            FixedNow.AddMinutes(-1));

        var unitOfWork =
            new FakeUnitOfWork();

        var accessTokenService =
            new FakeAccessTokenService();

        var handler =
            CreateHandler(
                user,
                mfa,
                challenge,
                new TotpVerificationResult(
                    false,
                    null),
                unitOfWork,
                accessTokenService);

        await Assert.ThrowsAsync<
            InvalidMfaLoginChallengeException>(
                () =>
                    handler.HandleAsync(
                        new VerifyMfaTotpCommand(
                            "RAW-CHALLENGE",
                            "000000")));

        Assert.Equal(
            MfaLoginChallenge.MaximumFailedAttempts,
            challenge.FailedAttemptCount);

        Assert.True(
            challenge.IsExhausted);

        Assert.False(
            challenge.IsUsable(
                FixedNow));

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);

        Assert.Equal(
            0,
            accessTokenService.CreateCount);
    }

    [Fact]
    public async Task EmptyTotpCode_CountsAsFailedAttempt()
    {
        var user = CreateUser();
        var mfa = CreateEnabledMfa(user.Id);
        var challenge = CreateChallenge(user.Id);

        var unitOfWork =
            new FakeUnitOfWork();

        var accessTokenService =
            new FakeAccessTokenService();

        var handler =
            CreateHandler(
                user,
                mfa,
                challenge,
                new TotpVerificationResult(
                    true,
                    101),
                unitOfWork,
                accessTokenService);

        await Assert.ThrowsAsync<
            InvalidMfaLoginChallengeException>(
                () =>
                    handler.HandleAsync(
                        new VerifyMfaTotpCommand(
                            "RAW-CHALLENGE",
                            "")));

        Assert.Equal(
            1,
            challenge.FailedAttemptCount);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);

        Assert.Equal(
            0,
            accessTokenService.CreateCount);
    }

    private static VerifyMfaTotpHandler CreateHandler(
        User user,
        UserMfa mfa,
        MfaLoginChallenge challenge,
        TotpVerificationResult verificationResult,
        FakeUnitOfWork unitOfWork,
        FakeAccessTokenService accessTokenService)
    {
        return new VerifyMfaTotpHandler(
            new FakeChallengeRepository(
                challenge),

            new FakeUserRepository(
                user),

            new FakeUserMfaRepository(
                mfa),

            new FakeChallengeTokenService(),

            new FakeMfaSecretProtector(),

            new FakeTotpService(
                verificationResult),

            accessTokenService,

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

    private static UserMfa CreateEnabledMfa(
        UserId userId)
    {
        var mfa =
            UserMfa.BeginEnrollment(
                userId,
                "PROTECTED-SECRET",
                FixedNow.AddMinutes(-20));

        mfa.ConfirmEnrollment(
            100,
            FixedNow.AddMinutes(-19));

        return mfa;
    }

    private static MfaLoginChallenge CreateChallenge(
        UserId userId)
    {
        return MfaLoginChallenge.Create(
            userId,
            "HASHED-CHALLENGE",
            FixedNow.AddMinutes(5),
            FixedNow.AddMinutes(-1));
    }

    private sealed class FakeChallengeRepository :
        IMfaLoginChallengeRepository
    {
        private readonly MfaLoginChallenge _challenge;

        public FakeChallengeRepository(
            MfaLoginChallenge challenge)
        {
            _challenge = challenge;
        }

        public Task<MfaLoginChallenge?> GetByIdAsync(
            MfaLoginChallengeId challengeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<MfaLoginChallenge?>(
                _challenge.Id == challengeId
                    ? _challenge
                    : null);
        }

        public Task<MfaLoginChallenge?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<MfaLoginChallenge?>(
                _challenge.TokenHash == tokenHash
                    ? _challenge
                    : null);
        }

        public Task<IReadOnlyList<MfaLoginChallenge>>
            GetActiveByUserIdAsync(
                UserId userId,
                DateTimeOffset nowUtc,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MfaLoginChallenge> result =
                _challenge.UserId == userId &&
                _challenge.IsUsable(nowUtc)
                    ? new[] { _challenge }
                    : Array.Empty<MfaLoginChallenge>();

            return Task.FromResult(
                result);
        }

        public Task AddAsync(
            MfaLoginChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeUserRepository :
        IUserRepository
    {
        private readonly User _user;

        public FakeUserRepository(
            User user)
        {
            _user = user;
        }

        public Task<User?> GetByIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(
                _user.Id == userId
                    ? _user
                    : null);
        }

        public Task<User?> GetByEmailAsync(
            EmailAddress email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(
                _user.Email == email
                    ? _user
                    : null);
        }

        public Task<bool> EmailExistsAsync(
            EmailAddress email,
            UserId? excludingUserId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _user.Email == email);
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
        private readonly UserMfa _mfa;

        public FakeUserMfaRepository(
            UserMfa mfa)
        {
            _mfa = mfa;
        }

        public Task<UserMfa?> GetByIdAsync(
            UserMfaId userMfaId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserMfa?>(
                _mfa.Id == userMfaId
                    ? _mfa
                    : null);
        }

        public Task<UserMfa?> GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UserMfa?>(
                _mfa.UserId == userId
                    ? _mfa
                    : null);
        }

        public Task<bool> ExistsForUserAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _mfa.UserId == userId);
        }

        public Task AddAsync(
            UserMfa userMfa,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeChallengeTokenService :
        IMfaLoginChallengeTokenService
    {
        public MfaLoginChallengeToken Create()
        {
            throw new NotSupportedException();
        }

        public bool TryHash(
            string? token,
            out string tokenHash)
        {
            if (token == "RAW-CHALLENGE")
            {
                tokenHash =
                    "HASHED-CHALLENGE";

                return true;
            }

            tokenHash =
                string.Empty;

            return false;
        }
    }

    private sealed class FakeMfaSecretProtector :
        IMfaSecretProtector
    {
        public string Protect(
            string secret)
        {
            return $"PROTECTED::{secret}";
        }

        public string Unprotect(
            string protectedSecret)
        {
            Assert.Equal(
                "PROTECTED-SECRET",
                protectedSecret);

            return "RAW-SECRET";
        }
    }

    private sealed class FakeTotpService :
        ITotpService
    {
        private readonly TotpVerificationResult
            _verificationResult;

        public FakeTotpService(
            TotpVerificationResult verificationResult)
        {
            _verificationResult =
                verificationResult;
        }

        public MfaEnrollmentData CreateEnrollment(
            string accountName,
            string issuer)
        {
            throw new NotSupportedException();
        }

        public TotpVerificationResult Verify(
            string secret,
            string code,
            DateTimeOffset nowUtc)
        {
            Assert.Equal(
                "RAW-SECRET",
                secret);

            return _verificationResult;
        }
    }

    private sealed class FakeAccessTokenService :
        IAccessTokenService
    {
        public int CreateCount { get; private set; }

        public AccessTokenResult Create(
            UserId userId,
            string email,
            DateTimeOffset nowUtc)
        {
            CreateCount++;

            return new AccessTokenResult(
                "ACCESS-TOKEN",
                nowUtc.AddMinutes(15));
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
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}