using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OFOQ.Market.Api.Security;
using OFOQ.Market.Application.Identity.CurrentUserContext;
using OFOQ.Market.Application.Identity.EmailVerification;
using OFOQ.Market.Application.Identity.EmailVerification.Confirm;
using OFOQ.Market.Application.Identity.EmailVerification.Start;
using OFOQ.Market.Application.Identity.GoogleSignIn;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Application.Identity.LoginUser;
using OFOQ.Market.Application.Identity.Mfa;
using OFOQ.Market.Application.Identity.Mfa.CompleteEnrollment;
using OFOQ.Market.Application.Identity.Mfa.Login.VerifyRecovery;
using OFOQ.Market.Application.Identity.Mfa.Login.VerifyTotp;
using OFOQ.Market.Application.Identity.Mfa.RecoveryCodes.Regenerate;
using OFOQ.Market.Application.Identity.Mfa.StartEnrollment;
using OFOQ.Market.Application.Identity.RegisterUser;
using OFOQ.Market.Application.Identity.Sessions;
using OFOQ.Market.Application.Identity.TrustedDevices;
using OFOQ.Market.Contracts.Identity;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Identity;

public static class AuthEndpoints
{
    private const string PasswordAuthenticationMethod =
        "pwd";

    private const string MultiFactorAuthenticationMethod =
        "mfa";

    private const string RefreshTokenCookieName =
        "rukn_refresh";

