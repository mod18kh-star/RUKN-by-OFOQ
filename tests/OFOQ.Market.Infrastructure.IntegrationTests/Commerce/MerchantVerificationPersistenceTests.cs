using Microsoft.EntityFrameworkCore;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;
using OFOQ.Market.Infrastructure.Persistence;

namespace OFOQ.Market.Infrastructure.IntegrationTests.Commerce;

public sealed class MerchantVerificationPersistenceTests
{
    [Fact]
    public void Model_ContainsMerchantVerificationProfileMapping()
    {
        using var dbContext =
            CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(
                    MerchantVerificationProfile));

        Assert.NotNull(
            entityType);

        Assert.Equal(
            "commerce_merchant_verification_profiles",
            entityType.GetTableName());

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.GetDatabaseName() ==
                "ux_commerce_merchant_verification_profiles_tenant");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.GetDatabaseName() ==
                "ix_commerce_merchant_verification_profiles_principal_user");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.GetDatabaseName() ==
                "ix_commerce_merchant_verification_profiles_status");

        Assert.NotEmpty(
            entityType.GetDeclaredQueryFilters());
    }

    [Fact]
    public void Model_ContainsMerchantVerificationDocumentMapping()
    {
        using var dbContext =
            CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(
                    MerchantVerificationDocument));

        Assert.NotNull(
            entityType);

        Assert.Equal(
            "commerce_merchant_verification_documents",
            entityType.GetTableName());

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.GetDatabaseName() ==
                "ux_commerce_merchant_verification_documents_profile_fingerprint");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                !index.IsUnique &&
                index.GetDatabaseName() ==
                "ix_commerce_merchant_verification_documents_fingerprint");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.GetDatabaseName() ==
                "ix_commerce_merchant_verification_documents_profile_type");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.GetDatabaseName() ==
                "ix_commerce_merchant_verification_documents_review_status");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.GetDatabaseName() ==
                "ix_commerce_merchant_verification_documents_expiry_date");

        Assert.NotEmpty(
            entityType.GetDeclaredQueryFilters());
    }

    [Fact]
    public void Model_ContainsMerchantVerificationDocumentFileMapping()
    {
        using var dbContext =
            CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(
                    MerchantVerificationDocumentFile));

        Assert.NotNull(
            entityType);

        Assert.Equal(
            "commerce_merchant_verification_document_files",
            entityType.GetTableName());

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.GetDatabaseName() ==
                "ux_commerce_merchant_verification_document_files_storage_key");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.GetDatabaseName() ==
                "ux_commerce_merchant_verification_document_files_document_sha256");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.GetDatabaseName() ==
                "ix_commerce_merchant_verification_document_files_document_side");

        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.GetDatabaseName() ==
                "ix_commerce_merchant_verification_document_files_sha256");

        Assert.NotEmpty(
            entityType.GetDeclaredQueryFilters());
    }

    [Fact]
    public void Model_UsesRestrictVerificationRelationships()
    {
        using var dbContext =
            CreateDbContext();

        var profileType =
            dbContext.Model.FindEntityType(
                typeof(
                    MerchantVerificationProfile));

        var documentType =
            dbContext.Model.FindEntityType(
                typeof(
                    MerchantVerificationDocument));

        var fileType =
            dbContext.Model.FindEntityType(
                typeof(
                    MerchantVerificationDocumentFile));

        Assert.NotNull(
            profileType);

        Assert.NotNull(
            documentType);

        Assert.NotNull(
            fileType);

        Assert.All(
            profileType.GetForeignKeys(),
            foreignKey =>
                Assert.Equal(
                    DeleteBehavior.Restrict,
                    foreignKey.DeleteBehavior));

        Assert.All(
            documentType.GetForeignKeys(),
            foreignKey =>
                Assert.Equal(
                    DeleteBehavior.Restrict,
                    foreignKey.DeleteBehavior));

        Assert.All(
            fileType.GetForeignKeys(),
            foreignKey =>
                Assert.Equal(
                    DeleteBehavior.Restrict,
                    foreignKey.DeleteBehavior));
    }

    [Fact]
    public void Model_DocumentProfileRelationship_IsTenantAware()
    {
        using var dbContext =
            CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(
                    MerchantVerificationDocument));

        Assert.NotNull(
            entityType);

        var foreignKey =
            Assert.Single(
                entityType.GetForeignKeys(),
                foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(
                        MerchantVerificationProfile));

        var dependentProperties =
            foreignKey.Properties
                .Select(
                    property =>
                        property.Name)
                .ToArray();

        var principalProperties =
            foreignKey.PrincipalKey.Properties
                .Select(
                    property =>
                        property.Name)
                .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(
                    MerchantVerificationDocument.TenantId),
                nameof(
                    MerchantVerificationDocument.ProfileId)
            },
            dependentProperties);

        Assert.Equal(
            new[]
            {
                nameof(
                    MerchantVerificationProfile.TenantId),
                nameof(
                    MerchantVerificationProfile.Id)
            },
            principalProperties);
    }

    [Fact]
    public void Model_DocumentFileRelationship_IsTenantAware()
    {
        using var dbContext =
            CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(
                    MerchantVerificationDocumentFile));

        Assert.NotNull(
            entityType);

        var foreignKey =
            Assert.Single(
                entityType.GetForeignKeys(),
                foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                    typeof(
                        MerchantVerificationDocument));

        var dependentProperties =
            foreignKey.Properties
                .Select(
                    property =>
                        property.Name)
                .ToArray();

        var principalProperties =
            foreignKey.PrincipalKey.Properties
                .Select(
                    property =>
                        property.Name)
                .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(
                    MerchantVerificationDocumentFile.TenantId),
                nameof(
                    MerchantVerificationDocumentFile.DocumentId)
            },
            dependentProperties);

        Assert.Equal(
            new[]
            {
                nameof(
                    MerchantVerificationDocument.TenantId),
                nameof(
                    MerchantVerificationDocument.Id)
            },
            principalProperties);
    }

    private static MarketDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<MarketDbContext>()
                .UseNpgsql(
                    "Host=127.0.0.1;Port=5432;Database=unused;Username=unused;Password=unused")
                .Options;

        return new MarketDbContext(
            options,
            new TestCurrentTenant(
                TenantId.New()));
    }
}
