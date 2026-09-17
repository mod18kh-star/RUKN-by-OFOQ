import {
  ArrowLeft,
  ArrowRight,
  Check,
  CheckCircle2,
  ChevronLeft,
  CircleAlert,
  LoaderCircle,
  Palette,
  Search,
  ShieldCheck,
  Sparkles,
} from "lucide-react";

import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  useNavigate,
} from "react-router";

import {
  readAdminStore,
  saveAdminStore,
} from "../admin/store-setup/storeSetupStorage";

import {
  checkTenantSlugAvailability,
  configurePrimaryVertical,
  createTenant,
  StoreSetupApiError,
} from "../admin/store-setup/storeSetupApi";

import {
  findVertical,
  VERTICAL_OPTIONS,
} from "../admin/store-setup/verticalCatalog";

import {
  MerchantRequestApiError,
  submitRegistrationRequest,
} from "../admin/requests/merchantRequestsApi";

import {
  loadStorefrontConfig,
  saveStorefrontConfig,
} from "../../storefront/config/storefrontConfig";

import {
  getPlanLabel,
  saveRecommendedPlan,
  saveRecommendedVertical,
  saveSelectedBilling,
  saveSelectedPlan,
  type OfoqPlan,
} from "./onboardingStorage";

import {
  clearMerchantOnboardingDraft,
  readMerchantOnboardingDraft,
  saveMerchantOnboardingDraft,
  type MerchantOnboardingDraft,
} from "./merchantOnboardingDraft";

import {
  getThemeStyleById,
  getThemeStylesForVertical,
  type OnboardingThemeStyle,
} from "./verticalThemeCatalog";

import {
  resolvePostAuthDestination,
} from "../auth/postAuth";

const STEP_LABELS = [
  "المتجر",
  "النشاط",
  "الأسئلة",
  "الهوية",
  "الرابط",
  "الباقة",
  "المراجعة",
];

const PROJECT_STAGE_OPTIONS = [
  {
    value: "new",
    label: "مشروع جديد",
    description: "لسه عم أجهز البداية.",
  },
  {
    value: "informal",
    label: "أبيع بشكل بسيط",
    description: "واتساب أو سوشيال بدون متجر متكامل.",
  },
  {
    value: "existing",
    label: "نشاط قائم",
    description: "عندي مبيعات وعملاء اليوم.",
  },
  {
    value: "migration",
    label: "أنقل من منصة ثانية",
    description: "عندي متجر وأفكر بالانتقال إلى RUKN.",
  },
];

const TEAM_OPTIONS = [
  { value: "solo", label: "أنا وحدي" },
  { value: "small", label: "2–5 أشخاص" },
  { value: "medium", label: "6–20 شخص" },
  { value: "large", label: "أكثر من 20" },
];

const ORDER_OPTIONS = [
  { value: "under-100", label: "أقل من 100 طلب" },
  { value: "100-500", label: "100–500 طلب" },
  { value: "500-2000", label: "500–2000 طلب" },
  { value: "2000-plus", label: "أكثر من 2000" },
];

const FULFILLMENT_OPTIONS = [
  { value: "delivery", label: "توصيل للعميل" },
  { value: "pickup", label: "استلام من الموقع" },
  { value: "branches", label: "أكثر من فرع" },
  { value: "digital", label: "تسليم رقمي" },
];

const PLANS: Array<{
  id: OfoqPlan;
  label: string;
  eyebrow: string;
  description: string;
  points: string[];
}> = [
  {
    id: "business",
    label: "Business",
    eyebrow: "بداية عملية",
    description:
      "للأنشطة الصغيرة اللي تحتاج إدارة مرتبة بدون تعقيد.",
    points: [
      "منتجات وطلبات ومخزون",
      "ثيمات أساسية احترافية",
      "تقارير وتشغيل أساسي",
    ],
  },
  {
    id: "pro",
    label: "Pro",
    eyebrow: "للنمو",
    description:
      "للمتجر اللي يحتاج تخصيص أوسع وفريق وتشغيل أقوى.",
    points: [
      "كل Business",
      "خيارات تصميم وتشغيل أوسع",
      "مرونة أكبر للفريق والنمو",
    ],
  },
  {
    id: "extra",
    label: "Extra",
    eyebrow: "تشغيل متقدم",
    description:
      "للأنشطة الأعلى حجمًا أو متعددة الفروع والاحتياجات.",
    points: [
      "كل Pro",
      "مرونة تشغيلية أعلى",
      "مساحة أكبر للتخصيص",
    ],
  },
];

