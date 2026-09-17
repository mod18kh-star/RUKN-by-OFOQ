using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.ProductRelations;

public sealed record ProductRelationInput(
    ProductId TargetProductId,
    bool IsVisible);

public sealed record SetProductRelationsCommand(
    ProductId ProductId,
    ProductRelationType Type,
    IReadOnlyCollection<ProductRelationInput> Relations,
    UserId ActorUserId);
