import type { ProductVerticalDefinition } from "./types";

// These definitions cover the remaining commerce verticals.
// Store vertical decides the store identity; product vertical decides the product schema.

export const footwearVertical: ProductVerticalDefinition = {
  code: "footwear",
  label: "أحذية",
  badgeLabel: "أحذية",
  description: "مواصفات الأحذية مع المقاسات، الخامة، نوع الاستخدام ونظام القياس.",
  fields: [
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
    },
    {
      key: "footwear-type",
      label: "نوع الحذاء",
      type: "select",
      group: "identity",
      options: [
        { value: "sneakers", label: "سنيكرز" },
        { value: "formal", label: "رسمي" },
        { value: "sandals", label: "صندل" },
        { value: "boots", label: "بوت" },
        { value: "slippers", label: "شبشب" },
        { value: "sports", label: "رياضي" },
        { value: "other", label: "أخرى" },
      ],
    },
    {
      key: "gender",
      label: "الفئة",
      type: "select",
      group: "identity",
      options: [
        { value: "unisex", label: "للجنسين" },
        { value: "men", label: "رجالي" },
        { value: "women", label: "نسائي" },
        { value: "kids", label: "أطفال" },
      ],
    },
    {
      key: "material",
      label: "الخامة",
      type: "text",
      group: "material",
    },
    {
      key: "sole-material",
      label: "خامة النعل",
      type: "text",
      group: "material",
    },
    {
      key: "closure-type",
      label: "نوع الإغلاق",
      type: "select",
      group: "details",
      options: [
        { value: "laces", label: "رباط" },
        { value: "slip-on", label: "بدون رباط" },
        { value: "velcro", label: "لاصق" },
        { value: "zipper", label: "سحاب" },
        { value: "buckle", label: "إبزيم" },
        { value: "other", label: "أخرى" },
      ],
    },
    {
      key: "use-case",
      label: "الاستخدام",
      type: "text",
      group: "details",
    },
    {
      key: "size-system",
      label: "نظام المقاسات",
      type: "select",
      group: "details",
      options: [
        { value: "eu", label: "EU" },
        { value: "us", label: "US" },
        { value: "uk", label: "UK" },
        { value: "cm", label: "CM" },
        { value: "custom", label: "خاص" },
      ],
    },
    {
      key: "width-fit",
      label: "عرض القدم",
      type: "select",
      group: "details",
      options: [
        { value: "narrow", label: "ضيق" },
        { value: "regular", label: "عادي" },
        { value: "wide", label: "عريض" },
      ],
    },
    {
      key: "waterproof",
      label: "مقاوم للماء",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "country-of-origin",
      label: "بلد الصنع",
      type: "text",
      group: "details",
    },
    {
      key: "care-instructions",
      label: "تعليمات العناية",
      type: "textarea",
      group: "care",
    },
  ],
  variantDimensions: [
    {
      key: "color",
      label: "اللون",
    },
    {
      key: "size",
      label: "المقاس",
    },
  ],
};

export const perfumesVertical: ProductVerticalDefinition = {
  code: "perfumes",
  label: "عطور",
  badgeLabel: "عطور",
  description: "مواصفات العطور من التركيز والعائلة العطرية إلى النوتات والحجم.",
  fields: [
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
    },
    {
      key: "perfume-name",
      label: "اسم العطر",
      type: "text",
      group: "identity",
    },
    {
      key: "gender",
      label: "الفئة",
      type: "select",
      group: "identity",
      options: [
        { value: "men", label: "رجالي" },
        { value: "women", label: "نسائي" },
        { value: "unisex", label: "للجنسين" },
      ],
    },
    {
      key: "fragrance-family",
      label: "العائلة العطرية",
      type: "select",
      group: "details",
      options: [
        { value: "fresh", label: "منعش" },
        { value: "floral", label: "زهري" },
        { value: "woody", label: "خشبي" },
        { value: "oriental", label: "شرقي" },
        { value: "citrus", label: "حمضي" },
        { value: "gourmand", label: "غورماند" },
        { value: "leather", label: "جلدي" },
        { value: "aromatic", label: "عطري" },
      ],
    },
    {
      key: "volume-ml",
      label: "الحجم الأساسي ML",
      type: "number",
      group: "details",
    },
    {
      key: "concentration",
      label: "التركيز",
      type: "select",
      group: "details",
      options: [
        { value: "EDC", label: "EDC" },
        { value: "EDT", label: "EDT" },
        { value: "EDP", label: "EDP" },
        { value: "Parfum", label: "Parfum" },
        { value: "Extrait", label: "Extrait" },
      ],
    },
    {
      key: "top-notes",
      label: "النوتات العليا",
      type: "textarea",
      group: "details",
    },
    {
      key: "heart-notes",
      label: "النوتات الوسطى",
      type: "textarea",
      group: "details",
    },
    {
      key: "base-notes",
      label: "النوتات الأساسية",
      type: "textarea",
      group: "details",
    },
    {
      key: "origin-country",
      label: "بلد المنشأ",
      type: "text",
      group: "details",
    },
    {
      key: "season",
      label: "الموسم المناسب",
      type: "text",
      group: "details",
    },
    {
      key: "tester",
      label: "تستر",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "gift-set",
      label: "طقم هدية",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
  ],
  variantDimensions: [
    {
      key: "volume",
      label: "الحجم",
    },
    {
      key: "concentration",
      label: "التركيز",
    },
  ],
};

