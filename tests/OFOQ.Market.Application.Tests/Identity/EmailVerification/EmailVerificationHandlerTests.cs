using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Identity.EmailVerification;
using OFOQ.Market.Application.Identity.EmailVerification.Confirm;
using OFOQ.Market.Application.Identity.EmailVerification.Start;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Tests.Identity.EmailVerification;

public sealed class EmailVerificationHandlerTests
{
    private static readonly DateTimeOffset FixedNow =
        new(
            2026,
            9,
            16,
            1,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task Start_CreatesChallenge_AndRevokesPreviousActiveChallenge()
    {
        var user =
            CreateUser();

        var existing =
            EmailVerificationChallenge.Create(
                user.Id,
                user.Email.Value,
                "OLD-HASH",
                FixedNow.AddMinutes(
                    10),
                FixedNow.AddMinutes(
                    -5));

        var challengeRepository =
            new FakeChallengeRepository(
                existing);

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new StartEmailVerificationHandler(
                new FakeUserRepository(
                    user),
                challengeRepository,
                new FakeTokenService(),
                unitOfWork,
                new FixedTimeProvider(
                    FixedNow));

        var result =
            await handler.HandleAsync(
                new StartEmailVerificationCommand(
                    user.Id));

        Assert.Equal(
            user.Email.Value,
            result.Email);

        Assert.Equal(
            "RAW-EMAIL-VERIFICATION-TOKEN",
            result.VerificationToken);

        Assert.True(
            existing.IsRevoked);

        Assert.NotNull(
            challengeRepository.Added);

        Assert.Equal(
            user.Id,
            challengeRepository.Added.UserId);

        Assert.Equal(
            user.Email.Value,
            challengeRepository.Added.EmailSnapshot);

        Assert.Equal(
            "TOKEN-HASH",
            challengeRepository.Added.TokenHash);

        Assert.Equal(
            FixedNow.AddMinutes(
                30),
            challengeRepository.Added.ExpiresAtUtc);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Start_RejectsAlreadyVerifiedEmail()
    {
        var user =
            CreateUser();

        user.MarkEmailVerified(
            FixedNow.AddMinutes(
                -1));

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new StartEmailVerificationHandler(
                new FakeUserRepository(
                    user),
                new FakeChallengeRepository(),
                new FakeTokenService(),
                unitOfWork,
                new FixedTimeProvider(
                    FixedNow));

        await Assert.ThrowsAsync<
            EmailAlreadyVerifiedException>(
                () =>
                    handler.HandleAsync(
                        new StartEmailVerificationCommand(
                            user.Id)));

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Confirm_MarksCurrentEmailVerified_AndConsumesChallenge()
    {
        var user =
            CreateUser();

        var challenge =
            EmailVerificationChallenge.Create(
                user.Id,
                user.Email.Value,
                "TOKEN-HASH",
                FixedNow.AddMinutes(
                    30),
                FixedNow.AddMinutes(
                    -1));

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ConfirmEmailVerificationHandler(
                new FakeUserRepository(
                    user),
                new FakeChallengeRepository(
                    challenge),
                new FakeTokenService(),
                unitOfWork,
                new FixedTimeProvider(
                    FixedNow));

        var result =
            await handler.HandleAsync(
                new ConfirmEmailVerificationCommand(
                    "RAW-EMAIL-VERIFICATION-TOKEN"));

        Assert.Equal(
            user.Id.Value,
            result.UserId);

        Assert.Equal(
            FixedNow,
            user.EmailVerifiedAtUtc);

        Assert.True(
            challenge.IsConsumed);

        Assert.Equal(
            1,
            unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Confirm_RejectsToken_WhenAccountEmailChangedAfterChallengeWasCreated()
    {
        var user =
            CreateUser();

        var challenge =
            EmailVerificationChallenge.Create(
                user.Id,
                user.Email.Value,
                "TOKEN-HASH",
                FixedNow.AddMinutes(
                    30),
                FixedNow.AddMinutes(
                    -2));

        user.ChangeEmail(
            "new@example.com",
            FixedNow.AddMinutes(
                -1));

        var unitOfWork =
            new FakeUnitOfWork();

        var handler =
            new ConfirmEmailVerificationHandler(
                new FakeUserRepository(
                    user),
                new FakeChallengeRepository(
                    challenge),
                new FakeTokenService(),
                unitOfWork,
                new FixedTimeProvider(
                    FixedNow));

        await Assert.ThrowsAsync<
            InvalidEmailVerificationException>(
                () =>
                    handler.HandleAsync(
                        new ConfirmEmailVerificationCommand(
                            "RAW-EMAIL-VERIFICATION-TOKEN")));

        Assert.Null(
            user.EmailVerifiedAtUtc);

        Assert.False(
            challenge.IsConsumed);

        Assert.Equal(
            0,
            unitOfWork.SaveChangesCount);
    }

    private static User CreateUser()
    {
        return User.Create(
            "user@example.com",
            "HASHED",
            FixedNow.AddHours(
                -1));
    }

    private sealed class FakeUserRepository :
        IUserRepository
    {
        private readonly User?
            _user;

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
                _user is not null &&
                _user.Id == userId
                    ? _user
                    : null);
        }

        public Task<User?> GetByEmailAsync(
            EmailAddress email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _user is not null &&
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
                false);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeChallengeRepository :
        IEmailVerificationChallengeRepository
    {
        private readonly List<EmailVerificationChallenge>
            _challenges;

        public FakeChallengeRepository(
            params EmailVerificationChallenge[] challenges)
        {
            _challenges =
                challenges.ToList();
        }

        public EmailVerificationChallenge?
            Added { get; private set; }

        public Task<EmailVerificationChallenge?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _challenges
                    .Concat(
                        Added is null
                            ? []
                            : [Added])
                    .SingleOrDefault(
                        challenge =>
                            challenge.TokenHash ==
                                tokenHash));
        }

        public Task<IReadOnlyList<EmailVerificationChallenge>>
            GetActiveByUserIdAsync(
                UserId userId,
                DateTimeOffset nowUtc,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<EmailVerificationChallenge> result =
                _challenges
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
            EmailVerificationChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            Added =
                challenge;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeTokenService :
        IEmailVerificationTokenService
    {
        public EmailVerificationToken Create()
        {
            return new EmailVerificationToken(
                "RAW-EMAIL-VERIFICATION-TOKEN",
                "TOKEN-HASH");
        }

        public bool TryHash(
            string? token,
            out string tokenHash)
        {
            tokenHash =
                token ==
                    "RAW-EMAIL-VERIFICATION-TOKEN"
                    ? "TOKEN-HASH"
                    : string.Empty;

            return tokenHash.Length >
                0;
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
        private readonly DateTimeOffset
            _utcNow;

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
