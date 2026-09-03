using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Consume;

public sealed class ConsumeRecoveryCodeHandler
{
    private readonly IUserMfaRepository _userMfaRepository;
    private readonly IUserMfaRecoveryCodeRepository
        _recoveryCodeRepository;
    private readonly IRecoveryCodeService
        _recoveryCodeService;
    private readonly TimeProvider _timeProvider;

    public ConsumeRecoveryCodeHandler(
        IUserMfaRepository userMfaRepository,
        IUserMfaRecoveryCodeRepository recoveryCodeRepository,
        IRecoveryCodeService recoveryCodeService,
        TimeProvider timeProvider)
    {
        _userMfaRepository =
            userMfaRepository;

        _recoveryCodeRepository =
            recoveryCodeRepository;

        _recoveryCodeService =
            recoveryCodeService;

        _timeProvider =
            timeProvider;
    }

    public async Task HandleAsync(
        ConsumeRecoveryCodeCommand command,
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
            throw new InvalidRecoveryCodeException();
        }

        string codeHash;

        try
        {
            codeHash =
                _recoveryCodeService.Hash(
                    command.Code);
        }
        catch (ArgumentException)
        {
            throw new InvalidRecoveryCodeException();
        }

        var consumed =
            await _recoveryCodeRepository
                .TryConsumeByHashAsync(
                    mfa.Id,
                    codeHash,
                    _timeProvider.GetUtcNow(),
                    command.UserId.Value,
                    cancellationToken);

        if (!consumed)
        {
            throw new InvalidRecoveryCodeException();
        }
    }
}