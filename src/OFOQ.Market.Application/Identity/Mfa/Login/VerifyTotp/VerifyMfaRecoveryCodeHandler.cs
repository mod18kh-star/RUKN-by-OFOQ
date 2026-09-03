using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.Login.VerifyRecovery;

public sealed class VerifyMfaRecoveryCodeHandler
{
    private readonly IMfaLoginChallengeRepository
        _challengeRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly IUserMfaRepository
        _userMfaRepository;

    private readonly IUserMfaRecoveryCodeRepository
        _recoveryCodeRepository;

    private readonly IMfaLoginChallengeTokenService
        _challengeTokenService;

    private readonly IRecoveryCodeService
        _recoveryCodeService;

    private readonly IAccessTokenService
        _accessTokenService;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly ITransactionExecutor
        _transactionExecutor;

    private readonly TimeProvider
        _timeProvider;

    public VerifyMfaRecoveryCodeHandler(
        IMfaLoginChallengeRepository challengeRepository,
        IUserRepository userRepository,
        IUserMfaRepository userMfaRepository,
        IUserMfaRecoveryCodeRepository recoveryCodeRepository,
        IMfaLoginChallengeTokenService challengeTokenService,
        IRecoveryCodeService recoveryCodeService,
        IAccessTokenService accessTokenService,
        IUnitOfWork unitOfWork,
        ITransactionExecutor transactionExecutor,
        TimeProvider timeProvider)
    {
        _challengeRepository =
            challengeRepository;

        _userRepository =
            userRepository;

        _userMfaRepository =
            userMfaRepository;

        _recoveryCodeRepository =
            recoveryCodeRepository;

        _challengeTokenService =
            challengeTokenService;

        _recoveryCodeService =
            recoveryCodeService;

        _accessTokenService =
            accessTokenService;

        _unitOfWork =
            unitOfWork;

        _transactionExecutor =
            transactionExecutor;

        _timeProvider =
            timeProvider;
    }

    public async Task<VerifyMfaRecoveryCodeResult> HandleAsync(
        VerifyMfaRecoveryCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (!_challengeTokenService.TryHash(
                command.ChallengeToken,
                out var challengeHash))
        {
            throw new InvalidMfaRecoveryVerificationException();
        }

        var now =
            _timeProvider.GetUtcNow();

        VerificationOutcome outcome;

        try
        {
            outcome =
                await _transactionExecutor
                    .ExecuteAsync(
                        async transactionCancellationToken =>
                        {
                            var challenge =
                                await _challengeRepository
                                    .GetByTokenHashAsync(
                                        challengeHash,
                                        transactionCancellationToken);

                            if (challenge is null ||
                                !challenge.IsUsable(now))
                            {
                                return VerificationOutcome.Failed();
                            }

                            var user =
                                await _userRepository
                                    .GetByIdAsync(
                                        challenge.UserId,
                                        transactionCancellationToken);

                            var mfa =
                                await _userMfaRepository
                                    .GetByUserIdAsync(
                                        challenge.UserId,
                                        transactionCancellationToken);

                            if (user is null ||
                                user.Status != UserStatus.Active ||
                                mfa is null ||
                                mfa.Status != UserMfaStatus.Enabled)
                            {
                                challenge.Revoke(
                                    now);

                                await _unitOfWork
                                    .SaveChangesAsync(
                                        transactionCancellationToken);

                                return VerificationOutcome.Failed();
                            }

                            string recoveryCodeHash;

                            try
                            {
                                recoveryCodeHash =
                                    _recoveryCodeService.Hash(
                                        command.RecoveryCode);
                            }
                            catch (ArgumentException)
                            {
                                challenge.RegisterFailedAttempt(
                                    now,
                                    user.Id.Value);

                                await _unitOfWork
                                    .SaveChangesAsync(
                                        transactionCancellationToken);

                                return VerificationOutcome.Failed();
                            }

                            var recoveryCodeConsumed =
                                await _recoveryCodeRepository
                                    .TryConsumeByHashAsync(
                                        mfa.Id,
                                        recoveryCodeHash,
                                        now,
                                        user.Id.Value,
                                        transactionCancellationToken);

                            if (!recoveryCodeConsumed)
                            {
                                challenge.RegisterFailedAttempt(
                                    now,
                                    user.Id.Value);

                                await _unitOfWork
                                    .SaveChangesAsync(
                                        transactionCancellationToken);

                                return VerificationOutcome.Failed();
                            }

                            /*
                             * The recovery code was consumed inside
                             * this same database transaction.
                             *
                             * The challenge must now also be consumed.
                             *
                             * If SaveChanges fails because another
                             * request consumed the challenge first,
                             * the entire transaction is rolled back,
                             * including the recovery-code update.
                             */
                            challenge.Consume(
                                now,
                                user.Id.Value);

                            await _unitOfWork
                                .SaveChangesAsync(
                                    transactionCancellationToken);

                            return VerificationOutcome.Succeeded(
                                user.Id,
                                user.Email.Value);
                        },
                        cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            throw new InvalidMfaRecoveryVerificationException();
        }

        if (!outcome.IsSuccessful)
        {
            throw new InvalidMfaRecoveryVerificationException();
        }

        /*
         * Do not create the JWT before the database transaction
         * has committed successfully.
         */
        var accessToken =
            _accessTokenService.Create(
                outcome.UserId,
                outcome.Email,
                now,
                AccessTokenAuthenticationLevel.MultiFactor);

        return new VerifyMfaRecoveryCodeResult(
            outcome.UserId,
            outcome.Email,
            accessToken.Token,
            accessToken.ExpiresAtUtc);
    }

    private sealed record VerificationOutcome(
        bool IsSuccessful,
        UserId UserId,
        string Email)
    {
        public static VerificationOutcome Failed()
        {
            return new VerificationOutcome(
                false,
                default,
                string.Empty);
        }

        public static VerificationOutcome Succeeded(
            UserId userId,
            string email)
        {
            return new VerificationOutcome(
                true,
                userId,
                email);
        }
    }
}