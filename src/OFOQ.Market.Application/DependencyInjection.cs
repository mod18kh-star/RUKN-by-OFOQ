using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Application.Identity.LoginUser;
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

        services.AddScoped<CreateTenantHandler>();

        services.AddScoped<GetTenantByIdHandler>();

        services.AddScoped<RegisterUserHandler>();

        services.AddScoped<LoginUserHandler>();

        return services;
    }
}