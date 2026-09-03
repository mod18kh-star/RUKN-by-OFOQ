using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Generate;

public sealed class GenerateRecoveryCodesHandler
{
    private const int RecoveryCodeCount =
        8;

    private readonly IUserMfaRepository _userMfaRepository;
    private readonly IUserMfaRecoveryCodeRepository
        _recoveryCodeRepository;
    private readonly IRecoveryCodeService
        _recoveryCodeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public GenerateRecoveryCodesHandler(
        IUserMfaRepository userMfaRepository,
        IUserMfaRecoveryCodeRepository recoveryCodeRepository,
        IRecoveryCodeService recoveryCodeService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userMfaRepository =
            userMfaRepository;

        _recoveryCodeRepository =
            recoveryCodeRepository;

        _recoveryCodeService =
            recoveryCodeService;

        _unitOfWork =
            unitOfWork;

        _timeProvider =
            timeProvider;
    }

    public async Task<RecoveryCodesResult> HandleAsync(
        GenerateRecoveryCodesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var mfa =
            await _userMfaRepository
                .GetByUserIdAsync(
                    command.UserId,
                    cancellationToken);

        if (mfa is null ||
            mfa.Status != UserMfaStatus.Enabled)
        {
            throw new InvalidOperationException(
                "MFA must be enabled before generating recovery codes.");
        }

        var alreadyGenerated =
            await _recoveryCodeRepository
                .AnyForUserMfaAsync(
                    mfa.Id,
                    cancellationToken);

        if (alreadyGenerated)
        {
            throw new RecoveryCodesAlreadyGeneratedException();
        }

        var codes =
            _recoveryCodeService
                .GenerateCodes(
                    RecoveryCodeCount);

        var now =
            _timeProvider.GetUtcNow();

        var recoveryCodes =
            codes
                .Select(
                    code =>
                        UserMfaRecoveryCode.Create(
                            mfa.Id,
                            _recoveryCodeService.Hash(
                                code),
                            now,
                            command.UserId.Value))
                .ToArray();

        await _recoveryCodeRepository
            .AddRangeAsync(
                recoveryCodes,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return new RecoveryCodesResult(
            codes);
    }
}