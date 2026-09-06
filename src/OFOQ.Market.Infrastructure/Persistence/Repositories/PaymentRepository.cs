using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Payments;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository :
    IPaymentRepository
{
    private readonly MarketDbContext _dbContext;

    public PaymentRepository(MarketDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Payment?> GetByIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Payments.SingleOrDefaultAsync(
            payment => payment.Id == paymentId,
            cancellationToken);
    }

    public Task<Payment?> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Payments.SingleOrDefaultAsync(
            payment => payment.OrderId == orderId,
            cancellationToken);
    }

    public Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return _dbContext.Payments
            .AddAsync(payment, cancellationToken)
            .AsTask();
    }
}
