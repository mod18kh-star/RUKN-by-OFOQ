using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class CommerceConfigurationDatabaseIntegrationTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Database_DuplicateVerticalForSameTenant_IsRejected()
    {
        await _database.ResetAsync();

        var tenant =
            await CreateTenantAsync(
                "Duplicate Vertical Store");

        var now =
            DateTimeOffset.UtcNow;

        await using var dbContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        dbContext.TenantCommerceVerticals.Add(
            TenantCommerceVertical.Create(
                tenant.Id,
                CommerceVerticalType.Apparel,
                true,
                now));

        await dbContext.SaveChangesAsync();

        dbContext.TenantCommerceVerticals.Add(
            TenantCommerceVertical.Create(
                tenant.Id,
                CommerceVerticalType.Apparel,
                false,
                now.AddSeconds(1)));

        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () =>
                    dbContext.SaveChangesAsync());

        AssertPostgresConstraint(
            exception,
            "23505",
            "ux_commerce_verticals_tenant_vertical");
    }

    [Fact]
    public async Task Database_TwoPrimaryVerticalsForSameTenant_AreRejected()
    {
        await _database.ResetAsync();

        var tenant =
            await CreateTenantAsync(
                "Primary Constraint Store");

        var now =
            DateTimeOffset.UtcNow;

        await using var dbContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        dbContext.TenantCommerceVerticals.AddRange(
            TenantCommerceVertical.Create(
                tenant.Id,
                CommerceVerticalType.Apparel,
                true,
                now),
            TenantCommerceVertical.Create(
                tenant.Id,
                CommerceVerticalType.Footwear,
                true,
                now));

        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () =>
                    dbContext.SaveChangesAsync());

        AssertPostgresConstraint(
            exception,
            "23505",
            "ux_commerce_verticals_tenant_primary");
    }

    [Fact]
    public async Task Database_PrimaryVerticalMustBeEnabled()
    {
        await _database.ResetAsync();

        var tenant =
            await CreateTenantAsync(
                "Primary Enabled Store");

        await using var dbContext =
            _database.CreateContext();

        var id =
            Guid.NewGuid();

        var now =
            DateTimeOffset.UtcNow;

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    dbContext.Database
                        .ExecuteSqlInterpolatedAsync(
                            $"""
                            INSERT INTO commerce_tenant_verticals
                            (
                                id,
                                tenant_id,
                                vertical_type,
                                is_enabled,
                                is_primary,
                                created_at_utc,
                                created_by_user_id,
                                updated_at_utc,
                                updated_by_user_id
                            )
                            VALUES
                            (
                                {id},
                                {tenant.Id.Value},
                                {(int)CommerceVerticalType.Apparel},
                                FALSE,
                                TRUE,
                                {now},
                                NULL,
                                NULL,
                                NULL
                            );
                            """));

        AssertPostgresConstraint(
            exception,
            "23514",
            "ck_commerce_verticals_primary_enabled");
    }

    [Fact]
    public async Task Database_UnknownVerticalType_IsRejected()
    {
        await _database.ResetAsync();

        var tenant =
            await CreateTenantAsync(
                "Unknown Vertical Store");

        await using var dbContext =
            _database.CreateContext();

        var id =
            Guid.NewGuid();

        var now =
            DateTimeOffset.UtcNow;

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    dbContext.Database
                        .ExecuteSqlInterpolatedAsync(
                            $"""
                            INSERT INTO commerce_tenant_verticals
                            (
                                id,
                                tenant_id,
                                vertical_type,
                                is_enabled,
                                is_primary,
                                created_at_utc,
                                created_by_user_id,
                                updated_at_utc,
                                updated_by_user_id
                            )
                            VALUES
                            (
                                {id},
                                {tenant.Id.Value},
                                0,
                                TRUE,
                                FALSE,
                                {now},
                                NULL,
                                NULL,
                                NULL
                            );
                            """));

        AssertPostgresConstraint(
            exception,
            "23514",
            "ck_commerce_verticals_vertical_type");
    }

    [Fact]
    public async Task Database_DuplicateCapabilityOverrideForSameTenant_IsRejected()
    {
        await _database.ResetAsync();

        var tenant =
            await CreateTenantAsync(
                "Duplicate Capability Store");

        var now =
            DateTimeOffset.UtcNow;

        await using var dbContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        dbContext.TenantCommerceCapabilityOverrides.Add(
            TenantCommerceCapabilityOverride.Create(
                tenant.Id,
                CommerceCapabilityType.MultiWarehouse,
                true,
                now));

        await dbContext.SaveChangesAsync();

        dbContext.TenantCommerceCapabilityOverrides.Add(
            TenantCommerceCapabilityOverride.Create(
                tenant.Id,
                CommerceCapabilityType.MultiWarehouse,
                false,
                now.AddSeconds(1)));

        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () =>
                    dbContext.SaveChangesAsync());

        AssertPostgresConstraint(
            exception,
            "23505",
            "ux_commerce_cap_overrides_tenant_capability");
    }

    [Fact]
    public async Task Database_UnknownCapabilityType_IsRejected()
    {
        await _database.ResetAsync();

        var tenant =
            await CreateTenantAsync(
                "Unknown Capability Store");

        await using var dbContext =
            _database.CreateContext();

        var id =
            Guid.NewGuid();

        var now =
            DateTimeOffset.UtcNow;

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    dbContext.Database
                        .ExecuteSqlInterpolatedAsync(
                            $"""
                            INSERT INTO commerce_tenant_capability_overrides
                            (
                                id,
                                tenant_id,
                                capability_type,
                                is_enabled,
                                created_at_utc,
                                created_by_user_id,
                                updated_at_utc,
                                updated_by_user_id
                            )
                            VALUES
                            (
                                {id},
                                {tenant.Id.Value},
                                0,
                                TRUE,
                                {now},
                                NULL,
                                NULL,
                                NULL
                            );
                            """));

        AssertPostgresConstraint(
            exception,
            "23514",
            "ck_commerce_cap_overrides_capability_type");
    }

    [Fact]
    public async Task Database_SameVerticalTypeAcrossDifferentTenants_IsAllowed()
    {
        await _database.ResetAsync();

        var firstTenant =
            await CreateTenantAsync(
                "First Apparel Store");

        var secondTenant =
            await CreateTenantAsync(
                "Second Apparel Store");

        var now =
            DateTimeOffset.UtcNow;

        await using (var firstContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             firstTenant.Id)))
        {
            firstContext.TenantCommerceVerticals.Add(
                TenantCommerceVertical.Create(
                    firstTenant.Id,
                    CommerceVerticalType.Apparel,
                    true,
                    now));

            await firstContext.SaveChangesAsync();
        }

        await using (var secondContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             secondTenant.Id)))
        {
            secondContext.TenantCommerceVerticals.Add(
                TenantCommerceVertical.Create(
                    secondTenant.Id,
                    CommerceVerticalType.Apparel,
                    true,
                    now));

            await secondContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var total =
            await verificationContext
                .TenantCommerceVerticals
                .IgnoreQueryFilters()
                .CountAsync(
                    vertical =>
                        vertical.VerticalType ==
                        CommerceVerticalType.Apparel);

        Assert.Equal(
            2,
            total);
    }

    [Fact]
    public async Task Database_SameCapabilityOverrideAcrossDifferentTenants_IsAllowed()
    {
        await _database.ResetAsync();

        var firstTenant =
            await CreateTenantAsync(
                "First Capability Store");

        var secondTenant =
            await CreateTenantAsync(
                "Second Capability Store");

        var now =
            DateTimeOffset.UtcNow;

        await using (var firstContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             firstTenant.Id)))
        {
            firstContext.TenantCommerceCapabilityOverrides.Add(
                TenantCommerceCapabilityOverride.Create(
                    firstTenant.Id,
                    CommerceCapabilityType.MultiWarehouse,
                    false,
                    now));

            await firstContext.SaveChangesAsync();
        }

        await using (var secondContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             secondTenant.Id)))
        {
            secondContext.TenantCommerceCapabilityOverrides.Add(
                TenantCommerceCapabilityOverride.Create(
                    secondTenant.Id,
                    CommerceCapabilityType.MultiWarehouse,
                    true,
                    now));

            await secondContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var total =
            await verificationContext
                .TenantCommerceCapabilityOverrides
                .IgnoreQueryFilters()
                .CountAsync(
                    capabilityOverride =>
                        capabilityOverride.CapabilityType ==
                        CommerceCapabilityType.MultiWarehouse);

        Assert.Equal(
            2,
            total);
    }

    [Fact]
    public async Task Database_QueryFilters_IsolateCommerceConfigurationByTenant()
    {
        await _database.ResetAsync();

        var firstTenant =
            await CreateTenantAsync(
                "First Isolated Store");

        var secondTenant =
            await CreateTenantAsync(
                "Second Isolated Store");

        var now =
            DateTimeOffset.UtcNow;

        await using (var firstContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             firstTenant.Id)))
        {
            firstContext.TenantCommerceVerticals.Add(
                TenantCommerceVertical.Create(
                    firstTenant.Id,
                    CommerceVerticalType.Apparel,
                    true,
                    now));

            firstContext.TenantCommerceCapabilityOverrides.Add(
                TenantCommerceCapabilityOverride.Create(
                    firstTenant.Id,
                    CommerceCapabilityType.MultiWarehouse,
                    false,
                    now));

            await firstContext.SaveChangesAsync();
        }

        await using (var secondContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             secondTenant.Id)))
        {
            secondContext.TenantCommerceVerticals.Add(
                TenantCommerceVertical.Create(
                    secondTenant.Id,
                    CommerceVerticalType.RealEstate,
                    true,
                    now));

            secondContext.TenantCommerceCapabilityOverrides.Add(
                TenantCommerceCapabilityOverride.Create(
                    secondTenant.Id,
                    CommerceCapabilityType.Maps,
                    false,
                    now));

            await secondContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    firstTenant.Id));

        var verticals =
            await verificationContext
                .TenantCommerceVerticals
                .AsNoTracking()
                .ToArrayAsync();

        var overrides =
            await verificationContext
                .TenantCommerceCapabilityOverrides
                .AsNoTracking()
                .ToArrayAsync();

        var vertical =
            Assert.Single(
                verticals);

        Assert.Equal(
            firstTenant.Id,
            vertical.TenantId);

        Assert.Equal(
            CommerceVerticalType.Apparel,
            vertical.VerticalType);

        var capabilityOverride =
            Assert.Single(
                overrides);

        Assert.Equal(
            firstTenant.Id,
            capabilityOverride.TenantId);

        Assert.Equal(
            CommerceCapabilityType.MultiWarehouse,
            capabilityOverride.CapabilityType);
    }

    [Fact]
    public async Task Database_CrossTenantCommerceWrite_IsBlockedBeforeDatabase()
    {
        await _database.ResetAsync();

        var firstTenant =
            await CreateTenantAsync(
                "Write Scope Store A");

        var secondTenant =
            await CreateTenantAsync(
                "Write Scope Store B");

        await using var dbContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    firstTenant.Id));

        dbContext.TenantCommerceVerticals.Add(
            TenantCommerceVertical.Create(
                secondTenant.Id,
                CommerceVerticalType.Apparel,
                true,
                DateTimeOffset.UtcNow));

        var exception =
            await Assert.ThrowsAsync<
                TenantScopeViolationException>(
                () =>
                    dbContext.SaveChangesAsync());

        Assert.Contains(
            "Cross-tenant",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Database_SwitchingPrimaryVertical_WithTwoStepTransaction_PersistsExactlyOnePrimary()
    {
        await _database.ResetAsync();

        var tenant =
            await CreateTenantAsync(
                "Primary Switch Store");

        var now =
            DateTimeOffset.UtcNow;

        await using (var setupContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            setupContext.TenantCommerceVerticals.AddRange(
                TenantCommerceVertical.Create(
                    tenant.Id,
                    CommerceVerticalType.Apparel,
                    true,
                    now),
                TenantCommerceVertical.Create(
                    tenant.Id,
                    CommerceVerticalType.Footwear,
                    false,
                    now));

            await setupContext.SaveChangesAsync();
        }

        await using (var updateContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            await using var transaction =
                await updateContext.Database
                    .BeginTransactionAsync();

            try
            {
                var verticals =
                    await updateContext
                        .TenantCommerceVerticals
                        .OrderBy(
                            vertical =>
                                vertical.VerticalType)
                        .ToArrayAsync();

                var apparel =
                    Assert.Single(
                        verticals,
                        vertical =>
                            vertical.VerticalType ==
                            CommerceVerticalType.Apparel);

                var footwear =
                    Assert.Single(
                        verticals,
                        vertical =>
                            vertical.VerticalType ==
                            CommerceVerticalType.Footwear);

                /*
                 * Flush the demotion first so PostgreSQL's partial
                 * unique index no longer sees an existing primary.
                 */
                apparel.RemovePrimary(
                    now.AddMinutes(1));

                await updateContext.SaveChangesAsync();

                /*
                 * The second update can now safely establish the
                 * replacement primary.
                 */
                footwear.MakePrimary(
                    now.AddMinutes(1));

                await updateContext.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        var savedVerticals =
            await verificationContext
                .TenantCommerceVerticals
                .AsNoTracking()
                .ToArrayAsync();

        Assert.Equal(
            2,
            savedVerticals.Length);

        var primary =
            Assert.Single(
                savedVerticals,
                vertical =>
                    vertical.IsPrimary);

        Assert.Equal(
            CommerceVerticalType.Footwear,
            primary.VerticalType);

        Assert.True(
            primary.IsEnabled);

        var previousPrimary =
            Assert.Single(
                savedVerticals,
                vertical =>
                    vertical.VerticalType ==
                    CommerceVerticalType.Apparel);

        Assert.False(
            previousPrimary.IsPrimary);
    }

    private async Task<Tenant> CreateTenantAsync(
        string name)
    {
        var tenant =
            Tenant.Create(
                name,
                $"commerce-db-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow);

        await using var dbContext =
            _database.CreateContext();

        dbContext.Tenants.Add(
            tenant);

        await dbContext.SaveChangesAsync();

        return tenant;
    }

    private static void AssertPostgresConstraint(
        Exception exception,
        string expectedSqlState,
        string expectedConstraintName)
    {
        var postgresException =
            FindPostgresException(
                exception);

        Assert.NotNull(
            postgresException);

        Assert.Equal(
            expectedSqlState,
            postgresException.SqlState);

        Assert.Equal(
            expectedConstraintName,
            postgresException.ConstraintName);
    }

    private static PostgresException? FindPostgresException(
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
}