namespace OFOQ.Market.Domain.Commerce.Configuration;

public enum CommerceCapabilityType
{
    Unknown = 0,

    // -------------------------------------------------
    // Core retail inventory
    // -------------------------------------------------

    PhysicalStock = 10,

    Variants = 20,

    MultiWarehouse = 30,

    Barcode = 40,

    SerialTracking = 50,

    ImeiTracking = 60,

    LotTracking = 70,

    ExpiryTracking = 80,

    Suppliers = 90,

    PurchaseOrders = 100,

    GoodsReceiving = 110,

    StockTransfers = 120,

    StockCounts = 130,

    Returns = 140,

    Bundles = 150,

    PreOrder = 160,

    BackOrder = 170,

    Warranty = 180,

    CustomConfiguration = 190,

    // -------------------------------------------------
    // Specialized product models
    // -------------------------------------------------

    RecipeInventory = 200,

    CompatibilityMatrix = 210,

    // -------------------------------------------------
    // Booking / availability / assets
    // -------------------------------------------------

    Bookings = 220,

    AssetCalendar = 230,

    Reservations = 240,

    Deposits = 250,

    Maintenance = 260,

    // -------------------------------------------------
    // Real estate
    // -------------------------------------------------

    Listings = 270,

    Leads = 280,

    Maps = 290,

    // -------------------------------------------------
    // Services
    // -------------------------------------------------

    Appointments = 300,

    // -------------------------------------------------
    // Subscription / digital
    // -------------------------------------------------

    RecurringBilling = 310,

    Entitlements = 320,

    DigitalDelivery = 330,

    LicenseKeys = 340,

    // -------------------------------------------------
    // Events
    // -------------------------------------------------

    SeatInventory = 350,

    QrTickets = 360,

    // -------------------------------------------------
    // Delivery / restaurants
    // -------------------------------------------------

    Delivery = 370,

    Drivers = 380,

    DeliveryZones = 390,

    Branches = 400,

    TableOrdering = 410,

    KitchenWorkflow = 420,

    AddOns = 430,

    // -------------------------------------------------
    // B2B
    // -------------------------------------------------

    B2B = 440,

    PriceTiers = 450,

    MinimumOrderQuantity = 460,

    CreditTerms = 470
}