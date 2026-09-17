using System.Text;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Returns;

public sealed class ReturnRequest : Entity<ReturnRequestId>, ITenantDataScoped, IAuditable
{
    public const int MaximumReasonLength = 1000;
    public const int MaximumMerchantNoteLength = 2000;
    private readonly List<ReturnRequestItem> _items = [];
    private ReturnRequest() { }

    private ReturnRequest(ReturnRequestId id, TenantId tenantId, OrderId orderId, UserId customerUserId, string reason, DateTimeOffset createdAtUtc, Guid? createdByUserId) : base(id)
    {
        if (tenantId.IsEmpty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (orderId.IsEmpty) throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
        if (customerUserId.IsEmpty) throw new ArgumentException("Customer user ID cannot be empty.", nameof(customerUserId));
        TenantId = tenantId; OrderId = orderId; CustomerUserId = customerUserId; Reason = NormalizeRequired(reason); Status = ReturnRequestStatus.Requested; CreatedAtUtc = createdAtUtc; CreatedByUserId = createdByUserId;
    }

    public TenantId TenantId { get; private set; }
    public OrderId OrderId { get; private set; }
    public UserId CustomerUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public ReturnRequestStatus Status { get; private set; }
    public string? MerchantNote { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }
    public DateTimeOffset? ReceivedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public IReadOnlyCollection<ReturnRequestItem> Items => _items.AsReadOnly();
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static ReturnRequest Create(TenantId tenantId, OrderId orderId, UserId customerUserId, string reason, IReadOnlyCollection<(OrderItemId OrderItemId, ProductId ProductId, ProductVariantId ProductVariantId, int Quantity)> items, DateTimeOffset createdAtUtc, Guid? createdByUserId = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0) throw new ArgumentException("A return request must contain at least one item.", nameof(items));
        var request = new ReturnRequest(ReturnRequestId.New(), tenantId, orderId, customerUserId, reason, createdAtUtc, createdByUserId);
        var seen = new HashSet<OrderItemId>();
        foreach (var item in items)
        {
            if (!seen.Add(item.OrderItemId)) throw new ArgumentException("A return request cannot contain the same order item more than once.", nameof(items));
            request._items.Add(ReturnRequestItem.Create(tenantId, request.Id, item.OrderItemId, item.ProductId, item.ProductVariantId, item.Quantity));
        }
        return request;
    }

    public void Approve(string? note, DateTimeOffset at, Guid actor) { EnsureStatus(ReturnRequestStatus.Requested); MerchantNote = NormalizeOptional(note); Status = ReturnRequestStatus.Approved; ApprovedAtUtc = at; MarkUpdated(at, actor); }
    public void Reject(string? note, DateTimeOffset at, Guid actor) { EnsureStatus(ReturnRequestStatus.Requested); MerchantNote = NormalizeOptional(note); Status = ReturnRequestStatus.Rejected; RejectedAtUtc = at; MarkUpdated(at, actor); }
    public void Cancel(DateTimeOffset at, Guid actor) { if (Status == ReturnRequestStatus.Cancelled) return; EnsureStatus(ReturnRequestStatus.Requested); Status = ReturnRequestStatus.Cancelled; CancelledAtUtc = at; MarkUpdated(at, actor); }
    public void MarkReceived(DateTimeOffset at, Guid actor) { if (Status is ReturnRequestStatus.Received or ReturnRequestStatus.Completed) return; EnsureStatus(ReturnRequestStatus.Approved); foreach (var item in _items) item.MarkRestocked(at); Status = ReturnRequestStatus.Received; ReceivedAtUtc = at; MarkUpdated(at, actor); }
    public void Complete(string? note, DateTimeOffset at, Guid actor) { if (Status == ReturnRequestStatus.Completed) return; EnsureStatus(ReturnRequestStatus.Received); MerchantNote = NormalizeOptional(note) ?? MerchantNote; Status = ReturnRequestStatus.Completed; CompletedAtUtc = at; MarkUpdated(at, actor); }

    private void EnsureStatus(ReturnRequestStatus expected) { if (Status != expected) throw new InvalidOperationException($"Return request must be {expected} for this operation."); }
    private void MarkUpdated(DateTimeOffset at, Guid actor) { UpdatedAtUtc = at; UpdatedByUserId = actor; }
    private static string NormalizeRequired(string value) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Return reason is required."); var n=value.Trim().Normalize(NormalizationForm.FormKC); if(n.Length>MaximumReasonLength) throw new ArgumentException($"Return reason cannot exceed {MaximumReasonLength} characters."); return n; }
    private static string? NormalizeOptional(string? value) { if(string.IsNullOrWhiteSpace(value)) return null; var n=value.Trim().Normalize(NormalizationForm.FormKC); if(n.Length>MaximumMerchantNoteLength) throw new ArgumentException($"Merchant note cannot exceed {MaximumMerchantNoteLength} characters."); return n; }
}
