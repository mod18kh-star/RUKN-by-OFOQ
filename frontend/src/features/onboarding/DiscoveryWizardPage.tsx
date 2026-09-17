import {
  useMemo,
  useState,
} from "react";

import {
  ArrowLeft,
  ArrowRight,
  Check,
  Info,
} from "lucide-react";

import {
  Link,
  useNavigate,
} from "react-router";

import {
  readAnswers,
  saveAnswer,
  type OnboardingAnswers,
} from "./onboardingStorage";

import {
  OnboardingProgress,
} from "./OnboardingProgress";

interface QuestionOption {
  value: string;
  label: string;
  description?: string;
  exclusive?: boolean;
}

interface Question {
  id: string;
  title: string;
  description: string;
  type:
    | "single"
    | "multi";
  why: string;
  options: QuestionOption[];
  showWhen?: (
    answers:
      OnboardingAnswers,
  ) => boolean;
}

const questions: Question[] = [
  {
    id: "projectStage",
    title:
      "وين مشروعك اليوم؟",
    description:
      "اختار الشي الأقرب لوضعك الحالي",
    type: "single",
    why:
      "حتى ما نعطيك نفس البداية لشخص لسه عم يخطط وشخص عنده مبيعات فعلية",
    options: [
      {
        value: "existing",
        label:
          "عندي نشاط قائم",
        description:
          "أبيع حاليًا وعندي عملاء",
      },
      {
        value: "new",
        label:
          "أبدأ مشروع جديد",
        description:
          "لسه في مرحلة البداية",
      },
      {
        value: "informal",
        label:
          "أبيع بشكل بسيط حاليًا",
        description:
          "مثل واتساب أو انستغرام بدون متجر متكامل",
      },
      {
        value: "migration",
        label:
          "أنقل متجري من منصة ثانية",
        description:
          "عندي متجر وأفكر بالانتقال إلى OFOQ",
      },
    ],
  },

  {
    id: "offerType",
    title:
      "شو ناوي تقدم لعملائك؟",
    description:
      "تقدر تختار أكثر من جواب إذا نشاطك يجمع أكثر من شيء",
    type: "multi",
    why:
      "نوع الشي اللي تبيعه يحدد المخزون والشحن والحجوزات والخصائص اللي تحتاجها",
    options: [
      {
        value: "physical",
        label:
          "منتجات ملموسة",
        description:
          "منتجات تنشحن أو تنستلم",
      },
      {
        value: "digital",
        label:
          "منتجات رقمية",
        description:
          "ملفات أو محتوى رقمي",
      },
      {
        value: "services",
        label:
          "خدمات",
        description:
          "خدمة تقدمها للعميل",
      },
      {
        value: "subscriptions",
        label:
          "اشتراكات",
        description:
          "دفع متكرر أو عضوية",
      },
      {
        value: "bookings",
        label:
          "حجوزات ومواعيد",
      },
      {
        value: "rental",
        label:
          "تأجير",
      },
      {
        value: "food",
        label:
          "طعام ومشروبات",
      },
      {
        value: "events",
        label:
          "فعاليات وتذاكر",
      },
      {
        value: "wholesale",
        label:
          "بيع بالجملة",
      },
    ],
  },

  {
    id: "activityCategory",
    title:
      "أي مجال أقرب لنشاطك؟",
    description:
      "مو لازم يكون الاختيار مثالي من أول مرة وتقدر تختار أكثر من مجال",
    type: "multi",
    why:
      "حتى نقترح شكل المتجر والحقول المناسبة بدل متجر عام لكل الناس",
    options: [
      {
        value: "apparel",
        label:
          "أزياء وملابس",
      },
      {
        value: "footwear",
        label:
          "أحذية",
      },
      {
        value: "mobile",
        label:
          "هواتف وجوالات",
      },
      {
        value: "electronics",
        label:
          "إلكترونيات",
      },
      {
        value: "perfumes",
        label:
          "عطور",
      },
      {
        value: "beauty",
        label:
          "تجميل وعناية",
      },
      {
        value: "restaurants",
        label:
          "مطاعم",
      },
      {
        value: "grocery",
        label:
          "مواد غذائية",
      },
      {
        value: "furniture",
        label:
          "أثاث وديكور",
      },
      {
        value: "home",
        label:
          "منتجات منزلية",
      },
      {
        value: "automotive",
        label:
          "سيارات وقطع غيار",
      },
      {
        value: "gifts",
        label:
          "هدايا",
      },
      {
        value: "jewelry",
        label:
          "مجوهرات وساعات",
      },
      {
        value: "realestate",
        label:
          "عقارات",
      },
      {
        value: "digital",
        label:
          "منتجات رقمية",
      },
      {
        value: "services",
        label:
          "خدمات",
      },
      {
        value: "subscriptions",
        label:
          "اشتراكات",
      },
      {
        value: "rental",
        label:
          "تأجير",
      },
      {
        value: "events",
        label:
          "فعاليات وتذاكر",
      },
      {
        value: "wholesale",
        label:
          "بيع بالجملة B2B",
      },
      {
        value: "other",
        label:
          "مجال آخر",
      },
    ],
  },

  {
    id: "deliveryNeeds",
    title:
      "شو تحتاج بموضوع الطلبات والمخزون؟",
    description:
      "اختار كل الأشياء اللي تنطبق على نشاطك",
    type: "multi",
    why:
      "حتى نعرف إذا لازم نجهز لك مخزون أو شحن أو استلام من الفرع",
    showWhen:
      (answers) =>
        answers.offerType?.some(
          (value) =>
            [
              "physical",
              "food",
              "rental",
            ].includes(
              value,
            ),
        ) ?? false,
    options: [
      {
        value: "delivery",
        label:
          "أحتاج توصيل للعميل",
      },
      {
        value: "inventory",
        label:
          "عندي مخزون",
      },
      {
        value: "pickup",
        label:
          "عندي استلام من الموقع أو الفرع",
      },
      {
        value: "branches",
        label:
          "عندي أكثر من فرع أو موقع",
      },
      {
        value: "dropship",
        label:
          "المورد يشحن مباشرة",
      },
      {
        value: "not-sure",
        label:
          "لسه مو محدد",
        exclusive: true,
      },
    ],
  },

  {
    id: "salesChannels",
    title:
      "وين تبيع اليوم؟",
    description:
      "اختار كل الأماكن اللي تستقبل منها طلبات حاليًا",
    type: "multi",
    why:
      "حتى نعرف إذا أنت تبدأ من الصفر أو تحتاج تجمع شغلك من أكثر من قناة",
    options: [
      {
        value: "physical-store",
        label:
          "محل أو فرع",
      },
      {
        value: "instagram",
        label:
          "Instagram",
      },
      {
        value: "tiktok",
        label:
          "TikTok",
      },
      {
        value: "whatsapp",
        label:
          "WhatsApp",
      },
      {
        value: "marketplace",
        label:
          "منصات بيع",
      },
      {
        value: "website",
        label:
          "موقع إلكتروني",
      },
      {
        value: "other-platform",
        label:
          "منصة تجارة إلكترونية ثانية",
      },
      {
        value: "not-selling",
        label:
          "ما بدأت البيع بعد",
        exclusive: true,
      },
    ],
  },

  {
    id: "customerType",
    title:
      "مين غالبًا يشتري منك؟",
    description:
      "إذا تبيع للطرفين اختار الاثنين",
    type: "multi",
    why:
      "بيع الشركات يختلف شوي عن البيع المباشر للأفراد",
    options: [
      {
        value: "b2c",
        label:
          "أفراد",
        description:
          "بيع مباشر للمستهلك",
      },
      {
        value: "b2b",
        label:
          "شركات ومؤسسات",
        description:
          "بيع تجاري B2B",
      },
    ],
  },

  {
    id: "businessStructure",
    title:
      "كيف تشتغل اليوم؟",
    description:
      "اختار الوصف الأقرب لوضع نشاطك",
    type: "single",
    why:
      "مو بهدف التدقيق عليك وإنما حتى نفهم نوع الحساب والخدمات اللي قد تحتاجها",
    options: [
      {
        value: "individual",
        label:
          "أشتغل كفرد",
      },
      {
        value: "establishment",
        label:
          "مؤسسة",
      },
      {
        value: "company",
        label:
          "شركة",
      },
      {
        value: "team",
        label:
          "فريق ولسه مو مسجلين رسميًا",
      },
      {
        value: "other",
        label:
          "شكل آخر",
      },
    ],
  },

  {
    id: "licenseStatus",
    title:
      "شو وضع الترخيص أو السجل التجاري؟",
    description:
      "ما رح نطلب منك أي مستند بهذه الخطوة",
    type: "single",
    why:
      "الجواب يساعدنا نعرف متى تكون جاهز للمدفوعات والتحقق التجاري لاحقًا",
    options: [
      {
        value: "licensed",
        label:
          "عندي ترخيص أو سجل تجاري",
      },
      {
        value: "in-progress",
        label:
          "قيد الإصدار",
      },
      {
        value: "none",
        label:
          "ما عندي حاليًا",
      },
      {
        value: "not-required",
        label:
          "غير مطلوب لنشاطي",
      },
      {
        value: "not-sure",
        label:
          "مو متأكد",
      },
    ],
  },

  {
    id: "bankingStatus",
    title:
      "شو وضع حسابك البنكي للنشاط؟",
    description:
      "نسأل عن الحالة فقط وما نطلب رقم حساب أو IBAN هنا",
    type: "single",
    why:
      "حتى نعرف متى نقدر نساعدك بتجهيز استقبال المدفوعات",
    options: [
      {
        value: "business",
        label:
          "عندي حساب بنكي تجاري",
      },
      {
        value: "personal",
        label:
          "عندي حساب شخصي فقط",
      },
      {
        value: "opening",
        label:
          "قيد فتح حساب تجاري",
      },
      {
        value: "none",
        label:
          "ما عندي حساب حاليًا",
      },
      {
        value: "not-sure",
        label:
          "مو متأكد شو أحتاج",
      },
    ],
  },

  {
    id: "teamSize",
    title:
      "كم شخص رح يدير المتجر؟",
    description:
      "عدد تقريبي يكفينا",
    type: "single",
    why:
      "حتى نقترح الباقة والصلاحيات بدون ما تدفع على شيء ما تحتاجه",
    options: [
      {
        value: "solo",
        label:
          "أنا لحالي",
      },
      {
        value: "2-5",
        label:
          "من 2 إلى 5",
      },
      {
        value: "6-20",
        label:
          "من 6 إلى 20",
      },
      {
        value: "21-plus",
        label:
          "أكثر من 20",
      },
    ],
  },

  {
    id: "salesVolume",
    title:
      "تقريبًا كم طلب تتوقع بالشهر؟",
    description:
      "مو لازم رقم دقيق وأكيد تقدر تكبر بعدين",
    type: "single",
    why:
      "حتى يكون اقتراح الباقة مناسب لحجم شغلك مو أكبر من حاجتك",
    options: [
      {
        value: "not-started",
        label:
          "لسه ما بدأت",
      },
      {
        value: "under-50",
        label:
          "أقل من 50 طلب",
      },
      {
        value: "50-500",
        label:
          "من 50 إلى 500",
      },
      {
        value: "500-5000",
        label:
          "من 500 إلى 5000",
      },
      {
        value: "5000-plus",
        label:
          "أكثر من 5000",
      },
      {
        value: "not-sure",
        label:
          "مو متأكد",
      },
    ],
  },
];

