using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Application.Commerce.Configuration.ConfigureVertical;

public sealed record ConfigureCommerceVerticalCommand(
    CommerceVerticalType VerticalType,
    bool IsEnabled,
    bool IsPrimary,
    Guid ActorUserId);