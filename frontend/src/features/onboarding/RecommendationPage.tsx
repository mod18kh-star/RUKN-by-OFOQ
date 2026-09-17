import {
  ArrowLeft,
  Check,
  Sparkles,
} from "lucide-react";

import {
  Link,
  useNavigate,
} from "react-router";

import {
  buildRecommendation,
} from "./recommendations";

import {
  getPlanLabel,
  readAnswers,
  saveRecommendedPlan,
  saveRecommendedVertical,
} from "./onboardingStorage";

import {
  OnboardingProgress,
} from "./OnboardingProgress";

export function RecommendationPage() {
  const navigate =
    useNavigate();

  const answers =
    readAnswers();

  const recommendation =
    buildRecommendation(
      answers,
    );

  function continueToPlans() {
    saveRecommendedVertical(
      recommendation.primary
        .type,
    );

    saveRecommendedPlan(
      recommendation.plan,
    );

    navigate(
      "/start/plans",
    );
  }

  return (
    <div
      dir="rtl"
      className="min-h-screen bg-[#f5f3ed] text-[#15211d]"
    >
      <header className="border-b border-black/[0.07]">
        <div className="mx-auto flex h-[70px] max-w-[1180px] items-center justify-between px-5 md:px-8">
          <Link
            to="/"
            className="text-[21px] font-bold tracking-[-0.055em]"
          >
            OFOQ
          </Link>
        </div>
      </header>

      <main className="px-5 py-9 md:py-12">
        <OnboardingProgress
          current="recommendation"
        />

        <div className="mx-auto mt-10 max-w-[900px]">
          <div className="text-center">
            <div className="mx-auto flex size-11 items-center justify-center rounded-full bg-[#f0e7d8] text-[#080b14]">
              <Sparkles
                size={18}
              />
            </div>

            <h1 className="mt-5 text-[34px] font-semibold tracking-[-0.045em] md:text-[43px]">
              هيك فهمنا مشروعك
            </h1>

            <p className="mx-auto mt-3 max-w-[570px] text-[12px] leading-7 text-black/50">
              هذا اقتراح بداية فقط
              ونقدر نعدله قبل إنشاء
              المتجر
            </p>
          </div>

          <div className="mt-9 grid gap-4 lg:grid-cols-[1.08fr_.92fr]">
            <section className="rounded-[16px] border border-[#080b14]/25 bg-white p-7">
              <p className="text-[10px] font-semibold text-[#9a713f]">
                نوع المتجر الأقرب لك
              </p>

              <h2 className="mt-3 text-[28px] font-semibold tracking-[-0.04em]">
                {
                  recommendation
                    .primary.label
                }
              </h2>

              <p className="mt-1 text-[10px] text-black/35">
                {
                  recommendation
                    .primary.code
                }
              </p>

              <div className="mt-7 space-y-3 border-t border-black/[0.07] pt-6">
                {recommendation.reasons.map(
                  (reason) => (
                    <div
                      key={reason}
                      className="flex items-start gap-2 text-[12px] leading-6 text-black/65"
                    >
                      <Check
                        size={14}
                        className="mt-1 shrink-0 text-[#080b14]"
                      />
                      {reason}
                    </div>
                  ),
                )}
              </div>

              {recommendation
                .alternatives
                .length > 0 ? (
                <div className="mt-7">
                  <p className="text-[10px] font-semibold text-black/40">
                    وممكن يناسبك أيضًا
                  </p>

                  <div className="mt-3 flex flex-wrap gap-2">
                    {recommendation.alternatives.map(
                      (item) => (
                        <span
                          key={
                            item.type
                          }
                          className="rounded-full bg-[#f2f3f0] px-3 py-2 text-[10px] font-medium"
                        >
                          {
                            item.label
                          }
                        </span>
                      ),
                    )}
                  </div>
                </div>
              ) : null}
            </section>

            <section className="rounded-[16px] border border-black/[0.08] bg-[#080b14] p-7 text-white">
              <p className="text-[10px] font-semibold text-white/55">
                الباقة اللي نتوقع تناسبك
              </p>

              <h2 className="mt-3 text-[29px] font-semibold">
                {getPlanLabel(
                  recommendation.plan,
                )}
              </h2>

              <p className="mt-3 text-[11px] leading-6 text-white/60">
                مو إلزامية
                بتشوف كل الباقات
                وأسعار بلدك بالخطوة
                الجاية
              </p>

              {recommendation
                .services
                .length > 0 ? (
                <>
                  <div className="my-6 h-px bg-white/15" />

                  <p className="text-[10px] font-semibold text-white/55">
                    أشياء ممكن تحتاجها
                  </p>

                  <div className="mt-4 space-y-3">
                    {recommendation.services.map(
                      (service) => (
                        <div
                          key={
                            service
                          }
                          className="text-[11px] leading-5 text-white/80"
                        >
                          • {service}
                        </div>
                      ),
                    )}
                  </div>
                </>
              ) : null}
            </section>
          </div>

          <div className="mt-6 flex flex-wrap items-center justify-between gap-3">
            <Link
              to="/start/discovery"
              className="px-2 text-[11px] font-semibold text-black/50"
            >
              تعديل إجاباتي
            </Link>

            <button
              type="button"
              onClick={
                continueToPlans
              }
              className="inline-flex h-12 items-center gap-3 rounded-[8px] bg-[#080b14] px-6 text-[12px] font-semibold text-white"
            >
              شوف الباقات والأسعار
              <ArrowLeft
                size={15}
              />
            </button>
          </div>
        </div>
      </main>
    </div>
  );
}