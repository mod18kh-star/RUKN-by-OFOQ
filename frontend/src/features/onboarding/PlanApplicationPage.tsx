import {
  ArrowLeft,
  CheckCircle2,
  Clock3,
  ShieldCheck,
} from "lucide-react";

import {
  Navigate,
  useNavigate,
} from "react-router";

import {
  getPlanLabel,
  readSelectedBilling,
  readSelectedPlan,
} from "./onboardingStorage";

import {
  OnboardingProgress,
} from "./OnboardingProgress";

export function PlanApplicationPage() {
  const navigate =
    useNavigate();

  const plan =
    readSelectedPlan();

  const billing =
    readSelectedBilling();

  if (!plan) {
    return (
      <Navigate
        replace
        to="/start/plans"
      />
    );
  }

  const isPro =
    plan === "pro";

  return (
    <div
      dir="rtl"
      className="min-h-screen bg-[#f5f3ed] px-5 py-10 text-[#15211d]"
    >
      <main className="mx-auto max-w-[860px]">
        <OnboardingProgress
          current="plan"
        />

        <section className="mt-10 rounded-[20px] border border-black/[0.08] bg-white p-7 md:p-10">
          <div className="flex h-12 w-12 items-center justify-center rounded-[12px] bg-[#f0e7d8] text-[#8c6432]">
            <ShieldCheck
              size={23}
            />
          </div>

          <p className="mt-7 text-[10px] font-semibold tracking-[0.05em] text-[#91672f]">
            طلب اعتماد الباقة
          </p>

          <h1 className="mt-3 text-[30px] font-semibold tracking-[-0.04em] md:text-[40px]">
            {isPro
              ? "تقديم طلب باقة Pro"
              : `متابعة طلب ${getPlanLabel(plan)}`}
          </h1>

          <p className="mt-4 max-w-2xl text-[13px] leading-7 text-black/55">
            رح نطلب منك بيانات المتجر الأساسية، وبعد إرسال الطلب
            يوصل مباشرة إلى إدارة RUKN للمراجعة والاعتماد.
          </p>

          <div className="mt-8 grid gap-3 md:grid-cols-3">
            <InfoItem
              icon={
                <CheckCircle2
                  size={17}
                />
              }
              title="الباقة"
              value={getPlanLabel(plan)}
            />

            <InfoItem
              icon={
                <Clock3
                  size={17}
                />
              }
              title="الفوترة"
              value={
                billing === "monthly"
                  ? "شهري"
                  : "سنوي"
              }
            />

            <InfoItem
              icon={
                <ShieldCheck
                  size={17}
                />
              }
              title="المراجعة"
              value="خلال 24 ساعة"
            />
          </div>

          <div className="mt-8 rounded-[14px] border border-[#d7c3a5] bg-[#fbf7f0] p-5">
            <p className="text-[12px] font-semibold">
              بعد الإرسال
            </p>

            <p className="mt-2 text-[11px] leading-6 text-black/50">
              رح تظهر لك شاشة تأكيد واضحة بأن الطلب قيد المراجعة،
              وما رح نرجعك لبداية التسجيل أو نطلب منك اختيار الباقة من جديد.
            </p>
          </div>

          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <button
              type="button"
              onClick={() =>
                navigate(
                  "/start/store",
                )
              }
              className="inline-flex h-12 items-center justify-center gap-2 rounded-[9px] bg-[#080b14] px-6 text-[12px] font-semibold text-white"
            >
              متابعة وتقديم الطلب
              <ArrowLeft
                size={16}
              />
            </button>

            <button
              type="button"
              onClick={() =>
                navigate(
                  "/start/plans",
                )
              }
              className="inline-flex h-12 items-center justify-center rounded-[9px] border border-black/10 px-6 text-[12px] font-semibold"
            >
              تغيير الباقة
            </button>
          </div>
        </section>
      </main>
    </div>
  );
}

function InfoItem({
  icon,
  title,
  value,
}: {
  icon: React.ReactNode;
  title: string;
  value: string;
}) {
  return (
    <div className="rounded-[13px] border border-black/[0.07] bg-[#faf9f6] p-4">
      <div className="text-[#9a713f]">
        {icon}
      </div>

      <p className="mt-4 text-[9px] font-medium text-black/40">
        {title}
      </p>

      <p className="mt-1 text-[13px] font-semibold">
        {value}
      </p>
    </div>
  );
}
