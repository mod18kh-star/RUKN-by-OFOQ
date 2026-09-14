using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Verification.Review;
using OFOQ.Market.Application.Common.Tenancy;
using OFOQ.Market.Contracts.Commerce.Verification;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class PlatformMerchantVerificationReviewEndpoints
{
    public static IEndpointRouteBuilder
        MapPlatformMerchantVerificationReviewEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/platform/merchant-verification/reviews")
                .WithTags(
                    "Platform Merchant Verification")
                .RequireAuthorization(
                    AuthorizationPolicies.PlatformMerchantVerificationReview);

        group.MapGet(
            "/",
            GetQueueAsync);

        group.MapGet(
            "/{profileId:guid}",
            GetDetailAsync);

        group.MapPost(
            "/{profileId:guid}/start",
            StartReviewAsync);

        group.MapPost(
            "/{profileId:guid}/request-more-information",
            RequestMoreInformationAsync);

        group.MapPost(
            "/{profileId:guid}/reject",
            RejectAsync);

        group.MapPost(
            "/{profileId:guid}/verify",
            VerifyAsync);

        return endpoints;
    }

    private static async Task<IResult> GetQueueAsync(
        string? status,
        int? take,
        GetMerchantVerificationReviewQueueHandler handler,
        CancellationToken cancellationToken)
    {
        MerchantVerificationStatus?
            parsedStatus =
                null;

        if (!string.IsNullOrWhiteSpace(
                status))
        {
            if (!Enum.TryParse<MerchantVerificationStatus>(
                    status.Trim(),
                    ignoreCase: true,
                    out var value) ||
                value ==
                    MerchantVerificationStatus.Unknown ||
                !Enum.IsDefined(
                    value))
            {
                return ValidationError(
                    "merchant_verification_status_invalid",
                    "A supported merchant verification status is required.");
            }

            parsedStatus =
                value;
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    parsedStatus,
                    take ??
                        GetMerchantVerificationReviewQueueHandler.DefaultTake,
                    cancellationToken);

            return Results.Ok(
                result
                    .Select(
                        MapSummary)
                    .ToArray());
        }
        catch (ArgumentException exception)
        {
            return ValidationError(
                "merchant_verification_review_query_invalid",
                exception.Message);
        }
    }

    private static async Task<IResult> GetDetailAsync(
        Guid profileId,
        GetMerchantVerificationReviewDetailHandler handler,
        CancellationToken cancellationToken)
    {
        if (profileId ==
            Guid.Empty)
        {
            return InvalidProfileId();
        }

        var result =
            await handler.HandleAsync(
                MerchantVerificationProfileId.From(
                    profileId),
                cancellationToken);

        if (result is null)
        {
            return ProfileNotFound();
        }

        return Results.Ok(
            MapDetail(
                result));
    }

    private static async Task<IResult> StartReviewAsync(
        Guid profileId,
        StartMerchantVerificationReviewHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActorUserId(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (profileId ==
            Guid.Empty)
        {
            return InvalidProfileId();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new StartMerchantVerificationReviewCommand(
                        MerchantVerificationProfileId.From(
                            profileId),
                        actor.Value),
                    cancellationToken);

            return Results.Ok(
                new PlatformMerchantVerificationReviewActionResponse(
                    result.ProfileId.Value,
                    result.TenantId.Value,
                    result.Status.ToString(),
                    result.ReviewStartedAtUtc,
                    result.ReviewerUserId.Value,
                    null));
        }
        catch (Exception exception)
        {
            return MapReviewException(
                exception);
        }
    }

    private static async Task<IResult> RequestMoreInformationAsync(
        Guid profileId,
        PlatformMerchantVerificationRequiredNoteRequest request,
        RequestMoreInformationMerchantVerificationReviewHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActorUserId(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (profileId ==
            Guid.Empty)
        {
            return InvalidProfileId();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new RequestMoreInformationMerchantVerificationReviewCommand(
                        MerchantVerificationProfileId.From(
                            profileId),
                        actor.Value,
                        request.ReviewNote),
                    cancellationToken);

            return Results.Ok(
                new PlatformMerchantVerificationReviewActionResponse(
                    result.ProfileId.Value,
                    result.TenantId.Value,
                    result.Status.ToString(),
                    result.ReviewedAtUtc,
                    result.ReviewerUserId.Value,
                    result.ReviewNote));
        }
        catch (Exception exception)
        {
            return MapReviewException(
                exception);
        }
    }

    private static async Task<IResult> RejectAsync(
        Guid profileId,
        PlatformMerchantVerificationRequiredNoteRequest request,
        RejectMerchantVerificationReviewHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActorUserId(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (profileId ==
            Guid.Empty)
        {
            return InvalidProfileId();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new RejectMerchantVerificationReviewCommand(
                        MerchantVerificationProfileId.From(
                            profileId),
                        actor.Value,
                        request.ReviewNote),
                    cancellationToken);

            return Results.Ok(
                new PlatformMerchantVerificationReviewActionResponse(
                    result.ProfileId.Value,
                    result.TenantId.Value,
                    result.Status.ToString(),
                    result.ReviewedAtUtc,
                    result.ReviewerUserId.Value,
                    result.ReviewNote));
        }
        catch (Exception exception)
        {
            return MapReviewException(
                exception);
        }
    }

    private static async Task<IResult> VerifyAsync(
        Guid profileId,
        PlatformMerchantVerificationVerifyRequest request,
        VerifyMerchantVerificationReviewHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActorUserId(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (profileId ==
            Guid.Empty)
        {
            return InvalidProfileId();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new VerifyMerchantVerificationReviewCommand(
                        MerchantVerificationProfileId.From(
                            profileId),
                        actor.Value,
                        request.ReviewNote),
                    cancellationToken);

            return Results.Ok(
                new PlatformMerchantVerificationReviewActionResponse(
                    result.ProfileId.Value,
                    result.TenantId.Value,
                    result.Status.ToString(),
                    result.VerifiedAtUtc,
                    result.ReviewerUserId.Value,
                    result.ReviewNote));
        }
        catch (Exception exception)
        {
            return MapReviewException(
                exception);
        }
    }

    private static PlatformMerchantVerificationReviewSummaryResponse
        MapSummary(
            MerchantVerificationReviewSummaryResult result)
    {
        return new PlatformMerchantVerificationReviewSummaryResponse(
            result.ProfileId.Value,
            result.TenantId.Value,
            result.PrincipalUserId.Value,
            result.SubjectType.ToString(),
            result.CountryCode,
            result.LegalName,
            result.Status.ToString(),
            result.SubmittedAtUtc,
            result.ReviewStartedAtUtc,
            result.ReviewedAtUtc,
            result.ReviewedByUserId,
            result.VerifiedAtUtc,
            result.ExpiredAtUtc,
            result.ReviewNote,
            result.CreatedAtUtc,
            result.UpdatedAtUtc);
    }

    private static PlatformMerchantVerificationReviewDetailResponse
        MapDetail(
            MerchantVerificationReviewDetailResult result)
    {
        return new PlatformMerchantVerificationReviewDetailResponse(
            MapSummary(
                result.Profile),
            result.Documents
                .Select(
                    document =>
                        new PlatformMerchantVerificationDocumentResponse(
                            document.DocumentId.Value,
                            document.DocumentType.ToString(),
                            document.IssuingCountryCode,
                            document.HolderName,
                            document.IssueDate,
                            document.ExpiryDate,
                            document.ReviewStatus.ToString(),
                            document.ReviewNote,
                            document.ReviewedAtUtc,
                            document.ReviewedByUserId,
                            document.Files
                                .Select(
                                    file =>
                                        new PlatformMerchantVerificationDocumentFileResponse(
                                            file.FileId.Value,
                                            file.Side.ToString(),
                                            file.OriginalFileName,
                                            file.ContentType,
                                            file.FileSizeBytes,
                                            file.CreatedAtUtc))
                                .ToArray()))
                .ToArray());
    }

    private static UserId? GetActorUserId(
        HttpContext httpContext)
    {
        var subject =
            httpContext.User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        if (!Guid.TryParse(
                subject,
                out var userGuid) ||
            userGuid ==
                Guid.Empty)
        {
            return null;
        }

        return UserId.From(
            userGuid);
    }

    private static IResult MapReviewException(
        Exception exception)
    {
        return exception switch
        {
            MerchantVerificationReviewConcurrencyException =>
                Results.Conflict(
                    new
                    {
                        code =
                            "merchant_verification_review_concurrency_conflict",

                        message =
                            exception.Message
                    }),

            MerchantVerificationReviewConflictException =>
                Results.Conflict(
                    new
                    {
                        code =
                            "merchant_verification_review_conflict",

                        message =
                            exception.Message
                    }),

            KeyNotFoundException =>
                ProfileNotFound(),

            TenantScopeViolationException =>
                Results.Forbid(),

            InvalidOperationException =>
                Results.Conflict(
                    new
                    {
                        code =
                            "merchant_verification_review_invalid_state",

                        message =
                            exception.Message
                    }),

            ArgumentException =>
                ValidationError(
                    "merchant_verification_review_invalid",
                    exception.Message),

            _ =>
                throw exception
        };
    }

    private static IResult InvalidProfileId()
    {
        return ValidationError(
            "merchant_verification_profile_id_invalid",
            "Merchant verification profile ID is required.");
    }

    private static IResult ProfileNotFound()
    {
        return Results.NotFound(
            new
            {
                code =
                    "merchant_verification_profile_not_found",

                message =
                    "Merchant verification profile was not found."
            });
    }

    private static IResult ValidationError(
        string code,
        string message)
    {
        return Results.BadRequest(
            new
            {
                code,
                message
            });
    }
}