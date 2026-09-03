using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Identity;

public sealed class IdentityPersistenceTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Database_RejectsDuplicateUserEmail()
    {
        await _database.ResetAsync();

        await using var dbContext =
            _database.CreateContext();

        var firstUser =
            User.Create(
                "user@example.com",
                "hash-one",
                DateTimeOffset.UtcNow);

        dbContext.Users.Add(firstUser);

        await dbContext.SaveChangesAsync();

        var secondUser =
            User.Create(
                "USER@EXAMPLE.COM",
                "hash-two",
                DateTimeOffset.UtcNow);

        dbContext.Users.Add(secondUser);

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
            "ux_users_email",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsDuplicateTenantMembership()
    {
        await _database.ResetAsync();

        await using var dbContext =
            _database.CreateContext();

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
                DateTimeOffset.UtcNow);

        var user =
            User.Create(
                "owner@example.com",
                "owner-hash",
                DateTimeOffset.UtcNow);

        dbContext.Tenants.Add(tenant);
        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        var firstMembership =
            TenantMembership.Create(
                tenant.Id,
                user.Id,
                TenantRole.Owner,
                DateTimeOffset.UtcNow);

        dbContext.TenantMemberships.Add(
            firstMembership);

        await dbContext.SaveChangesAsync();

        var secondMembership =
            TenantMembership.Create(
                tenant.Id,
                user.Id,
                TenantRole.Admin,
                DateTimeOffset.UtcNow);

        dbContext.TenantMemberships.Add(
            secondMembership);

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
            "ux_tenant_memberships_tenant_user",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsMembershipForNonExistingUser()
    {
        await _database.ResetAsync();

        await using var dbContext =
            _database.CreateContext();

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
                DateTimeOffset.UtcNow);

        dbContext.Tenants.Add(tenant);

        await dbContext.SaveChangesAsync();

        var membership =
            TenantMembership.Create(
                tenant.Id,
                UserId.New(),
                TenantRole.Staff,
                DateTimeOffset.UtcNow);

        dbContext.TenantMemberships.Add(
            membership);

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
            "fk_tenant_memberships_users_user_id",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_RejectsMembershipForNonExistingTenant()
    {
        await _database.ResetAsync();

        await using var dbContext =
            _database.CreateContext();

        var user =
            User.Create(
                "staff@example.com",
                "staff-hash",
                DateTimeOffset.UtcNow);

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        var membership =
            TenantMembership.Create(
                TenantId.New(),
                user.Id,
                TenantRole.Staff,
                DateTimeOffset.UtcNow);

        dbContext.TenantMemberships.Add(
            membership);

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
            "fk_tenant_memberships_tenants_tenant_id",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Database_PersistsValidUserAndTenantMembership()
    {
        await _database.ResetAsync();

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
                DateTimeOffset.UtcNow);

        var user =
            User.Create(
                "owner@example.com",
                "owner-hash",
                DateTimeOffset.UtcNow);

        var membership =
            TenantMembership.Create(
                tenant.Id,
                user.Id,
                TenantRole.Owner,
                DateTimeOffset.UtcNow);

        await using (
            var dbContext =
                _database.CreateContext())
        {
            dbContext.Tenants.Add(tenant);
            dbContext.Users.Add(user);
            dbContext.TenantMemberships.Add(
                membership);

            await dbContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var savedUser =
            await verificationContext.Users
                .SingleAsync(
                    item =>
                        item.Id == user.Id);

        var savedMembership =
            await verificationContext
                .TenantMemberships
                .SingleAsync(
                    item =>
                        item.Id == membership.Id);

        Assert.Equal(
            "owner@example.com",
            savedUser.Email.Value);

        Assert.Equal(
            tenant.Id,
            savedMembership.TenantId);

        Assert.Equal(
            user.Id,
            savedMembership.UserId);

        Assert.Equal(
            TenantRole.Owner,
            savedMembership.Role);
    }
}