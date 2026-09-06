using OFOQ.Market.Application.Commerce.Payments.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Application.Commerce.Payments.GetAvailableMethods;

public sealed class GetAvailablePaymentMethodsHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITenantPaymentMethodRepository _paymentMethodRepository;
    private readonly ITenantPaymentCapabilityRepository _capabilityRepository;
    private readonly ICurrentTenant _currentTenant;

    public GetAvailablePaymentMethodsHandler(
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        ITenantPaymentMethodRepository paymentMethodRepository,
        ITenantPaymentCapabilityRepository capabilityRepository,
        ICurrentTenant currentTenant)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _capabilityRepository = capabilityRepository;
        _currentTenant = currentTenant;
    }

    public async Task<IReadOnlyList<PaymentMethodResult>> HandleAsync(
        GetAvailablePaymentMethodsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        EnsureTenantContext();

        if (query.OrderId.IsEmpty)
        {
            throw new ArgumentException("Order ID cannot be empty.", nameof(query));
        }

        if (query.CustomerUserId.IsEmpty)
        {
            throw new ArgumentException("Customer user ID cannot be empty.", nameof(query));
        }

        var order = await _orderRepository.GetByIdAsync(
            query.OrderId,
            cancellationToken);

        PaymentRules.EnsureOrderIsPayable(order, query.CustomerUserId);
        var payableOrder = order!;

        var payment = await _paymentRepository.GetByOrderIdAsync(
            query.OrderId,
            cancellationToken);

        PaymentRules.EnsurePaymentCanAcceptAttempt(payment);

        var methods = await _paymentMethodRepository.GetEnabledAsync(
            payableOrder.Currency,
            cancellationToken);

        var capability = await _capabilityRepository.GetAsync(
            cancellationToken);

        return methods
            .Where(method => method.Type != PaymentMethodType.CashOnDelivery)
            .Where(method => method.SupportsAmount(payableOrder.TotalAmount))
            .Where(method =>
                method.Type != PaymentMethodType.Electronic ||
                capability is null ||
                capability.ElectronicPaymentsAllowed)
            .Select(method => new PaymentMethodResult(
                method.Id,
                method.Type.ToString(),
                method.ProviderCode.Value,
                method.DisplayName,
                method.Country.Value,
                method.Currency.Value,
                method.MinimumAmount,
                method.MaximumAmount))
            .ToArray();
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
