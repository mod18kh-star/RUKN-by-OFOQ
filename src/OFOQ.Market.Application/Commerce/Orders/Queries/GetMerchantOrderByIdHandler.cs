using OFOQ.Market.Application.Commerce.Orders.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Commerce.Orders.Queries;

public sealed class GetMerchantOrderByIdHandler
{
    private readonly IOrderRepository
        _orderRepository;

    private readonly IPaymentRepository
        _paymentRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetMerchantOrderByIdHandler(
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        IUserRepository userRepository,
        ICurrentTenant currentTenant)
    {
        _orderRepository =
            orderRepository;

        _paymentRepository =
            paymentRepository;

        _userRepository =
            userRepository;

        _currentTenant =
            currentTenant;
    }

    public async Task<MerchantOrderDetailResult?>
        HandleAsync(
            GetMerchantOrderByIdQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        EnsureTenant();

        if (query.OrderId.IsEmpty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(query));
        }

        var order =
            await _orderRepository
                .GetByIdAsync(
                    query.OrderId,
                    cancellationToken);

        if (order is null)
        {
            return null;
        }

        var payment =
            await _paymentRepository
                .GetByOrderIdAsync(
                    order.Id,
                    cancellationToken);

        var customer =
            await _userRepository
                .GetByIdAsync(
                    order.CustomerUserId,
                    cancellationToken);

        return MerchantOrderResultMapper.MapDetail(
            order,
            payment,
            customer?.Email.Value);
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to query a merchant order.");
        }
    }
}