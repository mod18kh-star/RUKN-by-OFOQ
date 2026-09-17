using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Application.Tenancy.StoreReadiness;

public static class StoreReadinessCalculator
{
    public static StoreReadinessResult Calculate(
        Tenant tenant,
        TenantStoreProfile? profile,
        StoreReadinessData data)
    {
        ArgumentNullException.ThrowIfNull(
            tenant);

        ArgumentNullException.ThrowIfNull(
            data);

        var hasCustomerContact =
            !string.IsNullOrWhiteSpace(
                profile?.WhatsAppNumber) ||
            !string.IsNullOrWhiteSpace(
                profile?.CustomerServicePhone);

        var hasDigitalPresence =
            !string.IsNullOrWhiteSpace(
                profile?.WebsiteUrl) ||
            data.VisibleSocialLinkCount >
                0;

        var hasBusinessIdentity =
            !string.IsNullOrWhiteSpace(
                profile?.CommercialRegistrationNumber) ||
            profile?.CommercialRegistrationNotApplicable ==
                true;

        var items =
            new[]
            {
                new StoreReadinessItemResult(
                    "store-identity",
                    10,
                    !string.IsNullOrWhiteSpace(
                        tenant.Name) &&
                    !string.IsNullOrWhiteSpace(
                        tenant.Slug.Value),
                    true),

                new StoreReadinessItemResult(
                    "primary-activity",
                    10,
                    data.HasPrimaryVertical,
                    true),

                new StoreReadinessItemResult(
                    "customer-contact",
                    15,
                    hasCustomerContact,
                    true),

                new StoreReadinessItemResult(
                    "digital-presence",
                    10,
                    hasDigitalPresence,
                    true),

                new StoreReadinessItemResult(
                    "business-identity",
                    5,
                    hasBusinessIdentity,
                    true),

                new StoreReadinessItemResult(
                    "first-product",
                    20,
                    data.ProductCount >
                        0,
                    true),

                new StoreReadinessItemResult(
                    "published-product",
                    20,
                    data.PublishedProductCount >
                        0,
                    true),

                new StoreReadinessItemResult(
                    "store-activation",
                    10,
                    tenant.Status ==
                        TenantStatus.Active,
                    false)
            };

        var percentage =
            items
                .Where(
                    item =>
                        item.Completed)
                .Sum(
                    item =>
                        item.Weight);

        var state =
            ResolveState(
                tenant.Status,
                percentage,
                items);

        return new StoreReadinessResult(
            percentage,
            state,
            tenant.Status.ToString(),
            items);
    }

    private static string ResolveState(
        TenantStatus tenantStatus,
        int percentage,
        IReadOnlyCollection<StoreReadinessItemResult> items)
    {
        if (tenantStatus ==
            TenantStatus.Suspended)
        {
            return "Suspended";
        }

        if (tenantStatus ==
            TenantStatus.Active &&
            percentage ==
                100)
        {
            return "Complete";
        }

        if (tenantStatus ==
            TenantStatus.Active)
        {
            return "NeedsAttention";
        }

        var merchantWorkComplete =
            items
                .Where(
                    item =>
                        item.MerchantActionRequired)
                .All(
                    item =>
                        item.Completed);

        if (tenantStatus ==
                TenantStatus.Draft &&
            merchantWorkComplete)
        {
            return "ReadyForReview";
        }

        if (percentage >=
            70)
        {
            return "NearlyReady";
        }

        return "NeedsSetup";
    }
}
