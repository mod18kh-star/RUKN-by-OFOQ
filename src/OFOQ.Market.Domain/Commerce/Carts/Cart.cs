using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Carts;

public sealed class Cart :
    AggregateRoot<CartId>,
    ITenantDataScoped,
    IAuditable
{
    private readonly List<CartItem> _items = [];

    private Guid? _customerUserId;

    private string? _currencyCode;

    private Cart()
    {
    }

    private Cart(
        CartId id,
        TenantId tenantId,
        UserId? customerUserId,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (customerUserId.HasValue &&
            customerUserId.Value.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        TenantId =
            tenantId;

        _customerUserId =
            customerUserId?.Value;

        Status =
            CartStatus.Active;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public UserId? CustomerUserId =>
        _customerUserId.HasValue
            ? UserId.From(
                _customerUserId.Value)
            : null;

    public CartStatus Status { get; private set; }

    public CurrencyCode? Currency =>
        string.IsNullOrWhiteSpace(
            _currencyCode)
            ? null
            : CurrencyCode.Create(
                _currencyCode);

    public IReadOnlyCollection<CartItem> Items =>
        _items.AsReadOnly();

    public int TotalQuantity =>
        _items.Sum(
            item =>
                item.Quantity);

    public decimal TotalAmount =>
        _items.Sum(
            item =>
                item.LineTotal);

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static Cart Create(
        TenantId tenantId,
        UserId? customerUserId,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new Cart(
            CartId.New(),
            tenantId,
            customerUserId,
            createdAtUtc,
            createdByUserId);
    }

    public CartItem AddItem(
        ProductId productId,
        ProductVariantId productVariantId,
        Money unitPrice,
        int quantity,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureActive();

        if (productId.IsEmpty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(productId));
        }

        if (productVariantId.IsEmpty)
        {
            throw new ArgumentException(
                "Product variant ID cannot be empty.",
                nameof(productVariantId));
        }

        EnsureCurrency(
            unitPrice.Currency);

        var existing =
            _items.FirstOrDefault(
                item =>
                    item.ProductVariantId ==
                    productVariantId);

        if (existing is not null)
        {
            if (existing.ProductId != productId)
            {
                throw new InvalidOperationException(
                    "The product variant is already associated with a different product in this cart.");
            }

            existing.RefreshUnitPrice(
                unitPrice,
                updatedAtUtc,
                updatedByUserId);

            existing.IncreaseQuantity(
                quantity,
                updatedAtUtc,
                updatedByUserId);

            MarkUpdated(
                updatedAtUtc,
                updatedByUserId);

            return existing;
        }

        var item =
            CartItem.Create(
                TenantId,
                Id,
                productId,
                productVariantId,
                unitPrice,
                quantity,
                updatedAtUtc,
                updatedByUserId);

        _items.Add(
            item);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);

        return item;
    }

    public void ChangeItemQuantity(
        CartItemId itemId,
        int quantity,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureActive();

        var item =
            GetRequiredItem(
                itemId);

        item.ChangeQuantity(
            quantity,
            updatedAtUtc,
            updatedByUserId);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void RefreshItemPrice(
        CartItemId itemId,
        Money unitPrice,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureActive();

        EnsureCurrency(
            unitPrice.Currency);

        var item =
            GetRequiredItem(
                itemId);

        item.RefreshUnitPrice(
            unitPrice,
            updatedAtUtc,
            updatedByUserId);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void RemoveItem(
        CartItemId itemId,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureActive();

        var item =
            GetRequiredItem(
                itemId);

        _items.Remove(
            item);

        ResetCurrencyIfEmpty();

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Clear(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureActive();

        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();

        _currencyCode =
            null;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void MarkConverted(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureActive();

        if (_items.Count == 0)
        {
            throw new InvalidOperationException(
                "An empty cart cannot be converted.");
        }

        Status =
            CartStatus.Converted;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void MarkAbandoned(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureActive();

        Status =
            CartStatus.Abandoned;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    private CartItem GetRequiredItem(
        CartItemId itemId)
    {
        if (itemId.IsEmpty)
        {
            throw new ArgumentException(
                "Cart item ID cannot be empty.",
                nameof(itemId));
        }

        return _items.FirstOrDefault(
                   item =>
                       item.Id == itemId)
               ?? throw new InvalidOperationException(
                   "The requested cart item was not found.");
    }

    private void EnsureCurrency(
        CurrencyCode currency)
    {
        if (string.IsNullOrWhiteSpace(
                _currencyCode))
        {
            _currencyCode =
                currency.Value;

            return;
        }

        if (!string.Equals(
                _currencyCode,
                currency.Value,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A cart cannot contain items with different currencies.");
        }
    }

    private void ResetCurrencyIfEmpty()
    {
        if (_items.Count == 0)
        {
            _currencyCode =
                null;
        }
    }

    private void EnsureActive()
    {
        if (Status != CartStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active cart can be modified.");
        }
    }

    private void MarkUpdated(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        UpdatedAtUtc =
            updatedAtUtc;

        UpdatedByUserId =
            updatedByUserId;
    }
}