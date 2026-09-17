using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Returns;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class ReturnRequestItemConfiguration : IEntityTypeConfiguration<ReturnRequestItem>
{
    public void Configure(EntityTypeBuilder<ReturnRequestItem> b)
    {
        b.ToTable("commerce_return_request_items",t=>t.HasCheckConstraint("ck_return_items_quantity","quantity > 0 AND restocked_quantity >= 0 AND restocked_quantity <= quantity"));
        b.HasKey(x=>x.Id);
        b.Property(x=>x.Id).HasColumnName("id").HasConversion(x=>x.Value,x=>ReturnRequestItemId.From(x)).ValueGeneratedNever();
        b.Property(x=>x.TenantId).HasColumnName("tenant_id").HasConversion(x=>x.Value,x=>TenantId.From(x)).IsRequired();
        b.Property(x=>x.ReturnRequestId).HasColumnName("return_request_id").HasConversion(x=>x.Value,x=>ReturnRequestId.From(x)).IsRequired();
        b.Property(x=>x.OrderItemId).HasColumnName("order_item_id").HasConversion(x=>x.Value,x=>OrderItemId.From(x)).IsRequired();
        b.Property(x=>x.ProductId).HasColumnName("product_id").HasConversion(x=>x.Value,x=>ProductId.From(x)).IsRequired();
        b.Property(x=>x.ProductVariantId).HasColumnName("product_variant_id").HasConversion(x=>x.Value,x=>ProductVariantId.From(x)).IsRequired();
        b.Property(x=>x.Quantity).HasColumnName("quantity").IsRequired(); b.Property(x=>x.RestockedQuantity).HasColumnName("restocked_quantity").HasDefaultValue(0).IsRequired(); b.Property(x=>x.RestockedAtUtc).HasColumnName("restocked_at_utc");
        b.HasIndex(x=>new{x.TenantId,x.ReturnRequestId,x.OrderItemId}).IsUnique().HasDatabaseName("ux_return_items_request_order_item");
        b.HasIndex(x=>new{x.TenantId,x.OrderItemId}).HasDatabaseName("ix_return_items_order_item");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
