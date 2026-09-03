using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Security;

public sealed record AccessTokenResult(
    string Token,
    DateTimeOffset ExpiresAtUtc);

public interface IAccessTokenService
{
    AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc);
}