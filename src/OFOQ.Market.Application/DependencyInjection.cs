using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<CreateTenantHandler>();
        services.AddScoped<GetTenantByIdHandler>();

        return services;
    }
}