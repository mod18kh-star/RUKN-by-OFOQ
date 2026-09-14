using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.ProductAttributes;

public sealed record ProductAttributeInput(
    string Key,
    string? Value);

public sealed record SetProductAttributesCommand(
    ProductId ProductId,
    IReadOnlyCollection<ProductAttributeInput> Values,
    UserId ActorUserId);