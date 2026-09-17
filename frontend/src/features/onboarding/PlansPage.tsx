import {
  useState,
} from "react";

import {
  ArrowLeft,
  Check,
  MapPin,
} from "lucide-react";

import {
  Link,
  useNavigate,
} from "react-router";

import {
  detectPricingCountry,
  formatPrice,
  getCountryLabel,
  getPlanPrice,
} from "./pricing";

import {
  getPlanLabel,
  readPricingCountry,
  readRecommendedPlan,
  savePricingCountry,
  saveSelectedBilling,
  saveSelectedPlan,
  type OfoqPlan,
  type PricingCountry,
} from "./onboardingStorage";

import {
  OnboardingProgress,
} from "./OnboardingProgress";

const plans: {
  id: OfoqPlan;
  eyebrow: string;
  description: string;
  points: string[];
}[] = [
  {
    id: "business",
    eyebrow:
      "بداية مرتبة",
    description:
      "للشخص أو الفريق الصغير اللي يحتاج كل أساسيات المتجر بدون تعقيد",
    points: [
      "المنتجات والطلبات والمخزون",
      "تصميم احترافي جاهز",
      "أدوات الإدارة الأساسية",
    ],
  },

  {
    id: "pro",
    eyebrow:
      "للنشاط اللي عم يكبر",
    description:
      "تحكم أوسع بالتصميم والإدارة ومناسب للمتاجر اللي عندها فريق أو مبيعات أعلى",
    points: [
      "كل ما في Business",
      "خيارات تصميم وتخصيص أوسع",
      "أدوات إضافية للنمو",
    ],
  },

  {
    id: "extra",
    eyebrow:
      "احتياجات أكبر",
    description:
      "للفرق والأنشطة اللي تحتاج إعداد أوسع وحلول أكثر تخصيصًا",
    points: [
      "كل ما في Pro",
      "مرونة أعلى",
      "إعداد يناسب الاحتياجات المتقدمة",
    ],
  },
];

