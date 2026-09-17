import type {
  FontId,
  HeroLayout,
  ProductCardStyle,
  ThemeId,
} from "../../storefront/theme/theme.types";

export interface OnboardingThemeStyle {
  id: string;
  name: string;
  description: string;
  themeId: ThemeId;
  fontId: FontId;
  productCardStyle: ProductCardStyle;
  heroLayout: HeroLayout;
  swatches: [string, string, string];
}

const STYLES:
  Record<string, OnboardingThemeStyle> = {
    "editorial-air": {
      id: "editorial-air",
      name: "Editorial Air",
      description:
        "صور كبيرة، مساحات واسعة، وبطاقات هادئة للبراندات البصرية.",
      themeId: "editorial",
      fontId: "tajawal",
      productCardStyle: "editorial",
      heroLayout: "wide-portrait",
      swatches: ["#f6f5f1", "#27463d", "#191916"],
    },
    "maison-luxe": {
      id: "maison-luxe",
      name: "Maison Luxe",
      description:
        "فخامة منخفضة الضجيج، تفاصيل دقيقة، وتكوين أقرب للمجلات.",
      themeId: "maison",
      fontId: "readex",
      productCardStyle: "minimal",
      heroLayout: "equal",
      swatches: ["#f4f0e8", "#6b4e38", "#211d18"],
    },
    "commerce-clear": {
      id: "commerce-clear",
      name: "Commerce Clear",
      description:
        "بيع مباشر، شبكة منتجات واضحة، ومسار سريع للوصول للمنتج.",
      themeId: "commerce",
      fontId: "plex",
      productCardStyle: "commerce",
      heroLayout: "single",
      swatches: ["#f7f7f5", "#185744", "#171817"],
    },
    "studio-soft": {
      id: "studio-soft",
      name: "Studio Soft",
      description:
        "بطاقات مرنة، زوايا أكثر نعومة، وترتيب مناسب للقصص والمجموعات.",
      themeId: "studio",
      fontId: "cairo",
      productCardStyle: "minimal",
      heroLayout: "focus-two",
      swatches: ["#f2f2ee", "#44594b", "#171917"],
    },
    "technical-grid": {
      id: "technical-grid",
      name: "Technical Grid",
      description:
        "معلومات أكثر، شبكة منظمة، وبطاقات مناسبة للمواصفات والمقارنات.",
      themeId: "technical",
      fontId: "plex",
      productCardStyle: "technical",
      heroLayout: "focus-one",
      swatches: ["#f3f6f6", "#185563", "#162021"],
    },
  };

const DEFAULT_IDS = [
  "commerce-clear",
  "editorial-air",
  "studio-soft",
];

const BY_VERTICAL:
  Record<string, string[]> = {
    GeneralRetail: [
      "commerce-clear",
      "editorial-air",
      "studio-soft",
      "technical-grid",
    ],
    Apparel: [
      "editorial-air",
      "maison-luxe",
      "studio-soft",
      "commerce-clear",
    ],
    Footwear: [
      "editorial-air",
      "commerce-clear",
      "technical-grid",
      "studio-soft",
      "maison-luxe",
    ],
    MobilePhones: [
      "technical-grid",
      "commerce-clear",
      "studio-soft",
    ],
    Perfumes: [
      "maison-luxe",
      "editorial-air",
      "studio-soft",
    ],
    Electronics: [
      "technical-grid",
      "commerce-clear",
      "editorial-air",
    ],
    Subscriptions: [
      "commerce-clear",
      "studio-soft",
      "technical-grid",
    ],
    Services: [
      "studio-soft",
      "editorial-air",
      "commerce-clear",
    ],
    CarRental: [
      "technical-grid",
      "editorial-air",
      "commerce-clear",
    ],
    RealEstate: [
      "editorial-air",
      "maison-luxe",
      "studio-soft",
    ],
    Restaurants: [
      "studio-soft",
      "editorial-air",
      "commerce-clear",
      "maison-luxe",
    ],
    DeliveryMarketplace: [
      "commerce-clear",
      "technical-grid",
      "studio-soft",
    ],
    Grocery: [
      "commerce-clear",
      "studio-soft",
      "technical-grid",
    ],
    AutomotiveParts: [
      "technical-grid",
      "commerce-clear",
      "editorial-air",
    ],
    DigitalProducts: [
      "technical-grid",
      "studio-soft",
      "commerce-clear",
    ],
    JewelryAndWatches: [
      "maison-luxe",
      "editorial-air",
      "studio-soft",
    ],
    FurnitureAndDecor: [
      "studio-soft",
      "editorial-air",
      "maison-luxe",
      "commerce-clear",
    ],
    Cosmetics: [
      "editorial-air",
      "maison-luxe",
      "studio-soft",
    ],
    EventsAndTickets: [
      "studio-soft",
      "technical-grid",
      "editorial-air",
    ],
    WholesaleB2B: [
      "commerce-clear",
      "technical-grid",
      "editorial-air",
    ],
    PersonalizedGifts: [
      "studio-soft",
      "editorial-air",
      "maison-luxe",
    ],
    EquipmentRental: [
      "technical-grid",
      "commerce-clear",
      "studio-soft",
    ],
    HomeGoods: [
      "studio-soft",
      "editorial-air",
      "commerce-clear",
    ],
  };

export function getThemeStylesForVertical(
  verticalType: string,
) {
  const ids =
    BY_VERTICAL[verticalType] ??
    DEFAULT_IDS;

  return ids
    .map((id) => STYLES[id])
    .filter(
      (style): style is OnboardingThemeStyle =>
        Boolean(style),
    );
}

export function getThemeStyleById(
  id: string,
) {
  return STYLES[id] ?? STYLES["commerce-clear"];
}