    private const string TrustedDeviceCookieName =
        "rukn_trusted_device";

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
                "/google",
                GoogleSignInAsync)
            .RequireRateLimiting(
                "auth-login");

        group.MapPost(
                "/refresh",
                RefreshSessionAsync)
            .RequireRateLimiting(
                "auth-session");

        group.MapPost(
                "/logout",
                LogoutAsync)
            .RequireRateLimiting(
                "auth-session");

        group.MapPost(
                "/mfa/totp",
                VerifyMfaTotpAsync)
            .RequireRateLimiting(
                "auth-mfa");

        group.MapPost(
                "/mfa/recovery",
                VerifyMfaRecoveryCodeAsync)
            .RequireRateLimiting(
                "auth-mfa");

        group.MapPost(
                "/mfa/enrollment/start",
                StartMfaEnrollmentAsync)
            .RequireAuthorization()
            .RequireRateLimiting(
                "auth-mfa");

        group.MapPost(
                "/mfa/enrollment/confirm",
                ConfirmMfaEnrollmentAsync)
            .RequireAuthorization()
            .RequireRateLimiting(
                "auth-mfa");

        group.MapPost(
                "/mfa/recovery-codes/regenerate",
                RegenerateMfaRecoveryCodesAsync)
            .RequireAuthorization()
            .RequireRateLimiting(
                "auth-mfa");

        group.MapPost(
                "/email-verification/start",
                StartEmailVerificationAsync)
            .RequireAuthorization()
            .RequireRateLimiting(
                "auth-email-verification");

        group.MapPost(
                "/email-verification/confirm",
                ConfirmEmailVerificationAsync)
            .RequireRateLimiting(
                "auth-email-verification");

        group.MapGet(
                "/sessions",
                GetSessionsAsync)
            .RequireAuthorization();

        group.MapPost(
                "/sessions/{sessionId:guid}/revoke",
                RevokeSessionAsync)
            .RequireAuthorization()
            .RequireRateLimiting(
                "auth-session");

        group.MapPost(
                "/sessions/revoke-others",
                RevokeOtherSessionsAsync)
            .RequireAuthorization()
            .RequireRateLimiting(
                "auth-session");

        group.MapGet(
                "/trusted-devices",
                GetTrustedDevicesAsync)
            .RequireAuthorization();

        group.MapPost(
                "/trusted-devices/{deviceId:guid}/revoke",
                RevokeTrustedDeviceAsync)
            .RequireAuthorization()
            .RequireRateLimiting(
                "auth-session");

        group.MapPost(
                "/trusted-devices/revoke-all",
                RevokeAllTrustedDevicesAsync)
            .RequireAuthorization()
            .RequireRateLimiting(
                "auth-session");

        group.MapGet(
                "/me",
                GetCurrentUserAsync)
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
                        request.Password,
                        request.FullName,
                        request.PhoneNumber),
                    cancellationToken);

            var response =
                new RegisterUserResponse(
                    result.UserId.Value,
                    result.Email,
                    result.Status.ToString(),
                    result.CreatedAtUtc,
                    result.FullName,
                    result.PhoneNumber);

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
        catch (InvalidRegistrationProfileException exception)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        exception.Code,

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
        AuthenticationSessionService sessionService,
        TrustedDeviceService trustedDeviceService,
        IHostEnvironment environment,
        IConfiguration configuration,
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

            if (result.RequiresMfa)
            {
                var trustedDeviceToken =
                    httpContext.Request.Cookies[
                        TrustedDeviceCookieName];

                var trustedDeviceIsValid =
                    await trustedDeviceService.ValidateAsync(
                        result.UserId,
                        trustedDeviceToken,
                        GetUserAgent(httpContext),
                        GetClientIpAddress(httpContext),
                        cancellationToken);

                if (trustedDeviceIsValid)
                {
                    var trustedSession =
                        await sessionService.IssueAsync(
                            result.UserId,
                            UserSessionAuthenticationLevel.MultiFactor,
                            GetClientIpAddress(httpContext),
                            GetUserAgent(httpContext),
                            cancellationToken);

                    SetRefreshTokenCookie(
                        httpContext,
                        trustedSession.RefreshToken,
                        trustedSession.RefreshTokenExpiresAtUtc);

                    return Results.Ok(
                        new LoginUserResponse(
                            result.UserId.Value,
                            result.Email,
                            RequiresMfa: false,
                            trustedSession.AccessToken,
                            trustedSession.AccessTokenExpiresAtUtc,
                            null,
                            null));
                }

                if (!string.IsNullOrWhiteSpace(
                        trustedDeviceToken))
                {
                    ClearTrustedDeviceCookie(
                        httpContext);
                }

                ClearRefreshTokenCookie(
                    httpContext);

                return Results.Ok(
                    new LoginUserResponse(
                        result.UserId.Value,
                        result.Email,
                        result.RequiresMfa,
                        null,
                        null,
                        result.MfaChallengeToken,
                        result.MfaChallengeExpiresAtUtc));
            }

            var session =
                await sessionService.IssueAsync(
                    result.UserId,
                    UserSessionAuthenticationLevel.PasswordOnly,
                    GetClientIpAddress(httpContext),
                    GetUserAgent(httpContext),
                    cancellationToken);

            SetRefreshTokenCookie(
                httpContext,
                session.RefreshToken,
                session.RefreshTokenExpiresAtUtc);

            return Results.Ok(
                new LoginUserResponse(
                    result.UserId.Value,
                    result.Email,
                    false,
                    session.AccessToken,
                    session.AccessTokenExpiresAtUtc,
                    null,
                    null));
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
        catch (InvalidAuthenticationSessionException)
        {
            ClearRefreshTokenCookie(
                httpContext);

            return Results.Json(
                new
                {
                    code =
                        "authentication_session_unavailable",

                    message =
                        "The authentication session could not be created."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }
    }

    private static async Task<IResult> GoogleSignInAsync(
        GoogleSignInRequest request,
        GoogleSignInHandler handler,
        AuthenticationSessionService sessionService,
        TrustedDeviceService trustedDeviceService,
        IHostEnvironment environment,
        IConfiguration configuration,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        try
        {
            var result =
                await handler.HandleAsync(
                    request.IdToken,
                    request.Nonce,
                    cancellationToken);

            if (result.RequiresMfa &&
                DevelopmentSecurity
                    .IsMfaBypassEnabled(
                        environment,
                        configuration))
            {
                var developmentSession =
                    await sessionService.IssueAsync(
                        result.UserId,
                        UserSessionAuthenticationMethod.Google,
                        UserSessionAuthenticationLevel.PasswordOnly,
                        GetClientIpAddress(httpContext),
                        GetUserAgent(httpContext),
                        cancellationToken);

                SetRefreshTokenCookie(
                    httpContext,
                    developmentSession.RefreshToken,
                    developmentSession.RefreshTokenExpiresAtUtc);

                ClearTrustedDeviceCookie(
                    httpContext);

                return Results.Ok(
                    new LoginUserResponse(
                        result.UserId.Value,
                        result.Email,
                        RequiresMfa: false,
                        developmentSession.AccessToken,
                        developmentSession.AccessTokenExpiresAtUtc,
                        null,
                        null));
            }

            if (result.RequiresMfa)
            {
                var trustedDeviceToken =
                    httpContext.Request.Cookies[
                        TrustedDeviceCookieName];

                var trustedDeviceIsValid =
                    await trustedDeviceService.ValidateAsync(
                        result.UserId,
                        trustedDeviceToken,
                        GetUserAgent(httpContext),
                        GetClientIpAddress(httpContext),
                        cancellationToken);

                if (trustedDeviceIsValid)
                {
                    var trustedSession =
                        await sessionService.IssueAsync(
                            result.UserId,
                            UserSessionAuthenticationMethod.Google,
                            UserSessionAuthenticationLevel.MultiFactor,
                            GetClientIpAddress(httpContext),
                            GetUserAgent(httpContext),
                            cancellationToken);

                    SetRefreshTokenCookie(
                        httpContext,
                        trustedSession.RefreshToken,
                        trustedSession.RefreshTokenExpiresAtUtc);

                    return Results.Ok(
                        new LoginUserResponse(
                            result.UserId.Value,
                            result.Email,
                            RequiresMfa: false,
                            trustedSession.AccessToken,
                            trustedSession.AccessTokenExpiresAtUtc,
                            null,
                            null));
                }

                if (!string.IsNullOrWhiteSpace(
                        trustedDeviceToken))
                {
                    ClearTrustedDeviceCookie(
                        httpContext);
                }

                ClearRefreshTokenCookie(
                    httpContext);

                return Results.Ok(
                    new LoginUserResponse(
                        result.UserId.Value,
                        result.Email,
                        RequiresMfa: true,
                        null,
                        null,
                        result.MfaChallengeToken,
                        result.MfaChallengeExpiresAtUtc));
            }

            var session =
                await sessionService.IssueAsync(
                    result.UserId,
                    UserSessionAuthenticationMethod.Google,
                    UserSessionAuthenticationLevel.PasswordOnly,
                    GetClientIpAddress(httpContext),
                    GetUserAgent(httpContext),
                    cancellationToken);

            SetRefreshTokenCookie(
                httpContext,
                session.RefreshToken,
                session.RefreshTokenExpiresAtUtc);

            return Results.Ok(
                new LoginUserResponse(
                    result.UserId.Value,
                    result.Email,
                    RequiresMfa: false,
                    session.AccessToken,
                    session.AccessTokenExpiresAtUtc,
                    null,
                    null));
        }
        catch (GoogleIdentityProviderUnavailableException)
        {
            ClearRefreshTokenCookie(
                httpContext);

            return Results.Json(
                new
                {
                    code =
                        "google_sign_in_unavailable",

                    message =
                        "Google sign-in is not available."
                },
                statusCode:
                    StatusCodes.Status503ServiceUnavailable);
        }
        catch (GoogleAccountAlreadyLinkedException)
        {
            ClearRefreshTokenCookie(
                httpContext);

            return Results.Conflict(
                new
                {
                    code =
                        "google_account_already_linked",

                    message =
                        "This account is already linked to another Google identity."
                });
        }
        catch (GoogleSignInRejectedException)
        {
            ClearRefreshTokenCookie(
                httpContext);

            return Results.Json(
                new
                {
                    code =
                        "invalid_google_sign_in",

                    message =
                        "Google sign-in could not be completed."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }
        catch (InvalidAuthenticationSessionException)
        {
            ClearRefreshTokenCookie(
                httpContext);

            return Results.Json(
                new
                {
                    code =
                        "authentication_session_unavailable",

                    message =
                        "The authentication session could not be created."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }
    }

    private static async Task<IResult> RefreshSessionAsync(
        AuthenticationSessionService sessionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        var refreshToken =
            httpContext.Request.Cookies[
                RefreshTokenCookieName];

        try
        {
            var session =
                await sessionService.RefreshAsync(
                    refreshToken ?? string.Empty,
                    GetClientIpAddress(httpContext),
                    GetUserAgent(httpContext),
                    cancellationToken);

            SetRefreshTokenCookie(
                httpContext,
                session.RefreshToken,
                session.RefreshTokenExpiresAtUtc);

            return Results.Ok(
                new RefreshSessionResponse(
                    session.AccessToken,
                    session.AccessTokenExpiresAtUtc));
        }
        catch (InvalidAuthenticationSessionException)
        {
            ClearRefreshTokenCookie(
                httpContext);

            return Results.Json(
                new
                {
                    code =
                        "invalid_refresh_session",

                    message =
                        "The session is invalid or expired."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }
    }

    private static async Task<IResult> LogoutAsync(
        AuthenticationSessionService sessionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        var refreshToken =
            httpContext.Request.Cookies[
                RefreshTokenCookieName];

        await sessionService.LogoutAsync(
            refreshToken,
            cancellationToken);

        ClearRefreshTokenCookie(
            httpContext);

        return Results.NoContent();
    }

    private static async Task<IResult> VerifyMfaTotpAsync(
        VerifyMfaTotpRequest request,
        VerifyMfaTotpHandler handler,
        AuthenticationSessionService sessionService,
        TrustedDeviceService trustedDeviceService,
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

            var session =
                await sessionService.IssueAsync(
                    result.UserId,
                    result.AuthenticationMethod,
                    UserSessionAuthenticationLevel.MultiFactor,
                    GetClientIpAddress(httpContext),
                    GetUserAgent(httpContext),
                    cancellationToken);

            SetRefreshTokenCookie(
                httpContext,
                session.RefreshToken,
                session.RefreshTokenExpiresAtUtc);

            if (request.RememberDevice)
            {
                var trustedDevice =
                    await trustedDeviceService.IssueAsync(
                        result.UserId,
                        GetUserAgent(httpContext),
                        GetClientIpAddress(httpContext),
                        cancellationToken);

                SetTrustedDeviceCookie(
                    httpContext,
                    trustedDevice.RawToken,
                    trustedDevice.ExpiresAtUtc);
            }

            return Results.Ok(
                new VerifyMfaTotpResponse(
                    result.UserId.Value,
                    result.Email,
                    session.AccessToken,
                    session.AccessTokenExpiresAtUtc));
        }
        catch (InvalidMfaLoginChallengeException)
        {
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
        catch (InvalidAuthenticationSessionException)
        {
            ClearRefreshTokenCookie(
                httpContext);

            return Results.Json(
                new
                {
                    code =
                        "authentication_session_unavailable",

                    message =
                        "The authentication session could not be created."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }
    }

    private static async Task<IResult> VerifyMfaRecoveryCodeAsync(
        VerifyMfaRecoveryCodeRequest request,
        VerifyMfaRecoveryCodeHandler handler,
        AuthenticationSessionService sessionService,
        TrustedDeviceService trustedDeviceService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        try
        {
            var result =
                await handler.HandleAsync(
                    new VerifyMfaRecoveryCodeCommand(
                        request.ChallengeToken,
                        request.RecoveryCode),
                    cancellationToken);

            var session =
                await sessionService.IssueAsync(
                    result.UserId,
                    result.AuthenticationMethod,
                    UserSessionAuthenticationLevel.MultiFactor,
                    GetClientIpAddress(httpContext),
                    GetUserAgent(httpContext),
                    cancellationToken);

            SetRefreshTokenCookie(
                httpContext,
                session.RefreshToken,
                session.RefreshTokenExpiresAtUtc);

            if (request.RememberDevice)
            {
                var trustedDevice =
                    await trustedDeviceService.IssueAsync(
                        result.UserId,
                        GetUserAgent(httpContext),
                        GetClientIpAddress(httpContext),
                        cancellationToken);

                SetTrustedDeviceCookie(
                    httpContext,
                    trustedDevice.RawToken,
                    trustedDevice.ExpiresAtUtc);
            }

            return Results.Ok(
                new VerifyMfaRecoveryCodeResponse(
                    result.UserId.Value,
                    result.Email,
                    session.AccessToken,
                    session.AccessTokenExpiresAtUtc));
        }
        catch (InvalidMfaRecoveryVerificationException)
        {
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
        catch (InvalidAuthenticationSessionException)
        {
            ClearRefreshTokenCookie(
                httpContext);

            return Results.Json(
                new
                {
                    code =
                        "authentication_session_unavailable",

                    message =
                        "The authentication session could not be created."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }
    }

    private static async Task<IResult> StartMfaEnrollmentAsync(
        ClaimsPrincipal principal,
        StartMfaEnrollmentHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId) ||
            !HasAuthenticationMethod(
                principal,
                PasswordAuthenticationMethod))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new StartMfaEnrollmentCommand(
                        userId),
                    cancellationToken);

            return Results.Ok(
                new StartMfaEnrollmentResponse(
                    result.ManualEntryKey,
                    result.ProvisioningUri));
        }
        catch (MfaAlreadyEnabledException)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "mfa_already_enabled",

                    message =
                        "Multi-factor authentication is already enabled."
                });
        }
        catch (InvalidOperationException)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "mfa_enrollment_unavailable",

                    message =
                        "Multi-factor authentication enrollment is unavailable for this account."
                });
        }
    }

    private static async Task<IResult> ConfirmMfaEnrollmentAsync(
        ConfirmMfaEnrollmentRequest request,
        ClaimsPrincipal principal,
        CompleteMfaEnrollmentHandler handler,
        AuthenticationSessionService sessionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId) ||
            !HasAuthenticationMethod(
                principal,
                PasswordAuthenticationMethod))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new CompleteMfaEnrollmentCommand(
                        userId,
                        request.Code),
                    cancellationToken);

            TryGetSessionId(
                principal,
                out var currentSessionId);

            var session =
                await sessionService.UpgradeOrIssueAsync(
                    userId,
                    currentSessionId.IsEmpty
                        ? null
                        : currentSessionId,
                    GetClientIpAddress(httpContext),
                    GetUserAgent(httpContext),
                    cancellationToken);

            SetRefreshTokenCookie(
                httpContext,
                session.RefreshToken,
                session.RefreshTokenExpiresAtUtc);

            return Results.Ok(
                new ConfirmMfaEnrollmentResponse(
                    result.RecoveryCodes,
                    session.AccessToken,
                    session.AccessTokenExpiresAtUtc));
        }
        catch (InvalidMfaCodeException)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_mfa_code",

                    message =
                        "The authenticator code is invalid or expired."
                });
        }
        catch (InvalidOperationException)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "mfa_enrollment_state_invalid",

                    message =
                        "Multi-factor authentication enrollment could not be completed."
                });
        }
        catch (InvalidAuthenticationSessionException)
        {
            ClearRefreshTokenCookie(
                httpContext);

            return Results.Json(
                new
                {
                    code =
                        "authentication_session_unavailable",

                    message =
                        "The authentication session could not be upgraded."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }
    }

    private static async Task<IResult> RegenerateMfaRecoveryCodesAsync(
        ClaimsPrincipal principal,
        RegenerateRecoveryCodesHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId) ||
            !HasAuthenticationMethod(
                principal,
                PasswordAuthenticationMethod) ||
            !HasAuthenticationMethod(
                principal,
                MultiFactorAuthenticationMethod))
        {
            return Results.Forbid();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new RegenerateRecoveryCodesCommand(
                        userId),
                    cancellationToken);

            return Results.Ok(
                new RegenerateMfaRecoveryCodesResponse(
                    result.Codes));
        }
        catch (InvalidOperationException)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "mfa_recovery_codes_unavailable",

                    message =
                        "Recovery codes cannot be regenerated for this account."
                });
        }
    }

    private static async Task<IResult> StartEmailVerificationAsync(
        ClaimsPrincipal principal,
        StartEmailVerificationHandler handler,
        IHostEnvironment environment,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId) ||
            !HasAuthenticationMethod(
                principal,
                PasswordAuthenticationMethod))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new StartEmailVerificationCommand(
                        userId),
                    cancellationToken);

            var developmentVerificationToken =
                environment.IsDevelopment() ||
                environment.IsEnvironment(
                    "Testing")
                    ? result.VerificationToken
                    : null;

            return Results.Ok(
                new StartEmailVerificationResponse(
                    result.Email,
                    result.ExpiresAtUtc,
                    developmentVerificationToken));
        }
        catch (EmailAlreadyVerifiedException)
        {
            return Results.Conflict(
                new
                {
                    code =
                        "email_already_verified",

                    message =
                        "The email address is already verified."
                });
        }
        catch (InvalidOperationException)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "email_verification_unavailable",

                    message =
                        "Email verification is unavailable for this account."
                });
        }
    }

    private static async Task<IResult> ConfirmEmailVerificationAsync(
        ConfirmEmailVerificationRequest request,
        ConfirmEmailVerificationHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        try
        {
            var result =
                await handler.HandleAsync(
                    new ConfirmEmailVerificationCommand(
                        request.Token),
                    cancellationToken);

            return Results.Ok(
                new ConfirmEmailVerificationResponse(
                    result.UserId,
                    result.Email,
                    result.VerifiedAtUtc));
        }
        catch (InvalidEmailVerificationException)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_email_verification",

                    message =
                        "The email verification request is invalid or expired."
                });
        }
    }

    private static async Task<IResult> GetSessionsAsync(
        ClaimsPrincipal principal,
        AuthenticationSessionService sessionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId))
        {
            return Results.Unauthorized();
        }

        TryGetSessionId(
            principal,
            out var currentSessionId);

        var sessions =
            await sessionService.ListAsync(
                userId,
                currentSessionId.IsEmpty
                    ? null
                    : currentSessionId,
                cancellationToken);

        return Results.Ok(
            sessions.Select(
                session =>
                    new UserSessionResponse(
                        session.SessionId.Value,
                        session.AuthenticationLevel.ToString(),
                        session.CreatedAtUtc,
                        session.LastSeenAtUtc,
                        session.ExpiresAtUtc,
                        session.IpAddress,
                        session.UserAgent,
                        session.IsCurrent)));
    }

    private static async Task<IResult> RevokeSessionAsync(
        Guid sessionId,
        ClaimsPrincipal principal,
        AuthenticationSessionService sessionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId) ||
            sessionId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var targetSessionId =
            UserSessionId.From(
                sessionId);

        var revoked =
            await sessionService.RevokeAsync(
                userId,
                targetSessionId,
                cancellationToken);

        if (!revoked)
        {
            return Results.NotFound();
        }

        if (TryGetSessionId(
                principal,
                out var currentSessionId) &&
            currentSessionId == targetSessionId)
        {
            ClearRefreshTokenCookie(
                httpContext);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RevokeOtherSessionsAsync(
        ClaimsPrincipal principal,
        AuthenticationSessionService sessionService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId) ||
            !TryGetSessionId(
                principal,
                out var currentSessionId))
        {
            return Results.Unauthorized();
        }

        if (!HasAuthenticationMethod(
                principal,
                PasswordAuthenticationMethod) ||
            !HasAuthenticationMethod(
                principal,
                MultiFactorAuthenticationMethod))
        {
            return Results.Forbid();
        }

        var revokedCount =
            await sessionService.RevokeOthersAsync(
                userId,
                currentSessionId,
                cancellationToken);

        return Results.Ok(
            new RevokeOtherSessionsResponse(
                revokedCount));
    }


    private static async Task<IResult> GetTrustedDevicesAsync(
        ClaimsPrincipal principal,
        TrustedDeviceService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId))
        {
            return Results.Unauthorized();
        }

        var devices =
            await service.GetAllAsync(
                userId,
                cancellationToken);

        return Results.Ok(
            devices
                .Select(
                    device =>
                        new TrustedDeviceResponse(
                            device.DeviceId,
                            device.CreatedAtUtc,
                            device.LastUsedAtUtc,
                            device.ExpiresAtUtc,
                            device.CreatedIpAddress,
                            device.LastIpAddress,
                            device.IsRevoked))
                .ToArray());
    }

    private static async Task<IResult> RevokeTrustedDeviceAsync(
        Guid deviceId,
        ClaimsPrincipal principal,
        TrustedDeviceService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId) ||
            deviceId == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var revoked =
            await service.RevokeAsync(
                userId,
                UserTrustedDeviceId.From(
                    deviceId),
                cancellationToken);

        if (!revoked)
        {
            return Results.NotFound(
                new
                {
                    code =
                        "trusted_device_not_found"
                });
        }

        ClearTrustedDeviceCookie(
            httpContext);

        return Results.NoContent();
    }

    private static async Task<IResult> RevokeAllTrustedDevicesAsync(
        ClaimsPrincipal principal,
        TrustedDeviceService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId))
        {
            return Results.Unauthorized();
        }

        await service.RevokeAllAsync(
            userId,
            cancellationToken);

        ClearTrustedDeviceCookie(
            httpContext);

        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        GetCurrentUserContextHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        SetNoStoreHeaders(
            httpContext);

        if (!TryGetAuthenticatedUserId(
                principal,
                out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new GetCurrentUserContextQuery(
                        userId),
                    cancellationToken);

            var authenticationMethods =
                principal
                    .FindAll(
                        "amr")
                    .Select(
                        claim =>
                            claim.Value)
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Distinct(
                        StringComparer.Ordinal)
                    .OrderBy(
                        value =>
                            value,
                        StringComparer.Ordinal)
                    .ToArray();

            return Results.Ok(
                new CurrentUserResponse(
                    result.UserId.Value,
                    result.Email,
                    result.Status.ToString(),
                    result.EmailVerified,
                    result.MfaEnabled,
                    authenticationMethods.Contains(
                        MultiFactorAuthenticationMethod,
                        StringComparer.Ordinal),
                    authenticationMethods,
                    result.PlatformRoles
                        .Select(
                            role =>
                                role.ToString())
                        .ToArray(),
                    result.HasTenantMemberships,
                    TryGetSessionId(
                        principal,
                        out var sessionId)
                            ? sessionId.Value
                            : null,
                    result.FullName,
                    result.PhoneNumber));
        }
        catch (CurrentUserUnavailableException)
        {
            return Results.Unauthorized();
        }
    }

    private static bool TryGetAuthenticatedUserId(
        ClaimsPrincipal principal,
        out UserId userId)
    {
        userId =
            default;

        var subject =
            principal.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(
                subject,
                out var userGuid) ||
            userGuid ==
                Guid.Empty)
        {
            return false;
        }

        userId =
            UserId.From(
                userGuid);

        return true;
    }

    private static bool HasAuthenticationMethod(
        ClaimsPrincipal principal,
        string authenticationMethod)
    {
        return principal
            .FindAll(
                "amr")
            .Any(
                claim =>
                    string.Equals(
                        claim.Value,
                        authenticationMethod,
                        StringComparison.Ordinal));
    }

    private static bool TryGetSessionId(
        ClaimsPrincipal principal,
        out UserSessionId sessionId)
    {
        sessionId = default;

        var sessionValue =
            principal.FindFirstValue(
                "sid");

        if (!Guid.TryParse(
                sessionValue,
                out var sessionGuid) ||
            sessionGuid == Guid.Empty)
        {
            return false;
        }

        sessionId =
            UserSessionId.From(
                sessionGuid);

        return true;
    }

    private static string? GetClientIpAddress(
        HttpContext httpContext)
    {
        return httpContext.Connection
            .RemoteIpAddress?
            .ToString();
    }

    private static string? GetUserAgent(
        HttpContext httpContext)
    {
        return httpContext.Request.Headers
            .UserAgent
            .ToString();
    }

    private static void SetRefreshTokenCookie(
        HttpContext httpContext,
        string refreshToken,
        DateTimeOffset expiresAtUtc)
    {
        httpContext.Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure =
                    httpContext.Request.IsHttps,
                SameSite =
                    SameSiteMode.Lax,
                Path =
                    "/api/auth",
                Expires =
                    expiresAtUtc,
                IsEssential =
                    true
            });
    }

    private static void ClearRefreshTokenCookie(
        HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure =
                    httpContext.Request.IsHttps,
                SameSite =
                    SameSiteMode.Lax,
                Path =
                    "/api/auth",
                IsEssential =
                    true
            });
    }


    private static void SetTrustedDeviceCookie(
        HttpContext httpContext,
        string token,
        DateTimeOffset expiresAtUtc)
    {
        httpContext.Response.Cookies.Append(
            TrustedDeviceCookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure =
                    httpContext.Request.IsHttps,
                SameSite =
                    SameSiteMode.Lax,
                Path =
                    "/api/auth",
                Expires =
                    expiresAtUtc,
                IsEssential =
                    true
            });
    }

    private static void ClearTrustedDeviceCookie(
        HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(
            TrustedDeviceCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure =
                    httpContext.Request.IsHttps,
                SameSite =
                    SameSiteMode.Lax,
                Path =
                    "/api/auth",
                IsEssential =
                    true
            });
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
