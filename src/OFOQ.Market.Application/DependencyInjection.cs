using OFOQ.Market.Application.Commerce.Verification.Merchant;
using OFOQ.Market.Application.Commerce.Verification.Review;
using Microsoft.Extensions.DependencyInjection;

using OFOQ.Market.Application.Catalog.Categories.CreateCategory;
using OFOQ.Market.Application.Catalog.Categories.GetCategories;
using OFOQ.Market.Application.Catalog.Categories.GetCategoryById;
using OFOQ.Market.Application.Catalog.Categories.MoveCategory;
using OFOQ.Market.Application.Catalog.Categories.UpdateCategory;

using OFOQ.Market.Application.Catalog.Products.ChangeState;
using OFOQ.Market.Application.Catalog.Products.CreateProduct;
using OFOQ.Market.Application.Catalog.Products.GetProductById;
using OFOQ.Market.Application.Catalog.Products.GetProducts;
using OFOQ.Market.Application.Catalog.Products.Inventory;
using OFOQ.Market.Application.Catalog.Products.UpdateProduct;

using OFOQ.Market.Application.Catalog.Products.Options.CreateOption;
using OFOQ.Market.Application.Catalog.Products.Options.CreateValue;
using OFOQ.Market.Application.Catalog.Products.Options.GetOptions;

using OFOQ.Market.Application.Catalog.Products.Variants.CreateVariant;
using OFOQ.Market.Application.Catalog.Products.Variants.GetVariants;

using OFOQ.Market.Application.Commerce.Carts.AddItem;
using OFOQ.Market.Application.Commerce.Carts.ClearCart;
using OFOQ.Market.Application.Commerce.Carts.GetCart;
using OFOQ.Market.Application.Commerce.Carts.RemoveItem;
using OFOQ.Market.Application.Commerce.Carts.UpdateItemQuantity;
using OFOQ.Market.Application.Commerce.Dashboard;

using OFOQ.Market.Application.Commerce.Checkout;
using OFOQ.Market.Application.Commerce.Fulfillment;
using OFOQ.Market.Application.Commerce.Customers;
using OFOQ.Market.Application.Commerce.Discounts;
using OFOQ.Market.Application.Commerce.Returns;
using OFOQ.Market.Application.Commerce.Reviews;
using OFOQ.Market.Application.Commerce.Orders.Cancel;

using OFOQ.Market.Application.Commerce.Configuration.CapabilityOverrides;
using OFOQ.Market.Application.Commerce.Configuration.ConfigureVertical;
using OFOQ.Market.Application.Commerce.Configuration.GetProfile;

using OFOQ.Market.Application.Commerce.Payments.CreateIntent;
using OFOQ.Market.Application.Commerce.Payments.ExecuteIntent;
using OFOQ.Market.Application.Commerce.Payments.GetAvailableMethods;
using OFOQ.Market.Application.Commerce.Payments.GetIntent;
using OFOQ.Market.Application.Commerce.Payments.ProcessWebhook;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Create;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Credentials;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Get;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.State;
using OFOQ.Market.Application.Commerce.Payments.ProviderAccounts.Wallets;
using OFOQ.Market.Application.Commerce.Payments.RetryIntent;

using OFOQ.Market.Application.Identity.CurrentUserContext;
using OFOQ.Market.Application.Content;
using OFOQ.Market.Application.Identity.EmailVerification.Confirm;
using OFOQ.Market.Application.Identity.GoogleSignIn;
using OFOQ.Market.Application.Identity.EmailVerification.Start;
using OFOQ.Market.Application.Identity.LoginUser;
using OFOQ.Market.Application.Identity.Mfa.CompleteEnrollment;
using OFOQ.Market.Application.Identity.Mfa.ConfirmEnrollment;
using OFOQ.Market.Application.Identity.Mfa.Login.VerifyRecovery;
using OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Consume;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Generate;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Regenerate;
using OFOQ.Market.Application.Identity.Mfa.StartEnrollment;
using OFOQ.Market.Application.Identity.RegisterUser;
using OFOQ.Market.Application.Identity.Sessions;
using OFOQ.Market.Application.Identity.TrustedDevices;
using OFOQ.Market.Application.Notifications;

using OFOQ.Market.Application.Tenancy.CreateTenant;
using OFOQ.Market.Application.Tenancy.GetTenantById;
using OFOQ.Market.Application.Tenancy.StoreProfile;
using OFOQ.Market.Application.Tenancy.StorefrontPresentation;
using OFOQ.Market.Application.Tenancy.StoreReadiness;