export const subscriptionsVertical: ProductVerticalDefinition = {
  code: "subscriptions",
  label: "اشتراكات",
  badgeLabel: "اشتراك",
  description: "منتجات اشتراكات بدورات دفع ومدد وصلاحيات قابلة للضبط.",
  fields: [
    {
      key: "subscription-type",
      label: "نوع الاشتراك",
      type: "text",
      group: "identity",
    },
    {
      key: "billing-cycle",
      label: "دورة الدفع",
      type: "select",
      group: "details",
      options: [
        { value: "monthly", label: "شهري" },
        { value: "quarterly", label: "ربع سنوي" },
        { value: "semiannual", label: "نصف سنوي" },
        { value: "yearly", label: "سنوي" },
        { value: "one-time", label: "مرة واحدة" },
      ],
    },
    {
      key: "duration-days",
      label: "مدة الاشتراك بالأيام",
      type: "number",
      group: "details",
    },
    {
      key: "max-users",
      label: "الحد الأقصى للمستخدمين",
      type: "number",
      group: "details",
    },
    {
      key: "max-devices",
      label: "الحد الأقصى للأجهزة",
      type: "number",
      group: "details",
    },
    {
      key: "auto-renewal",
      label: "تجديد تلقائي",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "trial-days",
      label: "مدة التجربة بالأيام",
      type: "number",
      group: "details",
    },
    {
      key: "feature-summary",
      label: "مزايا الاشتراك",
      type: "textarea",
      group: "details",
    },
    {
      key: "entitlement-notes",
      label: "تفاصيل الصلاحيات",
      type: "textarea",
      group: "details",
    },
    {
      key: "cancellation-policy",
      label: "سياسة الإلغاء",
      type: "textarea",
      group: "care",
    },
  ],
  variantDimensions: [
    {
      key: "plan",
      label: "الباقة",
    },
    {
      key: "billing-cycle",
      label: "مدة الاشتراك",
    },
  ],
};

export const servicesVertical: ProductVerticalDefinition = {
  code: "services",
  label: "خدمات",
  badgeLabel: "خدمة",
  description: "خدمات حضورية أو عن بعد مع مدة وتجهيزات وسياسة حجز.",
  fields: [
    {
      key: "service-category",
      label: "نوع الخدمة",
      type: "text",
      group: "identity",
    },
    {
      key: "duration-minutes",
      label: "مدة الخدمة بالدقائق",
      type: "number",
      group: "details",
    },
    {
      key: "service-mode",
      label: "طريقة تقديم الخدمة",
      type: "select",
      group: "details",
      options: [
        { value: "on-site", label: "في مقر الخدمة" },
        { value: "remote", label: "عن بعد" },
        { value: "at-customer", label: "عند العميل" },
      ],
    },
    {
      key: "appointment-required",
      label: "تحتاج موعد",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "lead-time-hours",
      label: "وقت التجهيز بالساعات",
      type: "number",
      group: "details",
    },
    {
      key: "location-notes",
      label: "تفاصيل الموقع",
      type: "textarea",
      group: "details",
    },
    {
      key: "preparation-notes",
      label: "ما يحتاجه العميل قبل الموعد",
      type: "textarea",
      group: "care",
    },
    {
      key: "deliverables",
      label: "ما الذي سيحصل عليه العميل",
      type: "textarea",
      group: "details",
    },
    {
      key: "staff-required",
      label: "عدد مقدمي الخدمة",
      type: "number",
      group: "details",
    },
    {
      key: "cancellation-policy",
      label: "سياسة الإلغاء",
      type: "textarea",
      group: "care",
    },
  ],
  variantDimensions: [
    {
      key: "package",
      label: "الباقة",
    },
    {
      key: "duration",
      label: "المدة",
    },
  ],
};

