using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductImageConfiguration :
    IEntityTypeConfiguration<ProductImage>
{
    public void Configure(
        EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable(
            "catalog_product_images");

        builder.HasKey(
            image =>
                image.Id);

        builder.Property(
                image =>
                    image.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductImageId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                image =>
                    image.TenantId)
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
                image =>
                    image.ProductId)
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
                image =>
                    image.Url)
            .HasColumnName(
                "url")
            .HasMaxLength(
                2048)
            .IsRequired();

        builder.Property(
                image =>
                    image.AltText)
            .HasColumnName(
                "alt_text")
            .HasMaxLength(
                300);

        builder.Property(
                image =>
                    image.SortOrder)
            .HasColumnName(
                "sort_order")
            .IsRequired();

        builder.Property(
                image =>
                    image.IsPrimary)
            .HasColumnName(
                "is_primary")
            .IsRequired();

        builder.Property(
                image =>
                    image.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                image =>
                    image.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.HasIndex(
                image =>
                    new
                    {
                        image.TenantId,
                        image.ProductId,
                        image.SortOrder
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_product_images_tenant_product_sort_order");

        builder.HasIndex(
                image =>
                    new
                    {
                        image.TenantId,
                        image.ProductId,
                        image.Url
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_product_images_tenant_product_url");

        builder.HasIndex(
                image =>
                    new
                    {
                        image.TenantId,
                        image.ProductId,
                        image.IsPrimary
                    })
            .IsUnique()
            .HasFilter(
                "\"is_primary\" = TRUE")
            .HasDatabaseName(
                "ux_catalog_product_images_primary");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                image =>
                    image.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                image =>
                    new
                    {
                        image.TenantId,
                        image.ProductId
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