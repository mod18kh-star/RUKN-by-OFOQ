using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OFOQ.Market.Application.Identity.LoginUser;
using OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;
using OFOQ.Market.Application.Identity.RegisterUser;
using OFOQ.Market.Contracts.Identity;

namespace OFOQ.Market.Api.Endpoints.Identity;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints.MapGroup(
                "/api/auth");

        group.MapPost(
                "/register",
                RegisterAsync)
            .RequireRateLimiting(
                "auth-register");

        group.MapPost(
                "/login",
                LoginAsync)
            .RequireRateLimiting(
                "auth-login");

        group.MapPost(
                "/mfa/totp",
                VerifyMfaTotpAsync)
            .RequireRateLimiting(
                "auth-mfa");

        group.MapGet(
                "/me",
                GetCurrentUser)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterUserRequest request,
        RegisterUserHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await handler.HandleAsync(
                    new RegisterUserCommand(
                        request.Email,
                        request.Password),
                    cancellationToken);

            var response =
                new RegisterUserResponse(
                    result.UserId.Value,
                    result.Email,
                    result.Status.ToString(),
                    result.CreatedAtUtc);

            return Results.Created(
                $"/api/users/{result.UserId.Value}",
                response);
        }
        catch (UserEmailAlreadyExistsException)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "user_email_already_exists",

                    message =
                        "An account with this email already exists."
                });
        }
        catch (InvalidPasswordException exception)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_password",

                    message =
                        exception.Message
                });
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_email",

                    message =
                        "The email address is invalid."
                });
        }
    }

    private static async Task<IResult> LoginAsync(
        LoginUserRequest request,
        LoginUserHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        try
        {
            var result =
                await handler.HandleAsync(
                    new LoginUserCommand(
                        request.Email,
                        request.Password),
                    cancellationToken);

            return Results.Ok(
                new LoginUserResponse(
                    result.UserId.Value,
                    result.Email,
                    result.RequiresMfa,
                    result.AccessToken,
                    result.AccessTokenExpiresAtUtc,
                    result.MfaChallengeToken,
                    result.MfaChallengeExpiresAtUtc));
        }
        catch (InvalidCredentialsException)
        {
            return Results.Json(
                new
                {
                    code =
                        "invalid_credentials",

                    message =
                        "Invalid email or password."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }
    }

    private static async Task<IResult> VerifyMfaTotpAsync(
        VerifyMfaTotpRequest request,
        VerifyMfaTotpHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        try
        {
            var result =
                await handler.HandleAsync(
                    new VerifyMfaTotpCommand(
                        request.ChallengeToken,
                        request.Code),
                    cancellationToken);

            return Results.Ok(
                new VerifyMfaTotpResponse(
                    result.UserId.Value,
                    result.Email,
                    result.AccessToken,
                    result.ExpiresAtUtc));
        }
        catch (InvalidMfaLoginChallengeException)
        {
            /*
             * Deliberately generic.
             *
             * We do not reveal whether:
             * - the challenge exists,
             * - it expired,
             * - it was consumed,
             * - it was revoked,
             * - attempts were exhausted,
             * - or the TOTP code was wrong/replayed.
             */
            return Results.Json(
                new
                {
                    code =
                        "invalid_mfa_verification",

                    message =
                        "The MFA verification could not be completed."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }
    }

    private static IResult GetCurrentUser(
        ClaimsPrincipal principal)
    {
        var userIdValue =
            principal.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

        var email =
            principal.FindFirstValue(
                JwtRegisteredClaimNames.Email);

        if (!Guid.TryParse(
                userIdValue,
                out var userId)
            || string.IsNullOrWhiteSpace(
                email))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(
            new CurrentUserResponse(
                userId,
                email));
    }

    private static void SetNoStoreHeaders(
        HttpContext httpContext)
    {
        httpContext.Response.Headers.CacheControl =
            "no-store";

        httpContext.Response.Headers.Pragma =
            "no-cache";

        httpContext.Response.Headers.Expires =
            "0";
    }
}