export function MerchantOnboardingPage() {
  const navigate =
    useNavigate();

  const [draft, setDraft] =
    useState<MerchantOnboardingDraft>(
      () => {
        const storedDraft =
          readMerchantOnboardingDraft();

        const storedStore =
          readAdminStore();

        if (!storedStore) {
          return storedDraft;
        }

        return {
          ...storedDraft,
          storeName:
            storedStore.name,
          slug:
            storedStore.slug,
        };
      },
    );

  const [step, setStep] =
    useState(0);

  const [verticalQuery, setVerticalQuery] =
    useState("");

  const [slugState, setSlugState] =
    useState<
      "idle" | "checking" | "available" | "taken" | "invalid" | "error"
    >("idle");

  const [submitting, setSubmitting] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const existingStore =
    readAdminStore();

  useEffect(
    () => {
      saveMerchantOnboardingDraft(
        draft,
      );
    },
    [draft],
  );

  useEffect(
    () => {
      if (
        step !== 4 ||
        existingStore
      ) {
        return;
      }

      const cleanSlug =
        slugify(draft.slug);

      if (cleanSlug.length < 2) {
        return;
      }

      let active = true;

      const timer =
        window.setTimeout(
          () => {
            if (!active) {
              return;
            }

            setSlugState("checking");

            void checkTenantSlugAvailability(
              cleanSlug,
            )
              .then(
                (result) => {
                  if (!active) {
                    return;
                  }

                  setSlugState(
                    result.available
                      ? "available"
                      : "taken",
                  );
                },
              )
              .catch(
                () => {
                  if (active) {
                    setSlugState(
                      "error",
                    );
                  }
                },
              );
          },
          450,
        );

      return () => {
        active = false;
        window.clearTimeout(timer);
      };
    }, [draft.slug, existingStore, step]);

  const filteredVerticals =
    useMemo(
      () => {
        const normalized =
          verticalQuery
            .trim()
            .toLowerCase();

        if (!normalized) {
          return VERTICAL_OPTIONS;
        }

        return VERTICAL_OPTIONS.filter(
          (item) =>
            item.label
              .toLowerCase()
              .includes(normalized) ||
            item.description
              .toLowerCase()
              .includes(normalized) ||
            item.code
              .toLowerCase()
              .includes(normalized),
        );
      },
      [verticalQuery],
    );

  const selectedVertical =
    findVertical(
      draft.verticalType,
    );

  const themeStyles =
    getThemeStylesForVertical(
      draft.verticalType,
    );

  const recommendedPlan =
    useMemo(
      () =>
        recommendPlan(
          draft,
        ),
      [draft],
    );

  function updateDraft(
    patch: Partial<MerchantOnboardingDraft>,
  ) {
    setDraft(
      (current) => ({
        ...current,
        ...patch,
      }),
    );
  }

  function next() {
    setError(null);

    const message =
      validateStep(
        step,
        draft,
        slugState,
        Boolean(existingStore),
      );

    if (message) {
      setError(message);
      return;
    }

    if (step === 0) {
      updateDraft({
        slug:
          draft.slug ||
          slugify(draft.storeName),
      });
    }

    setStep(
      (current) =>
        Math.min(
          STEP_LABELS.length - 1,
          current + 1,
        ),
    );
  }

  function previous() {
    setError(null);
    setStep(
      (current) =>
        Math.max(0, current - 1),
    );
  }

  function selectTheme(
    style: OnboardingThemeStyle,
  ) {
    updateDraft({
      themeStyleId: style.id,
      themeId: style.themeId,
      fontId: style.fontId,
      productCardStyle:
        style.productCardStyle,
      heroLayout:
        style.heroLayout,
    });
  }

  function toggleFulfillment(
    value: string,
  ) {
    const next =
      draft.fulfillment.includes(
        value,
      )
        ? draft.fulfillment.filter(
            (item) =>
              item !== value,
          )
        : [
            ...draft.fulfillment,
            value,
          ];

    updateDraft({
      fulfillment: next,
    });
  }

  async function submit() {
    setSubmitting(true);
    setError(null);

    try {
      let store =
        readAdminStore();

      if (!store) {
        const availability =
          await checkTenantSlugAvailability(
            slugify(draft.slug),
          );

        if (!availability.available) {
          setStep(4);
          setSlugState("taken");
          throw new Error(
            "الرابط صار محجوزًا قبل إرسال الطلب. اختر رابطًا ثانيًا.",
          );
        }

        const created =
          await createTenant({
            name:
              draft.storeName.trim(),
            slug:
              availability.slug,
          });

        store = {
          tenantId:
            created.tenantId,
          name:
            created.name,
          slug:
            created.slug,
          status:
            created.status,
        };

        saveAdminStore(
          store,
        );
      }

      const vertical =
        await configurePrimaryVertical(
          store.tenantId,
          draft.verticalType,
        );

      saveAdminStore({
        ...store,
        verticalType:
          vertical.verticalType,
        verticalCode:
          vertical.code,
      });

      saveSelectedPlan(
        draft.selectedPlan,
      );

      saveRecommendedPlan(
        recommendedPlan,
      );

      saveRecommendedVertical(
        draft.verticalType,
      );

      saveSelectedBilling(
        "annual",
      );

      const currentConfig =
        loadStorefrontConfig();

      saveStorefrontConfig({
        ...currentConfig,
        storeName:
          draft.storeName.trim(),
        planTier:
          draft.selectedPlan === "extra"
            ? "elite"
            : draft.selectedPlan,
        themeId:
          draft.themeId,
        fontId:
          draft.fontId,
        productCardStyle:
          draft.productCardStyle,
        heroLayout:
          draft.heroLayout,
        hero: {
          ...currentConfig.hero,
          eyebrow:
            selectedVertical?.label ??
            "متجر جديد",
          title:
            draft.storeName.trim(),
        },
      });

      await submitRegistrationRequest(
        store.tenantId,
        {
          planCode:
            draft.selectedPlan,
          billingCycle:
            "Annual",
        },
      );

      clearMerchantOnboardingDraft();

      navigate(
        "/start/review",
        {
          replace: true,
        },
      );
    }
    catch (caught) {
      if (
        caught instanceof StoreSetupApiError ||
        caught instanceof MerchantRequestApiError ||
        caught instanceof Error
      ) {
        setError(
          caught.message,
        );
      }
      else {
        setError(
          "تعذر إرسال الطلب الآن.",
        );
      }
    }
    finally {
      setSubmitting(false);
    }
  }

  useEffect(
    () => {
      if (
        existingStore?.status !==
        "Active"
      ) {
        return;
      }

      let active = true;

      const timer =
        window.setTimeout(
          () => {
            void resolvePostAuthDestination()
              .then(
                (destination) => {
                  if (
                    active &&
                    destination !==
                      "/start/onboarding"
                  ) {
                    navigate(
                      destination,
                      { replace: true },
                    );
                  }
                },
              );
          },
          0,
        );

      return () => {
        active = false;
        window.clearTimeout(timer);
      };
    },
    [existingStore?.status, navigate],
  );

  return (
    <div
      dir="rtl"
      className="min-h-screen bg-[#f5f3ed] text-[#111712]"
    >
      <header className="border-b border-black/[0.07] bg-[#f5f3ed]/95">
        <div className="mx-auto flex min-h-[72px] max-w-[1320px] items-center justify-between gap-4 px-5 md:px-8">
          <div>
            <p className="text-[20px] font-bold tracking-[-0.06em]">
              RUKN
            </p>
            <p className="mt-1 text-[9px] text-black/38">
              إعداد المتجر
            </p>
          </div>

          <div className="hidden items-center gap-2 md:flex">
            {STEP_LABELS.map(
              (label, index) => (
                <div
                  key={label}
                  className="flex items-center gap-2"
                >
                  <div
                    className={[
                      "flex size-7 items-center justify-center rounded-full border text-[9px] font-semibold transition",
                      index < step
                        ? "border-[#1d4d3f] bg-[#1d4d3f] text-white"
                        : index === step
                          ? "border-[#1d4d3f] bg-white text-[#1d4d3f]"
                          : "border-black/10 bg-transparent text-black/30",
                    ].join(" ")}
                  >
                    {index < step ? (
                      <Check size={12} />
                    ) : (
                      index + 1
                    )}
                  </div>

                  <span
                    className={[
                      "text-[9px]",
                      index === step
                        ? "font-semibold text-black/70"
                        : "text-black/30",
                    ].join(" ")}
                  >
                    {label}
                  </span>

                  {index < STEP_LABELS.length - 1 ? (
                    <ChevronLeft
                      size={12}
                      className="text-black/15"
                    />
                  ) : null}
                </div>
              ),
            )}
          </div>

          <div className="text-[10px] text-black/35">
            {step + 1} / {STEP_LABELS.length}
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-[1320px] px-5 py-8 md:px-8 md:py-12">
        <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
          <section className="min-w-0 border border-black/[0.07] bg-white p-6 md:p-9 lg:p-11">
            <StepHeader
              step={step}
              verticalLabel={
                selectedVertical?.label
              }
            />

            <div className="mt-8">
              {step === 0 ? (
                <StoreNameStep
                  value={draft.storeName}
                  locked={Boolean(existingStore)}
                  onChange={(value) =>
                    updateDraft({
                      storeName: value,
                    })
                  }
                />
              ) : null}

              {step === 1 ? (
                <VerticalStep
                  query={verticalQuery}
                  onQueryChange={setVerticalQuery}
                  options={filteredVerticals}
                  selected={draft.verticalType}
                  onSelect={(verticalType) => {
                    const firstTheme =
                      getThemeStylesForVertical(
                        verticalType,
                      )[0];

                    updateDraft({
                      verticalType,
                      ...(firstTheme
                        ? {
                            themeStyleId:
                              firstTheme.id,
                            themeId:
                              firstTheme.themeId,
                            fontId:
                              firstTheme.fontId,
                            productCardStyle:
                              firstTheme.productCardStyle,
                            heroLayout:
                              firstTheme.heroLayout,
                          }
                        : {}),
                    });
                  }}
                />
              ) : null}

              {step === 2 ? (
                <QuestionsStep
                  draft={draft}
                  onChange={updateDraft}
                  onToggleFulfillment={toggleFulfillment}
                />
              ) : null}

              {step === 3 ? (
                <ThemeStep
                  styles={themeStyles}
                  selected={draft.themeStyleId}
                  onSelect={selectTheme}
                />
              ) : null}

              {step === 4 ? (
                <SlugStep
                  storeName={draft.storeName}
                  value={draft.slug}
                  state={
                    existingStore
                      ? "available"
                      : slugState
                  }
                  lockedValue={existingStore?.slug}
                  onChange={(value) => {
                    updateDraft({
                      slug: slugify(value),
                    });
                    setSlugState("idle");
                  }}
                />
              ) : null}

              {step === 5 ? (
                <PlanStep
                  selected={draft.selectedPlan}
                  recommended={recommendedPlan}
                  onSelect={(selectedPlan) =>
                    updateDraft({
                      selectedPlan,
                    })
                  }
                />
              ) : null}

              {step === 6 ? (
                <ReviewStep
                  draft={draft}
                  verticalLabel={
                    selectedVertical?.label ??
                    draft.verticalType
                  }
                  recommendedPlan={recommendedPlan}
                />
              ) : null}
            </div>

            {error ? (
              <div className="mt-7 flex items-start gap-3 border border-[#a04b3d]/15 bg-[#fff5f2] px-4 py-3 text-[11px] leading-6 text-[#8d3f33]">
                <CircleAlert
                  size={16}
                  className="mt-1 shrink-0"
                />
                <span>{error}</span>
              </div>
            ) : null}

            <div className="mt-9 flex flex-wrap items-center justify-between gap-3 border-t border-black/[0.07] pt-6">
              <button
                type="button"
                onClick={previous}
                disabled={step === 0 || submitting}
                className="inline-flex h-11 items-center gap-2 px-2 text-[11px] font-semibold text-black/50 disabled:opacity-25"
              >
                <ArrowRight size={14} />
                رجوع
              </button>

              {step < STEP_LABELS.length - 1 ? (
                <button
                  type="button"
                  onClick={next}
                  className="inline-flex h-12 items-center gap-3 bg-[#173c32] px-6 text-[12px] font-semibold text-white transition hover:bg-[#102f27]"
                >
                  متابعة
                  <ArrowLeft size={15} />
                </button>
              ) : (
                <button
                  type="button"
                  onClick={() => void submit()}
                  disabled={submitting}
                  className="inline-flex h-12 min-w-[180px] items-center justify-center gap-3 bg-[#121923] px-6 text-[12px] font-semibold text-white disabled:opacity-50"
                >
                  {submitting ? (
                    <>
                      <LoaderCircle
                        size={15}
                        className="animate-spin"
                      />
                      جارٍ إرسال الطلب
                    </>
                  ) : (
                    <>
                      تقديم الطلب
                      <ArrowLeft size={15} />
                    </>
                  )}
                </button>
              )}
            </div>
          </section>

          <aside className="h-fit border border-black/[0.07] bg-[#101c19] p-6 text-white lg:sticky lg:top-6">
            <p className="text-[10px] font-semibold text-[#d4b784]">
              ملخص الإعداد
            </p>

            <h2 className="mt-3 text-[22px] font-semibold tracking-[-0.045em]">
              {draft.storeName.trim() || "متجرك الجديد"}
            </h2>

            <div className="mt-6 space-y-4 border-t border-white/10 pt-5 text-[11px]">
              <SummaryLine
                label="النشاط"
                value={
                  selectedVertical?.label ??
                  "—"
                }
              />
              <SummaryLine
                label="الرابط"
                value={
                  draft.slug
                    ? `${draft.slug}.ofoq.store`
                    : "—"
                }
              />
              <SummaryLine
                label="الثيم"
                value={
                  getThemeStyleById(
                    draft.themeStyleId,
                  ).name
                }
              />
              <SummaryLine
                label="الباقة"
                value={
                  getPlanLabel(
                    draft.selectedPlan,
                  )
                }
              />
            </div>

            <div className="mt-6 border-t border-white/10 pt-5">
              <div className="flex items-start gap-3">
                <ShieldCheck
                  size={17}
                  className="mt-1 shrink-0 text-[#d4b784]"
                />
                <p className="text-[10px] leading-6 text-white/55">
                  ما ننشئ المتجر ولا نرسل طلب الاعتماد إلا في الخطوة الأخيرة بعد ما تراجع كل شيء.
                </p>
              </div>
            </div>
          </aside>
        </div>
      </main>
    </div>
  );
}

