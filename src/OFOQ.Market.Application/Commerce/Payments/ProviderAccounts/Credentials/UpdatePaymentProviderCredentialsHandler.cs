using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Credentials;

public sealed class UpdatePaymentProviderCredentialsHandler
{
    private readonly ITenantPaymentProviderAccountRepository _accountRepository;
    private readonly ITenantPaymentWalletCapabilityRepository _walletRepository;
    private readonly IPaymentProviderCredentialProtector _credentialProtector;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdatePaymentProviderCredentialsHandler(
        ITenantPaymentProviderAccountRepository accountRepository,
        ITenantPaymentWalletCapabilityRepository walletRepository,
        IPaymentProviderCredentialProtector credentialProtector,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _accountRepository = accountRepository;
        _walletRepository = walletRepository;
        _credentialProtector = credentialProtector;
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PaymentProviderAccountResult> HandleAsync(
        UpdatePaymentProviderCredentialsCommand command,
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

        var payload =
            PaymentProviderCredentialPayload.Create(
                command.Credentials);

        var protectedCredentials =
            _credentialProtector.Protect(
                tenantId,
                account!.Id,
                payload);

        account.SetProtectedCredentials(
            protectedCredentials,
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