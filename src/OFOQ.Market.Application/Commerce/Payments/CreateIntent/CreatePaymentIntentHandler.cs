using OFOQ.Market.Application.Commerce.Payments.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.CreateIntent;

public sealed class CreatePaymentIntentHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentIntentRepository _paymentIntentRepository;
    private readonly ITenantPaymentMethodRepository _paymentMethodRepository;
    private readonly ITenantPaymentCapabilityRepository _capabilityRepository;
    private readonly IPaymentCreationLockRepository _lockRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CreatePaymentIntentHandler(
        IPaymentRepository paymentRepository,
        IPaymentIntentRepository paymentIntentRepository,
        ITenantPaymentMethodRepository paymentMethodRepository,
        ITenantPaymentCapabilityRepository capabilityRepository,
        IPaymentCreationLockRepository lockRepository,
        ICurrentTenant currentTenant,
        ITransactionExecutor transactionExecutor,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _paymentRepository = paymentRepository;
        _paymentIntentRepository = paymentIntentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _capabilityRepository = capabilityRepository;
        _lockRepository = lockRepository;
        _currentTenant = currentTenant;
        _transactionExecutor = transactionExecutor;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PaymentIntentResult> HandleAsync(
        CreatePaymentIntentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureTenantContext();
        ValidateCommand(command);

        var idempotencyKey = PaymentRules.NormalizeIdempotencyKey(
            command.IdempotencyKey);

        var replay = await TryResolveReplayAsync(
            command,
            idempotencyKey,
            cancellationToken);

        if (replay is not null)
        {
            return replay;
        }

        return await _transactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                var order = await _lockRepository.GetOrderForUpdateAsync(
                    command.OrderId,
                    command.CustomerUserId,
                    idempotencyKey,
                    transactionCancellationToken);

                PaymentRules.EnsureOrderIsPayable(
                    order,
                    command.CustomerUserId);

                var payableOrder = order!;

                var replayInsideTransaction = await TryResolveReplayAsync(
                    command,
                    idempotencyKey,
                    transactionCancellationToken);

                if (replayInsideTransaction is not null)
                {
                    return replayInsideTransaction;
                }

                var payment = await _paymentRepository.GetByOrderIdAsync(
                    command.OrderId,
                    transactionCancellationToken);

                PaymentRules.EnsurePaymentCanAcceptAttempt(payment);

                var method = await _paymentMethodRepository.GetByIdAsync(
                    command.TenantPaymentMethodId,
                    transactionCancellationToken);

                PaymentRules.EnsureMethodAvailable(
                    method,
                    payableOrder.TotalAmount,
                    payableOrder.Currency.Value);

                var capability = await _capabilityRepository.GetAsync(
                    transactionCancellationToken);

                PaymentRules.EnsureElectronicPaymentsAllowed(
                    method!,
                    capability);

                var now = _timeProvider.GetUtcNow();

                if (payment is null)
                {
                    payment = Payment.Create(
                        _currentTenant.TenantId!.Value,
                        payableOrder.Id,
                        command.CustomerUserId,
                        Money.Create(payableOrder.TotalAmount, payableOrder.Currency),
                        now,
                        command.CustomerUserId.Value);

                    await _paymentRepository.AddAsync(
                        payment,
                        transactionCancellationToken);
                }

                var intent = PaymentIntent.Create(
                    _currentTenant.TenantId!.Value,
                    payment.Id,
                    method!.Id,
                    command.CustomerUserId,
                    payment.Total,
                    method.Type,
                    method.ProviderCode,
                    now,
                    command.CustomerUserId.Value);

                await _paymentIntentRepository.AddAsync(
                    intent,
                    idempotencyKey,
                    transactionCancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    transactionCancellationToken);

                return PaymentRules.MapIntent(
                    intent,
                    payment,
                    isIdempotentReplay: false);
            },
            cancellationToken);
    }

    private async Task<PaymentIntentResult?> TryResolveReplayAsync(
        CreatePaymentIntentCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existingIntent = await _paymentIntentRepository
            .GetByCreateIdempotencyKeyAsync(
                command.CustomerUserId,
                idempotencyKey,
                cancellationToken);

        if (existingIntent is null)
        {
            return null;
        }

        var existingPayment = await _paymentRepository.GetByIdAsync(
            existingIntent.PaymentId,
            cancellationToken);

        if (existingPayment is null ||
            existingPayment.OrderId != command.OrderId ||
            existingIntent.TenantPaymentMethodId != command.TenantPaymentMethodId)
        {
            throw new PaymentIdempotencyConflictException();
        }

        return PaymentRules.MapIntent(
            existingIntent,
            existingPayment,
            isIdempotentReplay: true);
    }

    private static void ValidateCommand(CreatePaymentIntentCommand command)
    {
        if (command.OrderId.IsEmpty)
        {
            throw new ArgumentException("Order ID cannot be empty.", nameof(command));
        }

        if (command.CustomerUserId.IsEmpty)
        {
            throw new ArgumentException("Customer user ID cannot be empty.", nameof(command));
        }

        if (command.TenantPaymentMethodId.IsEmpty)
        {
            throw new ArgumentException("Tenant payment method ID cannot be empty.", nameof(command));
        }
    }

    private void EnsureTenantContext()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required.");
        }
    }
}