export const carRentalVertical: ProductVerticalDefinition = {
  code: "car-rental",
  label: "تأجير سيارات",
  badgeLabel: "تأجير",
  description: "مواصفات المركبات وخيارات التأجير والتأمين والوديعة.",
  fields: [
    {
      key: "make",
      label: "الشركة المصنعة",
      type: "text",
      group: "identity",
    },
    {
      key: "model",
      label: "الموديل",
      type: "text",
      group: "identity",
    },
    {
      key: "year",
      label: "سنة الصنع",
      type: "number",
      group: "details",
    },
    {
      key: "vehicle-class",
      label: "فئة السيارة",
      type: "text",
      group: "details",
    },
    {
      key: "transmission",
      label: "ناقل الحركة",
      type: "select",
      group: "details",
      options: [
        { value: "automatic", label: "أوتوماتيك" },
        { value: "manual", label: "عادي" },
      ],
    },
    {
      key: "fuel-type",
      label: "نوع الوقود",
      type: "select",
      group: "details",
      options: [
        { value: "petrol", label: "بنزين" },
        { value: "diesel", label: "ديزل" },
        { value: "hybrid", label: "هايبرد" },
        { value: "electric", label: "كهرباء" },
      ],
    },
    {
      key: "seats",
      label: "عدد المقاعد",
      type: "number",
      group: "details",
    },
    {
      key: "doors",
      label: "عدد الأبواب",
      type: "number",
      group: "details",
    },
    {
      key: "luggage",
      label: "سعة الأمتعة",
      type: "number",
      group: "details",
    },
    {
      key: "daily-mileage-limit",
      label: "حد المسافة اليومي KM",
      type: "number",
      group: "details",
    },
    {
      key: "unlimited-mileage",
      label: "كيلومترات مفتوحة",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "deposit-amount",
      label: "قيمة الوديعة",
      type: "number",
      group: "details",
    },
    {
      key: "insurance-type",
      label: "نوع التأمين",
      type: "text",
      group: "warranty",
    },
    {
      key: "minimum-driver-age",
      label: "الحد الأدنى لعمر السائق",
      type: "number",
      group: "details",
    },
    {
      key: "pickup-location",
      label: "موقع الاستلام",
      type: "text",
      group: "details",
    },
  ],
  variantDimensions: [
    {
      key: "rental-package",
      label: "خطة التأجير",
    },
  ],
};

export const realEstateVertical: ProductVerticalDefinition = {
  code: "real-estate",
  label: "عقارات",
  badgeLabel: "عقار",
  description: "قوائم عقارية للبيع أو الإيجار مع الموقع والمساحة والتجهيزات.",
  fields: [
    {
      key: "listing-purpose",
      label: "الغرض",
      type: "select",
      group: "identity",
      options: [
        { value: "sale", label: "بيع" },
        { value: "rent", label: "إيجار" },
      ],
    },
    {
      key: "property-type",
      label: "نوع العقار",
      type: "select",
      group: "identity",
      options: [
        { value: "apartment", label: "شقة" },
        { value: "villa", label: "فيلا" },
        { value: "office", label: "مكتب" },
        { value: "land", label: "أرض" },
        { value: "shop", label: "محل" },
        { value: "warehouse", label: "مستودع" },
      ],
    },
    {
      key: "city",
      label: "المدينة",
      type: "text",
      group: "details",
    },
    {
      key: "district",
      label: "الحي",
      type: "text",
      group: "details",
    },
    {
      key: "area-sqm",
      label: "المساحة م²",
      type: "number",
      group: "details",
    },
    {
      key: "bedrooms",
      label: "غرف النوم",
      type: "number",
      group: "details",
    },
    {
      key: "bathrooms",
      label: "دورات المياه",
      type: "number",
      group: "details",
    },
    {
      key: "furnished",
      label: "مفروش",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "parking-spaces",
      label: "مواقف السيارات",
      type: "number",
      group: "details",
    },
    {
      key: "property-age-years",
      label: "عمر العقار بالسنوات",
      type: "number",
      group: "details",
    },
    {
      key: "floor-number",
      label: "رقم الطابق",
      type: "number",
      group: "details",
    },
    {
      key: "orientation",
      label: "الواجهة",
      type: "text",
      group: "details",
    },
    {
      key: "latitude",
      label: "خط العرض",
      type: "number",
      group: "details",
    },
    {
      key: "longitude",
      label: "خط الطول",
      type: "number",
      group: "details",
    },
    {
      key: "amenities",
      label: "المرافق والمميزات",
      type: "textarea",
      group: "details",
    },
    {
      key: "available-from",
      label: "متاح من",
      type: "text",
      group: "details",
    },
  ],
  variantDimensions: [
  ],
};

export const restaurantsVertical: ProductVerticalDefinition = {
  code: "restaurants",
  label: "مطاعم",
  badgeLabel: "مطعم",
  description: "أطباق ومشروبات مع المكونات والسعرات والخيارات الغذائية.",
  fields: [
    {
      key: "dish-type",
      label: "نوع الصنف",
      type: "text",
      group: "identity",
    },
    {
      key: "cuisine",
      label: "المطبخ",
      type: "text",
      group: "identity",
    },
    {
      key: "dietary-type",
      label: "النظام الغذائي",
      type: "select",
      group: "details",
      options: [
        { value: "regular", label: "عادي" },
        { value: "vegetarian", label: "نباتي" },
        { value: "vegan", label: "نباتي صرف" },
        { value: "halal", label: "حلال" },
        { value: "gluten-free", label: "خالٍ من الغلوتين" },
      ],
    },
    {
      key: "spicy",
      label: "حار",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "spice-level",
      label: "درجة الحِر",
      type: "select",
      group: "details",
      options: [
        { value: "mild", label: "خفيف" },
        { value: "medium", label: "متوسط" },
        { value: "hot", label: "حار" },
        { value: "extra-hot", label: "حار جدًا" },
      ],
    },
    {
      key: "calories",
      label: "السعرات الحرارية",
      type: "number",
      group: "details",
    },
    {
      key: "preparation-minutes",
      label: "وقت التحضير بالدقائق",
      type: "number",
      group: "details",
    },
    {
      key: "ingredients",
      label: "المكونات",
      type: "textarea",
      group: "material",
    },
    {
      key: "allergens",
      label: "مسببات الحساسية",
      type: "textarea",
      group: "care",
    },
    {
      key: "serving-size",
      label: "حجم الحصة",
      type: "text",
      group: "details",
    },
    {
      key: "availability-window",
      label: "أوقات التوفر",
      type: "text",
      group: "details",
    },
  ],
  variantDimensions: [
    {
      key: "size",
      label: "الحجم",
    },
  ],
};

