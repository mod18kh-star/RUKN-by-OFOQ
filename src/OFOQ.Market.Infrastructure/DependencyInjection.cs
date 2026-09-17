using OFOQ.Market.Infrastructure.Files.Verification;
using OFOQ.Market.Application.Common.Files;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Application.Common.Payments;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;
using OFOQ.Market.Infrastructure.Security;
using OFOQ.Market.Infrastructure.Security.Payments;
using OFOQ.Market.Infrastructure.Security.Verification;

namespace OFOQ.Market.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        services.AddDbContext<MarketDbContext>(
            options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay:
                                TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                    });
            });

        services.AddDataProtection()
            .SetApplicationName(
                "OFOQ.Market");

        // -------------------------------------------------
        // Tenancy
        // -------------------------------------------------

        services.AddScoped<
            ITenantRepository,
            TenantRepository>();

        services.AddScoped<
            ITenantDomainRepository,
            TenantDomainRepository>();

        services.AddScoped<
            ITenantMembershipRepository,
            TenantMembershipRepository>();

        services.AddScoped<
            ITenantStoreProfileRepository,
            TenantStoreProfileRepository>();

        services.AddScoped<
            ITenantStorefrontPresentationRepository,
            TenantStorefrontPresentationRepository>();

        services.AddScoped<
            ITenantStoreSocialLinkRepository,
            TenantStoreSocialLinkRepository>();

        services.AddScoped<
            IStoreReadinessQueryRepository,
            StoreReadinessQueryRepository>();

        // -------------------------------------------------
        // Catalog
        // -------------------------------------------------

        services.AddScoped<
            ICategoryRepository,
            CategoryRepository>();

        services.AddScoped<
            IProductRepository,
            ProductRepository>();
        services.AddScoped<
            IProductAttributeValueRepository,
            ProductAttributeValueRepository>();
        services.AddScoped<
            IProductImageRepository,
            ProductImageRepository>();
        services.AddScoped<
            IProductContentBlockRepository,
            ProductContentBlockRepository>();

        services.AddScoped<
            IProductRelationRepository,
            ProductRelationRepository>();

        services.AddScoped<
            ITenantProductRecommendationSettingsRepository,
            TenantProductRecommendationSettingsRepository>();
        services.AddScoped<
            IStorefrontQueryRepository,
            StorefrontQueryRepository>();

        services.AddScoped<
            IProductVariantRepository,
            ProductVariantRepository>();

        services.AddScoped<
            IProductOptionRepository,
            ProductOptionRepository>();

        services.AddScoped<
            IProductOptionValueRepository,
            ProductOptionValueRepository>();

        services.AddScoped<
            IProductVariantOptionValueRepository,
            ProductVariantOptionValueRepository>();

        services.AddScoped<
            IContentPageRepository,
            ContentPageRepository>();

        services.AddScoped<
            INavigationItemRepository,
            NavigationItemRepository>();

        services.AddScoped<
            IStorefrontContentQueryRepository,
            StorefrontContentQueryRepository>();

        // -------------------------------------------------
        // Commerce / Configuration
        // -------------------------------------------------

        services.AddScoped<
            ITenantCommerceVerticalRepository,
            TenantCommerceVerticalRepository>();

        services.AddScoped<
            ITenantCommerceCapabilityOverrideRepository,
            TenantCommerceCapabilityOverrideRepository>();

        // -------------------------------------------------
        // Commerce / Merchant Verification
        // -------------------------------------------------

        services.AddScoped<
            IMerchantVerificationProfileRepository,
            MerchantVerificationProfileRepository>();

        services.AddScoped<
            IMerchantVerificationDocumentRepository,
            MerchantVerificationDocumentRepository>();

        services.AddScoped<
            IMerchantVerificationDocumentFileRepository,
            MerchantVerificationDocumentFileRepository>();

        services.AddScoped<
            IMerchantVerificationReviewRepository,
            MerchantVerificationReviewRepository>();

        services.AddScoped<
            IMerchantVerificationReviewQueryRepository,
            MerchantVerificationReviewQueryRepository>();

        // -------------------------------------------------
        // Commerce
        // -------------------------------------------------

        services.AddScoped<
            ICartRepository,
            CartRepository>();

        services.AddScoped<
            IOrderRepository,
            OrderRepository>();

        services.AddScoped<
            IReturnRequestRepository,
            ReturnRequestRepository>();

        services.AddScoped<
            IReturnRequestLockRepository,
            ReturnRequestLockRepository>();

        services.AddScoped<
            IProductReviewRepository,
            ProductReviewRepository>();

        services.AddScoped<
            ITenantTrustMetricSettingsRepository,
            TenantTrustMetricSettingsRepository>();

        services.AddScoped<
            IReviewQueryRepository,
            ReviewQueryRepository>();

        services.AddScoped<
            ICustomerProfileRepository,
            CustomerProfileRepository>();

        services.AddScoped<
            ICustomerAddressRepository,
            CustomerAddressRepository>();

        services.AddScoped<
            IFulfillmentLocationRepository,
            FulfillmentLocationRepository>();

        services.AddScoped<
            IShippingMethodRepository,
            ShippingMethodRepository>();

        services.AddScoped<
            IDiscountCouponRepository,
            DiscountCouponRepository>();

        services.AddScoped<
            ICouponRedemptionRepository,
            CouponRedemptionRepository>();

        services.AddScoped<
            IDiscountCouponLockRepository,
            DiscountCouponLockRepository>();

        services.AddScoped<
            IMerchantOrderQueryRepository,
            MerchantOrderQueryRepository>();
        services.AddScoped<
            IMerchantAnalyticsQueryRepository,
            MerchantAnalyticsQueryRepository>();

        services.AddScoped<
            IMerchantOperationsDashboardQueryRepository,
            MerchantOperationsDashboardQueryRepository>();

        services.AddScoped<
            ICheckoutLockRepository,
            CheckoutLockRepository>();

        services.AddScoped<
            IOrderStateLockRepository,
            OrderStateLockRepository>();

        services.AddScoped<
            IPaymentRepository,
            PaymentRepository>();

        services.AddScoped<
            IPaymentIntentRepository,
            PaymentIntentRepository>();

        services.AddScoped<
            ITenantPaymentMethodRepository,
            TenantPaymentMethodRepository>();

        services.AddScoped<
            ITenantPaymentCapabilityRepository,
            TenantPaymentCapabilityRepository>();

        services.AddScoped<
            IPaymentCreationLockRepository,
            PaymentCreationLockRepository>();

        services.AddScoped<
            IPaymentStateLockRepository,
            PaymentStateLockRepository>();

        services.AddScoped<
            IPaymentWebhookLockRepository,
            PaymentWebhookLockRepository>();

        // -------------------------------------------------
        // Payment Provider Accounts
        // -------------------------------------------------

        services.AddScoped<
            ITenantPaymentProviderAccountRepository,
            TenantPaymentProviderAccountRepository>();

        services.AddScoped<
            ITenantPaymentWalletCapabilityRepository,
            TenantPaymentWalletCapabilityRepository>();

        // -------------------------------------------------
        // Identity
        // -------------------------------------------------

        services.AddScoped<
            IUserRepository,
            UserRepository>();

        services.AddScoped<
            IUserMfaRepository,
            UserMfaRepository>();

        services.AddScoped<
            IUserMfaRecoveryCodeRepository,
            UserMfaRecoveryCodeRepository>();

        services.AddScoped<
            IMfaLoginChallengeRepository,
            MfaLoginChallengeRepository>();

        services.AddScoped<
            IEmailVerificationChallengeRepository,
            EmailVerificationChallengeRepository>();

        services.AddScoped<
            IUserSessionRepository,
            UserSessionRepository>();

        services.AddScoped<
            IUserExternalLoginRepository,
            UserExternalLoginRepository>();

        services.AddScoped<
            IUserTrustedDeviceRepository,
            UserTrustedDeviceRepository>();

        services.AddScoped<
            IPlatformUserRoleAssignmentRepository,
            PlatformUserRoleAssignmentRepository>();

        services.AddScoped<
            ITenantNotificationPreferencesRepository,
            TenantNotificationPreferencesRepository>();

        // -------------------------------------------------
        // Security
        // -------------------------------------------------

        services.AddSingleton<
            IPasswordHasher,
            AspNetPasswordHasher>();

        services.AddSingleton<
            IMfaSecretProtector,
            DataProtectionMfaSecretProtector>();

        services.AddSingleton<
            ITotpService,
            OtpNetTotpService>();

        services.AddSingleton<
            IMfaLoginChallengeTokenService,
            MfaLoginChallengeTokenService>();

        services.AddSingleton<
            IEmailVerificationTokenService,
            EmailVerificationTokenService>();

        services.AddSingleton<
            IRefreshTokenService,
            RefreshTokenService>();

        services.AddSingleton<
            IGoogleIdentityTokenValidator,
            GoogleIdentityTokenValidator>();

        services.AddSingleton<
            ITrustedDeviceTokenService,
            TrustedDeviceTokenService>();

        services.AddSingleton<
            IPaymentProviderCredentialProtector,
            AesGcmPaymentProviderCredentialProtector>();

        services.AddSingleton<
            IMerchantVerificationDocumentProtector,
            AesGcmMerchantVerificationDocumentProtector>();

        services.AddSingleton<
            IMerchantVerificationPrivateFileStore,
            FileSystemMerchantVerificationPrivateFileStore>();

        // -------------------------------------------------
        // Persistence abstractions
        // -------------------------------------------------

        services.AddScoped<
            ITransactionExecutor,
            EfTransactionExecutor>();

        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    MarketDbContext>());

        return services;
    }
}
