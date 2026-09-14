using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Commerce.Verification.Review;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Repositories;

internal sealed class MerchantVerificationReviewRepository :
    IMerchantVerificationReviewRepository,
    IAsyncDisposable
{
    private readonly MarketDbContext
        _discoveryContext;

    private readonly DbContextOptions<MarketDbContext>
        _dbContextOptions;

    private MarketDbContext?
        _reviewContext;

    public MerchantVerificationReviewRepository(
        MarketDbContext discoveryContext,
        DbContextOptions<MarketDbContext> dbContextOptions)
    {
        _discoveryContext =
            discoveryContext;

        _dbContextOptions =
            dbContextOptions;
    }

    public async Task<MerchantVerificationProfile?>
        GetProfileByIdAsync(
            MerchantVerificationProfileId profileId,
            CancellationToken cancellationToken = default)
    {
        /*
         * Platform review starts without a tenant route context.
         *
         * The first query is discovery-only. It bypasses the tenant
         * query filter solely to discover which tenant owns the
         * requested verification profile.
         *
         * The tenant ID is never accepted from the caller.
         */
        var discoveredProfile =
            await _discoveryContext
                .MerchantVerificationProfiles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    profile =>
                        profile.Id == profileId,
                    cancellationToken);

        if (discoveredProfile is null)
        {
            await ResetReviewContextAsync();

            return null;
        }

        /*
         * Any previous tenant-scoped review context is discarded
         * before creating the context for the discovered tenant.
         */
        await ResetReviewContextAsync();

        _reviewContext =
            new MarketDbContext(
                _dbContextOptions,
                new ReviewTenantContext(
                    discoveredProfile.TenantId));

        /*
         * Reload through the normal tenant query filter.
         *
         * This second instance is tracked and is the only instance
         * that may later be modified and saved.
         */
        return await _reviewContext
            .MerchantVerificationProfiles
            .SingleOrDefaultAsync(
                profile =>
                    profile.Id == profileId,
                cancellationToken);
    }

    public Task<bool>
        HasActiveTenantMembershipAsync(
            TenantId tenantId,
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        /*
         * Conflict-of-interest checks are platform-level and must
         * inspect membership independently from the current route.
         *
         * IgnoreQueryFilters is used intentionally, while the
         * active-state condition is applied explicitly.
         */
        return _discoveryContext
            .TenantMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.TenantId == tenantId &&
                    membership.UserId == userId &&
                    !membership.IsDeleted,
                cancellationToken);
    }

    public async Task<int>
        SaveChangesAsync(
            CancellationToken cancellationToken = default)
    {
        if (_reviewContext is null)
        {
            throw new InvalidOperationException(
                "A tenant-scoped merchant verification profile must be loaded before review changes can be saved.");
        }

        /*
         * This deliberately uses the tenant-scoped review context.
         *
         * MarketDbContext.EnforceTenantWriteScope therefore remains
         * the final protection against cross-tenant writes.
         *
         * The profile is protected by PostgreSQL xmin optimistic
         * concurrency. If another reviewer updates the same row
         * after it was loaded, EF Core raises a concurrency error
         * instead of silently overwriting that change.
         */
        try
        {
            return await _reviewContext
                .SaveChangesAsync(
                    cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new MerchantVerificationReviewConcurrencyException(
                exception);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ResetReviewContextAsync();

        GC.SuppressFinalize(
            this);
    }

    private async ValueTask ResetReviewContextAsync()
    {
        if (_reviewContext is null)
        {
            return;
        }

        await _reviewContext
            .DisposeAsync();

        _reviewContext =
            null;
    }

    private sealed class ReviewTenantContext :
        ICurrentTenant
    {
        public ReviewTenantContext(
            TenantId tenantId)
        {
            if (tenantId.IsEmpty)
            {
                throw new ArgumentException(
                    "Tenant ID cannot be empty.",
                    nameof(tenantId));
            }

            TenantId =
                tenantId;
        }

        public TenantId?
            TenantId { get; }

        public bool IsAvailable =>
            TenantId.HasValue &&
            !TenantId.Value.IsEmpty;
    }
}
