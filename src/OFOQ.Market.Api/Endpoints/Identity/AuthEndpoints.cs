using OFOQ.Market.Application.Identity.RegisterUser;
using OFOQ.Market.Contracts.Identity;

namespace OFOQ.Market.Api.Endpoints.Identity;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints.MapGroup("/api/auth");

        group.MapPost(
            "/register",
            RegisterAsync);

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
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new
                {
                    code =
                        "invalid_email",
                    message =
                        exception.Message
                });
        }
    }
}