export const deliveryMarketplaceVertical: ProductVerticalDefinition = {
  code: "delivery-marketplace",
  label: "منصة توصيل",
  badgeLabel: "توصيل",
  description: "خدمات توصيل مع مناطق وتكلفة ومدة تقديرية ونوع مركبة.",
  fields: [
    {
      key: "service-type",
      label: "نوع خدمة التوصيل",
      type: "text",
      group: "identity",
    },
    {
      key: "pickup-area",
      label: "منطقة الاستلام",
      type: "text",
      group: "details",
    },
    {
      key: "delivery-area",
      label: "منطقة التوصيل",
      type: "text",
      group: "details",
    },
    {
      key: "estimated-minutes",
      label: "المدة التقديرية بالدقائق",
      type: "number",
      group: "details",
    },
    {
      key: "max-distance-km",
      label: "أقصى مسافة KM",
      type: "number",
      group: "details",
    },
    {
      key: "vehicle-type",
      label: "نوع المركبة",
      type: "select",
      group: "details",
      options: [
        { value: "bike", label: "دراجة" },
        { value: "motorcycle", label: "دراجة نارية" },
        { value: "car", label: "سيارة" },
        { value: "van", label: "فان" },
        { value: "truck", label: "شاحنة" },
      ],
    },
    {
      key: "base-fee",
      label: "الرسوم الأساسية",
      type: "number",
      group: "details",
    },
    {
      key: "fee-per-km",
      label: "رسوم الكيلومتر",
      type: "number",
      group: "details",
    },
    {
      key: "cash-on-delivery",
      label: "دفع عند الاستلام",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "same-day",
      label: "توصيل بنفس اليوم",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "service-notes",
      label: "ملاحظات الخدمة",
      type: "textarea",
      group: "details",
    },
  ],
  variantDimensions: [
    {
      key: "service-level",
      label: "مستوى الخدمة",
    },
  ],
};

export const groceryVertical: ProductVerticalDefinition = {
  code: "grocery",
  label: "بقالة ومواد غذائية",
  badgeLabel: "بقالة",
  description: "منتجات غذائية مع الوزن والمنشأ والمكونات والتخزين والصلاحية.",
  fields: [
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
    },
    {
      key: "product-type",
      label: "نوع المنتج",
      type: "text",
      group: "identity",
    },
    {
      key: "weight-grams",
      label: "الوزن بالجرام",
      type: "number",
      group: "details",
    },
    {
      key: "weight-unit",
      label: "وحدة القياس",
      type: "select",
      group: "details",
      options: [
        { value: "g", label: "جرام" },
        { value: "kg", label: "كيلوجرام" },
        { value: "ml", label: "مل" },
        { value: "l", label: "لتر" },
        { value: "piece", label: "قطعة" },
      ],
    },
    {
      key: "origin-country",
      label: "بلد المنشأ",
      type: "text",
      group: "details",
    },
    {
      key: "ingredients",
      label: "المكونات",
      type: "textarea",
      group: "material",
    },
    {
      key: "allergens",
      label: "مسببات الحساسية",
      type: "textarea",
      group: "care",
    },
    {
      key: "storage-instructions",
      label: "تعليمات التخزين",
      type: "textarea",
      group: "care",
    },
    {
      key: "organic",
      label: "عضوي",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "halal-certified",
      label: "شهادة حلال",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "expiry-days",
      label: "مدة الصلاحية بالأيام",
      type: "number",
      group: "details",
    },
    {
      key: "pack-count",
      label: "عدد القطع في العبوة",
      type: "number",
      group: "details",
    },
    {
      key: "barcode",
      label: "الباركود",
      type: "text",
      group: "identity",
    },
  ],
  variantDimensions: [
    {
      key: "pack-size",
      label: "حجم العبوة",
    },
    {
      key: "flavor",
      label: "النكهة",
    },
  ],
};

