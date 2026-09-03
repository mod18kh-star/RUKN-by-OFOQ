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
    // 32 bytes encoded as Base64.
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
                    IUserRepository>();

                services.RemoveAll<
                    IUnitOfWork>();

                services.RemoveAll<
                    IPasswordHasher>();

                services.AddSingleton<
                    InMemoryTenantRepository>();

                services.AddSingleton<
                    ITenantRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryTenantRepository>());

                services.AddSingleton<
                    InMemoryUserRepository>();

                services.AddSingleton<
                    IUserRepository>(
                        provider =>
                            provider.GetRequiredService<
                                InMemoryUserRepository>());

                services.AddSingleton<
                    IPasswordHasher,
                    FakePasswordHasher>();

                services.AddSingleton<
                    IUnitOfWork,
                    FakeUnitOfWork>();
            });
    }
}