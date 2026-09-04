using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    CategoryId? ParentCategoryId,
    int SortOrder,
    UserId ActorUserId);