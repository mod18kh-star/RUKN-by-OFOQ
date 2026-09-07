using OFOQ.Market.Application.Commerce.Payments.Common;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.ProcessWebhook;

public sealed class ProcessPaymentWebhookHandler
{
    private const int MaximumExternalEventIdLength = 200;

    private readonly IPaymentIntentRepository _paymentIntentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITenantPaymentMethodRepository _paymentMethodRepository;
    private readonly IPaymentStateLockRepository _stateLockRepository;
    private readonly IPaymentWebhookLockRepository _webhookLockRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReadOnlyCollection<IPaymentWebhookProvider> _webhookProviders;
    private readonly TimeProvider _timeProvider;

    public ProcessPaymentWebhookHandler(
        IPaymentIntentRepository paymentIntentRepository,
        IPaymentRepository paymentRepository,
        ITenantPaymentMethodRepository paymentMethodRepository,
        IPaymentStateLockRepository stateLockRepository,
        IPaymentWebhookLockRepository webhookLockRepository,
        ICurrentTenant currentTenant,
        ITransactionExecutor transactionExecutor,
        IUnitOfWork unitOfWork,
        IEnumerable<IPaymentWebhookProvider> webhookProviders,
        TimeProvider timeProvider)
    {
        _paymentIntentRepository = paymentIntentRepository;
        _paymentRepository = paymentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _stateLockRepository = stateLockRepository;
        _webhookLockRepository = webhookLockRepository;
        _currentTenant = currentTenant;
        _transactionExecutor = transactionExecutor;
        _unitOfWork = unitOfWork;
        _webhookProviders = webhookProviders.ToArray();
        _timeProvider = timeProvider;
    }

    public async Task<PaymentWebhookProcessResult> HandleAsync(
        ProcessPaymentWebhookCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureTenantContext();

        if (command.TenantPaymentMethodId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant payment method ID cannot be empty.",
                nameof(command));
        }

        ArgumentNullException.ThrowIfNull(command.RawBody);
        ArgumentNullException.ThrowIfNull(command.Headers);

        var method = await _paymentMethodRepository.GetByIdAsync(
            command.TenantPaymentMethodId,
            cancellationToken);

        if (method is null)
        {
            throw new PaymentWebhookTargetNotFoundException();
        }

        var provider = ResolveWebhookProvider(
            method.ProviderCode);

        var webhook = await provider.ParseAndValidateWebhookAsync(
            new PaymentWebhookRequest(
                _currentTenant.TenantId!.Value,
                method.Id,
                command.RawBody,
                command.Headers),
            cancellationToken);

        ArgumentNullException.ThrowIfNull(webhook);

        if (!webhook.IsAuthentic)
        {
            throw new PaymentWebhookSignatureInvalidException();
        }

        var externalEventId = NormalizeExternalEventId(
            webhook.ExternalEventId);

        ValidateWebhookTarget(
            webhook);

        var initialIntent = await ResolveIntentAsync(
            method,
            webhook,
            cancellationToken);

        ValidateWebhookAmount(
            initialIntent,
            webhook);

        var initialPayment = await _paymentRepository.GetByIdAsync(
            initialIntent.PaymentId,
            cancellationToken);

        if (initialPayment is null)
        {
            throw new PaymentWebhookTargetNotFoundException();
        }

        return await _transactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                await _webhookLockRepository.AcquireAsync(
                    method.Id,
                    externalEventId,
                    transactionCancellationToken);

                var lockedOrder =
                    await _stateLockRepository.GetOrderForUpdateAsync(
                        initialPayment.OrderId,
                        transactionCancellationToken);

                var lockedPayment =
                    await _stateLockRepository.GetPaymentForUpdateAsync(
                        initialPayment.Id,
                        transactionCancellationToken);

                var lockedIntent =
                    await _stateLockRepository.GetIntentForUpdateAsync(
                        initialIntent.Id,
                        transactionCancellationToken);

                EnsureStateMatches(
                    method,
                    lockedOrder,
                    lockedPayment,
                    lockedIntent);

                var order = lockedOrder!;
                var payment = lockedPayment!;
                var intent = lockedIntent!;

                if (intent.Transactions.Any(
                        transaction =>
                            string.Equals(
                                transaction.ExternalEventId,
                                externalEventId,
                                StringComparison.Ordinal)))
                {
                    return MapResult(
                        intent,
                        payment,
                        order,
                        duplicate: true,
                        applied: false);
                }

                ValidateWebhookAmount(
                    intent,
                    webhook);

                var now = _timeProvider.GetUtcNow();

                if (payment.Status == PaymentStatus.Succeeded ||
                    order.Status == OrderStatus.Paid)
                {
                    intent.RecordTransaction(
                        PaymentTransactionType.ProviderConfirmation,
                        webhook.ProviderReference ??
                        intent.ProviderReference,
                        externalEventId,
                        now);

                    await _unitOfWork.SaveChangesAsync(
                        transactionCancellationToken);

                    return MapResult(
                        intent,
                        payment,
                        order,
                        duplicate: false,
                        applied: false);
                }

                if (intent.Status is
                    PaymentIntentStatus.Succeeded or
                    PaymentIntentStatus.Failed or
                    PaymentIntentStatus.Cancelled or
                    PaymentIntentStatus.Expired)
                {
                    intent.RecordTransaction(
                        TransactionTypeFor(
                            webhook.Status),
                        webhook.ProviderReference ??
                        intent.ProviderReference,
                        externalEventId,
                        now);

                    await _unitOfWork.SaveChangesAsync(
                        transactionCancellationToken);

                    return MapResult(
                        intent,
                        payment,
                        order,
                        duplicate: false,
                        applied: false);
                }

