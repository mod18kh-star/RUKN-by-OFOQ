using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Security;

public sealed class JwtAccessTokenService :
    IAccessTokenService
{
    private readonly JwtSettings _settings;

    public JwtAccessTokenService(
        JwtSettings settings)
    {
        _settings = settings;
    }

    public AccessTokenResult Create(
        UserId userId,
        string email,
        DateTimeOffset nowUtc)
    {
        var expiresAtUtc =
            nowUtc.AddMinutes(
                _settings.AccessTokenMinutes);

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    userId.Value.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    email),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

        var securityKey =
            new SymmetricSecurityKey(
                _settings.GetSigningKeyBytes());

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: nowUtc.UtcDateTime,
                expires: expiresAtUtc.UtcDateTime,
                signingCredentials: credentials);

        var value =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new AccessTokenResult(
            value,
            expiresAtUtc);
    }
}