using OFOQ.Market.Application.Common.Notifications;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.EmailVerification.Start;

public sealed class StartEmailVerificationHandler
{
    private static readonly TimeSpan ChallengeLifetime =
        TimeSpan.FromMinutes(
            30);

    private readonly IUserRepository
        _userRepository;

    private readonly IEmailVerificationChallengeRepository
        _challengeRepository;

    private readonly IEmailVerificationTokenService
        _tokenService;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    private readonly ITransactionalEmailQueue?
        _emailQueue;

    public StartEmailVerificationHandler(
        IUserRepository userRepository,
        IEmailVerificationChallengeRepository challengeRepository,
        IEmailVerificationTokenService tokenService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ITransactionalEmailQueue? emailQueue = null)
    {
        _userRepository =
            userRepository;

        _challengeRepository =
            challengeRepository;

        _tokenService =
            tokenService;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;

        _emailQueue =
            emailQueue;
    }

    public async Task<StartEmailVerificationResult>
        HandleAsync(
            StartEmailVerificationCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var user =
            await _userRepository
                .GetByIdAsync(
                    command.UserId,
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "The account is unavailable.");

        if (user.Status !=
            UserStatus.Active)
        {
            throw new InvalidOperationException(
                "The account is unavailable.");
        }

        if (user.EmailVerifiedAtUtc.HasValue)
        {
            throw new EmailAlreadyVerifiedException();
        }

        var now =
            _timeProvider.GetUtcNow();

        var activeChallenges =
            await _challengeRepository
                .GetActiveByUserIdAsync(
                    user.Id,
                    now,
                    cancellationToken);

        foreach (var activeChallenge in
                 activeChallenges)
        {
            activeChallenge.Revoke(
                now,
                user.Id.Value);
        }

        var generatedToken =
            _tokenService.Create();

        var expiresAtUtc =
            now.Add(
                ChallengeLifetime);

        var challenge =
            EmailVerificationChallenge.Create(
                user.Id,
                user.Email.Value,
                generatedToken.TokenHash,
                expiresAtUtc,
                now,
                user.Id.Value);

        await _challengeRepository
            .AddAsync(
                challenge,
                cancellationToken);

        if (_emailQueue is not null)
        {
            await _emailQueue.QueueEmailVerificationAsync(
                user.Id,
                user.Email.Value,
                generatedToken.Token,
                expiresAtUtc,
                now,
                cancellationToken);
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return new StartEmailVerificationResult(
            user.Email.Value,
            expiresAtUtc,
            generatedToken.Token);
    }
}
