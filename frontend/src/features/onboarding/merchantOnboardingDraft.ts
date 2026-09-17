import type {
  OfoqPlan,
} from "./onboardingStorage";

import type {
  FontId,
  HeroLayout,
  ProductCardStyle,
  ThemeId,
} from "../../storefront/theme/theme.types";

export interface MerchantOnboardingDraft {
  storeName: string;
  verticalType: string;
  projectStage: string;
  operatingModel: string;
  fulfillment: string[];
  teamSize: string;
  monthlyOrders: string;
  themeStyleId: string;
  themeId: ThemeId;
  fontId: FontId;
  productCardStyle: ProductCardStyle;
  heroLayout: HeroLayout;
  slug: string;
  selectedPlan: OfoqPlan;
}

const STORAGE_KEY =
  "rukn.onboarding.merchant-draft.v2";

export const EMPTY_MERCHANT_ONBOARDING_DRAFT:
  MerchantOnboardingDraft = {
    storeName: "",
    verticalType: "GeneralRetail",
    projectStage: "existing",
    operatingModel: "inventory",
    fulfillment: ["delivery"],
    teamSize: "solo",
    monthlyOrders: "under-100",
    themeStyleId: "commerce-clear",
    themeId: "commerce",
    fontId: "plex",
    productCardStyle: "commerce",
    heroLayout: "single",
    slug: "",
    selectedPlan: "business",
  };

export function readMerchantOnboardingDraft():
  MerchantOnboardingDraft {
  try {
    const raw =
      window.localStorage.getItem(
        STORAGE_KEY,
      );

    if (!raw) {
      return {
        ...EMPTY_MERCHANT_ONBOARDING_DRAFT,
      };
    }

    const parsed =
      JSON.parse(raw) as
        Partial<MerchantOnboardingDraft>;

    return {
      ...EMPTY_MERCHANT_ONBOARDING_DRAFT,
      ...parsed,
      fulfillment:
        Array.isArray(
          parsed.fulfillment,
        )
          ? parsed.fulfillment
          : EMPTY_MERCHANT_ONBOARDING_DRAFT.fulfillment,
    };
  }
  catch {
    return {
      ...EMPTY_MERCHANT_ONBOARDING_DRAFT,
    };
  }
}

export function saveMerchantOnboardingDraft(
  draft: MerchantOnboardingDraft,
) {
  window.localStorage.setItem(
    STORAGE_KEY,
    JSON.stringify(draft),
  );
}

export function clearMerchantOnboardingDraft() {
  window.localStorage.removeItem(
    STORAGE_KEY,
  );
}
