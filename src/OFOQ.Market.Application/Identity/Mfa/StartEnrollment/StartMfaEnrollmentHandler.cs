using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.StartEnrollment;

public sealed class StartMfaEnrollmentHandler
{
    private const string Issuer =
        "RUKN";

    private readonly IUserRepository _userRepository;
    private readonly IUserMfaRepository _userMfaRepository;
    private readonly ITotpService _totpService;
    private readonly IMfaSecretProtector _secretProtector;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public StartMfaEnrollmentHandler(
        IUserRepository userRepository,
        IUserMfaRepository userMfaRepository,
        ITotpService totpService,
        IMfaSecretProtector secretProtector,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userRepository =
            userRepository;

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

    public async Task<StartMfaEnrollmentResult> HandleAsync(
        StartMfaEnrollmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var user =
            await _userRepository.GetByIdAsync(
                command.UserId,
                cancellationToken);

        if (user is null ||
            user.Status != UserStatus.Active)
        {
            throw new InvalidOperationException(
                "The user cannot enroll MFA.");
        }

        var existingMfa =
            await _userMfaRepository.GetByUserIdAsync(
                user.Id,
                cancellationToken);

        if (existingMfa?.Status ==
            UserMfaStatus.Enabled)
        {
            throw new MfaAlreadyEnabledException();
        }

        var enrollment =
            _totpService.CreateEnrollment(
                user.Email.Value,
                Issuer);

        var protectedSecret =
            _secretProtector.Protect(
                enrollment.Secret);

        var now =
            _timeProvider.GetUtcNow();

        if (existingMfa is null)
        {
            var userMfa =
                UserMfa.BeginEnrollment(
                    user.Id,
                    protectedSecret,
                    now,
                    user.Id.Value);

            await _userMfaRepository.AddAsync(
                userMfa,
                cancellationToken);
        }
        else
        {
            existingMfa.RestartEnrollment(
                protectedSecret,
                now,
                user.Id.Value);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new StartMfaEnrollmentResult(
            enrollment.Secret,
            enrollment.ProvisioningUri);
    }
}