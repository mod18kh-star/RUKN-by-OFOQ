using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Application.Tenancy.CreateTenant;

namespace OFOQ.Market.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddSingleton(
            TimeProvider.System);

        services.AddScoped<CreateTenantHandler>();

        return services;
    }
}