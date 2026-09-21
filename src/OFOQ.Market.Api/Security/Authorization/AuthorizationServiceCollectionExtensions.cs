using Microsoft.AspNetCore.Authorization;
using OFOQ.Market.Api.Security.Tenancy;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Api.Security.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddOfoqAuthorization(
        this IServiceCollection services)
    {
        services.AddScoped<
            CurrentTenantContext>();

        services.AddScoped<
            ICurrentTenant>(
                serviceProvider =>
                    serviceProvider.GetRequiredService<
                        CurrentTenantContext>());

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    AuthorizationPolicies.TenantBackOffice,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.AddRequirements(
                            TenantBackOfficeRequirement.Instance);
                    });

                options.AddPolicy(
                    AuthorizationPolicies.TenantPaymentAdministration,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.AddRequirements(
                            TenantPaymentAdministrationRequirement.Instance);
                    });

                options.AddPolicy(
                    AuthorizationPolicies.TenantCommerceAdministration,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.AddRequirements(
                            TenantCommerceAdministrationRequirement.Instance);
                    });

                options.AddPolicy(
                    AuthorizationPolicies.PlatformMerchantVerificationReview,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();

                        policy.AddRequirements(
                            PlatformMerchantVerificationReviewRequirement.Instance);
                    });
            });

        services.AddScoped<
            IAuthorizationHandler,
            TenantBackOfficeAuthorizationHandler>();

        services.AddScoped<
            IAuthorizationHandler,
            TenantPaymentAdministrationAuthorizationHandler>();

        services.AddScoped<
            IAuthorizationHandler,
            TenantCommerceAdministrationAuthorizationHandler>();

        services.AddScoped<
            IAuthorizationHandler,
            PlatformMerchantVerificationReviewAuthorizationHandler>();
        services.AddScoped<
            PlatformTenantAdministrationAccessEvaluator>();

        services.AddScoped<
            IAuthorizationHandler,
            PlatformTenantBackOfficeAuthorizationHandler>();

        services.AddScoped<
            IAuthorizationHandler,
            PlatformTenantCommerceAdministrationAuthorizationHandler>();

        return services;
    }
}
