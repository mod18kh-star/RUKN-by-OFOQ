using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Verification.Review;
using OFOQ.Market.Domain.Commerce.Verification;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class PlatformMerchantVerificationReviewFileEndpoints
{
    public static IEndpointRouteBuilder
        MapPlatformMerchantVerificationReviewFileEndpoints(
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
            "/{profileId:guid}/files/{fileId:guid}",
            OpenAsync);

        return endpoints;
    }

    private static async Task<IResult> OpenAsync(
        Guid profileId,
        Guid fileId,
        OpenMerchantVerificationReviewFileHandler handler,
        CancellationToken cancellationToken)
    {
        if (profileId ==
                Guid.Empty ||
            fileId ==
                Guid.Empty)
        {
            return Results.NotFound();
        }

        var result =
            await handler.HandleAsync(
                MerchantVerificationProfileId.From(
                    profileId),
                MerchantVerificationDocumentFileId.From(
                    fileId),
                cancellationToken);

        if (result is null)
        {
            return Results.NotFound();
        }

        return Results.Stream(
            result.Content,
            result.ContentType,
            enableRangeProcessing:
                true);
    }
}