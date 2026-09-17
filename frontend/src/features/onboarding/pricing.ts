import type {
  OfoqPlan,
  PricingCountry,
} from "./onboardingStorage";

export interface LocalPlanPrice {
  monthly: number | null;
  annual: number | null;
  currencyCode: string;
  currencyLabel: string;
  countryLabel: string;
}

const prices: Record<
  PricingCountry,
  Record<OfoqPlan, LocalPlanPrice>
> = {
  SA: {
    business: {
      monthly: 55,
      annual: 600,
      currencyCode: "SAR",
      currencyLabel: "ر.س",
      countryLabel: "السعودية",
    },

    pro: {
      monthly: 179.4,
      annual: 1794,
      currencyCode: "SAR",
      currencyLabel: "ر.س",
      countryLabel: "السعودية",
    },

    extra: {
      monthly: null,
      annual: null,
      currencyCode: "SAR",
      currencyLabel: "ر.س",
      countryLabel: "السعودية",
    },
  },

  AE: {
    business: {
      monthly: 55,
      annual: 600,
      currencyCode: "AED",
      currencyLabel: "د.إ",
      countryLabel: "الإمارات",
    },

    pro: {
      monthly: 179.4,
      annual: 1794,
      currencyCode: "AED",
      currencyLabel: "د.إ",
      countryLabel: "الإمارات",
    },

    extra: {
      monthly: null,
      annual: null,
      currencyCode: "AED",
      currencyLabel: "د.إ",
      countryLabel: "الإمارات",
    },
  },

  SY: {
    business: {
      monthly: 10,
      annual: 100,
      currencyCode: "USD",
      currencyLabel: "$",
      countryLabel: "سوريا",
    },

    pro: {
      monthly: 30,
      annual: 300,
      currencyCode: "USD",
      currencyLabel: "$",
      countryLabel: "سوريا",
    },

    extra: {
      monthly: null,
      annual: null,
      currencyCode: "USD",
      currencyLabel: "$",
      countryLabel: "سوريا",
    },
  },

  OTHER: {
    business: {
      monthly: null,
      annual: null,
      currencyCode: "USD",
      currencyLabel: "$",
      countryLabel: "دولة أخرى",
    },

    pro: {
      monthly: null,
      annual: null,
      currencyCode: "USD",
      currencyLabel: "$",
      countryLabel: "دولة أخرى",
    },

    extra: {
      monthly: null,
      annual: null,
      currencyCode: "USD",
      currencyLabel: "$",
      countryLabel: "دولة أخرى",
    },
  },
};

export function detectPricingCountry():
  PricingCountry {
  const timezone =
    Intl.DateTimeFormat()
      .resolvedOptions()
      .timeZone;

  if (
    timezone === "Asia/Riyadh"
  ) {
    return "SA";
  }

  if (
    timezone === "Asia/Dubai"
  ) {
    return "AE";
  }

  if (
    timezone === "Asia/Damascus"
  ) {
    return "SY";
  }

  const languages =
    navigator.languages?.length
      ? navigator.languages
      : [navigator.language];

  for (const language of languages) {
    const normalized =
      language.toUpperCase();

    if (
      normalized.endsWith("-SA")
    ) {
      return "SA";
    }

    if (
      normalized.endsWith("-AE")
    ) {
      return "AE";
    }

    if (
      normalized.endsWith("-SY")
    ) {
      return "SY";
    }
  }

  return "OTHER";
}

export function getPlanPrice(
  country: PricingCountry,
  plan: OfoqPlan,
) {
  return prices[country][plan];
}

export function getCountryLabel(
  country: PricingCountry,
) {
  return prices[country]
    .business
    .countryLabel;
}

export function formatPrice(
  value: number,
) {
  return new Intl.NumberFormat(
    "ar",
    {
      maximumFractionDigits: 2,
    },
  ).format(value);
}