function StepHeader({
  step,
  verticalLabel,
}: {
  step: number;
  verticalLabel?: string;
}) {
  const content = [
    {
      eyebrow: "الخطوة الأولى",
      title: "شو اسم متجرك؟",
      body: "نحتاج الاسم فقط الآن. تقدر تغيره قبل إرسال الطلب.",
    },
    {
      eyebrow: "نوع النشاط",
      title: "اختَر المجال الأقرب لمتجرك.",
      body: "هذا الاختيار يغيّر الحقول، الثيمات، وطريقة إدارة المنتجات لاحقًا.",
    },
    {
      eyebrow: verticalLabel ?? "فهم النشاط",
      title: "كم سؤال خفيف ونكمل.",
      body: "نستخدمها لاقتراح الإعداد والباقة، بدون نموذج طويل أو أسئلة مالها داعي.",
    },
    {
      eyebrow: "الهوية الأولية",
      title: "اختَر شكل البداية.",
      body: "كل خيار يغيّر الثيم والخط والبطاقات والـHero وطريقة عرض المحتوى، مو مجرد لون.",
    },
    {
      eyebrow: "رابط المتجر",
      title: "خلّينا نحجز رابط مرتب.",
      body: "نفحص الرابط على السيرفر قبل ما نسمح لك تكمل.",
    },
    {
      eyebrow: "الباقة",
      title: "اختَر الباقة المناسبة.",
      body: "نوضح اقتراحنا، لكن القرار لك وتقدر تغيّره قبل إرسال الطلب.",
    },
    {
      eyebrow: "آخر خطوة",
      title: "راجع كل شيء وقدّم الطلب.",
      body: "بعد الإرسال يصير الطلب عند الإدارة وتظهر لك حالة المراجعة بدل ما نعيدك للبداية.",
    },
  ][step];

  return (
    <div>
      <p className="text-[10px] font-semibold tracking-[0.08em] text-[#9b7039]">
        {content.eyebrow}
      </p>
      <h1 className="mt-3 max-w-[760px] text-[31px] font-semibold tracking-[-0.055em] md:text-[40px]">
        {content.title}
      </h1>
      <p className="mt-3 max-w-[680px] text-[12px] leading-7 text-black/48">
        {content.body}
      </p>
    </div>
  );
}

