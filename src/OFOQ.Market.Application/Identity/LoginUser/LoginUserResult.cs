using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Identity.LoginUser;

public sealed record LoginUserResult(
    UserId UserId,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);