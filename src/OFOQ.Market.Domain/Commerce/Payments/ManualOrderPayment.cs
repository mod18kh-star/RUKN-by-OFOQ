using OFOQ.Market.Domain.Commerce.Orders;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Payments;

// Merchant-owned manual transfer ledger; NOT a platform wallet or proof of receipt of funds.
public sealed class ManualOrderPayment : ITenantDataScoped
{
    private ManualOrderPayment() { }
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public OrderId OrderId { get; private set; }
    public Guid CustomerUserId { get; private set; }
    public Guid AccountId { get; private set; }
    public string CustomerPhone { get; private set; } = string.Empty;
    public string AccountName { get; private set; } = string.Empty;
    public string AccountKind { get; private set; } = string.Empty;
    public string ProtectedAccountSnapshot { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Status { get; private set; } = "AwaitingReceipt";
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ManualOrderPayment Create(TenantId tenantId, OrderId orderId, Guid customer,
        Guid paymentId, Guid accountId, string accountName, string accountKind, string protectedSnapshot,
        decimal amount, string currency, string customerPhone, DateTimeOffset now)
    {
        if (tenantId.IsEmpty || orderId.IsEmpty || customer == Guid.Empty || paymentId == Guid.Empty || accountId == Guid.Empty ||
            string.IsNullOrWhiteSpace(protectedSnapshot) || amount <= 0)
            throw new ArgumentException("Invalid manual payment order data.");
        return new ManualOrderPayment
        {
            Id = paymentId, TenantId = tenantId, OrderId = orderId,
            CustomerUserId = customer, AccountId = accountId, CustomerPhone = customerPhone, AccountName = accountName,
            AccountKind = accountKind, ProtectedAccountSnapshot = protectedSnapshot,
            Amount = amount, Currency = currency, CreatedAtUtc = now, UpdatedAtUtc = now
        };
    }
    public void Submit(DateTimeOffset now)
    {
        if (Status is not ("AwaitingReceipt" or "Rejected"))
            throw new InvalidOperationException("This payment is not awaiting a new receipt.");
        Status = "PendingReview";
        UpdatedAtUtc = now;
    }
    public void Reject(DateTimeOffset now)
    {
        if (Status != "PendingReview") throw new InvalidOperationException("Payment is not pending review.");
        Status = "Rejected"; UpdatedAtUtc = now;
    }
    public void Approve(DateTimeOffset now)
    {
        if (Status != "PendingReview") throw new InvalidOperationException("Payment is not pending review.");
        Status = "Approved"; UpdatedAtUtc = now;
    }
}

// Immutable receipt file per attempt; its review metadata is updated only on explicit review.
public sealed class ManualPaymentReceipt : ITenantDataScoped
{
    private ManualPaymentReceipt() { }
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid ManualPaymentId { get; private set; }
    public OrderId OrderId { get; private set; }
    public Guid SubmittedByUserId { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public byte[] Ciphertext { get; private set; } = [];
    public byte[] Nonce { get; private set; } = [];
    public byte[] AuthTag { get; private set; } = [];
    public string? TransferReference { get; private set; }
    public string ReviewStatus { get; private set; } = "PendingReview";
    public string? RejectionReason { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }

    public static ManualPaymentReceipt Create(TenantId tenantId, Guid manualPaymentId, OrderId orderId,
        Guid customer, string contentType, byte[] ciphertext, byte[] nonce, byte[] tag,
        string? transferReference, DateTimeOffset now, Guid receiptId)
    {
        if (tenantId.IsEmpty || manualPaymentId == Guid.Empty || customer == Guid.Empty || receiptId == Guid.Empty ||
            ciphertext.Length == 0 || nonce.Length != 12 || tag.Length != 16)
            throw new ArgumentException("Invalid receipt payload.");
        return new ManualPaymentReceipt
        {
            Id = receiptId, TenantId = tenantId, ManualPaymentId = manualPaymentId,
            OrderId = orderId, SubmittedByUserId = customer, ContentType = contentType,
            Ciphertext = ciphertext, Nonce = nonce, AuthTag = tag,
            TransferReference = transferReference, SubmittedAtUtc = now
        };
    }
    public void Review(bool approved, string? reason, Guid reviewer, DateTimeOffset now)
    {
        if (ReviewStatus != "PendingReview" || reviewer == Guid.Empty)
            throw new InvalidOperationException("Receipt has already been reviewed.");
        if (!approved && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection reason is required.");
        ReviewStatus = approved ? "Approved" : "Rejected";
        RejectionReason = approved ? null : reason?.Trim();
        ReviewedByUserId = reviewer; ReviewedAtUtc = now;
    }
}
