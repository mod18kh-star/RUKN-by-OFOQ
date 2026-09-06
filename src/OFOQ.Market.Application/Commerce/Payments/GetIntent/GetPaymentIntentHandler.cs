using OFOQ.Market.Application.Commerce.Payments.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Commerce.Payments.GetIntent;

public sealed class GetPaymentIntentHandler
{
    private readonly IPaymentIntentRepository _paymentIntentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICurrentTenant _currentTenant;

    public GetPaymentIntentHandler(
        IPaymentIntentRepository paymentIntentRepository,
        IPaymentRepository paymentRepository,
        ICurrentTenant currentTenant)
    {
        _paymentIntentRepository = paymentIntentRepository;
        _paymentRepository = paymentRepository;
        _currentTenant = currentTenant;
    }

    public async Task<PaymentIntentResult> HandleAsync(
        GetPaymentIntentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        EnsureTenantContext();

        if (query.PaymentIntentId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment intent ID cannot be empty.",
                nameof(query));
        }

        if (query.CustomerUserId.IsEmpty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(query));
        }

        var intent = await _paymentIntentRepository.GetByIdAsync(
            query.PaymentIntentId,
            cancellationToken);

        if (intent is null ||
            intent.CustomerUserId != query.CustomerUserId)
        {
            throw new PaymentIntentNotFoundException();
        }

        var payment = await _paymentRepository.GetByIdAsync(
            intent.PaymentId,
            cancellationToken);

        if (payment is null ||
            payment.CustomerUserId != query.CustomerUserId)
        {
            throw new PaymentIntentNotFoundException();
        }

        return PaymentRules.MapIntent(
            intent,
            payment,
            isIdempotentReplay: false);
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
