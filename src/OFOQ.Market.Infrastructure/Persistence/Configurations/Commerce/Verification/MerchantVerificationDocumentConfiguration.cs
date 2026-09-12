using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce.Verification;

internal sealed class MerchantVerificationDocumentConfiguration :
    IEntityTypeConfiguration<MerchantVerificationDocument>
{
    public void Configure(
        EntityTypeBuilder<MerchantVerificationDocument> builder)
    {
        builder.ToTable(
            "commerce_merchant_verification_documents",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_documents_type",
                    "document_type > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_documents_review_status",
                    "review_status > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_documents_country_code",
                    "char_length(issuing_country_code) = 2");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_documents_holder_name",
                    "char_length(btrim(holder_name)) > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_documents_protected_number",
                    "char_length(btrim(protected_document_number)) > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_documents_fingerprint",
                    "char_length(document_number_fingerprint) = 64");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_documents_dates",
                    "issue_date IS NULL OR expiry_date IS NULL OR expiry_date >= issue_date");
            });

        builder.HasKey(
            entity =>
                entity.Id);

        builder.Property(
                entity =>
                    entity.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    MerchantVerificationDocumentId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                entity =>
                    entity.TenantId)
            .HasColumnName(
                "tenant_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    TenantId.From(
                        value))
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ProfileId)
            .HasColumnName(
                "profile_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    MerchantVerificationProfileId.From(
                        value))
            .IsRequired();

        builder.Property(
                entity =>
                    entity.DocumentType)
            .HasColumnName(
                "document_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                entity =>
                    entity.IssuingCountryCode)
            .HasColumnName(
                "issuing_country_code")
            .HasMaxLength(
                2)
            .IsFixedLength()
            .IsRequired();

        builder.Property(
                entity =>
                    entity.HolderName)
            .HasColumnName(
                "holder_name")
            .HasMaxLength(
                MerchantVerificationDocument.MaxHolderNameLength)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ProtectedDocumentNumber)
            .HasColumnName(
                "protected_document_number")
            .HasMaxLength(
                MerchantVerificationDocument.MaxProtectedDocumentNumberLength)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.DocumentNumberFingerprint)
            .HasColumnName(
                "document_number_fingerprint")
            .HasMaxLength(
                MerchantVerificationDocument.DocumentNumberFingerprintLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(
                entity =>
                    entity.IssueDate)
            .HasColumnName(
                "issue_date")
            .HasColumnType(
                "date");

        builder.Property(
                entity =>
                    entity.ExpiryDate)
            .HasColumnName(
                "expiry_date")
            .HasColumnType(
                "date");

        builder.Property(
                entity =>
                    entity.ReviewStatus)
            .HasColumnName(
                "review_status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ReviewNote)
            .HasColumnName(
                "review_note")
            .HasMaxLength(
                MerchantVerificationDocument.MaxReviewNoteLength);

        builder.Property(
                entity =>
                    entity.ReviewedAtUtc)
            .HasColumnName(
                "reviewed_at_utc");

        builder.Property(
                entity =>
                    entity.ReviewedByUserId)
            .HasColumnName(
                "reviewed_by_user_id");

        builder.Property(
                entity =>
                    entity.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                entity =>
                    entity.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                entity =>
                    entity.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                entity =>
                    entity.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        // Prevent the same protected identity/business
        // document from being added twice to one profile.
        builder.HasIndex(
                entity =>
                    new
                    {
                        entity.TenantId,
                        entity.ProfileId,
                        entity.DocumentNumberFingerprint
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_merchant_verification_documents_profile_fingerprint");

        // Global, non-unique fingerprint index.
        //
        // This intentionally allows one verified person/business
        // to be related to multiple stores while still allowing
        // fraud/risk tooling to discover those relationships.
        builder.HasIndex(
                entity =>
                    entity.DocumentNumberFingerprint)
            .HasDatabaseName(
                "ix_commerce_merchant_verification_documents_fingerprint");

        builder.HasIndex(
                entity =>
                    new
                    {
                        entity.TenantId,
                        entity.ProfileId,
                        entity.DocumentType
                    })
            .HasDatabaseName(
                "ix_commerce_merchant_verification_documents_profile_type");

        builder.HasIndex(
                entity =>
                    entity.ReviewStatus)
            .HasDatabaseName(
                "ix_commerce_merchant_verification_documents_review_status");

        builder.HasIndex(
                entity =>
                    entity.ExpiryDate)
            .HasDatabaseName(
                "ix_commerce_merchant_verification_documents_expiry_date");

        // Required so document files can later use a
        // tenant-aware composite foreign key.
        builder.HasAlternateKey(
                entity =>
                    new
                    {
                        entity.TenantId,
                        entity.Id
                    })
            .HasName(
                "ak_commerce_merchant_verification_documents_tenant_id");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    entity.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_merchant_verification_documents_tenants");

        builder.HasOne<MerchantVerificationProfile>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    new
                    {
                        entity.TenantId,
                        entity.ProfileId
                    })
            .HasPrincipalKey(
                profile =>
                    new
                    {
                        profile.TenantId,
                        profile.Id
                    })
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_merchant_verification_documents_profiles");
    }
}