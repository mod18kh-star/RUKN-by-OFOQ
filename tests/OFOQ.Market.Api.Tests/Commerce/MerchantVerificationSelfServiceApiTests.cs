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

public sealed class MerchantVerificationSelfServiceApiTests
{
    [Fact]
    public async Task SelfService_Unauthenticated_ReturnsUnauthorized()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                $"/api/tenants/{Guid.NewGuid()}/merchant-verification");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task SelfService_UserWithoutTenantMembership_ReturnsForbidden()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var user =
            await CreateUserAsync(
                factory);

        var tenant =
            Tenant.Create(
                "Verification Tenant",
                $"verification-{Guid.NewGuid():N}",
                DateTimeOffset.UtcNow,
                user.Id.Value);

        await factory.Services
            .GetRequiredService<ITenantRepository>()
            .AddAsync(
                tenant);

        SetAccessToken(
            factory,
            client,
            user);

        var response =
            await client.GetAsync(
                BaseUrl(
                    tenant.Id));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithoutDocuments_ReturnsConflict()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMerchantAsync(
                factory);

        SetAccessToken(
            factory,
            client,
            setup.User);

        var profileResponse =
            await client.PutAsJsonAsync(
                $"{BaseUrl(setup.Tenant.Id)}/profile",
                new MerchantVerificationProfileRequest(
                    "Individual",
                    "SA",
                    "Verification Owner"));

        Assert.Equal(
            HttpStatusCode.OK,
            profileResponse.StatusCode);

        var submitResponse =
            await client.PostAsync(
                $"{BaseUrl(setup.Tenant.Id)}/submit",
                content:
                    null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            submitResponse.StatusCode);
    }

    [Fact]
    public async Task FullMerchantLifecycle_AndPlatformReviewer_CanOpenPrivateFile()
    {
        await using var factory =
            new MarketApiFactory();

        using var client =
            factory.CreateClient();

        var setup =
            await CreateMerchantAsync(
                factory);

        SetAccessToken(
            factory,
            client,
            setup.User);

        // -------------------------------------------------
        // Profile
        // -------------------------------------------------

        var profileResponse =
            await client.PutAsJsonAsync(
                $"{BaseUrl(setup.Tenant.Id)}/profile",
                new MerchantVerificationProfileRequest(
                    "Individual",
                    "SA",
                    "Verification Owner"));

        Assert.Equal(
            HttpStatusCode.OK,
            profileResponse.StatusCode);

        var profile =
            await profileResponse.Content
                .ReadFromJsonAsync<
                    MerchantVerificationProfileResponse>();

        Assert.NotNull(
            profile);

        Assert.Equal(
            "Draft",
            profile.Status);

        // -------------------------------------------------
        // Document
        // -------------------------------------------------

        var documentResponse =
            await client.PostAsJsonAsync(
                $"{BaseUrl(setup.Tenant.Id)}/documents",
                new MerchantVerificationDocumentRequest(
                    "NationalId",
                    "SA",
                    "Verification Owner",
                    "1234567890",
                    new DateOnly(
                        2024,
                        1,
                        1),
                    new DateOnly(
                        2034,
                        1,
                        1)));

        Assert.Equal(
            HttpStatusCode.OK,
            documentResponse.StatusCode);

        var document =
            await documentResponse.Content
                .ReadFromJsonAsync<
                    MerchantVerificationDocumentResponse>();

        Assert.NotNull(
            document);

        // -------------------------------------------------
        // Private file upload
        // -------------------------------------------------

        var expectedBytes =
            "private-kyc-document"u8.ToArray();

        using var multipart =
            new MultipartFormDataContent();

        using var fileContent =
            new ByteArrayContent(
                expectedBytes);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                "image/jpeg");

        multipart.Add(
            fileContent,
            "file",
            "identity-front.jpg");

        var uploadResponse =
            await client.PostAsync(
                $"{BaseUrl(setup.Tenant.Id)}/documents/{document.DocumentId}/files/Front",
                multipart);

        Assert.Equal(
            HttpStatusCode.OK,
            uploadResponse.StatusCode);

        var uploadedFile =
            await uploadResponse.Content
                .ReadFromJsonAsync<
                    MerchantVerificationFileResponse>();

        Assert.NotNull(
            uploadedFile);

        Assert.Equal(
            "identity-front.jpg",
            uploadedFile.OriginalFileName);

        // -------------------------------------------------
        // Merchant can retrieve own private file
        // -------------------------------------------------

        var ownFileResponse =
            await client.GetAsync(
                $"{BaseUrl(setup.Tenant.Id)}/files/{uploadedFile.FileId}");

        Assert.Equal(
            HttpStatusCode.OK,
            ownFileResponse.StatusCode);

        Assert.Equal(
            expectedBytes,
            await ownFileResponse.Content.ReadAsByteArrayAsync());

        // -------------------------------------------------
        // Self-service state contains safe metadata
        // -------------------------------------------------

        var selfServiceResponse =
            await client.GetAsync(
                BaseUrl(
                    setup.Tenant.Id));

        Assert.Equal(
            HttpStatusCode.OK,
            selfServiceResponse.StatusCode);

        var selfService =
            await selfServiceResponse.Content
                .ReadFromJsonAsync<
                    MerchantVerificationSelfServiceResponse>();

        Assert.NotNull(
            selfService);

        Assert.NotNull(
            selfService.Profile);

        Assert.Single(
            selfService.Documents);

        Assert.Single(
            selfService.Documents[0].Files);

        var rawSelfServiceJson =
            await selfServiceResponse.Content
                .ReadAsStringAsync();

        Assert.DoesNotContain(
            "1234567890",
            rawSelfServiceJson,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "protected:",
            rawSelfServiceJson,
            StringComparison.OrdinalIgnoreCase);

        // -------------------------------------------------
        // Submit
        // -------------------------------------------------

        var submitResponse =
            await client.PostAsync(
                $"{BaseUrl(setup.Tenant.Id)}/submit",
                content:
                    null);

        Assert.Equal(
            HttpStatusCode.OK,
            submitResponse.StatusCode);

        var submitted =
            await submitResponse.Content
                .ReadFromJsonAsync<
                    MerchantVerificationProfileResponse>();

        Assert.NotNull(
            submitted);

        Assert.Equal(
            "Submitted",
            submitted.Status);

        // Merchant cannot edit material under review.
        var editAfterSubmit =
            await client.PutAsJsonAsync(
                $"{BaseUrl(setup.Tenant.Id)}/profile",
                new MerchantVerificationProfileRequest(
                    "Individual",
                    "SA",
                    "Changed Name"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            editAfterSubmit.StatusCode);

        // -------------------------------------------------
        // Seed the platform review query fake with the same
        // stored domain objects. The private bytes remain in
        // the shared private-file store.
        // -------------------------------------------------

        var merchantStore =
            factory.Services
                .GetRequiredService<
                    InMemoryMerchantVerificationSelfServiceStore>();

        var profileEntity =
            merchantStore.GetProfileById(
                MerchantVerificationProfileId.From(
                    profile.ProfileId));

        var documentEntity =
            merchantStore.GetDocumentById(
                MerchantVerificationDocumentId.From(
                    document.DocumentId));

        var fileEntity =
            merchantStore.GetFileById(
                MerchantVerificationDocumentFileId.From(
                    uploadedFile.FileId));

        Assert.NotNull(
            profileEntity);

        Assert.NotNull(
            documentEntity);

        Assert.NotNull(
            fileEntity);

        var reviewStore =
            factory.Services
                .GetRequiredService<
                    InMemoryMerchantVerificationReviewStore>();

        reviewStore.AddProfile(
            profileEntity!);

        reviewStore.AddDocument(
            documentEntity!);

        reviewStore.AddFile(
            fileEntity!);

        // -------------------------------------------------
        // Platform reviewer
        // -------------------------------------------------

        var reviewer =
            await CreateUserAsync(
                factory);

        var roleRepository =
            factory.Services
                .GetRequiredService<
                    IPlatformUserRoleAssignmentRepository>();

        await roleRepository.AddAsync(
            PlatformUserRoleAssignment.Create(
                reviewer.Id,
                PlatformRole.ComplianceReviewer,
                DateTimeOffset.UtcNow,
                reviewer.Id.Value));

        SetAccessToken(
            factory,
            client,
            reviewer);

        var reviewerFileResponse =
            await client.GetAsync(
                $"/api/platform/merchant-verification/reviews/{profile.ProfileId}/files/{uploadedFile.FileId}");

        Assert.Equal(
            HttpStatusCode.OK,
            reviewerFileResponse.StatusCode);

        Assert.Equal(
            expectedBytes,
            await reviewerFileResponse.Content
                .ReadAsByteArrayAsync());
    }

    private static string BaseUrl(
        TenantId tenantId)
    {
        return
            $"/api/tenants/{tenantId.Value}/merchant-verification";
    }

    private static async Task<(User User, Tenant Tenant)>
        CreateMerchantAsync(
            MarketApiFactory factory)
    {
        var user =
            await CreateUserAsync(
                factory);

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                "KYC Merchant",
                $"kyc-{Guid.NewGuid():N}",
                now,
                user.Id.Value);

        await factory.Services
            .GetRequiredService<ITenantRepository>()
            .AddAsync(
                tenant);

        await factory.Services
            .GetRequiredService<
                ITenantMembershipRepository>()
            .AddAsync(
                TenantMembership.Create(
                    tenant.Id,
                    user.Id,
                    TenantRole.Owner,
                    now,
                    user.Id.Value));

        return (
            user,
            tenant);
    }

    private static async Task<User> CreateUserAsync(
        MarketApiFactory factory)
    {
        var user =
            User.Create(
                $"kyc-{Guid.NewGuid():N}@example.com",
                "test-password-hash",
                DateTimeOffset.UtcNow);

        await factory.Services
            .GetRequiredService<IUserRepository>()
            .AddAsync(
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