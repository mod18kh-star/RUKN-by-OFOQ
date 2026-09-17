using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Domain.Catalog.Attributes;

public static class ProductAttributeSchemaCatalog
{
    private static readonly IReadOnlyDictionary<
        CommerceVerticalType,
        ProductAttributeSchema> Schemas =
        CreateSchemas();

    public static ProductAttributeSchema Get(
        CommerceVerticalType verticalType)
    {
        if (verticalType ==
            CommerceVerticalType.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(verticalType));
        }

        return Schemas.TryGetValue(
            verticalType,
            out var schema)
            ? schema
            : Empty(
                verticalType);
    }

    private static IReadOnlyDictionary<
        CommerceVerticalType,
        ProductAttributeSchema> CreateSchemas()
    {
        return new Dictionary<
            CommerceVerticalType,
            ProductAttributeSchema>
        {
            [CommerceVerticalType.GeneralRetail] =
                Schema(
                    CommerceVerticalType.GeneralRetail,
                    Text("brand", "Brand"),
                    Text("model", "Model")),

            [CommerceVerticalType.Apparel] =
                Schema(
                    CommerceVerticalType.Apparel,
                    Text("brand", "Brand"),
                    Choice(
                        "gender",
                        "Gender",
                        "Unisex",
                        "Men",
                        "Women",
                        "Kids"),
                    Text("material", "Material"),
                    Choice(
                        "fit",
                        "Fit",
                        "Slim",
                        "Regular",
                        "Relaxed",
                        "Oversized")),

            [CommerceVerticalType.Footwear] =
                Schema(
                    CommerceVerticalType.Footwear,
                    Text("brand", "Brand"),
                    Choice(
                        "gender",
                        "Gender",
                        "Unisex",
                        "Men",
                        "Women",
                        "Kids"),
                    Text("material", "Material")),

            [CommerceVerticalType.MobilePhones] =
                Schema(
                    CommerceVerticalType.MobilePhones,
                    Text("brand", "Brand"),
                    Text("model", "Model"),
                    Integer("storage-gb", "Storage GB"),
                    Integer("ram-gb", "RAM GB"),
                    Text("color", "Color"),
                    Decimal("screen-size-inch", "Screen Size Inch"),
                    Integer("warranty-months", "Warranty Months"),
                    Integer("release-year", "Release Year"),
                    Boolean("dual-sim", "Dual SIM"),
                    Choice(
                        "condition",
                        "Condition",
                        "New",
                        "Used",
                        "Refurbished")),

            [CommerceVerticalType.Perfumes] =
                Schema(
                    CommerceVerticalType.Perfumes,
                    Text("brand", "Brand"),
                    Choice(
                        "fragrance-family",
                        "Fragrance Family",
                        "Fresh",
                        "Floral",
                        "Woody",
                        "Oriental",
                        "Citrus"),
                    Decimal("volume-ml", "Volume ML"),
                    Choice(
                        "concentration",
                        "Concentration",
                        "EDC",
                        "EDT",
                        "EDP",
                        "Parfum")),

            [CommerceVerticalType.Electronics] =
                Schema(
                    CommerceVerticalType.Electronics,
                    Text("brand", "Brand"),
                    Text("model", "Model"),
                    Integer(
                        "warranty-months",
                        "Warranty Months")),

            [CommerceVerticalType.Services] =
                Schema(
                    CommerceVerticalType.Services,
                    Integer(
                        "duration-minutes",
                        "Duration Minutes"),
                    Choice(
                        "service-mode",
                        "Service Mode",
                        "On Site",
                        "Remote",
                        "At Customer")),

            [CommerceVerticalType.CarRental] =
                Schema(
                    CommerceVerticalType.CarRental,
                    Text("make", "Make"),
                    Text("model", "Model"),
                    Integer("year", "Year"),
                    Choice(
                        "transmission",
                        "Transmission",
                        "Automatic",
                        "Manual"),
                    Integer("seats", "Seats")),

            [CommerceVerticalType.RealEstate] =
                Schema(
                    CommerceVerticalType.RealEstate,
                    Choice(
                        "property-type",
                        "Property Type",
                        "Apartment",
                        "Villa",
                        "Office",
                        "Land",
                        "Shop"),
                    Integer("bedrooms", "Bedrooms"),
                    Decimal("bathrooms", "Bathrooms"),
                    Decimal("area-sqm", "Area SQM"),
                    Boolean("furnished", "Furnished")),

            [CommerceVerticalType.Restaurants] =
                Schema(
                    CommerceVerticalType.Restaurants,
                    Choice(
                        "dietary-type",
                        "Dietary Type",
                        "Regular",
                        "Vegetarian",
                        "Vegan",
                        "Halal"),
                    Boolean("spicy", "Spicy"),
                    Integer("calories", "Calories")),

            [CommerceVerticalType.Grocery] =
                Schema(
                    CommerceVerticalType.Grocery,
                    Text("brand", "Brand"),
                    Decimal(
                        "weight-grams",
                        "Weight Grams"),
                    Text(
                        "origin-country",
                        "Country of Origin"),
                    Boolean("organic", "Organic")),

            [CommerceVerticalType.AutomotiveParts] =
                Schema(
                    CommerceVerticalType.AutomotiveParts,
                    Text("brand", "Brand"),
                    Text(
                        "part-number",
                        "Part Number"),
                    Text(
                        "compatible-make",
                        "Compatible Make"),
                    Text(
                        "compatible-model",
                        "Compatible Model"),
                    Integer(
                        "year-from",
                        "Year From"),
                    Integer(
                        "year-to",
                        "Year To")),

            [CommerceVerticalType.DigitalProducts] =
                Schema(
                    CommerceVerticalType.DigitalProducts,
                    Text(
                        "file-format",
                        "File Format"),
                    Choice(
                        "license-type",
                        "License Type",
                        "Personal",
                        "Commercial",
                        "Subscription")),

            [CommerceVerticalType.JewelryAndWatches] =
                Schema(
                    CommerceVerticalType.JewelryAndWatches,
                    Text("brand", "Brand"),
                    Text("material", "Material"),
                    Text("gemstone", "Gemstone"),
                    Choice(
                        "gender",
                        "Gender",
                        "Unisex",
                        "Men",
                        "Women")),

            [CommerceVerticalType.FurnitureAndDecor] =
                Schema(
                    CommerceVerticalType.FurnitureAndDecor,
                    Text("material", "Material"),
                    Text("color", "Color"),
                    Decimal("width-cm", "Width CM"),
                    Decimal("height-cm", "Height CM"),
                    Decimal("depth-cm", "Depth CM")),

            [CommerceVerticalType.Cosmetics] =
                Schema(
                    CommerceVerticalType.Cosmetics,
                    Text("brand", "Brand"),
                    Text("shade", "Shade"),
                    Choice(
                        "skin-type",
                        "Skin Type",
                        "All",
                        "Dry",
                        "Normal",
                        "Oily",
                        "Combination",
                        "Sensitive")),

            [CommerceVerticalType.WholesaleB2B] =
                Schema(
                    CommerceVerticalType.WholesaleB2B,
                    Text("brand", "Brand"),
                    Integer(
                        "minimum-order-quantity",
                        "Minimum Order Quantity"),
                    Text(
                        "unit-of-measure",
                        "Unit of Measure")),

            [CommerceVerticalType.PersonalizedGifts] =
                Schema(
                    CommerceVerticalType.PersonalizedGifts,
                    Boolean(
                        "personalization-enabled",
                        "Personalization Enabled"),
                    Text(
                        "personalization-notes",
                        "Personalization Notes")),

            [CommerceVerticalType.EquipmentRental] =
                Schema(
                    CommerceVerticalType.EquipmentRental,
                    Text(
                        "equipment-type",
                        "Equipment Type"),
                    Choice(
                        "rental-unit",
                        "Rental Unit",
                        "Hour",
                        "Day",
                        "Week",
                        "Month"),
                    Boolean(
                        "deposit-required",
                        "Deposit Required")),

            [CommerceVerticalType.HomeGoods] =
                Schema(
                    CommerceVerticalType.HomeGoods,
                    Text("brand", "Brand"),
                    Text("material", "Material"),
                    Text("color", "Color"))
        };
    }

    private static ProductAttributeSchema Empty(
        CommerceVerticalType verticalType) =>
        new(
            verticalType,
            Array.Empty<ProductAttributeDefinition>());

    private static ProductAttributeSchema Schema(
        CommerceVerticalType verticalType,
        params ProductAttributeDefinition[] attributes) =>
        new(
            verticalType,
            attributes);

    private static ProductAttributeDefinition Text(
        string key,
        string label) =>
        new(
            key,
            label,
            ProductAttributeValueType.Text);

    private static ProductAttributeDefinition Integer(
        string key,
        string label) =>
        new(
            key,
            label,
            ProductAttributeValueType.Integer);

    private static ProductAttributeDefinition Decimal(
        string key,
        string label) =>
        new(
            key,
            label,
            ProductAttributeValueType.Decimal);

    private static ProductAttributeDefinition Boolean(
        string key,
        string label) =>
        new(
            key,
            label,
            ProductAttributeValueType.Boolean);

    private static ProductAttributeDefinition Choice(
        string key,
        string label,
        params string[] values) =>
        new(
            key,
            label,
            ProductAttributeValueType.Choice,
            values);
}