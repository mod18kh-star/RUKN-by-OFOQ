using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Payments;

public sealed class TenantPaymentWalletCapability :
    AggregateRoot<TenantPaymentWalletCapabilityId>,
    ITenantDataScoped,
    IAuditable
{
    private TenantPaymentWalletCapability()
    {
    }

    private TenantPaymentWalletCapability(
        TenantPaymentWalletCapabilityId id,
        TenantId tenantId,
        TenantPaymentProviderAccountId providerAccountId,
        PaymentWalletType walletType,
        bool isEnabled,
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

        if (providerAccountId.IsEmpty)
        {
            throw new ArgumentException(
                "Payment provider account ID cannot be empty.",
                nameof(providerAccountId));
        }

        if (walletType == PaymentWalletType.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(walletType),
                "A supported payment wallet type is required.");
        }

        TenantId = tenantId;
        ProviderAccountId = providerAccountId;
        WalletType = walletType;
        IsEnabled = isEnabled;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public TenantPaymentProviderAccountId ProviderAccountId { get; private set; }

    public PaymentWalletType WalletType { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantPaymentWalletCapability Create(
        TenantId tenantId,
        TenantPaymentProviderAccountId providerAccountId,
        PaymentWalletType walletType,
        bool isEnabled,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new TenantPaymentWalletCapability(
            TenantPaymentWalletCapabilityId.New(),
            tenantId,
            providerAccountId,
            walletType,
            isEnabled,
            createdAtUtc,
            createdByUserId);
    }

    public void SetEnabled(
        bool enabled,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        IsEnabled = enabled;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }
}