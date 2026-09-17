namespace OFOQ.Market.Application.Catalog.ProductRelations;

public sealed record ProductRecommendationSettingsResult(
    bool IsEnabled,
    bool AutomaticSuggestionsEnabled);
