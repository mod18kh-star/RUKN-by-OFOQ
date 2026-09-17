export type PlanTier =
  | "free"
  | "business"
  | "pro"
  | "elite";

export type ThemeId =
  | "editorial"
  | "maison"
  | "commerce"
  | "studio"
  | "technical";

export type FontId =
  | "plex"
  | "tajawal"
  | "cairo"
  | "readex";

export type ProductCardStyle =
  | "minimal"
  | "editorial"
  | "commerce"
  | "compact"
  | "technical";

export type HeroLayout =
  | "single"
  | "wide-portrait"
  | "equal"
  | "focus-one"
  | "focus-two";

export type ProductSectionLayout =
  | "theme-default"
  | "grid-3"
  | "featured-grid"
  | "horizontal"
  | "spotlight";

export type StorySectionLayout =
  | "theme-default"
  | "split-start"
  | "split-end"
  | "centered"
  | "full-bleed";

export type ProductContentSource =
  | "catalog"
  | "category"
  | "manual";

export type CategoryContentSource =
  | "all"
  | "manual";

export type ExtraSectionType =
  | "categories"
  | "banner";

export type StorefrontSectionKey =
  | "categories"
  | "products"
  | "banner"
  | "story";

export interface HeroContent {
  eyebrow: string;
  title: string;
  description: string;

  primaryImage: string;
  secondaryImage: string;

  ctaLabel: string;
  ctaHref: string;
}

export interface CategorySectionContent {
  id: "categories";
  enabled: boolean;

  eyebrow: string;
  title: string;

  sourceType:
    CategoryContentSource;

  manualCategorySlugs:
    string[];

  itemLimit: number;
}

export interface ProductSectionContent {
  id: "products";
  enabled: boolean;

  eyebrow: string;
  title: string;

  layout:
    ProductSectionLayout;

  sourceType:
    ProductContentSource;

  categorySlug: string;

  manualProductIds:
    string[];

  itemLimit: number;
}

export interface BannerSectionContent {
  id: "banner";
  enabled: boolean;

  eyebrow: string;
  title: string;
  body: string;

  image: string;

  ctaLabel: string;
  ctaHref: string;
}

export interface StoryContent {
  id: "story";
  enabled: boolean;

  eyebrow: string;
  title: string;
  body: string;

  image: string;

  ctaLabel: string;
  ctaHref: string;

  layout:
    StorySectionLayout;
}

export interface StorefrontConfig {
  planTier: PlanTier;

  storeName: string;
  announcement: string;

  logoUrl: string;
  coverImageUrl: string;

  brandPrimaryColor: string;
  brandAccentColor: string;

  themeId: ThemeId;
  fontId: FontId;

  productCardStyle:
    ProductCardStyle;

  heroLayout:
    HeroLayout;

  hero:
    HeroContent;

  categorySection:
    CategorySectionContent;

  productSection:
    ProductSectionContent;

  bannerSection:
    BannerSectionContent;

  story:
    StoryContent;

  sectionOrder:
    StorefrontSectionKey[];
}