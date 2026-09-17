using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Platform;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Platform;

public sealed class PlatformRequestTests
{
    [Fact]
    public void Create_StartsPending()
    {
        var request = PlatformRequest.Create(
            TenantId.New(),
            UserId.New(),
            PlatformRequestType.PlanChange,
            "طلب تغيير الباقة",
            "{\"planCode\":\"pro\"}",
            DateTimeOffset.UtcNow);

        Assert.Equal(PlatformRequestStatus.Pending, request.Status);
        Assert.Null(request.ReviewedAtUtc);
        Assert.Null(request.ReviewedByUserId);
    }

    [Fact]
    public void RequestMoreInfo_ThenRevise_ReturnsToPending()
    {
        var now = DateTimeOffset.UtcNow;
        var reviewer = UserId.New();
        var request = PlatformRequest.Create(
            TenantId.New(),
            UserId.New(),
            PlatformRequestType.StoreIdentityChange,
            "طلب تغيير بيانات المتجر",
            "{\"name\":\"ركن\"}",
            now);

        request.RequestMoreInfo(
            reviewer,
            "أرفق سبب التغيير",
            now.AddMinutes(1));

        request.Revise(
            "طلب تغيير بيانات المتجر",
            "{\"name\":\"ركن الجديد\"}",
            now.AddMinutes(2));

        Assert.Equal(PlatformRequestStatus.Pending, request.Status);
        Assert.Null(request.ReviewReason);
        Assert.Null(request.ReviewedByUserId);
        Assert.Null(request.ReviewedAtUtc);
    }

    [Fact]
    public void ApprovedRequest_CannotBeReviewedAgain()
    {
        var now = DateTimeOffset.UtcNow;
        var request = PlatformRequest.Create(
            TenantId.New(),
            UserId.New(),
            PlatformRequestType.OwnerEmailChange,
            "طلب تغيير بريد المالك",
            "{\"email\":\"owner@example.com\"}",
            now);

        request.Approve(
            UserId.New(),
            "تم التحقق",
            now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(
            () => request.Reject(
                UserId.New(),
                "رفض لاحق",
                now.AddMinutes(2)));
    }
}
