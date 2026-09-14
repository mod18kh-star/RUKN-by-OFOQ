using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class MerchantVerificationReviewRepositoryIntegrationTests
{
    private readonly IntegrationTestDatabase
        _database =
            IntegrationTestDatabase.Create();

    [Fact]
    public async Task GetProfileByIdAsync_WithoutTenantContext_DiscoversAndLoadsProfile()
    {
        await _database.ResetAsync();

        var seed =
            await CreateSubmittedVerificationSeedAsync(
                "Review Discovery Store");

        /*
         * Ordinary tenant-filtered access without a tenant context
         * must still reveal nothing.
         */
        await using (
            var noTenantContext =
                _database.CreateContext())
        {
            var ordinaryProfiles =
                await noTenantContext
                    .MerchantVerificationProfiles
                    .ToArrayAsync();

            Assert.Empty(
                ordinaryProfiles);
        }

        await using var reviewScope =
            CreateReviewRepositoryScope();

        var profile =
            await reviewScope
                .Repository
                .GetProfileByIdAsync(
                    seed.Profile.Id);

        Assert.NotNull(
            profile);

        Assert.Equal(
            seed.Profile.Id,
            profile!.Id);

        Assert.Equal(
            seed.Tenant.Id,
            profile.TenantId);

        Assert.Equal(
            seed.PrincipalUser.Id,
            profile.PrincipalUserId);

        Assert.Equal(
            MerchantVerificationStatus.Submitted,
            profile.Status);
    }

    [Fact]
    public async Task GetProfileByIdAsync_UnknownProfile_ReturnsNull()
    {
        await _database.ResetAsync();

        await CreateSubmittedVerificationSeedAsync(
            "Unknown Review Profile Store");

        await using var reviewScope =
            CreateReviewRepositoryScope();

        var profile =
            await reviewScope
                .Repository
                .GetProfileByIdAsync(
                    MerchantVerificationProfileId.New());

        Assert.Null(
            profile);
    }

    [Fact]
    public async Task HasActiveTenantMembershipAsync_ReflectsLiveSoftDeleteState()
    {
        await _database.ResetAsync();

        var seed =
            await CreateSubmittedVerificationSeedAsync(
                "Review Conflict Store");

        var membership =
            TenantMembership.Create(
                seed.Tenant.Id,
                seed.ReviewerUser.Id,
                TenantRole.Staff,
                DateTimeOffset.UtcNow,
                seed.ReviewerUser.Id.Value);

        await using (
            var membershipContext =
                _database.CreateContext(
                    new TestCurrentTenant(
                        seed.Tenant.Id)))
        {
            membershipContext
                .TenantMemberships
                .Add(
                    membership);

            await membershipContext
                .SaveChangesAsync();
        }

        await using var reviewScope =
            CreateReviewRepositoryScope();

        var activeBeforeDelete =
            await reviewScope
                .Repository
                .HasActiveTenantMembershipAsync(
                    seed.Tenant.Id,
                    seed.ReviewerUser.Id);

        Assert.True(
            activeBeforeDelete);

        await using (
            var deleteContext =
                _database.CreateContext(
                    new TestCurrentTenant(
                        seed.Tenant.Id)))
        {
            var storedMembership =
                await deleteContext
                    .TenantMemberships
                    .SingleAsync(
                        item =>
                            item.Id ==
                            membership.Id);

            storedMembership.Delete(
                DateTimeOffset.UtcNow,
                seed.ReviewerUser.Id.Value);

            await deleteContext
                .SaveChangesAsync();
        }

        var activeAfterDelete =
            await reviewScope
                .Repository
                .HasActiveTenantMembershipAsync(
                    seed.Tenant.Id,
                    seed.ReviewerUser.Id);

        Assert.False(
            activeAfterDelete);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsReviewOnlyForDiscoveredTenant()
    {
        await _database.ResetAsync();

        var first =
            await CreateSubmittedVerificationSeedAsync(
                "Review Tenant A");

        var second =
            await CreateSubmittedVerificationSeedAsync(
                "Review Tenant B");

        await using var reviewScope =
            CreateReviewRepositoryScope();

        var profile =
            await reviewScope
                .Repository
                .GetProfileByIdAsync(
                    first.Profile.Id);

        Assert.NotNull(
            profile);

        var reviewedAtUtc =
            DateTimeOffset.UtcNow;

        profile!.StartReview(
            reviewedAtUtc,
            first.ReviewerUser.Id.Value);

        var affectedRows =
            await reviewScope
                .Repository
                .SaveChangesAsync();

        Assert.True(
            affectedRows > 0);

        await using (
            var firstTenantContext =
                _database.CreateContext(
                    new TestCurrentTenant(
                        first.Tenant.Id)))
        {
            var storedFirst =
                await firstTenantContext
                    .MerchantVerificationProfiles
                    .SingleAsync(
                        item =>
                            item.Id ==
                            first.Profile.Id);

            Assert.Equal(
                MerchantVerificationStatus.UnderReview,
                storedFirst.Status);

            Assert.Equal(
                first.ReviewerUser.Id.Value,
                storedFirst.ReviewedByUserId);

            Assert.NotNull(
                storedFirst.ReviewStartedAtUtc);
        }

        await using (
            var secondTenantContext =
                _database.CreateContext(
                    new TestCurrentTenant(
                        second.Tenant.Id)))
        {
            var storedSecond =
                await secondTenantContext
                    .MerchantVerificationProfiles
                    .SingleAsync(
                        item =>
                            item.Id ==
                            second.Profile.Id);

            Assert.Equal(
                MerchantVerificationStatus.Submitted,
                storedSecond.Status);

            Assert.Null(
                storedSecond.ReviewStartedAtUtc);

            Assert.Null(
                storedSecond.ReviewedByUserId);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutLoadedTenantScopedProfile_IsRejected()
    {
        await _database.ResetAsync();

        await using var reviewScope =
            CreateReviewRepositoryScope();

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    reviewScope
                        .Repository
                        .SaveChangesAsync());

        Assert.Contains(
            "tenant-scoped",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task<VerificationSeed>
        CreateSubmittedVerificationSeedAsync(
            string storeName)
    {
        var suffix =
            Guid.NewGuid()
                .ToString(
                    "N");

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                storeName,
                $"review-{suffix}",
                now);

        var principalUser =
            User.Create(
                $"principal-{suffix}@example.com",
                $"principal-password-hash-{suffix}",
                now);

        var reviewerUser =
            User.Create(
                $"reviewer-{suffix}@example.com",
                $"reviewer-password-hash-{suffix}",
                now);

        await using (
            var identityContext =
                _database.CreateContext())
        {
            identityContext
                .Tenants
                .Add(
                    tenant);

            identityContext
                .Users
                .AddRange(
                    principalUser,
                    reviewerUser);

            await identityContext
                .SaveChangesAsync();
        }

        var profile =
            MerchantVerificationProfile.Create(
                tenant.Id,
                principalUser.Id,
                MerchantVerificationSubjectType.Individual,
                "SA",
                $"{storeName} Legal Owner",
                now,
                principalUser.Id.Value);

        profile.Submit(
            now.AddSeconds(1),
            principalUser.Id.Value);

        await using (
            var profileContext =
                _database.CreateContext(
                    new TestCurrentTenant(
                        tenant.Id)))
        {
            profileContext
                .MerchantVerificationProfiles
                .Add(
                    profile);

            await profileContext
                .SaveChangesAsync();
        }

        return new VerificationSeed(
            tenant,
            principalUser,
            reviewerUser,
            profile);
    }

    private ReviewRepositoryScope
        CreateReviewRepositoryScope()
    {
        using var templateContext =
            _database.CreateContext();

        var connectionString =
            templateContext
                .Database
                .GetConnectionString();

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            throw new InvalidOperationException(
                "The integration test database connection string is unavailable.");
        }

        var services =
            new ServiceCollection();

        services.AddInfrastructure(
            connectionString);

        var serviceProvider =
            services.BuildServiceProvider();

        var serviceScope =
            serviceProvider
                .CreateAsyncScope();

        var repository =
            serviceScope
                .ServiceProvider
                .GetRequiredService<
                    IMerchantVerificationReviewRepository>();

        return new ReviewRepositoryScope(
            serviceProvider,
            serviceScope,
            repository);
    }

    private sealed record VerificationSeed(
        Tenant Tenant,
        User PrincipalUser,
        User ReviewerUser,
        MerchantVerificationProfile Profile);

    private sealed class ReviewRepositoryScope :
        IAsyncDisposable
    {
        private readonly ServiceProvider
            _serviceProvider;

        private readonly AsyncServiceScope
            _serviceScope;

        public ReviewRepositoryScope(
            ServiceProvider serviceProvider,
            AsyncServiceScope serviceScope,
            IMerchantVerificationReviewRepository repository)
        {
            _serviceProvider =
                serviceProvider;

            _serviceScope =
                serviceScope;

            Repository =
                repository;
        }

        public IMerchantVerificationReviewRepository
            Repository { get; }

        public async ValueTask DisposeAsync()
        {
            await _serviceScope
                .DisposeAsync();

            await _serviceProvider
                .DisposeAsync();
        }
    }
}
