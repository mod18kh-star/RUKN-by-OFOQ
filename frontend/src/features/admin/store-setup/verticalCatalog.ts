export interface VerticalOption {
  type: string;
  code: string;
  label: string;
  description: string;
}

export const VERTICAL_OPTIONS:
  VerticalOption[] = [
    {
      type: "GeneralRetail",
      code: "general-retail",
      label: "متجر عام",
      description:
        "منتجات متنوعة ومتجر مرن يناسب أغلب الأنشطة.",
    },
    {
      type: "Apparel",
      code: "apparel",
      label: "أزياء وملابس",
      description:
        "ملابس ومقاسات وألوان ومجموعات موسمية.",
    },
    {
      type: "Footwear",
      code: "footwear",
      label: "أحذية",
      description:
        "أحذية ومقاسات وخيارات متعددة للمنتج.",
    },
    {
      type: "MobilePhones",
      code: "mobile-phones",
      label: "هواتف وجوالات",
      description:
        "هواتف وملحقات ومواصفات تقنية.",
    },
    {
      type: "Perfumes",
      code: "perfumes",
      label: "عطور",
      description:
        "عطور وروائح وأحجام ومجموعات.",
    },
    {
      type: "Electronics",
      code: "electronics",
      label: "إلكترونيات",
      description:
        "أجهزة وإكسسوارات ومواصفات وضمان.",
    },
    {
      type: "Subscriptions",
      code: "subscriptions",
      label: "اشتراكات",
      description:
        "خدمات أو منتجات بدفع متكرر.",
    },
    {
      type: "Services",
      code: "services",
      label: "خدمات",
      description:
        "بيع خدمات ومواعيد وخيارات مخصصة.",
    },
    {
      type: "CarRental",
      code: "car-rental",
      label: "تأجير سيارات",
      description:
        "حجوزات ومركبات ومدد تأجير.",
    },
    {
      type: "RealEstate",
      code: "real-estate",
      label: "عقارات",
      description:
        "عروض عقارية وخصائص وتفاصيل الوحدات.",
    },
    {
      type: "Restaurants",
      code: "restaurants",
      label: "مطاعم",
      description:
        "قائمة طعام وإضافات وطلبات.",
    },
    {
      type: "DeliveryMarketplace",
      code: "delivery-marketplace",
      label: "منصة توصيل",
      description:
        "متاجر متعددة وطلبات وتوصيل.",
    },
    {
      type: "Grocery",
      code: "grocery",
      label: "بقالة ومواد غذائية",
      description:
        "منتجات استهلاكية ومخزون سريع الدوران.",
    },
    {
      type: "AutomotiveParts",
      code: "automotive-parts",
      label: "قطع سيارات",
      description:
        "قطع غيار وتوافق ومواصفات المركبات.",
    },
    {
      type: "DigitalProducts",
      code: "digital-products",
      label: "منتجات رقمية",
      description:
        "ملفات ومحتوى ومنتجات غير مادية.",
    },
    {
      type: "JewelryAndWatches",
      code: "jewelry-watches",
      label: "مجوهرات وساعات",
      description:
        "قطع فاخرة وخيارات وضمان.",
    },
    {
      type: "FurnitureAndDecor",
      code: "furniture-decor",
      label: "أثاث وديكور",
      description:
        "أثاث وديكور وخيارات وتخصيص.",
    },
    {
      type: "Cosmetics",
      code: "cosmetics",
      label: "تجميل وعناية",
      description:
        "مستحضرات تجميل وعناية وتشغيلات وصلاحية.",
    },
    {
      type: "EventsAndTickets",
      code: "events-tickets",
      label: "فعاليات وتذاكر",
      description:
        "حجوزات ومقاعد وتذاكر QR.",
    },
    {
      type: "WholesaleB2B",
      code: "wholesale-b2b",
      label: "جملة وشركات",
      description:
        "أسعار جملة وكميات وشروط تجارية.",
    },
    {
      type: "PersonalizedGifts",
      code: "personalized-gifts",
      label: "هدايا مخصصة",
      description:
        "هدايا وطباعة وتخصيص حسب الطلب.",
    },
    {
      type: "EquipmentRental",
      code: "equipment-rental",
      label: "تأجير معدات",
      description:
        "معدات وحجوزات وصيانة وتقويم توفر.",
    },
    {
      type: "HomeGoods",
      code: "home-goods",
      label: "مستلزمات منزلية",
      description:
        "منتجات منزلية ومخزون وخيارات متعددة.",
    },
  ];

export function findVertical(
  type?: string,
) {
  return VERTICAL_OPTIONS.find(
    (item) =>
      item.type ===
      type,
  );
}