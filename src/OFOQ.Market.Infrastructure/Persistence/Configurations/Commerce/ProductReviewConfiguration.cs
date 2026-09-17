using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Reviews;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;
namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;
public sealed class ProductReviewConfiguration:IEntityTypeConfiguration<ProductReview>
{
 public void Configure(EntityTypeBuilder<ProductReview> b)
 {
  b.ToTable("commerce_product_reviews",t=>t.HasCheckConstraint("ck_product_reviews_rating","rating >= 1 AND rating <= 5"));
  b.HasKey(x=>x.Id);
  b.Property(x=>x.Id).HasColumnName("id").HasConversion(x=>x.Value,x=>ProductReviewId.From(x)).ValueGeneratedNever();
  b.Property(x=>x.TenantId).HasColumnName("tenant_id").HasConversion(x=>x.Value,x=>TenantId.From(x)).IsRequired();
  b.Property(x=>x.ProductId).HasColumnName("product_id").HasConversion(x=>x.Value,x=>ProductId.From(x)).IsRequired();
  b.Property(x=>x.CustomerUserId).HasColumnName("customer_user_id").HasConversion(x=>x.Value,x=>UserId.From(x)).IsRequired();
  b.Property(x=>x.OrderId).HasColumnName("order_id").HasConversion(x=>x.Value,x=>OrderId.From(x)).IsRequired();
  b.Property(x=>x.Rating).HasColumnName("rating").IsRequired(); b.Property(x=>x.Body).HasColumnName("body").HasMaxLength(ProductReview.MaxBodyLength); b.Property(x=>x.IsVerifiedPurchase).HasColumnName("is_verified_purchase").IsRequired(); b.Property(x=>x.Status).HasColumnName("status").HasConversion<int>().IsRequired(); b.Property(x=>x.MerchantReply).HasColumnName("merchant_reply").HasMaxLength(ProductReview.MaxReplyLength); b.Property(x=>x.PublishedAtUtc).HasColumnName("published_at_utc"); b.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired(); b.Property(x=>x.CreatedByUserId).HasColumnName("created_by_user_id"); b.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc"); b.Property(x=>x.UpdatedByUserId).HasColumnName("updated_by_user_id");
  b.HasIndex(x=>new{x.TenantId,x.CustomerUserId,x.ProductId}).IsUnique().HasDatabaseName("ux_reviews_customer_product"); b.HasIndex(x=>new{x.TenantId,x.ProductId,x.Status}).HasDatabaseName("ix_reviews_product_status");
  b.HasOne<Tenant>().WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Product>().WithMany().HasForeignKey(x=>new{x.TenantId,x.ProductId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict); b.HasOne<Order>().WithMany().HasForeignKey(x=>new{x.TenantId,x.OrderId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict); b.HasOne<User>().WithMany().HasForeignKey(x=>x.CustomerUserId).OnDelete(DeleteBehavior.Restrict);
 }
}
