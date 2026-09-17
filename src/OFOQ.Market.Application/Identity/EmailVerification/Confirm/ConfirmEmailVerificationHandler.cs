using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.EmailVerification.Confirm;

public sealed class ConfirmEmailVerificationHandler
{
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

    public ConfirmEmailVerificationHandler(
        IUserRepository userRepository,
        IEmailVerificationChallengeRepository challengeRepository,
        IEmailVerificationTokenService tokenService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
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
    }

    public async Task<ConfirmEmailVerificationResult>
        HandleAsync(
            ConfirmEmailVerificationCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (!_tokenService.TryHash(
                command.Token,
                out var tokenHash))
        {
            throw new InvalidEmailVerificationException();
        }

        var challenge =
            await _challengeRepository
                .GetByTokenHashAsync(
                    tokenHash,
                    cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        if (challenge is null ||
            !challenge.IsUsable(
                now))
        {
            throw new InvalidEmailVerificationException();
        }

        var user =
            await _userRepository
                .GetByIdAsync(
                    challenge.UserId,
                    cancellationToken);

        if (user is null ||
            user.Status !=
                UserStatus.Active ||
            !string.Equals(
                user.Email.Value,
                challenge.EmailSnapshot,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidEmailVerificationException();
        }

        user.MarkEmailVerified(
            now,
            user.Id.Value);

        challenge.Consume(
            now,
            user.Id.Value);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return new ConfirmEmailVerificationResult(
            user.Id.Value,
            user.Email.Value,
            now);
    }
}
