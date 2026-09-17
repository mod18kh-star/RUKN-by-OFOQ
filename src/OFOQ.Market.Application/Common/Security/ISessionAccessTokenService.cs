using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Security;

public interface ISessionAccessTokenService
{
    AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc,
        AccessTokenAuthenticationLevel authenticationLevel,
        UserSessionAuthenticationMethod authenticationMethod,
        UserSessionId sessionId);
}
