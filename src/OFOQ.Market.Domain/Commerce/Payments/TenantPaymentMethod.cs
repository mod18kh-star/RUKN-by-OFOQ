using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Payments;

public sealed class TenantPaymentMethod :
    AggregateRoot<TenantPaymentMethodId>,
    ITenantDataScoped,
    IAuditable
{
    private string _providerCode = string.Empty;
    private string _countryCode = string.Empty;
    private string _currencyCode = string.Empty;

    private TenantPaymentMethod()
    {
    }

    private TenantPaymentMethod(
        TenantPaymentMethodId id,
        TenantId tenantId,
        PaymentMethodType type,
        PaymentProviderCode providerCode,
        string displayName,
        CountryCode country,
        CurrencyCode currency,
        decimal? minimumAmount,
        decimal? maximumAmount,
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

        TenantId = tenantId;
        Type = type;
        _providerCode = providerCode.Value;
        DisplayName = NormalizeDisplayName(displayName);
        _countryCode = country.Value;
        _currencyCode = currency.Value;
        ValidateLimits(minimumAmount, maximumAmount);
        MinimumAmount = minimumAmount;
        MaximumAmount = maximumAmount;
        IsEnabled = true;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public PaymentMethodType Type { get; private set; }

    public PaymentProviderCode ProviderCode =>
        PaymentProviderCode.Create(_providerCode);

    public string DisplayName { get; private set; } = string.Empty;

    public CountryCode Country =>
        CountryCode.Create(_countryCode);

    public CurrencyCode Currency =>
        CurrencyCode.Create(_currencyCode);

    public decimal? MinimumAmount { get; private set; }

    public decimal? MaximumAmount { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantPaymentMethod Create(
        TenantId tenantId,
        PaymentMethodType type,
        string providerCode,
        string displayName,
        string countryCode,
        string currencyCode,
        decimal? minimumAmount,
        decimal? maximumAmount,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantPaymentMethod(
            TenantPaymentMethodId.New(),
            tenantId,
            type,
            PaymentProviderCode.Create(providerCode),
            displayName,
            CountryCode.Create(countryCode),
            CurrencyCode.Create(currencyCode),
            minimumAmount,
            maximumAmount,
            createdAtUtc,
            createdByUserId);
    }

    public void Enable(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (IsEnabled)
        {
            return;
        }

        IsEnabled = true;
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    public void Disable(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        IsEnabled = false;
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    public void Rename(
        string displayName,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        DisplayName = NormalizeDisplayName(displayName);
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    public void ChangeLimits(
        decimal? minimumAmount,
        decimal? maximumAmount,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        ValidateLimits(minimumAmount, maximumAmount);
        MinimumAmount = minimumAmount;
        MaximumAmount = maximumAmount;
        MarkUpdated(updatedAtUtc, updatedByUserId);
    }

    public bool SupportsAmount(decimal amount)
    {
        if (amount < 0)
        {
            return false;
        }

        if (MinimumAmount.HasValue &&
            amount < MinimumAmount.Value)
        {
            return false;
        }

        if (MaximumAmount.HasValue &&
            amount > MaximumAmount.Value)
        {
            return false;
        }

        return true;
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private static string NormalizeDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Payment method display name is required.",
                nameof(value));
        }

        var normalized = value.Trim();

        if (normalized.Length > 120)
        {
            throw new ArgumentException(
                "Payment method display name cannot exceed 120 characters.",
                nameof(value));
        }

        return normalized;
    }

    private static void ValidateLimits(
        decimal? minimumAmount,
        decimal? maximumAmount)
    {
        if (minimumAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumAmount),
                "Minimum amount cannot be negative.");
        }

        if (maximumAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAmount),
                "Maximum amount cannot be negative.");
        }

        if (minimumAmount.HasValue &&
            maximumAmount.HasValue &&
            minimumAmount.Value > maximumAmount.Value)
        {
            throw new ArgumentException(
                "Minimum amount cannot exceed maximum amount.");
        }
    }
}
