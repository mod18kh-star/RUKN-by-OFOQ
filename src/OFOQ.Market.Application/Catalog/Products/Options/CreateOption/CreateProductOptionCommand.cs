using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Products.Options.CreateOption;

public sealed record CreateProductOptionCommand(
    ProductId ProductId,
    string Name,
    int SortOrder,
    UserId ActorUserId);