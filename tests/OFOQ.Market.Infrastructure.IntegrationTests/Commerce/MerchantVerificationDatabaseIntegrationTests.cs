using Microsoft.EntityFrameworkCore;
using Npgsql;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class MerchantVerificationDatabaseIntegrationTests
{
    private const long MaximumDocumentFileSizeBytes =
        15L * 1024L * 1024L;

    private const int PostgreSqlMaximumIdentifierLength =
        63;

    private readonly IntegrationTestDatabase _database =
        IntegrationTestDatabase.Create();

    [Fact]
    public async Task Database_RejectsSecondVerificationProfileForSameTenant()
    {
        await _database.ResetAsync();

        var identity =
            await CreateTenantAndUserAsync(
                "Single Verification Profile Store");

        await using var dbContext =
            _database.CreateContext();

        await InsertProfileAsync(
            dbContext,
            Guid.NewGuid(),
            identity.Tenant.Id,
            identity.User.Id,
            "First Legal Name");

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    InsertProfileAsync(
                        dbContext,
                        Guid.NewGuid(),
                        identity.Tenant.Id,
                        identity.User.Id,
                        "Second Legal Name"));

        AssertPostgresConstraint(
            exception,
            "23505",
            "ux_commerce_merchant_verification_profiles_tenant");
    }

    [Fact]
    public async Task Database_RejectsProfileForNonExistingPrincipalUser()
    {
        await _database.ResetAsync();

        var tenant =
            await CreateTenantAsync(
                "Missing Principal Store");

        await using var dbContext =
            _database.CreateContext();

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    InsertProfileAsync(
                        dbContext,
                        Guid.NewGuid(),
                        tenant.Id,
                        UserId.New(),
                        "Missing Principal"));

        AssertPostgresConstraint(
            exception,
            "23503",
            "fk_commerce_merchant_verification_profiles_users");
    }

    [Fact]
    public async Task Database_RejectsDocumentReferencingProfileFromAnotherTenant()
    {
        await _database.ResetAsync();

        var firstIdentity =
            await CreateTenantAndUserAsync(
                "Document Tenant A");

        var secondIdentity =
            await CreateTenantAndUserAsync(
                "Document Tenant B");

        var firstProfileId =
            Guid.NewGuid();

        await using var dbContext =
            _database.CreateContext();

        await InsertProfileAsync(
            dbContext,
            firstProfileId,
            firstIdentity.Tenant.Id,
            firstIdentity.User.Id,
            "Tenant A Owner");

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    InsertDocumentAsync(
                        dbContext,
                        Guid.NewGuid(),
                        secondIdentity.Tenant.Id,
                        firstProfileId,
                        Fingerprint('a'),
                        "Tenant B Holder"));

        AssertPostgresConstraint(
            exception,
            "23503",
            "fk_commerce_merchant_verification_documents_profiles");
    }

    [Fact]
    public async Task Database_RejectsDuplicateDocumentFingerprintWithinSameProfile()
    {
        await _database.ResetAsync();

        var identity =
            await CreateTenantAndUserAsync(
                "Duplicate Fingerprint Store");

        var profileId =
            Guid.NewGuid();

        var fingerprint =
            Fingerprint('b');

        await using var dbContext =
            _database.CreateContext();

        await InsertProfileAsync(
            dbContext,
            profileId,
            identity.Tenant.Id,
            identity.User.Id,
            "Fingerprint Owner");

        await InsertDocumentAsync(
            dbContext,
            Guid.NewGuid(),
            identity.Tenant.Id,
            profileId,
            fingerprint,
            "Fingerprint Owner");

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    InsertDocumentAsync(
                        dbContext,
                        Guid.NewGuid(),
                        identity.Tenant.Id,
                        profileId,
                        fingerprint,
                        "Fingerprint Owner"));

        AssertPostgresConstraint(
            exception,
            "23505",
            "ux_commerce_merchant_verification_documents_profile_fingerprint");
    }

    [Fact]
    public async Task Database_AllowsSameDocumentFingerprintAcrossDifferentTenants()
    {
        await _database.ResetAsync();

        var firstIdentity =
            await CreateTenantAndUserAsync(
                "Shared Identity Store A");

        var secondIdentity =
            await CreateTenantAndUserAsync(
                "Shared Identity Store B");

        var firstProfileId =
            Guid.NewGuid();

        var secondProfileId =
            Guid.NewGuid();

        var fingerprint =
            Fingerprint('c');

        await using (
            var dbContext =
                _database.CreateContext())
        {
            await InsertProfileAsync(
                dbContext,
                firstProfileId,
                firstIdentity.Tenant.Id,
                firstIdentity.User.Id,
                "Shared Person");

            await InsertProfileAsync(
                dbContext,
                secondProfileId,
                secondIdentity.Tenant.Id,
                secondIdentity.User.Id,
                "Shared Person");

            await InsertDocumentAsync(
                dbContext,
                Guid.NewGuid(),
                firstIdentity.Tenant.Id,
                firstProfileId,
                fingerprint,
                "Shared Person");

            await InsertDocumentAsync(
                dbContext,
                Guid.NewGuid(),
                secondIdentity.Tenant.Id,
                secondProfileId,
                fingerprint,
                "Shared Person");
        }

        await using var verificationContext =
            _database.CreateContext();

        var count =
            await verificationContext
                .MerchantVerificationDocuments
                .IgnoreQueryFilters()
                .CountAsync(
                    document =>
                        document.DocumentNumberFingerprint ==
                        fingerprint);

        Assert.Equal(
            2,
            count);
    }

    [Fact]
    public async Task Database_RejectsDocumentWhoseExpiryPrecedesIssueDate()
    {
        await _database.ResetAsync();

        var identity =
            await CreateTenantAndUserAsync(
                "Invalid Document Dates Store");

        var profileId =
            Guid.NewGuid();

        await using var dbContext =
            _database.CreateContext();

        await InsertProfileAsync(
            dbContext,
            profileId,
            identity.Tenant.Id,
            identity.User.Id,
            "Date Test Owner");

        var issueDate =
            new DateOnly(
                2026,
                9,
                12);

        var expiryDate =
            new DateOnly(
                2026,
                9,
                11);

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    InsertDocumentAsync(
                        dbContext,
                        Guid.NewGuid(),
                        identity.Tenant.Id,
                        profileId,
                        Fingerprint('d'),
                        "Date Test Owner",
                        issueDate,
                        expiryDate));

        AssertPostgresConstraint(
            exception,
            "23514",
            "ck_commerce_merchant_verification_documents_dates");
    }

    [Fact]
    public async Task Database_RejectsFileReferencingDocumentFromAnotherTenant()
    {
        await _database.ResetAsync();

        var firstIdentity =
            await CreateTenantAndUserAsync(
                "File Tenant A");

        var secondIdentity =
            await CreateTenantAndUserAsync(
                "File Tenant B");

        var firstProfileId =
            Guid.NewGuid();

        var firstDocumentId =
            Guid.NewGuid();

        await using var dbContext =
            _database.CreateContext();

        await InsertProfileAsync(
            dbContext,
            firstProfileId,
            firstIdentity.Tenant.Id,
            firstIdentity.User.Id,
            "File Tenant A Owner");

        await InsertDocumentAsync(
            dbContext,
            firstDocumentId,
            firstIdentity.Tenant.Id,
            firstProfileId,
            Fingerprint('e'),
            "File Tenant A Owner");

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    InsertFileAsync(
                        dbContext,
                        Guid.NewGuid(),
                        secondIdentity.Tenant.Id,
                        firstDocumentId,
                        $"private/{Guid.NewGuid():N}",
                        Sha256('e'),
                        1024));

        AssertPostgresConstraint(
            exception,
            "23503",
            "fk_commerce_merchant_verification_document_files_documents");
    }

    [Fact]
    public async Task Database_RejectsDuplicatePrivateStorageKeyGlobally()
    {
        await _database.ResetAsync();

        var first =
            await CreateVerificationDocumentSeedAsync(
                "Storage Key Store A",
                'f');

        var second =
            await CreateVerificationDocumentSeedAsync(
                "Storage Key Store B",
                'g');

        var storageKey =
            $"private/verification/{Guid.NewGuid():N}";

        await using var dbContext =
            _database.CreateContext();

        await InsertFileAsync(
            dbContext,
            Guid.NewGuid(),
            first.TenantId,
            first.DocumentId,
            storageKey,
            Sha256('f'),
            1024);

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    InsertFileAsync(
                        dbContext,
                        Guid.NewGuid(),
                        second.TenantId,
                        second.DocumentId,
                        storageKey,
                        Sha256('g'),
                        2048));

        AssertPostgresConstraint(
            exception,
            "23505",
            "ux_commerce_merchant_verification_document_files_storage_key");
    }

    [Fact]
    public async Task Database_RejectsDuplicateBinaryWithinSameDocument()
    {
        await _database.ResetAsync();

        var seed =
            await CreateVerificationDocumentSeedAsync(
                "Duplicate Binary Store",
                'h');

        var sha256 =
            Sha256('h');

        await using var dbContext =
            _database.CreateContext();

        await InsertFileAsync(
            dbContext,
            Guid.NewGuid(),
            seed.TenantId,
            seed.DocumentId,
            $"private/{Guid.NewGuid():N}",
            sha256,
            1024);

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    InsertFileAsync(
                        dbContext,
                        Guid.NewGuid(),
                        seed.TenantId,
                        seed.DocumentId,
                        $"private/{Guid.NewGuid():N}",
                        sha256,
                        1024));

        AssertPostgresConstraint(
            exception,
            "23505",
            "ux_commerce_merchant_verification_document_files_document_sha256");
    }

    [Fact]
    public async Task Database_RejectsVerificationFileLargerThanFifteenMegabytes()
    {
        await _database.ResetAsync();

        var seed =
            await CreateVerificationDocumentSeedAsync(
                "Oversized File Store",
                'i');

        await using var dbContext =
            _database.CreateContext();

        var exception =
            await Assert.ThrowsAnyAsync<Exception>(
                () =>
                    InsertFileAsync(
                        dbContext,
                        Guid.NewGuid(),
                        seed.TenantId,
                        seed.DocumentId,
                        $"private/{Guid.NewGuid():N}",
                        Sha256('i'),
                        MaximumDocumentFileSizeBytes + 1));

        AssertPostgresConstraint(
            exception,
            "23514",
            "ck_commerce_merchant_verification_document_files_size");
    }

    [Fact]
    public async Task QueryFilters_IsolateMerchantVerificationDataByTenant()
    {
        await _database.ResetAsync();

        var first =
            await CreateCompleteVerificationSeedAsync(
                "Isolation Store A",
                'j');

        var second =
            await CreateCompleteVerificationSeedAsync(
                "Isolation Store B",
                'k');

        await using (
            var firstContext =
                _database.CreateContext(
                    new TestCurrentTenant(
                        first.TenantId)))
        {
            var profile =
                await firstContext
                    .MerchantVerificationProfiles
                    .SingleAsync();

            var document =
                await firstContext
                    .MerchantVerificationDocuments
                    .SingleAsync();

            var file =
                await firstContext
                    .MerchantVerificationDocumentFiles
                    .SingleAsync();

            Assert.Equal(
                first.ProfileId,
                profile.Id.Value);

            Assert.Equal(
                first.DocumentId,
                document.Id.Value);

            Assert.Equal(
                first.FileId,
                file.Id.Value);
        }

        await using (
            var secondContext =
                _database.CreateContext(
                    new TestCurrentTenant(
                        second.TenantId)))
        {
            var profile =
                await secondContext
                    .MerchantVerificationProfiles
                    .SingleAsync();

            var document =
                await secondContext
                    .MerchantVerificationDocuments
                    .SingleAsync();

            var file =
                await secondContext
                    .MerchantVerificationDocumentFiles
                    .SingleAsync();

            Assert.Equal(
                second.ProfileId,
                profile.Id.Value);

            Assert.Equal(
                second.DocumentId,
                document.Id.Value);

            Assert.Equal(
                second.FileId,
                file.Id.Value);
        }
    }

    [Fact]
    public async Task QueryWithoutTenantContext_ReturnsNoMerchantVerificationData()
    {
        await _database.ResetAsync();

        await CreateCompleteVerificationSeedAsync(
            "No Tenant Query Store",
            'l');

        await using var dbContext =
            _database.CreateContext();

        Assert.Empty(
            await dbContext
                .MerchantVerificationProfiles
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .MerchantVerificationDocuments
                .ToArrayAsync());

        Assert.Empty(
            await dbContext
                .MerchantVerificationDocumentFiles
                .ToArrayAsync());
    }

    private async Task<IdentitySeed> CreateTenantAndUserAsync(
        string storeName)
    {
        var suffix =
            Guid.NewGuid()
                .ToString(
                    "N");

        var now =
            DateTimeOffset.UtcNow;

        var tenant =
            Tenant.Create(
                storeName,
                $"verification-{suffix}",
                now);

        var user =
            User.Create(
                $"verification-owner-{suffix}@example.com",
                $"verification-hash-{suffix}",
                now);

        await using var dbContext =
            _database.CreateContext();

        dbContext.Tenants.Add(
            tenant);

        dbContext.Users.Add(
            user);

        await dbContext.SaveChangesAsync();

        return new IdentitySeed(
            tenant,
            user);
    }

    private async Task<Tenant> CreateTenantAsync(
        string storeName)
    {
        var suffix =
            Guid.NewGuid()
                .ToString(
                    "N");

        var tenant =
            Tenant.Create(
                storeName,
                $"verification-{suffix}",
                DateTimeOffset.UtcNow);

        await using var dbContext =
            _database.CreateContext();

        dbContext.Tenants.Add(
            tenant);

        await dbContext.SaveChangesAsync();

        return tenant;
    }

    private async Task<VerificationDocumentSeed>
        CreateVerificationDocumentSeedAsync(
            string storeName,
            char fingerprintCharacter)
    {
        var identity =
            await CreateTenantAndUserAsync(
                storeName);

        var profileId =
            Guid.NewGuid();

        var documentId =
            Guid.NewGuid();

        await using var dbContext =
            _database.CreateContext();

        await InsertProfileAsync(
            dbContext,
            profileId,
            identity.Tenant.Id,
            identity.User.Id,
            $"{storeName} Legal Owner");

        await InsertDocumentAsync(
            dbContext,
            documentId,
            identity.Tenant.Id,
            profileId,
            Fingerprint(
                fingerprintCharacter),
            $"{storeName} Holder");

        return new VerificationDocumentSeed(
            identity.Tenant.Id,
            profileId,
            documentId);
    }

    private async Task<CompleteVerificationSeed>
        CreateCompleteVerificationSeedAsync(
            string storeName,
            char fingerprintCharacter)
    {
        var documentSeed =
            await CreateVerificationDocumentSeedAsync(
                storeName,
                fingerprintCharacter);

        var fileId =
            Guid.NewGuid();

        await using var dbContext =
            _database.CreateContext();

        await InsertFileAsync(
            dbContext,
            fileId,
            documentSeed.TenantId,
            documentSeed.DocumentId,
            $"private/verification/{Guid.NewGuid():N}",
            Sha256(
                fingerprintCharacter),
            2048);

        return new CompleteVerificationSeed(
            documentSeed.TenantId,
            documentSeed.ProfileId,
            documentSeed.DocumentId,
            fileId);
    }

    private static async Task InsertProfileAsync(
        MarketDbContext dbContext,
        Guid profileId,
        TenantId tenantId,
        UserId principalUserId,
        string legalName)
    {
        var now =
            DateTimeOffset.UtcNow;

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO commerce_merchant_verification_profiles
                (
                    id,
                    tenant_id,
                    principal_user_id,
                    subject_type,
                    country_code,
                    legal_name,
                    status,
                    created_at_utc,
                    created_by_user_id
                )
                VALUES
                (
                    {profileId},
                    {tenantId.Value},
                    {principalUserId.Value},
                    10,
                    'SA',
                    {legalName},
                    10,
                    {now},
                    {principalUserId.Value}
                );
                """);
    }

    private static async Task InsertDocumentAsync(
        MarketDbContext dbContext,
        Guid documentId,
        TenantId tenantId,
        Guid profileId,
        string fingerprint,
        string holderName,
        DateOnly? issueDate = null,
        DateOnly? expiryDate = null)
    {
        var now =
            DateTimeOffset.UtcNow;

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO commerce_merchant_verification_documents
                (
                    id,
                    tenant_id,
                    profile_id,
                    document_type,
                    issuing_country_code,
                    holder_name,
                    protected_document_number,
                    document_number_fingerprint,
                    issue_date,
                    expiry_date,
                    review_status,
                    created_at_utc
                )
                VALUES
                (
                    {documentId},
                    {tenantId.Value},
                    {profileId},
                    10,
                    'SA',
                    {holderName},
                    'protected-document-number',
                    {fingerprint},
                    {issueDate},
                    {expiryDate},
                    10,
                    {now}
                );
                """);
    }

    private static async Task InsertFileAsync(
        MarketDbContext dbContext,
        Guid fileId,
        TenantId tenantId,
        Guid documentId,
        string storageKey,
        string sha256,
        long fileSizeBytes)
    {
        var now =
            DateTimeOffset.UtcNow;

        await dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO commerce_merchant_verification_document_files
                (
                    id,
                    tenant_id,
                    document_id,
                    side,
                    storage_key,
                    original_file_name,
                    content_type,
                    file_size_bytes,
                    sha256,
                    created_at_utc
                )
                VALUES
                (
                    {fileId},
                    {tenantId.Value},
                    {documentId},
                    10,
                    {storageKey},
                    'verification-document.jpg',
                    'image/jpeg',
                    {fileSizeBytes},
                    {sha256},
                    {now}
                );
                """);
    }

    private static string Fingerprint(
        char character)
    {
        return new string(
            character,
            64);
    }

    private static string Sha256(
        char character)
    {
        return new string(
            character,
            64);
    }

    private static void AssertPostgresConstraint(
        Exception exception,
        string expectedSqlState,
        string expectedConstraintName)
    {
        var postgresException =
            FindPostgresException(
                exception);

        Assert.NotNull(
            postgresException);

        Assert.Equal(
            expectedSqlState,
            postgresException!.SqlState);

        var expectedDatabaseConstraintName =
            TruncatePostgreSqlIdentifier(
                expectedConstraintName);

        Assert.Equal(
            expectedDatabaseConstraintName,
            postgresException.ConstraintName);
    }

    private static string TruncatePostgreSqlIdentifier(
        string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identifier);

        return identifier.Length <=
               PostgreSqlMaximumIdentifierLength
            ? identifier
            : identifier[
                ..PostgreSqlMaximumIdentifierLength];
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        Exception? current =
            exception;

        while (current is not null)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }

            current =
                current.InnerException;
        }

        return null;
    }

    private sealed record IdentitySeed(
        Tenant Tenant,
        User User);

    private sealed record VerificationDocumentSeed(
        TenantId TenantId,
        Guid ProfileId,
        Guid DocumentId);

    private sealed record CompleteVerificationSeed(
        TenantId TenantId,
        Guid ProfileId,
        Guid DocumentId,
        Guid FileId);
}
