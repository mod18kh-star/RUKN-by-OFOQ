export type OfoqPlan =
  | "business"
  | "pro"
  | "extra";

export type BillingCycle =
  | "monthly"
  | "annual";

export type OnboardingAnswers =
  Record<string, string[]>;

export type PricingCountry =
  | "SA"
  | "AE"
  | "SY"
  | "OTHER";

const PLAN_KEY =
  "ofoq.onboarding.plan";

const BILLING_KEY =
  "ofoq.onboarding.billing";

const ANSWERS_KEY =
  "ofoq.onboarding.answers";

const RECOMMENDED_VERTICAL_KEY =
  "ofoq.onboarding.recommended-vertical";

const RECOMMENDED_PLAN_KEY =
  "ofoq.onboarding.recommended-plan";

const COUNTRY_KEY =
  "ofoq.onboarding.pricing-country";

export function saveSelectedPlan(
  plan: OfoqPlan,
) {
  window.localStorage.setItem(
    PLAN_KEY,
    plan,
  );
}

export function saveSelectedBilling(
  billing: BillingCycle,
) {
  window.localStorage.setItem(
    BILLING_KEY,
    billing,
  );
}

export function readSelectedBilling():
  BillingCycle | null {
  const value =
    window.localStorage.getItem(
      BILLING_KEY,
    );

  return value === "monthly" ||
    value === "annual"
      ? value
      : null;
}

export function readSelectedPlan():
  OfoqPlan | null {
  const value =
    window.localStorage.getItem(
      PLAN_KEY,
    );

  if (
    value === "business" ||
    value === "pro" ||
    value === "extra"
  ) {
    return value;
  }

  return null;
}

export function getPlanLabel(
  plan: OfoqPlan | null,
) {
  switch (plan) {
    case "business":
      return "Business";

    case "pro":
      return "Pro";

    case "extra":
      return "Extra";

    default:
      return "لم يتم الاختيار";
  }
}

export function readAnswers():
  OnboardingAnswers {
  const raw =
    window.localStorage.getItem(
      ANSWERS_KEY,
    );

  if (!raw) {
    return {};
  }

  try {
    const parsed =
      JSON.parse(raw) as unknown;

    if (
      typeof parsed !== "object" ||
      parsed === null
    ) {
      return {};
    }

    return parsed as OnboardingAnswers;
  } catch {
    return {};
  }
}

export function saveAnswers(
  answers: OnboardingAnswers,
) {
  window.localStorage.setItem(
    ANSWERS_KEY,
    JSON.stringify(answers),
  );
}

export function saveAnswer(
  questionId: string,
  values: string[],
) {
  const answers =
    readAnswers();

  answers[questionId] =
    values;

  saveAnswers(answers);
}

export function saveRecommendedVertical(
  verticalType: string,
) {
  window.localStorage.setItem(
    RECOMMENDED_VERTICAL_KEY,
    verticalType,
  );
}

export function readRecommendedVertical() {
  return (
    window.localStorage.getItem(
      RECOMMENDED_VERTICAL_KEY,
    ) ?? ""
  );
}

export function saveRecommendedPlan(
  plan: OfoqPlan,
) {
  window.localStorage.setItem(
    RECOMMENDED_PLAN_KEY,
    plan,
  );
}

export function readRecommendedPlan():
  OfoqPlan | null {
  const value =
    window.localStorage.getItem(
      RECOMMENDED_PLAN_KEY,
    );

  if (
    value === "business" ||
    value === "pro" ||
    value === "extra"
  ) {
    return value;
  }

  return null;
}

export function savePricingCountry(
  country: PricingCountry,
) {
  window.localStorage.setItem(
    COUNTRY_KEY,
    country,
  );
}

export function readPricingCountry():
  PricingCountry | null {
  const value =
    window.localStorage.getItem(
      COUNTRY_KEY,
    );

  if (
    value === "SA" ||
    value === "AE" ||
    value === "SY" ||
    value === "OTHER"
  ) {
    return value;
  }

  return null;
}