using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;

public sealed class VerifyMfaTotpHandler
{
    private readonly IMfaLoginChallengeRepository
        _challengeRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly IUserMfaRepository
        _userMfaRepository;

    private readonly IMfaLoginChallengeTokenService
        _challengeTokenService;

    private readonly IMfaSecretProtector
        _mfaSecretProtector;

    private readonly ITotpService
        _totpService;

    private readonly IAccessTokenService
        _accessTokenService;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public VerifyMfaTotpHandler(
        IMfaLoginChallengeRepository challengeRepository,
        IUserRepository userRepository,
        IUserMfaRepository userMfaRepository,
        IMfaLoginChallengeTokenService challengeTokenService,
        IMfaSecretProtector mfaSecretProtector,
        ITotpService totpService,
        IAccessTokenService accessTokenService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _challengeRepository =
            challengeRepository;

        _userRepository =
            userRepository;

        _userMfaRepository =
            userMfaRepository;

        _challengeTokenService =
            challengeTokenService;

        _mfaSecretProtector =
            mfaSecretProtector;

        _totpService =
            totpService;

        _accessTokenService =
            accessTokenService;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<VerifyMfaTotpResult> HandleAsync(
        VerifyMfaTotpCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var now =
            _timeProvider.GetUtcNow();

        if (!_challengeTokenService.TryHash(
                command.ChallengeToken,
                out var challengeTokenHash))
        {
            throw new InvalidMfaLoginChallengeException();
        }

        var challenge =
            await _challengeRepository
                .GetByTokenHashAsync(
                    challengeTokenHash,
                    cancellationToken);

        if (challenge is null ||
            !challenge.IsUsable(now))
        {
            throw new InvalidMfaLoginChallengeException();
        }

        var user =
            await _userRepository
                .GetByIdAsync(
                    challenge.UserId,
                    cancellationToken);

        var mfa =
            await _userMfaRepository
                .GetByUserIdAsync(
                    challenge.UserId,
                    cancellationToken);

        if (user is null ||
            user.Status != UserStatus.Active ||
            mfa is null ||
            mfa.Status != UserMfaStatus.Enabled)
        {
            challenge.Revoke(
                now);

            await _unitOfWork
                .SaveChangesAsync(
                    cancellationToken);

            throw new InvalidMfaLoginChallengeException();
        }

        if (string.IsNullOrWhiteSpace(
                command.Code))
        {
            await RejectAttemptAsync(
                challenge,
                now,
                cancellationToken);

            throw new InvalidMfaLoginChallengeException();
        }

        var secret =
            _mfaSecretProtector.Unprotect(
                mfa.ProtectedSecret);

        var verification =
            _totpService.Verify(
                secret,
                command.Code,
                now);

        if (!verification.IsValid ||
            !verification.TimeStep.HasValue)
        {
            await RejectAttemptAsync(
                challenge,
                now,
                cancellationToken);

            throw new InvalidMfaLoginChallengeException();
        }

        var verifiedTimeStep =
            verification.TimeStep.Value;

        if (mfa.LastAcceptedTimeStep.HasValue &&
            verifiedTimeStep <=
            mfa.LastAcceptedTimeStep.Value)
        {
            await RejectAttemptAsync(
                challenge,
                now,
                cancellationToken);

            throw new InvalidMfaLoginChallengeException();
        }

        /*
         * Both state changes are tracked by the same UnitOfWork.
         *
         * No access token is created until SaveChanges succeeds.
         *
         * Therefore:
         * - TOTP timestep acceptance
         * - challenge consumption
         *
         * commit together before authentication is completed.
         */
        mfa.AcceptTimeStep(
            verifiedTimeStep,
            now,
            user.Id.Value);

        challenge.Consume(
            now,
            user.Id.Value);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        var accessToken =
            _accessTokenService.Create(
                user.Id,
                user.Email.Value,
                now,
                AccessTokenAuthenticationLevel.MultiFactor,
                challenge.AuthenticationMethod);

        return new VerifyMfaTotpResult(
            user.Id,
            user.Email.Value,
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            challenge.AuthenticationMethod);
    }

    private async Task RejectAttemptAsync(
        MfaLoginChallenge challenge,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        challenge.RegisterFailedAttempt(
            now);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);
    }
}