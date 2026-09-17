using Microsoft.Extensions.FileProviders;
using OFOQ.Market.Api.Endpoints.Platform;
using OFOQ.Market.Api.Operations;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OFOQ.Market.Api.Endpoints.Catalog;
using OFOQ.Market.Api.Endpoints.Commerce;
using OFOQ.Market.Api.Endpoints.Content;
using OFOQ.Market.Api.Endpoints.Identity;
using OFOQ.Market.Api.Endpoints.Tenancy;
using OFOQ.Market.Api.Security;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Api.Security.Tenancy;
using OFOQ.Market.Application;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Infrastructure;
using OFOQ.Market.Infrastructure.Security;

var builder =
    WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString(
        "MarketDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'MarketDatabase' was not found.");

var recoveryCodeHmacKey =
    builder.Configuration[
        "Authentication:Mfa:RecoveryCodeHmacKey"]
    ?? throw new InvalidOperationException(
        "Recovery code HMAC key was not configured.");

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
        Issuer =
            jwtIssuer,

        Audience =
            jwtAudience,

        SigningKey =
            jwtSigningKey,

        AccessTokenMinutes =
            accessTokenMinutes
    };

jwtSettings.Validate();

var googleIdentityEnabledValue =
    builder.Configuration[
        "Authentication:Google:Enabled"];

var googleIdentityEnabled =
    false;

if (!string.IsNullOrWhiteSpace(
        googleIdentityEnabledValue) &&
    !bool.TryParse(
        googleIdentityEnabledValue,
        out googleIdentityEnabled))
{
    throw new InvalidOperationException(
        "Google authentication enabled flag was not configured correctly.");
}

var googleIdentityOptions =
    new GoogleIdentityOptions
    {
        Enabled =
            googleIdentityEnabled,

        ClientId =
            builder.Configuration[
                "Authentication:Google:ClientId"]
    };

googleIdentityOptions.Validate();

var signingKey =
    new SymmetricSecurityKey(
        jwtSettings.GetSigningKeyBytes());

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    connectionString);

builder.Services.AddRecoveryCodeSecurity(
    recoveryCodeHmacKey);

builder.Services.AddSingleton(
    jwtSettings);

builder.Services.AddSingleton(
    googleIdentityOptions);

builder.Services.AddSingleton<
    JwtAccessTokenService>();

builder.Services.AddSingleton<
    IAccessTokenService>(
        serviceProvider =>
            serviceProvider.GetRequiredService<
                JwtAccessTokenService>());

builder.Services.AddSingleton<
    ISessionAccessTokenService>(
        serviceProvider =>
            serviceProvider.GetRequiredService<
                JwtAccessTokenService>());

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.IncludeErrorDetails =
                false;

            options.MapInboundClaims =
                false;

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

                    ClockSkew =
                        TimeSpan.FromSeconds(30),

                    NameClaimType =
                        JwtRegisteredClaimNames.Sub
                };

            options.Events =
                new JwtBearerEvents
                {
                    OnTokenValidated =
                        async context =>
                        {
                            var sessionClaim =
                                context.Principal?
                                    .FindFirst(
                                        "sid")?
                                    .Value;

                            // Legacy/service tokens without a session claim keep
                            // working during this closure phase. All interactive
                            // login tokens issued by /api/auth now carry sid.
                            if (string.IsNullOrWhiteSpace(
                                    sessionClaim))
                            {
                                return;
                            }

                            var subject =
                                context.Principal?
                                    .FindFirst(
                                        JwtRegisteredClaimNames.Sub)?
                                    .Value;

                            if (!Guid.TryParse(
                                    sessionClaim,
                                    out var sessionGuid) ||
                                sessionGuid == Guid.Empty ||
                                !Guid.TryParse(
                                    subject,
                                    out var userGuid) ||
                                userGuid == Guid.Empty)
                            {
                                context.Fail(
                                    "Invalid authentication session.");

                                return;
                            }

                            var sessionRepository =
                                context.HttpContext
                                    .RequestServices
                                    .GetRequiredService<
                                        IUserSessionRepository>();

                            var userRepository =
                                context.HttpContext
                                    .RequestServices
                                    .GetRequiredService<
                                        IUserRepository>();

                            var timeProvider =
                                context.HttpContext
                                    .RequestServices
                                    .GetRequiredService<
                                        TimeProvider>();

                            var session =
                                await sessionRepository
                                    .GetByIdAsync(
                                        UserSessionId.From(
                                            sessionGuid),
                                        context.HttpContext
                                            .RequestAborted);

                            var userId =
                                UserId.From(
                                    userGuid);

                            var user =
                                await userRepository
                                    .GetByIdAsync(
                                        userId,
                                        context.HttpContext
                                            .RequestAborted);

                            if (session is null ||
                                session.UserId != userId ||
                                !session.IsUsable(
                                    timeProvider.GetUtcNow()) ||
                                user is null ||
                                user.IsDeleted ||
                                user.Status != UserStatus.Active)
                            {
                                context.Fail(
                                    "Authentication session is no longer active.");

                                return;
                            }

                            var tokenHasMfa =
                                context.Principal!
                                    .FindAll(
                                        "amr")
                                    .Any(
                                        claim =>
                                            string.Equals(
                                                claim.Value,
                                                "mfa",
                                                StringComparison.Ordinal));

                            if (tokenHasMfa &&
                                session.AuthenticationLevel !=
                                    UserSessionAuthenticationLevel.MultiFactor)
                            {
                                context.Fail(
                                    "Authentication session level mismatch.");
                            }
                        }
                };
        });

