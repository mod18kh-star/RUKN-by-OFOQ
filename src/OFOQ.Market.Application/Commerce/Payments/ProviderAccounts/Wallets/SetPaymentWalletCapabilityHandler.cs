using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Wallets;

public sealed class SetPaymentWalletCapabilityHandler
{
    private readonly ITenantPaymentProviderAccountRepository _accountRepository;
    private readonly ITenantPaymentWalletCapabilityRepository _walletRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public SetPaymentWalletCapabilityHandler(
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

    public async Task<PaymentWalletCapabilityResult> HandleAsync(
        SetPaymentWalletCapabilityCommand command,
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

        if (command.WalletType == PaymentWalletType.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "A supported payment wallet type is required.");
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

        var capability =
            await _walletRepository.GetByAccountAndWalletAsync(
                account!.Id,
                command.WalletType,
                cancellationToken);

        var now =
            _timeProvider.GetUtcNow();

        if (capability is null)
        {
            capability =
                TenantPaymentWalletCapability.Create(
                    tenantId,
                    account.Id,
                    command.WalletType,
                    command.Enabled,
                    now,
                    command.ActorUserId);

            await _walletRepository.AddAsync(
                capability,
                cancellationToken);
        }
        else
        {
            if (capability.TenantId != tenantId)
            {
                throw new PaymentProviderAccountNotFoundException();
            }

            capability.SetEnabled(
                command.Enabled,
                now,
                command.ActorUserId);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return PaymentProviderAccountRules.MapWallet(
            capability);
    }
}