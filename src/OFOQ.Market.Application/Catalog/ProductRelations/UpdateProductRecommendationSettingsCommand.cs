using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Catalog.ProductRelations;

public sealed record UpdateProductRecommendationSettingsCommand(
    bool IsEnabled,
    bool AutomaticSuggestionsEnabled,
    UserId ActorUserId);
