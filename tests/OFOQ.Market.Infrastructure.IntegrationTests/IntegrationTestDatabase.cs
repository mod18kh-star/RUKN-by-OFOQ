using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Infrastructure.IntegrationTests;

internal sealed class IntegrationTestDatabase
{
    private const string ExpectedDatabaseName =
        "ofoq_market_tests";

    private readonly string _connectionString;

    private IntegrationTestDatabase(
        string connectionString)
    {
        _connectionString =
            connectionString;
    }

    public static IntegrationTestDatabase Create()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddUserSecrets<IntegrationTestDatabase>(
                    optional: false)
                .Build();

        var connectionString =
            configuration[
                "ConnectionStrings:MarketTestDatabase"];

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new InvalidOperationException(
                "The integration test database connection string was not found.");
        }

        ValidateConnectionString(
            connectionString);

        return new IntegrationTestDatabase(
            connectionString);
    }

    public MarketDbContext CreateContext(
        TestCurrentTenant? currentTenant = null)
    {
        var options =
            new DbContextOptionsBuilder<MarketDbContext>()
                .UseNpgsql(
                    _connectionString)
                .Options;

        return new MarketDbContext(
            options,
            currentTenant);
    }

    public async Task ResetAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            CreateContext();

        await dbContext.Database.MigrateAsync(
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
                    TRUNCATE TABLE
                catalog_product_variants,
                catalog_products,
                catalog_categories,
                mfa_login_challenges,
                user_mfa_recovery_codes,
                user_mfa,
                tenant_memberships,
                tenant_domains,
                users,
                tenants
            RESTART IDENTITY
            CASCADE;
            """,
            cancellationToken);
    }

    private static void ValidateConnectionString(
        string connectionString)
    {
        var builder =
            new NpgsqlConnectionStringBuilder(
                connectionString);

        if (!string.Equals(
                builder.Database,
                ExpectedDatabaseName,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Integration tests are allowed to use only the '{ExpectedDatabaseName}' database.");
        }

        var isLocalHost =
            string.Equals(
                builder.Host,
                "localhost",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                builder.Host,
                "127.0.0.1",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                builder.Host,
                "::1",
                StringComparison.OrdinalIgnoreCase);

        if (!isLocalHost)
        {
            throw new InvalidOperationException(
                "Integration tests are allowed to use only a local PostgreSQL server.");
        }
    }
}