import {
  DEFAULT_STOREFRONT_CONFIG,
} from "./storefrontConfig";

import type {
  StorefrontInfo,
} from "../data/storefrontApi";

import type {
  FontId,
  StorefrontConfig,
  ThemeId,
} from "../theme/theme.types";

const themeIds:
  ThemeId[] = [
    "editorial",
    "maison",
    "commerce",
    "studio",
    "technical",
  ];

const fontIds:
  FontId[] = [
    "plex",
    "tajawal",
    "cairo",
    "readex",
  ];

function resolveThemeId(
  value: string,
): ThemeId {
  return themeIds.includes(
    value as ThemeId,
  )
    ? value as ThemeId
    : "editorial";
}

function resolveFontId(
  value: string,
): FontId {
  return fontIds.includes(
    value as FontId,
  )
    ? value as FontId
    : "plex";
}

export function createLiveStorefrontConfig(
  store: StorefrontInfo,
): StorefrontConfig {
  const cleanName =
    store.name.trim() ||
    "المتجر";

  const presentation =
    store.presentation;

  return {
    ...DEFAULT_STOREFRONT_CONFIG,

    planTier:
      "pro",

    storeName:
      cleanName,

    announcement:
      presentation.announcement ??
      "",

    logoUrl:
      presentation.logoUrl ??
      "",

    coverImageUrl:
      presentation.coverImageUrl ??
      "",

    brandPrimaryColor:
      presentation.primaryColor ??
      "",

    brandAccentColor:
      presentation.accentColor ??
      "",

    themeId:
      resolveThemeId(
        presentation.themePresetCode,
      ),

    fontId:
      resolveFontId(
        presentation.fontCode,
      ),

    hero: {
      ...DEFAULT_STOREFRONT_CONFIG.hero,

      eyebrow:
        "",

      title:
        "",

      description:
        "",

      primaryImage:
        presentation.coverImageUrl ??
        "",

      secondaryImage:
        "",

      ctaLabel:
        "",

      ctaHref:
        "",
    },

    categorySection: {
      ...DEFAULT_STOREFRONT_CONFIG.categorySection,

      enabled:
        presentation.showCategoriesOnHome,

      eyebrow:
        "الأقسام",

      title:
        presentation.categorySectionTitle,

      sourceType:
        "all",

      manualCategorySlugs:
        [],

      itemLimit:
        24,
    },

    productSection: {
      ...DEFAULT_STOREFRONT_CONFIG.productSection,

      enabled:
        presentation.showProductsOnHome,

      eyebrow:
        "المنتجات",

      title:
        presentation.productSectionTitle,

      sourceType:
        "catalog",

      categorySlug:
        "",

      manualProductIds:
        [],

      itemLimit:
        24,
    },

    bannerSection: {
      ...DEFAULT_STOREFRONT_CONFIG.bannerSection,

      enabled:
        false,

      eyebrow:
        "",

      title:
        "",

      body:
        "",

      image:
        "",

      ctaLabel:
        "",

      ctaHref:
        "",
    },

    story: {
      ...DEFAULT_STOREFRONT_CONFIG.story,

      enabled:
        false,

      eyebrow:
        "",

      title:
        "",

      body:
        "",

      image:
        "",

      ctaLabel:
        "",

      ctaHref:
        "",
    },

    sectionOrder: [
      "categories",
      "products",
      "banner",
      "story",
    ],
  };
}
