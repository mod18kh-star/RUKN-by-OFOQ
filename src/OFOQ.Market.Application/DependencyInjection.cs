using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Application.Catalog.Categories.CreateCategory;
using OFOQ.Market.Application.Catalog.Categories.GetCategories;
using OFOQ.Market.Application.Catalog.Categories.GetCategoryById;
using OFOQ.Market.Application.Catalog.Products.ChangeState;
using OFOQ.Market.Application.Catalog.Products.CreateProduct;
using OFOQ.Market.Application.Catalog.Products.GetProductById;
using OFOQ.Market.Application.Catalog.Products.GetProducts;
using OFOQ.Market.Application.Catalog.Products.Inventory;
using OFOQ.Market.Application.Catalog.Products.Options.CreateOption;
using OFOQ.Market.Application.Catalog.Products.Options.CreateValue;
using OFOQ.Market.Application.Catalog.Products.Options.GetOptions;
using OFOQ.Market.Application.Catalog.Products.UpdateProduct;
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

        // Tenancy
        services.AddScoped<
            CreateTenantHandler>();

        services.AddScoped<
            GetTenantByIdHandler>();

        // Categories
        services.AddScoped<
            CreateCategoryHandler>();

        services.AddScoped<
            GetCategoriesHandler>();

        services.AddScoped<
            GetCategoryByIdHandler>();

        // Products
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

        // Structured product options
        services.AddScoped<
            CreateProductOptionHandler>();

        services.AddScoped<
            CreateProductOptionValueHandler>();

        services.AddScoped<
            GetProductOptionsHandler>();

        // Identity
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