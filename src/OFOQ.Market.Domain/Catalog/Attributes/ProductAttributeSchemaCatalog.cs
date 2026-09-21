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

        if (SpecializedProductAttributeSchemaCatalog.TryGet(
                verticalType,
                out var specializedSchema))
        {
            return specializedSchema;
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

                    Text(
                        "brand",
                        "العلامة التجارية"),

                    Text(
                        "garment-type",
                        "نوع القطعة"),

                    Choice(
                        "gender",
                        "الفئة",
                        "men",
                        "women",
                        "kids",
                        "unisex"),

                    Text(
                        "material",
                        "الخامة / التركيبة"),

                    Text(
                        "fabric",
                        "نوع القماش"),

                    Choice(
                        "fit",
                        "القصة / Fit",
                        "slim",
                        "regular",
                        "relaxed",
                        "oversized"),

                    Text(
                        "style",
                        "النمط"),

                    Choice(
                        "season",
                        "الموسم",
                        "summer",
                        "winter",
                        "spring",
                        "autumn",
                        "all-seasons"),

                    Text(
                        "sleeve-type",
                        "نوع الأكمام"),

                    Text(
                        "neckline",
                        "نوع الياقة"),

                    Text(
                        "length",
                        "الطول"),

                    Text(
                        "pattern",
                        "النقشة / التصميم"),

                    Choice(
                        "size-system",
                        "نظام المقاسات",
                        "letter",
                        "eu",
                        "us",
                        "uk",
                        "custom"),

                    Text(
                        "country-of-origin",
                        "بلد الصنع"),

                    Text(
                        "care-instructions",
                        "تعليمات الغسيل والعناية")),
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

                    Text(
                        "brand",
                        "العلامة التجارية"),

                    Text(
                        "model",
                        "الموديل"),

                    Text(
                        "color",
                        "اللون"),

                    Integer(
                        "release-year",
                        "سنة الإصدار"),

                    Text(
                        "chipset",
                        "المعالج / Chipset"),

                    Text(
                        "gpu",
                        "معالج الرسوميات"),

                    Integer(
                        "ram-gb",
                        "الذاكرة RAM بالجيجابايت"),

                    Integer(
                        "storage-gb",
                        "مساحة التخزين بالجيجابايت"),

                    Decimal(
                        "screen-size-inch",
                        "حجم الشاشة بالبوصة"),

                    Choice(
                        "screen-type",
                        "نوع الشاشة",
                        "oled",
                        "amoled",
                        "ltpo-oled",
                        "lcd",
                        "other"),

                    Text(
                        "screen-resolution",
                        "دقة الشاشة"),

                    Integer(
                        "refresh-rate-hz",
                        "معدل تحديث الشاشة Hz"),

                    Integer(
                        "battery-mah",
                        "سعة البطارية mAh"),

                    Integer(
                        "charging-watt",
                        "سرعة الشحن W"),

                    Boolean(
                        "wireless-charging",
                        "شحن لاسلكي"),

                    Text(
                        "operating-system",
                        "نظام التشغيل"),

                    Choice(
                        "network",
                        "شبكة الاتصال",
                        "4g",
                        "5g"),

                    Boolean(
                        "dual-sim",
                        "شريحتان"),

                    Boolean(
                        "esim",
                        "يدعم eSIM"),

                    Boolean(
                        "nfc",
                        "يدعم NFC"),

                    Text(
                        "water-resistance",
                        "مقاومة الماء والغبار"),

                    Decimal(
                        "main-camera-mp",
                        "الكاميرا الرئيسية MP"),

                    Decimal(
                        "front-camera-mp",
                        "الكاميرا الأمامية MP"),

                    Integer(
                        "warranty-months",
                        "مدة الضمان بالأشهر"),

                    Text(
                        "warranty-provider",
                        "جهة الضمان"),

                    Choice(
                        "condition",
                        "حالة الجهاز",
                        "new",
                        "used",
                        "refurbished")),


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

                    Choice(
                        "electronics-type",
                        "نوع المنتج الإلكتروني",
                        "laptop",
                        "accessory"),

                    Text(
                        "brand",
                        "العلامة التجارية"),

                    Text(
                        "model",
                        "الموديل"),

                    Integer(
                        "release-year",
                        "سنة الإصدار"),

                    Integer(
                        "warranty-months",
                        "مدة الضمان بالأشهر"),

                    Text(
                        "warranty-provider",
                        "جهة الضمان"),

                    Choice(
                        "condition",
                        "حالة المنتج",
                        "new",
                        "used",
                        "refurbished"),

                    Choice(
                        "processor-brand",
                        "شركة المعالج",
                        "intel",
                        "amd",
                        "apple",
                        "qualcomm",
                        "other"),

                    Text(
                        "processor-model",
                        "المعالج"),

                    Text(
                        "processor-generation",
                        "جيل المعالج"),

                    Integer(
                        "cpu-cores",
                        "عدد أنوية المعالج"),

                    Text(
                        "gpu-model",
                        "كرت الشاشة"),

                    Choice(
                        "gpu-type",
                        "نوع كرت الشاشة",
                        "integrated",
                        "dedicated"),

                    Integer(
                        "vram-gb",
                        "ذاكرة كرت الشاشة VRAM"),

                    Integer(
                        "ram-gb",
                        "الذاكرة RAM بالجيجابايت"),

                    Text(
                        "ram-type",
                        "نوع RAM"),

                    Integer(
                        "ram-speed-mhz",
                        "سرعة RAM MHz"),

                    Integer(
                        "storage-gb",
                        "مساحة التخزين بالجيجابايت"),

                    Choice(
                        "storage-type",
                        "نوع التخزين",
                        "nvme",
                        "ssd",
                        "hdd",
                        "emmc"),

                    Decimal(
                        "screen-size-inch",
                        "حجم الشاشة بالبوصة"),

                    Text(
                        "screen-resolution",
                        "دقة الشاشة"),

                    Text(
                        "screen-panel",
                        "نوع لوحة الشاشة"),

                    Integer(
                        "refresh-rate-hz",
                        "معدل تحديث الشاشة Hz"),

                    Decimal(
                        "battery-wh",
                        "سعة البطارية Wh"),

                    Integer(
                        "charger-watt",
                        "قدرة الشاحن W"),

                    Text(
                        "operating-system",
                        "نظام التشغيل"),

                    Text(
                        "wifi",
                        "Wi-Fi"),

                    Text(
                        "bluetooth",
                        "Bluetooth"),

                    Text(
                        "ports",
                        "المنافذ"),

                    Boolean(
                        "backlit-keyboard",
                        "كيبورد بإضاءة"),

                    Decimal(
                        "weight-kg",
                        "الوزن بالكيلوجرام"),

                    Choice(
                        "accessory-type",
                        "نوع الإكسسوار",
                        "headphones",
                        "earbuds",
                        "charger",
                        "cable",
                        "power-bank",
                        "case",
                        "mouse",
                        "keyboard",
                        "hub",
                        "stand",
                        "other"),

                    Text(
                        "compatibility",
                        "الأجهزة المتوافقة"),

                    Choice(
                        "connection-type",
                        "نوع الاتصال",
                        "bluetooth",
                        "usb-c",
                        "usb-a",
                        "lightning",
                        "3.5mm",
                        "wireless",
                        "other"),

                    Boolean(
                        "wireless",
                        "لاسلكي"),

                    Integer(
                        "battery-mah",
                        "سعة البطارية mAh"),

                    Decimal(
                        "battery-life-hours",
                        "مدة تشغيل البطارية بالساعات"),

                    Decimal(
                        "power-watt",
                        "القدرة W"),

                    Decimal(
                        "cable-length-meter",
                        "طول الكابل بالمتر"),

                    Text(
                        "material",
                        "الخامة"),

                    Text(
                        "color",
                        "اللون"),

                    Text(
                        "size",
                        "المقاس / الأبعاد")),


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

    public static ProductAttributeSchema GetByCode(
        string verticalCode)
    {
        if (string.IsNullOrWhiteSpace(
                verticalCode))
        {
            throw new ArgumentException(
                "Product vertical code is required.",
                nameof(verticalCode));
        }

        var normalized =
            verticalCode
                .Trim()
                .ToLowerInvariant()
                .Replace('_', '-');

        foreach (var definition in
                 CommerceVerticalCatalog.All)
        {
            if (!string.Equals(
                    definition.Code,
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return Get(
                definition.VerticalType);
        }

        throw new ArgumentException(
            $"Unsupported product vertical code '{normalized}'.",
            nameof(verticalCode));
    }
}