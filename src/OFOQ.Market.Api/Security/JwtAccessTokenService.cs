using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Security;

public sealed class JwtAccessTokenService :
    IAccessTokenService,
    ISessionAccessTokenService
{
    private const string AuthenticationMethodClaim =
        "amr";

    private const string PasswordAuthenticationMethod =
        "pwd";

    private const string GoogleAuthenticationMethod =
        "google";

    private const string MultiFactorAuthenticationMethod =
        "mfa";

    private readonly JwtSettings _settings;

    public JwtAccessTokenService(
        JwtSettings settings)
    {
        _settings =
            settings;
    }

    public AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc,
        AccessTokenAuthenticationLevel authenticationLevel)
    {
        return CreateCore(
            userId,
            email,
            nowUtc,
            authenticationLevel,
            UserSessionAuthenticationMethod.Password,
            sessionId: null);
    }

    public AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc,
        AccessTokenAuthenticationLevel authenticationLevel,
        UserSessionAuthenticationMethod authenticationMethod)
    {
        return CreateCore(
            userId,
            email,
            nowUtc,
            authenticationLevel,
            authenticationMethod,
            sessionId: null);
    }

    public AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc,
        AccessTokenAuthenticationLevel authenticationLevel,
        UserSessionAuthenticationMethod authenticationMethod,
        UserSessionId sessionId)
    {
        if (sessionId.IsEmpty)
        {
            throw new ArgumentException(
                "User session ID cannot be empty.",
                nameof(sessionId));
        }

        return CreateCore(
            userId,
            email,
            nowUtc,
            authenticationLevel,
            authenticationMethod,
            sessionId);
    }

    private AccessTokenResult CreateCore(
        UserId userId,
        string email,
        DateTimeOffset nowUtc,
        AccessTokenAuthenticationLevel authenticationLevel,
        UserSessionAuthenticationMethod authenticationMethod,
        UserSessionId? sessionId)
    {
        if (userId.IsEmpty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(
                email))
        {
            throw new ArgumentException(
                "Email is required.",
                nameof(email));
        }

        if (!Enum.IsDefined(
                authenticationMethod))
        {
            throw new ArgumentOutOfRangeException(
                nameof(authenticationMethod));
        }

        var expiresAtUtc =
            nowUtc.AddMinutes(
                _settings.AccessTokenMinutes);

        var claims =
            new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    userId.Value.ToString()),

                new(
                    JwtRegisteredClaimNames.Email,
                    email),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new(
                    AuthenticationMethodClaim,
                    authenticationMethod switch
                    {
                        UserSessionAuthenticationMethod.Password =>
                            PasswordAuthenticationMethod,

                        UserSessionAuthenticationMethod.Google =>
                            GoogleAuthenticationMethod,

                        _ =>
                            throw new ArgumentOutOfRangeException(
                                nameof(authenticationMethod))
                    })
            };

        if (sessionId.HasValue)
        {
            claims.Add(
                new Claim(
                    "sid",
                    sessionId.Value.Value.ToString()));
        }

        switch (authenticationLevel)
        {
            case AccessTokenAuthenticationLevel.PasswordOnly:
                break;

            case AccessTokenAuthenticationLevel.MultiFactor:
                claims.Add(
                    new Claim(
                        AuthenticationMethodClaim,
                        MultiFactorAuthenticationMethod));
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(authenticationLevel),
                    authenticationLevel,
                    "Unsupported authentication level.");
        }

        var securityKey =
            new SymmetricSecurityKey(
                _settings.GetSigningKeyBytes());

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer:
                    _settings.Issuer,

                audience:
                    _settings.Audience,

                claims:
                    claims,

                notBefore:
                    nowUtc.UtcDateTime,

                expires:
                    expiresAtUtc.UtcDateTime,

                signingCredentials:
                    credentials);

        var value =
            new JwtSecurityTokenHandler()
                .WriteToken(
                    token);

        return new AccessTokenResult(
            value,
            expiresAtUtc);
    }
}
