using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class MarketApiFactory :
    WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=127.0.0.1;Port=5432;Database=unused;Username=unused;Password=unused";

    // Test-only key.
    // Never use this value in production.
    private const string TestRecoveryCodeHmacKey =
        "MDEyMzQ1Njc4OUFCQ0RFRjAxMjM0NTY3ODlBQkNERUY=";

    public MarketApiFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__MarketDatabase",
            TestConnectionString);

        Environment.SetEnvironmentVariable(
            "Authentication__Jwt__Issuer",
            TestAuthenticationConstants.Issuer);

        Environment.SetEnvironmentVariable(
            "Authentication__Jwt__Audience",
            TestAuthenticationConstants.Audience);

        Environment.SetEnvironmentVariable(
            "Authentication__Jwt__SigningKey",
            TestAuthenticationConstants.SigningKey);

        Environment.SetEnvironmentVariable(
            "Authentication__Jwt__AccessTokenMinutes",
            "15");

        Environment.SetEnvironmentVariable(
            "Authentication__Mfa__RecoveryCodeHmacKey",
            TestRecoveryCodeHmacKey);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "Testing");

        builder.ConfigureServices(
            services =>
            {
                services.RemoveAll<
                    ITenantRepository>();

                services.RemoveAll<
                    ITenantMembershipRepository>();

                services.RemoveAll<
                    ICategoryRepository>();

                services.RemoveAll<
                    IProductRepository>();

                services.RemoveAll<
                    IProductVariantRepository>();

                services.RemoveAll<
                    IUserRepository>();

                services.RemoveAll<
                    IUserMfaRepository>();

                services.RemoveAll<
                    IUserMfaRecoveryCodeRepository>();

                services.RemoveAll<
                    IMfaLoginChallengeRepository>();

                services.RemoveAll<
                    ITransactionExecutor>();

                services.RemoveAll<
                    IUnitOfWork>();

                services.RemoveAll<
                    IPasswordHasher>();

                services.RemoveAll<
                    IMfaSecretProtector>();

                services.RemoveAll<
                    ITotpService>();

                services.AddSingleton<
                    InMemoryTenantRepository>();

                services.AddSingleton<
                    ITenantRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryTenantRepository>());

                services.AddSingleton<
                    InMemoryTenantMembershipRepository>();

                services.AddSingleton<
                    ITenantMembershipRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryTenantMembershipRepository>());

                /*
                 * Category data persists for the lifetime
                 * of the test factory.
                 *
                 * Repository remains request-scoped because
                 * it depends on ICurrentTenant.
                 */
                services.AddSingleton<
                    InMemoryCategoryStore>();

                services.AddScoped<
                    ICategoryRepository,
                    InMemoryCategoryRepository>();

                /*
                 * Product data follows the same model.
                 *
                 * Store = factory lifetime.
                 * Repository = request lifetime.
                 */
                services.AddSingleton<
                    InMemoryProductStore>();

                services.AddScoped<
                    IProductRepository,
                    InMemoryProductRepository>();

                services.AddSingleton<
                    InMemoryProductVariantStore>();

                services.AddScoped<
                    IProductVariantRepository,
                    InMemoryProductVariantRepository>();

                services.AddSingleton<
                    InMemoryUserRepository>();

                services.AddSingleton<
                    IUserRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryUserRepository>());

                services.AddSingleton<
                    InMemoryUserMfaRepository>();

                services.AddSingleton<
                    IUserMfaRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryUserMfaRepository>());

                services.AddSingleton<
                    InMemoryUserMfaRecoveryCodeRepository>();

                services.AddSingleton<
                    IUserMfaRecoveryCodeRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryUserMfaRecoveryCodeRepository>());

                services.AddSingleton<
                    InMemoryMfaLoginChallengeRepository>();

                services.AddSingleton<
                    IMfaLoginChallengeRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryMfaLoginChallengeRepository>());

                services.AddSingleton<
                    FakeTransactionExecutor>();

                services.AddSingleton<
                    ITransactionExecutor>(
                        provider =>
                            provider.GetRequiredService<
                                FakeTransactionExecutor>());

                services.AddSingleton<
                    IPasswordHasher,
                    FakePasswordHasher>();

                services.AddSingleton<
                    IMfaSecretProtector,
                    FakeMfaSecretProtector>();

                services.AddSingleton<
                    ITotpService,
                    FakeTotpService>();

                services.AddSingleton<
                    IUnitOfWork,
                    FakeUnitOfWork>();
            });
    }
}