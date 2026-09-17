using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class TenantProductRecommendationSettings :
    Entity<TenantProductRecommendationSettingsId>,
    ITenantDataScoped,
    IAuditable
{
    private TenantProductRecommendationSettings()
    {
    }

    private TenantProductRecommendationSettings(
        TenantProductRecommendationSettingsId id,
        TenantId tenantId,
        bool isEnabled,
        bool automaticSuggestionsEnabled,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        IsEnabled =
            isEnabled;

        AutomaticSuggestionsEnabled =
            automaticSuggestionsEnabled;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public bool IsEnabled { get; private set; }

    public bool AutomaticSuggestionsEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static TenantProductRecommendationSettings Create(
        TenantId tenantId,
        bool isEnabled,
        bool automaticSuggestionsEnabled,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        return new TenantProductRecommendationSettings(
            TenantProductRecommendationSettingsId.New(),
            tenantId,
            isEnabled,
            automaticSuggestionsEnabled,
            createdAtUtc,
            createdByUserId);
    }

    public void Update(
        bool isEnabled,
        bool automaticSuggestionsEnabled,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (IsEnabled ==
                isEnabled &&
            AutomaticSuggestionsEnabled ==
                automaticSuggestionsEnabled)
        {
            return;
        }

        IsEnabled =
            isEnabled;

        AutomaticSuggestionsEnabled =
            automaticSuggestionsEnabled;

        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }
}
