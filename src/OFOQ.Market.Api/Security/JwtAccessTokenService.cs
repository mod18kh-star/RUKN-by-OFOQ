using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Security;

public sealed class JwtAccessTokenService :
    IAccessTokenService
{
    private const string AuthenticationMethodClaim =
        "amr";

    private const string PasswordAuthenticationMethod =
        "pwd";

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
                    PasswordAuthenticationMethod)
            };

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