import { parseVisualContent } from "./visualContent";

import {
  DEFAULT_STOREFRONT_CONFIG,
} from "./storefrontConfig";

import type {
  StorefrontInfo,
} from "../data/storefrontApi";

import {
  THEME_PRESETS,
} from "../theme/themePresets";

import type {
  FontId,
  StorefrontConfig,
  ThemeId,
} from "../theme/theme.types";

const themeIds:
  ThemeId[] = THEME_PRESETS.map((theme) => theme.id);

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

  const editorial = parseVisualContent(presentation.visualContentJson);
  const allowedProductLayouts = ["theme-default", "grid-3", "featured-grid", "horizontal", "spotlight"] as const;
  const allowedCardStyles = ["minimal", "editorial", "commerce", "compact", "technical", "mobile-flagship", "mobile-market", "vertical-signature", "vertical-market"] as const;
  const layout = allowedProductLayouts.find((value) => value === editorial.productLayout) ?? "theme-default";
  const customCardStyle = allowedCardStyles.find((value) => value === editorial.productCardStyle);
  const categoryCardLayout = editorial.categoryCardLayout === "grid" || editorial.categoryCardLayout === "compact"
    ? editorial.categoryCardLayout : "theme-default";
  const sectionOrder = editorial.sectionOrder.split(",");
  const allowedSections = ["categories", "products", "banner", "story"];
  const validSectionOrder = sectionOrder.length === allowedSections.length &&
    new Set(sectionOrder).size === allowedSections.length &&
    sectionOrder.every((section) => allowedSections.includes(section));

  return {
    ...DEFAULT_STOREFRONT_CONFIG,

    planTier:
      "pro",

    verticalCode:
      store.verticalCode,

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

    bodyTextColor: editorial.bodyTextColor,
    primaryButtonColor: editorial.primaryButtonColor,
    primaryButtonTextColor: editorial.primaryButtonTextColor,
    accentButtonTextColor: editorial.accentButtonTextColor,

    themeId:
      resolveThemeId(
        presentation.themePresetCode,
      ),

    fontId:
      resolveFontId(
        presentation.fontCode,
      ),

    productCardStyle: customCardStyle ?? DEFAULT_STOREFRONT_CONFIG.productCardStyle,
    productCardStyleOverride: Boolean(customCardStyle),
    productSectionLayoutOverride: layout !== "theme-default" || Boolean(customCardStyle),
    categoryCardLayout,

    hero: {
      ...DEFAULT_STOREFRONT_CONFIG.hero,
      heroFeaturedCaption: editorial.heroFeaturedCaption,
      heroStat1: editorial.heroStat1,
      heroStat2: editorial.heroStat2,
      heroStat3: editorial.heroStat3,
      heroNote1Title: editorial.heroNote1Title,
      heroNote1Body: editorial.heroNote1Body,
      heroNote2Title: editorial.heroNote2Title,
      heroNote2Body: editorial.heroNote2Body,
      heroNote3Title: editorial.heroNote3Title,
      heroNote3Body: editorial.heroNote3Body,
      flagshipBand1Title: editorial.flagshipBand1Title,
      flagshipBand1Body: editorial.flagshipBand1Body,
      flagshipBand2Title: editorial.flagshipBand2Title,
      flagshipBand2Body: editorial.flagshipBand2Body,
      flagshipBand3Title: editorial.flagshipBand3Title,
      flagshipBand3Body: editorial.flagshipBand3Body,
      smartBand1Title: editorial.smartBand1Title,
      smartBand1Body: editorial.smartBand1Body,
      smartBand2Title: editorial.smartBand2Title,
      smartBand2Body: editorial.smartBand2Body,
      smartBand3Title: editorial.smartBand3Title,
      smartBand3Body: editorial.smartBand3Body,
      smartBand4Title: editorial.smartBand4Title,
      smartBand4Body: editorial.smartBand4Body,
      showcaseMode: ["default", "image", "text", "product", "products"].includes(editorial.heroShowcaseMode)
        ? editorial.heroShowcaseMode : "default",
      showcaseText: editorial.heroShowcaseText,
      showcaseImage: editorial.heroShowcaseImage,
      featuredProductIds: editorial.featuredProductIds,

      eyebrow: editorial.heroEyebrow,

      title: editorial.heroTitle,

      description: editorial.heroDescription,

      primaryImage:
        presentation.coverImageUrl ??
        "",

      secondaryImage: editorial.heroSecondaryImage,

      ctaLabel: editorial.heroCtaLabel,

      ctaHref: editorial.heroCtaHref,
    },

    categorySection: {
      ...DEFAULT_STOREFRONT_CONFIG.categorySection,

      enabled:
        presentation.showCategoriesOnHome,

      eyebrow: editorial.categoryEyebrow || "الأقسام",

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
      layout: customCardStyle && layout === "theme-default" ? "grid-3" : layout,

      enabled:
        presentation.showProductsOnHome,

      eyebrow: editorial.productEyebrow || "المنتجات",

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

    sectionOrder: validSectionOrder
      ? sectionOrder as ("categories" | "products" | "banner" | "story")[]
      : DEFAULT_STOREFRONT_CONFIG.sectionOrder,
  };
}
