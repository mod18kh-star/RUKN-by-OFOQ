using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce.Verification;

internal sealed class MerchantVerificationDocumentFileConfiguration :
    IEntityTypeConfiguration<MerchantVerificationDocumentFile>
{
    public void Configure(
        EntityTypeBuilder<MerchantVerificationDocumentFile> builder)
    {
        builder.ToTable(
            "commerce_merchant_verification_document_files",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_document_files_side",
                    "side > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_document_files_storage_key",
                    "char_length(btrim(storage_key)) > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_document_files_file_name",
                    "char_length(btrim(original_file_name)) > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_document_files_content_type",
                    "char_length(btrim(content_type)) > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_document_files_size",
                    $"file_size_bytes > 0 AND file_size_bytes <= {MerchantVerificationDocumentFile.MaxFileSizeBytes}");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_document_files_sha256",
                    $"char_length(sha256) = {MerchantVerificationDocumentFile.MaxSha256Length}");
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
                    MerchantVerificationDocumentFileId.From(
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
                    entity.DocumentId)
            .HasColumnName(
                "document_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    MerchantVerificationDocumentId.From(
                        value))
            .IsRequired();

        builder.Property(
                entity =>
                    entity.Side)
            .HasColumnName(
                "side")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                entity =>
                    entity.StorageKey)
            .HasColumnName(
                "storage_key")
            .HasMaxLength(
                MerchantVerificationDocumentFile.MaxStorageKeyLength)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.OriginalFileName)
            .HasColumnName(
                "original_file_name")
            .HasMaxLength(
                MerchantVerificationDocumentFile.MaxOriginalFileNameLength)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.ContentType)
            .HasColumnName(
                "content_type")
            .HasMaxLength(
                MerchantVerificationDocumentFile.MaxContentTypeLength)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.FileSizeBytes)
            .HasColumnName(
                "file_size_bytes")
            .IsRequired();

        builder.Property(
                entity =>
                    entity.Sha256)
            .HasColumnName(
                "sha256")
            .HasMaxLength(
                MerchantVerificationDocumentFile.MaxSha256Length)
            .IsFixedLength()
            .IsRequired();

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

        // Storage keys identify private objects and must
        // never point two database rows to the same object.
        builder.HasIndex(
                entity =>
                    entity.StorageKey)
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_merchant_verification_document_files_storage_key");

        // Prevent the exact same uploaded binary from being
        // linked repeatedly to the same verification document.
        builder.HasIndex(
                entity =>
                    new
                    {
                        entity.TenantId,
                        entity.DocumentId,
                        entity.Sha256
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_merchant_verification_document_files_document_sha256");

        builder.HasIndex(
                entity =>
                    new
                    {
                        entity.TenantId,
                        entity.DocumentId,
                        entity.Side
                    })
            .HasDatabaseName(
                "ix_commerce_merchant_verification_document_files_document_side");

        builder.HasIndex(
                entity =>
                    entity.Sha256)
            .HasDatabaseName(
                "ix_commerce_merchant_verification_document_files_sha256");

        // Tenant-aware relationship:
        // a file from Tenant A cannot reference a
        // verification document belonging to Tenant B.
        builder.HasOne<MerchantVerificationDocument>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    new
                    {
                        entity.TenantId,
                        entity.DocumentId
                    })
            .HasPrincipalKey(
                document =>
                    new
                    {
                        document.TenantId,
                        document.Id
                    })
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_merchant_verification_document_files_documents");
    }
}
