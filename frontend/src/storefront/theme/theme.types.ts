export type PlanTier =
  | "free"
  | "business"
  | "pro"
  | "elite";

export type VerticalThemeCode =
  | "general-retail"
  | "apparel"
  | "footwear"
  | "perfumes"
  | "electronics"
  | "subscriptions"
  | "services"
  | "car-rental"
  | "real-estate"
  | "restaurants"
  | "delivery-marketplace"
  | "grocery"
  | "automotive-parts"
  | "digital-products"
  | "jewelry-watches"
  | "furniture-decor"
  | "cosmetics"
  | "events-tickets"
  | "wholesale-b2b"
  | "personalized-gifts"
  | "equipment-rental"
  | "home-goods";

export type GeneratedVerticalThemeId =
  `${VerticalThemeCode}-${"signature" | "market"}`;

export type ThemeId =
  | "editorial"
  | "maison"
  | "commerce"
  | "studio"
  | "technical"
  | "mobile-flagship"
  | "mobile-smart-market"
  | GeneratedVerticalThemeId;

export type ThemeExperience =
  | "signature"
  | "market";

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
  | "technical"
  | "mobile-flagship"
  | "mobile-market"
  | "vertical-signature"
  | "vertical-market";

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
  heroFeaturedCaption?: string;
  heroStat1?: string;
  heroStat2?: string;
  heroStat3?: string;
  heroNote1Title?: string;
  heroNote1Body?: string;
  heroNote2Title?: string;
  heroNote2Body?: string;
  heroNote3Title?: string;
  heroNote3Body?: string;
  flagshipBand1Title?: string;
  flagshipBand1Body?: string;
  flagshipBand2Title?: string;
  flagshipBand2Body?: string;
  flagshipBand3Title?: string;
  flagshipBand3Body?: string;
  smartBand1Title?: string;
  smartBand1Body?: string;
  smartBand2Title?: string;
  smartBand2Body?: string;
  smartBand3Title?: string;
  smartBand3Body?: string;
  smartBand4Title?: string;
  smartBand4Body?: string;
  showcaseMode?: string;
  showcaseText?: string;
  showcaseImage?: string;
  featuredProductIds?: string;

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

  verticalCode:
    string | null;

  storeName: string;
  announcement: string;

  logoUrl: string;
  coverImageUrl: string;

  brandPrimaryColor: string;
  brandAccentColor: string;
  bodyTextColor?: string;
  primaryButtonColor?: string;
  primaryButtonTextColor?: string;
  accentButtonTextColor?: string;

  themeId: ThemeId;
  fontId: FontId;

  productCardStyle:
    ProductCardStyle;
  productCardStyleOverride?: boolean;
  productSectionLayoutOverride?: boolean;
  categoryCardLayout?: "theme-default" | "grid" | "compact";

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
