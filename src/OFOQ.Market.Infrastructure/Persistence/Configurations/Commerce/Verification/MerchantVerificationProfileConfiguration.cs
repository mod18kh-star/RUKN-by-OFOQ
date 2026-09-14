using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Verification;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce.Verification;

internal sealed class MerchantVerificationProfileConfiguration :
    IEntityTypeConfiguration<MerchantVerificationProfile>
{
    public void Configure(
        EntityTypeBuilder<MerchantVerificationProfile> builder)
    {
        builder.ToTable(
            "commerce_merchant_verification_profiles",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_profiles_subject_type",
                    "subject_type > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_profiles_status",
                    "status > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_profiles_country_code",
                    "char_length(country_code) = 2");

                tableBuilder.HasCheckConstraint(
                    "ck_commerce_merchant_verification_profiles_legal_name",
                    "char_length(btrim(legal_name)) > 0");
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
                    MerchantVerificationProfileId.From(
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
                    entity.PrincipalUserId)
            .HasColumnName(
                "principal_user_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    UserId.From(
                        value))
            .IsRequired();

        builder.Property(
                entity =>
                    entity.SubjectType)
            .HasColumnName(
                "subject_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                entity =>
                    entity.CountryCode)
            .HasColumnName(
                "country_code")
            .HasMaxLength(
                2)
            .IsFixedLength()
            .IsRequired();

        builder.Property(
                entity =>
                    entity.LegalName)
            .HasColumnName(
                "legal_name")
            .HasMaxLength(
                MerchantVerificationProfile.MaxLegalNameLength)
            .IsRequired();

        builder.Property(
                entity =>
                    entity.Status)
            .HasColumnName(
                "status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                entity =>
                    entity.SubmittedAtUtc)
            .HasColumnName(
                "submitted_at_utc");

        builder.Property(
                entity =>
                    entity.ReviewStartedAtUtc)
            .HasColumnName(
                "review_started_at_utc");

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
                    entity.VerifiedAtUtc)
            .HasColumnName(
                "verified_at_utc");

        builder.Property(
                entity =>
                    entity.ExpiredAtUtc)
            .HasColumnName(
                "expired_at_utc");

        builder.Property(
                entity =>
                    entity.ReviewNote)
            .HasColumnName(
                "review_note")
            .HasMaxLength(
                MerchantVerificationProfile.MaxReviewNoteLength);

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

        /*
         * PostgreSQL xmin is maintained automatically whenever
         * the row is updated. Npgsql maps a uint row-version
         * property to xmin and EF Core uses it for optimistic
         * concurrency checks.
         *
         * A shadow property keeps this persistence concern out
         * of the domain aggregate.
         */
        builder.Property<uint>(
                "Version")
            .IsRowVersion();

        // One verification profile represents the legal
        // identity/business identity of one tenant.
        builder.HasIndex(
                entity =>
                    entity.TenantId)
            .IsUnique()
            .HasDatabaseName(
                "ux_commerce_merchant_verification_profiles_tenant");

        builder.HasIndex(
                entity =>
                    entity.PrincipalUserId)
            .HasDatabaseName(
                "ix_commerce_merchant_verification_profiles_principal_user");

        builder.HasIndex(
                entity =>
                    entity.Status)
            .HasDatabaseName(
                "ix_commerce_merchant_verification_profiles_status");

        // Required so child verification records can use
        // composite tenant-aware foreign keys.
        builder.HasAlternateKey(
                entity =>
                    new
                    {
                        entity.TenantId,
                        entity.Id
                    })
            .HasName(
                "ak_commerce_merchant_verification_profiles_tenant_id");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    entity.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_merchant_verification_profiles_tenants");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(
                entity =>
                    entity.PrincipalUserId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_merchant_verification_profiles_users");
    }
}