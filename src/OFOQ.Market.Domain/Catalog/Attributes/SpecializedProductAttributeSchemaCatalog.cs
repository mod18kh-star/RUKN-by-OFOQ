using OFOQ.Market.Domain.Commerce.Configuration;

namespace OFOQ.Market.Domain.Catalog.Attributes;

public static class SpecializedProductAttributeSchemaCatalog
{
    private static readonly IReadOnlyDictionary<CommerceVerticalType, ProductAttributeSchema> Schemas =
        CreateSchemas();

    public static bool TryGet(
        CommerceVerticalType verticalType,
        out ProductAttributeSchema? schema)
    {
        return Schemas.TryGetValue(verticalType, out schema);
    }

    private static IReadOnlyDictionary<CommerceVerticalType, ProductAttributeSchema> CreateSchemas()
    {
        return new Dictionary<CommerceVerticalType, ProductAttributeSchema>
        {
            [CommerceVerticalType.Footwear] =
                Schema(
                    CommerceVerticalType.Footwear,
                    Text("brand", "العلامة التجارية"),
                    Choice("footwear-type", "نوع الحذاء", "sneakers", "formal", "sandals", "boots", "slippers", "sports", "other"),
                    Choice("gender", "الفئة", "unisex", "men", "women", "kids"),
                    Text("material", "الخامة"),
                    Text("sole-material", "خامة النعل"),
                    Choice("closure-type", "نوع الإغلاق", "laces", "slip-on", "velcro", "zipper", "buckle", "other"),
                    Text("use-case", "الاستخدام"),
                    Choice("size-system", "نظام المقاسات", "eu", "us", "uk", "cm", "custom"),
                    Choice("width-fit", "عرض القدم", "narrow", "regular", "wide"),
                    Boolean("waterproof", "مقاوم للماء"),
                    Text("country-of-origin", "بلد الصنع"),
                    Text("care-instructions", "تعليمات العناية")
                ),
            [CommerceVerticalType.Perfumes] =
                Schema(
                    CommerceVerticalType.Perfumes,
                    Text("brand", "العلامة التجارية"),
                    Text("perfume-name", "اسم العطر"),
                    Choice("gender", "الفئة", "men", "women", "unisex"),
                    Choice("fragrance-family", "العائلة العطرية", "fresh", "floral", "woody", "oriental", "citrus", "gourmand", "leather", "aromatic"),
                    Decimal("volume-ml", "الحجم الأساسي ML"),
                    Choice("concentration", "التركيز", "EDC", "EDT", "EDP", "Parfum", "Extrait"),
                    Text("top-notes", "النوتات العليا"),
                    Text("heart-notes", "النوتات الوسطى"),
                    Text("base-notes", "النوتات الأساسية"),
                    Text("origin-country", "بلد المنشأ"),
                    Text("season", "الموسم المناسب"),
                    Boolean("tester", "تستر"),
                    Boolean("gift-set", "طقم هدية")
                ),
            [CommerceVerticalType.Subscriptions] =
                Schema(
                    CommerceVerticalType.Subscriptions,
                    Text("subscription-type", "نوع الاشتراك"),
                    Choice("billing-cycle", "دورة الدفع", "monthly", "quarterly", "semiannual", "yearly", "one-time"),
                    Integer("duration-days", "مدة الاشتراك بالأيام"),
                    Integer("max-users", "الحد الأقصى للمستخدمين"),
                    Integer("max-devices", "الحد الأقصى للأجهزة"),
                    Boolean("auto-renewal", "تجديد تلقائي"),
                    Integer("trial-days", "مدة التجربة بالأيام"),
                    Text("feature-summary", "مزايا الاشتراك"),
                    Text("entitlement-notes", "تفاصيل الصلاحيات"),
                    Text("cancellation-policy", "سياسة الإلغاء")
                ),
            [CommerceVerticalType.Services] =
                Schema(
                    CommerceVerticalType.Services,
                    Text("service-category", "نوع الخدمة"),
                    Integer("duration-minutes", "مدة الخدمة بالدقائق"),
                    Choice("service-mode", "طريقة تقديم الخدمة", "on-site", "remote", "at-customer"),
                    Boolean("appointment-required", "تحتاج موعد"),
                    Integer("lead-time-hours", "وقت التجهيز بالساعات"),
                    Text("location-notes", "تفاصيل الموقع"),
                    Text("preparation-notes", "ما يحتاجه العميل قبل الموعد"),
                    Text("deliverables", "ما الذي سيحصل عليه العميل"),
                    Integer("staff-required", "عدد مقدمي الخدمة"),
                    Text("cancellation-policy", "سياسة الإلغاء")
                ),
            [CommerceVerticalType.CarRental] =
                Schema(
                    CommerceVerticalType.CarRental,
                    Text("make", "الشركة المصنعة"),
                    Text("model", "الموديل"),
                    Integer("year", "سنة الصنع"),
                    Text("vehicle-class", "فئة السيارة"),
                    Choice("transmission", "ناقل الحركة", "automatic", "manual"),
                    Choice("fuel-type", "نوع الوقود", "petrol", "diesel", "hybrid", "electric"),
                    Integer("seats", "عدد المقاعد"),
                    Integer("doors", "عدد الأبواب"),
                    Integer("luggage", "سعة الأمتعة"),
                    Decimal("daily-mileage-limit", "حد المسافة اليومي KM"),
                    Boolean("unlimited-mileage", "كيلومترات مفتوحة"),
                    Decimal("deposit-amount", "قيمة الوديعة"),
                    Text("insurance-type", "نوع التأمين"),
                    Integer("minimum-driver-age", "الحد الأدنى لعمر السائق"),
                    Text("pickup-location", "موقع الاستلام")
                ),
            [CommerceVerticalType.RealEstate] =
                Schema(
                    CommerceVerticalType.RealEstate,
                    Choice("listing-purpose", "الغرض", "sale", "rent"),
                    Choice("property-type", "نوع العقار", "apartment", "villa", "office", "land", "shop", "warehouse"),
                    Text("city", "المدينة"),
                    Text("district", "الحي"),
                    Decimal("area-sqm", "المساحة م²"),
                    Integer("bedrooms", "غرف النوم"),
                    Decimal("bathrooms", "دورات المياه"),
                    Boolean("furnished", "مفروش"),
                    Integer("parking-spaces", "مواقف السيارات"),
                    Integer("property-age-years", "عمر العقار بالسنوات"),
                    Integer("floor-number", "رقم الطابق"),
                    Text("orientation", "الواجهة"),
                    Decimal("latitude", "خط العرض"),
                    Decimal("longitude", "خط الطول"),
                    Text("amenities", "المرافق والمميزات"),
                    Text("available-from", "متاح من")
                ),
            [CommerceVerticalType.Restaurants] =
                Schema(
                    CommerceVerticalType.Restaurants,
                    Text("dish-type", "نوع الصنف"),
                    Text("cuisine", "المطبخ"),
                    Choice("dietary-type", "النظام الغذائي", "regular", "vegetarian", "vegan", "halal", "gluten-free"),
                    Boolean("spicy", "حار"),
                    Choice("spice-level", "درجة الحِر", "mild", "medium", "hot", "extra-hot"),
                    Integer("calories", "السعرات الحرارية"),
                    Integer("preparation-minutes", "وقت التحضير بالدقائق"),
                    Text("ingredients", "المكونات"),
                    Text("allergens", "مسببات الحساسية"),
                    Text("serving-size", "حجم الحصة"),
                    Text("availability-window", "أوقات التوفر")
                ),
            [CommerceVerticalType.DeliveryMarketplace] =
                Schema(
                    CommerceVerticalType.DeliveryMarketplace,
                    Text("service-type", "نوع خدمة التوصيل"),
                    Text("pickup-area", "منطقة الاستلام"),
                    Text("delivery-area", "منطقة التوصيل"),
                    Integer("estimated-minutes", "المدة التقديرية بالدقائق"),
                    Decimal("max-distance-km", "أقصى مسافة KM"),
                    Choice("vehicle-type", "نوع المركبة", "bike", "motorcycle", "car", "van", "truck"),
                    Decimal("base-fee", "الرسوم الأساسية"),
                    Decimal("fee-per-km", "رسوم الكيلومتر"),
                    Boolean("cash-on-delivery", "دفع عند الاستلام"),
                    Boolean("same-day", "توصيل بنفس اليوم"),
                    Text("service-notes", "ملاحظات الخدمة")
                ),
            [CommerceVerticalType.Grocery] =
                Schema(
                    CommerceVerticalType.Grocery,
                    Text("brand", "العلامة التجارية"),
                    Text("product-type", "نوع المنتج"),
                    Decimal("weight-grams", "الوزن بالجرام"),
                    Choice("weight-unit", "وحدة القياس", "g", "kg", "ml", "l", "piece"),
                    Text("origin-country", "بلد المنشأ"),
                    Text("ingredients", "المكونات"),
                    Text("allergens", "مسببات الحساسية"),
                    Text("storage-instructions", "تعليمات التخزين"),
                    Boolean("organic", "عضوي"),
                    Boolean("halal-certified", "شهادة حلال"),
                    Integer("expiry-days", "مدة الصلاحية بالأيام"),
                    Integer("pack-count", "عدد القطع في العبوة"),
                    Text("barcode", "الباركود")
                ),
            [CommerceVerticalType.AutomotiveParts] =
                Schema(
                    CommerceVerticalType.AutomotiveParts,
                    Text("brand", "العلامة التجارية"),
                    Text("part-number", "رقم القطعة"),
                    Text("oem-number", "رقم OEM"),
                    Text("part-category", "فئة القطعة"),
                    Choice("condition", "الحالة", "new", "used", "refurbished"),
                    Text("compatible-make", "الشركة المتوافقة"),
                    Text("compatible-model", "الموديل المتوافق"),
                    Integer("year-from", "من سنة"),
                    Integer("year-to", "إلى سنة"),
                    Text("engine", "المحرك المتوافق"),
                    Text("position", "موضع القطعة"),
                    Boolean("original-equipment", "أصلي OEM"),
                    Integer("warranty-months", "مدة الضمان بالأشهر")
                ),
            [CommerceVerticalType.DigitalProducts] =
                Schema(
                    CommerceVerticalType.DigitalProducts,
                    Choice("digital-type", "نوع المنتج الرقمي", "ebook", "software", "template", "course", "audio", "image", "license", "other"),
                    Text("file-format", "صيغة الملف"),
                    Decimal("file-size-mb", "حجم الملف MB"),
                    Text("version", "الإصدار"),
                    Choice("license-type", "نوع الترخيص", "personal", "commercial", "subscription", "single-device", "multi-device"),
                    Integer("license-duration-days", "مدة الترخيص بالأيام"),
                    Integer("download-limit", "حد مرات التنزيل"),
                    Text("platform-compatibility", "الأنظمة المتوافقة"),
                    Text("system-requirements", "متطلبات التشغيل"),
                    Text("delivery-notes", "تعليمات التسليم"),
                    Boolean("updates-included", "التحديثات مشمولة")
                ),
            [CommerceVerticalType.JewelryAndWatches] =
                Schema(
                    CommerceVerticalType.JewelryAndWatches,
                    Choice("product-type", "نوع المنتج", "jewelry", "watch"),
                    Text("brand", "العلامة التجارية"),
                    Text("material", "المعدن / الخامة"),
                    Text("karat", "العيار"),
                    Decimal("weight-grams", "الوزن بالجرام"),
                    Text("gemstone", "الحجر"),
                    Decimal("gemstone-carat", "وزن الحجر بالقيراط"),
                    Choice("gender", "الفئة", "unisex", "men", "women"),
                    Text("watch-movement", "حركة الساعة"),
                    Decimal("case-size-mm", "حجم هيكل الساعة MM"),
                    Text("water-resistance", "مقاومة الماء"),
                    Text("certificate-number", "رقم الشهادة"),
                    Integer("warranty-months", "مدة الضمان بالأشهر")
                ),
            [CommerceVerticalType.FurnitureAndDecor] =
                Schema(
                    CommerceVerticalType.FurnitureAndDecor,
                    Text("product-type", "نوع المنتج"),
                    Text("material", "الخامة"),
                    Text("color", "اللون"),
                    Decimal("width-cm", "العرض CM"),
                    Decimal("height-cm", "الارتفاع CM"),
                    Decimal("depth-cm", "العمق CM"),
                    Decimal("weight-kg", "الوزن KG"),
                    Text("room-type", "الغرفة المناسبة"),
                    Text("style", "الستايل"),
                    Boolean("assembly-required", "يحتاج تركيب"),
                    Boolean("installation-included", "التركيب مشمول"),
                    Decimal("package-count", "عدد الطرود"),
                    Text("care-instructions", "تعليمات العناية")
                ),
            [CommerceVerticalType.Cosmetics] =
                Schema(
                    CommerceVerticalType.Cosmetics,
                    Text("brand", "العلامة التجارية"),
                    Text("cosmetics-type", "نوع المنتج"),
                    Text("shade", "الدرجة / Shade"),
                    Choice("skin-type", "نوع البشرة", "all", "dry", "normal", "oily", "combination", "sensitive"),
                    Text("hair-type", "نوع الشعر"),
                    Decimal("volume-ml", "الحجم ML"),
                    Decimal("weight-grams", "الوزن بالجرام"),
                    Text("ingredients", "المكونات"),
                    Text("active-ingredients", "المكونات الفعالة"),
                    Decimal("spf", "SPF"),
                    Boolean("cruelty-free", "غير مجرب على الحيوانات"),
                    Boolean("vegan", "نباتي"),
                    Boolean("fragrance-free", "خالٍ من العطر"),
                    Decimal("expiry-months", "مدة الصلاحية بالأشهر"),
                    Text("usage-instructions", "طريقة الاستخدام")
                ),
            [CommerceVerticalType.EventsAndTickets] =
                Schema(
                    CommerceVerticalType.EventsAndTickets,
                    Text("event-type", "نوع الفعالية"),
                    Text("venue", "المكان"),
                    Text("city", "المدينة"),
                    Text("event-date", "تاريخ الفعالية"),
                    Text("start-time", "وقت البداية"),
                    Text("end-time", "وقت النهاية"),
                    Text("organizer", "المنظم"),
                    Text("age-rating", "الفئة العمرية"),
                    Integer("capacity", "السعة"),
                    Choice("seating-type", "نوع المقاعد", "general", "reserved", "standing"),
                    Choice("entry-type", "طريقة الدخول", "qr", "barcode", "name-list", "manual"),
                    Text("ticket-validity", "صلاحية التذكرة"),
                    Text("terms", "شروط الحضور")
                ),
            [CommerceVerticalType.WholesaleB2B] =
                Schema(
                    CommerceVerticalType.WholesaleB2B,
                    Text("brand", "العلامة التجارية"),
                    Integer("minimum-order-quantity", "الحد الأدنى للطلب"),
                    Text("unit-of-measure", "وحدة البيع"),
                    Integer("units-per-case", "عدد الوحدات في الكرتون"),
                    Decimal("case-weight-kg", "وزن الكرتون KG"),
                    Integer("lead-time-days", "مدة التجهيز بالأيام"),
                    Text("country-of-origin", "بلد المنشأ"),
                    Text("price-tier-notes", "ملاحظات شرائح الأسعار"),
                    Text("tax-code", "الرمز الضريبي"),
                    Integer("pallet-quantity", "عدد الوحدات في البالت"),
                    Boolean("customization-available", "تخصيص للطلبات الكبيرة")
                ),
            [CommerceVerticalType.PersonalizedGifts] =
                Schema(
                    CommerceVerticalType.PersonalizedGifts,
                    Text("gift-type", "نوع الهدية"),
                    Text("material", "الخامة"),
                    Boolean("personalization-enabled", "يدعم التخصيص"),
                    Text("personalization-type", "نوع التخصيص"),
                    Text("personalization-notes", "تعليمات التخصيص"),
                    Integer("max-text-length", "أقصى عدد أحرف"),
                    Boolean("image-upload-supported", "يدعم رفع صورة"),
                    Integer("production-days", "مدة الإنتاج بالأيام"),
                    Text("occasion", "المناسبة"),
                    Boolean("gift-wrap-available", "تغليف هدية")
                ),
            [CommerceVerticalType.EquipmentRental] =
                Schema(
                    CommerceVerticalType.EquipmentRental,
                    Text("equipment-type", "نوع المعدة"),
                    Text("brand", "العلامة التجارية"),
                    Text("model", "الموديل"),
                    Choice("condition", "الحالة", "new", "excellent", "good", "used"),
                    Choice("rental-unit", "وحدة التأجير", "hour", "day", "week", "month"),
                    Integer("minimum-rental-units", "الحد الأدنى لمدة التأجير"),
                    Boolean("deposit-required", "تحتاج وديعة"),
                    Decimal("deposit-amount", "قيمة الوديعة"),
                    Boolean("operator-included", "مشغل متوفر"),
                    Boolean("delivery-available", "التوصيل متوفر"),
                    Text("pickup-location", "موقع الاستلام"),
                    Text("specifications", "المواصفات الفنية"),
                    Text("maintenance-notes", "ملاحظات الصيانة")
                ),
            [CommerceVerticalType.HomeGoods] =
                Schema(
                    CommerceVerticalType.HomeGoods,
                    Text("brand", "العلامة التجارية"),
                    Text("product-type", "نوع المنتج"),
                    Text("room-type", "الغرفة المناسبة"),
                    Text("material", "الخامة"),
                    Text("color", "اللون"),
                    Decimal("width-cm", "العرض CM"),
                    Decimal("height-cm", "الارتفاع CM"),
                    Decimal("depth-cm", "العمق CM"),
                    Decimal("capacity-liter", "السعة باللتر"),
                    Integer("pack-count", "عدد القطع"),
                    Boolean("dishwasher-safe", "آمن لغسالة الصحون"),
                    Boolean("microwave-safe", "آمن للمايكرويف"),
                    Text("care-instructions", "تعليمات العناية")
                )
        };
    }

    private static ProductAttributeSchema Schema(
        CommerceVerticalType verticalType,
        params ProductAttributeDefinition[] attributes) =>
        new(verticalType, attributes);

    private static ProductAttributeDefinition Text(string key, string label) =>
        new(key, label, ProductAttributeValueType.Text);

    private static ProductAttributeDefinition Integer(string key, string label) =>
        new(key, label, ProductAttributeValueType.Integer);

    private static ProductAttributeDefinition Decimal(string key, string label) =>
        new(key, label, ProductAttributeValueType.Decimal);

    private static ProductAttributeDefinition Boolean(string key, string label) =>
        new(key, label, ProductAttributeValueType.Boolean);

    private static ProductAttributeDefinition Choice(
        string key,
        string label,
        params string[] values) =>
        new(key, label, ProductAttributeValueType.Choice, values);
}