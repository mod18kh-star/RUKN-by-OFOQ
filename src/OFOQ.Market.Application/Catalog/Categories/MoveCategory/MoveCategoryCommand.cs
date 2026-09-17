using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Categories.MoveCategory;

public sealed record MoveCategoryCommand(
    CategoryId CategoryId,
    CategoryId? ParentCategoryId,
    int? Position,
    UserId ActorUserId);