namespace OFOQ.Market.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddSingleton(
            TimeProvider.System);

        // -------------------------------------------------
        // Tenancy
        // -------------------------------------------------

        services.AddScoped<
            CreateTenantHandler>();

        services.AddScoped<
            GetTenantByIdHandler>();

        services.AddScoped<
            GetStoreProfileHandler>();

        services.AddScoped<
            UpdateStoreProfileHandler>();

        services.AddScoped<
            ReplaceStoreSocialLinksHandler>();

        services.AddScoped<
            GetStorefrontPresentationHandler>();

        services.AddScoped<
            UpdateStorefrontPresentationHandler>();

        services.AddScoped<
            GetStoreReadinessHandler>();

        // -------------------------------------------------
        // Categories
        // -------------------------------------------------

        services.AddScoped<
            CreateCategoryHandler>();

        services.AddScoped<
            GetCategoriesHandler>();

        services.AddScoped<
            GetCategoryByIdHandler>();

        services.AddScoped<
            UpdateCategoryHandler>();

        services.AddScoped<
            MoveCategoryHandler>();

        // -------------------------------------------------
        // Products
        // -------------------------------------------------

        services.AddScoped<
            CreateProductHandler>();

        services.AddScoped<
            GetProductsHandler>();

        services.AddScoped<
            GetProductByIdHandler>();

        services.AddScoped<
            UpdateProductHandler>();

        services.AddScoped<
            ChangeProductStateHandler>();

        services.AddScoped<
            UpdateProductInventoryHandler>();
        services.AddScoped<
            OFOQ.Market.Application.Catalog.ProductAttributes.GetProductAttributesHandler>();

        services.AddScoped<
            OFOQ.Market.Application.Catalog.ProductAttributes.SetProductAttributesHandler>();
        services.AddScoped<
            OFOQ.Market.Application.Storefront.StorefrontQueryService>();
        services.AddScoped<
            OFOQ.Market.Application.Catalog.ProductImages.GetProductImagesHandler>();

        services.AddScoped<
            OFOQ.Market.Application.Catalog.ProductImages.SetProductImagesHandler>();

        services.AddScoped<
            OFOQ.Market.Application.Catalog.ProductContentBlocks.GetProductContentBlocksHandler>();

        services.AddScoped<
            OFOQ.Market.Application.Catalog.ProductContentBlocks.SetProductContentBlocksHandler>();

        services.AddScoped<
            OFOQ.Market.Application.Catalog.ProductRelations.GetProductRelationsHandler>();

        services.AddScoped<
            OFOQ.Market.Application.Catalog.ProductRelations.SetProductRelationsHandler>();

        services.AddScoped<
            OFOQ.Market.Application.Catalog.ProductRelations.ProductRecommendationSettingsHandler>();

        // -------------------------------------------------
        // Structured product options
        // -------------------------------------------------

        services.AddScoped<
            CreateProductOptionHandler>();

        services.AddScoped<
            CreateProductOptionValueHandler>();

        services.AddScoped<
            GetProductOptionsHandler>();

        // -------------------------------------------------
        // Structured product variants
        // -------------------------------------------------

        services.AddScoped<
            CreateProductVariantHandler>();

        services.AddScoped<
            GetProductVariantsHandler>();

        // -------------------------------------------------
        // Commerce / Configuration
        // -------------------------------------------------

        services.AddScoped<
            GetCommerceProfileHandler>();

        services.AddScoped<
            ConfigureCommerceVerticalHandler>();

        services.AddScoped<
            SetCommerceCapabilityOverrideHandler>();

        // -------------------------------------------------
        // Commerce / Cart
        // -------------------------------------------------

        services.AddScoped<
            AddToCartHandler>();

        services.AddScoped<
            GetCartHandler>();

        services.AddScoped<
            UpdateCartItemQuantityHandler>();

        services.AddScoped<
            RemoveCartItemHandler>();

        services.AddScoped<
            ClearCartHandler>();

        services.AddScoped<
            GetMerchantOperationsDashboardHandler>();

        // -------------------------------------------------
        // Commerce / Checkout
        // -------------------------------------------------

        services.AddScoped<
            CheckoutHandler>();

        services.AddScoped<
            CheckoutPricingService>();

        services.AddScoped<
            CustomerAccountService>();

        services.AddScoped<
            CustomerSavedAddressService>();

        services.AddScoped<
            CouponAdministrationService>();

        services.AddScoped<
            FulfillmentSettingsService>();

        services.AddScoped<
            ReturnManagementService>();

        services.AddScoped<
            ReviewManagementService>();

        services.AddScoped<
            CancelOrderHandler>();
        services.AddScoped<
            OFOQ.Market.Application.Commerce.Orders.Queries.GetMerchantOrdersHandler>();

        services.AddScoped<
            OFOQ.Market.Application.Commerce.Orders.Queries.GetMerchantOrderByIdHandler>();

        services.AddScoped<
            OFOQ.Market.Application.Commerce.Orders.State.ChangeOrderLifecycleHandler>();
        services.AddScoped<
            OFOQ.Market.Application.Commerce.Analytics.GetMerchantAnalyticsHandler>();

        services.AddScoped<
            ContentManagementService>();

        // -------------------------------------------------
        // Commerce / Payments
        // -------------------------------------------------

        services.AddScoped<
            GetAvailablePaymentMethodsHandler>();

        services.AddScoped<
            CreatePaymentIntentHandler>();

        services.AddScoped<
            GetPaymentIntentHandler>();

        services.AddScoped<
            RetryPaymentIntentHandler>();

        services.AddScoped<
            ExecutePaymentIntentHandler>();

        services.AddScoped<
            ProcessPaymentWebhookHandler>();

        // -------------------------------------------------
        // Commerce / Payment Provider Accounts
        // -------------------------------------------------

        services.AddScoped<
            CreatePaymentProviderAccountHandler>();

        services.AddScoped<
            GetPaymentProviderAccountsHandler>();

        services.AddScoped<
            UpdatePaymentProviderCredentialsHandler>();

        services.AddScoped<
            SetPaymentProviderAccountStateHandler>();

        services.AddScoped<
            SetPaymentWalletCapabilityHandler>();

        // -------------------------------------------------
        // Commerce / Merchant Verification Review
        // -------------------------------------------------

        services.AddScoped<
            MerchantVerificationReviewConflictGuard>();

        services.AddScoped<
            GetMerchantVerificationReviewQueueHandler>();

        services.AddScoped<
            GetMerchantVerificationReviewDetailHandler>();

        services.AddScoped<
            StartMerchantVerificationReviewHandler>();

        services.AddScoped<
            RequestMoreInformationMerchantVerificationReviewHandler>();

        services.AddScoped<
            RejectMerchantVerificationReviewHandler>();

        services.AddScoped<
            VerifyMerchantVerificationReviewHandler>();
        // -------------------------------------------------
        // Commerce / Merchant Verification Self Service
        // -------------------------------------------------

        services.AddScoped<
            GetMerchantVerificationSelfServiceHandler>();

        services.AddScoped<
            UpsertMerchantVerificationProfileHandler>();

        services.AddScoped<
            UpsertMerchantVerificationDocumentHandler>();

        services.AddScoped<
            UploadMerchantVerificationFileHandler>();

        services.AddScoped<
            SubmitMerchantVerificationHandler>();

        services.AddScoped<
            OpenMerchantVerificationOwnFileHandler>();

        services.AddScoped<
            OpenMerchantVerificationReviewFileHandler>();
        // -------------------------------------------------
        // Identity
        // -------------------------------------------------

        services.AddScoped<
            RegisterUserHandler>();

        services.AddScoped<
            LoginUserHandler>();

        services.AddScoped<
            GetCurrentUserContextHandler>();

        services.AddScoped<
            StartEmailVerificationHandler>();

        services.AddScoped<
            ConfirmEmailVerificationHandler>();

        services.AddScoped<
            AuthenticationSessionService>();

        services.AddScoped<
            GoogleSignInHandler>();

        services.AddScoped<
            TrustedDeviceService>();

        services.AddScoped<
            VerifyMfaTotpHandler>();

        services.AddScoped<
            VerifyMfaRecoveryCodeHandler>();

        services.AddScoped<
            StartMfaEnrollmentHandler>();

        services.AddScoped<
            ConfirmMfaEnrollmentHandler>();

        services.AddScoped<
            CompleteMfaEnrollmentHandler>();

        services.AddScoped<
            GenerateRecoveryCodesHandler>();

        services.AddScoped<
            RegenerateRecoveryCodesHandler>();

        services.AddScoped<
            ConsumeRecoveryCodeHandler>();

        // -------------------------------------------------
        // Notifications
        // -------------------------------------------------

        services.AddScoped<
            GetNotificationPreferencesHandler>();

        services.AddScoped<
            UpdateNotificationPreferencesHandler>();

        return services;
    }
}