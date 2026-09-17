using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductContentBlockConfiguration :
    IEntityTypeConfiguration<ProductContentBlock>
{
    public void Configure(
        EntityTypeBuilder<ProductContentBlock> builder)
    {
        builder.ToTable(
            "catalog_product_content_blocks");

        builder.HasKey(
            block =>
                block.Id);

        builder.Property(
                block =>
                    block.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductContentBlockId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                block =>
                    block.TenantId)
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
                block =>
                    block.ProductId)
            .HasColumnName(
                "product_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductId.From(
                        value))
            .IsRequired();

        builder.Property(
                block =>
                    block.Type)
            .HasColumnName(
                "type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                block =>
                    block.Title)
            .HasColumnName(
                "title")
            .HasMaxLength(
                160);

        builder.Property(
                block =>
                    block.Body)
            .HasColumnName(
                "body")
            .HasMaxLength(
                12000);

        builder.Property(
                block =>
                    block.MediaUrl)
            .HasColumnName(
                "media_url")
            .HasMaxLength(
                2048);

        builder.Property(
                block =>
                    block.SortOrder)
            .HasColumnName(
                "sort_order")
            .IsRequired();

        builder.Property(
                block =>
                    block.IsVisible)
            .HasColumnName(
                "is_visible")
            .IsRequired();

        builder.Property(
                block =>
                    block.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                block =>
                    block.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                block =>
                    block.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                block =>
                    block.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.HasIndex(
                block =>
                    new
                    {
                        block.TenantId,
                        block.ProductId,
                        block.SortOrder
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_product_content_blocks_order");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                block =>
                    block.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                block =>
                    new
                    {
                        block.TenantId,
                        block.ProductId
                    })
            .HasPrincipalKey(
                product =>
                    new
                    {
                        product.TenantId,
                        product.Id
                    })
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
