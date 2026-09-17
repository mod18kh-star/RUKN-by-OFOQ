using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Commerce.Returns;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Tests.Commerce.Returns;

public sealed class ReturnRequestTests
{
    [Fact]
    public void Approve_receive_complete_follows_expected_lifecycle()
    {
        var now = DateTimeOffset.UtcNow;
        var request = ReturnRequest.Create(
            TenantId.From(Guid.NewGuid()),
            OrderId.From(Guid.NewGuid()),
            UserId.From(Guid.NewGuid()),
            "Wrong size",
            new[]
            {
                (OrderItemId.From(Guid.NewGuid()), ProductId.From(Guid.NewGuid()), ProductVariantId.From(Guid.NewGuid()), 1)
            },
            now);

        request.Approve("Approved", now.AddMinutes(1), Guid.NewGuid());
        request.MarkReceived(now.AddMinutes(2), Guid.NewGuid());
        request.Complete("Closed", now.AddMinutes(3), Guid.NewGuid());

        Assert.Equal(ReturnRequestStatus.Completed, request.Status);
        Assert.Single(request.Items);
        Assert.Equal(1, request.Items.Single().RestockedQuantity);
        Assert.NotNull(request.CompletedAtUtc);
    }

    [Fact]
    public void Reject_prevents_receiving()
    {
        var now = DateTimeOffset.UtcNow;
        var request = ReturnRequest.Create(
            TenantId.From(Guid.NewGuid()),
            OrderId.From(Guid.NewGuid()),
            UserId.From(Guid.NewGuid()),
            "Changed mind",
            new[]
            {
                (OrderItemId.From(Guid.NewGuid()), ProductId.From(Guid.NewGuid()), ProductVariantId.From(Guid.NewGuid()), 1)
            },
            now);

        request.Reject("Not eligible", now.AddMinutes(1), Guid.NewGuid());

        Assert.Equal(ReturnRequestStatus.Rejected, request.Status);
        Assert.Throws<InvalidOperationException>(() => request.MarkReceived(now.AddMinutes(2), Guid.NewGuid()));
    }
}
