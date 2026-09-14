using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Catalog.Attributes;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductAttributeValueConfiguration :
    IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(
        EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable(
            "catalog_product_attribute_values");

        builder.HasKey(
            value =>
                value.Id);

        builder.Property(
                value =>
                    value.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    ProductAttributeValueId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                value =>
                    value.TenantId)
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
                value =>
                    value.ProductId)
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
                value =>
                    value.Key)
            .HasColumnName(
                "attribute_key")
            .HasMaxLength(
                80)
            .IsRequired();

        builder.Property(
                value =>
                    value.Value)
            .HasColumnName(
                "attribute_value")
            .HasMaxLength(
                500)
            .IsRequired();

        builder.Property(
                value =>
                    value.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                value =>
                    value.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.Property(
                value =>
                    value.UpdatedAtUtc)
            .HasColumnName(
                "updated_at_utc");

        builder.Property(
                value =>
                    value.UpdatedByUserId)
            .HasColumnName(
                "updated_by_user_id");

        builder.HasIndex(
                value =>
                    new
                    {
                        value.TenantId,
                        value.ProductId,
                        value.Key
                    })
            .IsUnique()
            .HasDatabaseName(
                "ux_catalog_product_attribute_values_tenant_product_key");

        builder.HasIndex(
                value =>
                    new
                    {
                        value.TenantId,
                        value.ProductId
                    })
            .HasDatabaseName(
                "ix_catalog_product_attribute_values_tenant_product");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                value =>
                    value.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_catalog_product_attribute_values_tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(
                value =>
                    new
                    {
                        value.TenantId,
                        value.ProductId
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
                "fk_catalog_product_attribute_values_products");
    }
}