export const automotivePartsVertical: ProductVerticalDefinition = {
  code: "automotive-parts",
  label: "قطع سيارات",
  badgeLabel: "قطع سيارات",
  description: "قطع غيار مع رقم القطعة والتوافق مع الماركة والموديل والسنة.",
  fields: [
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
    },
    {
      key: "part-number",
      label: "رقم القطعة",
      type: "text",
      group: "identity",
    },
    {
      key: "oem-number",
      label: "رقم OEM",
      type: "text",
      group: "identity",
    },
    {
      key: "part-category",
      label: "فئة القطعة",
      type: "text",
      group: "identity",
    },
    {
      key: "condition",
      label: "الحالة",
      type: "select",
      group: "details",
      options: [
        { value: "new", label: "جديد" },
        { value: "used", label: "مستعمل" },
        { value: "refurbished", label: "مجدد" },
      ],
    },
    {
      key: "compatible-make",
      label: "الشركة المتوافقة",
      type: "text",
      group: "compatibility",
    },
    {
      key: "compatible-model",
      label: "الموديل المتوافق",
      type: "text",
      group: "compatibility",
    },
    {
      key: "year-from",
      label: "من سنة",
      type: "number",
      group: "compatibility",
    },
    {
      key: "year-to",
      label: "إلى سنة",
      type: "number",
      group: "compatibility",
    },
    {
      key: "engine",
      label: "المحرك المتوافق",
      type: "text",
      group: "compatibility",
    },
    {
      key: "position",
      label: "موضع القطعة",
      type: "text",
      group: "details",
    },
    {
      key: "original-equipment",
      label: "أصلي OEM",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "warranty-months",
      label: "مدة الضمان بالأشهر",
      type: "number",
      group: "warranty",
    },
  ],
  variantDimensions: [
    {
      key: "quality",
      label: "الفئة / الجودة",
    },
  ],
};

export const digitalProductsVertical: ProductVerticalDefinition = {
  code: "digital-products",
  label: "منتجات رقمية",
  badgeLabel: "رقمي",
  description: "ملفات وتراخيص رقمية مع الصيغة والإصدار وشروط التنزيل.",
  fields: [
    {
      key: "digital-type",
      label: "نوع المنتج الرقمي",
      type: "select",
      group: "identity",
      options: [
        { value: "ebook", label: "كتاب إلكتروني" },
        { value: "software", label: "برنامج" },
        { value: "template", label: "قالب" },
        { value: "course", label: "دورة" },
        { value: "audio", label: "صوت" },
        { value: "image", label: "صورة" },
        { value: "license", label: "ترخيص" },
        { value: "other", label: "أخرى" },
      ],
    },
    {
      key: "file-format",
      label: "صيغة الملف",
      type: "text",
      group: "details",
    },
    {
      key: "file-size-mb",
      label: "حجم الملف MB",
      type: "number",
      group: "details",
    },
    {
      key: "version",
      label: "الإصدار",
      type: "text",
      group: "details",
    },
    {
      key: "license-type",
      label: "نوع الترخيص",
      type: "select",
      group: "details",
      options: [
        { value: "personal", label: "شخصي" },
        { value: "commercial", label: "تجاري" },
        { value: "subscription", label: "اشتراك" },
        { value: "single-device", label: "جهاز واحد" },
        { value: "multi-device", label: "عدة أجهزة" },
      ],
    },
    {
      key: "license-duration-days",
      label: "مدة الترخيص بالأيام",
      type: "number",
      group: "details",
    },
    {
      key: "download-limit",
      label: "حد مرات التنزيل",
      type: "number",
      group: "details",
    },
    {
      key: "platform-compatibility",
      label: "الأنظمة المتوافقة",
      type: "textarea",
      group: "compatibility",
    },
    {
      key: "system-requirements",
      label: "متطلبات التشغيل",
      type: "textarea",
      group: "compatibility",
    },
    {
      key: "delivery-notes",
      label: "تعليمات التسليم",
      type: "textarea",
      group: "details",
    },
    {
      key: "updates-included",
      label: "التحديثات مشمولة",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
  ],
  variantDimensions: [
    {
      key: "license",
      label: "نوع الترخيص",
    },
  ],
};

export const jewelryWatchesVertical: ProductVerticalDefinition = {
  code: "jewelry-watches",
  label: "مجوهرات وساعات",
  badgeLabel: "مجوهرات",
  description: "مجوهرات وساعات مع المعدن والعيار والحجر والمواصفات الفنية.",
  fields: [
    {
      key: "product-type",
      label: "نوع المنتج",
      type: "select",
      group: "identity",
      options: [
        { value: "jewelry", label: "مجوهرات" },
        { value: "watch", label: "ساعة" },
      ],
    },
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
    },
    {
      key: "material",
      label: "المعدن / الخامة",
      type: "text",
      group: "material",
    },
    {
      key: "karat",
      label: "العيار",
      type: "text",
      group: "material",
    },
    {
      key: "weight-grams",
      label: "الوزن بالجرام",
      type: "number",
      group: "details",
    },
    {
      key: "gemstone",
      label: "الحجر",
      type: "text",
      group: "material",
    },
    {
      key: "gemstone-carat",
      label: "وزن الحجر بالقيراط",
      type: "number",
      group: "details",
    },
    {
      key: "gender",
      label: "الفئة",
      type: "select",
      group: "identity",
      options: [
        { value: "unisex", label: "للجنسين" },
        { value: "men", label: "رجالي" },
        { value: "women", label: "نسائي" },
      ],
    },
    {
      key: "watch-movement",
      label: "حركة الساعة",
      type: "text",
      group: "performance",
    },
    {
      key: "case-size-mm",
      label: "حجم هيكل الساعة MM",
      type: "number",
      group: "details",
    },
    {
      key: "water-resistance",
      label: "مقاومة الماء",
      type: "text",
      group: "details",
    },
    {
      key: "certificate-number",
      label: "رقم الشهادة",
      type: "text",
      group: "warranty",
    },
    {
      key: "warranty-months",
      label: "مدة الضمان بالأشهر",
      type: "number",
      group: "warranty",
    },
  ],
  variantDimensions: [
    {
      key: "size",
      label: "المقاس",
    },
    {
      key: "material",
      label: "الخامة",
    },
  ],
};

