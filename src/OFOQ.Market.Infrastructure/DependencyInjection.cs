using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Infrastructure.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;
using OFOQ.Market.Infrastructure.Security;

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
            IUserMfaRepository,
            UserMfaRepository>();

        services.AddScoped<
            IUserMfaRecoveryCodeRepository,
            UserMfaRecoveryCodeRepository>();

        services.AddScoped<
            IMfaLoginChallengeRepository,
            MfaLoginChallengeRepository>();

        services.AddScoped<
            ITenantMembershipRepository,
            TenantMembershipRepository>();

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

        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    MarketDbContext>());

        return services;
    }
}