export function PlansPage() {
  const navigate =
    useNavigate();

  const recommendedPlan =
    readRecommendedPlan();

  const [billing, setBilling] =
    useState<
      "monthly" | "annual"
    >("annual");

  const [country, setCountry] =
    useState<PricingCountry>(
      () =>
        readPricingCountry() ??
        detectPricingCountry(),
    );

  function changeCountry(
    next:
      PricingCountry,
  ) {
    setCountry(next);

    savePricingCountry(
      next,
    );
  }

  function selectPlan(
    plan: OfoqPlan,
  ) {
    saveSelectedPlan(
      plan,
    );

    saveSelectedBilling(
      billing,
    );

    navigate(
      plan === "pro"
        ? "/start/application"
        : "/start/store",
    );
  }

  return (
    <div
      dir="rtl"
      className="min-h-screen bg-[#f5f3ed] text-[#15211d]"
    >
      <header className="border-b border-black/[0.07]">
        <div className="mx-auto flex min-h-[70px] max-w-[1260px] items-center justify-between gap-4 px-5 py-3 md:px-8">
          <Link
            to="/"
            className="text-[21px] font-bold tracking-[-0.055em]"
          >
            OFOQ
          </Link>

          <div className="flex items-center gap-2 text-[10px] text-black/45">
            <MapPin
              size={13}
            />

            <span>
              تم تحديد بلدك تلقائيًا
            </span>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-[1260px] px-5 py-9 md:px-8 md:py-12">
        <OnboardingProgress
          current="plan"
        />

        <div className="mx-auto mt-10 max-w-[760px] text-center">
          <p className="text-[10px] font-semibold text-[#9a713f]">
            اختر اللي يناسبك الآن
          </p>

          <h1 className="mt-3 text-[34px] font-semibold tracking-[-0.045em] md:text-[44px]">
            باقات واضحة حسب بلدك
          </h1>

          <p className="mx-auto mt-3 max-w-xl text-[12px] leading-7 text-black/50">
            تقدر تبدأ بالأنسب لك
            وتغيّر خطتك لاحقًا مع
            نمو متجرك
          </p>
        </div>

        <div className="mt-8 flex flex-col items-center justify-center gap-4">
          <div className="inline-flex rounded-[10px] bg-white p-1 shadow-sm">
            <button
              type="button"
              onClick={() =>
                setBilling(
                  "monthly",
                )
              }
              className={[
                "h-9 rounded-[8px] px-5 text-[11px] font-semibold",
                billing ===
                "monthly"
                  ? "bg-[#080b14] text-white"
                  : "text-black/45",
              ].join(" ")}
            >
              شهري
            </button>

            <button
              type="button"
              onClick={() =>
                setBilling(
                  "annual",
                )
              }
              className={[
                "h-9 rounded-[8px] px-5 text-[11px] font-semibold",
                billing ===
                "annual"
                  ? "bg-[#080b14] text-white"
                  : "text-black/45",
              ].join(" ")}
            >
              سنوي
            </button>
          </div>

          <div className="flex flex-wrap items-center justify-center gap-2">
            {(
              [
                "SA",
                "AE",
                "SY",
                "OTHER",
              ] as PricingCountry[]
            ).map(
              (item) => (
                <button
                  key={item}
                  type="button"
                  onClick={() =>
                    changeCountry(
                      item,
                    )
                  }
                  className={[
                    "rounded-full border px-3 py-2 text-[10px] font-medium",
                    country === item
                      ? "border-[#080b14] bg-[#f0e7d8] text-[#080b14]"
                      : "border-black/10 bg-white text-black/45",
                  ].join(" ")}
                >
                  {getCountryLabel(
                    item,
                  )}
                </button>
              ),
            )}
          </div>
        </div>

        <div className="mt-9 grid gap-4 lg:grid-cols-3">
          {plans.map(
            (plan) => {
              const price =
                getPlanPrice(
                  country,
                  plan.id,
                );

              const value =
                billing ===
                "annual"
                  ? price.annual
                  : price.monthly;

              const recommended =
                recommendedPlan ===
                plan.id;

              const annualSaving =
                price.monthly !==
                  null &&
                price.annual !==
                  null
                  ? price.monthly *
                      12 -
                    price.annual
                  : 0;

              return (
                <article
                  key={plan.id}
                  className={[
                    "relative flex min-h-[430px] flex-col rounded-[16px] border bg-white p-7",
                    recommended
                      ? "border-[#080b14] shadow-[0_20px_55px_rgba(27,65,53,0.10)]"
                      : "border-black/[0.08]",
                  ].join(" ")}
                >
                  {recommended ? (
                    <span className="absolute left-5 top-5 rounded-full bg-[#f0e7d8] px-3 py-1 text-[9px] font-semibold text-[#080b14]">
                      اقتراحنا لك
                    </span>
                  ) : null}

                  <p className="text-[10px] font-medium text-black/40">
                    {
                      plan.eyebrow
                    }
                  </p>

                  <h2 className="mt-3 text-[28px] font-semibold tracking-[-0.04em]">
                    {getPlanLabel(
                      plan.id,
                    )}
                  </h2>

                  <div className="mt-6 min-h-[84px] border-y border-black/[0.07] py-5">
                    {value !==
                    null ? (
                      <>
                        <div className="flex items-end gap-2">
                          <span className="text-[30px] font-semibold tracking-[-0.04em]">
                            {formatPrice(
                              value,
                            )}
                          </span>

                          <span className="mb-1 text-[11px] text-black/45">
                            {
                              price.currencyLabel
                            }
                          </span>
                        </div>

                        <p className="mt-1 text-[10px] text-black/40">
                          {billing ===
                          "annual"
                            ? "للسنة كاملة"
                            : "شهريًا"}
                        </p>

                        {billing ===
                          "annual" &&
                        annualSaving >
                          0 ? (
                          <p className="mt-2 text-[10px] font-semibold text-[#9a713f]">
                            توفر{" "}
                            {formatPrice(
                              annualSaving,
                            )}{" "}
                            {
                              price.currencyLabel
                            }{" "}
                            مقارنة بالدفع
                            الشهري
                          </p>
                        ) : null}
                      </>
                    ) : (
                      <>
                        <div className="text-[22px] font-semibold">
                          حسب احتياجك
                        </div>

                        <p className="mt-2 text-[10px] text-black/40">
                          السعر يحدد
                          بعد معرفة
                          متطلباتك
                        </p>
                      </>
                    )}
                  </div>

                  <p className="mt-5 min-h-[66px] text-[12px] leading-6 text-black/50">
                    {
                      plan.description
                    }
                  </p>

                  <div className="mt-5 space-y-3">
                    {plan.points.map(
                      (point) => (
                        <div
                          key={
                            point
                          }
                          className="flex items-center gap-2 text-[11px]"
                        >
                          <Check
                            size={14}
                            className="text-[#080b14]"
                          />
                          {point}
                        </div>
                      ),
                    )}
                  </div>

                  <button
                    type="button"
                    onClick={() =>
                      selectPlan(
                        plan.id,
                      )
                    }
                    className={[
                      "mt-auto flex h-11 w-full items-center justify-center gap-2 rounded-[8px] text-[12px] font-semibold transition",
                      recommended
                        ? "bg-[#080b14] text-white"
                        : "border border-black/15 hover:border-black/30",
                    ].join(" ")}
                  >
                    {plan.id === "pro"
                      ? "تقديم طلب "
                      : "اختيار "}
                    {getPlanLabel(
                      plan.id,
                    )}

                    <ArrowLeft
                      size={15}
                    />
                  </button>
                </article>
              );
            },
          )}
        </div>

        <p className="mx-auto mt-6 max-w-xl text-center text-[10px] leading-5 text-black/35">
          الأسعار المعروضة مبنية
          على البلد المحدد
          ويمكن تصحيح البلد
          يدويًا إذا كان تحديد
          المتصفح غير دقيق
        </p>
      </main>
    </div>
  );
}