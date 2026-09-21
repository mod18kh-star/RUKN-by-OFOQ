using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OFOQ.Market.Domain.Commerce.Customers;
using OFOQ.Market.Domain.Identity;

namespace OFOQ.Market.Infrastructure.Persistence.Configurations.Commerce;

public sealed class CustomerSavedAddressConfiguration
    : IEntityTypeConfiguration<CustomerSavedAddress>
{
    public void Configure(
        EntityTypeBuilder<CustomerSavedAddress> b)
    {
        b.ToTable("commerce_customer_saved_addresses");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        b.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasConversion(
                x => x.Value,
                x => UserId.From(x))
            .IsRequired();

        b.Property(x => x.Label)
            .HasColumnName("label")
            .HasMaxLength(80)
            .IsRequired();

        b.Property(x => x.RecipientName)
            .HasColumnName("recipient_name")
            .HasMaxLength(160)
            .IsRequired();

        b.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(40)
            .IsRequired();

        b.Property(x => x.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(2)
            .IsRequired();

        b.Property(x => x.Region)
            .HasColumnName("region")
            .HasMaxLength(120);

        b.Property(x => x.City)
            .HasColumnName("city")
            .HasMaxLength(120)
            .IsRequired();

        b.Property(x => x.PostalCode)
            .HasColumnName("postal_code")
            .HasMaxLength(32);

        b.Property(x => x.Line1)
            .HasColumnName("line1")
            .HasMaxLength(240)
            .IsRequired();

        b.Property(x => x.Line2)
            .HasColumnName("line2")
            .HasMaxLength(240);

        b.Property(x => x.Latitude)
            .HasColumnName("latitude")
            .HasPrecision(9, 6);

        b.Property(x => x.Longitude)
            .HasColumnName("longitude")
            .HasPrecision(9, 6);

        b.Property(x => x.MapUrl)
            .HasColumnName("map_url")
            .HasMaxLength(2048);

        b.Property(x => x.DeliveryNotes)
            .HasColumnName("delivery_notes")
            .HasMaxLength(600);

        b.Property(x => x.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();

        b.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        b.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        b.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        b.HasIndex(x => new
        {
            x.UserId,
            x.IsActive
        });

        b.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("is_default = TRUE AND is_active = TRUE");

        b.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}