function StoreNameStep({
  value,
  locked,
  onChange,
}: {
  value: string;
  locked: boolean;
  onChange: (value: string) => void;
}) {
  return (
    <div className="max-w-[680px]">
      <label className="text-[11px] font-semibold">
        اسم المتجر
      </label>
      <input
        value={value}
        disabled={locked}
        onChange={(event) =>
          onChange(event.target.value)
        }
        autoFocus={!locked}
        placeholder="مثال: نور"
        className="mt-3 h-14 w-full border border-black/10 bg-[#fbfaf7] px-4 text-[17px] outline-none transition focus:border-[#244f42] disabled:text-black/55"
      />
      <p className="mt-3 text-[10px] leading-6 text-black/38">
        {locked
          ? "هذا المتجر انحفظ سابقًا كمسودة، لذلك نحافظ على اسمه الحالي ونكمل الطلب بدل إنشاء متجر ثاني."
          : "الاسم رح يظهر للعملاء وفي لوحة الإدارة. الرابط نختاره بخطوة مستقلة."}
      </p>
    </div>
  );
}

function VerticalStep({
  query,
  onQueryChange,
  options,
  selected,
  onSelect,
}: {
  query: string;
  onQueryChange: (value: string) => void;
  options: typeof VERTICAL_OPTIONS;
  selected: string;
  onSelect: (value: string) => void;
}) {
  return (
    <div>
      <div className="relative max-w-[430px]">
        <Search
          size={16}
          className="absolute right-4 top-1/2 -translate-y-1/2 text-black/35"
        />
        <input
          value={query}
          onChange={(event) =>
            onQueryChange(event.target.value)
          }
          placeholder="ابحث عن نشاط"
          className="h-12 w-full border border-black/10 bg-[#fbfaf7] pr-11 pl-4 text-[12px] outline-none focus:border-[#244f42]"
        />
      </div>

      <div className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        {options.map(
          (item) => {
            const active =
              item.type === selected;

            return (
              <button
                key={item.type}
                type="button"
                onClick={() =>
                  onSelect(item.type)
                }
                className={[
                  "min-h-[132px] border p-5 text-right transition",
                  active
                    ? "border-[#21493d] bg-[#edf2ef]"
                    : "border-black/[0.08] bg-white hover:border-black/20",
                ].join(" ")}
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="text-[12px] font-semibold">
                      {item.label}
                    </p>
                    <p className="mt-1 text-[9px] text-black/32">
                      {item.code}
                    </p>
                  </div>
                  <div
                    className={[
                      "flex size-7 items-center justify-center rounded-full border",
                      active
                        ? "border-[#21493d] bg-[#21493d] text-white"
                        : "border-black/10 text-transparent",
                    ].join(" ")}
                  >
                    <Check size={12} />
                  </div>
                </div>
                <p className="mt-4 text-[10px] leading-6 text-black/48">
                  {item.description}
                </p>
              </button>
            );
          },
        )}
      </div>
    </div>
  );
}

