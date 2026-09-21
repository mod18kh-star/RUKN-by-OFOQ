export interface StorefrontVisualContent {
  bodyTextColor: string;
  primaryButtonColor: string;
  primaryButtonTextColor: string;
  accentButtonTextColor: string;
  heroEyebrow: string;
  heroTitle: string;
  heroDescription: string;
  heroCtaLabel: string;
  heroCtaHref: string;
  heroSecondaryImage: string;
  categoryEyebrow: string;
  productEyebrow: string;
  footerDescription: string;
  sectionOrder: string;
  heroFeaturedCaption: string;
  heroStat1: string;
  heroStat2: string;
  heroStat3: string;
  heroNote1Title: string;
  heroNote1Body: string;
  heroNote2Title: string;
  heroNote2Body: string;
  heroNote3Title: string;
  heroNote3Body: string;
  flagshipBand1Title: string;
  flagshipBand1Body: string;
  flagshipBand2Title: string;
  flagshipBand2Body: string;
  flagshipBand3Title: string;
  flagshipBand3Body: string;
  smartBand1Title: string;
  smartBand1Body: string;
  smartBand2Title: string;
  smartBand2Body: string;
  smartBand3Title: string;
  smartBand3Body: string;
  smartBand4Title: string;
  smartBand4Body: string;
  heroShowcaseMode: string;
  heroShowcaseText: string;
  heroShowcaseImage: string;
  featuredProductIds: string;
  productLayout: string;
  productCardStyle: string;
  categoryCardLayout: string;
}

export const EMPTY_VISUAL_CONTENT: StorefrontVisualContent = {
  bodyTextColor: "", primaryButtonColor: "", primaryButtonTextColor: "", accentButtonTextColor: "",
  heroEyebrow: "", heroTitle: "", heroDescription: "",
  heroCtaLabel: "", heroCtaHref: "", heroSecondaryImage: "",
  categoryEyebrow: "", productEyebrow: "", footerDescription: "",
  sectionOrder: "categories,products,banner,story",
  heroFeaturedCaption: "",
  heroStat1: "",
  heroStat2: "",
  heroStat3: "",
  heroNote1Title: "",
  heroNote1Body: "",
  heroNote2Title: "",
  heroNote2Body: "",
  heroNote3Title: "",
  heroNote3Body: "",
  flagshipBand1Title: "",
  flagshipBand1Body: "",
  flagshipBand2Title: "",
  flagshipBand2Body: "",
  flagshipBand3Title: "",
  flagshipBand3Body: "",
  smartBand1Title: "",
  smartBand1Body: "",
  smartBand2Title: "",
  smartBand2Body: "",
  smartBand3Title: "",
  smartBand3Body: "",
  smartBand4Title: "",
  smartBand4Body: "",
  heroShowcaseMode: "default",
  heroShowcaseText: "",
  heroShowcaseImage: "",
  featuredProductIds: "",
  productLayout: "theme-default",
  productCardStyle: "theme-default",
  categoryCardLayout: "theme-default",
};

export function parseVisualContent(json: string | null | undefined): StorefrontVisualContent {
  if (!json) return { ...EMPTY_VISUAL_CONTENT };
  try {
    const value: unknown = JSON.parse(json);
    if (!value || typeof value !== "object" || Array.isArray(value)) {
      return { ...EMPTY_VISUAL_CONTENT };
    }
    const source = value as Record<string, unknown>;
    const result = { ...EMPTY_VISUAL_CONTENT };
    for (const key of Object.keys(result) as (keyof StorefrontVisualContent)[]) {
      const field = source[key];
      if (typeof field === "string") result[key] = field;
    }
    return result;
  } catch {
    return { ...EMPTY_VISUAL_CONTENT };
  }
}