export const furnitureDecorVertical: ProductVerticalDefinition = {
  code: "furniture-decor",
  label: "أثاث وديكور",
  badgeLabel: "أثاث",
  description: "أثاث وديكور مع الأبعاد والخامة والتركيب والغرفة المناسبة.",
  fields: [
    {
      key: "product-type",
      label: "نوع المنتج",
      type: "text",
      group: "identity",
    },
    {
      key: "material",
      label: "الخامة",
      type: "text",
      group: "material",
    },
    {
      key: "color",
      label: "اللون",
      type: "text",
      group: "design",
    },
    {
      key: "width-cm",
      label: "العرض CM",
      type: "number",
      group: "details",
    },
    {
      key: "height-cm",
      label: "الارتفاع CM",
      type: "number",
      group: "details",
    },
    {
      key: "depth-cm",
      label: "العمق CM",
      type: "number",
      group: "details",
    },
    {
      key: "weight-kg",
      label: "الوزن KG",
      type: "number",
      group: "details",
    },
    {
      key: "room-type",
      label: "الغرفة المناسبة",
      type: "text",
      group: "details",
    },
    {
      key: "style",
      label: "الستايل",
      type: "text",
      group: "design",
    },
    {
      key: "assembly-required",
      label: "يحتاج تركيب",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "installation-included",
      label: "التركيب مشمول",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "package-count",
      label: "عدد الطرود",
      type: "number",
      group: "details",
    },
    {
      key: "care-instructions",
      label: "تعليمات العناية",
      type: "textarea",
      group: "care",
    },
  ],
  variantDimensions: [
    {
      key: "color",
      label: "اللون",
    },
    {
      key: "size",
      label: "المقاس",
    },
    {
      key: "material",
      label: "الخامة",
    },
  ],
};

export const cosmeticsVertical: ProductVerticalDefinition = {
  code: "cosmetics",
  label: "تجميل وعناية",
  badgeLabel: "تجميل",
  description: "منتجات تجميل وعناية بدرجات وأحجام ومكونات وخصائص البشرة والشعر.",
  fields: [
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
    },
    {
      key: "cosmetics-type",
      label: "نوع المنتج",
      type: "text",
      group: "identity",
    },
    {
      key: "shade",
      label: "الدرجة / Shade",
      type: "text",
      group: "design",
    },
    {
      key: "skin-type",
      label: "نوع البشرة",
      type: "select",
      group: "details",
      options: [
        { value: "all", label: "كل الأنواع" },
        { value: "dry", label: "جافة" },
        { value: "normal", label: "عادية" },
        { value: "oily", label: "دهنية" },
        { value: "combination", label: "مختلطة" },
        { value: "sensitive", label: "حساسة" },
      ],
    },
    {
      key: "hair-type",
      label: "نوع الشعر",
      type: "text",
      group: "details",
    },
    {
      key: "volume-ml",
      label: "الحجم ML",
      type: "number",
      group: "details",
    },
    {
      key: "weight-grams",
      label: "الوزن بالجرام",
      type: "number",
      group: "details",
    },
    {
      key: "ingredients",
      label: "المكونات",
      type: "textarea",
      group: "material",
    },
    {
      key: "active-ingredients",
      label: "المكونات الفعالة",
      type: "textarea",
      group: "material",
    },
    {
      key: "spf",
      label: "SPF",
      type: "number",
      group: "details",
    },
    {
      key: "cruelty-free",
      label: "غير مجرب على الحيوانات",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "vegan",
      label: "نباتي",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "fragrance-free",
      label: "خالٍ من العطر",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "expiry-months",
      label: "مدة الصلاحية بالأشهر",
      type: "number",
      group: "details",
    },
    {
      key: "usage-instructions",
      label: "طريقة الاستخدام",
      type: "textarea",
      group: "care",
    },
  ],
  variantDimensions: [
    {
      key: "shade",
      label: "الدرجة",
    },
    {
      key: "size",
      label: "الحجم",
    },
  ],
};

