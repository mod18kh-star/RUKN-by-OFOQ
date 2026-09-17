using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Commerce.Fulfillment;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Commerce.Discounts;
using OFOQ.Market.Domain.Commerce.Configuration;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Returns;
using OFOQ.Market.Domain.Commerce.Reviews;
using OFOQ.Market.Domain.Commerce.Payments;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Content;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Notifications;
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

    // -------------------------------------------------
    // Tenancy
    // -------------------------------------------------

    public DbSet<Tenant> Tenants =>
        Set<Tenant>();

    public DbSet<TenantDomain> TenantDomains =>
        Set<TenantDomain>();

    public DbSet<TenantMembership> TenantMemberships =>
        Set<TenantMembership>();

    public DbSet<TenantStoreProfile> TenantStoreProfiles =>
        Set<TenantStoreProfile>();

    public DbSet<TenantStorefrontPresentation>
        TenantStorefrontPresentations =>
            Set<TenantStorefrontPresentation>();

    public DbSet<TenantStoreSocialLink> TenantStoreSocialLinks =>
        Set<TenantStoreSocialLink>();

    // -------------------------------------------------
    // Identity
    // -------------------------------------------------

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<UserMfa> UserMfas =>
        Set<UserMfa>();

    public DbSet<UserMfaRecoveryCode> UserMfaRecoveryCodes =>
        Set<UserMfaRecoveryCode>();

    public DbSet<MfaLoginChallenge> MfaLoginChallenges =>
        Set<MfaLoginChallenge>();

    public DbSet<EmailVerificationChallenge>
        EmailVerificationChallenges =>
            Set<EmailVerificationChallenge>();

    public DbSet<UserSession> UserSessions =>
        Set<UserSession>();

    public DbSet<UserExternalLogin> UserExternalLogins =>
        Set<UserExternalLogin>();

    public DbSet<UserTrustedDevice> UserTrustedDevices =>
        Set<UserTrustedDevice>();

    public DbSet<PlatformUserRoleAssignment>
        PlatformUserRoleAssignments =>
            Set<PlatformUserRoleAssignment>();

    // -------------------------------------------------
    // Catalog
    // -------------------------------------------------

    public DbSet<Category> Categories =>
        Set<Category>();

    public DbSet<Product> Products =>
        Set<Product>();

    public DbSet<ProductVariant> ProductVariants =>
        Set<ProductVariant>();

    public DbSet<ProductOption> ProductOptions =>
        Set<ProductOption>();

    public DbSet<ProductOptionValue> ProductOptionValues =>
        Set<ProductOptionValue>();

    public DbSet<ProductVariantOptionValue> ProductVariantOptionValues =>
        Set<ProductVariantOptionValue>();
    public DbSet<ProductAttributeValue> ProductAttributeValues =>
        Set<ProductAttributeValue>();
    public DbSet<ProductImage> ProductImages =>
        Set<ProductImage>();

    public DbSet<ProductContentBlock> ProductContentBlocks =>
        Set<ProductContentBlock>();

    public DbSet<ProductRelation> ProductRelations =>
        Set<ProductRelation>();

    public DbSet<TenantProductRecommendationSettings>
        TenantProductRecommendationSettings =>
            Set<TenantProductRecommendationSettings>();

    // -------------------------------------------------
    // Content
    // -------------------------------------------------

    public DbSet<ContentPage> ContentPages =>
        Set<ContentPage>();

    public DbSet<NavigationItem> NavigationItems =>
        Set<NavigationItem>();

    // -------------------------------------------------
    // Notifications
    // -------------------------------------------------

    public DbSet<TenantNotificationPreferences>
        TenantNotificationPreferences =>
            Set<TenantNotificationPreferences>();

    public DbSet<EmailOutboxMessage>
        EmailOutboxMessages =>
            Set<EmailOutboxMessage>();

    // -------------------------------------------------
    // Commerce / Configuration
    // -------------------------------------------------

    public DbSet<TenantCommerceVertical> TenantCommerceVerticals =>
        Set<TenantCommerceVertical>();

    public DbSet<TenantCommerceCapabilityOverride>
        TenantCommerceCapabilityOverrides =>
            Set<TenantCommerceCapabilityOverride>();

    // -------------------------------------------------
    // Commerce / Merchant Verification
    // -------------------------------------------------

    public DbSet<MerchantVerificationProfile>
        MerchantVerificationProfiles =>
            Set<MerchantVerificationProfile>();

    public DbSet<MerchantVerificationDocument>
        MerchantVerificationDocuments =>
            Set<MerchantVerificationDocument>();

    public DbSet<MerchantVerificationDocumentFile>
        MerchantVerificationDocumentFiles =>
            Set<MerchantVerificationDocumentFile>();

    // -------------------------------------------------
    // Commerce
    // -------------------------------------------------

    public DbSet<CustomerProfile> CustomerProfiles =>
        Set<CustomerProfile>();

    public DbSet<CustomerAddress> CustomerAddresses =>
        Set<CustomerAddress>();

    public DbSet<DiscountCoupon> DiscountCoupons =>
        Set<DiscountCoupon>();

    public DbSet<DiscountCouponProductTarget> DiscountCouponProductTargets =>
        Set<DiscountCouponProductTarget>();

    public DbSet<DiscountCouponCategoryTarget> DiscountCouponCategoryTargets =>
        Set<DiscountCouponCategoryTarget>();

    public DbSet<CouponRedemption> CouponRedemptions =>
        Set<CouponRedemption>();

    public DbSet<FulfillmentLocation> FulfillmentLocations =>
        Set<FulfillmentLocation>();

    public DbSet<ShippingMethod> ShippingMethods =>
        Set<ShippingMethod>();

    public DbSet<Cart> Carts =>
        Set<Cart>();

    public DbSet<CartItem> CartItems =>
        Set<CartItem>();

    public DbSet<Order> Orders =>
        Set<Order>();

    public DbSet<OrderItem> OrderItems =>
        Set<OrderItem>();

    public DbSet<OrderTimelineEntry> OrderTimelineEntries =>
        Set<OrderTimelineEntry>();

    public DbSet<InventoryMovement> InventoryMovements =>
        Set<InventoryMovement>();

    public DbSet<ReturnRequest> ReturnRequests =>
        Set<ReturnRequest>();

    public DbSet<ReturnRequestItem> ReturnRequestItems =>
        Set<ReturnRequestItem>();

    public DbSet<ProductReview> ProductReviews =>
        Set<ProductReview>();

    public DbSet<TenantTrustMetricSettings> TenantTrustMetricSettings =>
        Set<TenantTrustMetricSettings>();

    // -------------------------------------------------
    // Commerce / Payments
    // -------------------------------------------------

    public DbSet<Payment> Payments =>
        Set<Payment>();

    public DbSet<PaymentIntent> PaymentIntents =>
        Set<PaymentIntent>();

    public DbSet<PaymentTransaction> PaymentTransactions =>
        Set<PaymentTransaction>();

    public DbSet<TenantPaymentMethod> TenantPaymentMethods =>
        Set<TenantPaymentMethod>();

    public DbSet<TenantPaymentCapability> TenantPaymentCapabilities =>
        Set<TenantPaymentCapability>();

    // -------------------------------------------------
    // Commerce / Payment Provider Accounts
    // -------------------------------------------------

    public DbSet<TenantPaymentProviderAccount> TenantPaymentProviderAccounts =>
        Set<TenantPaymentProviderAccount>();

    public DbSet<TenantPaymentWalletCapability> TenantPaymentWalletCapabilities =>
        Set<TenantPaymentWalletCapability>();

    // -------------------------------------------------
    // Save changes
    // -------------------------------------------------

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

    // -------------------------------------------------
    // Model
    // -------------------------------------------------

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

    // -------------------------------------------------
    // Tenant query filters
    // -------------------------------------------------

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

    private LambdaExpression CreateTenantDataQueryFilter(
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

    // -------------------------------------------------
    // Tenant write protection
    // -------------------------------------------------

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
