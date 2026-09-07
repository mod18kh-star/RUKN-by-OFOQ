using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Common;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Create;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Credentials;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.State;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Wallets;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tests.Commerce.Payments;

public sealed class PaymentProviderAccountHandlersTests
{
    [Fact]
    public async Task CreateAccount_CreatesDisabledAccountWithoutCredentials()
    {
        var setup =
            CreateSetup();

        var handler =
            new CreatePaymentProviderAccountHandler(
                setup.Accounts,
                setup.CurrentTenant,
                setup.UnitOfWork,
                setup.TimeProvider);

        var result =
            await handler.HandleAsync(
                new CreatePaymentProviderAccountCommand(
                    "provider-a",
                    "Provider A",
                    PaymentProviderEnvironment.Sandbox,
                    setup.ActorUserId));

        Assert.Equal(
            "provider-a",
            result.ProviderCode);

        Assert.False(
            result.IsEnabled);

        Assert.False(
            result.HasCredentials);

        Assert.Equal(
            0,
            result.CredentialsVersion);
    }

    [Fact]
    public async Task CreateAccount_DuplicateProviderAndEnvironment_IsRejected()
    {
        var setup =
            CreateSetup();

        var handler =
            new CreatePaymentProviderAccountHandler(
                setup.Accounts,
                setup.CurrentTenant,
                setup.UnitOfWork,
                setup.TimeProvider);

        var command =
            new CreatePaymentProviderAccountCommand(
                "provider-a",
                "Provider A",
                PaymentProviderEnvironment.Production,
                setup.ActorUserId);

        await handler.HandleAsync(
            command);

        await Assert.ThrowsAsync<
            PaymentProviderAccountAlreadyExistsException>(
            () => handler.HandleAsync(
                command));
    }

