import type {
  CSSProperties,
} from "react";

import {
  getFont,
} from "./fonts";

import {
  getPlanEntitlements,
} from "./planEntitlements";

import {
  getThemePreset,
} from "./themePresets";

import type {
  StorefrontConfig,
} from "./theme.types";

type ThemeStyle =
  CSSProperties &
  Record<
    `--${string}`,
    string | number
  >;

export function resolveStorefrontConfig(
  input: StorefrontConfig,
): StorefrontConfig {
  const rights =
    getPlanEntitlements(
      input.planTier,
    );

  const themeId =
    rights.themes.includes(
      input.themeId,
    )
      ? input.themeId
      : rights.themes[0];

  const fontId =
    rights.fonts.includes(
      input.fontId,
    )
      ? input.fontId
      : rights.fonts[0];

  const productCardStyle =
    rights.cardStyles.includes(
      input.productCardStyle,
    )
      ? input.productCardStyle
      : rights.cardStyles[0];

  const heroLayout =
    rights.heroLayouts.includes(
      input.heroLayout,
    )
      ? input.heroLayout
      : rights.heroLayouts[0];

  const productSectionLayout =
    rights.productSectionLayouts.includes(
      input.productSection.layout,
    )
      ? input.productSection.layout
      : rights.productSectionLayouts[0];

  const storySectionLayout =
    rights.storySectionLayouts.includes(
      input.story.layout,
    )
      ? input.story.layout
      : rights.storySectionLayouts[0];

  const categoriesAllowed =
    rights.extraSectionTypes.includes(
      "categories",
    );

  const bannerAllowed =
    rights.extraSectionTypes.includes(
      "banner",
    );

  const categoriesEnabled =
    input.categorySection.enabled &&
    categoriesAllowed &&
    rights.maxExtraSections >= 1;

  const bannerRequiredSlot =
    categoriesEnabled
      ? 2
      : 1;

  const bannerEnabled =
    input.bannerSection.enabled &&
    bannerAllowed &&
    rights.maxExtraSections >=
      bannerRequiredSlot;

  return {
    ...input,

    themeId,
    fontId,
    productCardStyle,
    heroLayout,

    categorySection: {
      ...input.categorySection,
      enabled:
        categoriesEnabled,
    },

    productSection: {
      ...input.productSection,
      layout:
        productSectionLayout,
    },

    bannerSection: {
      ...input.bannerSection,
      enabled:
        bannerEnabled,
    },

    story: {
      ...input.story,
      layout:
        storySectionLayout,
    },
  };
}

export function createThemeStyle(
  input: StorefrontConfig,
): ThemeStyle {
  const config =
    resolveStorefrontConfig(
      input,
    );

  const theme =
    getThemePreset(
      config.themeId,
    );

  const font =
    getFont(
      config.fontId,
    );

  return {
    "--store-canvas":
      theme.canvas,

    "--store-surface":
      theme.surface,

    "--store-soft":
      theme.soft,

    "--store-ink":
      config.brandPrimaryColor ||
      theme.ink,

    "--store-ink-soft":
      theme.inkSoft,

    "--store-muted":
      theme.muted,

    "--store-accent":
      config.brandAccentColor ||
      theme.accent,

    "--store-radius":
      theme.radius,

    "--store-content":
      theme.contentWidth,

    "--store-image-ratio":
      theme.imageRatio,

    "--store-font":
      font.family,
  };
}