export const eventsTicketsVertical: ProductVerticalDefinition = {
  code: "events-tickets",
  label: "فعاليات وتذاكر",
  badgeLabel: "تذاكر",
  description: "فعاليات وتذاكر مع المكان والموعد والسعة وفئات الدخول.",
  fields: [
    {
      key: "event-type",
      label: "نوع الفعالية",
      type: "text",
      group: "identity",
    },
    {
      key: "venue",
      label: "المكان",
      type: "text",
      group: "details",
    },
    {
      key: "city",
      label: "المدينة",
      type: "text",
      group: "details",
    },
    {
      key: "event-date",
      label: "تاريخ الفعالية",
      type: "text",
      group: "details",
    },
    {
      key: "start-time",
      label: "وقت البداية",
      type: "text",
      group: "details",
    },
    {
      key: "end-time",
      label: "وقت النهاية",
      type: "text",
      group: "details",
    },
    {
      key: "organizer",
      label: "المنظم",
      type: "text",
      group: "identity",
    },
    {
      key: "age-rating",
      label: "الفئة العمرية",
      type: "text",
      group: "details",
    },
    {
      key: "capacity",
      label: "السعة",
      type: "number",
      group: "details",
    },
    {
      key: "seating-type",
      label: "نوع المقاعد",
      type: "select",
      group: "details",
      options: [
        { value: "general", label: "دخول عام" },
        { value: "reserved", label: "مقاعد محجوزة" },
        { value: "standing", label: "وقوف" },
      ],
    },
    {
      key: "entry-type",
      label: "طريقة الدخول",
      type: "select",
      group: "details",
      options: [
        { value: "qr", label: "QR" },
        { value: "barcode", label: "Barcode" },
        { value: "name-list", label: "قائمة أسماء" },
        { value: "manual", label: "يدوي" },
      ],
    },
    {
      key: "ticket-validity",
      label: "صلاحية التذكرة",
      type: "text",
      group: "details",
    },
    {
      key: "terms",
      label: "شروط الحضور",
      type: "textarea",
      group: "care",
    },
  ],
  variantDimensions: [
    {
      key: "ticket-tier",
      label: "فئة التذكرة",
    },
    {
      key: "seat-zone",
      label: "منطقة الجلوس",
    },
  ],
};

export const wholesaleB2BVertical: ProductVerticalDefinition = {
  code: "wholesale-b2b",
  label: "جملة وشركات B2B",
  badgeLabel: "B2B",
  description: "بيع بالجملة مع حد أدنى وكميات الكراتين وأسعار الشرائح والتجهيز.",
  fields: [
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
    },
    {
      key: "minimum-order-quantity",
      label: "الحد الأدنى للطلب",
      type: "number",
      group: "details",
    },
    {
      key: "unit-of-measure",
      label: "وحدة البيع",
      type: "text",
      group: "details",
    },
    {
      key: "units-per-case",
      label: "عدد الوحدات في الكرتون",
      type: "number",
      group: "details",
    },
    {
      key: "case-weight-kg",
      label: "وزن الكرتون KG",
      type: "number",
      group: "details",
    },
    {
      key: "lead-time-days",
      label: "مدة التجهيز بالأيام",
      type: "number",
      group: "details",
    },
    {
      key: "country-of-origin",
      label: "بلد المنشأ",
      type: "text",
      group: "details",
    },
    {
      key: "price-tier-notes",
      label: "ملاحظات شرائح الأسعار",
      type: "textarea",
      group: "details",
    },
    {
      key: "tax-code",
      label: "الرمز الضريبي",
      type: "text",
      group: "details",
    },
    {
      key: "pallet-quantity",
      label: "عدد الوحدات في البالت",
      type: "number",
      group: "details",
    },
    {
      key: "customization-available",
      label: "تخصيص للطلبات الكبيرة",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
  ],
  variantDimensions: [
    {
      key: "pack-size",
      label: "حجم العبوة",
    },
  ],
};

