using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;

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

        services.AddScoped<
            ITenantRepository,
            TenantRepository>();

        services.AddScoped<
            ITenantDomainRepository,
            TenantDomainRepository>();

        services.AddScoped<
            IUserRepository,
            UserRepository>();

        services.AddScoped<
            ITenantMembershipRepository,
            TenantMembershipRepository>();

        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    MarketDbContext>());

        return services;
    }
}