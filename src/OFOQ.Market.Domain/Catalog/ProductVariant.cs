using OFOQ.Market.Domain.Common;
using OFOQ.Market.Domain.Tenancy;

namespace OFOQ.Market.Domain.Catalog;

public sealed class ProductVariant :
    Entity<ProductVariantId>,
    ITenantDataScoped,
    IAuditable,
    ISoftDeletable
{
    private ProductVariant()
    {
    }

    private ProductVariant(
        ProductVariantId id,
        TenantId tenantId,
        ProductId productId,
        string name,
        ProductSku sku,
        bool isDefault,
        Money? priceOverride,
        Inventory inventory,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId)
        : base(id)
    {
        TenantId =
            tenantId;

        ProductId =
            productId;

        Name =
            NormalizeName(
                name);

        Sku =
            sku;

        IsDefault =
            isDefault;

        PriceOverride =
            priceOverride;

        Inventory =
            inventory;

        IsEnabled =
            true;

        CreatedAtUtc =
            createdAtUtc;

        CreatedByUserId =
            createdByUserId;
    }

    public TenantId TenantId { get; private set; }

    public ProductId ProductId { get; private set; }

    public string Name { get; private set; } =
        string.Empty;

    public ProductSku Sku { get; private set; }

    public bool IsDefault { get; private set; }

    public Money? PriceOverride { get; private set; }

    public Inventory Inventory { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static ProductVariant Create(
        TenantId tenantId,
        ProductId productId,
        string name,
        ProductSku sku,
        CurrencyCode productCurrency,
        Inventory inventory,
        DateTimeOffset createdAtUtc,
        Money? priceOverride = null,
        bool isDefault = false,
        Guid? createdByUserId = null)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException(
                "Tenant ID cannot be empty.",
                nameof(tenantId));
        }

        if (productId.IsEmpty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(productId));
        }

        if (sku.IsEmpty)
        {
            throw new ArgumentException(
                "Product variant SKU is required.",
                nameof(sku));
        }

        if (productCurrency.IsEmpty)
        {
            throw new ArgumentException(
                "Product currency is required.",
                nameof(productCurrency));
        }

        ValidatePriceOverride(
            productCurrency,
            priceOverride);

        return new ProductVariant(
            ProductVariantId.New(),
            tenantId,
            productId,
            name,
            sku,
            isDefault,
            priceOverride,
            inventory,
            createdAtUtc,
            createdByUserId);
    }

    public void Rename(
        string name,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureCanModify();

        var normalized =
            NormalizeName(
                name);

        if (Name ==
            normalized)
        {
            return;
        }

        Name =
            normalized;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ChangeSku(
        ProductSku sku,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureCanModify();

        if (sku.IsEmpty)
        {
            throw new ArgumentException(
                "Product variant SKU is required.",
                nameof(sku));
        }

        if (Sku ==
            sku)
        {
            return;
        }

        Sku =
            sku;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void SetPriceOverride(
        Money? priceOverride,
        CurrencyCode productCurrency,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureCanModify();

        ValidatePriceOverride(
            productCurrency,
            priceOverride);

        if (PriceOverride ==
            priceOverride)
        {
            return;
        }

        PriceOverride =
            priceOverride;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void IncreaseStock(
        int quantity,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureCanModify();

        Inventory =
            Inventory.Increase(
                quantity);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void DecreaseStock(
        int quantity,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureCanModify();

        Inventory =
            Inventory.Decrease(
                quantity);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void SetStockQuantity(
        int quantity,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureCanModify();

        Inventory =
            Inventory.SetQuantity(
                quantity);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void ConfigureInventory(
        bool trackInventory,
        int lowStockThreshold,
        bool continueSellingWhenOutOfStock,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureCanModify();

        Inventory =
            Inventory.Configure(
                trackInventory,
                lowStockThreshold,
                continueSellingWhenOutOfStock);

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Enable(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureNotDeleted();

        if (IsEnabled)
        {
            return;
        }

        IsEnabled =
            true;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Disable(
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId = null)
    {
        EnsureNotDeleted();

        if (!IsEnabled)
        {
            return;
        }

        IsEnabled =
            false;

        MarkUpdated(
            updatedAtUtc,
            updatedByUserId);
    }

    public void Delete(
        DateTimeOffset deletedAtUtc,
        Guid? deletedByUserId = null)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted =
            true;

        IsEnabled =
            false;

        DeletedAtUtc =
            deletedAtUtc;

        DeletedByUserId =
            deletedByUserId;

        MarkUpdated(
            deletedAtUtc,
            deletedByUserId);
    }

    private static void ValidatePriceOverride(
        CurrencyCode productCurrency,
        Money? priceOverride)
    {
        if (!priceOverride.HasValue)
        {
            return;
        }

        if (priceOverride.Value.Currency !=
            productCurrency)
        {
            throw new ArgumentException(
                "Variant price override must use the same currency as the product.",
                nameof(priceOverride));
        }
    }

    private void EnsureCanModify()
    {
        EnsureNotDeleted();

        if (!IsEnabled)
        {
            throw new InvalidOperationException(
                "A disabled product variant cannot be modified.");
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "A deleted product variant cannot be modified.");
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

    private static string NormalizeName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Product variant name is required.",
                nameof(name));
        }

        var normalized =
            name.Trim();

        if (normalized.Length > 160)
        {
            throw new ArgumentException(
                "Product variant name cannot exceed 160 characters.",
                nameof(name));
        }

        return normalized;
    }
}