                var effectiveProviderReference =
                    webhook.ProviderReference ??
                    intent.ProviderReference;

                var transactionType =
                    PaymentProviderStateRules.Apply(
                        intent,
                        webhook.Status,
                        effectiveProviderReference,
                        webhook.Action,
                        now,
                        updatedByUserId: null);

                if (intent.Status ==
                    PaymentIntentStatus.Succeeded)
                {
                    payment.MarkSucceeded(
                        now,
                        updatedByUserId: null);

                    order.MarkPaid(
                        now,
                        updatedByUserId: null);
                }

                intent.RecordTransaction(
                    transactionType,
                    intent.ProviderReference,
                    externalEventId,
                    now);

                await _unitOfWork.SaveChangesAsync(
                    transactionCancellationToken);

                return MapResult(
                    intent,
                    payment,
                    order,
                    duplicate: false,
                    applied: true);
            },
            cancellationToken);
    }

    private async Task<PaymentIntent> ResolveIntentAsync(
        TenantPaymentMethod method,
        PaymentWebhookResult webhook,
        CancellationToken cancellationToken)
    {
        PaymentIntent? intent;

        if (webhook.PaymentIntentId.HasValue &&
            !webhook.PaymentIntentId.Value.IsEmpty)
        {
            intent =
                await _paymentIntentRepository.GetByIdAsync(
                    webhook.PaymentIntentId.Value,
                    cancellationToken);
        }
        else
        {
            intent =
                await _paymentIntentRepository.GetByProviderReferenceAsync(
                    method.Id,
                    webhook.ProviderReference!,
                    cancellationToken);
        }

        if (intent is null ||
            intent.TenantPaymentMethodId != method.Id ||
            intent.ProviderCode != method.ProviderCode)
        {
            throw new PaymentWebhookTargetNotFoundException();
        }

        return intent;
    }

    private static void EnsureStateMatches(
        TenantPaymentMethod method,
        Order? order,
        Payment? payment,
        PaymentIntent? intent)
    {
        if (order is null ||
            payment is null ||
            intent is null ||
            payment.OrderId != order.Id ||
            intent.PaymentId != payment.Id ||
            intent.TenantPaymentMethodId != method.Id ||
            intent.ProviderCode != method.ProviderCode)
        {
            throw new PaymentWebhookTargetNotFoundException();
        }
    }

    private static void ValidateWebhookTarget(
        PaymentWebhookResult webhook)
    {
        var hasIntentId =
            webhook.PaymentIntentId.HasValue &&
            !webhook.PaymentIntentId.Value.IsEmpty;

        var hasProviderReference =
            !string.IsNullOrWhiteSpace(
                webhook.ProviderReference);

        if (!hasIntentId &&
            !hasProviderReference)
        {
            throw new PaymentWebhookPayloadInvalidException(
                "The webhook must identify a payment intent or provider reference.");
        }
    }

    private static void ValidateWebhookAmount(
        PaymentIntent intent,
        PaymentWebhookResult webhook)
    {
        if (webhook.Amount.HasValue &&
            webhook.Amount.Value != intent.Amount)
        {
            throw new PaymentWebhookAmountMismatchException();
        }

        if (!string.IsNullOrWhiteSpace(webhook.Currency) &&
            !string.Equals(
                webhook.Currency.Trim(),
                intent.Currency.Value,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new PaymentWebhookAmountMismatchException();
        }
    }

    private IPaymentWebhookProvider ResolveWebhookProvider(
        PaymentProviderCode providerCode)
    {
        var matches = _webhookProviders
            .Where(
                provider =>
                    provider.ProviderCode == providerCode)
            .Take(2)
            .ToArray();

        if (matches.Length != 1)
        {
            throw new PaymentProviderNotConfiguredException(
                providerCode.Value);
        }

        return matches[0];
    }

    private static string NormalizeExternalEventId(
        string externalEventId)
    {
        if (string.IsNullOrWhiteSpace(externalEventId))
        {
            throw new PaymentWebhookPayloadInvalidException(
                "The webhook external event ID is required.");
        }

        var normalized =
            externalEventId.Trim();

        if (normalized.Length >
            MaximumExternalEventIdLength)
        {
            throw new PaymentWebhookPayloadInvalidException(
                $"The webhook external event ID cannot exceed {MaximumExternalEventIdLength} characters.");
        }

        return normalized;
    }

    private static PaymentTransactionType TransactionTypeFor(
        PaymentIntentStatus status)
    {
        return status switch
        {
            PaymentIntentStatus.Failed =>
                PaymentTransactionType.Failure,

            PaymentIntentStatus.Cancelled =>
                PaymentTransactionType.Cancellation,

            PaymentIntentStatus.Expired =>
                PaymentTransactionType.Expiration,

            _ =>
                PaymentTransactionType.ProviderConfirmation
        };
    }

    private static PaymentWebhookProcessResult MapResult(
        PaymentIntent intent,
        Payment payment,
        Order order,
        bool duplicate,
        bool applied)
    {
        return new PaymentWebhookProcessResult(
            intent.Id,
            payment.Id,
            order.Id,
            intent.Status.ToString(),
            duplicate,
            applied);
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