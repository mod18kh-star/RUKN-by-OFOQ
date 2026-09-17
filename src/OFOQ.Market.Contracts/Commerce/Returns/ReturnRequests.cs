namespace OFOQ.Market.Contracts.Commerce.Returns;

public sealed record CreateReturnRequest(Guid OrderId, string Reason, IReadOnlyList<CreateReturnItemRequest> Items);
public sealed record CreateReturnItemRequest(Guid OrderItemId, int Quantity);
public sealed record ReturnDecisionRequest(string? Note);
public sealed record ReturnItemResponse(Guid Id, Guid OrderItemId, Guid ProductId, Guid ProductVariantId, int Quantity, int RestockedQuantity, DateTimeOffset? RestockedAtUtc);
public sealed record ReturnRequestResponse(Guid Id, Guid OrderId, Guid CustomerUserId, string Reason, string Status, string? MerchantNote, DateTimeOffset CreatedAtUtc, DateTimeOffset? ApprovedAtUtc, DateTimeOffset? RejectedAtUtc, DateTimeOffset? ReceivedAtUtc, DateTimeOffset? CompletedAtUtc, DateTimeOffset? CancelledAtUtc, IReadOnlyList<ReturnItemResponse> Items);
