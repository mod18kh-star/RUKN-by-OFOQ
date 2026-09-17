using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Application.Common.Security;

public enum AccessTokenAuthenticationLevel
{
    PasswordOnly = 0,
    MultiFactor = 1
}

public sealed record AccessTokenResult(
    string Token,
    DateTimeOffset ExpiresAtUtc);

public interface IAccessTokenService
{
    AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc,
        AccessTokenAuthenticationLevel authenticationLevel);

    AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc,
        AccessTokenAuthenticationLevel authenticationLevel,
        UserSessionAuthenticationMethod authenticationMethod)
    {
        if (authenticationMethod != UserSessionAuthenticationMethod.Password)
        {
            throw new NotSupportedException(
                "This access-token service does not support external authentication methods.");
        }

        return Create(
            userId,
            email,
            nowUtc,
            authenticationLevel);
    }
}