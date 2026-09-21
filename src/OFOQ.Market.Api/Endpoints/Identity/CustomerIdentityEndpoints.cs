using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Identity.CurrentUserContext;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Identity;

public sealed record UpdateCustomerIdentityProfileRequest(
    string? FullName,
    string? PhoneNumber
);

public static class CustomerIdentityEndpoints
{
    public static IEndpointRouteBuilder MapCustomerIdentityEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/customer")
            .WithTags("Customer Identity")
            .RequireAuthorization();

        group.MapPut("/profile", UpdateProfileAsync)
            .RequireRateLimiting("auth-session");

        return endpoints;
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateCustomerIdentityProfileRequest request,
        GetCurrentUserContextHandler currentUser,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        HttpContext http,
        CancellationToken ct)
    {
        http.Response.Headers["Cache-Control"] = "no-store";

        var subject = http.User
            .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(subject, out var id) ||
            id == Guid.Empty)
        {
            return Results.Unauthorized();
        }

        var userId = UserId.From(id);

        try
        {
            await currentUser.HandleAsync(
                new GetCurrentUserContextQuery(userId),
                ct
            );

            var user = await users.GetByIdAsync(
                userId,
                ct
            );

            if (user is null ||
                user.IsDeleted ||
                user.Status != UserStatus.Active)
            {
                return Results.Unauthorized();
            }

            user.UpdateProfile(
                request.FullName,
                request.PhoneNumber,
                timeProvider.GetUtcNow(),
                id
            );

            await unitOfWork.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                userId = id,
                Email = user.Email.Value,
                user.FullName,
                user.PhoneNumber
            });
        }
        catch (CurrentUserUnavailableException)
        {
            return Results.Unauthorized();
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new
            {
                code = "customer_profile_invalid",
                message = "Check your name and phone number."
            });
        }
    }
}