export function DiscoveryWizardPage() {
  const navigate =
    useNavigate();

  const [answers, setAnswers] =
    useState<OnboardingAnswers>(
      () => readAnswers(),
    );

  const [index, setIndex] =
    useState(0);

  const [showWhy, setShowWhy] =
    useState(false);

  const visibleQuestions =
    useMemo(
      () =>
        questions.filter(
          (question) =>
            !question.showWhen ||
            question.showWhen(
              answers,
            ),
        ),
      [answers],
    );


  const question =
    visibleQuestions[index];

  if (!question) {
    return null;
  }

  const selected =
    answers[question.id] ??
    [];

  const canContinue =
    selected.length > 0;

  function choose(
    option:
      QuestionOption,
  ) {
    let nextValues:
      string[];

    if (
      question.type ===
      "single"
    ) {
      nextValues = [
        option.value,
      ];
    } else if (
      option.exclusive
    ) {
      nextValues =
        selected.includes(
          option.value,
        )
          ? []
          : [option.value];
    } else {
      const exclusiveValues =
        question.options
          .filter(
            (item) =>
              item.exclusive,
          )
          .map(
            (item) =>
              item.value,
          );

      const clean =
        selected.filter(
          (value) =>
            !exclusiveValues.includes(
              value,
            ),
        );

      nextValues =
        clean.includes(
          option.value,
        )
          ? clean.filter(
              (value) =>
                value !==
                option.value,
            )
          : [
              ...clean,
              option.value,
            ];
    }

    const nextAnswers = {
      ...answers,
      [question.id]:
        nextValues,
    };

    setAnswers(
      nextAnswers,
    );

    saveAnswer(
      question.id,
      nextValues,
    );
  }

  function continueNext() {
    if (!canContinue) {
      return;
    }

    setShowWhy(false);

    if (
      index ===
      visibleQuestions.length -
        1
    ) {
      navigate(
        "/start/recommendation",
      );

      return;
    }

    setIndex(
      (value) =>
        value + 1,
    );
  }

  function goBack() {
    setShowWhy(false);

    if (index === 0) {
      navigate(
        "/start/login",
      );

      return;
    }

    setIndex(
      (value) =>
        value - 1,
    );
  }

  const progress =
    ((index + 1) /
      visibleQuestions.length) *
    100;

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

          <span className="text-[10px] font-medium text-black/35">
            إعداد بسيط وخفيف
          </span>
        </div>
      </header>

      <main className="px-5 py-8 md:py-11">
        <OnboardingProgress
          current="project"
        />

        <div className="mx-auto mt-10 max-w-[720px]">
          <div className="mb-7">
            <div className="flex items-center justify-between text-[10px] text-black/40">
              <span>
                سؤال{" "}
                {index + 1} من{" "}
                {
                  visibleQuestions.length
                }
              </span>

              <span>
                {question.type ===
                "multi"
                  ? "تقدر تختار أكثر من جواب"
                  : "اختيار واحد"}
              </span>
            </div>

            <div className="mt-3 h-[3px] overflow-hidden rounded-full bg-black/[0.07]">
              <div
                className="h-full rounded-full bg-[#080b14] transition-[width] duration-300"
                style={{
                  width:
                    `${progress}%`,
                }}
              />
            </div>
          </div>

          <section className="rounded-[18px] border border-black/[0.08] bg-white p-6 shadow-[0_16px_45px_rgba(20,38,32,0.04)] md:p-9">
            <h1 className="max-w-[580px] text-[29px] font-semibold leading-[1.35] tracking-[-0.04em] md:text-[36px]">
              {question.title}
            </h1>

            <p className="mt-3 max-w-[580px] text-[13px] leading-7 text-black/50">
              {
                question.description
              }
            </p>

            <button
              type="button"
              onClick={() =>
                setShowWhy(
                  (value) =>
                    !value,
                )
              }
              className="mt-5 inline-flex items-center gap-2 text-[11px] font-semibold text-[#9a713f]"
            >
              <Info
                size={14}
              />
              ليش نسأل؟
            </button>

            {showWhy ? (
              <div className="mt-3 rounded-[10px] bg-[#f2eee6] px-4 py-3 text-[11px] leading-6 text-black/55">
                {question.why}
              </div>
            ) : null}

            <div className="mt-7 grid gap-3 sm:grid-cols-2">
              {question.options.map(
                (option) => {
                  const isSelected =
                    selected.includes(
                      option.value,
                    );

                  return (
                    <button
                      key={
                        option.value
                      }
                      type="button"
                      onClick={() =>
                        choose(
                          option,
                        )
                      }
                      className={[
                        "relative min-h-[78px] rounded-[12px] border p-4 text-right transition",
                        isSelected
                          ? "border-[#080b14] bg-[#f0e7d8]"
                          : "border-black/[0.09] bg-[#fcfcfa] hover:border-black/20",
                      ].join(" ")}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="text-[13px] font-semibold">
                            {
                              option.label
                            }
                          </div>

                          {option.description ? (
                            <div className="mt-1.5 text-[10px] leading-5 text-black/45">
                              {
                                option.description
                              }
                            </div>
                          ) : null}
                        </div>

                        <div
                          className={[
                            "flex size-5 shrink-0 items-center justify-center rounded-full border",
                            isSelected
                              ? "border-[#080b14] bg-[#080b14] text-white"
                              : "border-black/15",
                          ].join(" ")}
                        >
                          {isSelected ? (
                            <Check
                              size={12}
                            />
                          ) : null}
                        </div>
                      </div>
                    </button>
                  );
                },
              )}
            </div>

            <div className="mt-8 flex items-center justify-between gap-3 border-t border-black/[0.07] pt-6">
              <button
                type="button"
                onClick={goBack}
                className="inline-flex h-11 items-center gap-2 px-2 text-[11px] font-semibold text-black/50"
              >
                <ArrowRight
                  size={15}
                />
                رجوع
              </button>

              <button
                type="button"
                disabled={
                  !canContinue
                }
                onClick={
                  continueNext
                }
                className="inline-flex h-11 min-w-[150px] items-center justify-center gap-2 rounded-[8px] bg-[#080b14] px-5 text-[12px] font-semibold text-white transition disabled:cursor-not-allowed disabled:opacity-35"
              >
                {index ===
                visibleQuestions.length -
                  1
                  ? "شوف اقتراحنا"
                  : "متابعة"}

                <ArrowLeft
                  size={15}
                />
              </button>
            </div>
          </section>
        </div>
      </main>
    </div>
  );
}