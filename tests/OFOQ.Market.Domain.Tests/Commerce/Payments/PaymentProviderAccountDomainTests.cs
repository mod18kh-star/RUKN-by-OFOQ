using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Payments;

public sealed class PaymentProviderAccountDomainTests
{
    [Fact]
    public void ProviderAccount_CannotEnableBeforeCredentialsExist()
    {
        var account =
            CreateAccount();

        Assert.Throws<InvalidOperationException>(
            () => account.SetEnabled(
                true,
                DateTimeOffset.UtcNow,
                Guid.NewGuid()));
    }

    [Fact]
    public void ProviderAccount_CredentialsCanBeRotatedAndVersioned()
    {
        var account =
            CreateAccount();

        account.SetProtectedCredentials(
            "protected-v1",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        Assert.True(
            account.HasCredentials);

        Assert.Equal(
            1,
            account.CredentialsVersion);

        account.SetProtectedCredentials(
            "protected-v2",
            DateTimeOffset.UtcNow.AddMinutes(1),
            Guid.NewGuid());

        Assert.Equal(
            2,
            account.CredentialsVersion);
    }

    [Fact]
    public void ProviderAccount_CanEnableAfterCredentialsExist()
    {
        var account =
            CreateAccount();

        account.SetProtectedCredentials(
            "protected-v1",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        account.SetEnabled(
            true,
            DateTimeOffset.UtcNow.AddMinutes(1),
            Guid.NewGuid());

        Assert.True(
            account.IsEnabled);
    }

    [Theory]
    [InlineData(PaymentWalletType.ApplePay)]
    [InlineData(PaymentWalletType.SamsungPay)]
    public void WalletCapability_CanBeConfigured(
        PaymentWalletType walletType)
    {
        var account =
            CreateAccount();

        var capability =
            TenantPaymentWalletCapability.Create(
                account.TenantId,
                account.Id,
                walletType,
                false,
                DateTimeOffset.UtcNow,
                Guid.NewGuid());

        capability.SetEnabled(
            true,
            DateTimeOffset.UtcNow.AddMinutes(1),
            Guid.NewGuid());

        Assert.True(
            capability.IsEnabled);

        Assert.Equal(
            walletType,
            capability.WalletType);
    }

    private static TenantPaymentProviderAccount CreateAccount()
    {
        return TenantPaymentProviderAccount.Create(
            TenantId.New(),
            PaymentProviderCode.Create(
                "provider-a"),
            "Provider A",
            PaymentProviderEnvironment.Sandbox,
            DateTimeOffset.UtcNow,
            Guid.NewGuid());
    }
}