function QuestionsStep({
  draft,
  onChange,
  onToggleFulfillment,
}: {
  draft: MerchantOnboardingDraft;
  onChange: (
    patch: Partial<MerchantOnboardingDraft>,
  ) => void;
  onToggleFulfillment: (value: string) => void;
}) {
  const isServiceLike =
    [
      "Services",
      "Subscriptions",
      "DigitalProducts",
      "RealEstate",
      "EventsAndTickets",
    ].includes(draft.verticalType);

  return (
    <div className="space-y-8">
      <QuestionBlock
        title="وين مشروعك اليوم؟"
        options={PROJECT_STAGE_OPTIONS}
        selected={[draft.projectStage]}
        onSelect={(value) =>
          onChange({
            projectStage: value,
          })
        }
      />

      <div className="grid gap-6 lg:grid-cols-2">
        <QuestionBlock
          title="حجم الفريق؟"
          options={TEAM_OPTIONS}
          selected={[draft.teamSize]}
          onSelect={(value) =>
            onChange({
              teamSize: value,
            })
          }
          compact
        />

        <QuestionBlock
          title="تقريبًا كم طلب بالشهر؟"
          options={ORDER_OPTIONS}
          selected={[draft.monthlyOrders]}
          onSelect={(value) =>
            onChange({
              monthlyOrders: value,
            })
          }
          compact
        />
      </div>

      {!isServiceLike ? (
        <QuestionBlock
          title="كيف توصل الطلبات؟"
          options={FULFILLMENT_OPTIONS}
          selected={draft.fulfillment}
          onSelect={onToggleFulfillment}
          multi
          compact
        />
      ) : (
        <QuestionBlock
          title="طريقة تقديم الخدمة"
          options={[
            {
              value: "online",
              label: "أونلاين",
            },
            {
              value: "appointment",
              label: "موعد أو حجز",
            },
            {
              value: "location",
              label: "في موقع فعلي",
            },
            {
              value: "hybrid",
              label: "أكثر من طريقة",
            },
          ]}
          selected={[draft.operatingModel]}
          onSelect={(value) =>
            onChange({
              operatingModel: value,
            })
          }
          compact
        />
      )}
    </div>
  );
}

