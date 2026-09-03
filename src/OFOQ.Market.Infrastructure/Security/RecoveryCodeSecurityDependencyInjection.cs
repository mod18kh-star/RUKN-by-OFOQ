using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Infrastructure.Security;

public static class RecoveryCodeSecurityDependencyInjection
{
    public static IServiceCollection AddRecoveryCodeSecurity(
        this IServiceCollection services,
        string hmacKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            hmacKey);

        services.AddSingleton<IRecoveryCodeService>(
            new HmacRecoveryCodeService(
                hmacKey));

        return services;
    }
}