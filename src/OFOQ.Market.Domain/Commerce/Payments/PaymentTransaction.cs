using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Payments;

public sealed class PaymentTransaction :
    Entity<PaymentTransactionId>,
    ITenantDataScoped
{
    private string _currencyCode = string.Empty;

    private PaymentTransaction()
    {
    }

    private PaymentTransaction(
        PaymentTransactionId id,
        TenantId tenantId,
        PaymentIntentId paymentIntentId,
        PaymentTransactionType type,
        PaymentIntentStatus resultingStatus,
        Money amount,
        string? providerReference,
        string? externalEventId,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        PaymentIntentId = paymentIntentId;
        Type = type;
        ResultingStatus = resultingStatus;
        Amount = amount.Amount;
        _currencyCode = amount.Currency.Value;
        ProviderReference = NormalizeOptional(providerReference, 200, nameof(providerReference));
        ExternalEventId = NormalizeOptional(externalEventId, 200, nameof(externalEventId));
        CreatedAtUtc = createdAtUtc;
    }

    public TenantId TenantId { get; private set; }

    public PaymentIntentId PaymentIntentId { get; private set; }

    public PaymentTransactionType Type { get; private set; }

    public PaymentIntentStatus ResultingStatus { get; private set; }

    public decimal Amount { get; private set; }

    public CurrencyCode Currency =>
        CurrencyCode.Create(_currencyCode);

    public string? ProviderReference { get; private set; }

    public string? ExternalEventId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal static PaymentTransaction Create(
        TenantId tenantId,
        PaymentIntentId paymentIntentId,
        PaymentTransactionType type,
        PaymentIntentStatus resultingStatus,
        Money amount,
        string? providerReference,
        string? externalEventId,
        DateTimeOffset createdAtUtc)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (paymentIntentId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment intent ID cannot be empty.",
                nameof(paymentIntentId));
        }

        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Transaction amount must be greater than zero.");
        }

        return new PaymentTransaction(
            PaymentTransactionId.New(),
            tenantId,
            paymentIntentId,
            type,
            resultingStatus,
            amount,
            providerReference,
            externalEventId,
            createdAtUtc);
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