function QuestionBlock({
  title,
  options,
  selected,
  onSelect,
  multi = false,
  compact = false,
}: {
  title: string;
  options: Array<{
    value: string;
    label: string;
    description?: string;
  }>;
  selected: string[];
  onSelect: (value: string) => void;
  multi?: boolean;
  compact?: boolean;
}) {
  return (
    <div>
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-[12px] font-semibold">
          {title}
        </h3>
        {multi ? (
          <span className="text-[9px] text-black/35">
            تقدر تختار أكثر من خيار
          </span>
        ) : null}
      </div>

      <div
        className={[
          "mt-3 grid gap-2",
          compact
            ? "sm:grid-cols-2"
            : "md:grid-cols-2",
        ].join(" ")}
      >
        {options.map(
          (option) => {
            const active =
              selected.includes(
                option.value,
              );

            return (
              <button
                key={option.value}
                type="button"
                onClick={() =>
                  onSelect(option.value)
                }
                className={[
                  "border px-4 py-3 text-right transition",
                  active
                    ? "border-[#244f42] bg-[#edf2ef]"
                    : "border-black/[0.08] hover:border-black/20",
                ].join(" ")}
              >
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="text-[11px] font-semibold">
                      {option.label}
                    </p>
                    {option.description ? (
                      <p className="mt-1 text-[9px] leading-5 text-black/40">
                        {option.description}
                      </p>
                    ) : null}
                  </div>
                  <div
                    className={[
                      "flex size-6 shrink-0 items-center justify-center rounded-full border",
                      active
                        ? "border-[#244f42] bg-[#244f42] text-white"
                        : "border-black/10 text-transparent",
                    ].join(" ")}
                  >
                    <Check size={11} />
                  </div>
                </div>
              </button>
            );
          },
        )}
      </div>
    </div>
  );
}

function ThemeStep({
  styles,
  selected,
  onSelect,
}: {
  styles: OnboardingThemeStyle[];
  selected: string;
  onSelect: (style: OnboardingThemeStyle) => void;
}) {
  return (
    <div className="grid gap-4 xl:grid-cols-2">
      {styles.map(
        (style) => {
          const active =
            selected === style.id;

          return (
            <button
              key={style.id}
              type="button"
              onClick={() =>
                onSelect(style)
              }
              className={[
                "overflow-hidden border text-right transition",
                active
                  ? "border-[#21493d] ring-1 ring-[#21493d]"
                  : "border-black/[0.08] hover:border-black/20",
              ].join(" ")}
            >
              <div className="grid h-[150px] grid-cols-[1.2fr_.8fr] bg-[#f8f7f3] p-4">
                <div
                  className="border border-black/[0.08] p-3"
                  style={{
                    backgroundColor:
                      style.swatches[0],
                  }}
                >
                  <div
                    className="h-2 w-12"
                    style={{
                      backgroundColor:
                        style.swatches[2],
                    }}
                  />
                  <div className="mt-5 grid grid-cols-2 gap-2">
                    <div
                      className="h-16"
                      style={{
                        backgroundColor:
                          style.swatches[1],
                      }}
                    />
                    <div className="h-16 bg-black/[0.06]" />
                  </div>
                </div>
                <div className="mr-3 space-y-2 pt-2">
                  <div className="h-4 w-3/4 bg-black/10" />
                  <div className="h-3 w-full bg-black/[0.06]" />
                  <div className="h-3 w-5/6 bg-black/[0.06]" />
                  <div
                    className="mt-4 h-8 w-full"
                    style={{
                      backgroundColor:
                        style.swatches[1],
                    }}
                  />
                </div>
              </div>

              <div className="flex items-start justify-between gap-4 bg-white p-5">
                <div>
                  <div className="flex items-center gap-2">
                    <Palette
                      size={14}
                      className="text-[#8a6437]"
                    />
                    <p className="text-[12px] font-semibold">
                      {style.name}
                    </p>
                  </div>
                  <p className="mt-2 max-w-[460px] text-[10px] leading-6 text-black/45">
                    {style.description}
                  </p>
                  <p className="mt-3 text-[9px] text-black/30">
                    {style.fontId} · {style.productCardStyle} · {style.heroLayout}
                  </p>
                </div>

                <div
                  className={[
                    "flex size-7 shrink-0 items-center justify-center rounded-full border",
                    active
                      ? "border-[#21493d] bg-[#21493d] text-white"
                      : "border-black/10 text-transparent",
                  ].join(" ")}
                >
                  <Check size={12} />
                </div>
              </div>
            </button>
          );
        },
      )}

      <div className="border border-dashed border-black/12 bg-[#faf9f6] p-5 xl:col-span-2">
        <p className="text-[10px] font-semibold text-black/55">
          الشعار والغلاف
        </p>
        <p className="mt-2 text-[10px] leading-6 text-black/38">
          ما رح نخزن صورة بشكل مؤقت أو محلي ونوهمك أنها محفوظة. رفع الشعار والغلاف الحقيقي رح يكون ضمن محرر الهوية بربط تخزين ملفات دائم، وبعد قبول الطلب تقدر تضيفهم مباشرة.
        </p>
      </div>
    </div>
  );
}

