interface OnboardingProgressProps {
  current:
    | "account"
    | "project"
    | "recommendation"
    | "plan"
    | "store"
    | "theme";
}

const steps = [
  {
    key: "account",
    label: "الحساب",
  },
  {
    key: "project",
    label: "مشروعك",
  },
  {
    key: "recommendation",
    label: "الاقتراح",
  },
  {
    key: "plan",
    label: "الباقة",
  },
  {
    key: "store",
    label: "المتجر",
  },
  {
    key: "theme",
    label: "التصميم",
  },
] as const;

export function OnboardingProgress({
  current,
}: OnboardingProgressProps) {
  const currentIndex =
    steps.findIndex(
      (step) =>
        step.key === current,
    );

  const progress =
    ((currentIndex + 1) /
      steps.length) *
    100;

  return (
    <div className="mx-auto w-full max-w-[720px]">
      <div className="mb-3 flex items-center justify-between text-[10px] font-medium text-black/40">
        <span>
          {steps[currentIndex]
            ?.label}
        </span>

        <span dir="ltr">
          {currentIndex + 1} /{" "}
          {steps.length}
        </span>
      </div>

      <div className="h-[3px] overflow-hidden rounded-full bg-black/[0.08]">
        <div
          className="h-full rounded-full bg-[#080b14] transition-[width] duration-300"
          style={{
            width:
              `${progress}%`,
          }}
        />
      </div>
    </div>
  );
}