using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Returns;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> b)
    {
        b.ToTable("commerce_return_requests");
        b.HasKey(x=>x.Id);
        b.HasAlternateKey(x=>new{x.TenantId,x.Id}).HasName("ak_commerce_return_requests_tenant_id_id");
        b.Property(x=>x.Id).HasColumnName("id").HasConversion(x=>x.Value,x=>ReturnRequestId.From(x)).ValueGeneratedNever();
        b.Property(x=>x.TenantId).HasColumnName("tenant_id").HasConversion(x=>x.Value,x=>TenantId.From(x)).IsRequired();
        b.Property(x=>x.OrderId).HasColumnName("order_id").HasConversion(x=>x.Value,x=>OrderId.From(x)).IsRequired();
        b.Property(x=>x.CustomerUserId).HasColumnName("customer_user_id").HasConversion(x=>x.Value,x=>UserId.From(x)).IsRequired();
        b.Property(x=>x.Reason).HasColumnName("reason").HasMaxLength(ReturnRequest.MaximumReasonLength).IsRequired();
        b.Property(x=>x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        b.Property(x=>x.MerchantNote).HasColumnName("merchant_note").HasMaxLength(ReturnRequest.MaximumMerchantNoteLength);
        b.Property(x=>x.ApprovedAtUtc).HasColumnName("approved_at_utc"); b.Property(x=>x.RejectedAtUtc).HasColumnName("rejected_at_utc"); b.Property(x=>x.ReceivedAtUtc).HasColumnName("received_at_utc"); b.Property(x=>x.CompletedAtUtc).HasColumnName("completed_at_utc"); b.Property(x=>x.CancelledAtUtc).HasColumnName("cancelled_at_utc");
        b.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired(); b.Property(x=>x.CreatedByUserId).HasColumnName("created_by_user_id"); b.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc"); b.Property(x=>x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        b.Ignore(x=>x.Items);
        b.HasMany<ReturnRequestItem>("_items").WithOne().HasForeignKey(x=>new{x.TenantId,x.ReturnRequestId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x=>new{x.TenantId,x.OrderId}).HasDatabaseName("ix_return_requests_tenant_order");
        b.HasIndex(x=>new{x.TenantId,x.CustomerUserId,x.CreatedAtUtc}).HasDatabaseName("ix_return_requests_tenant_customer_created");
        b.HasOne<Tenant>().WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Order>().WithMany().HasForeignKey(x=>new{x.TenantId,x.OrderId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
    }
}
