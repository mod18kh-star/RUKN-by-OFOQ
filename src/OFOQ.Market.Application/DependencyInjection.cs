using Microsoft.Extensions.DependencyInjection;

using OFOQ.Market.Application.Catalog.Categories.CreateCategory;
using OFOQ.Market.Application.Catalog.Categories.GetCategories;
using OFOQ.Market.Application.Catalog.Categories.GetCategoryById;

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

using OFOQ.Market.Application.Commerce.Checkout;

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

using OFOQ.Market.Application.Identity.LoginUser;
using OFOQ.Market.Application.Identity.Mfa.ConfirmEnrollment;
using OFOQ.Market.Application.Identity.Mfa.Login.VerifyRecovery;
using OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Consume;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Generate;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Regenerate;
using OFOQ.Market.Application.Identity.Mfa.StartEnrollment;
using OFOQ.Market.Application.Identity.RegisterUser;

using OFOQ.Market.Application.Tenancy.CreateTenant;
using OFOQ.Market.Application.Tenancy.GetTenantById;

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

        // -------------------------------------------------
        // Categories
        // -------------------------------------------------

        services.AddScoped<
            CreateCategoryHandler>();

        services.AddScoped<
            GetCategoriesHandler>();

        services.AddScoped<
            GetCategoryByIdHandler>();

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

        // -------------------------------------------------
        // Commerce / Checkout
        // -------------------------------------------------

        services.AddScoped<
            CheckoutHandler>();

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
        // Identity
        // -------------------------------------------------

        services.AddScoped<
            RegisterUserHandler>();

        services.AddScoped<
            LoginUserHandler>();

        services.AddScoped<
            VerifyMfaTotpHandler>();

        services.AddScoped<
            VerifyMfaRecoveryCodeHandler>();

        services.AddScoped<
            StartMfaEnrollmentHandler>();

        services.AddScoped<
            ConfirmMfaEnrollmentHandler>();

        services.AddScoped<
            GenerateRecoveryCodesHandler>();

        services.AddScoped<
            RegenerateRecoveryCodesHandler>();

        services.AddScoped<
            ConsumeRecoveryCodeHandler>();

        return services;
    }
}