builder.Services.AddOfoqAuthorization();
builder.Services.AddPlatformAdministrationAuthorization();

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "FrontendDevelopment",
            policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173",
                        "http://127.0.0.1:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });

builder.Services.AddRateLimiter(
    options =>
    {
        options.RejectionStatusCode =
            StatusCodes.Status429TooManyRequests;

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
                                    PermitLimit =
                                        5,

                                    Window =
                                        TimeSpan.FromMinutes(1),

                                    SegmentsPerWindow =
                                        6,

                                    QueueLimit =
                                        0,

                                    AutoReplenishment =
                                        true
                                }));

        options.AddPolicy(
            "auth-mfa",
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
                                    PermitLimit =
                                        5,

                                    Window =
                                        TimeSpan.FromMinutes(1),

                                    SegmentsPerWindow =
                                        6,

                                    QueueLimit =
                                        0,

                                    AutoReplenishment =
                                        true
                                }));

        options.AddPolicy(
            "auth-email-verification",
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
                                    PermitLimit =
                                        3,

                                    Window =
                                        TimeSpan.FromMinutes(1),

                                    SegmentsPerWindow =
                                        6,

                                    QueueLimit =
                                        0,

                                    AutoReplenishment =
                                        true
                                }));

        options.AddPolicy(
            "auth-session",
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
                                    PermitLimit =
                                        10,

                                    Window =
                                        TimeSpan.FromMinutes(1),

                                    SegmentsPerWindow =
                                        6,

                                    QueueLimit =
                                        0,

                                    AutoReplenishment =
                                        true
                                }));

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
                                    PermitLimit =
                                        3,

                                    Window =
                                        TimeSpan.FromMinutes(1),

                                    SegmentsPerWindow =
                                        6,

                                    QueueLimit =
                                        0,

                                    AutoReplenishment =
                                        true
                                }));
    });

var app =
    builder.Build();

var publicUploadsRoot =
    Path.Combine(
        app.Environment.ContentRootPath,
        "App_Data",
        "public-uploads");

Directory.CreateDirectory(
    publicUploadsRoot);

if (DevelopmentPlatformAdminSeeder.IsRequested(args))
{
    await DevelopmentPlatformAdminSeeder.RunAsync(app);
    return;
}

app.UseStaticFiles(
    new StaticFileOptions
    {
        FileProvider =
            new PhysicalFileProvider(
                publicUploadsRoot),

        RequestPath =
            "/public-uploads"
    });

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors(
        "FrontendDevelopment");
}

app.UseMiddleware<
    TenantRouteContextMiddleware>();

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

app.MapGet(
    "/health",
    () =>
        Results.Ok(
            new
            {
                status =
                    "ok",

                service =
                    "OFOQ.Market.Api"
            }));

app.MapAuthEndpoints();

app.MapTenantEndpoints();
app.MapMyTenantEndpoints();
app.MapMerchantPlatformRequestEndpoints();

app.MapTenantBackOfficeEndpoints();
app.MapNotificationPreferenceEndpoints();
app.MapTenantStoreProfileEndpoints();
app.MapTenantStorefrontPresentationEndpoints();

app.MapCategoryEndpoints();

app.MapProductEndpoints();

app.MapProductAttributeEndpoints();

app.MapProductImageEndpoints();

app.MapProductContentAndRecommendationEndpoints();

app.MapStorefrontEndpoints();

app.MapProductOptionEndpoints();

app.MapProductVariantEndpoints();

app.MapCommerceOnboardingEndpoints();
app.MapCommerceConfigurationEndpoints();

app.MapPlatformMerchantVerificationReviewEndpoints();

app.MapMerchantVerificationEndpoints();

app.MapPlatformMerchantVerificationReviewFileEndpoints();

app.MapCustomerAccountEndpoints();
app.MapFulfillmentEndpoints();
app.MapDiscountCouponEndpoints();

app.MapCartEndpoints();

app.MapCheckoutEndpoints();

app.MapOrderEndpoints();
app.MapReturnEndpoints();
app.MapReviewEndpoints();
app.MapContentEndpoints();

app.MapAnalyticsEndpoints();
app.MapMerchantDashboardEndpoints();

app.MapPaymentEndpoints();

app.MapPlatformAdministrationEndpoints();

app.Run();

public partial class Program;