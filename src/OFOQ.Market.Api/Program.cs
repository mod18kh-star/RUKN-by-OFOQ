using OFOQ.Market.Api.Endpoints.Tenancy;
using OFOQ.Market.Application;
using OFOQ.Market.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString(
        "MarketDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'MarketDatabase' was not found.");

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    connectionString);

var app = builder.Build();

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "ok",
        service = "OFOQ.Market.Api"
    }));

app.MapTenantEndpoints();

app.Run();