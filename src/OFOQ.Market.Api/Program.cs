using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OFOQ.Market.Api.Endpoints.Identity;
using OFOQ.Market.Api.Endpoints.Tenancy;
using OFOQ.Market.Api.Security;
using OFOQ.Market.Application;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Infrastructure;

var builder =
    WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString(
        "MarketDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'MarketDatabase' was not found.");

var jwtIssuer =
    builder.Configuration[
        "Authentication:Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer was not configured.");

var jwtAudience =
    builder.Configuration[
        "Authentication:Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience was not configured.");

var jwtSigningKey =
    builder.Configuration[
        "Authentication:Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key was not configured.");

var accessTokenMinutesValue =
    builder.Configuration[
        "Authentication:Jwt:AccessTokenMinutes"];

if (!int.TryParse(
        accessTokenMinutesValue,
        out var accessTokenMinutes))
{
    throw new InvalidOperationException(
        "JWT access token lifetime was not configured correctly.");
}

var jwtSettings =
    new JwtSettings
    {
        Issuer = jwtIssuer,
        Audience = jwtAudience,
        SigningKey = jwtSigningKey,
        AccessTokenMinutes = accessTokenMinutes
    };

jwtSettings.Validate();

var signingKey =
    new SymmetricSecurityKey(
        jwtSettings.GetSigningKeyBytes());

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    connectionString);

builder.Services.AddSingleton(
    jwtSettings);

builder.Services.AddSingleton<
    IAccessTokenService,
    JwtAccessTokenService>();

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            // لا نكشف تفاصيل فشل JWT للعميل.
            options.IncludeErrorDetails =
                false;

            // نحافظ على أسماء Claims الأصلية مثل sub و email.
            options.MapInboundClaims =
                false;

            // لا حاجة لحفظ الـBearer Token داخل AuthenticationProperties.
            options.SaveToken =
                false;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer =
                        true,

                    ValidIssuer =
                        jwtSettings.Issuer,

                    ValidateAudience =
                        true,

                    ValidAudience =
                        jwtSettings.Audience,

                    ValidateIssuerSigningKey =
                        true,

                    IssuerSigningKey =
                        signingKey,

                    ValidateLifetime =
                        true,

                    RequireExpirationTime =
                        true,

                    RequireSignedTokens =
                        true,

                    ValidAlgorithms =
                    [
                        SecurityAlgorithms.HmacSha256
                    ],

                    // سماحية صغيرة فقط لفارق الوقت بين الأجهزة.
                    ClockSkew =
                        TimeSpan.FromSeconds(30),

                    NameClaimType =
                        JwtRegisteredClaimNames.Sub
                };
        });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(
    options =>
    {
        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;

        // حماية أولية ضد brute-force على تسجيل الدخول.
        options.AddPolicy(
            "auth-login",
            httpContext =>
                RateLimitPartition
                    .GetSlidingWindowLimiter(
                        partitionKey:
                            httpContext.Connection
                                .RemoteIpAddress?
                                .ToString()
                            ?? "unknown",

                        factory:
                            _ =>
                                new SlidingWindowRateLimiterOptions
                                {
                                    PermitLimit = 5,

                                    Window =
                                        TimeSpan.FromMinutes(1),

                                    SegmentsPerWindow = 6,

                                    QueueLimit = 0,

                                    AutoReplenishment = true
                                }));

        // حماية إنشاء الحسابات من السبام والإساءة.
        options.AddPolicy(
            "auth-register",
            httpContext =>
                RateLimitPartition
                    .GetSlidingWindowLimiter(
                        partitionKey:
                            httpContext.Connection
                                .RemoteIpAddress?
                                .ToString()
                            ?? "unknown",

                        factory:
                            _ =>
                                new SlidingWindowRateLimiterOptions
                                {
                                    PermitLimit = 3,

                                    Window =
                                        TimeSpan.FromMinutes(1),

                                    SegmentsPerWindow = 6,

                                    QueueLimit = 0,

                                    AutoReplenishment = true
                                }));
    });

var app =
    builder.Build();

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

app.MapGet(
    "/health",
    () =>
        Results.Ok(
            new
            {
                status = "ok",
                service = "OFOQ.Market.Api"
            }));

app.MapAuthEndpoints();

app.MapTenantEndpoints();

app.Run();

public partial class Program;