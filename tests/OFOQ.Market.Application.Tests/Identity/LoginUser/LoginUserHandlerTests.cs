using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Identity.LoginUser;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Tests.Identity.LoginUser;

public sealed class LoginUserHandlerTests
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
    public async Task Login_WithoutEnabledMfa_ReturnsAccessToken()
    {
        var user =
            CreateUser();

        var accessTokenService =
            new FakeAccessTokenService();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                user,
                mfa: null,
                accessTokenService:
                    accessTokenService,
                unitOfWork:
                    unitOfWork);

        var result =
            await handler.HandleAsync(
                new LoginUserCommand(
                    "user@example.com",
                    "correct-password"));

        Assert.False(
            result.RequiresMfa);

        Assert.Equal(
            user.Id,
            result.UserId);

        Assert.Equal(
            user.Email.Value,
            result.Email);

        Assert.Equal(
            "ACCESS-TOKEN",
            result.AccessToken);

        Assert.Equal(
            FixedNow.AddMinutes(15),
            result.AccessTokenExpiresAtUtc);

        Assert.Null(
            result.MfaChallengeToken);

        Assert.Null(
            result.MfaChallengeExpiresAtUtc);

        Assert.Equal(
            1,
            accessTokenService.CreateCount);

        Assert.Equal(
            AccessTokenAuthenticationLevel.PasswordOnly,
            accessTokenService.LastAuthenticationLevel);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Login_WithEnabledMfa_ReturnsChallengeWithoutAccessToken()
    {
        var user =
            CreateUser();

        var mfa =
            CreateEnabledMfa(
                user.Id);

        var challengeRepository =
            new FakeMfaLoginChallengeRepository();

        var accessTokenService =
            new FakeAccessTokenService();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                user,
                mfa,
                challengeRepository:
                    challengeRepository,
                accessTokenService:
                    accessTokenService,
                unitOfWork:
                    unitOfWork);

        var result =
            await handler.HandleAsync(
                new LoginUserCommand(
                    "user@example.com",
                    "correct-password"));

        Assert.True(
            result.RequiresMfa);

        Assert.Null(
            result.AccessToken);

        Assert.Null(
            result.AccessTokenExpiresAtUtc);

        Assert.Equal(
            "RAW-CHALLENGE-TOKEN",
            result.MfaChallengeToken);

        Assert.Equal(
            FixedNow.AddMinutes(5),
            result.MfaChallengeExpiresAtUtc);

        Assert.Equal(
            0,
            accessTokenService.CreateCount);

        Assert.Null(
            accessTokenService.LastAuthenticationLevel);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);

        var storedChallenge =
            Assert.Single(
                challengeRepository.Items);

        Assert.Equal(
            user.Id,
            storedChallenge.UserId);

        Assert.Equal(
            "HASHED-CHALLENGE-TOKEN",
            storedChallenge.TokenHash);

        Assert.Equal(
            FixedNow.AddMinutes(5),
            storedChallenge.ExpiresAtUtc);

        Assert.NotEqual(
            result.MfaChallengeToken,
            storedChallenge.TokenHash);
    }

    [Fact]
    public async Task Login_WithPendingMfa_ReturnsAccessToken()
    {
        var user =
            CreateUser();

        var pendingMfa =
            UserMfa.BeginEnrollment(
                user.Id,
                "PROTECTED-SECRET",
                FixedNow.AddMinutes(-10));

        var accessTokenService =
            new FakeAccessTokenService();

        var handler =
            CreateHandler(
                user,
                pendingMfa,
                accessTokenService:
                    accessTokenService);

        var result =
            await handler.HandleAsync(
                new LoginUserCommand(
                    "user@example.com",
                    "correct-password"));

        Assert.False(
            result.RequiresMfa);

        Assert.Equal(
            "ACCESS-TOKEN",
            result.AccessToken);

        Assert.Null(
            result.MfaChallengeToken);

        Assert.Equal(
            AccessTokenAuthenticationLevel.PasswordOnly,
            accessTokenService.LastAuthenticationLevel);
    }

    [Fact]
    public async Task Login_WithEnabledMfa_RevokesExistingActiveChallenge()
    {
        var user =
            CreateUser();

        var mfa =
            CreateEnabledMfa(
                user.Id);

        var previousChallenge =
            MfaLoginChallenge.Create(
                user.Id,
                "OLD-TOKEN-HASH",
                FixedNow.AddMinutes(3),
                FixedNow.AddMinutes(-2));

        var challengeRepository =
            new FakeMfaLoginChallengeRepository(
                previousChallenge);

        var handler =
            CreateHandler(
                user,
                mfa,
                challengeRepository:
                    challengeRepository);

        var result =
            await handler.HandleAsync(
                new LoginUserCommand(
                    "user@example.com",
                    "correct-password"));

        Assert.True(
            result.RequiresMfa);

        Assert.True(
            previousChallenge.IsRevoked);

        Assert.Equal(
            FixedNow,
            previousChallenge.RevokedAtUtc);

        Assert.Equal(
            2,
            challengeRepository.Items.Count);

        Assert.Single(
            challengeRepository.Items,
            challenge =>
                challenge.TokenHash ==
                "HASHED-CHALLENGE-TOKEN");
    }

    [Fact]
    public async Task Login_WithWrongPassword_DoesNotCreateChallengeOrAccessToken()
    {
        var user =
            CreateUser();

        var mfa =
            CreateEnabledMfa(
                user.Id);

        var challengeRepository =
            new FakeMfaLoginChallengeRepository();

        var accessTokenService =
            new FakeAccessTokenService();

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            CreateHandler(
                user,
                mfa,
                challengeRepository:
                    challengeRepository,
                accessTokenService:
                    accessTokenService,
                unitOfWork:
                    unitOfWork);

        await Assert.ThrowsAsync<
            InvalidCredentialsException>(
                () =>
                    handler.HandleAsync(
                        new LoginUserCommand(
                            "user@example.com",
                            "wrong-password")));

        Assert.Empty(
            challengeRepository.Items);

        Assert.Equal(
            0,
            accessTokenService.CreateCount);

        Assert.Null(
            accessTokenService.LastAuthenticationLevel);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_PerformsDummyPasswordVerification()
    {
        var passwordHasher =
            new FakePasswordHasher();

        var handler =
            new LoginUserHandler(
                new FakeUserRepository(
                    null),
                new FakeUserMfaRepository(
                    null),
                new FakeMfaLoginChallengeRepository(),
                passwordHasher,
                new FakeAccessTokenService(),
                new FakeChallengeTokenService(),
                new FakeUnitOfWork(),
                new FixedTimeProvider(
                    FixedNow));

        await Assert.ThrowsAsync<
            InvalidCredentialsException>(
                () =>
                    handler.HandleAsync(
                        new LoginUserCommand(
                            "missing@example.com",
                            "correct-password")));

        Assert.Equal(
            1,
            passwordHasher.DummyVerificationCount);
    }

    private static LoginUserHandler CreateHandler(
        User user,
        UserMfa? mfa,
        FakeMfaLoginChallengeRepository? challengeRepository = null,
        FakeAccessTokenService? accessTokenService = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new LoginUserHandler(
            new FakeUserRepository(
                user),
            new FakeUserMfaRepository(
                mfa),
            challengeRepository ??
                new FakeMfaLoginChallengeRepository(),
            new FakePasswordHasher(),
            accessTokenService ??
                new FakeAccessTokenService(),
            new FakeChallengeTokenService(),
            unitOfWork ??
                new FakeUnitOfWork(),
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
                FixedNow.AddMinutes(-15));

        mfa.ConfirmEnrollment(
            100,
            FixedNow.AddMinutes(-14));

        return mfa;
    }

    private sealed class FakeUserRepository :
        IUserRepository
    {
        private readonly User? _user;

        public FakeUserRepository(
            User? user)
        {
            _user =
                user;
        }

        public Task<User?> GetByIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _user?.Id == userId
                    ? _user
                    : null);
        }

        public Task<User?> GetByEmailAsync(
            EmailAddress email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _user?.Email == email
                    ? _user
                    : null);
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

    private sealed class FakeMfaLoginChallengeRepository :
        IMfaLoginChallengeRepository
    {
        private readonly List<MfaLoginChallenge> _items;

        public FakeMfaLoginChallengeRepository(
            params MfaLoginChallenge[] challenges)
        {
            _items =
                challenges.ToList();
        }

        public IReadOnlyList<MfaLoginChallenge> Items =>
            _items;

        public Task<MfaLoginChallenge?> GetByIdAsync(
            MfaLoginChallengeId challengeId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _items.SingleOrDefault(
                    challenge =>
                        challenge.Id ==
                        challengeId));
        }

        public Task<MfaLoginChallenge?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _items.SingleOrDefault(
                    challenge =>
                        challenge.TokenHash ==
                        tokenHash));
        }

        public Task<IReadOnlyList<MfaLoginChallenge>>
            GetActiveByUserIdAsync(
                UserId userId,
                DateTimeOffset nowUtc,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MfaLoginChallenge> result =
                _items
                    .Where(
                        challenge =>
                            challenge.UserId ==
                                userId &&
                            challenge.IsUsable(
                                nowUtc))
                    .ToArray();

            return Task.FromResult(
                result);
        }

        public Task AddAsync(
            MfaLoginChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            _items.Add(
                challenge);

            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher :
        IPasswordHasher
    {
        public int DummyVerificationCount { get; private set; }

        public string Hash(
            string password)
        {
            return "HASHED-PASSWORD";
        }

        public bool Verify(
            string passwordHash,
            string password)
        {
            return passwordHash ==
                    "HASHED-PASSWORD"
                && password ==
                    "correct-password";
        }

        public void PerformDummyVerification(
            string password)
        {
            DummyVerificationCount++;
        }
    }

    private sealed class FakeAccessTokenService :
        IAccessTokenService
    {
        public int CreateCount { get; private set; }

        public AccessTokenAuthenticationLevel?
            LastAuthenticationLevel { get; private set; }

        public AccessTokenResult Create(
            UserId userId,
            string email,
            DateTimeOffset nowUtc,
            AccessTokenAuthenticationLevel authenticationLevel)
        {
            CreateCount++;

            LastAuthenticationLevel =
                authenticationLevel;

            return new AccessTokenResult(
                "ACCESS-TOKEN",
                nowUtc.AddMinutes(15));
        }
    }

    private sealed class FakeChallengeTokenService :
        IMfaLoginChallengeTokenService
    {
        public MfaLoginChallengeToken Create()
        {
            return new MfaLoginChallengeToken(
                "RAW-CHALLENGE-TOKEN",
                "HASHED-CHALLENGE-TOKEN");
        }

        public bool TryHash(
            string? token,
            out string tokenHash)
        {
            if (token ==
                "RAW-CHALLENGE-TOKEN")
            {
                tokenHash =
                    "HASHED-CHALLENGE-TOKEN";

                return true;
            }

            tokenHash =
                string.Empty;

            return false;
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