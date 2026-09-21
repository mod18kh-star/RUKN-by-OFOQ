import {
  VERTICAL_THEME_IDS,
} from "./themePresets";

import type {
  ExtraSectionType,
  FontId,
  HeroLayout,
  PlanTier,
  ProductCardStyle,
  ProductSectionLayout,
  StorySectionLayout,
  ThemeId,
} from "./theme.types";

export interface PlanEntitlements {
  themes: ThemeId[];
  fonts: FontId[];

  cardStyles: ProductCardStyle[];
  heroLayouts: HeroLayout[];

  productSectionLayouts:
    ProductSectionLayout[];

  storySectionLayouts:
    StorySectionLayout[];

  extraSectionTypes:
    ExtraSectionType[];

  maxExtraSections: number;

  canReorderSections: boolean;
  canAdvancedCustomize: boolean;
}

export const PLAN_ENTITLEMENTS: Record<
  PlanTier,
  PlanEntitlements
> = {
  free: {
    themes: [
      "editorial",
    ],

    fonts: [
      "plex",
    ],

    cardStyles: [
      "minimal",
    ],

    heroLayouts: [
      "single",
    ],

    productSectionLayouts: [
      "theme-default",
    ],

    storySectionLayouts: [
      "theme-default",
    ],

    extraSectionTypes: [],
    maxExtraSections: 0,

    canReorderSections: false,
    canAdvancedCustomize: false,
  },

  business: {
    themes: [
      "editorial",
      "maison",
      "commerce",
      "studio",
      "mobile-flagship",
      "mobile-smart-market",
      ...VERTICAL_THEME_IDS,
    ],

    fonts: [
      "plex",
      "tajawal",
      "cairo",
      "readex",
    ],

    cardStyles: [
      "minimal",
      "editorial",
      "commerce",
      "mobile-flagship",
      "mobile-market",
      "vertical-signature",
      "vertical-market",
    ],

    heroLayouts: [
      "single",
      "wide-portrait",
      "focus-one",
    ],

    productSectionLayouts: [
      "theme-default",
      "grid-3",
      "featured-grid",
    ],

    storySectionLayouts: [
      "theme-default",
      "split-start",
      "split-end",
    ],

    extraSectionTypes: [
      "categories",
      "banner",
    ],

    maxExtraSections: 2,

    canReorderSections: true,
    canAdvancedCustomize: false,
  },

  pro: {
    themes: [
      "editorial",
      "maison",
      "commerce",
      "studio",
      "technical",
      "mobile-flagship",
      "mobile-smart-market",
      ...VERTICAL_THEME_IDS,
    ],

    fonts: [
      "plex",
      "tajawal",
      "cairo",
      "readex",
    ],

    cardStyles: [
      "minimal",
      "editorial",
      "commerce",
      "compact",
      "technical",
      "mobile-flagship",
      "mobile-market",
      "vertical-signature",
      "vertical-market",
    ],

    heroLayouts: [
      "single",
      "wide-portrait",
      "equal",
      "focus-one",
      "focus-two",
    ],

    productSectionLayouts: [
      "theme-default",
      "grid-3",
      "featured-grid",
      "horizontal",
      "spotlight",
    ],

    storySectionLayouts: [
      "theme-default",
      "split-start",
      "split-end",
      "centered",
      "full-bleed",
    ],

    extraSectionTypes: [
      "categories",
      "banner",
    ],

    maxExtraSections: 8,

    canReorderSections: true,
    canAdvancedCustomize: true,
  },

  elite: {
    themes: [
      "editorial",
      "maison",
      "commerce",
      "studio",
      "technical",
      "mobile-flagship",
      "mobile-smart-market",
      ...VERTICAL_THEME_IDS,
    ],

    fonts: [
      "plex",
      "tajawal",
      "cairo",
      "readex",
    ],

    cardStyles: [
      "minimal",
      "editorial",
      "commerce",
      "compact",
      "technical",
      "mobile-flagship",
      "mobile-market",
      "vertical-signature",
      "vertical-market",
    ],

    heroLayouts: [
      "single",
      "wide-portrait",
      "equal",
      "focus-one",
      "focus-two",
    ],

    productSectionLayouts: [
      "theme-default",
      "grid-3",
      "featured-grid",
      "horizontal",
      "spotlight",
    ],

    storySectionLayouts: [
      "theme-default",
      "split-start",
      "split-end",
      "centered",
      "full-bleed",
    ],

    extraSectionTypes: [
      "categories",
      "banner",
    ],

    maxExtraSections: 20,

    canReorderSections: true,
    canAdvancedCustomize: true,
  },
};

export function getPlanEntitlements(
  tier: PlanTier,
) {
  return PLAN_ENTITLEMENTS[tier];
}