export const personalizedGiftsVertical: ProductVerticalDefinition = {
  code: "personalized-gifts",
  label: "هدايا مخصصة",
  badgeLabel: "هدايا",
  description: "هدايا قابلة للتخصيص بالنص أو الصورة مع مدة إنتاج وخيارات تغليف.",
  fields: [
    {
      key: "gift-type",
      label: "نوع الهدية",
      type: "text",
      group: "identity",
    },
    {
      key: "material",
      label: "الخامة",
      type: "text",
      group: "material",
    },
    {
      key: "personalization-enabled",
      label: "يدعم التخصيص",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "personalization-type",
      label: "نوع التخصيص",
      type: "text",
      group: "details",
    },
    {
      key: "personalization-notes",
      label: "تعليمات التخصيص",
      type: "textarea",
      group: "details",
    },
    {
      key: "max-text-length",
      label: "أقصى عدد أحرف",
      type: "number",
      group: "details",
    },
    {
      key: "image-upload-supported",
      label: "يدعم رفع صورة",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "production-days",
      label: "مدة الإنتاج بالأيام",
      type: "number",
      group: "details",
    },
    {
      key: "occasion",
      label: "المناسبة",
      type: "text",
      group: "details",
    },
    {
      key: "gift-wrap-available",
      label: "تغليف هدية",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
  ],
  variantDimensions: [
    {
      key: "color",
      label: "اللون",
    },
    {
      key: "size",
      label: "المقاس",
    },
  ],
};

export const equipmentRentalVertical: ProductVerticalDefinition = {
  code: "equipment-rental",
  label: "تأجير معدات",
  badgeLabel: "معدات",
  description: "معدات للإيجار مع وحدة التأجير والوديعة والتوصيل والصيانة.",
  fields: [
    {
      key: "equipment-type",
      label: "نوع المعدة",
      type: "text",
      group: "identity",
    },
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
    },
    {
      key: "model",
      label: "الموديل",
      type: "text",
      group: "identity",
    },
    {
      key: "condition",
      label: "الحالة",
      type: "select",
      group: "details",
      options: [
        { value: "new", label: "جديدة" },
        { value: "excellent", label: "ممتازة" },
        { value: "good", label: "جيدة" },
        { value: "used", label: "مستخدمة" },
      ],
    },
    {
      key: "rental-unit",
      label: "وحدة التأجير",
      type: "select",
      group: "details",
      options: [
        { value: "hour", label: "ساعة" },
        { value: "day", label: "يوم" },
        { value: "week", label: "أسبوع" },
        { value: "month", label: "شهر" },
      ],
    },
    {
      key: "minimum-rental-units",
      label: "الحد الأدنى لمدة التأجير",
      type: "number",
      group: "details",
    },
    {
      key: "deposit-required",
      label: "تحتاج وديعة",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "deposit-amount",
      label: "قيمة الوديعة",
      type: "number",
      group: "details",
    },
    {
      key: "operator-included",
      label: "مشغل متوفر",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "delivery-available",
      label: "التوصيل متوفر",
      type: "select",
      group: "details",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "pickup-location",
      label: "موقع الاستلام",
      type: "text",
      group: "details",
    },
    {
      key: "specifications",
      label: "المواصفات الفنية",
      type: "textarea",
      group: "performance",
    },
    {
      key: "maintenance-notes",
      label: "ملاحظات الصيانة",
      type: "textarea",
      group: "care",
    },
  ],
  variantDimensions: [
    {
      key: "rental-package",
      label: "خطة التأجير",
    },
  ],
};

export const homeGoodsVertical: ProductVerticalDefinition = {
  code: "home-goods",
  label: "مستلزمات منزلية",
  badgeLabel: "منزل",
  description: "مستلزمات منزلية مع الخامة والأبعاد والسعة وخيارات الاستخدام.",
  fields: [
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
    },
    {
      key: "product-type",
      label: "نوع المنتج",
      type: "text",
      group: "identity",
    },
    {
      key: "room-type",
      label: "الغرفة المناسبة",
      type: "text",
      group: "details",
    },
    {
      key: "material",
      label: "الخامة",
      type: "text",
      group: "material",
    },
    {
      key: "color",
      label: "اللون",
      type: "text",
      group: "design",
    },
    {
      key: "width-cm",
      label: "العرض CM",
      type: "number",
      group: "details",
    },
    {
      key: "height-cm",
      label: "الارتفاع CM",
      type: "number",
      group: "details",
    },
    {
      key: "depth-cm",
      label: "العمق CM",
      type: "number",
      group: "details",
    },
    {
      key: "capacity-liter",
      label: "السعة باللتر",
      type: "number",
      group: "details",
    },
    {
      key: "pack-count",
      label: "عدد القطع",
      type: "number",
      group: "details",
    },
    {
      key: "dishwasher-safe",
      label: "آمن لغسالة الصحون",
      type: "select",
      group: "care",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "microwave-safe",
      label: "آمن للمايكرويف",
      type: "select",
      group: "care",
      options: [
        { value: "true", label: "نعم" },
        { value: "false", label: "لا" },
      ],
    },
    {
      key: "care-instructions",
      label: "تعليمات العناية",
      type: "textarea",
      group: "care",
    },
  ],
  variantDimensions: [
    {
      key: "color",
      label: "اللون",
    },
    {
      key: "size",
      label: "المقاس",
    },
  ],
};

export const specializedVerticals: readonly ProductVerticalDefinition[] = [
  footwearVertical,
  perfumesVertical,
  subscriptionsVertical,
  servicesVertical,
  carRentalVertical,
  realEstateVertical,
  restaurantsVertical,
  deliveryMarketplaceVertical,
  groceryVertical,
  automotivePartsVertical,
  digitalProductsVertical,
  jewelryWatchesVertical,
  furnitureDecorVertical,
  cosmeticsVertical,
  eventsTicketsVertical,
  wholesaleB2BVertical,
  personalizedGiftsVertical,
  equipmentRentalVertical,
  homeGoodsVertical,
];
