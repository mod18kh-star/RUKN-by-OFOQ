using OFOQ.Market.Application.Commerce.Payments.Common;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Commerce.Payments.ExecuteIntent;

public sealed class ExecutePaymentIntentHandler
{
    private readonly IPaymentIntentRepository _paymentIntentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITenantPaymentMethodRepository _paymentMethodRepository;
    private readonly ITenantPaymentCapabilityRepository _capabilityRepository;
    private readonly IPaymentStateLockRepository _stateLockRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITransactionExecutor _transactionExecutor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReadOnlyCollection<IPaymentProvider> _providers;
    private readonly TimeProvider _timeProvider;

    public ExecutePaymentIntentHandler(
        IPaymentIntentRepository paymentIntentRepository,
        IPaymentRepository paymentRepository,
        ITenantPaymentMethodRepository paymentMethodRepository,
        ITenantPaymentCapabilityRepository capabilityRepository,
        IPaymentStateLockRepository stateLockRepository,
        ICurrentTenant currentTenant,
        ITransactionExecutor transactionExecutor,
        IUnitOfWork unitOfWork,
        IEnumerable<IPaymentProvider> providers,
        TimeProvider timeProvider)
    {
        _paymentIntentRepository = paymentIntentRepository;
        _paymentRepository = paymentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _capabilityRepository = capabilityRepository;
        _stateLockRepository = stateLockRepository;
        _currentTenant = currentTenant;
        _transactionExecutor = transactionExecutor;
        _unitOfWork = unitOfWork;
        _providers = providers.ToArray();
        _timeProvider = timeProvider;
    }

    public async Task<PaymentIntentResult> HandleAsync(
        ExecutePaymentIntentCommand command,
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

        var initialIntent = await GetOwnedIntentAsync(
            command.PaymentIntentId,
            command.CustomerUserId,
            cancellationToken);

        var initialPayment = await GetOwnedPaymentAsync(
            initialIntent,
            command.CustomerUserId,
            cancellationToken);

        if (PaymentRules.IsProviderAttemptActive(
                initialIntent.Status))
        {
            return PaymentRules.MapIntent(
                initialIntent,
                initialPayment,
                isIdempotentReplay: false);
        }

        if (initialIntent.Status == PaymentIntentStatus.Succeeded &&
            initialPayment.Status == PaymentStatus.Succeeded)
        {
            return PaymentRules.MapIntent(
                initialIntent,
                initialPayment,
                isIdempotentReplay: false);
        }

        if (initialIntent.Status != PaymentIntentStatus.Pending)
        {
            throw new PaymentIntentNotExecutableException();
        }

        var provider = ResolveProvider(
            initialIntent.ProviderCode);

        var claim = await _transactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
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
                        command.PaymentIntentId,
                        transactionCancellationToken);

                EnsureOwnedState(
                    lockedOrder,
                    lockedPayment,
                    lockedIntent,
                    command.CustomerUserId);

                if (lockedIntent!.Status == PaymentIntentStatus.Succeeded &&
                    lockedPayment!.Status == PaymentStatus.Succeeded &&
                    lockedOrder!.Status == OrderStatus.Paid)
                {
                    return ExecutionClaim.Replay(
                        PaymentRules.MapIntent(
                            lockedIntent,
                            lockedPayment,
                            isIdempotentReplay: false));
                }

                PaymentRules.EnsureOrderIsPayable(
                    lockedOrder,
                    command.CustomerUserId);

                PaymentRules.EnsurePaymentCanAcceptAttempt(
                    lockedPayment);

                if (PaymentRules.IsProviderAttemptActive(
                        lockedIntent.Status))
                {
                    return ExecutionClaim.Replay(
                        PaymentRules.MapIntent(
                            lockedIntent,
                            lockedPayment!,
                            isIdempotentReplay: false));
                }

                if (lockedIntent.Status != PaymentIntentStatus.Pending)
                {
                    throw new PaymentIntentNotExecutableException();
                }

                var method =
                    await _paymentMethodRepository.GetByIdAsync(
                        lockedIntent.TenantPaymentMethodId,
                        transactionCancellationToken);

                PaymentRules.EnsureMethodAvailable(
                    method,
                    lockedPayment!.Amount,
                    lockedPayment.Currency.Value);

                var capability =
                    await _capabilityRepository.GetAsync(
                        transactionCancellationToken);

                PaymentRules.EnsureElectronicPaymentsAllowed(
                    method!,
                    capability);

                var paymentIntents =
                    await _paymentIntentRepository.GetByPaymentIdAsync(
                        lockedPayment.Id,
                        transactionCancellationToken);

                var hasAnotherActiveIntent =
                    paymentIntents.Any(
                        intent =>
                            intent.Id != lockedIntent.Id &&
                            PaymentRules.IsProviderAttemptActive(
                                intent.Status));

                if (hasAnotherActiveIntent)
                {
                    throw new PaymentAttemptAlreadyActiveException();
                }

                var now = _timeProvider.GetUtcNow();

                lockedIntent.MarkProcessing(
                    lockedIntent.ProviderReference,
                    now,
                    command.CustomerUserId.Value);

                lockedIntent.RecordTransaction(
                    PaymentTransactionType.ProviderRequest,
                    lockedIntent.ProviderReference,
                    externalEventId: null,
                    now);

                await _unitOfWork.SaveChangesAsync(
                    transactionCancellationToken);

