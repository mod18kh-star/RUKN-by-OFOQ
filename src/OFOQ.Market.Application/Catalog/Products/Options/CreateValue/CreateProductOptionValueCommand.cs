using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Products.Options.CreateValue;

public sealed record CreateProductOptionValueCommand(
    ProductId ProductId,
    ProductOptionId OptionId,
    string Value,
    int SortOrder,
    UserId ActorUserId);