    [Fact]
    public async Task Credentials_AreProtectedAndNeverReturnedByResult()
    {
        var setup =
            CreateSetup();

        var account =
            TenantPaymentProviderAccount.Create(
                setup.TenantId,
                PaymentProviderCode.Create(
                    "provider-a"),
                "Provider A",
                PaymentProviderEnvironment.Production,
                setup.Now,
                setup.ActorUserId);

        setup.Accounts.Items.Add(
            account);

        var handler =
            new UpdatePaymentProviderCredentialsHandler(
                setup.Accounts,
                setup.Wallets,
                setup.Protector,
                setup.CurrentTenant,
                setup.UnitOfWork,
                setup.TimeProvider);

        const string secret =
            "super-secret-value";

        var result =
            await handler.HandleAsync(
                new UpdatePaymentProviderCredentialsCommand(
                    account.Id,
                    new Dictionary<string, string>
                    {
                        ["api_key"] = secret
                    },
                    setup.ActorUserId));

        Assert.True(
            result.HasCredentials);

        Assert.Equal(
            1,
            result.CredentialsVersion);

        Assert.NotEqual(
            secret,
            account.ProtectedCredentials);

        Assert.DoesNotContain(
            secret,
            result.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Account_CanBeEnabledAfterCredentialsAreConfigured()
    {
        var setup =
            CreateSetup();

        var account =
            CreateConfiguredAccount(
                setup,
                "provider-a");

        setup.Accounts.Items.Add(
            account);

        var handler =
            new SetPaymentProviderAccountStateHandler(
                setup.Accounts,
                setup.Wallets,
                setup.CurrentTenant,
                setup.UnitOfWork,
                setup.TimeProvider);

        var result =
            await handler.HandleAsync(
                new SetPaymentProviderAccountStateCommand(
                    account.Id,
                    true,
                    setup.ActorUserId));

        Assert.True(
            result.IsEnabled);
    }

    [Theory]
    [InlineData(PaymentWalletType.ApplePay)]
    [InlineData(PaymentWalletType.SamsungPay)]
    public async Task WalletCapability_CanBeEnabled(
        PaymentWalletType walletType)
    {
        var setup =
            CreateSetup();

        var account =
            CreateConfiguredAccount(
                setup,
                "provider-a");

        setup.Accounts.Items.Add(
            account);

        var handler =
            new SetPaymentWalletCapabilityHandler(
                setup.Accounts,
                setup.Wallets,
                setup.CurrentTenant,
                setup.UnitOfWork,
                setup.TimeProvider);

        var result =
            await handler.HandleAsync(
                new SetPaymentWalletCapabilityCommand(
                    account.Id,
                    walletType,
                    true,
                    setup.ActorUserId));

        Assert.True(
            result.IsEnabled);

        Assert.Equal(
            walletType.ToString(),
            result.WalletType);
    }

    private static TestSetup CreateSetup()
    {
        var tenantId =
            TenantId.New();

        var now =
            new DateTimeOffset(
                2026,
                9,
                7,
                20,
                0,
                0,
                TimeSpan.Zero);

        return new TestSetup(
            tenantId,
            Guid.NewGuid(),
            now,
            new FakeCurrentTenant(
                tenantId),
            new FakeAccountRepository(),
            new FakeWalletRepository(),
            new FakeCredentialProtector(),
            new FakeUnitOfWork(),
            new FixedTimeProvider(
                now));
    }

    private static TenantPaymentProviderAccount CreateConfiguredAccount(
        TestSetup setup,
        string providerCode)
    {
        var account =
            TenantPaymentProviderAccount.Create(
                setup.TenantId,
                PaymentProviderCode.Create(
                    providerCode),
                providerCode,
                PaymentProviderEnvironment.Production,
                setup.Now,
                setup.ActorUserId);

        account.SetProtectedCredentials(
            "protected:test",
            setup.Now,
            setup.ActorUserId);

        return account;
    }

    private sealed record TestSetup(
        TenantId TenantId,
        Guid ActorUserId,
        DateTimeOffset Now,
        FakeCurrentTenant CurrentTenant,
        FakeAccountRepository Accounts,
        FakeWalletRepository Wallets,
        FakeCredentialProtector Protector,
        FakeUnitOfWork UnitOfWork,
        FixedTimeProvider TimeProvider);

    private sealed class FakeCurrentTenant :
        ICurrentTenant
    {
        public FakeCurrentTenant(
            TenantId tenantId)
        {
            TenantId = tenantId;
        }

        public TenantId? TenantId { get; }
    }

    private sealed class FakeAccountRepository :
        ITenantPaymentProviderAccountRepository
    {
        public List<TenantPaymentProviderAccount> Items { get; } =
            [];

        public Task<TenantPaymentProviderAccount?> GetByIdAsync(
            TenantPaymentProviderAccountId accountId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item =>
                        item.Id == accountId));
        }

        public Task<TenantPaymentProviderAccount?> GetByProviderAndEnvironmentAsync(
            PaymentProviderCode providerCode,
            PaymentProviderEnvironment environment,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item =>
                        item.ProviderCode == providerCode &&
                        item.Environment == environment));
        }

        public Task<TenantPaymentProviderAccount?> GetEnabledByProviderAsync(
            PaymentProviderCode providerCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item =>
                        item.ProviderCode == providerCode &&
                        item.IsEnabled));
        }

        public Task<IReadOnlyList<TenantPaymentProviderAccount>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TenantPaymentProviderAccount> result =
                Items.ToArray();

            return Task.FromResult(
                result);
        }

        public Task AddAsync(
            TenantPaymentProviderAccount account,
            CancellationToken cancellationToken = default)
        {
            Items.Add(
                account);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeWalletRepository :
        ITenantPaymentWalletCapabilityRepository
    {
        public List<TenantPaymentWalletCapability> Items { get; } =
            [];

        public Task<TenantPaymentWalletCapability?> GetByIdAsync(
            TenantPaymentWalletCapabilityId capabilityId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item =>
                        item.Id == capabilityId));
        }

        public Task<TenantPaymentWalletCapability?> GetByAccountAndWalletAsync(
            TenantPaymentProviderAccountId providerAccountId,
            PaymentWalletType walletType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Items.SingleOrDefault(
                    item =>
                        item.ProviderAccountId == providerAccountId &&
                        item.WalletType == walletType));
        }

        public Task<IReadOnlyList<TenantPaymentWalletCapability>> GetByAccountAsync(
            TenantPaymentProviderAccountId providerAccountId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TenantPaymentWalletCapability> result =
                Items
                    .Where(
                        item =>
                            item.ProviderAccountId == providerAccountId)
                    .ToArray();

            return Task.FromResult(
                result);
        }

        public Task AddAsync(
            TenantPaymentWalletCapability capability,
            CancellationToken cancellationToken = default)
        {
            Items.Add(
                capability);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeCredentialProtector :
        IPaymentProviderCredentialProtector
    {
        public string Protect(
            TenantId tenantId,
            TenantPaymentProviderAccountId accountId,
            PaymentProviderCredentialPayload payload)
        {
            return $"protected:{tenantId.Value:N}:{accountId.Value:N}";
        }

        public PaymentProviderCredentialPayload Unprotect(
            TenantId tenantId,
            TenantPaymentProviderAccountId accountId,
            string protectedPayload)
        {
            return PaymentProviderCredentialPayload.Create(
                new Dictionary<string, string>
                {
                    ["test"] = "test"
                });
        }
    }

    private sealed class FakeUnitOfWork :
        IUnitOfWork
    {
        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                1);
        }
    }

    private sealed class FixedTimeProvider :
        TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(
            DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}