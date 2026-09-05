using System.Text;
using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Orders;

public sealed class OrderItem :
    Entity<OrderItemId>,
    ITenantDataScoped
{
    public const int MaximumQuantity =
        999;

    private decimal _unitPriceAmount;

    private string _unitPriceCurrencyCode =
        string.Empty;

    private OrderItem()
    {
    }

    private OrderItem(
        OrderItemId id,
        TenantId tenantId,
        OrderId orderId,
        ProductId productId,
        ProductVariantId productVariantId,
        string productName,
        string variantName,
        string sku,
        Money unitPrice,
        int quantity)
        : base(id)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (orderId.IsEmpty)
        {
            throw new ArgumentException(
                "Order ID cannot be empty.",
                nameof(orderId));
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

        OrderId =
            orderId;

        ProductId =
            productId;

        ProductVariantId =
            productVariantId;

        ProductName =
            NormalizeRequiredText(
                productName,
                200,
                nameof(productName));

        VariantName =
            NormalizeRequiredText(
                variantName,
                160,
                nameof(variantName));

        Sku =
            NormalizeRequiredText(
                sku,
                64,
                nameof(sku));

        ApplyUnitPrice(
            unitPrice);

        Quantity =
            ValidateQuantity(
                quantity);
    }

    public TenantId TenantId { get; private set; }

    public OrderId OrderId { get; private set; }

    public ProductId ProductId { get; private set; }

    public ProductVariantId ProductVariantId { get; private set; }

    public string ProductName { get; private set; } =
        string.Empty;

    public string VariantName { get; private set; } =
        string.Empty;

    public string Sku { get; private set; } =
        string.Empty;

    public Money UnitPrice =>
        Money.Create(
            _unitPriceAmount,
            _unitPriceCurrencyCode);

    public int Quantity { get; private set; }

    public decimal LineTotal =>
        checked(
            _unitPriceAmount *
            Quantity);

    internal static OrderItem Create(
        TenantId tenantId,
        OrderId orderId,
        OrderItemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(
            snapshot);

        return new OrderItem(
            OrderItemId.New(),
            tenantId,
            orderId,
            snapshot.ProductId,
            snapshot.ProductVariantId,
            snapshot.ProductName,
            snapshot.VariantName,
            snapshot.Sku,
            snapshot.UnitPrice,
            snapshot.Quantity);
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
        if (quantity <= 0 ||
            quantity > MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                $"Order item quantity must be between 1 and {MaximumQuantity}.");
        }

        return quantity;
    }

    private static string NormalizeRequiredText(
        string value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                "A required order item snapshot value cannot be empty.",
                parameterName);
        }

        var normalized =
            value
                .Trim()
                .Normalize(
                    NormalizationForm.FormKC);

        if (normalized.Length >
            maximumLength)
        {
            throw new ArgumentException(
                $"Order item snapshot value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}