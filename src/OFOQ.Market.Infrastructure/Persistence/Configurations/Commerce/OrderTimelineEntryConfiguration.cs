using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class OrderTimelineEntryConfiguration :
    IEntityTypeConfiguration<OrderTimelineEntry>
{
    public void Configure(
        EntityTypeBuilder<OrderTimelineEntry> builder)
    {
        builder.ToTable(
            "commerce_order_timeline");

        builder.HasKey(
            entry =>
                entry.Id);

        builder.Property(
                entry =>
                    entry.Id)
            .HasColumnName(
                "id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    OrderTimelineEntryId.From(
                        value))
            .ValueGeneratedNever();

        builder.Property(
                entry =>
                    entry.TenantId)
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
                entry =>
                    entry.OrderId)
            .HasColumnName(
                "order_id")
            .HasConversion(
                id =>
                    id.Value,
                value =>
                    OrderId.From(
                        value))
            .IsRequired();

        builder.Property(
                entry =>
                    entry.Type)
            .HasColumnName(
                "type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                entry =>
                    entry.OrderStatus)
            .HasColumnName(
                "order_status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                entry =>
                    entry.FulfillmentStatus)
            .HasColumnName(
                "fulfillment_status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(
                entry =>
                    entry.Note)
            .HasColumnName(
                "note")
            .HasMaxLength(
                500);

        builder.Property(
                entry =>
                    entry.CreatedAtUtc)
            .HasColumnName(
                "created_at_utc")
            .IsRequired();

        builder.Property(
                entry =>
                    entry.CreatedByUserId)
            .HasColumnName(
                "created_by_user_id");

        builder.HasIndex(
                entry =>
                    new
                    {
                        entry.TenantId,
                        entry.OrderId,
                        entry.CreatedAtUtc
                    })
            .HasDatabaseName(
                "ix_commerce_order_timeline_order_created");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                entry =>
                    entry.TenantId)
            .OnDelete(
                DeleteBehavior.Restrict)
            .HasConstraintName(
                "fk_commerce_order_timeline_tenants");
    }
}