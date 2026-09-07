using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Payments;

public sealed class PaymentIntent :
    AggregateRoot<PaymentIntentId>,
    ITenantDataScoped,
    IAuditable
{
    private readonly List<PaymentTransaction> _transactions = [];

    private Guid _customerUserId;

    private string _currencyCode = string.Empty;

    private string _providerCode = string.Empty;


    private PaymentIntent()
    {
    }


    private PaymentIntent(
        PaymentIntentId id,
        TenantId tenantId,
        PaymentId paymentId,
        TenantPaymentMethodId tenantPaymentMethodId,
        UserId customerUserId,
        Money amount,
        PaymentMethodType methodType,
        PaymentProviderCode providerCode,
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

        if (paymentId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment ID cannot be empty.",
                nameof(paymentId));
        }

        if (tenantPaymentMethodId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant payment method ID cannot be empty.",
                nameof(tenantPaymentMethodId));
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
                "Payment intent amount must be greater than zero.");
        }

        if (providerCode.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider code is required.",
                nameof(providerCode));
        }

        TenantId = tenantId;

        PaymentId = paymentId;

        TenantPaymentMethodId = tenantPaymentMethodId;

        _customerUserId = customerUserId.Value;

        Amount = amount.Amount;

        _currencyCode = amount.Currency.Value;

        MethodType = methodType;

        _providerCode = providerCode.Value;

        Status = PaymentIntentStatus.Pending;

        CreatedAtUtc = createdAtUtc;

        CreatedByUserId = createdByUserId;
    }


    public TenantId TenantId { get; private set; }

    public PaymentId PaymentId { get; private set; }

    public TenantPaymentMethodId TenantPaymentMethodId { get; private set; }

    public UserId CustomerUserId =>
        UserId.From(
            _customerUserId);


    public decimal Amount { get; private set; }


    public CurrencyCode Currency =>
        CurrencyCode.Create(
            _currencyCode);


    public Money Total =>
        Money.Create(
            Amount,
            Currency);


    public PaymentMethodType MethodType { get; private set; }


    public PaymentProviderCode ProviderCode =>
        PaymentProviderCode.Create(
            _providerCode);


    public PaymentIntentStatus Status { get; private set; }


    public PaymentIntentActionType? ActionType { get; private set; }


    public string? ActionValue { get; private set; }


    public string? ProviderReference { get; private set; }


    public IReadOnlyCollection<PaymentTransaction> Transactions =>
        _transactions.AsReadOnly();


    public DateTimeOffset CreatedAtUtc { get; private set; }


    public Guid? CreatedByUserId { get; private set; }


    public DateTimeOffset? UpdatedAtUtc { get; private set; }


    public Guid? UpdatedByUserId { get; private set; }


    public static PaymentIntent Create(
        TenantId tenantId,
        PaymentId paymentId,
        TenantPaymentMethodId tenantPaymentMethodId,
        UserId customerUserId,
        Money amount,
        PaymentMethodType methodType,
        PaymentProviderCode providerCode,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new PaymentIntent(
            PaymentIntentId.New(),
            tenantId,
            paymentId,
            tenantPaymentMethodId,
            customerUserId,
            amount,
            methodType,
            providerCode,
            createdAtUtc,
            createdByUserId);
    }


    public void SetProviderAction(
        PaymentIntentActionType actionType,
        string actionValue,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(actionValue))
        {
            throw new ArgumentException(
                "Action value is required.",
                nameof(actionValue));
        }

        ActionType = actionType;

        ActionValue = actionValue.Trim();

        UpdatedAtUtc = updatedAtUtc;

        UpdatedByUserId = updatedByUserId;
    }


    public PaymentTransaction RecordTransaction(
        PaymentTransactionType type,
        string? providerReference,
        string? externalEventId,
        DateTimeOffset createdAtUtc)
    {
        var transaction =
            PaymentTransaction.Create(
                TenantId,
                Id,
                type,
                Status,
                Total,
                providerReference,
                externalEventId,
                createdAtUtc);

        _transactions.Add(transaction);

        return transaction;
    }


    public void MarkRequiresAction(
        string? providerReference,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        TransitionTo(
            PaymentIntentStatus.RequiresAction,
            providerReference,
            updatedAtUtc,
            updatedByUserId);
    }


    public void MarkProcessing(
        string? providerReference,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        TransitionTo(
            PaymentIntentStatus.Processing,
            providerReference,
            updatedAtUtc,
            updatedByUserId);
    }


    public void MarkSucceeded(
        string providerReference,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        TransitionTo(
            PaymentIntentStatus.Succeeded,
            providerReference,
            updatedAtUtc,
            updatedByUserId);
    }


    public void MarkFailed(
        string? providerReference,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        TransitionTo(
            PaymentIntentStatus.Failed,
            providerReference,
            updatedAtUtc,
            updatedByUserId);
    }


    public void Cancel(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        TransitionTo(
            PaymentIntentStatus.Cancelled,
            ProviderReference,
            updatedAtUtc,
            updatedByUserId);
    }


    public void Expire(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        TransitionTo(
            PaymentIntentStatus.Expired,
            ProviderReference,
            updatedAtUtc,
            updatedByUserId);
    }


    private void TransitionTo(
        PaymentIntentStatus next,
        string? providerReference,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        if (!CanTransition(
                Status,
                next))
        {
            throw new InvalidOperationException(
                $"Payment intent cannot transition from {Status} to {next}.");
        }

        Status = next;

        if (!string.IsNullOrWhiteSpace(providerReference))
        {
            ProviderReference =
                providerReference.Trim();
        }

        if (next != PaymentIntentStatus.RequiresAction)
        {
            ActionType = null;

            ActionValue = null;
        }

        UpdatedAtUtc = updatedAtUtc;

        UpdatedByUserId = updatedByUserId;
    }


    private static bool CanTransition(
        PaymentIntentStatus current,
        PaymentIntentStatus next)
    {
        return current switch
        {
            PaymentIntentStatus.Pending =>
                next is
                    PaymentIntentStatus.RequiresAction or
                    PaymentIntentStatus.Processing or
                    PaymentIntentStatus.Succeeded or
                    PaymentIntentStatus.Failed or
                    PaymentIntentStatus.Cancelled or
                    PaymentIntentStatus.Expired,

            PaymentIntentStatus.RequiresAction =>
                next is
                    PaymentIntentStatus.RequiresAction or
                    PaymentIntentStatus.Processing or
                    PaymentIntentStatus.Succeeded or
                    PaymentIntentStatus.Failed or
                    PaymentIntentStatus.Cancelled or
                    PaymentIntentStatus.Expired,

            PaymentIntentStatus.Processing =>
                next is
                    PaymentIntentStatus.Processing or
                    PaymentIntentStatus.RequiresAction or
                    PaymentIntentStatus.Succeeded or
                    PaymentIntentStatus.Failed or
                    PaymentIntentStatus.Cancelled or
                    PaymentIntentStatus.Expired,

            PaymentIntentStatus.Succeeded => false,

            PaymentIntentStatus.Failed => false,

            PaymentIntentStatus.Cancelled => false,

            PaymentIntentStatus.Expired => false,

            _ => false
        };
    }
}