using System.IdentityModel.Tokens.Jwt;
using OFOQ.Market.Api.Security.Authorization;
using OFOQ.Market.Application.Commerce.Verification.Merchant;
using OFOQ.Market.Contracts.Commerce.Verification;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Api.Endpoints.Commerce;

public static class MerchantVerificationEndpoints
{
    public static IEndpointRouteBuilder
        MapMerchantVerificationEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group =
            endpoints
                .MapGroup(
                    "/api/tenants/{tenantId:guid}/merchant-verification")
                .WithTags(
                    "Merchant Verification")
                .RequireAuthorization(
                    AuthorizationPolicies.TenantCommerceAdministration);

        group.MapGet(
            "/",
            GetAsync);

        group.MapPut(
            "/profile",
            UpsertProfileAsync);

        group.MapPost(
            "/documents",
            CreateDocumentAsync);

        group.MapPut(
            "/documents/{documentId:guid}",
            UpdateDocumentAsync);

        group.MapPost(
            "/documents/{documentId:guid}/files/{side}",
            UploadFileAsync);

        group.MapGet(
            "/files/{fileId:guid}",
            OpenFileAsync);

        group.MapPost(
            "/submit",
            SubmitAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        GetMerchantVerificationSelfServiceHandler handler,
        CancellationToken cancellationToken)
    {
        var result =
            await handler.HandleAsync(
                cancellationToken);

        return Results.Ok(
            Map(
                result));
    }

