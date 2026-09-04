using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Catalog;

public sealed class CategoryTenantIsolationTests
{
    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Query_ReturnsOnlyCurrentTenantCategories()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenantA =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        var tenantB =
            Tenant.Create(
                "Store B",
                "store-b",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.AddRange(
                tenantA,
                tenantB);

            await setupContext.SaveChangesAsync();
        }

        var categoryA =
            Category.Create(
                tenantA.Id,
                "Category A",
                "category-a",
                now);

        await using (var tenantAContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantA.Id)))
        {
            tenantAContext.Categories.Add(
                categoryA);

            await tenantAContext.SaveChangesAsync();
        }

        var categoryB =
            Category.Create(
                tenantB.Id,
                "Category B",
                "category-b",
                now);

        await using (var tenantBContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            tenantBContext.Categories.Add(
                categoryB);

            await tenantBContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        var categories =
            await verificationContext
                .Categories
                .ToListAsync();

        var savedCategory =
            Assert.Single(
                categories);

        Assert.Equal(
            tenantA.Id,
            savedCategory.TenantId);

        Assert.Equal(
            "Category A",
            savedCategory.Name);
    }

    [Fact]
    public async Task Query_ByForeignTenantCategoryId_ReturnsNull()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenantA =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        var tenantB =
            Tenant.Create(
                "Store B",
                "store-b",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.AddRange(
                tenantA,
                tenantB);

            await setupContext.SaveChangesAsync();
        }

        var foreignCategory =
            Category.Create(
                tenantB.Id,
                "Secret Category",
                "secret-category",
                now);

        await using (var tenantBContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenantB.Id)))
        {
            tenantBContext.Categories.Add(
                foreignCategory);

            await tenantBContext.SaveChangesAsync();
        }

        await using var tenantAContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        var result =
            await tenantAContext
                .Categories
                .SingleOrDefaultAsync(
                    category =>
                        category.Id ==
                        foreignCategory.Id);

        Assert.Null(
            result);
    }

    [Fact]
    public async Task Query_WithoutTenantContext_ReturnsNoTenantBusinessData()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        var category =
            Category.Create(
                tenant.Id,
                "Category",
                "category",
                now);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Categories.Add(
                category);

            await tenantContext.SaveChangesAsync();
        }

        await using var noTenantContext =
            _database.CreateContext();

        var categories =
            await noTenantContext
                .Categories
                .ToListAsync();

        Assert.Empty(
            categories);
    }

    [Fact]
    public async Task SaveChanges_BlocksCrossTenantWrite()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenantA =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        var tenantB =
            Tenant.Create(
                "Store B",
                "store-b",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.AddRange(
                tenantA,
                tenantB);

            await setupContext.SaveChangesAsync();
        }

        await using var tenantAContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenantA.Id));

        var categoryForTenantB =
            Category.Create(
                tenantB.Id,
                "Foreign Category",
                "foreign-category",
                now);

        tenantAContext.Categories.Add(
            categoryForTenantB);

        await Assert.ThrowsAsync<
            TenantScopeViolationException>(
                async () =>
                {
                    await tenantAContext
                        .SaveChangesAsync();
                });
    }

    [Fact]
    public async Task SaveChanges_BlocksTenantDataWriteWithoutTenantContext()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        await using var noTenantContext =
            _database.CreateContext();

        noTenantContext.Categories.Add(
            Category.Create(
                tenant.Id,
                "Category",
                "category",
                now));

        await Assert.ThrowsAsync<
            TenantScopeViolationException>(
                async () =>
                {
                    await noTenantContext
                        .SaveChangesAsync();
                });
    }

    [Fact]
    public async Task Query_HidesSoftDeletedCategory()
    {
        await _database.ResetAsync();

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "Store A",
                "store-a",
                now);

        await using (var setupContext =
                     _database.CreateContext())
        {
            setupContext.Tenants.Add(
                tenant);

            await setupContext.SaveChangesAsync();
        }

        var category =
            Category.Create(
                tenant.Id,
                "Hidden Category",
                "hidden-category",
                now);

        await using (var tenantContext =
                     _database.CreateContext(
                         new TestCurrentTenant(
                             tenant.Id)))
        {
            tenantContext.Categories.Add(
                category);

            await tenantContext.SaveChangesAsync();

            category.Delete(
                now.AddMinutes(1));

            await tenantContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext(
                new TestCurrentTenant(
                    tenant.Id));

        var categories =
            await verificationContext
                .Categories
                .ToListAsync();

        Assert.Empty(
            categories);
    }
}