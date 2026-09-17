using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.CompleteEnrollment;

public sealed class CompleteMfaEnrollmentHandler
{
    private const int RecoveryCodeCount =
        8;

    private readonly IUserRepository
        _userRepository;

    private readonly IUserMfaRepository
        _userMfaRepository;

    private readonly IUserMfaRecoveryCodeRepository
        _recoveryCodeRepository;

    private readonly ITotpService
        _totpService;

    private readonly IMfaSecretProtector
        _secretProtector;

    private readonly IRecoveryCodeService
        _recoveryCodeService;

    private readonly IAccessTokenService
        _accessTokenService;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly TimeProvider
        _timeProvider;

    public CompleteMfaEnrollmentHandler(
        IUserRepository userRepository,
        IUserMfaRepository userMfaRepository,
        IUserMfaRecoveryCodeRepository recoveryCodeRepository,
        ITotpService totpService,
        IMfaSecretProtector secretProtector,
        IRecoveryCodeService recoveryCodeService,
        IAccessTokenService accessTokenService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userRepository =
            userRepository;

        _userMfaRepository =
            userMfaRepository;

        _recoveryCodeRepository =
            recoveryCodeRepository;

        _totpService =
            totpService;

        _secretProtector =
            secretProtector;

        _recoveryCodeService =
            recoveryCodeService;

        _accessTokenService =
            accessTokenService;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<CompleteMfaEnrollmentResult> HandleAsync(
        CompleteMfaEnrollmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var user =
            await _userRepository
                .GetByIdAsync(
                    command.UserId,
                    cancellationToken);

        var mfa =
            await _userMfaRepository
                .GetByUserIdAsync(
                    command.UserId,
                    cancellationToken);

        if (user is null ||
            user.IsDeleted ||
            user.Status !=
                UserStatus.Active ||
            mfa is null ||
            mfa.Status !=
                UserMfaStatus.PendingEnrollment)
        {
            throw new InvalidMfaCodeException();
        }

        if (string.IsNullOrWhiteSpace(
                command.Code))
        {
            throw new InvalidMfaCodeException();
        }

        var secret =
            _secretProtector.Unprotect(
                mfa.ProtectedSecret);

        var now =
            _timeProvider.GetUtcNow();

        var verification =
            _totpService.Verify(
                secret,
                command.Code,
                now);

        if (!verification.IsValid ||
            !verification.TimeStep.HasValue)
        {
            throw new InvalidMfaCodeException();
        }

        var recoveryCodesAlreadyExist =
            await _recoveryCodeRepository
                .AnyForUserMfaAsync(
                    mfa.Id,
                    cancellationToken);

        if (recoveryCodesAlreadyExist)
        {
            throw new InvalidOperationException(
                "Recovery codes already exist for this MFA enrollment.");
        }

        var rawRecoveryCodes =
            _recoveryCodeService
                .GenerateCodes(
                    RecoveryCodeCount);

        var recoveryCodeEntities =
            rawRecoveryCodes
                .Select(
                    code =>
                        UserMfaRecoveryCode.Create(
                            mfa.Id,
                            _recoveryCodeService.Hash(
                                code),
                            now,
                            user.Id.Value))
                .ToArray();

        mfa.ConfirmEnrollment(
            verification.TimeStep.Value,
            now,
            user.Id.Value);

        await _recoveryCodeRepository
            .AddRangeAsync(
                recoveryCodeEntities,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        var accessToken =
            _accessTokenService.Create(
                user.Id,
                user.Email.Value,
                now,
                AccessTokenAuthenticationLevel.MultiFactor);

        return new CompleteMfaEnrollmentResult(
            rawRecoveryCodes,
            accessToken.Token,
            accessToken.ExpiresAtUtc);
    }
}
