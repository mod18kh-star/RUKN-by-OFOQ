namespace OFOQ.Market.Domain.Commerce.Configuration;

public static class CommerceVerticalCatalog
{
    private static readonly IReadOnlyDictionary<
        CommerceVerticalType,
        CommerceVerticalDefinition> Definitions =
        CreateDefinitions();

    public static IReadOnlyCollection<CommerceVerticalDefinition> All =>
        Definitions
            .Values
            .OrderBy(
                definition =>
                    definition.VerticalType)
            .ToArray();

    public static CommerceVerticalDefinition Get(
        CommerceVerticalType verticalType)
    {
        if (!Definitions.TryGetValue(
                verticalType,
                out var definition))
        {
            throw new ArgumentOutOfRangeException(
                nameof(verticalType),
                verticalType,
                "Unsupported commerce vertical.");
        }

        return definition;
    }

    public static bool TryGet(
        CommerceVerticalType verticalType,
        out CommerceVerticalDefinition? definition)
    {
        return Definitions.TryGetValue(
            verticalType,
            out definition);
    }

    private static IReadOnlyDictionary<
        CommerceVerticalType,
        CommerceVerticalDefinition> CreateDefinitions()
    {
        var definitions =
            new[]
            {
                Define(
                    CommerceVerticalType.GeneralRetail,
                    "general-retail",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.Returns,
                    CommerceCapabilityType.Bundles,
                    CommerceCapabilityType.PreOrder,
                    CommerceCapabilityType.BackOrder),

                Define(
                    CommerceVerticalType.Apparel,
                    "apparel",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.Returns,
                    CommerceCapabilityType.PreOrder,
                    CommerceCapabilityType.BackOrder),

                Define(
                    CommerceVerticalType.Footwear,
                    "footwear",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.Returns,
                    CommerceCapabilityType.PreOrder,
                    CommerceCapabilityType.BackOrder),

                Define(
                    CommerceVerticalType.MobilePhones,
                    "mobile-phones",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.SerialTracking,
                    CommerceCapabilityType.ImeiTracking,
                    CommerceCapabilityType.Warranty,
                    CommerceCapabilityType.Returns),

                Define(
                    CommerceVerticalType.Perfumes,
                    "perfumes",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.Bundles,
                    CommerceCapabilityType.Returns),

                Define(
                    CommerceVerticalType.Electronics,
                    "electronics",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.SerialTracking,
                    CommerceCapabilityType.Warranty,
                    CommerceCapabilityType.Returns,
                    CommerceCapabilityType.Bundles),

                Define(
                    CommerceVerticalType.Subscriptions,
                    "subscriptions",
                    CommerceCapabilityType.RecurringBilling,
                    CommerceCapabilityType.Entitlements),

                Define(
                    CommerceVerticalType.Services,
                    "services",
                    CommerceCapabilityType.Bookings,
                    CommerceCapabilityType.Appointments,
                    CommerceCapabilityType.CustomConfiguration,
                    CommerceCapabilityType.AddOns),

                Define(
                    CommerceVerticalType.CarRental,
                    "car-rental",
                    CommerceCapabilityType.Bookings,
                    CommerceCapabilityType.AssetCalendar,
                    CommerceCapabilityType.Reservations,
                    CommerceCapabilityType.Deposits,
                    CommerceCapabilityType.Maintenance,
                    CommerceCapabilityType.Branches),

                Define(
                    CommerceVerticalType.RealEstate,
                    "real-estate",
                    CommerceCapabilityType.Listings,
                    CommerceCapabilityType.Leads,
                    CommerceCapabilityType.Maps,
                    CommerceCapabilityType.Appointments,
                    CommerceCapabilityType.Reservations),

                Define(
                    CommerceVerticalType.Restaurants,
                    "restaurants",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.RecipeInventory,
                    CommerceCapabilityType.Branches,
                    CommerceCapabilityType.Delivery,
                    CommerceCapabilityType.DeliveryZones,
                    CommerceCapabilityType.TableOrdering,
                    CommerceCapabilityType.KitchenWorkflow,
                    CommerceCapabilityType.AddOns,
                    CommerceCapabilityType.Bundles),

                Define(
                    CommerceVerticalType.DeliveryMarketplace,
                    "delivery-marketplace",
                    CommerceCapabilityType.Delivery,
                    CommerceCapabilityType.Drivers,
                    CommerceCapabilityType.DeliveryZones,
                    CommerceCapabilityType.Branches),

                Define(
                    CommerceVerticalType.Grocery,
                    "grocery",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.LotTracking,
                    CommerceCapabilityType.ExpiryTracking,
                    CommerceCapabilityType.Suppliers,
                    CommerceCapabilityType.PurchaseOrders,
                    CommerceCapabilityType.GoodsReceiving,
                    CommerceCapabilityType.StockTransfers,
                    CommerceCapabilityType.StockCounts,
                    CommerceCapabilityType.Returns,
                    CommerceCapabilityType.PreOrder,
                    CommerceCapabilityType.BackOrder),

                Define(
                    CommerceVerticalType.AutomotiveParts,
                    "automotive-parts",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.CompatibilityMatrix,
                    CommerceCapabilityType.Suppliers,
                    CommerceCapabilityType.PurchaseOrders,
                    CommerceCapabilityType.GoodsReceiving,
                    CommerceCapabilityType.StockTransfers,
                    CommerceCapabilityType.StockCounts,
                    CommerceCapabilityType.Returns),

                Define(
                    CommerceVerticalType.DigitalProducts,
                    "digital-products",
                    CommerceCapabilityType.DigitalDelivery,
                    CommerceCapabilityType.Entitlements,
                    CommerceCapabilityType.LicenseKeys),

                Define(
                    CommerceVerticalType.JewelryAndWatches,
                    "jewelry-watches",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.SerialTracking,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.Warranty,
                    CommerceCapabilityType.CustomConfiguration,
                    CommerceCapabilityType.Returns),

                Define(
                    CommerceVerticalType.FurnitureAndDecor,
                    "furniture-decor",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.CustomConfiguration,
                    CommerceCapabilityType.Suppliers,
                    CommerceCapabilityType.PreOrder,
                    CommerceCapabilityType.BackOrder,
                    CommerceCapabilityType.Returns),

                Define(
                    CommerceVerticalType.Cosmetics,
                    "cosmetics",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.LotTracking,
                    CommerceCapabilityType.ExpiryTracking,
                    CommerceCapabilityType.Bundles,
                    CommerceCapabilityType.Returns),

                Define(
                    CommerceVerticalType.EventsAndTickets,
                    "events-tickets",
                    CommerceCapabilityType.Bookings,
                    CommerceCapabilityType.Reservations,
                    CommerceCapabilityType.SeatInventory,
                    CommerceCapabilityType.QrTickets),

                Define(
                    CommerceVerticalType.WholesaleB2B,
                    "wholesale-b2b",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.Suppliers,
                    CommerceCapabilityType.PurchaseOrders,
                    CommerceCapabilityType.GoodsReceiving,
                    CommerceCapabilityType.StockTransfers,
                    CommerceCapabilityType.StockCounts,
                    CommerceCapabilityType.B2B,
                    CommerceCapabilityType.PriceTiers,
                    CommerceCapabilityType.MinimumOrderQuantity,
                    CommerceCapabilityType.CreditTerms),

                Define(
                    CommerceVerticalType.PersonalizedGifts,
                    "personalized-gifts",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.CustomConfiguration,
                    CommerceCapabilityType.Bundles,
                    CommerceCapabilityType.PreOrder),

                Define(
                    CommerceVerticalType.EquipmentRental,
                    "equipment-rental",
                    CommerceCapabilityType.Bookings,
                    CommerceCapabilityType.AssetCalendar,
                    CommerceCapabilityType.Reservations,
                    CommerceCapabilityType.Deposits,
                    CommerceCapabilityType.Maintenance,
                    CommerceCapabilityType.Branches),

                Define(
                    CommerceVerticalType.HomeGoods,
                    "home-goods",
                    CommerceCapabilityType.PhysicalStock,
                    CommerceCapabilityType.Variants,
                    CommerceCapabilityType.MultiWarehouse,
                    CommerceCapabilityType.Barcode,
                    CommerceCapabilityType.Bundles,
                    CommerceCapabilityType.Returns)
            };

        return definitions.ToDictionary(
            definition =>
                definition.VerticalType);
    }

    private static CommerceVerticalDefinition Define(
        CommerceVerticalType verticalType,
        string code,
        params CommerceCapabilityType[] capabilities)
    {
        return new CommerceVerticalDefinition(
            verticalType,
            code,
            capabilities);
    }
}