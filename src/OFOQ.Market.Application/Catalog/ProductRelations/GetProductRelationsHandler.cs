using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Domain.Catalog;

namespace OFOQ.Market.Application.Catalog.ProductRelations;

public sealed class GetProductRelationsHandler
{
    private readonly IProductRepository _products;
    private readonly IProductRelationRepository _relations;
    private readonly ITenantProductRecommendationSettingsRepository _settings;

    public GetProductRelationsHandler(
        IProductRepository products,
        IProductRelationRepository relations,
        ITenantProductRecommendationSettingsRepository settings)
    {
        _products = products;
        _relations = relations;
        _settings = settings;
    }

    public async Task<ProductRelationsResult?> HandleAsync(
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        if (await _products.GetByIdAsync(productId, cancellationToken) is null)
            return null;

        var relations =
            await _relations.GetBySourceProductIdAsync(
                productId,
                cancellationToken);

        var mapped = new List<ProductRelationResult>();

        foreach (var relation in relations)
        {
            var target =
                await _products.GetByIdAsync(
                    relation.TargetProductId,
                    cancellationToken);

            if (target is null)
                continue;

            mapped.Add(
                new ProductRelationResult(
                    relation.Id.Value,
                    relation.Type.ToString(),
                    relation.TargetProductId.Value,
                    target.Name,
                    target.Slug,
                    relation.SortOrder,
                    relation.IsVisible));
        }

        var settings =
            await _settings.GetAsync(
                cancellationToken);

        return new ProductRelationsResult(
            productId.Value,
            settings?.IsEnabled ?? true,
            settings?.AutomaticSuggestionsEnabled ?? false,
            mapped);
    }
}
