using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OFOQ.Market.Application.Common.Files;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class MarketApiFactory :
    WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=127.0.0.1;Port=5432;Database=unused;Username=unused;Password=unused";

    private const string TestRecoveryCodeHmacKey =
        "MDEyMzQ1Njc4OUFCQ0RFRjAxMjM0NTY3ODlBQkNERUY=";

    public MarketApiFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__MarketDatabase",
            TestConnectionString);

        Environment.SetEnvironmentVariable(
            "Authentication__Jwt__Issuer",
            TestAuthenticationConstants.Issuer);

        Environment.SetEnvironmentVariable(
            "Authentication__Jwt__Audience",
            TestAuthenticationConstants.Audience);

        Environment.SetEnvironmentVariable(
            "Authentication__Jwt__SigningKey",
            TestAuthenticationConstants.SigningKey);

        Environment.SetEnvironmentVariable(
            "Authentication__Jwt__AccessTokenMinutes",
            "15");

        Environment.SetEnvironmentVariable(
            "Authentication__Mfa__RecoveryCodeHmacKey",
            TestRecoveryCodeHmacKey);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "Testing");

        builder.ConfigureServices(
            services =>
            {
                // -------------------------------------------------
                // Remove real persistence registrations
                // -------------------------------------------------

                // Tenancy
                services.RemoveAll<
                    ITenantRepository>();

                services.RemoveAll<
                    ITenantMembershipRepository>();

                // Catalog
                services.RemoveAll<
                    ICategoryRepository>();

                services.RemoveAll<
                    IProductRepository>();

                services.RemoveAll<
                    IProductVariantRepository>();

                services.RemoveAll<
                    IProductOptionRepository>();

                services.RemoveAll<
                    IProductOptionValueRepository>();

                services.RemoveAll<
                    IProductVariantOptionValueRepository>();

                // Commerce configuration
                services.RemoveAll<
                    ITenantCommerceVerticalRepository>();

                services.RemoveAll<
                    ITenantCommerceCapabilityOverrideRepository>();

                // Commerce
                services.RemoveAll<
                    ICartRepository>();

                services.RemoveAll<
                    IOrderRepository>();

                services.RemoveAll<
                    IMerchantOrderQueryRepository>();

                services.RemoveAll<
                    IMerchantAnalyticsQueryRepository>();

                services.RemoveAll<
                    IOrderStateLockRepository>();

                services.RemoveAll<
                    ICheckoutLockRepository>();

                services.RemoveAll<
                    IPaymentRepository>();

                services.RemoveAll<
                    IPaymentIntentRepository>();

                services.RemoveAll<
                    ITenantPaymentMethodRepository>();

                services.RemoveAll<
                    ITenantPaymentCapabilityRepository>();

                services.RemoveAll<
                    IPaymentCreationLockRepository>();

                services.RemoveAll<
                    IPaymentStateLockRepository>();

                services.RemoveAll<
                    IPaymentWebhookLockRepository>();

                // Payment provider account persistence
                services.RemoveAll<
                    ITenantPaymentProviderAccountRepository>();

                services.RemoveAll<
                    ITenantPaymentWalletCapabilityRepository>();

                // Payment providers
                services.RemoveAll<
                    IPaymentProvider>();

                services.RemoveAll<
                    IPaymentWebhookProvider>();

                services.RemoveAll<
                    IPaymentProviderCredentialProtector>();

                // Identity
                services.RemoveAll<
                    IUserRepository>();

                services.RemoveAll<
                    IPlatformUserRoleAssignmentRepository>();

                services.RemoveAll<
                    IMerchantVerificationReviewRepository>();

                services.RemoveAll<
                    IMerchantVerificationReviewQueryRepository>();

                services.RemoveAll<
                    IUserMfaRepository>();

                services.RemoveAll<
                    IUserMfaRecoveryCodeRepository>();

                services.RemoveAll<
                    IMfaLoginChallengeRepository>();

                // Infrastructure abstractions
                services.RemoveAll<
                    ITransactionExecutor>();

                services.RemoveAll<
                    IUnitOfWork>();

                services.RemoveAll<
                    IPasswordHasher>();

                services.RemoveAll<
                    IMfaSecretProtector>();

                services.RemoveAll<
                    ITotpService>();

                // -------------------------------------------------
                // Tenancy fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryTenantRepository>();

                services.AddSingleton<
                    ITenantRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryTenantRepository>());

                services.AddSingleton<
                    InMemoryTenantMembershipRepository>();

                services.AddSingleton<
                    ITenantMembershipRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryTenantMembershipRepository>());

                // -------------------------------------------------
                // Category fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryCategoryStore>();

                services.AddScoped<
                    ICategoryRepository,
                    InMemoryCategoryRepository>();

                // -------------------------------------------------
                // Product fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryProductStore>();

                services.AddScoped<
                    IProductRepository,
                    InMemoryProductRepository>();

                services.AddSingleton<
                    InMemoryProductVariantStore>();

                services.AddScoped<
                    IProductVariantRepository,
                    InMemoryProductVariantRepository>();

                // -------------------------------------------------
                // Structured product option fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryProductOptionStore>();

                services.AddScoped<
                    IProductOptionRepository,
                    InMemoryProductOptionRepository>();

                services.AddSingleton<
                    InMemoryProductOptionValueStore>();

                services.AddScoped<
                    IProductOptionValueRepository,
                    InMemoryProductOptionValueRepository>();

                // -------------------------------------------------
                // Variant option assignment fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryProductVariantOptionValueStore>();

                services.AddScoped<
                    IProductVariantOptionValueRepository,
                    InMemoryProductVariantOptionValueRepository>();

                // -------------------------------------------------
                // Commerce configuration fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryTenantCommerceVerticalStore>();

                services.AddScoped<
                    ITenantCommerceVerticalRepository,
                    InMemoryTenantCommerceVerticalRepository>();

                services.AddSingleton<
                    InMemoryTenantCommerceCapabilityOverrideStore>();

                services.AddScoped<
                    ITenantCommerceCapabilityOverrideRepository,
                    InMemoryTenantCommerceCapabilityOverrideRepository>();

                // -------------------------------------------------
                // Cart / order fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryCartStore>();

                services.AddScoped<
                    ICartRepository,
                    InMemoryCartRepository>();

                services.AddSingleton<
                    InMemoryOrderStore>();

                services.AddScoped<
                    IOrderRepository,
                    InMemoryOrderRepository>();

                services.AddScoped<
                    IMerchantOrderQueryRepository,
                    InMemoryMerchantOrderQueryRepository>();

                services.AddScoped<
                    IMerchantAnalyticsQueryRepository,
                    InMemoryMerchantAnalyticsQueryRepository>();

                services.AddScoped<
                    IOrderStateLockRepository,
                    InMemoryOrderStateLockRepository>();

                services.AddScoped<
                    ICheckoutLockRepository,
                    InMemoryCheckoutLockRepository>();

                // -------------------------------------------------
                // Payment fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryPaymentStore>();

                services.AddScoped<
                    IPaymentRepository,
                    InMemoryPaymentRepository>();

                services.AddSingleton<
                    InMemoryPaymentIntentStore>();

                services.AddScoped<
                    IPaymentIntentRepository,
                    InMemoryPaymentIntentRepository>();

                services.AddSingleton<
                    InMemoryTenantPaymentMethodStore>();

                services.AddScoped<
                    ITenantPaymentMethodRepository,
                    InMemoryTenantPaymentMethodRepository>();

                services.AddSingleton<
                    InMemoryTenantPaymentCapabilityStore>();

                services.AddScoped<
                    ITenantPaymentCapabilityRepository,
                    InMemoryTenantPaymentCapabilityRepository>();

                services.AddScoped<
                    IPaymentCreationLockRepository,
                    InMemoryPaymentCreationLockRepository>();

                services.AddScoped<
                    IPaymentStateLockRepository,
                    InMemoryPaymentStateLockRepository>();

                services.AddScoped<
                    IPaymentWebhookLockRepository,
                    InMemoryPaymentWebhookLockRepository>();

                // -------------------------------------------------
                // Payment provider account fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryTenantPaymentProviderAccountStore>();

                services.AddScoped<
                    ITenantPaymentProviderAccountRepository,
                    InMemoryTenantPaymentProviderAccountRepository>();

                services.AddSingleton<
                    InMemoryTenantPaymentWalletCapabilityStore>();

                services.AddScoped<
                    ITenantPaymentWalletCapabilityRepository,
                    InMemoryTenantPaymentWalletCapabilityRepository>();

                services.AddSingleton<
                    FakePaymentProviderCredentialProtector>();

                services.AddSingleton<
                    IPaymentProviderCredentialProtector>(
                        provider =>
                            provider.GetRequiredService<
                                FakePaymentProviderCredentialProtector>());

                // -------------------------------------------------
                // Payment provider fake
                // -------------------------------------------------

                services.AddSingleton<
                    FakePaymentProvider>();

                services.AddSingleton<
                    IPaymentProvider>(
                        provider =>
                            provider.GetRequiredService<
                                FakePaymentProvider>());

                services.AddSingleton<
                    IPaymentWebhookProvider>(
                        provider =>
                            provider.GetRequiredService<
                                FakePaymentProvider>());

                // -------------------------------------------------
                // User / identity fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryUserRepository>();

                services.AddSingleton<
                    IUserRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryUserRepository>());

                services.AddSingleton<
                    InMemoryPlatformUserRoleAssignmentRepository>();

                services.AddSingleton<
                    IPlatformUserRoleAssignmentRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryPlatformUserRoleAssignmentRepository>());

                // -------------------------------------------------
                // Merchant verification review fakes
                // -------------------------------------------------

                services.AddSingleton<
                    InMemoryMerchantVerificationReviewStore>();

                services.AddScoped<
                    InMemoryMerchantVerificationReviewRepository>();

                services.AddScoped<
                    IMerchantVerificationReviewRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryMerchantVerificationReviewRepository>());

                services.AddScoped<
                    IMerchantVerificationReviewQueryRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryMerchantVerificationReviewRepository>());

                services.AddSingleton<
                    InMemoryUserMfaRepository>();

                services.AddSingleton<
                    IUserMfaRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryUserMfaRepository>());

                services.AddSingleton<
                    InMemoryUserMfaRecoveryCodeRepository>();

                services.AddSingleton<
                    IUserMfaRecoveryCodeRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryUserMfaRecoveryCodeRepository>());

                services.AddSingleton<
                    InMemoryMfaLoginChallengeRepository>();

                services.AddSingleton<
                    IMfaLoginChallengeRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryMfaLoginChallengeRepository>());

                // -------------------------------------------------
                // Transaction / security fakes
                // -------------------------------------------------

                services.AddSingleton<
                    FakeTransactionExecutor>();

                services.AddSingleton<
                    ITransactionExecutor>(
                        provider =>
                            provider.GetRequiredService<
                                FakeTransactionExecutor>());

                services.AddSingleton<
                    IPasswordHasher,
                    FakePasswordHasher>();

                services.AddSingleton<
                    IMfaSecretProtector,
                    FakeMfaSecretProtector>();

                services.AddSingleton<
                    ITotpService,
                    FakeTotpService>();

                services.AddSingleton<
                    IUnitOfWork,
                    FakeUnitOfWork>();

                // -------------------------------------------------
                // FINAL KYC TEST OVERRIDES
                //
                // Keep these registrations last. Merchant KYC API
                // tests must never fall back to EF/PostgreSQL.
                // -------------------------------------------------

                services.RemoveAll<
                    IMerchantVerificationProfileRepository>();

                services.RemoveAll<
                    IMerchantVerificationDocumentRepository>();

                services.RemoveAll<
                    IMerchantVerificationDocumentFileRepository>();

                services.RemoveAll<
                    IMerchantVerificationDocumentProtector>();

                services.RemoveAll<
                    IMerchantVerificationPrivateFileStore>();

                services.RemoveAll<
                    InMemoryMerchantVerificationSelfServiceStore>();

                services.RemoveAll<
                    InMemoryMerchantVerificationSelfServiceRepository>();

                services.RemoveAll<
                    FakeMerchantVerificationDocumentProtector>();

                services.RemoveAll<
                    InMemoryMerchantVerificationPrivateFileStore>();

                services.RemoveAll<
                    IUnitOfWork>();

                services.AddSingleton<
                    InMemoryMerchantVerificationSelfServiceStore>();

                services.AddScoped<
                    InMemoryMerchantVerificationSelfServiceRepository>();

                services.AddScoped<
                    IMerchantVerificationProfileRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryMerchantVerificationSelfServiceRepository>());

                services.AddScoped<
                    IMerchantVerificationDocumentRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryMerchantVerificationSelfServiceRepository>());

                services.AddScoped<
                    IMerchantVerificationDocumentFileRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryMerchantVerificationSelfServiceRepository>());

                services.AddSingleton<
                    FakeMerchantVerificationDocumentProtector>();

                services.AddSingleton<
                    IMerchantVerificationDocumentProtector>(
                        provider =>
                            provider.GetRequiredService<
                                FakeMerchantVerificationDocumentProtector>());

                services.AddSingleton<
                    InMemoryMerchantVerificationPrivateFileStore>();

                services.AddSingleton<
                    IMerchantVerificationPrivateFileStore>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryMerchantVerificationPrivateFileStore>());

                services.AddSingleton<
                    IUnitOfWork,
                    FakeUnitOfWork>();
            });
    }
}
