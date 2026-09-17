using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.CurrentUserContext;

public sealed record GetCurrentUserContextQuery(
    UserId UserId);
