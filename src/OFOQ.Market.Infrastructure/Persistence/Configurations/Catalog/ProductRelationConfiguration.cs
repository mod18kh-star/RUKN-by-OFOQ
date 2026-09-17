using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductRelationConfiguration :
    IEntityTypeConfiguration<ProductRelation>
{
    public void Configure(
        EntityTypeBuilder<ProductRelation> builder)
    {
        builder.ToTable(
            "catalog_product_relations");

        builder.HasKey(
            relation =>
                relation.Id);

        builder.Property(
                relation =>
                    relation.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductRelationId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                relation =>
                    relation.TenantId)
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
                relation =>
                    relation.SourceProductId)
            .HasColumnName(
                "source_product_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductId.From(
                        value))
            .IsRequired();

        builder.Property(
                relation =>
                    relation.TargetProductId)
            .HasColumnName(
                "target_product_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductId.From(
                        value))
            .IsRequired();

        builder.Property(
                relation =>
                    relation.Type)
            .HasColumnName(
                "type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                relation =>
                    relation.SortOrder)
            .HasColumnName(
                "sort_order")
            .IsRequired();

        builder.Property(
                relation =>
                    relation.IsVisible)
            .HasColumnName(
                "is_visible")
            .IsRequired();

        builder.Property(
                relation =>
                    relation.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                relation =>
                    relation.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                relation =>
                    relation.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                relation =>
                    relation.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.HasIndex(
                relation =>
                    new
                    {
                        relation.TenantId,
                        relation.SourceProductId,
                        relation.Type,
                        relation.SortOrder
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_product_relations_order");

        builder.HasIndex(
                relation =>
                    new
                    {
                        relation.TenantId,
                        relation.SourceProductId,
                        relation.TargetProductId,
                        relation.Type
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_product_relations_target_type");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                relation =>
                    relation.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                relation =>
                    new
                    {
                        relation.TenantId,
                        relation.SourceProductId
                    })
            .HasPrincipalKey(
                product =>
                    new
                    {
                        product.TenantId,
                        product.Id
                    })
            .OnDelete(
                DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_catalog_product_relations_source");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                relation =>
                    new
                    {
                        relation.TenantId,
                        relation.TargetProductId
                    })
            .HasPrincipalKey(
                product =>
                    new
                    {
                        product.TenantId,
                        product.Id
                    })
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_product_relations_target");
    }
}
