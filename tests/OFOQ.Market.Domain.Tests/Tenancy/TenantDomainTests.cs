using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Tenancy;

public sealed class TenantDomainTests
{
    [Fact]
    public void DomainName_Create_NormalizesDomainToLowercase()
    {
        var domain = DomainName.Create(
            "WWW.Turks.COM");

        Assert.Equal(
            "www.turks.com",
            domain.Value);
    }

    [Fact]
    public void DomainName_Create_WithHttpScheme_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            DomainName.Create(
                "https://turks.com"));
    }

    [Fact]
    public void DomainName_Create_WithPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            DomainName.Create(
                "turks.com/products"));
    }

    [Fact]
    public void DomainName_Create_WithSingleLabel_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            DomainName.Create(
                "turks"));
    }

    [Fact]
    public void Create_CreatesPendingNonPrimaryDomain()
    {
        var tenantId = TenantId.New();

        var now = DateTimeOffset.UtcNow;

        var domain = TenantDomain.Create(
            tenantId,
            "turks.com",
            now);

        Assert.False(domain.Id.IsEmpty);

        Assert.Equal(
            tenantId.Value,
            domain.TenantId);

        Assert.Equal(
            "turks.com",
            domain.Domain.Value);

        Assert.Equal(
            TenantDomainStatus.PendingVerification,
            domain.Status);

        Assert.False(domain.IsPrimary);
        Assert.Null(domain.VerifiedAtUtc);
        Assert.False(domain.IsDeleted);
    }

    [Fact]
    public void MakePrimary_BeforeVerification_ThrowsInvalidOperationException()
    {
        var domain = TenantDomain.Create(
            TenantId.New(),
            "turks.com",
            DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            domain.MakePrimary(
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkVerified_ChangesDomainStatusToVerified()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var verifiedAt = createdAt.AddMinutes(10);

        var domain = TenantDomain.Create(
            TenantId.New(),
            "turks.com",
            createdAt);

        domain.MarkVerified(
            verifiedAt);

        Assert.Equal(
            TenantDomainStatus.Verified,
            domain.Status);

        Assert.Equal(
            verifiedAt,
            domain.VerifiedAtUtc);
    }

    [Fact]
    public void MakePrimary_AfterVerification_SetsDomainAsPrimary()
    {
        var now = DateTimeOffset.UtcNow;

        var domain = TenantDomain.Create(
            TenantId.New(),
            "turks.com",
            now);

        domain.MarkVerified(
            now.AddMinutes(5));

        domain.MakePrimary(
            now.AddMinutes(10));

        Assert.True(domain.IsPrimary);
    }

    [Fact]
    public void Disable_RemovesPrimaryStatus()
    {
        var now = DateTimeOffset.UtcNow;

        var domain = TenantDomain.Create(
            TenantId.New(),
            "turks.com",
            now);

        domain.MarkVerified(
            now.AddMinutes(5));

        domain.MakePrimary(
            now.AddMinutes(10));

        domain.Disable(
            now.AddMinutes(15));

        Assert.Equal(
            TenantDomainStatus.Disabled,
            domain.Status);

        Assert.False(domain.IsPrimary);
    }

    [Fact]
    public void Delete_SoftDeletesDomainAndRemovesPrimaryStatus()
    {
        var now = DateTimeOffset.UtcNow;

        var domain = TenantDomain.Create(
            TenantId.New(),
            "turks.com",
            now);

        domain.MarkVerified(
            now.AddMinutes(5));

        domain.MakePrimary(
            now.AddMinutes(10));

        domain.Delete(
            now.AddMinutes(15));

        Assert.True(domain.IsDeleted);
        Assert.False(domain.IsPrimary);
        Assert.NotNull(domain.DeletedAtUtc);
    }
}