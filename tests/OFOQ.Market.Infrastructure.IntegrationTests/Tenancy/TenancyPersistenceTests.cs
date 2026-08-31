using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Tenancy;

public sealed class TenancyPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Database_RejectsDuplicateTenantSlug()
    {
        await _database.ResetAsync();

        await using var dbContext =
            _database.CreateContext();

        var firstTenant = Tenant.Create(
            "Store A",
            "turks",
            DateTimeOffset.UtcNow);

        dbContext.Tenants.Add(firstTenant);

        await dbContext.SaveChangesAsync();

        var secondTenant = Tenant.Create(
            "Store B",
            "turks",
            DateTimeOffset.UtcNow);

        dbContext.Tenants.Add(secondTenant);

        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () => dbContext.SaveChangesAsync());

        var postgresException =
            Assert.IsType<PostgresException>(
                exception.InnerException);

        Assert.Equal(
            "23505",
            postgresException.SqlState);

        Assert.Equal(
            "ux_tenants_slug",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsDuplicateCustomDomain()
    {
        await _database.ResetAsync();

        await using var dbContext =
            _database.CreateContext();

        var firstTenant = Tenant.Create(
            "Store A",
            "store-a",
            DateTimeOffset.UtcNow);

        var secondTenant = Tenant.Create(
            "Store B",
            "store-b",
            DateTimeOffset.UtcNow);

        dbContext.Tenants.AddRange(
            firstTenant,
            secondTenant);

        await dbContext.SaveChangesAsync();

        var firstDomain = TenantDomain.Create(
            firstTenant.Id,
            "turks.com",
            DateTimeOffset.UtcNow);

        dbContext.TenantDomains.Add(
            firstDomain);

        await dbContext.SaveChangesAsync();

        var secondDomain = TenantDomain.Create(
            secondTenant.Id,
            "turks.com",
            DateTimeOffset.UtcNow);

        dbContext.TenantDomains.Add(
            secondDomain);

        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () => dbContext.SaveChangesAsync());

        var postgresException =
            Assert.IsType<PostgresException>(
                exception.InnerException);

        Assert.Equal(
            "23505",
            postgresException.SqlState);

        Assert.Equal(
            "ux_tenant_domains_domain",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsTwoPrimaryDomainsForSameTenant()
    {
        await _database.ResetAsync();

        await using var dbContext =
            _database.CreateContext();

        var tenant = Tenant.Create(
            "Store A",
            "store-a",
            DateTimeOffset.UtcNow);

        dbContext.Tenants.Add(tenant);

        await dbContext.SaveChangesAsync();

        var firstDomain = TenantDomain.Create(
            tenant.Id,
            "turks.com",
            DateTimeOffset.UtcNow);

        firstDomain.MarkVerified(
            DateTimeOffset.UtcNow);

        firstDomain.MakePrimary(
            DateTimeOffset.UtcNow);

        dbContext.TenantDomains.Add(
            firstDomain);

        await dbContext.SaveChangesAsync();

        var secondDomain = TenantDomain.Create(
            tenant.Id,
            "turks-store.com",
            DateTimeOffset.UtcNow);

        secondDomain.MarkVerified(
            DateTimeOffset.UtcNow);

        secondDomain.MakePrimary(
            DateTimeOffset.UtcNow);

        dbContext.TenantDomains.Add(
            secondDomain);

        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () => dbContext.SaveChangesAsync());

        var postgresException =
            Assert.IsType<PostgresException>(
                exception.InnerException);

        Assert.Equal(
            "23505",
            postgresException.SqlState);

        Assert.Equal(
            "ux_tenant_domains_one_primary_per_tenant",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsDomainForNonExistingTenant()
    {
        await _database.ResetAsync();

        await using var dbContext =
            _database.CreateContext();

        var nonExistingTenantId =
            TenantId.New();

        var domain = TenantDomain.Create(
            nonExistingTenantId,
            "orphan-store.com",
            DateTimeOffset.UtcNow);

        dbContext.TenantDomains.Add(domain);

        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () => dbContext.SaveChangesAsync());

        var postgresException =
            Assert.IsType<PostgresException>(
                exception.InnerException);

        Assert.Equal(
            "23503",
            postgresException.SqlState);

        Assert.Equal(
            "fk_tenant_domains_tenants_tenant_id",
            postgresException.ConstraintName);
    }
}