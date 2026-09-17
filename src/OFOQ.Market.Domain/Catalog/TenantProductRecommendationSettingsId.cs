namespace OFOQ.Market.Domain.Catalog;

public readonly record struct TenantProductRecommendationSettingsId(
    Guid Value)
{
    public static TenantProductRecommendationSettingsId New() =>
        new(
            Guid.NewGuid());

    public static TenantProductRecommendationSettingsId From(
        Guid value)
    {
        if (value ==
            Guid.Empty)
        {
            throw new ArgumentException(
                "Recommendation settings ID cannot be empty.",
                nameof(value));
        }

        return new TenantProductRecommendationSettingsId(
            value);
    }

    public bool IsEmpty =>
        Value == Guid.Empty;
}
