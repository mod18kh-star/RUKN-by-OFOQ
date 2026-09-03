using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence;

public sealed class MarketDbContext :
    DbContext,
    IUnitOfWork
{
    public MarketDbContext(
        DbContextOptions<MarketDbContext> options)
        : base(options)
    {
    }

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

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(
            modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MarketDbContext).Assembly);
    }
}