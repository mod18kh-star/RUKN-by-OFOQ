using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Tenancy;

public sealed class TenantTests
{
    [Fact]
    public void Create_WithValidData_CreatesDraftTenantAndRaisesDomainEvent()
    {
        var now = new DateTimeOffset(
            2026, 8, 31,
            12, 0, 0,
            TimeSpan.Zero);

        var userId = Guid.NewGuid();

        var tenant = Tenant.Create(
            "متجر أفق",
            "ofoq-store",
            now,
            userId);

        Assert.False(tenant.Id.IsEmpty);
        Assert.Equal("متجر أفق", tenant.Name);
        Assert.Equal("ofoq-store", tenant.Slug.Value);
        Assert.Equal(TenantStatus.Draft, tenant.Status);

        Assert.Equal(now, tenant.CreatedAtUtc);
        Assert.Equal(userId, tenant.CreatedByUserId);

        Assert.False(tenant.IsDeleted);

        var domainEvent = Assert.Single(tenant.DomainEvents);

        var createdEvent =
            Assert.IsType<TenantCreatedDomainEvent>(domainEvent);

        Assert.Equal(tenant.Id, createdEvent.TenantId);
        Assert.Equal(now, createdEvent.OccurredAtUtc);
    }

    [Fact]
    public void Rename_WithValidName_UpdatesNameAndAuditInformation()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(10);
        var userId = Guid.NewGuid();

        var tenant = Tenant.Create(
            "Old Name",
            "old-name",
            createdAt);

        tenant.Rename(
            "New Name",
            updatedAt,
            userId);

        Assert.Equal("New Name", tenant.Name);
        Assert.Equal(updatedAt, tenant.UpdatedAtUtc);
        Assert.Equal(userId, tenant.UpdatedByUserId);
    }

    [Fact]
    public void Activate_ChangesStatusToActive()
    {
        var now = DateTimeOffset.UtcNow;

        var tenant = Tenant.Create(
            "OFOQ Store",
            "ofoq-store",
            now);

        tenant.Activate(now.AddMinutes(1));

        Assert.Equal(
            TenantStatus.Active,
            tenant.Status);
    }

    [Fact]
    public void Delete_And_Restore_PreserveTenantInsteadOfHardDeletingIt()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        var tenant = Tenant.Create(
            "OFOQ Store",
            "ofoq-store",
            now);

        tenant.Delete(
            now.AddHours(1),
            userId);

        Assert.True(tenant.IsDeleted);
        Assert.NotNull(tenant.DeletedAtUtc);
        Assert.Equal(
            userId,
            tenant.DeletedByUserId);

        tenant.Restore(
            now.AddHours(2),
            userId);

        Assert.False(tenant.IsDeleted);
        Assert.Null(tenant.DeletedAtUtc);
        Assert.Null(tenant.DeletedByUserId);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Tenant.Create(
                "",
                "valid-slug",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void TenantId_FromEmptyGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            TenantId.From(Guid.Empty));
    }
}