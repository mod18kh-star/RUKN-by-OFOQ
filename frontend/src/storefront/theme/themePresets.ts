import type {
  FontId,
  ThemeExperience,
  ThemeId,
} from "./theme.types";

export type HeaderStyle =
  | "editorial"
  | "centered"
  | "catalog"
  | "studio"
  | "technical"
  | "mobile-flagship"
  | "mobile-market"
  | "vertical-signature"
  | "vertical-market";

export type SectionRhythm =
  | "airy"
  | "luxury"
  | "dense"
  | "creative"
  | "structured"
  | "showcase"
  | "market";

export type ThemePlanBadge =
  | "Business"
  | "Pro";

export interface ThemePreset {
  id: ThemeId;
  name: string;
  description: string;

  canvas: string;
  surface: string;
  soft: string;

  ink: string;
  inkSoft: string;
  muted: string;

  accent: string;

  radius: string;
  contentWidth: string;
  imageRatio: string;

  headerStyle: HeaderStyle;
  rhythm: SectionRhythm;

  defaultFontId?: FontId;
  verticalCodes?: string[];
  planBadge?: ThemePlanBadge;

  experience?: ThemeExperience;
  verticalLabel?: string;
  productLabel?: string;
  productSingularLabel?: string;
  categoryLabel?: string;
  searchPlaceholder?: string;
  defaultHeroTitle?: string;
  defaultHeroDescription?: string;
  featureLabels?: [string, string, string] | string[];
}