function SlugStep({
  storeName,
  value,
  state,
  lockedValue,
  onChange,
}: {
  storeName: string;
  value: string;
  state:
    | "idle"
    | "checking"
    | "available"
    | "taken"
    | "invalid"
    | "error";
  lockedValue?: string;
  onChange: (value: string) => void;
}) {
  const display =
    lockedValue ?? value;

  return (
    <div className="max-w-[720px]">
      <label className="text-[11px] font-semibold">
        رابط {storeName || "المتجر"}
      </label>

      <div className="mt-3 flex h-14 overflow-hidden border border-black/10 bg-[#fbfaf7]">
        <input
          value={display}
          disabled={Boolean(lockedValue)}
          onChange={(event) =>
            onChange(event.target.value)
          }
          className="min-w-0 flex-1 bg-transparent px-4 text-left text-[14px] outline-none disabled:text-black/55"
          dir="ltr"
          placeholder="noor"
        />
        <div className="flex items-center border-r border-black/[0.08] px-4 text-[11px] text-black/38" dir="ltr">
          .ofoq.store
        </div>
      </div>

      <div className="mt-3 min-h-6 text-[10px]">
        {state === "checking" ? (
          <span className="inline-flex items-center gap-2 text-black/40">
            <LoaderCircle size={12} className="animate-spin" />
            عم نتأكد من الرابط…
          </span>
        ) : null}
        {state === "available" ? (
          <span className="inline-flex items-center gap-2 font-semibold text-[#2f6b52]">
            <CheckCircle2 size={13} />
            الرابط متاح
          </span>
        ) : null}
        {state === "taken" ? (
          <span className="text-[#a04436]">
            الرابط مستخدم. جرّب اسمًا مختلفًا.
          </span>
        ) : null}
        {state === "error" ? (
          <span className="text-[#a04436]">
            تعذر فحص الرابط الآن. أعد المحاولة بعد قليل.
          </span>
        ) : null}
      </div>
    </div>
  );
}

function PlanStep({
  selected,
  recommended,
  onSelect,
}: {
  selected: OfoqPlan;
  recommended: OfoqPlan;
  onSelect: (plan: OfoqPlan) => void;
}) {
  return (
    <div className="grid gap-4 lg:grid-cols-3">
      {PLANS.map(
        (plan) => {
          const active =
            selected === plan.id;
          const isRecommended =
            recommended === plan.id;

          return (
            <button
              key={plan.id}
              type="button"
              onClick={() =>
                onSelect(plan.id)
              }
              className={[
                "relative border p-6 text-right transition",
                active
                  ? "border-[#173c32] bg-[#f0f4f1] ring-1 ring-[#173c32]"
                  : "border-black/[0.08] bg-white hover:border-black/20",
              ].join(" ")}
            >
              {isRecommended ? (
                <span className="absolute left-4 top-4 bg-[#d9c19b] px-2 py-1 text-[8px] font-bold text-[#2c2114]">
                  مقترحة لك
                </span>
              ) : null}

              <p className="text-[9px] font-semibold text-[#8f6839]">
                {plan.eyebrow}
              </p>
              <h3 className="mt-3 text-[24px] font-semibold">
                {plan.label}
              </h3>
              <p className="mt-3 min-h-[56px] text-[10px] leading-6 text-black/45">
                {plan.description}
              </p>

              <div className="mt-5 space-y-3 border-t border-black/[0.07] pt-5">
                {plan.points.map(
                  (point) => (
                    <div
                      key={point}
                      className="flex items-start gap-2 text-[10px] leading-5 text-black/60"
                    >
                      <Check
                        size={12}
                        className="mt-1 shrink-0 text-[#173c32]"
                      />
                      {point}
                    </div>
                  ),
                )}
              </div>
            </button>
          );
        },
      )}
    </div>
  );
}

