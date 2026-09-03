using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.ConfirmEnrollment;

public sealed class ConfirmMfaEnrollmentHandler
{
    private readonly IUserMfaRepository _userMfaRepository;
    private readonly ITotpService _totpService;
    private readonly IMfaSecretProtector _secretProtector;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ConfirmMfaEnrollmentHandler(
        IUserMfaRepository userMfaRepository,
        ITotpService totpService,
        IMfaSecretProtector secretProtector,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userMfaRepository =
            userMfaRepository;

        _totpService =
            totpService;

        _secretProtector =
            secretProtector;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task HandleAsync(
        ConfirmMfaEnrollmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var mfa =
            await _userMfaRepository.GetByUserIdAsync(
                command.UserId,
                cancellationToken);

        if (mfa is null ||
            mfa.Status !=
            UserMfaStatus.PendingEnrollment)
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

        mfa.ConfirmEnrollment(
            verification.TimeStep.Value,
            now,
            command.UserId.Value);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }
}