const CORE_THEME_PRESETS: ThemePreset[] = [
  {
    id: "editorial",
    name: "Editorial",
    description:
      "صور قوية ومساحات هادئة للأزياء والبراندات الحديثة.",

    canvas: "#f6f5f1",
    surface: "#ffffff",
    soft: "#ebe8df",

    ink: "#191916",
    inkSoft: "#5d5c56",
    muted: "#89877f",

    accent: "#27463d",

    radius: "2px",
    contentWidth: "1440px",
    imageRatio: "4 / 5",

    headerStyle: "editorial",
    rhythm: "airy",
    defaultFontId: "plex",
  },

  {
    id: "maison",
    name: "Maison",
    description:
      "فخامة هادئة للعطور والمجوهرات والساعات.",

    canvas: "#f4f0e8",
    surface: "#fbf8f2",
    soft: "#e6ded1",

    ink: "#211d18",
    inkSoft: "#655e55",
    muted: "#91877b",

    accent: "#6b4e38",

    radius: "0px",
    contentWidth: "1360px",
    imageRatio: "3 / 4",

    headerStyle: "centered",
    rhythm: "luxury",
    defaultFontId: "tajawal",
  },

  {
    id: "commerce",
    name: "Commerce",
    description:
      "واجهة مباشرة وسريعة للمتاجر متعددة المنتجات.",

    canvas: "#f7f7f5",
    surface: "#ffffff",
    soft: "#ecefea",

    ink: "#171817",
    inkSoft: "#565b58",
    muted: "#818783",

    accent: "#185744",

    radius: "12px",
    contentWidth: "1500px",
    imageRatio: "1 / 1",

    headerStyle: "catalog",
    rhythm: "dense",
    defaultFontId: "cairo",
  },

  {
    id: "studio",
    name: "Studio",
    description:
      "شخصية مرنة للديكور والهدايا والمنتجات الإبداعية.",

    canvas: "#f2f2ee",
    surface: "#fcfcfa",
    soft: "#e5e7e1",

    ink: "#171917",
    inkSoft: "#555b56",
    muted: "#838b84",

    accent: "#44594b",

    radius: "22px",
    contentWidth: "1460px",
    imageRatio: "4 / 5",

    headerStyle: "studio",
    rhythm: "creative",
    defaultFontId: "readex",
  },

  {
    id: "technical",
    name: "Technical",
    description:
      "واجهة منظمة للإلكترونيات والمنتجات ذات المواصفات.",

    canvas: "#f3f6f6",
    surface: "#ffffff",
    soft: "#e8eeee",

    ink: "#162021",
    inkSoft: "#536164",
    muted: "#819094",

    accent: "#185563",

    radius: "8px",
    contentWidth: "1500px",
    imageRatio: "1 / 1",

    headerStyle: "technical",
    rhythm: "structured",
    defaultFontId: "plex",
  },

  {
    id: "mobile-flagship",
    name: "Flagship",
    description:
      "واجهة فاخرة للأجهزة الرائدة: صور كبيرة، مساحات واثقة، وعرض يشبه إطلاق منتج جديد.",

    canvas: "#f5f2eb",
    surface: "#fffdfa",
    soft: "#e9e4da",

    ink: "#0b0d11",
    inkSoft: "#4d5159",
    muted: "#7c8088",

    accent: "#c29a61",

    radius: "28px",
    contentWidth: "1600px",
    imageRatio: "4 / 5",

    headerStyle: "mobile-flagship",
    rhythm: "showcase",
    defaultFontId: "readex",
    verticalCodes: ["mobile-phones"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "هواتف وجوالات",
    productLabel: "الجوالات",
    productSingularLabel: "الجهاز",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث عن جوال أو موديل",
    defaultHeroTitle: "الجهاز الذي تريده، في واجهة تليق به.",
    defaultHeroDescription: "اكتشف الأجهزة بصور كبيرة وتفاصيل مقروءة، وانتقل من الاختيار إلى صفحة المنتج بسهولة.",
    featureLabels: ["صورة أوضح", "تفاصيل مقروءة", "تنقل هادئ"],
  },

  {
    id: "mobile-smart-market",
    name: "Smart Market",
    description:
      "واجهة بيع واضحة وسريعة لمتاجر الجوالات: بحث بارز، أقسام مباشرة، وبطاقات سهلة المقارنة.",

    canvas: "#f3f6fb",
    surface: "#ffffff",
    soft: "#e6edf7",

    ink: "#111827",
    inkSoft: "#4f5d73",
    muted: "#7b8798",

    accent: "#2563eb",

    radius: "18px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "mobile-market",
    rhythm: "market",
    defaultFontId: "cairo",
    verticalCodes: ["mobile-phones"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "هواتف وجوالات",
    productLabel: "الجوالات",
    productSingularLabel: "الجهاز",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث عن جوال أو موديل",
    defaultHeroTitle: "كل الجوالات. بشكل أسهل.",
    defaultHeroDescription: "تصفح الأقسام والأسعار والمنتجات بوضوح من أول نظرة، وافتح أي جهاز لمراجعة صوره وخياراته.",
    featureLabels: ["المنتجات", "الأقسام", "التفاصيل"],
  },
];

const GENERATED_VERTICAL_THEME_PRESETS: ThemePreset[] = [
  {
    id: "general-retail-signature" as ThemeId,
    name: "Everyday Select",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط متجر عام.",

    canvas: "#f6f2ea",
    surface: "#fffdfa",
    soft: "#ebe4d8",

    ink: "#171915",
    inkSoft: "#55584f",
    muted: "#84877d",

    accent: "#8d6a42",

    radius: "26px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["general-retail"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "متجر عام",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "الأقسام",
    searchPlaceholder: "ابحث عن منتج أو قسم",
    defaultHeroTitle: "اختيارات يومية، مرتبة كما تحب.",
    defaultHeroDescription: "واجهة مرنة تجمع المنتجات والأقسام والعروض في تجربة واضحة ومريحة.",
    featureLabels: ["تشكيلة واسعة", "أقسام واضحة", "تسوق سريع"],
  },
  {
    id: "general-retail-market" as ThemeId,
    name: "Market One",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط متجر عام.",

    canvas: "#f4f7f5",
    surface: "#ffffff",
    soft: "#e8efeb",

    ink: "#14201b",
    inkSoft: "#4e5b55",
    muted: "#7d8983",

    accent: "#1f7a55",

    radius: "16px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["general-retail"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "متجر عام",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "الأقسام",
    searchPlaceholder: "ابحث عن منتج أو قسم",
    defaultHeroTitle: "اختيارات يومية، مرتبة كما تحب.",
    defaultHeroDescription: "واجهة مرنة تجمع المنتجات والأقسام والعروض في تجربة واضحة ومريحة.",
    featureLabels: ["تشكيلة واسعة", "أقسام واضحة", "تسوق سريع"],
  },
  {
    id: "apparel-signature" as ThemeId,
    name: "Atelier",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط أزياء وملابس.",

    canvas: "#f6f3f0",
    surface: "#fffdfb",
    soft: "#ebe3df",

    ink: "#1c1716",
    inkSoft: "#5d5350",
    muted: "#8a7f7a",

    accent: "#9a5f55",

    radius: "8px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "plex" as FontId,
    verticalCodes: ["apparel"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "أزياء وملابس",
    productLabel: "القطع",
    productSingularLabel: "القطعة",
    categoryLabel: "المجموعات",
    searchPlaceholder: "ابحث عن قطعة أو مجموعة",
    defaultHeroTitle: "إطلالة تبدأ من التفاصيل.",
    defaultHeroDescription: "صور كبيرة ومساحات أنيقة تضع القصّة والخامة والمجموعة في المقدمة.",
    featureLabels: ["مجموعات موسمية", "صور تحريرية", "مقاسات وألوان"],
  },
  {
    id: "apparel-market" as ThemeId,
    name: "Street Edit",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط أزياء وملابس.",

    canvas: "#f5f5f3",
    surface: "#ffffff",
    soft: "#e9e9e4",

    ink: "#151515",
    inkSoft: "#525252",
    muted: "#858585",

    accent: "#252525",

    radius: "18px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["apparel"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "أزياء وملابس",
    productLabel: "القطع",
    productSingularLabel: "القطعة",
    categoryLabel: "المجموعات",
    searchPlaceholder: "ابحث عن قطعة أو مجموعة",
    defaultHeroTitle: "إطلالة تبدأ من التفاصيل.",
    defaultHeroDescription: "صور كبيرة ومساحات أنيقة تضع القصّة والخامة والمجموعة في المقدمة.",
    featureLabels: ["مجموعات موسمية", "صور تحريرية", "مقاسات وألوان"],
  },
  {
    id: "footwear-signature" as ThemeId,
    name: "Sole Atelier",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط أحذية.",

    canvas: "#f4f0ea",
    surface: "#fffdfa",
    soft: "#e9e0d5",

    ink: "#191816",
    inkSoft: "#59534e",
    muted: "#887f77",

    accent: "#9a6a43",

    radius: "24px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["footwear"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "أحذية",
    productLabel: "الأحذية",
    productSingularLabel: "الحذاء",
    categoryLabel: "الأنماط",
    searchPlaceholder: "ابحث عن حذاء أو مقاس",
    defaultHeroTitle: "خطوتك القادمة تبدأ هنا.",
    defaultHeroDescription: "عرض يبرز التصميم والخامة والمقاس مع تنقل سريع بين الأنماط.",
    featureLabels: ["مقاسات واضحة", "ألوان متعددة", "اختيار أسرع"],
  },
  {
    id: "footwear-market" as ThemeId,
    name: "Sneaker Grid",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط أحذية.",

    canvas: "#f2f5f7",
    surface: "#ffffff",
    soft: "#e5ebef",

    ink: "#151b20",
    inkSoft: "#53616b",
    muted: "#81909a",

    accent: "#2d6f95",

    radius: "16px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["footwear"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "أحذية",
    productLabel: "الأحذية",
    productSingularLabel: "الحذاء",
    categoryLabel: "الأنماط",
    searchPlaceholder: "ابحث عن حذاء أو مقاس",
    defaultHeroTitle: "خطوتك القادمة تبدأ هنا.",
    defaultHeroDescription: "عرض يبرز التصميم والخامة والمقاس مع تنقل سريع بين الأنماط.",
    featureLabels: ["مقاسات واضحة", "ألوان متعددة", "اختيار أسرع"],
  },
  {
    id: "perfumes-signature" as ThemeId,
    name: "Scent Maison",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط عطور.",

    canvas: "#f3eee6",
    surface: "#fcf8f1",
    soft: "#e7ddcf",

    ink: "#211b18",
    inkSoft: "#625952",
    muted: "#91867c",

    accent: "#9d7650",

    radius: "0px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "tajawal" as FontId,
    verticalCodes: ["perfumes"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "عطور",
    productLabel: "العطور",
    productSingularLabel: "العطر",
    categoryLabel: "العائلات العطرية",
    searchPlaceholder: "ابحث عن عطر أو علامة",
    defaultHeroTitle: "رائحة لها حضور قبل أن تُحكى.",
    defaultHeroDescription: "تجربة راقية للنوتات والتركيزات والمجموعات، بهدوء بصري ومساحات فاخرة.",
    featureLabels: ["نوتات واضحة", "تركيزات متعددة", "هدايا مختارة"],
  },
  {
    id: "perfumes-market" as ThemeId,
    name: "Noir Parfum",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط عطور.",

    canvas: "#151312",
    surface: "#1c1917",
    soft: "#29231f",

    ink: "#f5eee5",
    inkSoft: "#c8bbb0",
    muted: "#8f8176",

    accent: "#c69b6d",

    radius: "18px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["perfumes"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "عطور",
    productLabel: "العطور",
    productSingularLabel: "العطر",
    categoryLabel: "العائلات العطرية",
    searchPlaceholder: "ابحث عن عطر أو علامة",
    defaultHeroTitle: "رائحة لها حضور قبل أن تُحكى.",
    defaultHeroDescription: "تجربة راقية للنوتات والتركيزات والمجموعات، بهدوء بصري ومساحات فاخرة.",
    featureLabels: ["نوتات واضحة", "تركيزات متعددة", "هدايا مختارة"],
  },
  {
    id: "electronics-signature" as ThemeId,
    name: "Circuit",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط إلكترونيات.",

    canvas: "#eef3f4",
    surface: "#ffffff",
    soft: "#e2eaec",

    ink: "#102124",
    inkSoft: "#4f6267",
    muted: "#7e9095",

    accent: "#2a7a88",

    radius: "14px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "plex" as FontId,
    verticalCodes: ["electronics"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "إلكترونيات",
    productLabel: "الأجهزة",
    productSingularLabel: "الجهاز",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث بالموديل أو المواصفة",
    defaultHeroTitle: "تقنية واضحة، بدون تعقيد.",
    defaultHeroDescription: "واجهة منظمة للمواصفات والمقارنة والوصول السريع إلى الجهاز المناسب.",
    featureLabels: ["مواصفات تقنية", "مقارنة أسهل", "ضمان واضح"],
  },
  {
    id: "electronics-market" as ThemeId,
    name: "Tech Grid",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط إلكترونيات.",

    canvas: "#f2f5f8",
    surface: "#ffffff",
    soft: "#e6edf3",

    ink: "#111a23",
    inkSoft: "#4e5e6e",
    muted: "#7c8b99",

    accent: "#2563eb",

    radius: "14px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["electronics"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "إلكترونيات",
    productLabel: "الأجهزة",
    productSingularLabel: "الجهاز",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث بالموديل أو المواصفة",
    defaultHeroTitle: "تقنية واضحة، بدون تعقيد.",
    defaultHeroDescription: "واجهة منظمة للمواصفات والمقارنة والوصول السريع إلى الجهاز المناسب.",
    featureLabels: ["مواصفات تقنية", "مقارنة أسهل", "ضمان واضح"],
  },
  {
    id: "subscriptions-signature" as ThemeId,
    name: "Pulse",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط اشتراكات.",

    canvas: "#f2f2f8",
    surface: "#ffffff",
    soft: "#e7e7f2",

    ink: "#19182a",
    inkSoft: "#56556b",
    muted: "#85839b",

    accent: "#6c5ce7",

    radius: "24px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["subscriptions"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "اشتراكات",
    productLabel: "الباقات",
    productSingularLabel: "الباقة",
    categoryLabel: "الخطط",
    searchPlaceholder: "ابحث عن باقة أو مدة",
    defaultHeroTitle: "اشتراك واضح، قيمة مستمرة.",
    defaultHeroDescription: "خطط وميزات ومدد تظهر بوضوح حتى يفهم العميل ما يحصل عليه من أول نظرة.",
    featureLabels: ["خطط واضحة", "مدد مرنة", "مزايا مقروءة"],
  },
  {
    id: "subscriptions-market" as ThemeId,
    name: "Member Hub",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط اشتراكات.",

    canvas: "#f4f7fb",
    surface: "#ffffff",
    soft: "#e7eef8",

    ink: "#141c28",
    inkSoft: "#526174",
    muted: "#8190a3",

    accent: "#3178c6",

    radius: "16px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["subscriptions"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "اشتراكات",
    productLabel: "الباقات",
    productSingularLabel: "الباقة",
    categoryLabel: "الخطط",
    searchPlaceholder: "ابحث عن باقة أو مدة",
    defaultHeroTitle: "اشتراك واضح، قيمة مستمرة.",
    defaultHeroDescription: "خطط وميزات ومدد تظهر بوضوح حتى يفهم العميل ما يحصل عليه من أول نظرة.",
    featureLabels: ["خطط واضحة", "مدد مرنة", "مزايا مقروءة"],
  },
  {
    id: "services-signature" as ThemeId,
    name: "Service Studio",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط خدمات.",

    canvas: "#f5f2ed",
    surface: "#fffdfa",
    soft: "#e9e2d8",

    ink: "#1c1a17",
    inkSoft: "#5e5952",
    muted: "#8a847b",

    accent: "#8b6b4a",

    radius: "24px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["services"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "خدمات",
    productLabel: "الخدمات",
    productSingularLabel: "الخدمة",
    categoryLabel: "التخصصات",
    searchPlaceholder: "ابحث عن خدمة",
    defaultHeroTitle: "الخدمة المناسبة، بخطوات أبسط.",
    defaultHeroDescription: "عرض مهني يشرح الخدمة وما تتضمنه ويقود العميل إلى الحجز أو الطلب بسرعة.",
    featureLabels: ["شرح واضح", "وقت الخدمة", "طلب مباشر"],
  },
  {
    id: "services-market" as ThemeId,
    name: "Book & Go",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط خدمات.",

    canvas: "#f2f7f6",
    surface: "#ffffff",
    soft: "#e5efec",

    ink: "#14211d",
    inkSoft: "#50615b",
    muted: "#7f9089",

    accent: "#23845d",

    radius: "16px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["services"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "خدمات",
    productLabel: "الخدمات",
    productSingularLabel: "الخدمة",
    categoryLabel: "التخصصات",
    searchPlaceholder: "ابحث عن خدمة",
    defaultHeroTitle: "الخدمة المناسبة، بخطوات أبسط.",
    defaultHeroDescription: "عرض مهني يشرح الخدمة وما تتضمنه ويقود العميل إلى الحجز أو الطلب بسرعة.",
    featureLabels: ["شرح واضح", "وقت الخدمة", "طلب مباشر"],
  },
  {
    id: "car-rental-signature" as ThemeId,
    name: "Drive Executive",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط تأجير سيارات.",

    canvas: "#f1f2f2",
    surface: "#fbfcfc",
    soft: "#e3e5e5",

    ink: "#171b1c",
    inkSoft: "#545d5f",
    muted: "#838c8e",

    accent: "#7a5b3d",

    radius: "18px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "plex" as FontId,
    verticalCodes: ["car-rental"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "تأجير سيارات",
    productLabel: "السيارات",
    productSingularLabel: "السيارة",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث عن سيارة أو فئة",
    defaultHeroTitle: "اختر طريقك قبل أن تبدأ الرحلة.",
    defaultHeroDescription: "صور واسعة ومعلومات السيارة والسعر والتأجير في واجهة واثقة وسريعة.",
    featureLabels: ["فئات واضحة", "سعر التأجير", "معلومات السيارة"],
  },
  {
    id: "car-rental-market" as ThemeId,
    name: "Fleet Booking",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط تأجير سيارات.",

    canvas: "#f3f6f8",
    surface: "#ffffff",
    soft: "#e5ebef",

    ink: "#121b22",
    inkSoft: "#50606c",
    muted: "#7e8d98",

    accent: "#1f6f9c",

    radius: "14px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["car-rental"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "تأجير سيارات",
    productLabel: "السيارات",
    productSingularLabel: "السيارة",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث عن سيارة أو فئة",
    defaultHeroTitle: "اختر طريقك قبل أن تبدأ الرحلة.",
    defaultHeroDescription: "صور واسعة ومعلومات السيارة والسعر والتأجير في واجهة واثقة وسريعة.",
    featureLabels: ["فئات واضحة", "سعر التأجير", "معلومات السيارة"],
  },
  {
    id: "real-estate-signature" as ThemeId,
    name: "Residence",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط عقارات.",

    canvas: "#f3f0ea",
    surface: "#fffdf9",
    soft: "#e6dfd4",

    ink: "#1b1a17",
    inkSoft: "#5c574f",
    muted: "#898278",

    accent: "#92724d",

    radius: "4px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "tajawal" as FontId,
    verticalCodes: ["real-estate"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "عقارات",
    productLabel: "العقارات",
    productSingularLabel: "العقار",
    categoryLabel: "أنواع العقارات",
    searchPlaceholder: "ابحث بالمدينة أو نوع العقار",
    defaultHeroTitle: "مكان يستحق أن تراه بتفاصيله.",
    defaultHeroDescription: "مساحات واسعة للصور والموقع والتفاصيل الأساسية، مع حضور هادئ واحترافي.",
    featureLabels: ["صور ومساحات", "موقع واضح", "تفاصيل العقار"],
  },
  {
    id: "real-estate-market" as ThemeId,
    name: "Property Grid",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط عقارات.",

    canvas: "#f3f6f5",
    surface: "#ffffff",
    soft: "#e6ece9",

    ink: "#13201b",
    inkSoft: "#51615a",
    muted: "#809088",

    accent: "#31705a",

    radius: "14px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["real-estate"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "عقارات",
    productLabel: "العقارات",
    productSingularLabel: "العقار",
    categoryLabel: "أنواع العقارات",
    searchPlaceholder: "ابحث بالمدينة أو نوع العقار",
    defaultHeroTitle: "مكان يستحق أن تراه بتفاصيله.",
    defaultHeroDescription: "مساحات واسعة للصور والموقع والتفاصيل الأساسية، مع حضور هادئ واحترافي.",
    featureLabels: ["صور ومساحات", "موقع واضح", "تفاصيل العقار"],
  },
  {
    id: "restaurants-signature" as ThemeId,
    name: "Table",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط مطاعم.",

    canvas: "#f7f1e8",
    surface: "#fffaf3",
    soft: "#eee1cf",

    ink: "#251a12",
    inkSoft: "#6a5849",
    muted: "#9a8878",

    accent: "#b7652f",

    radius: "26px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["restaurants"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "مطاعم",
    productLabel: "الأطباق",
    productSingularLabel: "الطبق",
    categoryLabel: "القائمة",
    searchPlaceholder: "ابحث عن طبق أو قسم",
    defaultHeroTitle: "الطعم يبدأ من النظرة الأولى.",
    defaultHeroDescription: "صور شهية وقائمة سهلة القراءة، مع وصول سريع للأطباق والإضافات.",
    featureLabels: ["صور شهية", "قائمة واضحة", "إضافات سهلة"],
  },
  {
    id: "restaurants-market" as ThemeId,
    name: "Quick Menu",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط مطاعم.",

    canvas: "#fff7f1",
    surface: "#ffffff",
    soft: "#f5e7dc",

    ink: "#2a1710",
    inkSoft: "#6f574e",
    muted: "#9d887f",

    accent: "#e25822",

    radius: "18px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["restaurants"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "مطاعم",
    productLabel: "الأطباق",
    productSingularLabel: "الطبق",
    categoryLabel: "القائمة",
    searchPlaceholder: "ابحث عن طبق أو قسم",
    defaultHeroTitle: "الطعم يبدأ من النظرة الأولى.",
    defaultHeroDescription: "صور شهية وقائمة سهلة القراءة، مع وصول سريع للأطباق والإضافات.",
    featureLabels: ["صور شهية", "قائمة واضحة", "إضافات سهلة"],
  },
  {
    id: "delivery-marketplace-signature" as ThemeId,
    name: "Dispatch",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط منصة توصيل.",

    canvas: "#f3f4f6",
    surface: "#ffffff",
    soft: "#e7e9ed",

    ink: "#171a20",
    inkSoft: "#555d69",
    muted: "#858d98",

    accent: "#ef5b3f",

    radius: "22px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["delivery-marketplace"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "منصة توصيل",
    productLabel: "المتاجر والطلبات",
    productSingularLabel: "الخيار",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث عن متجر أو منتج",
    defaultHeroTitle: "كل ما تحتاجه، أقرب إليك.",
    defaultHeroDescription: "واجهة سريعة للعثور على المتاجر والمنتجات والعروض مع إحساس بالحركة والسرعة.",
    featureLabels: ["تصفح سريع", "متاجر متعددة", "وصول أسهل"],
  },
  {
    id: "delivery-marketplace-market" as ThemeId,
    name: "Delivery Hub",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط منصة توصيل.",

    canvas: "#f3f7fb",
    surface: "#ffffff",
    soft: "#e5edf6",

    ink: "#111c28",
    inkSoft: "#4f6173",
    muted: "#7d8fa0",

    accent: "#2563eb",

    radius: "18px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["delivery-marketplace"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "منصة توصيل",
    productLabel: "المتاجر والطلبات",
    productSingularLabel: "الخيار",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث عن متجر أو منتج",
    defaultHeroTitle: "كل ما تحتاجه، أقرب إليك.",
    defaultHeroDescription: "واجهة سريعة للعثور على المتاجر والمنتجات والعروض مع إحساس بالحركة والسرعة.",
    featureLabels: ["تصفح سريع", "متاجر متعددة", "وصول أسهل"],
  },
  {
    id: "grocery-signature" as ThemeId,
    name: "Fresh Market",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط بقالة ومواد غذائية.",

    canvas: "#f5f5ea",
    surface: "#fffef8",
    soft: "#e9ead8",

    ink: "#1a2115",
    inkSoft: "#59624d",
    muted: "#87907b",

    accent: "#6f8e3c",

    radius: "22px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["grocery"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "بقالة ومواد غذائية",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "الأقسام",
    searchPlaceholder: "ابحث عن منتج أو علامة",
    defaultHeroTitle: "كل يوم، أشياء تحتاجها فعلًا.",
    defaultHeroDescription: "تجربة دافئة وسريعة للمواد اليومية والعروض والمنتجات الطازجة.",
    featureLabels: ["منتجات يومية", "عروض واضحة", "تصفح سريع"],
  },
  {
    id: "grocery-market" as ThemeId,
    name: "Daily Basket",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط بقالة ومواد غذائية.",

    canvas: "#f5f8f3",
    surface: "#ffffff",
    soft: "#e7efe2",

    ink: "#172214",
    inkSoft: "#53614d",
    muted: "#82907b",

    accent: "#3f8f45",

    radius: "16px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["grocery"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "بقالة ومواد غذائية",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "الأقسام",
    searchPlaceholder: "ابحث عن منتج أو علامة",
    defaultHeroTitle: "كل يوم، أشياء تحتاجها فعلًا.",
    defaultHeroDescription: "تجربة دافئة وسريعة للمواد اليومية والعروض والمنتجات الطازجة.",
    featureLabels: ["منتجات يومية", "عروض واضحة", "تصفح سريع"],
  },
  {
    id: "automotive-parts-signature" as ThemeId,
    name: "Garage Pro",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط قطع سيارات.",

    canvas: "#222322",
    surface: "#292a29",
    soft: "#363735",

    ink: "#f4f3ef",
    inkSoft: "#c9c6bf",
    muted: "#918f88",

    accent: "#d17a2d",

    radius: "12px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "plex" as FontId,
    verticalCodes: ["automotive-parts"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "قطع سيارات",
    productLabel: "القطع",
    productSingularLabel: "القطعة",
    categoryLabel: "التصنيفات",
    searchPlaceholder: "ابحث برقم القطعة أو السيارة",
    defaultHeroTitle: "القطعة الصحيحة، بدون تخمين.",
    defaultHeroDescription: "واجهة عملية تبرز رقم القطعة والتوافق والماركة والمواصفات الأساسية.",
    featureLabels: ["رقم القطعة", "توافق المركبة", "مواصفات واضحة"],
  },
  {
    id: "automotive-parts-market" as ThemeId,
    name: "Parts Grid",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط قطع سيارات.",

    canvas: "#f2f4f5",
    surface: "#ffffff",
    soft: "#e4e9eb",

    ink: "#171d20",
    inkSoft: "#536068",
    muted: "#818d93",

    accent: "#36728d",

    radius: "10px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["automotive-parts"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "قطع سيارات",
    productLabel: "القطع",
    productSingularLabel: "القطعة",
    categoryLabel: "التصنيفات",
    searchPlaceholder: "ابحث برقم القطعة أو السيارة",
    defaultHeroTitle: "القطعة الصحيحة، بدون تخمين.",
    defaultHeroDescription: "واجهة عملية تبرز رقم القطعة والتوافق والماركة والمواصفات الأساسية.",
    featureLabels: ["رقم القطعة", "توافق المركبة", "مواصفات واضحة"],
  },
  {
    id: "digital-products-signature" as ThemeId,
    name: "Creator Vault",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط منتجات رقمية.",

    canvas: "#f3f1f8",
    surface: "#ffffff",
    soft: "#e8e4f1",

    ink: "#1d1827",
    inkSoft: "#5c556b",
    muted: "#8b8499",

    accent: "#7b5cc7",

    radius: "24px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["digital-products"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "منتجات رقمية",
    productLabel: "المنتجات الرقمية",
    productSingularLabel: "المنتج الرقمي",
    categoryLabel: "التصنيفات",
    searchPlaceholder: "ابحث عن ملف أو محتوى",
    defaultHeroTitle: "محتوى جاهز للوصول فورًا.",
    defaultHeroDescription: "تصميم نظيف للمحتوى الرقمي والملفات والتراخيص مع تركيز على القيمة والوضوح.",
    featureLabels: ["تحميل رقمي", "تراخيص واضحة", "وصول فوري"],
  },
  {
    id: "digital-products-market" as ThemeId,
    name: "Download Hub",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط منتجات رقمية.",

    canvas: "#f3f6fa",
    surface: "#ffffff",
    soft: "#e5ebf3",

    ink: "#151c27",
    inkSoft: "#526071",
    muted: "#8190a1",

    accent: "#3b82f6",

    radius: "16px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["digital-products"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "منتجات رقمية",
    productLabel: "المنتجات الرقمية",
    productSingularLabel: "المنتج الرقمي",
    categoryLabel: "التصنيفات",
    searchPlaceholder: "ابحث عن ملف أو محتوى",
    defaultHeroTitle: "محتوى جاهز للوصول فورًا.",
    defaultHeroDescription: "تصميم نظيف للمحتوى الرقمي والملفات والتراخيص مع تركيز على القيمة والوضوح.",
    featureLabels: ["تحميل رقمي", "تراخيص واضحة", "وصول فوري"],
  },
  {
    id: "jewelry-watches-signature" as ThemeId,
    name: "Heirloom",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط مجوهرات وساعات.",

    canvas: "#f3efe7",
    surface: "#fcf9f2",
    soft: "#e5ddcf",

    ink: "#201a14",
    inkSoft: "#62584d",
    muted: "#928579",

    accent: "#a47c46",

    radius: "0px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "tajawal" as FontId,
    verticalCodes: ["jewelry-watches"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "مجوهرات وساعات",
    productLabel: "القطع",
    productSingularLabel: "القطعة",
    categoryLabel: "المجموعات",
    searchPlaceholder: "ابحث عن قطعة أو مجموعة",
    defaultHeroTitle: "تفاصيل صغيرة، حضور لا يُنسى.",
    defaultHeroDescription: "مساحات فاخرة للذهب والأحجار والساعات مع صور قريبة وهوية راقية.",
    featureLabels: ["صور قريبة", "خامات واضحة", "شهادات وضمان"],
  },
  {
    id: "jewelry-watches-market" as ThemeId,
    name: "Time & Gold",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط مجوهرات وساعات.",

    canvas: "#171614",
    surface: "#1f1d1a",
    soft: "#2b2823",

    ink: "#f7f0e4",
    inkSoft: "#c9bcaa",
    muted: "#938675",

    accent: "#c59a54",

    radius: "16px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["jewelry-watches"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "مجوهرات وساعات",
    productLabel: "القطع",
    productSingularLabel: "القطعة",
    categoryLabel: "المجموعات",
    searchPlaceholder: "ابحث عن قطعة أو مجموعة",
    defaultHeroTitle: "تفاصيل صغيرة، حضور لا يُنسى.",
    defaultHeroDescription: "مساحات فاخرة للذهب والأحجار والساعات مع صور قريبة وهوية راقية.",
    featureLabels: ["صور قريبة", "خامات واضحة", "شهادات وضمان"],
  },
  {
    id: "furniture-decor-signature" as ThemeId,
    name: "Living Studio",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط أثاث وديكور.",

    canvas: "#f1efe9",
    surface: "#fbfaf6",
    soft: "#e4e0d6",

    ink: "#1d1d19",
    inkSoft: "#5e5d55",
    muted: "#8c8a81",

    accent: "#7a7356",

    radius: "28px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["furniture-decor"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "أثاث وديكور",
    productLabel: "القطع",
    productSingularLabel: "القطعة",
    categoryLabel: "المساحات",
    searchPlaceholder: "ابحث عن قطعة أو مساحة",
    defaultHeroTitle: "مساحة أجمل تبدأ بقطعة واحدة.",
    defaultHeroDescription: "صور كبيرة وتكوينات هادئة للأثاث والديكور مع إبراز الخامة والأبعاد.",
    featureLabels: ["أبعاد واضحة", "خامات متعددة", "صور مساحات"],
  },
  {
    id: "furniture-decor-market" as ThemeId,
    name: "Home Catalog",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط أثاث وديكور.",

    canvas: "#f4f5f2",
    surface: "#ffffff",
    soft: "#e8ebe4",

    ink: "#181d17",
    inkSoft: "#555e52",
    muted: "#858e82",

    accent: "#55774d",

    radius: "18px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["furniture-decor"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "أثاث وديكور",
    productLabel: "القطع",
    productSingularLabel: "القطعة",
    categoryLabel: "المساحات",
    searchPlaceholder: "ابحث عن قطعة أو مساحة",
    defaultHeroTitle: "مساحة أجمل تبدأ بقطعة واحدة.",
    defaultHeroDescription: "صور كبيرة وتكوينات هادئة للأثاث والديكور مع إبراز الخامة والأبعاد.",
    featureLabels: ["أبعاد واضحة", "خامات متعددة", "صور مساحات"],
  },
  {
    id: "cosmetics-signature" as ThemeId,
    name: "Glow",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط تجميل وعناية.",

    canvas: "#fff4f5",
    surface: "#fffafa",
    soft: "#f6e4e7",

    ink: "#2b1b20",
    inkSoft: "#765b64",
    muted: "#a58c95",

    accent: "#d16f8a",

    radius: "28px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["cosmetics"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "تجميل وعناية",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "روتين العناية",
    searchPlaceholder: "ابحث عن منتج أو درجة",
    defaultHeroTitle: "روتينك، بألوان أقرب لك.",
    defaultHeroDescription: "تجربة ناعمة للدرجات والمكونات والعناية، مع صور واضحة وتفاصيل سهلة القراءة.",
    featureLabels: ["درجات وألوان", "مكونات واضحة", "روتين مرتب"],
  },
  {
    id: "cosmetics-market" as ThemeId,
    name: "Beauty Market",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط تجميل وعناية.",

    canvas: "#fff6f2",
    surface: "#ffffff",
    soft: "#f6e7df",

    ink: "#2a1b17",
    inkSoft: "#735d54",
    muted: "#a08b82",

    accent: "#e07a5f",

    radius: "18px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["cosmetics"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "تجميل وعناية",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "روتين العناية",
    searchPlaceholder: "ابحث عن منتج أو درجة",
    defaultHeroTitle: "روتينك، بألوان أقرب لك.",
    defaultHeroDescription: "تجربة ناعمة للدرجات والمكونات والعناية، مع صور واضحة وتفاصيل سهلة القراءة.",
    featureLabels: ["درجات وألوان", "مكونات واضحة", "روتين مرتب"],
  },
  {
    id: "events-tickets-signature" as ThemeId,
    name: "Stage",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط فعاليات وتذاكر.",

    canvas: "#141318",
    surface: "#1c1a22",
    soft: "#292632",

    ink: "#f6f2ff",
    inkSoft: "#c8bdd9",
    muted: "#9185a3",

    accent: "#a768ff",

    radius: "22px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["events-tickets"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "فعاليات وتذاكر",
    productLabel: "الفعاليات",
    productSingularLabel: "الفعالية",
    categoryLabel: "التصنيفات",
    searchPlaceholder: "ابحث عن فعالية أو مدينة",
    defaultHeroTitle: "اللحظة تبدأ قبل فتح الأبواب.",
    defaultHeroDescription: "واجهة حيوية للفعاليات والتذاكر والتواريخ مع تركيز على الصورة والموعد.",
    featureLabels: ["موعد واضح", "فئات التذاكر", "مكان الفعالية"],
  },
  {
    id: "events-tickets-market" as ThemeId,
    name: "Ticket Hub",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط فعاليات وتذاكر.",

    canvas: "#f4f3f9",
    surface: "#ffffff",
    soft: "#e9e6f2",

    ink: "#1b1826",
    inkSoft: "#5d566d",
    muted: "#8d859d",

    accent: "#7c5ce0",

    radius: "16px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["events-tickets"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "فعاليات وتذاكر",
    productLabel: "الفعاليات",
    productSingularLabel: "الفعالية",
    categoryLabel: "التصنيفات",
    searchPlaceholder: "ابحث عن فعالية أو مدينة",
    defaultHeroTitle: "اللحظة تبدأ قبل فتح الأبواب.",
    defaultHeroDescription: "واجهة حيوية للفعاليات والتذاكر والتواريخ مع تركيز على الصورة والموعد.",
    featureLabels: ["موعد واضح", "فئات التذاكر", "مكان الفعالية"],
  },
  {
    id: "wholesale-b2b-signature" as ThemeId,
    name: "Trade Desk",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط جملة وشركات.",

    canvas: "#eff2f1",
    surface: "#ffffff",
    soft: "#e1e8e5",

    ink: "#13201c",
    inkSoft: "#4e5e58",
    muted: "#7c8c86",

    accent: "#356a5b",

    radius: "12px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "plex" as FontId,
    verticalCodes: ["wholesale-b2b"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "جملة وشركات",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "الكتالوج",
    searchPlaceholder: "ابحث عن SKU أو منتج",
    defaultHeroTitle: "تجارة أوضح للكميات الأكبر.",
    defaultHeroDescription: "تصميم مهني للكميات والأسعار والحد الأدنى والطلبات التجارية.",
    featureLabels: ["كميات وأسعار", "MOQ واضح", "طلبات شركات"],
  },
  {
    id: "wholesale-b2b-market" as ThemeId,
    name: "Bulk Market",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط جملة وشركات.",

    canvas: "#f3f5f5",
    surface: "#ffffff",
    soft: "#e5eaea",

    ink: "#172021",
    inkSoft: "#536163",
    muted: "#829092",

    accent: "#2f6f75",

    radius: "10px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["wholesale-b2b"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "جملة وشركات",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "الكتالوج",
    searchPlaceholder: "ابحث عن SKU أو منتج",
    defaultHeroTitle: "تجارة أوضح للكميات الأكبر.",
    defaultHeroDescription: "تصميم مهني للكميات والأسعار والحد الأدنى والطلبات التجارية.",
    featureLabels: ["كميات وأسعار", "MOQ واضح", "طلبات شركات"],
  },
  {
    id: "personalized-gifts-signature" as ThemeId,
    name: "Crafted",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط هدايا مخصصة.",

    canvas: "#f8f1ec",
    surface: "#fffaf7",
    soft: "#efe2d9",

    ink: "#271d18",
    inkSoft: "#6c594f",
    muted: "#9a877d",

    accent: "#c47755",

    radius: "28px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "readex" as FontId,
    verticalCodes: ["personalized-gifts"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "هدايا مخصصة",
    productLabel: "الهدايا",
    productSingularLabel: "الهدية",
    categoryLabel: "المناسبات",
    searchPlaceholder: "ابحث عن هدية أو مناسبة",
    defaultHeroTitle: "هدية تحمل اسم صاحبها.",
    defaultHeroDescription: "تصميم دافئ للصور والتخصيص والمناسبات، مع مساحة واضحة للنصوص والخيارات.",
    featureLabels: ["تخصيص الاسم", "صور وملفات", "مناسبات متعددة"],
  },
  {
    id: "personalized-gifts-market" as ThemeId,
    name: "Gift Lab",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط هدايا مخصصة.",

    canvas: "#f6f3fb",
    surface: "#ffffff",
    soft: "#ebe6f4",

    ink: "#211b2c",
    inkSoft: "#655a73",
    muted: "#94899f",

    accent: "#8b6ccf",

    radius: "20px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["personalized-gifts"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "هدايا مخصصة",
    productLabel: "الهدايا",
    productSingularLabel: "الهدية",
    categoryLabel: "المناسبات",
    searchPlaceholder: "ابحث عن هدية أو مناسبة",
    defaultHeroTitle: "هدية تحمل اسم صاحبها.",
    defaultHeroDescription: "تصميم دافئ للصور والتخصيص والمناسبات، مع مساحة واضحة للنصوص والخيارات.",
    featureLabels: ["تخصيص الاسم", "صور وملفات", "مناسبات متعددة"],
  },
  {
    id: "equipment-rental-signature" as ThemeId,
    name: "Field Gear",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط تأجير معدات.",

    canvas: "#202322",
    surface: "#292d2b",
    soft: "#363c39",

    ink: "#f3f4ef",
    inkSoft: "#c7cdc8",
    muted: "#919a94",

    accent: "#d0a34b",

    radius: "14px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "plex" as FontId,
    verticalCodes: ["equipment-rental"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "تأجير معدات",
    productLabel: "المعدات",
    productSingularLabel: "المعدة",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث عن معدة أو فئة",
    defaultHeroTitle: "المعدة المناسبة، عندما تحتاجها.",
    defaultHeroDescription: "واجهة قوية للمواصفات والحالة ومدة التأجير وموقع الاستلام.",
    featureLabels: ["مدة التأجير", "حالة المعدة", "موقع الاستلام"],
  },
  {
    id: "equipment-rental-market" as ThemeId,
    name: "Rental Hub",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط تأجير معدات.",

    canvas: "#f1f4f3",
    surface: "#ffffff",
    soft: "#e3e9e7",

    ink: "#17201d",
    inkSoft: "#53605b",
    muted: "#818e89",

    accent: "#3d7f69",

    radius: "14px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["equipment-rental"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "تأجير معدات",
    productLabel: "المعدات",
    productSingularLabel: "المعدة",
    categoryLabel: "الفئات",
    searchPlaceholder: "ابحث عن معدة أو فئة",
    defaultHeroTitle: "المعدة المناسبة، عندما تحتاجها.",
    defaultHeroDescription: "واجهة قوية للمواصفات والحالة ومدة التأجير وموقع الاستلام.",
    featureLabels: ["مدة التأجير", "حالة المعدة", "موقع الاستلام"],
  },
  {
    id: "home-goods-signature" as ThemeId,
    name: "Hearth",
    description: "هوية بصرية فاخرة ومفتوحة مصممة خصيصًا لنشاط مستلزمات منزلية.",

    canvas: "#f4f0e8",
    surface: "#fffdf9",
    soft: "#e8e1d5",

    ink: "#201c17",
    inkSoft: "#625a50",
    muted: "#91877c",

    accent: "#a17952",

    radius: "26px",
    contentWidth: "1580px",
    imageRatio: "4 / 5",

    headerStyle: "vertical-signature",
    rhythm: "showcase",
    defaultFontId: "tajawal" as FontId,
    verticalCodes: ["home-goods"],
    planBadge: "Business",
    experience: "signature",
    verticalLabel: "مستلزمات منزلية",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "الأقسام",
    searchPlaceholder: "ابحث عن منتج للمنزل",
    defaultHeroTitle: "تفاصيل بسيطة تجعل البيت أقرب لك.",
    defaultHeroDescription: "واجهة دافئة وعملية للمستلزمات اليومية والقطع المنزلية المتنوعة.",
    featureLabels: ["استخدام يومي", "أحجام وخامات", "أقسام مرتبة"],
  },
  {
    id: "home-goods-market" as ThemeId,
    name: "Home Market",
    description: "هوية تجارية واضحة وسريعة مصممة خصيصًا لنشاط مستلزمات منزلية.",

    canvas: "#f4f6f2",
    surface: "#ffffff",
    soft: "#e7ebe3",

    ink: "#182018",
    inkSoft: "#566052",
    muted: "#85907f",

    accent: "#5e7d4f",

    radius: "18px",
    contentWidth: "1580px",
    imageRatio: "1 / 1",

    headerStyle: "vertical-market",
    rhythm: "market",
    defaultFontId: "cairo" as FontId,
    verticalCodes: ["home-goods"],
    planBadge: "Business",
    experience: "market",
    verticalLabel: "مستلزمات منزلية",
    productLabel: "المنتجات",
    productSingularLabel: "المنتج",
    categoryLabel: "الأقسام",
    searchPlaceholder: "ابحث عن منتج للمنزل",
    defaultHeroTitle: "تفاصيل بسيطة تجعل البيت أقرب لك.",
    defaultHeroDescription: "واجهة دافئة وعملية للمستلزمات اليومية والقطع المنزلية المتنوعة.",
    featureLabels: ["استخدام يومي", "أحجام وخامات", "أقسام مرتبة"],
  }
];

export const VERTICAL_THEME_IDS: ThemeId[] =
  GENERATED_VERTICAL_THEME_PRESETS.map((theme) => theme.id);

export const THEME_PRESETS: ThemePreset[] = [
  ...CORE_THEME_PRESETS,
  ...GENERATED_VERTICAL_THEME_PRESETS,
];

export function getThemePreset(
  id: ThemeId,
) {
  return (
    THEME_PRESETS.find(
      (theme) =>
        theme.id === id,
    ) ??
    THEME_PRESETS[0]
  );
}

export function themeSupportsVertical(
  themeId: ThemeId,
  verticalCode: string | null,
) {
  // Themes stay selectable across verticals.
  // verticalCodes are recommendation metadata, not a hard compatibility gate.
  void themeId;
  void verticalCode;
  return true;
}

export function isThemeRecommendedForVertical(
  theme: ThemePreset,
  verticalCode: string | null,
) {
  if (!verticalCode) return false;

  return (
    theme.verticalCodes?.includes(verticalCode) ??
    false
  );
}

export function getThemePresetsForVertical(
  verticalCode: string | null,
) {
  return [...THEME_PRESETS].sort((left, right) => {
    const leftRecommended =
      isThemeRecommendedForVertical(
        left,
        verticalCode,
      );
    const rightRecommended =
      isThemeRecommendedForVertical(
        right,
        verticalCode,
      );

    if (leftRecommended === rightRecommended) {
      return 0;
    }

    return leftRecommended ? -1 : 1;
  });
}
