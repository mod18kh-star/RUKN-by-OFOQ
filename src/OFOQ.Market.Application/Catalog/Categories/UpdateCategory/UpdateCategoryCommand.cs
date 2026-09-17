using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(
    CategoryId CategoryId,
    string Name,
    string Slug,
    bool IsVisible,
    UserId ActorUserId,
    string? ImageUrl = null);
