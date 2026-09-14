using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OFOQ.Market.Api.Tests.Support;
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Application.Common.Security;
using OFOQ.Market.Contracts.Commerce.Verification;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Api.Tests.Commerce;

public sealed class PlatformMerchantVerificationReviewApiTests
{
    private const string BaseUrl =
        "/api/platform/merchant-verification/reviews";

    [Fact]
    public async Task Queue_Unauthenticated_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                BaseUrl);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Queue_UserWithoutPlatformRole_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var reviewer =
            await CreateUserAsync(
                factory);

        SetAccessToken(
            factory,
            client,
            reviewer);

        var response =
            await client.GetAsync(
                BaseUrl);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Queue_ComplianceReviewer_CanListSubmittedProfiles()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var reviewer =
            await CreatePlatformReviewerAsync(
                factory);

        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryMerchantVerificationReviewStore>();

        var submitted =
            CreateSubmittedProfile(
                "Submitted Merchant");

        var draft =
            CreateDraftProfile(
                "Draft Merchant");

        store.AddProfile(
            submitted);

        store.AddProfile(
            draft);

        SetAccessToken(
            factory,
            client,
            reviewer);

        var response =
            await client.GetAsync(
                $"{BaseUrl}?status=Submitted&take=20");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var profiles =
            await response.Content
                .ReadFromJsonAsync<
                    List<PlatformMerchantVerificationReviewSummaryResponse>>();

        Assert.NotNull(
            profiles);

        var profile =
            Assert.Single(
                profiles);

        Assert.Equal(
            submitted.Id.Value,
            profile.ProfileId);

        Assert.Equal(
            "Submitted",
            profile.Status);

        Assert.Equal(
            "Submitted Merchant",
            profile.LegalName);
    }

    [Fact]
    public async Task Detail_ReturnsSafeDocumentMetadata()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var reviewer =
            await CreatePlatformReviewerAsync(
                factory);

        var store =
            factory.Services
                .GetRequiredService<
                    InMemoryMerchantVerificationReviewStore>();

        var profile =
            CreateSubmittedProfile(
                "Document Merchant");

        var document =
            MerchantVerificationDocument.Create(
                profile.TenantId,
                profile.Id,
                MerchantVerificationDocumentType.NationalId,
                "SA",
                "Document Holder",
                "protected-document-number",
                new string(
                    'a',
                    64),
                new DateOnly(
                    2024,
                    1,
                    1),
                new DateOnly(
                    2030,
                    1,
                    1),
                DateTimeOffset.UtcNow,
                profile.PrincipalUserId.Value);

        var file =
            MerchantVerificationDocumentFile.Create(
                profile.TenantId,
                document.Id,
                MerchantVerificationDocumentSide.Front,
                "private/kyc/secret-storage-key",
                "national-id-front.jpg",
                "image/jpeg",
                12345,
                new string(
                    'b',
                    64),
                DateTimeOffset.UtcNow,
                profile.PrincipalUserId.Value);

        store.AddProfile(
            profile);

        store.AddDocument(
            document);

        store.AddFile(
            file);

        SetAccessToken(
            factory,
            client,
            reviewer);

        var response =
            await client.GetAsync(
                $"{BaseUrl}/{profile.Id.Value}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var detail =
            await response.Content
                .ReadFromJsonAsync<
                    PlatformMerchantVerificationReviewDetailResponse>();

        Assert.NotNull(
            detail);

        Assert.Equal(
            profile.Id.Value,
            detail.Profile.ProfileId);

        var responseDocument =
            Assert.Single(
                detail.Documents);

        Assert.Equal(
            "NationalId",
            responseDocument.DocumentType);

        var responseFile =
            Assert.Single(
                responseDocument.Files);

        Assert.Equal(
            "national-id-front.jpg",
            responseFile.OriginalFileName);

        var rawJson =
            await response.Content
                .ReadAsStringAsync();

        Assert.DoesNotContain(
            "secret-storage-key",
            rawJson,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "protected-document-number",
            rawJson,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            new string(
                'b',
                64),
            rawJson,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartReview_TransitionsSubmittedProfileToUnderReview()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var reviewer =
            await CreatePlatformReviewerAsync(
                factory);

        var profile =
            CreateSubmittedProfile(
                "Start Review Merchant");

        SeedProfile(
            factory,
            profile);

        SetAccessToken(
            factory,
            client,
            reviewer);

        var response =
            await client.PostAsync(
                $"{BaseUrl}/{profile.Id.Value}/start",
                content:
                    null);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    PlatformMerchantVerificationReviewActionResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            "UnderReview",
            result.Status);

        Assert.Equal(
            reviewer.Id.Value,
            result.ReviewerUserId);

        Assert.Equal(
            MerchantVerificationStatus.UnderReview,
            profile.Status);
    }

    [Fact]
    public async Task RequestMoreInformation_TransitionsProfile()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var reviewer =
            await CreatePlatformReviewerAsync(
                factory);

        var profile =
            CreateSubmittedProfile(
                "More Information Merchant");

        SeedProfile(
            factory,
            profile);

        SetAccessToken(
            factory,
            client,
            reviewer);

        var response =
            await client.PostAsJsonAsync(
                $"{BaseUrl}/{profile.Id.Value}/request-more-information",
                new PlatformMerchantVerificationRequiredNoteRequest(
                    "Please upload a clearer identity document."));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            MerchantVerificationStatus.RequiresMoreInformation,
            profile.Status);

        Assert.Equal(
            "Please upload a clearer identity document.",
            profile.ReviewNote);
    }

    [Fact]
    public async Task Reject_TransitionsProfile()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var reviewer =
            await CreatePlatformReviewerAsync(
                factory);

        var profile =
            CreateSubmittedProfile(
                "Rejected Merchant");

        SeedProfile(
            factory,
            profile);

        SetAccessToken(
            factory,
            client,
            reviewer);

        var response =
            await client.PostAsJsonAsync(
                $"{BaseUrl}/{profile.Id.Value}/reject",
                new PlatformMerchantVerificationRequiredNoteRequest(
                    "Submitted evidence could not be validated."));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            MerchantVerificationStatus.Rejected,
            profile.Status);

        Assert.Equal(
            "Submitted evidence could not be validated.",
            profile.ReviewNote);
    }

    [Fact]
    public async Task Verify_AfterStartReview_TransitionsProfileToVerified()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var reviewer =
            await CreatePlatformReviewerAsync(
                factory);

        var profile =
            CreateSubmittedProfile(
                "Verified Merchant");

        SeedProfile(
            factory,
            profile);

        SetAccessToken(
            factory,
            client,
            reviewer);

        var startResponse =
            await client.PostAsync(
                $"{BaseUrl}/{profile.Id.Value}/start",
                content:
                    null);

        Assert.Equal(
            HttpStatusCode.OK,
            startResponse.StatusCode);

        var verifyResponse =
            await client.PostAsJsonAsync(
                $"{BaseUrl}/{profile.Id.Value}/verify",
                new PlatformMerchantVerificationVerifyRequest(
                    "Evidence verified."));

        Assert.Equal(
            HttpStatusCode.OK,
            verifyResponse.StatusCode);

        var result =
            await verifyResponse.Content
                .ReadFromJsonAsync<
                    PlatformMerchantVerificationReviewActionResponse>();

        Assert.NotNull(
            result);

        Assert.Equal(
            "Verified",
            result.Status);

        Assert.Equal(
            reviewer.Id.Value,
            result.ReviewerUserId);

        Assert.Equal(
            MerchantVerificationStatus.Verified,
            profile.Status);

        Assert.NotNull(
            profile.VerifiedAtUtc);
    }

    private static void SeedProfile(
        MarketApiFactory factory,
        MerchantVerificationProfile profile)
    {
        factory.Services
            .GetRequiredService<
                InMemoryMerchantVerificationReviewStore>()
            .AddProfile(
                profile);
    }

    private static MerchantVerificationProfile
        CreateSubmittedProfile(
            string legalName)
    {
        var profile =
            CreateDraftProfile(
                legalName);

        profile.Submit(
            DateTimeOffset.UtcNow,
            profile.PrincipalUserId.Value);

        return profile;
    }

    private static MerchantVerificationProfile
        CreateDraftProfile(
            string legalName)
    {
        return MerchantVerificationProfile.Create(
            TenantId.New(),
            UserId.New(),
            MerchantVerificationSubjectType.Individual,
            "SA",
            legalName,
            DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    private static async Task<User>
        CreatePlatformReviewerAsync(
            MarketApiFactory factory)
    {
        var user =
            await CreateUserAsync(
                factory);

        var assignment =
            PlatformUserRoleAssignment.Create(
                user.Id,
                PlatformRole.ComplianceReviewer,
                DateTimeOffset.UtcNow,
                user.Id.Value);

        var roleRepository =
            factory.Services
                .GetRequiredService<
                    IPlatformUserRoleAssignmentRepository>();

        await roleRepository.AddAsync(
            assignment);

        return user;
    }

    private static async Task<User>
        CreateUserAsync(
            MarketApiFactory factory)
    {
        var user =
            User.Create(
                $"platform-api-{Guid.NewGuid():N}@example.com",
                "integration-test-password-hash",
                DateTimeOffset.UtcNow);

        var repository =
            factory.Services
                .GetRequiredService<
                    IUserRepository>();

        await repository.AddAsync(
            user);

        return user;
    }

    private static void SetAccessToken(
        MarketApiFactory factory,
        HttpClient client,
        User user)
    {
        var accessTokenService =
            factory.Services
                .GetRequiredService<
                    IAccessTokenService>();

        var token =
            accessTokenService.Create(
                user.Id,
                user.Email.Value,
                DateTimeOffset.UtcNow,
                AccessTokenAuthenticationLevel.MultiFactor);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token.Token);
    }
}