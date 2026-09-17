import type {
  StorefrontConfig,
  StorefrontSectionKey,
} from "../theme/theme.types";

export const STOREFRONT_CONFIG_CHANGED_EVENT =
  "ofoq-storefront-config-changed";

const SECTION_KEYS:
  StorefrontSectionKey[] = [
    "categories",
    "products",
    "banner",
    "story",
  ];

export const DEFAULT_STOREFRONT_CONFIG:
  StorefrontConfig = {
  planTier:
    "business",

  storeName:
    "NOOR",

  announcement:
    "شحن مجاني للطلبات فوق 300 ر.س",

  logoUrl:
    "",

  coverImageUrl:
    "",

  brandPrimaryColor:
    "",

  brandAccentColor:
    "",

  themeId:
    "editorial",

  fontId:
    "plex",

  productCardStyle:
    "editorial",

  heroLayout:
    "wide-portrait",

  hero: {
    eyebrow:
      "مجموعة خريف 2026",

    title:
      "قطع هادئة حضورها واضح",

    description:
      "مجموعة مختارة بعناية للاستخدام اليومي",

    primaryImage:
      "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?auto=format&fit=crop&w=1800&q=90",

    secondaryImage:
      "https://images.unsplash.com/photo-1529139574466-a303027c1d8b?auto=format&fit=crop&w=1200&q=90",

    ctaLabel:
      "اكتشف المجموعة",

    ctaHref:
      "#products",
  },

  categorySection: {
    id:
      "categories",

    enabled:
      false,

    eyebrow:
      "تسوق حسب القسم",

    title:
      "اكتشف ما يناسبك",

    sourceType:
      "all",

    manualCategorySlugs:
      [],

    itemLimit:
      4,
  },

  productSection: {
    id:
      "products",

    enabled:
      true,

    eyebrow:
      "وصل حديثا",

    title:
      "اختيارات هذا الأسبوع",

    layout:
      "theme-default",

    sourceType:
      "catalog",

    categorySlug:
      "",

    manualProductIds:
      [],

    itemLimit:
      8,
  },

  bannerSection: {
    id:
      "banner",

    enabled:
      false,

    eyebrow:
      "اختيار خاص",

    title:
      "مجموعة صنعت لتعيش معك",

    body:
      "اكتشف مجموعة مختارة بتفاصيل هادئة وجودة مصممة للاستخدام اليومي.",

    image:
      "https://images.unsplash.com/photo-1525507119028-ed4c629a60a3?auto=format&fit=crop&w=1800&q=90",

    ctaLabel:
      "اكتشف الآن",

    ctaHref:
      "#products",
  },

  story: {
    id:
      "story",

    enabled:
      true,

    eyebrow:
      "قصة المجموعة",

    title:
      "تصميم لا يحتاج أن يصرخ حتى تلاحظه",

    body:
      "اخترنا هذه المجموعة على أساس الخامة والتفاصيل والاستمرارية بعيدا عن الضجيج والمواسم السريعة.",

    image:
      "https://images.unsplash.com/photo-1483985988355-763728e1935b?auto=format&fit=crop&w=1600&q=90",

    ctaLabel:
      "اكتشف القصة",

    ctaHref:
      "#",

    layout:
      "theme-default",
  },

  sectionOrder: [
    "categories",
    "products",
    "banner",
    "story",
  ],
};

const STORAGE_KEY =
  "ofoq-storefront-config-v2";

function normalizeSectionOrder(
  value: unknown,
): StorefrontSectionKey[] {
  const result:
    StorefrontSectionKey[] = [];

  if (
    Array.isArray(
      value,
    )
  ) {
    for (
      const candidate
      of value
    ) {
      if (
        typeof candidate !==
        "string"
      ) {
        continue;
      }

      const key =
        candidate as
          StorefrontSectionKey;

      if (
        !SECTION_KEYS.includes(
          key,
        )
      ) {
        continue;
      }

      if (
        result.includes(
          key,
        )
      ) {
        continue;
      }

      result.push(
        key,
      );
    }
  }

  for (
    const key
    of SECTION_KEYS
  ) {
    if (
      !result.includes(
        key,
      )
    ) {
      result.push(
        key,
      );
    }
  }

  return result;
}

export function loadStorefrontConfig():
  StorefrontConfig {
  if (
    typeof window ===
    "undefined"
  ) {
    return DEFAULT_STOREFRONT_CONFIG;
  }

  const raw =
    window.localStorage.getItem(
      STORAGE_KEY,
    );

  if (!raw) {
    return DEFAULT_STOREFRONT_CONFIG;
  }

  try {
    const parsed =
      JSON.parse(
        raw,
      ) as Partial<StorefrontConfig>;

    return {
      ...DEFAULT_STOREFRONT_CONFIG,
      ...parsed,

      hero: {
        ...DEFAULT_STOREFRONT_CONFIG.hero,
        ...(parsed.hero ?? {}),
      },

      categorySection: {
        ...DEFAULT_STOREFRONT_CONFIG.categorySection,
        ...(parsed.categorySection ?? {}),

        manualCategorySlugs:
          Array.isArray(
            parsed.categorySection
              ?.manualCategorySlugs,
          )
            ? parsed.categorySection
                .manualCategorySlugs
            : [],
      },

      productSection: {
        ...DEFAULT_STOREFRONT_CONFIG.productSection,
        ...(parsed.productSection ?? {}),

        manualProductIds:
          Array.isArray(
            parsed.productSection
              ?.manualProductIds,
          )
            ? parsed.productSection
                .manualProductIds
            : [],
      },

      bannerSection: {
        ...DEFAULT_STOREFRONT_CONFIG.bannerSection,
        ...(parsed.bannerSection ?? {}),
      },

      story: {
        ...DEFAULT_STOREFRONT_CONFIG.story,
        ...(parsed.story ?? {}),
      },

      sectionOrder:
        normalizeSectionOrder(
          parsed.sectionOrder,
        ),
    };
  }
  catch {
    return DEFAULT_STOREFRONT_CONFIG;
  }
}

export function saveStorefrontConfig(
  config: StorefrontConfig,
) {
  window.localStorage.setItem(
    STORAGE_KEY,
    JSON.stringify(
      config,
    ),
  );

  window.dispatchEvent(
    new CustomEvent(
      STOREFRONT_CONFIG_CHANGED_EVENT,
      {
        detail:
          config,
      },
    ),
  );
}