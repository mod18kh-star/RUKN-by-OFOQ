using Microsoft.AspNetCore.Authorization;
using OFOQ.Market.Api.Security.Tenancy;
using OFOQ.Market.Application.Common.Tenancy;

namespace OFOQ.Market.Api.Security.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddOfoqAuthorization(
        this IServiceCollection services)
    {
        /*
         * One tenant context per HTTP request.
         */
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
            });

        services.AddScoped<
            IAuthorizationHandler,
            TenantBackOfficeAuthorizationHandler>();

        return services;
    }
}