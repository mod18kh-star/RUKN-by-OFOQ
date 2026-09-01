using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OFOQ.Market.Application.Common.Persistence;

namespace OFOQ.Market.Api.Tests.Support;

internal sealed class MarketApiFactory :
    WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=127.0.0.1;Port=5432;Database=unused;Username=unused;Password=unused";

    public MarketApiFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__MarketDatabase",
            TestConnectionString);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(
            services =>
            {
                services.RemoveAll<ITenantRepository>();
                services.RemoveAll<IUnitOfWork>();

                services.AddSingleton<
                    InMemoryTenantRepository>();

                services.AddSingleton<ITenantRepository>(
                    provider =>
                        provider.GetRequiredService<
                            InMemoryTenantRepository>());

                services.AddSingleton<
                    IUnitOfWork,
                    FakeUnitOfWork>();
            });
    }
}