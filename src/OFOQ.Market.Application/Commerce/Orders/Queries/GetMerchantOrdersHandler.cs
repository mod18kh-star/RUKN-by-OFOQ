using OFOQ.Market.Application.Commerce.Orders.Common;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Application.Commerce.Orders.Queries;

public sealed class GetMerchantOrdersHandler
{
    private readonly IMerchantOrderQueryRepository
        _merchantOrderQueryRepository;

    private readonly IPaymentRepository
        _paymentRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly ICurrentTenant
        _currentTenant;

    public GetMerchantOrdersHandler(
        IMerchantOrderQueryRepository merchantOrderQueryRepository,
        IPaymentRepository paymentRepository,
        IUserRepository userRepository,
        ICurrentTenant currentTenant)
    {
        _merchantOrderQueryRepository =
            merchantOrderQueryRepository;

        _paymentRepository =
            paymentRepository;

        _userRepository =
            userRepository;

        _currentTenant =
            currentTenant;
    }

    public async Task<IReadOnlyList<MerchantOrderSummaryResult>>
        HandleAsync(
            GetMerchantOrdersQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        EnsureTenant();

        if (query.Take <= 0 ||
            query.Take > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.Take),
                "Take must be between 1 and 100.");
        }

        var orders =
            await _merchantOrderQueryRepository
                .GetAsync(
                    query.Status,
                    query.FulfillmentStatus,
                    query.Take,
                    cancellationToken);

        var customerEmails =
            new Dictionary<Guid, string?>();

        var results =
            new List<MerchantOrderSummaryResult>(
                orders.Count);

        foreach (var order in
                 orders)
        {
            var payment =
                await _paymentRepository
                    .GetByOrderIdAsync(
                        order.Id,
                        cancellationToken);

            var customerId =
                order.CustomerUserId.Value;

            if (!customerEmails.TryGetValue(
                    customerId,
                    out var customerEmail))
            {
                var customer =
                    await _userRepository
                        .GetByIdAsync(
                            order.CustomerUserId,
                            cancellationToken);

                customerEmail =
                    customer?.Email.Value;

                customerEmails[
                    customerId] =
                    customerEmail;
            }

            results.Add(
                MerchantOrderResultMapper.MapSummary(
                    order,
                    payment,
                    customerEmail));
        }

        return results;
    }

    private void EnsureTenant()
    {
        if (!_currentTenant.IsAvailable ||
            !_currentTenant.TenantId.HasValue ||
            _currentTenant.TenantId.Value.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "A tenant context is required to query merchant orders.");
        }
    }
}