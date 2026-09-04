namespace OFOQ.Market.Domain.Catalog;

public readonly record struct Inventory
{
    private Inventory(
        bool trackInventory,
        int quantity,
        int lowStockThreshold,
        bool continueSellingWhenOutOfStock)
    {
        TrackInventory =
            trackInventory;

        Quantity =
            quantity;

        LowStockThreshold =
            lowStockThreshold;

        ContinueSellingWhenOutOfStock =
            continueSellingWhenOutOfStock;
    }

    public bool TrackInventory { get; }

    public int Quantity { get; }

    public int LowStockThreshold { get; }

    public bool ContinueSellingWhenOutOfStock { get; }

    public bool IsOutOfStock =>
        TrackInventory &&
        Quantity <= 0;

    public bool IsLowStock =>
        TrackInventory &&
        Quantity > 0 &&
        Quantity <= LowStockThreshold;

    public bool IsAvailableForSale =>
        !TrackInventory
        || Quantity > 0
        || ContinueSellingWhenOutOfStock;

    public static Inventory Create(
        bool trackInventory,
        int quantity = 0,
        int lowStockThreshold = 0,
        bool continueSellingWhenOutOfStock = false)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Inventory quantity cannot be negative.");
        }

        if (lowStockThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lowStockThreshold),
                "Low stock threshold cannot be negative.");
        }

        return new Inventory(
            trackInventory,
            quantity,
            lowStockThreshold,
            continueSellingWhenOutOfStock);
    }

    public Inventory Increase(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Inventory increase quantity must be greater than zero.");
        }

        checked
        {
            return new Inventory(
                TrackInventory,
                Quantity + quantity,
                LowStockThreshold,
                ContinueSellingWhenOutOfStock);
        }
    }

    public Inventory Decrease(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Inventory decrease quantity must be greater than zero.");
        }

        if (!TrackInventory)
        {
            return this;
        }

        if (!ContinueSellingWhenOutOfStock &&
            quantity > Quantity)
        {
            throw new InvalidOperationException(
                "Insufficient inventory quantity.");
        }

        var newQuantity =
            Math.Max(
                0,
                Quantity - quantity);

        return new Inventory(
            TrackInventory,
            newQuantity,
            LowStockThreshold,
            ContinueSellingWhenOutOfStock);
    }

    public Inventory Configure(
        bool trackInventory,
        int lowStockThreshold,
        bool continueSellingWhenOutOfStock)
    {
        if (lowStockThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lowStockThreshold),
                "Low stock threshold cannot be negative.");
        }

        return new Inventory(
            trackInventory,
            Quantity,
            lowStockThreshold,
            continueSellingWhenOutOfStock);
    }

    public Inventory SetQuantity(
        int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Inventory quantity cannot be negative.");
        }

        return new Inventory(
            TrackInventory,
            quantity,
            LowStockThreshold,
            ContinueSellingWhenOutOfStock);
    }
}