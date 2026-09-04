using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence;

public sealed class MarketDbContext :
    DbContext,
    IUnitOfWork
{
    private readonly ICurrentTenant?
        _currentTenant;

    public MarketDbContext(
        DbContextOptions<MarketDbContext> options,
        ICurrentTenant? currentTenant = null)
        : base(options)
    {
        _currentTenant =
            currentTenant;
    }

    public bool HasCurrentTenant =>
        _currentTenant?.IsAvailable ==
        true;

    public TenantId CurrentTenantId =>
        _currentTenant?.TenantId
        ?? default;

    public DbSet<Tenant> Tenants =>
        Set<Tenant>();

    public DbSet<TenantDomain> TenantDomains =>
        Set<TenantDomain>();

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<UserMfa> UserMfas =>
        Set<UserMfa>();

    public DbSet<UserMfaRecoveryCode> UserMfaRecoveryCodes =>
        Set<UserMfaRecoveryCode>();

    public DbSet<MfaLoginChallenge> MfaLoginChallenges =>
        Set<MfaLoginChallenge>();

    public DbSet<TenantMembership> TenantMemberships =>
        Set<TenantMembership>();

    public DbSet<Category> Categories =>
        Set<Category>();

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        EnforceTenantWriteScope();

        return base.SaveChanges(
            acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceTenantWriteScope();

        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(
            modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MarketDbContext).Assembly);

        ApplyTenantDataQueryFilters(
            modelBuilder);
    }

    private void ApplyTenantDataQueryFilters(
        ModelBuilder modelBuilder)
    {
        var tenantScopedEntityTypes =
            modelBuilder.Model
                .GetEntityTypes()
                .Where(
                    entityType =>
                        typeof(ITenantDataScoped)
                            .IsAssignableFrom(
                                entityType.ClrType))
                .ToArray();

        foreach (var entityType in tenantScopedEntityTypes)
        {
            var filter =
                CreateTenantDataQueryFilter(
                    entityType.ClrType);

            modelBuilder
                .Entity(
                    entityType.ClrType)
                .HasQueryFilter(
                    filter);
        }
    }

    private LambdaExpression
        CreateTenantDataQueryFilter(
            Type entityType)
    {
        var entityParameter =
            Expression.Parameter(
                entityType,
                "entity");

        var tenantIdProperty =
            Expression.Property(
                entityParameter,
                nameof(
                    ITenantDataScoped.TenantId));

        var currentContext =
            Expression.Constant(
                this);

        var hasCurrentTenant =
            Expression.Property(
                currentContext,
                nameof(
                    HasCurrentTenant));

        var currentTenantId =
            Expression.Property(
                currentContext,
                nameof(
                    CurrentTenantId));

        Expression filterBody =
            Expression.AndAlso(
                hasCurrentTenant,
                Expression.Equal(
                    tenantIdProperty,
                    currentTenantId));

        if (typeof(ISoftDeletable)
            .IsAssignableFrom(
                entityType))
        {
            var isDeletedProperty =
                Expression.Property(
                    entityParameter,
                    nameof(
                        ISoftDeletable.IsDeleted));

            filterBody =
                Expression.AndAlso(
                    filterBody,
                    Expression.Not(
                        isDeletedProperty));
        }

        return Expression.Lambda(
            filterBody,
            entityParameter);
    }

    private void EnforceTenantWriteScope()
    {
        var tenantEntries =
            ChangeTracker
                .Entries()
                .Where(
                    entry =>
                        entry.Entity is
                            ITenantDataScoped &&
                        entry.State is
                            EntityState.Added or
                            EntityState.Modified or
                            EntityState.Deleted)
                .ToArray();

        if (tenantEntries.Length == 0)
        {
            return;
        }

        if (!HasCurrentTenant ||
            CurrentTenantId.IsEmpty)
        {
            throw new TenantScopeViolationException(
                "Tenant-scoped business data cannot be written without an active tenant context.");
        }

        foreach (var entry in tenantEntries)
        {
            var tenantEntity =
                (ITenantDataScoped)
                entry.Entity;

            if (tenantEntity.TenantId !=
                CurrentTenantId)
            {
                throw new TenantScopeViolationException(
                    "Cross-tenant data modification was blocked.");
            }

            if (entry.State ==
                EntityState.Modified)
            {
                var tenantProperty =
                    entry.Property(
                        nameof(
                            ITenantDataScoped.TenantId));

                if (tenantProperty.IsModified)
                {
                    throw new TenantScopeViolationException(
                        "The tenant ownership of an existing entity cannot be changed.");
                }
            }
        }
    }
}