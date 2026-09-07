using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Payments;

public sealed class TenantPaymentProviderAccount :
    AggregateRoot<TenantPaymentProviderAccountId>,
    ITenantDataScoped,
    IAuditable
{
    private const int MaximumDisplayNameLength = 200;
    private const int MaximumProtectedCredentialsLength = 131072;

    private string _providerCode = string.Empty;

    private TenantPaymentProviderAccount()
    {
    }

    private TenantPaymentProviderAccount(
        TenantPaymentProviderAccountId id,
        TenantId tenantId,
        PaymentProviderCode providerCode,
        string displayName,
        PaymentProviderEnvironment environment,
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

        if (providerCode.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider code is required.",
                nameof(providerCode));
        }

        TenantId = tenantId;
        _providerCode = providerCode.Value;
        DisplayName = NormalizeDisplayName(
            displayName);
        Environment = environment;
        IsEnabled = false;
        CredentialsVersion = 0;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public PaymentProviderCode ProviderCode =>
        PaymentProviderCode.Create(
            _providerCode);

    public string DisplayName { get; private set; } =
        string.Empty;

    public PaymentProviderEnvironment Environment { get; private set; }

    public bool IsEnabled { get; private set; }

    public string? ProtectedCredentials { get; private set; }

    public int CredentialsVersion { get; private set; }

    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(
            ProtectedCredentials);

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantPaymentProviderAccount Create(
        TenantId tenantId,
        PaymentProviderCode providerCode,
        string displayName,
        PaymentProviderEnvironment environment,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantPaymentProviderAccount(
            TenantPaymentProviderAccountId.New(),
            tenantId,
            providerCode,
            displayName,
            environment,
            createdAtUtc,
            createdByUserId);
    }

    public void SetProtectedCredentials(
        string protectedCredentials,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(
                protectedCredentials))
        {
            throw new ArgumentException(
                "Protected payment provider credentials are required.",
                nameof(protectedCredentials));
        }

        if (protectedCredentials.Length >
            MaximumProtectedCredentialsLength)
        {
            throw new ArgumentException(
                $"Protected credentials cannot exceed {MaximumProtectedCredentialsLength} characters.",
                nameof(protectedCredentials));
        }

        ProtectedCredentials =
            protectedCredentials;

        checked
        {
            CredentialsVersion++;
        }

        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void SetEnabled(
        bool enabled,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (enabled &&
            !HasCredentials)
        {
            throw new InvalidOperationException(
                "A payment provider account cannot be enabled before credentials are configured.");
        }

        IsEnabled = enabled;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private static string NormalizeDisplayName(
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(
                displayName))
        {
            throw new ArgumentException(
                "Payment provider display name is required.",
                nameof(displayName));
        }

        var normalized =
            displayName.Trim();

        if (normalized.Length >
            MaximumDisplayNameLength)
        {
            throw new ArgumentException(
                $"Payment provider display name cannot exceed {MaximumDisplayNameLength} characters.",
                nameof(displayName));
        }

        return normalized;
    }
}