                return ExecutionClaim.CallProvider(
                    lockedIntent,
                    lockedPayment,
                    lockedOrder!,
                    method!);
            },
            cancellationToken);

        if (!claim.ShouldCallProvider)
        {
            return claim.ExistingResult!;
        }

        PaymentProviderResult providerResult;

        try
        {
            providerResult = await provider.CreatePaymentAsync(
                new PaymentProviderRequest(
                    _currentTenant.TenantId!.Value,
                    claim.PaymentId,
                    claim.PaymentIntentId,
                    claim.OrderId,
                    command.CustomerUserId,
                    claim.TenantPaymentMethodId,
                    Money.Create(
                        claim.Amount,
                        claim.Currency),
                    CreateProviderIdempotencyKey(
                        claim.PaymentIntentId)),
                cancellationToken);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            throw new PaymentProviderUnavailableException(
                provider.ProviderCode.Value,
                exception);
        }

        ArgumentNullException.ThrowIfNull(
            providerResult);

        return await _transactionExecutor.ExecuteAsync(
            async transactionCancellationToken =>
            {
                var lockedOrder =
                    await _stateLockRepository.GetOrderForUpdateAsync(
                        claim.OrderId,
                        transactionCancellationToken);

                var lockedPayment =
                    await _stateLockRepository.GetPaymentForUpdateAsync(
                        claim.PaymentId,
                        transactionCancellationToken);

                var lockedIntent =
                    await _stateLockRepository.GetIntentForUpdateAsync(
                        claim.PaymentIntentId,
                        transactionCancellationToken);

                EnsureOwnedState(
                    lockedOrder,
                    lockedPayment,
                    lockedIntent,
                    command.CustomerUserId);

                if (lockedIntent!.Status == PaymentIntentStatus.Succeeded &&
                    lockedPayment!.Status == PaymentStatus.Succeeded &&
                    lockedOrder!.Status == OrderStatus.Paid)
                {
                    return PaymentRules.MapIntent(
                        lockedIntent,
                        lockedPayment,
                        isIdempotentReplay: false);
                }

                if (lockedPayment!.Status == PaymentStatus.Succeeded)
                {
                    throw new PaymentAlreadySucceededException();
                }

                if (lockedIntent.Status is
                    PaymentIntentStatus.Failed or
                    PaymentIntentStatus.Cancelled or
                    PaymentIntentStatus.Expired)
                {
                    return PaymentRules.MapIntent(
                        lockedIntent,
                        lockedPayment,
                        isIdempotentReplay: false);
                }

                var now = _timeProvider.GetUtcNow();

                var transactionType =
                    PaymentProviderStateRules.Apply(
                        lockedIntent,
                        providerResult.Status,
                        providerResult.ProviderReference,
                        providerResult.Action,
                        now,
                        command.CustomerUserId.Value);

                if (lockedIntent.Status ==
                    PaymentIntentStatus.Succeeded)
                {
                    lockedPayment.MarkSucceeded(
                        now,
                        command.CustomerUserId.Value);

                    lockedOrder!.MarkPaid(
                        now,
                        command.CustomerUserId.Value);
                }

                lockedIntent.RecordTransaction(
                    transactionType,
                    lockedIntent.ProviderReference,
                    externalEventId: null,
                    now);

                await _unitOfWork.SaveChangesAsync(
                    transactionCancellationToken);

                return PaymentRules.MapIntent(
                    lockedIntent,
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
        var intent =
            await _paymentIntentRepository.GetByIdAsync(
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
        var payment =
            await _paymentRepository.GetByIdAsync(
                intent.PaymentId,
                cancellationToken);

        if (payment is null ||
            payment.CustomerUserId != customerUserId)
        {
            throw new PaymentIntentNotFoundException();
        }

        return payment;
    }

    private static void EnsureOwnedState(
        Order? order,
        Payment? payment,
        PaymentIntent? intent,
        UserId customerUserId)
    {
        if (order is null ||
            payment is null ||
            intent is null ||
            order.CustomerUserId != customerUserId ||
            payment.CustomerUserId != customerUserId ||
            intent.CustomerUserId != customerUserId ||
            payment.OrderId != order.Id ||
            intent.PaymentId != payment.Id)
        {
            throw new PaymentIntentNotFoundException();
        }
    }

    private IPaymentProvider ResolveProvider(
        PaymentProviderCode providerCode)
    {
        var matches = _providers
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

    private static string CreateProviderIdempotencyKey(
        PaymentIntentId paymentIntentId)
    {
        return $"ofoq-payment-intent:{paymentIntentId.Value:N}";
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

    private sealed record ExecutionClaim(
        bool ShouldCallProvider,
        PaymentIntentResult? ExistingResult,
        PaymentIntentId PaymentIntentId,
        PaymentId PaymentId,
        OrderId OrderId,
        TenantPaymentMethodId TenantPaymentMethodId,
        decimal Amount,
        CurrencyCode Currency)
    {
        public static ExecutionClaim Replay(
            PaymentIntentResult result)
        {
            return new ExecutionClaim(
                false,
                result,
                result.PaymentIntentId,
                result.PaymentId,
                result.OrderId,
                result.TenantPaymentMethodId,
                result.Amount,
                CurrencyCode.Create(
                    result.Currency));
        }

        public static ExecutionClaim CallProvider(
            PaymentIntent intent,
            Payment payment,
            Order order,
            TenantPaymentMethod method)
        {
            return new ExecutionClaim(
                true,
                null,
                intent.Id,
                payment.Id,
                order.Id,
                method.Id,
                payment.Amount,
                payment.Currency);
        }
    }
}