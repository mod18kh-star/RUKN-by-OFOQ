using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Create;

public sealed class CreatePaymentProviderAccountHandler
{
    private readonly ITenantPaymentProviderAccountRepository _accountRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreatePaymentProviderAccountHandler(
        ITenantPaymentProviderAccountRepository accountRepository,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _accountRepository = accountRepository;
        _currentTenant = currentTenant;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PaymentProviderAccountResult> HandleAsync(
        CreatePaymentProviderAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (command.ActorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Actor user ID cannot be empty.",
                nameof(command));
        }

        var tenantId =
            PaymentProviderAccountRules.GetRequiredTenantId(
                _currentTenant);

        var providerCode =
            PaymentProviderCode.Create(
                command.ProviderCode);

        var existing =
            await _accountRepository.GetByProviderAndEnvironmentAsync(
                providerCode,
                command.Environment,
                cancellationToken);

        if (existing is not null)
        {
            throw new PaymentProviderAccountAlreadyExistsException();
        }

        var account =
            TenantPaymentProviderAccount.Create(
                tenantId,
                providerCode,
                command.DisplayName,
                command.Environment,
                _timeProvider.GetUtcNow(),
                command.ActorUserId);

        await _accountRepository.AddAsync(
            account,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return PaymentProviderAccountRules.MapAccount(
            account,
            []);
    }
}