    private static async Task<IResult> UpsertProfileAsync(
        MerchantVerificationProfileRequest request,
        UpsertMerchantVerificationProfileHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActor(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryParseEnum(
                request.SubjectType,
                out MerchantVerificationSubjectType subjectType))
        {
            return BadRequest(
                "merchant_verification_subject_type_invalid",
                "A supported merchant verification subject type is required.");
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new UpsertMerchantVerificationProfileCommand(
                        actor.Value,
                        subjectType,
                        request.CountryCode,
                        request.LegalName),
                    cancellationToken);

            return Results.Ok(
                MapProfile(
                    result));
        }
        catch (Exception exception)
        {
            return MapException(
                exception);
        }
    }

    private static Task<IResult> CreateDocumentAsync(
        MerchantVerificationDocumentRequest request,
        UpsertMerchantVerificationDocumentHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        return UpsertDocumentAsync(
            null,
            request,
            handler,
            httpContext,
            cancellationToken);
    }

    private static Task<IResult> UpdateDocumentAsync(
        Guid documentId,
        MerchantVerificationDocumentRequest request,
        UpsertMerchantVerificationDocumentHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (documentId ==
            Guid.Empty)
        {
            return Task.FromResult<IResult>(
                BadRequest(
                    "merchant_verification_document_id_invalid",
                    "Merchant verification document ID is required."));
        }

        return UpsertDocumentAsync(
            MerchantVerificationDocumentId.From(
                documentId),
            request,
            handler,
            httpContext,
            cancellationToken);
    }

    private static async Task<IResult> UpsertDocumentAsync(
        MerchantVerificationDocumentId? documentId,
        MerchantVerificationDocumentRequest request,
        UpsertMerchantVerificationDocumentHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActor(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (!TryParseEnum(
                request.DocumentType,
                out MerchantVerificationDocumentType documentType))
        {
            return BadRequest(
                "merchant_verification_document_type_invalid",
                "A supported merchant verification document type is required.");
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    new UpsertMerchantVerificationDocumentCommand(
                        documentId,
                        actor.Value,
                        documentType,
                        request.IssuingCountryCode,
                        request.HolderName,
                        request.DocumentNumber,
                        request.IssueDate,
                        request.ExpiryDate),
                    cancellationToken);

            return Results.Ok(
                MapDocument(
                    result));
        }
        catch (Exception exception)
        {
            return MapException(
                exception);
        }
    }

    private static async Task<IResult> UploadFileAsync(
        Guid documentId,
        string side,
        UploadMerchantVerificationFileHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActor(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        if (documentId ==
            Guid.Empty)
        {
            return BadRequest(
                "merchant_verification_document_id_invalid",
                "Merchant verification document ID is required.");
        }

        if (!TryParseEnum(
                side,
                out MerchantVerificationDocumentSide parsedSide))
        {
            return BadRequest(
                "merchant_verification_document_side_invalid",
                "A supported merchant verification document side is required.");
        }

        if (!httpContext.Request.HasFormContentType)
        {
            return BadRequest(
                "merchant_verification_file_required",
                "A multipart verification file is required.");
        }

        var form =
            await httpContext.Request.ReadFormAsync(
                cancellationToken);

        var file =
            form.Files.GetFile(
                "file");

        if (file is null ||
            file.Length <= 0)
        {
            return BadRequest(
                "merchant_verification_file_required",
                "A verification file is required.");
        }

        try
        {
            await using var stream =
                file.OpenReadStream();

            var result =
                await handler.HandleAsync(
                    new UploadMerchantVerificationFileCommand(
                        MerchantVerificationDocumentId.From(
                            documentId),
                        parsedSide,
                        actor.Value,
                        file.FileName,
                        file.ContentType,
                        file.Length,
                        stream),
                    cancellationToken);

            return Results.Ok(
                MapFile(
                    result));
        }
        catch (Exception exception)
        {
            return MapException(
                exception);
        }
    }

    private static async Task<IResult> OpenFileAsync(
        Guid fileId,
        OpenMerchantVerificationOwnFileHandler handler,
        CancellationToken cancellationToken)
    {
        if (fileId ==
            Guid.Empty)
        {
            return Results.NotFound();
        }

        var result =
            await handler.HandleAsync(
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

    private static async Task<IResult> SubmitAsync(
        SubmitMerchantVerificationHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor =
            GetActor(
                httpContext);

        if (!actor.HasValue)
        {
            return Results.Unauthorized();
        }

        try
        {
            var result =
                await handler.HandleAsync(
                    actor.Value,
                    cancellationToken);

            return Results.Ok(
                MapProfile(
                    result));
        }
        catch (Exception exception)
        {
            return MapException(
                exception);
        }
    }

    private static MerchantVerificationSelfServiceResponse Map(
        MerchantVerificationSelfServiceResult result)
    {
        return new MerchantVerificationSelfServiceResponse(
            result.Profile is null
                ? null
                : MapProfile(
                    result.Profile),

            result.Documents
                .Select(
                    MapDocument)
                .ToArray());
    }

    private static MerchantVerificationProfileResponse MapProfile(
        MerchantVerificationProfileResult result)
    {
        return new MerchantVerificationProfileResponse(
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
            result.VerifiedAtUtc,
            result.ExpiredAtUtc,
            result.ReviewNote);
    }

    private static MerchantVerificationDocumentResponse MapDocument(
        MerchantVerificationDocumentResult result)
    {
        return new MerchantVerificationDocumentResponse(
            result.DocumentId.Value,
            result.DocumentType.ToString(),
            result.IssuingCountryCode,
            result.HolderName,
            result.IssueDate,
            result.ExpiryDate,
            result.ReviewStatus.ToString(),
            result.ReviewNote,
            result.ReviewedAtUtc,
            result.Files
                .Select(
                    MapFile)
                .ToArray());
    }

    private static MerchantVerificationFileResponse MapFile(
        MerchantVerificationFileResult result)
    {
        return new MerchantVerificationFileResponse(
            result.FileId.Value,
            result.Side.ToString(),
            result.OriginalFileName,
            result.ContentType,
            result.FileSizeBytes,
            result.CreatedAtUtc);
    }

    private static UserId? GetActor(
        HttpContext httpContext)
    {
        var subject =
            httpContext.User
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        if (!Guid.TryParse(
                subject,
                out var value) ||
            value ==
                Guid.Empty)
        {
            return null;
        }

        return UserId.From(
            value);
    }

    private static bool TryParseEnum<TEnum>(
        string? value,
        out TEnum parsed)
        where TEnum :
            struct,
            Enum
    {
        parsed =
            default;

        if (string.IsNullOrWhiteSpace(
                value) ||
            !Enum.TryParse(
                value.Trim(),
                ignoreCase:
                    true,
                out parsed) ||
            Convert.ToInt32(
                parsed) ==
                0 ||
            !Enum.IsDefined(
                parsed))
        {
            return false;
        }

        return true;
    }

    private static IResult MapException(
        Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException =>
                Results.NotFound(
                    new
                    {
                        code =
                            "merchant_verification_not_found",

                        message =
                            exception.Message
                    }),

            InvalidOperationException =>
                Results.Conflict(
                    new
                    {
                        code =
                            "merchant_verification_invalid_state",

                        message =
                            exception.Message
                    }),

            ArgumentException =>
                BadRequest(
                    "merchant_verification_invalid",
                    exception.Message),

            _ =>
                throw exception
        };
    }

    private static IResult BadRequest(
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