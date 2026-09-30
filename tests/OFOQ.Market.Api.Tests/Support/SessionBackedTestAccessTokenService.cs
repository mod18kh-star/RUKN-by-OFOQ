using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Tests.Support;

internal interface ITestAccessTokenService
{
    AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc,
        AccessTokenAuthenticationLevel authenticationLevel);
}

internal sealed class SessionBackedTestAccessTokenService :
    ITestAccessTokenService
{
    private static readonly TimeSpan SessionLifetime =
        TimeSpan.FromDays(30);

    private readonly IUserSessionRepository _sessionRepository;
    private readonly ISessionAccessTokenService _sessionAccessTokenService;

    public SessionBackedTestAccessTokenService(
        IUserSessionRepository sessionRepository,
        ISessionAccessTokenService sessionAccessTokenService)
    {
        _sessionRepository =
            sessionRepository;

        _sessionAccessTokenService =
            sessionAccessTokenService;
    }

    public AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc,
        AccessTokenAuthenticationLevel authenticationLevel)
    {
        var sessionLevel =
            authenticationLevel switch
            {
                AccessTokenAuthenticationLevel.PasswordOnly =>
                    UserSessionAuthenticationLevel.PasswordOnly,

                AccessTokenAuthenticationLevel.MultiFactor =>
                    UserSessionAuthenticationLevel.MultiFactor,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(authenticationLevel))
            };

        var session =
            UserSession.Create(
                userId,
                $"test-refresh-{Guid.NewGuid():N}",
                sessionLevel,
                nowUtc.Add(
                    SessionLifetime),
                nowUtc,
                "127.0.0.1",
                "RUKN API test",
                UserSessionAuthenticationMethod.Password);

        _sessionRepository
            .AddAsync(
                session)
            .GetAwaiter()
            .GetResult();

        return _sessionAccessTokenService.Create(
            userId,
            email,
            nowUtc,
            authenticationLevel,
            UserSessionAuthenticationMethod.Password,
            session.Id);
    }
}