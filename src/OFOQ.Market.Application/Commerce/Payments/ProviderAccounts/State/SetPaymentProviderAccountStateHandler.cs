using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.State;

public sealed class SetPaymentProviderAccountStateHandler
{
    private readonly ITenantPaymentProviderAccountRepository _accountRepository;
    private readonly ITenantPaymentWalletCapabilityRepository _walletRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SetPaymentProviderAccountStateHandler(
        ITenantPaymentProviderAccountRepository accountRepository,
        ITenantPaymentWalletCapabilityRepository walletRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _accountRepository = accountRepository;
        _walletRepository = walletRepository;
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PaymentProviderAccountResult> HandleAsync(
        SetPaymentProviderAccountStateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (command.AccountId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider account ID cannot be empty.",
                nameof(command));
        }

        if (command.ActorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Actor user ID cannot be empty.",
                nameof(command));
        }

        var tenantId =
            PaymentProviderAccountRules.GetRequiredTenantId(
                _currentTenant);

        var account =
            await _accountRepository.GetByIdAsync(
                command.AccountId,
                cancellationToken);

        PaymentProviderAccountRules.EnsureOwned(
            account,
            tenantId);

        if (command.Enabled)
        {
            var activeAccount =
                await _accountRepository.GetEnabledByProviderAsync(
                    account!.ProviderCode,
                    cancellationToken);

            if (activeAccount is not null &&
                activeAccount.Id != account.Id)
            {
                throw new PaymentProviderAccountAlreadyExistsException(
                    "Another enabled account already exists for this payment provider.");
            }
        }

        account!.SetEnabled(
            command.Enabled,
            _timeProvider.GetUtcNow(),
            command.ActorUserId);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var wallets =
            await _walletRepository.GetByAccountAsync(
                account.Id,
                cancellationToken);

        return PaymentProviderAccountRules.MapAccount(
            account,
            wallets);
    }
}