function ReviewStep({
  draft,
  verticalLabel,
  recommendedPlan,
}: {
  draft: MerchantOnboardingDraft;
  verticalLabel: string;
  recommendedPlan: OfoqPlan;
}) {
  const theme =
    getThemeStyleById(
      draft.themeStyleId,
    );

  return (
    <div className="grid gap-4 md:grid-cols-2">
      <ReviewCard
        title="المتجر"
        rows={[
          ["الاسم", draft.storeName],
          ["النشاط", verticalLabel],
          ["الرابط", `${draft.slug}.ofoq.store`],
        ]}
      />

      <ReviewCard
        title="التصميم"
        rows={[
          ["الثيم", theme.name],
          ["الخط", theme.fontId],
          ["البطاقات", theme.productCardStyle],
        ]}
      />

      <ReviewCard
        title="التشغيل"
        rows={[
          ["حالة المشروع", labelProjectStage(draft.projectStage)],
          ["الفريق", labelFrom(TEAM_OPTIONS, draft.teamSize)],
          ["الطلبات", labelFrom(ORDER_OPTIONS, draft.monthlyOrders)],
        ]}
      />

      <ReviewCard
        title="الباقة"
        rows={[
          ["اختيارك", getPlanLabel(draft.selectedPlan)],
          ["اقتراح RUKN", getPlanLabel(recommendedPlan)],
          ["الدفع", "سنوي"],
        ]}
      />

      <div className="border border-[#b89562]/30 bg-[#fbf6ed] p-5 md:col-span-2">
        <div className="flex items-start gap-3">
          <Sparkles
            size={17}
            className="mt-1 shrink-0 text-[#8f6839]"
          />
          <div>
            <p className="text-[11px] font-semibold">
              بعد تقديم الطلب
            </p>
            <p className="mt-2 text-[10px] leading-6 text-black/48">
              ننشئ المتجر كمسودة ونرسل طلب الاعتماد للإدارة. بعدها تشوف صفحة حالة واحدة فقط. عند الموافقة تنتقل تلقائيًا للأمان ثم لوحة الإدارة.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

function ReviewCard({
  title,
  rows,
}: {
  title: string;
  rows: string[][];
}) {
  return (
    <section className="border border-black/[0.08] p-5">
      <p className="text-[11px] font-semibold">
        {title}
      </p>
      <div className="mt-4 space-y-3 border-t border-black/[0.07] pt-4">
        {rows.map(([label, value]) => (
          <div
            key={label}
            className="flex items-start justify-between gap-4 text-[10px]"
          >
            <span className="text-black/38">
              {label}
            </span>
            <span className="max-w-[65%] text-left font-semibold text-black/70">
              {value || "—"}
            </span>
          </div>
        ))}
      </div>
    </section>
  );
}

function SummaryLine({
  label,
  value,
}: {
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-start justify-between gap-4">
      <span className="text-white/35">
        {label}
      </span>
      <span className="max-w-[58%] text-left font-semibold text-white/80">
        {value}
      </span>
    </div>
  );
}

function validateStep(
  step: number,
  draft: MerchantOnboardingDraft,
  slugState: string,
  hasExistingStore: boolean,
) {
  if (
    step === 0 &&
    draft.storeName.trim().length < 2
  ) {
    return "اكتب اسم متجر واضحًا من حرفين على الأقل.";
  }

  if (
    step === 1 &&
    !draft.verticalType
  ) {
    return "اختَر نوع النشاط قبل المتابعة.";
  }

  if (
    step === 4 &&
    !hasExistingStore &&
    slugState !== "available"
  ) {
    return "لازم نتأكد أن رابط المتجر متاح قبل المتابعة.";
  }

  return null;
}

function recommendPlan(
  draft: MerchantOnboardingDraft,
): OfoqPlan {
  if (
    draft.teamSize === "large" ||
    draft.monthlyOrders === "2000-plus" ||
    draft.fulfillment.includes("branches")
  ) {
    return "extra";
  }

  if (
    draft.teamSize === "medium" ||
    draft.monthlyOrders === "500-2000" ||
    draft.projectStage === "migration"
  ) {
    return "pro";
  }

  return "business";
}

function labelProjectStage(
  value: string,
) {
  return labelFrom(
    PROJECT_STAGE_OPTIONS,
    value,
  );
}

function labelFrom(
  options: Array<{
    value: string;
    label: string;
  }>,
  value: string,
) {
  return (
    options.find(
      (option) =>
        option.value === value,
    )?.label ?? value
  );
}

function slugify(
  value: string,
) {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, "")
    .replace(/[\s_]+/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "")
    .slice(0, 64);
}
