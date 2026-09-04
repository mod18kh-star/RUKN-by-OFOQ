using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Carts;

public sealed class CartItem :
    Entity<CartItemId>,
    ITenantDataScoped,
    IAuditable
{
    public const int MaximumQuantity = 999;

    private decimal _unitPriceAmount;

    private string _unitPriceCurrencyCode =
        string.Empty;

    private CartItem()
    {
    }

    private CartItem(
        CartItemId id,
        TenantId tenantId,
        CartId cartId,
        ProductId productId,
        ProductVariantId productVariantId,
        Money unitPrice,
        int quantity,
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

        if (cartId.IsEmpty)
        {
            throw new ArgumentException(
                "Cart ID cannot be empty.",
                nameof(cartId));
        }

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

        TenantId =
            tenantId;

        CartId =
            cartId;

        ProductId =
            productId;

        ProductVariantId =
            productVariantId;

        ApplyUnitPrice(
            unitPrice);

        Quantity =
            ValidateQuantity(
                quantity);

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public CartId CartId { get; private set; }

    public ProductId ProductId { get; private set; }

    public ProductVariantId ProductVariantId { get; private set; }

    public Money UnitPrice =>
        Money.Create(
            _unitPriceAmount,
            _unitPriceCurrencyCode);

    public int Quantity { get; private set; }

    public decimal LineTotal =>
        _unitPriceAmount * Quantity;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    internal static CartItem Create(
        TenantId tenantId,
        CartId cartId,
        ProductId productId,
        ProductVariantId productVariantId,
        Money unitPrice,
        int quantity,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        return new CartItem(
            CartItemId.New(),
            tenantId,
            cartId,
            productId,
            productVariantId,
            unitPrice,
            quantity,
            createdAtUtc,
            createdByUserId);
    }

    internal void IncreaseQuantity(
        int quantity,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity increment must be greater than zero.");
        }

        Quantity =
            ValidateQuantity(
                checked(
                    Quantity + quantity));

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    internal void ChangeQuantity(
        int quantity,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        var validated =
            ValidateQuantity(
                quantity);

        if (Quantity == validated)
        {
            return;
        }

        Quantity =
            validated;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    internal void RefreshUnitPrice(
        Money unitPrice,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        if (UnitPrice == unitPrice)
        {
            return;
        }

        ApplyUnitPrice(
            unitPrice);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    private void ApplyUnitPrice(
        Money unitPrice)
    {
        _unitPriceAmount =
            unitPrice.Amount;

        _unitPriceCurrencyCode =
            unitPrice.Currency.Value;
    }

    private static int ValidateQuantity(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Cart item quantity must be greater than zero.");
        }

        if (quantity > MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                $"Cart item quantity cannot exceed {MaximumQuantity}.");
        }

        return quantity;
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