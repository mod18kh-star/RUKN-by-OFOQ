using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class PaymentProviderAccountIntegrationTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public void Model_ContainsProviderAccountMapping()
    {
        using var dbContext =
            CreateModelDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(
                    TenantPaymentProviderAccount));

        Assert.NotNull(
            entityType);

        Assert.Equal(
            "commerce_tenant_payment_provider_accounts",
            entityType.GetTableName());

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Count == 3);

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.GetDatabaseName() ==
                "ux_commerce_pay_provider_accounts_tenant_provider_enabled");
    }

    [Fact]
    public void Model_ContainsWalletCapabilityMapping()
    {
        using var dbContext =
            CreateModelDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(
                    TenantPaymentWalletCapability));

        Assert.NotNull(
            entityType);

        Assert.Equal(
            "commerce_tenant_payment_wallet_capabilities",
            entityType.GetTableName());

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Count == 3);

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.GetDatabaseName() ==
                "ux_commerce_pay_wallet_caps_tenant_account_wallet");
    }

    [Fact]
    public async Task Database_ConcurrentEnabledAccountsForSameProvider_AllowsOnlyOne()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Concurrent Payment Provider Store",
                $"payment-provider-concurrency-{Guid.NewGuid():N}",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        const string providerCode =
            "provider-concurrent";

        var firstReady =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var secondReady =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var release =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask =
            InsertEnabledProviderAccountAsync(
                tenant.Id,
                Guid.NewGuid(),
                providerCode,
                "Concurrent Provider Sandbox",
                environment: 0,
                firstReady,
                release);

        var secondTask =
            InsertEnabledProviderAccountAsync(
                tenant.Id,
                Guid.NewGuid(),
                providerCode,
                "Concurrent Provider Production",
                environment: 1,
                secondReady,
                release);

        await Task.WhenAll(
            firstReady.Task,
            secondReady.Task);

        release.SetResult(
            true);

        var results =
            await Task.WhenAll(
                firstTask,
                secondTask);

        var successes =
            results.Count(
                result =>
                    result is null);

        var failures =
            results
                .Where(
                    result =>
                        result is not null)
                .Cast<Exception>()
                .ToArray();

        Assert.Equal(
            1,
            successes);

        var failure =
            Assert.Single(
                failures);

        var postgresException =
            FindPostgresException(
                failure);

        Assert.NotNull(
            postgresException);

        Assert.Equal(
            "23505",
            postgresException.SqlState);

        Assert.Equal(
            "ux_commerce_pay_provider_accounts_tenant_provider_enabled",
            postgresException.ConstraintName);

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        var accounts =
            await verificationContext
                .TenantPaymentProviderAccounts
                .AsNoTracking()
                .ToArrayAsync();

        var enabledAccounts =
            accounts
                .Where(
                    account =>
                        account.IsEnabled &&
                        string.Equals(
                            account.ProviderCode.Value,
                            providerCode,
                            StringComparison.Ordinal))
                .ToArray();

        Assert.Single(
            enabledAccounts);
    }

    private async Task<Exception?>
        InsertEnabledProviderAccountAsync(
            TenantId tenantId,
            Guid accountId,
            string providerCode,
            string displayName,
            int environment,
            TaskCompletionSource<bool> ready,
            TaskCompletionSource<bool> release)
    {
        var connectionString =
            GetConcurrentConnectionString(
                tenantId);

        await using var connection =
            new NpgsqlConnection(
                connectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        ready.SetResult(
            true);

        await release.Task;

        try
        {
            await using var command =
                connection.CreateCommand();

            command.Transaction =
                transaction;

            command.CommandText =
                """
                INSERT INTO commerce_tenant_payment_provider_accounts
                (
                    id,
                    tenant_id,
                    provider_code,
                    display_name,
                    environment,
                    is_enabled,
                    protected_credentials,
                    credentials_version,
                    created_at_utc,
                    created_by_user_id,
                    updated_at_utc,
                    updated_by_user_id
                )
                VALUES
                (
                    @id,
                    @tenant_id,
                    @provider_code,
                    @display_name,
                    @environment,
                    TRUE,
                    @protected_credentials,
                    1,
                    @created_at_utc,
                    NULL,
                    NULL,
                    NULL
                );
                """;

            command.Parameters.AddWithValue(
                "id",
                accountId);

            command.Parameters.AddWithValue(
                "tenant_id",
                tenantId.Value);

            command.Parameters.AddWithValue(
                "provider_code",
                providerCode);

            command.Parameters.AddWithValue(
                "display_name",
                displayName);

            command.Parameters.AddWithValue(
                "environment",
                environment);

            command.Parameters.AddWithValue(
                "protected_credentials",
                "integration-test-protected-credentials");

            command.Parameters.AddWithValue(
                "created_at_utc",
                DateTimeOffset.UtcNow);

            await command.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            return null;
        }
        catch (Exception exception)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // PostgreSQL may already have aborted the transaction
                // after the unique-constraint violation.
            }

            return exception;
        }
    }

    private string GetConcurrentConnectionString(
        TenantId tenantId)
    {
        using var templateContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantId));

        var connectionString =
            templateContext.Database
                .GetConnectionString();

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new InvalidOperationException(
                "The integration test database connection string is unavailable.");
        }

        var builder =
            new NpgsqlConnectionStringBuilder(
                connectionString)
            {
                Pooling = false,
                Multiplexing = false
            };

        return builder.ConnectionString;
    }

    private static PostgresException?
        FindPostgresException(
            Exception exception)
    {
        Exception? current =
            exception;

        while (current is not null)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }

            current =
                current.InnerException;
        }

        return null;
    }

    private static MarketDbContext CreateModelDbContext()
    {
        var tenantId =
            TenantId.New();

        var options =
            new DbContextOptionsBuilder<MarketDbContext>()
                .UseNpgsql(
                    "Host=127.0.0.1;Port=5432;Database=unused;Username=unused;Password=unused")
                .Options;

        return new MarketDbContext(
            options,
            new TestCurrentTenant(
                tenantId));
    }
}