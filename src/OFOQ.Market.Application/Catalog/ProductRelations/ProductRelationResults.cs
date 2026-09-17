namespace OFOQ.Market.Application.Catalog.ProductRelations;

public sealed record ProductRelationResult(
    Guid RelationId,
    string Type,
    Guid TargetProductId,
    string TargetProductName,
    string TargetProductSlug,
    int SortOrder,
    bool IsVisible);

public sealed record ProductRelationsResult(
    Guid ProductId,
    bool RecommendationsEnabled,
    bool AutomaticSuggestionsEnabled,
    IReadOnlyList<ProductRelationResult> Relations);
