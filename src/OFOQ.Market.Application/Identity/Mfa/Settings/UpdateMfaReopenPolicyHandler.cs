using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.Mfa.Settings;

public sealed class UpdateMfaReopenPolicyHandler
{
    private readonly IUserMfaRepository _mfaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdateMfaReopenPolicyHandler(
        IUserMfaRepository mfaRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _mfaRepository = mfaRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<MfaReopenPolicy> HandleAsync(
        UserId userId,
        MfaReopenPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var mfa =
            await _mfaRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        if (mfa is null ||
            mfa.Status != UserMfaStatus.Enabled)
        {
            throw new InvalidOperationException(
                "MFA is not enabled.");
        }

        mfa.UpdateReopenPolicy(
            policy,
            _timeProvider.GetUtcNow(),
            userId.Value);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return mfa.ReopenPolicy;
    }
}