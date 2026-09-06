using OFOQ.Market.Application.Commerce.Payments.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Payments.RetryIntent;

public sealed class RetryPaymentIntentHandler
{
    private readonly IPaymentIntentRepository _paymentIntentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITenantPaymentMethodRepository _paymentMethodRepository;
    private readonly ITenantPaymentCapabilityRepository _capabilityRepository;
    private readonly IPaymentCreationLockRepository _lockRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RetryPaymentIntentHandler(
        IPaymentIntentRepository paymentIntentRepository,
        IPaymentRepository paymentRepository,
        ITenantPaymentMethodRepository paymentMethodRepository,
        ITenantPaymentCapabilityRepository capabilityRepository,
        IPaymentCreationLockRepository lockRepository,
        ICurrentTenant currentTenant,
        ITransactionExecutor transactionExecutor,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _paymentIntentRepository = paymentIntentRepository;
        _paymentRepository = paymentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _capabilityRepository = capabilityRepository;
        _lockRepository = lockRepository;
        _currentTenant = currentTenant;
        _transactionExecutor = transactionExecutor;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PaymentIntentResult> HandleAsync(
        RetryPaymentIntentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureTenantContext();

        if (command.PaymentIntentId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment intent ID cannot be empty.",
                nameof(command));
        }

        if (command.CustomerUserId.IsEmpty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(command));
        }

        var idempotencyKey = PaymentRules.NormalizeIdempotencyKey(
            command.IdempotencyKey);

        var source = await GetOwnedIntentAsync(
            command.PaymentIntentId,
            command.CustomerUserId,
            cancellationToken);

        var payment = await GetOwnedPaymentAsync(
            source,
            command.CustomerUserId,
            cancellationToken);

        var replay = await TryResolveRetryReplayAsync(
            source,
            payment,
            command.CustomerUserId,
            idempotencyKey,
            cancellationToken);

        if (replay is not null)
        {
            return replay;
        }

        return await _transactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                var lockedOrder = await _lockRepository.GetOrderForUpdateAsync(
                    payment.OrderId,
                    command.CustomerUserId,
                    idempotencyKey,
                    transactionCancellationToken);

                PaymentRules.EnsureOrderIsPayable(
                    lockedOrder,
                    command.CustomerUserId);

                var lockedSource = await GetOwnedIntentAsync(
                    command.PaymentIntentId,
                    command.CustomerUserId,
                    transactionCancellationToken);

                var lockedPayment = await GetOwnedPaymentAsync(
                    lockedSource,
                    command.CustomerUserId,
                    transactionCancellationToken);

                PaymentRules.EnsurePaymentCanAcceptAttempt(
                    lockedPayment);

                if (!PaymentRules.IsRetryable(lockedSource.Status))
                {
                    throw new PaymentIntentNotRetryableException();
                }

                var replayInsideTransaction = await TryResolveRetryReplayAsync(
                    lockedSource,
                    lockedPayment,
                    command.CustomerUserId,
                    idempotencyKey,
                    transactionCancellationToken);

                if (replayInsideTransaction is not null)
                {
                    return replayInsideTransaction;
                }

                var method = await _paymentMethodRepository.GetByIdAsync(
                    lockedSource.TenantPaymentMethodId,
                    transactionCancellationToken);

                PaymentRules.EnsureMethodAvailable(
                    method,
                    lockedPayment.Amount,
                    lockedPayment.Currency.Value);

                var capability = await _capabilityRepository.GetAsync(
                    transactionCancellationToken);

                PaymentRules.EnsureElectronicPaymentsAllowed(
                    method!,
                    capability);

                var now = _timeProvider.GetUtcNow();

                var retry = PaymentIntent.Create(
                    _currentTenant.TenantId!.Value,
                    lockedPayment.Id,
                    method!.Id,
                    command.CustomerUserId,
                    lockedPayment.Total,
                    method.Type,
                    method.ProviderCode,
                    now,
                    command.CustomerUserId.Value);

                await _paymentIntentRepository.AddAsync(
                    retry,
                    idempotencyKey,
                    transactionCancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    transactionCancellationToken);

                return PaymentRules.MapIntent(
                    retry,
                    lockedPayment,
                    isIdempotentReplay: false);
            },
            cancellationToken);
    }

    private async Task<PaymentIntent> GetOwnedIntentAsync(
        PaymentIntentId paymentIntentId,
        UserId customerUserId,
        CancellationToken cancellationToken)
    {
        var intent = await _paymentIntentRepository.GetByIdAsync(
            paymentIntentId,
            cancellationToken);

        if (intent is null ||
            intent.CustomerUserId != customerUserId)
        {
            throw new PaymentIntentNotFoundException();
        }

        return intent;
    }

    private async Task<Payment> GetOwnedPaymentAsync(
        PaymentIntent intent,
        UserId customerUserId,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(
            intent.PaymentId,
            cancellationToken);

        if (payment is null ||
            payment.CustomerUserId != customerUserId)
        {
            throw new PaymentIntentNotFoundException();
        }

        return payment;
    }

    private async Task<PaymentIntentResult?> TryResolveRetryReplayAsync(
        PaymentIntent source,
        Payment payment,
        UserId customerUserId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existing = await _paymentIntentRepository
            .GetByCreateIdempotencyKeyAsync(
                customerUserId,
                idempotencyKey,
                cancellationToken);

        if (existing is null)
        {
            return null;
        }

        if (existing.Id == source.Id)
        {
            throw new PaymentIdempotencyConflictException();
        }

        if (existing.PaymentId != payment.Id ||
            existing.TenantPaymentMethodId != source.TenantPaymentMethodId)
        {
            throw new PaymentIdempotencyConflictException();
        }

        var existingPayment = await _paymentRepository.GetByIdAsync(
            existing.PaymentId,
            cancellationToken);

        if (existingPayment is null ||
            existingPayment.OrderId != payment.OrderId)
        {
            throw new PaymentIdempotencyConflictException();
        }

        return PaymentRules.MapIntent(
            existing,
            existingPayment,
            isIdempotentReplay: true);
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
