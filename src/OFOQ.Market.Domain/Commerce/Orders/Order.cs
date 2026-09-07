using OFOQ.Market.Domain.Catalog;
using OFOQ.Market.Domain.Commerce.Carts;
using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Identity;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Commerce.Orders;

public sealed class Order :
    AggregateRoot<OrderId>,
    ITenantDataScoped,
    IAuditable
{
    private readonly List<OrderItem> _items =
        [];

    private Guid _customerUserId;

    private CartId _sourceCartId;

    private string _currencyCode =
        string.Empty;

    private Order()
    {
    }


    private Order(
        OrderId id,
        TenantId tenantId,
        UserId customerUserId,
        CartId sourceCartId,
        CurrencyCode currency,
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

        if (customerUserId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer user ID cannot be empty.",
                nameof(customerUserId));
        }

        if (sourceCartId.IsEmpty)
        {
            throw new ArgumentException(
                "Source cart ID cannot be empty.",
                nameof(sourceCartId));
        }

        if (currency.IsEmpty)
        {
            throw new ArgumentException(
                "Order currency cannot be empty.",
                nameof(currency));
        }


        TenantId = tenantId;

        _customerUserId =
            customerUserId.Value;

        _sourceCartId =
            sourceCartId;

        _currencyCode =
            currency.Value;

        Status =
            OrderStatus.Pending;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }


    public TenantId TenantId { get; private set; }


    public UserId CustomerUserId =>
        UserId.From(
            _customerUserId);


    public CartId SourceCartId =>
        _sourceCartId;


    public CurrencyCode Currency =>
        CurrencyCode.Create(
            _currencyCode);


    public OrderStatus Status { get; private set; }


    public IReadOnlyCollection<OrderItem> Items =>
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



    public static Order Create(
        TenantId tenantId,
        UserId customerUserId,
        CartId sourceCartId,
        CurrencyCode currency,
        IReadOnlyCollection<OrderItemSnapshot> items,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
    {
        ArgumentNullException.ThrowIfNull(
            items);


        if (items.Count == 0)
        {
            throw new ArgumentException(
                "An order must contain at least one item.",
                nameof(items));
        }


        var order =
            new Order(
                OrderId.New(),
                tenantId,
                customerUserId,
                sourceCartId,
                currency,
                createdAtUtc,
                createdByUserId);



        var seenVariantIds =
            new HashSet<ProductVariantId>();


        foreach (var snapshot in items)
        {
            ArgumentNullException.ThrowIfNull(
                snapshot);


            if (snapshot.UnitPrice.Currency != currency)
            {
                throw new ArgumentException(
                    "Every order item must use the order currency.",
                    nameof(items));
            }


            if (!seenVariantIds.Add(
                    snapshot.ProductVariantId))
            {
                throw new ArgumentException(
                    "An order cannot contain the same product variant more than once.",
                    nameof(items));
            }


            order._items.Add(
                OrderItem.Create(
                    tenantId,
                    order.Id,
                    snapshot));
        }


        return order;
    }



    public void MarkPaid(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        Status =
            OrderStatus.Paid;


        UpdatedAtUtc =
            updatedAtUtc;


        UpdatedByUserId =
            updatedByUserId;
    }
}