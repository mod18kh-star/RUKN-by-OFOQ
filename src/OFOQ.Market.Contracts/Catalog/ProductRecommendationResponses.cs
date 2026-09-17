namespace OFOQ.Market.Contracts.Catalog;

public sealed record ProductRelationResponse(
    Guid RelationId,
    string Type,
    Guid TargetProductId,
    string TargetProductName,
    string TargetProductSlug,
    int SortOrder,
    bool IsVisible);

public sealed record ProductRelationsResponse(
    Guid ProductId,
    bool RecommendationsEnabled,
    bool AutomaticSuggestionsEnabled,
    IReadOnlyList<ProductRelationResponse> Relations);

public sealed record SetProductRelationRequest(
    Guid TargetProductId,
    bool IsVisible);

public sealed record SetProductRelationsRequest(
    IReadOnlyCollection<SetProductRelationRequest> Relations);

public sealed record ProductRecommendationSettingsResponse(
    bool IsEnabled,
    bool AutomaticSuggestionsEnabled);

public sealed record UpdateProductRecommendationSettingsRequest(
    bool IsEnabled,
    bool AutomaticSuggestionsEnabled);
