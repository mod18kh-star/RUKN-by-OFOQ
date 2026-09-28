using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.Reauthentication;

public sealed record VerifyMfaReauthenticationResult(
    UserId UserId,
    string Email);

public sealed class VerifyMfaReauthenticationHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUserMfaRepository _mfaRepository;
    private readonly IMfaSecretProtector _mfaSecretProtector;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public VerifyMfaReauthenticationHandler(
        IUserRepository userRepository,
        IUserMfaRepository mfaRepository,
        IMfaSecretProtector mfaSecretProtector,
        ITotpService totpService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _mfaRepository = mfaRepository;
        _mfaSecretProtector = mfaSecretProtector;
        _totpService = totpService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<VerifyMfaReauthenticationResult> HandleAsync(
        UserId userId,
        string? code,
        CancellationToken cancellationToken = default)
    {
        var now =
            _timeProvider.GetUtcNow();

        var user =
            await _userRepository.GetByIdAsync(
                userId,
                cancellationToken);

        var mfa =
            await _mfaRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        if (user is null ||
            user.IsDeleted ||
            user.Status != UserStatus.Active ||
            mfa is null ||
            mfa.Status != UserMfaStatus.Enabled ||
            string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidMfaLoginChallengeException();
        }

        var secret =
            _mfaSecretProtector.Unprotect(
                mfa.ProtectedSecret);

        var verification =
            _totpService.Verify(
                secret,
                code,
                now);

        if (!verification.IsValid ||
            !verification.TimeStep.HasValue)
        {
            throw new InvalidMfaLoginChallengeException();
        }

        var verifiedTimeStep =
            verification.TimeStep.Value;

        if (mfa.LastAcceptedTimeStep.HasValue &&
            verifiedTimeStep <=
                mfa.LastAcceptedTimeStep.Value)
        {
            throw new InvalidMfaLoginChallengeException();
        }

        mfa.AcceptTimeStep(
            verifiedTimeStep,
            now,
            user.Id.Value);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new VerifyMfaReauthenticationResult(
            user.Id,
            user.Email.Value);
    }
}