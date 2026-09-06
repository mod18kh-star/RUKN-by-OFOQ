using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Payments;

public sealed class Payment :
    AggregateRoot<PaymentId>,
    ITenantDataScoped,
    IAuditable
{
    private Guid _customerUserId;
    private string _currencyCode = string.Empty;

    private Payment()
    {
    }

    private Payment(
        PaymentId id,
        TenantId tenantId,
        OrderId orderId,
        UserId customerUserId,
        Money amount,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (orderId.IsEmpty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(orderId));
        }

        if (customerUserId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero.");
        }

        TenantId = tenantId;
        OrderId = orderId;
        _customerUserId = customerUserId.Value;
        Amount = amount.Amount;
        _currencyCode = amount.Currency.Value;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public OrderId OrderId { get; private set; }

    public UserId CustomerUserId =>
        UserId.From(_customerUserId);

    public decimal Amount { get; private set; }

    public CurrencyCode Currency =>
        CurrencyCode.Create(_currencyCode);

    public Money Total =>
        Money.Create(Amount, Currency);

    public PaymentStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static Payment Create(
        TenantId tenantId,
        OrderId orderId,
        UserId customerUserId,
        Money amount,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new Payment(
            PaymentId.New(),
            tenantId,
            orderId,
            customerUserId,
            amount,
            createdAtUtc,
            createdByUserId);
    }

    public void MarkSucceeded(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == PaymentStatus.Succeeded)
        {
            return;
        }

        if (Status == PaymentStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "A cancelled payment cannot succeed.");
        }

        Status = PaymentStatus.Succeeded;
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    public void Cancel(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (Status == PaymentStatus.Cancelled)
        {
            return;
        }

        if (Status == PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException(
                "A succeeded payment cannot be cancelled.");
        }

        Status = PaymentStatus.Cancelled;
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }
}
