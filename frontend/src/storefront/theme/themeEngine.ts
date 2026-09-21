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
  themeSupportsVertical,
} from "./themePresets";

import type {
  ProductCardStyle,
  StorefrontConfig,
  ThemeId,
} from "./theme.types";

type ThemeStyle =
  CSSProperties &
  Record<
    `--${string}`,
    string | number
  >;

function resolveThemeId(
  input: StorefrontConfig,
): ThemeId {
  const rights =
    getPlanEntitlements(
      input.planTier,
    );

  if (
    rights.themes.includes(
      input.themeId,
    ) &&
    themeSupportsVertical(
      input.themeId,
      input.verticalCode,
    )
  ) {
    return input.themeId;
  }

  return (
    rights.themes.find(
      (candidate) =>
        themeSupportsVertical(
          candidate,
          input.verticalCode,
        ),
    ) ??
    rights.themes[0]
  );
}

function themeCardStyle(
  themeId: ThemeId,
): ProductCardStyle | null {
  if (themeId === "mobile-flagship") {
    return "mobile-flagship";
  }

  if (themeId === "mobile-smart-market") {
    return "mobile-market";
  }

  const theme = getThemePreset(themeId);

  if (theme.experience === "signature") {
    return "vertical-signature";
  }

  if (theme.experience === "market") {
    return "vertical-market";
  }

  return null;
}

export function resolveStorefrontConfig(
  input: StorefrontConfig,
): StorefrontConfig {
  const rights =
    getPlanEntitlements(
      input.planTier,
    );

  const themeId =
    resolveThemeId(input);

  const theme =
    getThemePreset(themeId);

  const fontId =
    rights.fonts.includes(
      input.fontId,
    )
      ? input.fontId
      : rights.fonts[0];

  const requestedThemeCardStyle = input.productCardStyleOverride
    ? null
    : themeCardStyle(themeId);

  const productCardStyle =
    requestedThemeCardStyle &&
    rights.cardStyles.includes(
      requestedThemeCardStyle,
    )
      ? requestedThemeCardStyle
      : rights.cardStyles.includes(
          input.productCardStyle,
        )
        ? input.productCardStyle
        : rights.cardStyles[0];

  const themeHeroLayout =
    theme.experience === "market" && themeId !== "mobile-smart-market"
      ? "focus-one"
      : theme.experience === "signature" && themeId !== "mobile-flagship"
        ? "wide-portrait"
        : input.heroLayout;

  const heroLayout =
    rights.heroLayouts.includes(
      themeHeroLayout,
    )
      ? themeHeroLayout
      : rights.heroLayouts[0];

  const themeProductSectionLayout =
    theme.experience === "signature" && themeId !== "mobile-flagship"
      ? "featured-grid"
      : theme.experience === "market" && themeId !== "mobile-smart-market"
        ? "grid-3"
        : input.productSection.layout;

  const productSectionLayout =
    rights.productSectionLayouts.includes(
      themeProductSectionLayout,
    )
      ? themeProductSectionLayout
      : rights.productSectionLayouts[0];

  const themeStorySectionLayout =
    theme.experience === "signature" && themeId !== "mobile-flagship"
      ? "split-end"
      : theme.experience === "market" && themeId !== "mobile-smart-market"
        ? "split-start"
        : input.story.layout;

  const storySectionLayout =
    rights.storySectionLayouts.includes(
      themeStorySectionLayout,
    )
      ? themeStorySectionLayout
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

function readableTextColor(hex: string): string {
  const raw = hex.trim().replace(/^#/, "");
  const normalized = /^[0-9a-fA-F]{3}$/.test(raw)
    ? raw.split("").map((part) => part + part).join("")
    : raw;
  if (!/^[0-9a-fA-F]{6}$/.test(normalized)) return "#FFFFFF";
  const rgb = [0, 2, 4].map((offset) => parseInt(normalized.slice(offset, offset + 2), 16) / 255);
  const linear = rgb.map((value) => value <= .04045 ? value / 12.92 : ((value + .055) / 1.055) ** 2.4);
  const lightness = linear[0] * .2126 + linear[1] * .7152 + linear[2] * .0722;
  const whiteContrast = 1.05 / (lightness + .05);
  const darkContrast = (lightness + .05) / .05;
  return whiteContrast >= darkContrast ? "#FFFFFF" : "#000000";
}


function safeHex(value: string | undefined, fallback: string): string {
  return value && /^#[0-9a-fA-F]{6}$/.test(value) ? value : fallback;
}

function colorContrast(first: string, second: string): number {
  const luminosity = (hex: string) => {
    const rgb = [1, 3, 5].map(offset => parseInt(hex.slice(offset, offset + 2), 16) / 255);
    const linear = rgb.map(v => v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4);
    return linear[0] * .2126 + linear[1] * .7152 + linear[2] * .0722;
  };
  const a = luminosity(first), b = luminosity(second);
  return (Math.max(a, b) + .05) / (Math.min(a, b) + .05);
}

function legibleColor(requested: string | undefined, background: string): string {
  const safe = safeHex(requested, readableTextColor(background));
  return colorContrast(safe, background) >= 4.5 ? safe : readableTextColor(background);
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

  const primary = safeHex(config.brandPrimaryColor, theme.ink);
  const accent = safeHex(config.brandAccentColor, theme.accent);
  const buttonBackground = safeHex(config.primaryButtonColor, primary);
  return {
    "--store-body-text": legibleColor(config.bodyTextColor, theme.surface),
    "--store-button-bg": buttonBackground,
    "--store-button-text": legibleColor(config.primaryButtonTextColor, buttonBackground),
    "--store-canvas":
      theme.canvas,

    "--store-surface":
      theme.surface,

    "--store-soft":
      theme.soft,

    "--store-ink":
      config.brandPrimaryColor ||
      theme.ink,

    "--store-theme-ink":
      theme.ink,

    "--store-ink-soft":
      theme.inkSoft,

    "--store-muted":
      theme.muted,

    "--store-accent":
      config.brandAccentColor ||
      theme.accent,

    "--store-theme-accent":
      theme.accent,

    "--store-ink-contrast":
      readableTextColor(config.brandPrimaryColor || theme.ink),

    "--store-accent-contrast":
      legibleColor(config.accentButtonTextColor, accent),

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
