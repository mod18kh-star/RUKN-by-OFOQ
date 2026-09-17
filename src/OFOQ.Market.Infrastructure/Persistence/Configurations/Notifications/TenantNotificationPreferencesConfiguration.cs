using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Notifications;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Notifications;

public sealed class TenantNotificationPreferencesConfiguration :
    IEntityTypeConfiguration<TenantNotificationPreferences>
{
    public void Configure(
        EntityTypeBuilder<TenantNotificationPreferences> builder)
    {
        builder.ToTable(
            "tenant_notification_preferences");

        builder.HasKey(
            item =>
                item.Id);

        builder.Property(
                item =>
                    item.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantNotificationPreferencesId.From(value))
            .ValueGeneratedNever();

        builder.Property(
                item =>
                    item.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(
                id => id.Value,
                value =>
                    TenantId.From(value))
            .IsRequired();

        builder.Property(
                item =>
                    item.NewOrderEmailEnabled)
            .HasColumnName(
                "new_order_email_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(
                item =>
                    item.LowStockEmailEnabled)
            .HasColumnName(
                "low_stock_email_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(
                item =>
                    item.ReviewEmailEnabled)
            .HasColumnName(
                "review_email_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(
                item =>
                    item.PlatformRequestEmailEnabled)
            .HasColumnName(
                "platform_request_email_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(
                item =>
                    item.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(
                item =>
                    item.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(
                item =>
                    item.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(
                item =>
                    item.UpdatedByUserId)
            .HasColumnName("updated_by_user_id");

        builder.HasIndex(
                item =>
                    item.TenantId)
            .IsUnique()
            .HasDatabaseName(
                "ux_tenant_notification_preferences_tenant");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(
                item =>
                    item.TenantId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
