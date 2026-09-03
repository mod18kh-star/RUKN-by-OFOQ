using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Tenancy;

public sealed class TenantMembershipTests
{
    [Fact]
    public void Create_CreatesOwnerMembership()
    {
        var tenantId =
            TenantId.New();

        var userId =
            UserId.New();

        var membership =
            TenantMembership.Create(
                tenantId,
                userId,
                TenantRole.Owner,
                DateTimeOffset.UtcNow);

        Assert.Equal(
            tenantId,
            membership.TenantId);

        Assert.Equal(
            userId,
            membership.UserId);

        Assert.Equal(
            TenantRole.Owner,
            membership.Role);
    }

    [Fact]
    public void Create_RejectsEmptyTenantId()
    {
        Assert.Throws<ArgumentException>(
            () =>
                TenantMembership.Create(
                    default,
                    UserId.New(),
                    TenantRole.Staff,
                    DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ChangeRole_ChangesMembershipRole()
    {
        var membership =
            TenantMembership.Create(
                TenantId.New(),
                UserId.New(),
                TenantRole.Staff,
                DateTimeOffset.UtcNow);

        membership.ChangeRole(
            TenantRole.Manager,
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(
            TenantRole.Manager,
            membership.Role);
    }

    [Fact]
    public void Delete_SoftDeletesMembership()
    {
        var membership =
            TenantMembership.Create(
                TenantId.New(),
                UserId.New(),
                TenantRole.Staff,
                DateTimeOffset.UtcNow);

        membership.Delete(
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.True(membership.IsDeleted);
        Assert.NotNull(membership.DeletedAtUtc);
    }
}