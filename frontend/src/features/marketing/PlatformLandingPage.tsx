import {
  ArrowLeft,
  ArrowUpLeft,
  BarChart3,
  Boxes,
  CheckCircle2,
  ChevronLeft,
  ClipboardList,
  ExternalLink,
  Headphones,
  Image as ImageIcon,
  LayoutDashboard,
  LayoutTemplate,
  MessageCircle,

  PackageCheck,
  Palette,
  PanelsTopLeft,
  Settings2,
  Sparkles,
  Store,
  Truck,
  Type,
  Users,
  WandSparkles,
} from "lucide-react";

import {
  Link,
} from "react-router";

const OFOQ_URL =
  "https://ofoqsy.com/ar/";

const operationalFeatures = [
  {
    icon: Boxes,
    title:
      "إدارة مخزون كاملة",
    description:
      "تابع الكميات وحركات المخزون والتنبيهات وخيارات المنتجات والمتغيرات من لوحة واحدة.",
  },
  {
    icon: ClipboardList,
    title:
      "إدارة وتتبع الطلبات",
    description:
      "تابع كل طلب من لحظة وصوله وحتى التجهيز والشحن والتسليم بسجل واضح.",
  },
  {
    icon: Users,
    title:
      "موظفون وصلاحيات",
    description:
      "أضف فريقك وحدد لكل موظف ما يستطيع الوصول إليه وإدارته داخل متجرك.",
  },
  {
    icon: LayoutDashboard,
    title:
      "لوحة إدارة واضحة",
    description:
      "طلبات ومبيعات ومخزون وتنبيهات وأهم تفاصيل تجارتك بدون تشتيت.",
  },
  {
    icon: Truck,
    title:
      "الشحن والتوصيل",
    description:
      "جهّز طرق الشحن والتوصيل والاستلام لتناسب طريقة تشغيل نشاطك.",
  },
  {
    icon: BarChart3,
    title:
      "تقارير تساعدك تتخذ القرار",
    description:
      "اعرف حركة الطلبات وأداء المنتجات وما يحتاج انتباهك بدل الاعتماد على التخمين.",
  },
];

const designControls = [
  {
    icon: Palette,
    title: "الألوان",
    text:
      "ألوان علامتك والخلفيات والتباين.",
  },
  {
    icon: Type,
    title: "الخطوط",
    text:
      "خطوط عربية وإنجليزية تناسب هويتك.",
  },
  {
    icon: PanelsTopLeft,
    title: "البطاقات",
    text:
      "شكل المنتجات وطريقة عرض التفاصيل.",
  },
  {
    icon: ImageIcon,
    title: "الصور",
    text:
      "أسلوب عرض الصور والمساحات البصرية.",
  },
  {
    icon: WandSparkles,
    title: "الحركات",
    text:
      "انتقالات وحركة محسوبة بدون إزعاج.",
  },
  {
    icon: Settings2,
    title: "النصوص والتفاصيل",
    text:
      "العناوين والأزرار والأقسام وطريقة التقديم.",
  },
];

const activities = [
  {
    title:
      "الأزياء والملابس",
    description:
      "مقاسات وألوان وخامات ومجموعات ومخزون مناسب لطبيعة المنتجات.",
  },
  {
    title:
      "الجوالات والإلكترونيات",
    description:
      "سعات وألوان ومواصفات تقنية ومتغيرات ومخزون دقيق.",
  },
  {
    title:
      "العطور والعناية",
    description:
      "أحجام وتركيزات وأنواع وخصائص تساعد العميل يختار بسهولة.",
  },
  {
    title:
      "المطاعم والأغذية",
    description:
      "أحجام وإضافات وأقسام وخيارات تناسب تجربة الطلب.",
  },
  {
    title:
      "الخدمات والاشتراكات",
    description:
      "تجربة مختلفة عن المتاجر التي تعتمد على المنتجات والمخزون التقليدي.",
  },
];

const journeySteps = [
  {
    number: "01",
    title:
      "نتعرف على مشروعك",
    description:
      "أسئلة قصيرة ومريحة تساعدنا نفهم تجارتك ووضعها الحالي وما تحتاجه فعلًا.",
  },
  {
    number: "02",
    title:
      "نرتب احتياجاتك",
    description:
      "نقترح النشاط والأدوات والخصائص الأقرب لطريقة عمل مشروعك.",
  },
  {
    number: "03",
    title:
      "تختار خطتك",
    description:
      "تشوف الباقات والأسعار المناسبة لبلدك بعد ما صار عندك تصور واضح.",
  },
  {
    number: "04",
    title:
      "تبدأ تجهيز متجرك",
    description:
      "اسم المتجر والرابط والثيم وبعدها تدخل لوحة الإدارة وتبدأ شغلك.",
  },
];

function Brand() {
  return (
    <div className="flex items-center gap-3.5">
      <div className="relative flex size-11 items-center justify-center overflow-hidden rounded-[12px] bg-[#070a13] text-white shadow-[0_10px_28px_rgba(7,10,19,.14)]">
        <span className="relative z-10 text-[22px] font-bold">
          ر
        </span>

        <span className="absolute -bottom-5 -left-3 size-11 rounded-full border border-[#d0aa70]/40" />
      </div>

      <div>
        <div className="text-[24px] font-bold leading-none tracking-[-0.045em]">
          ركن
        </div>

        <div
          dir="ltr"
          className="mt-1.5 flex items-center gap-1.5 text-[9px] font-semibold tracking-[0.12em] text-black/38"
        >
          <span className="font-normal tracking-normal text-black/28">
            by
          </span>

          <span>
            OFOQ
          </span>
        </div>
      </div>
    </div>
  );
}

function PrimaryCta({
  label =
    "جرّب بناء متجرك الأول",
  light = false,
}: {
  label?: string;
  light?: boolean;
}) {
  return (
    <Link
      to="/start"
      style={{
        color:
          light
            ? "#080b14"
            : "#ffffff",
      }}
      className={[
        "group inline-flex h-[56px] items-center justify-center gap-3 rounded-[11px] px-7 text-[13px] font-semibold transition duration-200",
        light
          ? "bg-white shadow-[0_14px_34px_rgba(0,0,0,.12)] hover:bg-[#f4f1ea]"
          : "bg-[#080b14] shadow-[0_14px_38px_rgba(7,10,19,.18)] hover:-translate-y-0.5 hover:bg-[#141a28]",
      ].join(" ")}
    >
      <span>
        {label}
      </span>

      <ArrowLeft
        size={16}
        className="transition-transform group-hover:-translate-x-1"
      />
    </Link>
  );
}

function DashboardPreview() {
  return (
    <div className="relative mx-auto w-full max-w-[730px]">
      <div className="absolute -inset-10 -z-10 rounded-[44px] border border-[#c7a36a]/12" />

      <div className="overflow-hidden rounded-[27px] border border-white/[0.09] bg-[#070a13] shadow-[0_50px_130px_rgba(7,10,19,.24)]">
        <div className="flex h-[58px] items-center justify-between border-b border-white/[0.08] px-5">
          <div className="flex gap-2">
            <span className="size-2.5 rounded-full bg-white/12" />
            <span className="size-2.5 rounded-full bg-white/12" />
            <span className="size-2.5 rounded-full bg-[#d0aa70]" />
          </div>

          <span className="rounded-full border border-white/[0.08] bg-white/[0.04] px-5 py-2 text-[10px] font-medium text-white/48">
            app.rukn.store
          </span>

          <span className="text-[10px] font-medium text-white/40">
            لوحة إدارة ركن
          </span>
        </div>

        <div className="grid min-h-[500px] lg:grid-cols-[185px_1fr]">
          <aside className="hidden border-l border-white/[0.08] bg-[#0c101b] p-5 lg:block">
            <div className="mb-8 flex items-center gap-2.5">
              <div className="flex size-8 items-center justify-center rounded-[8px] bg-[#d0aa70] text-[11px] font-bold text-[#070a13]">
                ر
              </div>

              <span className="text-[12px] font-semibold text-white">
                ركن
              </span>
            </div>

            <div className="space-y-2">
              {[
                "نظرة عامة",
                "الطلبات",
                "المنتجات",
                "المخزون",
                "الموظفون",
                "تصميم المتجر",
                "التقارير",
              ].map(
                (item, index) => (
                  <div
                    key={item}
                    className={[
                      "flex h-10 items-center rounded-[8px] px-3 text-[10px] font-medium",
                      index === 0
                        ? "bg-white/[0.08] text-[#e3c898]"
                        : "text-white/43",
                    ].join(" ")}
                  >
                    {item}
                  </div>
                ),
              )}
            </div>
          </aside>

          <div className="bg-[#f8f6f1] p-5 text-[#080b14] md:p-7">
            <div className="flex items-start justify-between">
              <div>
                <p className="text-[10px] font-medium text-black/42">
                  تفاصيل متجرك اليوم
                </p>

                <h3 className="mt-1.5 text-[21px] font-semibold tracking-[-0.03em]">
                  كل تجارتك قدامك بوضوح
                </h3>
              </div>

              <div className="flex size-10 items-center justify-center rounded-full bg-[#ece8df]">
                <Store
                  size={17}
                />
              </div>
            </div>

            <div className="mt-7 grid grid-cols-3 gap-3">
              {[
                [
                  "طلبات جديدة",
                  "18",
                ],
                [
                  "جاهزة للشحن",
                  "11",
                ],
                [
                  "مخزون منخفض",
                  "3",
                ],
              ].map(
                ([label, value]) => (
                  <div
                    key={label}
                    className="rounded-[14px] border border-black/[0.07] bg-white p-4"
                  >
                    <p className="text-[9px] text-black/45">
                      {label}
                    </p>

                    <p className="mt-2 text-[22px] font-semibold">
                      {value}
                    </p>
                  </div>
                ),
              )}
            </div>

            <div className="mt-4 grid gap-4 md:grid-cols-[1.3fr_.7fr]">
              <div className="rounded-[16px] bg-[#080b14] p-5 text-white">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-[9px] text-white/48">
                      حركة الطلبات
                    </p>

                    <p className="mt-2 text-[19px] font-semibold">
                      متابعة لحظية
                    </p>
                  </div>

                  <BarChart3
                    size={21}
                    className="text-[#d0aa70]"
                  />
                </div>

                <div className="mt-7 flex h-[85px] items-end gap-2">
                  {[
                    33,
                    47,
                    41,
                    61,
                    52,
                    73,
                    65,
                    83,
                    75,
                    95,
                  ].map(
                    (height, index) => (
                      <div
                        key={`${height}-${index}`}
                        className="flex-1 rounded-t-[4px] bg-[#d0aa70]/55"
                        style={{
                          height:
                            `${height}%`,
                        }}
                      />
                    ),
                  )}
                </div>
              </div>

              <div className="grid gap-3">
                <div className="rounded-[14px] border border-black/[0.07] bg-white p-4">
                  <PackageCheck
                    size={18}
                    className="text-[#9c7441]"
                  />

                  <p className="mt-4 text-[11px] font-semibold">
                    المخزون
                  </p>

                  <p className="mt-1.5 text-[9px] leading-5 text-black/44">
                    حركة وتنبيهات
                  </p>
                </div>

                <div className="rounded-[14px] border border-black/[0.07] bg-white p-4">
                  <Users
                    size={18}
                    className="text-[#9c7441]"
                  />

                  <p className="mt-4 text-[11px] font-semibold">
                    الفريق
                  </p>

                  <p className="mt-1.5 text-[9px] leading-5 text-black/44">
                    موظفون وصلاحيات
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-4 flex items-center justify-between rounded-[14px] border border-black/[0.07] bg-white p-4">
              <div className="flex items-center gap-3">
                <div className="flex size-10 items-center justify-center rounded-[10px] bg-[#f0e7d8]">
                  <Sparkles
                    size={16}
                    className="text-[#936b38]"
                  />
                </div>

                <div>
                  <p className="text-[10px] font-semibold">
                    تخصيص يناسب طبيعة متجرك
                  </p>

                  <p className="mt-1 text-[9px] text-black/43">
                    الأدوات والخصائص تتكيف مع نوع نشاطك
                  </p>
                </div>
              </div>

              <ChevronLeft
                size={16}
                className="text-black/25"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export function PlatformLandingPage() {
  return (
    <div
      dir="rtl"
      className="min-h-screen overflow-x-hidden bg-[#f5f3ed] text-[#080b14]"
      style={{
        fontFamily:
          '"Readex Pro", "IBM Plex Sans Arabic", "Segoe UI", Arial, sans-serif',
      }}
    >
      <header className="sticky top-0 z-50 border-b border-black/[0.07] bg-[#f5f3ed]/95 backdrop-blur-xl">
        <div className="mx-auto flex h-[82px] max-w-[1380px] items-center justify-between gap-7 px-5 md:px-9">
          <Link
            to="/"
            aria-label="ركن"
          >
            <Brand />
          </Link>

          <nav className="hidden items-center gap-8 xl:flex">
            <a
              href="#management"
              className="text-[12px] font-medium text-black/56 transition hover:text-black"
            >
              إدارة المتجر
            </a>

            <a
              href="#specialty"
              className="text-[12px] font-medium text-black/56 transition hover:text-black"
            >
              حسب اختصاصك
            </a>

            <a
              href="#design"
              className="text-[12px] font-medium text-black/56 transition hover:text-black"
            >
              التصميم
            </a>

            <a
              href="#themes"
              className="text-[12px] font-medium text-black/56 transition hover:text-black"
            >
              الثيمات
            </a>

            <a
              href="#services"
              className="text-[12px] font-medium text-black/56 transition hover:text-black"
            >
              خدمات أفق
            </a>

            <a
              href="#start"
              className="text-[12px] font-medium text-black/56 transition hover:text-black"
            >
              كيف تبدأ
            </a>
          </nav>

          <div className="flex items-center gap-2">
            <Link
              to="/start/login"
              className="hidden h-11 items-center px-3 text-[12px] font-semibold text-black/60 sm:flex"
            >
              تسجيل الدخول
            </Link>

            <Link
              to="/start"
              style={{
                color:
                  "#ffffff",
              }}
              className="inline-flex h-11 items-center gap-2.5 rounded-[10px] bg-[#080b14] px-5 text-[12px] font-semibold shadow-[0_10px_28px_rgba(8,11,20,.13)]"
            >
              ابدأ الآن

              <ArrowLeft
                size={15}
              />
            </Link>
          </div>
        </div>
      </header>

      <main>
        <section className="relative overflow-hidden">
          <div className="pointer-events-none absolute inset-0">
            <div className="absolute -right-52 top-8 size-[620px] rounded-full border border-[#b88e55]/10" />
            <div className="absolute -right-20 top-44 size-[350px] rounded-full border border-[#b88e55]/10" />
          </div>

          <div className="relative mx-auto grid min-h-[810px] max-w-[1380px] items-center gap-16 px-5 py-16 md:px-9 lg:grid-cols-[.88fr_1.12fr] lg:py-24">
            <div>
              <div className="inline-flex items-center gap-2.5 rounded-full border border-black/[0.08] bg-white/72 px-4 py-2.5">
                <span className="size-2 rounded-full bg-[#c39a61]" />

                <span className="text-[11px] font-semibold">
                  ركن من أفق
                </span>

                <span className="text-[10px] text-black/36">
                  منصة تجارة إلكترونية
                </span>
              </div>

              <h1 className="mt-8 max-w-[800px] text-[49px] font-bold leading-[1.08] tracking-[-0.052em] sm:text-[60px] lg:text-[74px]">
                ابدأ تجهيز
                <br />
                متجرك خلال
                <br />
                <span className="text-[#a77a43]">
                  أقل من ساعة.
                </span>
              </h1>

              <p className="mt-7 max-w-[650px] text-[16px] leading-[2] text-black/58 md:text-[17px]">
                جهّز أساس تجارتك
                من المنتجات والمخزون والطلبات
                حتى التصميم والفريق
                ضمن تجربة عربية واضحة
                تساعدك تبدأ بسرعة وتكبر براحتك.
              </p>

              <div className="mt-9 flex flex-wrap gap-3">
                <PrimaryCta />

                <a
                  href="#management"
                  className="inline-flex h-[56px] items-center gap-3 rounded-[11px] border border-black/12 bg-white/72 px-6 text-[13px] font-semibold text-[#080b14] transition hover:bg-white"
                >
                  اكتشف ركن

                  <ArrowUpLeft
                    size={15}
                  />
                </a>
              </div>

              <div className="mt-9 flex flex-wrap gap-x-7 gap-y-3">
                {[
                  "بداية مرتبة",
                  "إعداد حسب نشاطك",
                  "تكبر وقت ما تحتاج",
                ].map(
                  (item) => (
                    <span
                      key={item}
                      className="flex items-center gap-2 text-[12px] font-medium text-black/48"
                    >
                      <CheckCircle2
                        size={15}
                        className="text-[#9e753f]"
                      />

                      {item}
                    </span>
                  ),
                )}
              </div>
            </div>

            <DashboardPreview />
          </div>
        </section>

        <section className="border-y border-black/[0.07] bg-[#eae7df]">
          <div className="mx-auto grid max-w-[1380px] md:grid-cols-3">
            {[
              [
                "انشر حضورك الرقمي",
                "حوّل نشاطك إلى متجر مرتب قابل للمشاركة والوصول من أي مكان.",
              ],
              [
                "ابدأ بوضوح",
                "بدل عشرات الإعدادات من أول يوم تبدأ بالأشياء اللي يحتاجها نشاطك.",
              ],
              [
                "وسع تجارتك براحتك",
                "ابدأ بالأساس وفعّل الأدوات المتقدمة لما يصير وقتها مناسب.",
              ],
            ].map(
              ([title, text]) => (
                <div
                  key={title}
                  className="border-b border-black/[0.07] px-8 py-10 md:border-b-0 md:border-l"
                >
                  <h2 className="text-[18px] font-semibold">
                    {title}
                  </h2>

                  <p className="mt-3 max-w-[360px] text-[12px] leading-7 text-black/48">
                    {text}
                  </p>
                </div>
              ),
            )}
          </div>
        </section>

        <section
          id="management"
          className="bg-[#080b14] text-white"
        >
          <div className="mx-auto max-w-[1380px] px-5 py-24 md:px-9 md:py-32">
            <div className="grid gap-14 lg:grid-cols-[.7fr_1.3fr]">
              <div>
                <p className="text-[12px] font-semibold text-[#d1ad76]">
                  تشغيل متجرك
                </p>

                <h2 className="mt-5 max-w-[510px] text-[40px] font-bold leading-[1.2] tracking-[-0.04em] md:text-[53px]">
                  إدارة حقيقية
                  لكل يوم في تجارتك
                </h2>

                <p className="mt-6 max-w-[480px] text-[13px] leading-8 text-white/52">
                  ركن مو مجرد واجهة للمنتجات
                  بل مساحة تجمع العمليات
                  اللي تحتاجها لتشغيل متجرك
                  يومًا بعد يوم.
                </p>

                <div className="mt-8">
                  <PrimaryCta
                    light
                    label="ابدأ متجرك الآن"
                  />
                </div>
              </div>

              <div className="grid gap-px overflow-hidden rounded-[22px] bg-white/10 md:grid-cols-2">
                {operationalFeatures.map(
                  (item) => {
                    const Icon =
                      item.icon;

                    return (
                      <article
                        key={
                          item.title
                        }
                        className="min-h-[255px] bg-[#0d111c] p-8 transition hover:bg-[#121827]"
                      >
                        <div className="flex size-11 items-center justify-center rounded-[11px] border border-white/10 bg-white/[0.04]">
                          <Icon
                            size={20}
                            className="text-[#d0aa70]"
                          />
                        </div>

                        <h3 className="mt-10 text-[20px] font-semibold">
                          {item.title}
                        </h3>

                        <p className="mt-3 max-w-[370px] text-[12px] leading-7 text-white/50">
                          {
                            item.description
                          }
                        </p>
                      </article>
                    );
                  },
                )}
              </div>
            </div>
          </div>
        </section>

        <section
          id="specialty"
          className="mx-auto max-w-[1380px] px-5 py-24 md:px-9 md:py-32"
        >
          <div className="grid gap-14 lg:grid-cols-[.82fr_1.18fr]">
            <div>
              <p className="text-[12px] font-semibold text-[#9d723d]">
                تخصيص يناسب طبيعة متجرك
              </p>

              <h2 className="mt-5 max-w-[560px] text-[40px] font-bold leading-[1.22] tracking-[-0.04em] md:text-[53px]">
                كل نشاط
                له طريقة بيع
                مختلفة
              </h2>

              <p className="mt-6 max-w-[550px] text-[14px] leading-8 text-black/53">
                متجر الجوالات يحتاج
                سعات ومواصفات وألوان
                بينما العطور تحتاج
                أحجامًا وتركيزات مختلفة.
                لذلك نجهز خصائص المنتج
                وإدارته لتكون أقرب لاختصاصك.
              </p>

              <div className="mt-8">
                <PrimaryCta
                  label="جرّب إعداد متجرك"
                />
              </div>
            </div>

            <div className="overflow-hidden rounded-[24px] bg-[#080b14] p-8 text-white md:p-10">
              <p className="text-[11px] font-semibold text-[#d3af79]">
                مثال عملي
              </p>

              <h3 className="mt-5 max-w-[620px] text-[29px] font-semibold leading-[1.42] md:text-[37px]">
                الخصائص وطريقة عرض المنتج
                وإدارة المخزون
                تتغير حسب نشاطك
              </h3>

              <p className="mt-5 max-w-[590px] text-[12px] leading-7 text-white/53">
                بدل نموذج واحد لكل المنتجات
                تحصل على التفاصيل
                الأقرب لطبيعة الأشياء
                التي تبيعها فعلًا.
              </p>

              <div className="mt-9 grid grid-cols-2 gap-3 sm:grid-cols-4">
                {[
                  "السعة",
                  "اللون",
                  "المواصفات",
                  "المخزون",
                ].map(
                  (item) => (
                    <div
                      key={item}
                      className="rounded-[13px] border border-white/12 bg-white/[0.04] px-4 py-5 text-center text-[11px] font-medium"
                    >
                      {item}
                    </div>
                  ),
                )}
              </div>
            </div>
          </div>
        </section>

        <section
          id="design"
          className="border-y border-black/[0.07] bg-[#eeebe3]"
        >
          <div className="mx-auto grid max-w-[1380px] items-center gap-16 px-5 py-24 md:px-9 md:py-32 lg:grid-cols-2">
            <div>
              <p className="text-[12px] font-semibold text-[#9d723d]">
                هويتك داخل متجرك
              </p>

              <h2 className="mt-5 max-w-[600px] text-[40px] font-bold leading-[1.2] tracking-[-0.04em] md:text-[53px]">
                تحكم كامل
                بشكل وتجربة متجرك
              </h2>

              <p className="mt-6 max-w-[580px] text-[14px] leading-8 text-black/53">
                عدّل الألوان والصور والخطوط
                وشكل البطاقات والأقسام
                والحركات والنصوص والتفاصيل
                حتى يصير متجرك امتدادًا
                فعليًا لعلامتك التجارية.
              </p>

              <div className="mt-8">
                <PrimaryCta
                  label="ابدأ تصميم متجرك"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
              {designControls.map(
                (item) => {
                  const Icon =
                    item.icon;

                  return (
                    <article
                      key={
                        item.title
                      }
                      className="flex min-h-[185px] flex-col rounded-[17px] border border-black/[0.08] bg-[#faf9f6] p-6"
                    >
                      <Icon
                        size={21}
                        className="text-[#9e753f]"
                      />

                      <h3 className="mt-auto text-[16px] font-semibold">
                        {item.title}
                      </h3>

                      <p className="mt-2 text-[10px] leading-5 text-black/44">
                        {item.text}
                      </p>
                    </article>
                  );
                },
              )}
            </div>
          </div>
        </section>

        <section
          id="themes"
          className="mx-auto max-w-[1380px] px-5 py-24 md:px-9 md:py-32"
        >
          <div className="grid items-center gap-14 lg:grid-cols-[.7fr_1.3fr]">
            <div>
              <p className="text-[12px] font-semibold text-[#9d723d]">
                ثيمات ركن
              </p>

              <h2 className="mt-5 text-[36px] font-bold leading-[1.26] tracking-[-0.04em] md:text-[48px]">
                ابدأ من تصميم
                وبعدين خليه
                يشبهك أنت
              </h2>

              <p className="mt-6 max-w-[500px] text-[13px] leading-8 text-black/50">
                الثيم هو نقطة البداية فقط.
                بعدها تقدر تعدّل التفاصيل
                وتبني شخصية مختلفة لمتجرك.
              </p>
            </div>

            <div className="grid grid-cols-3 gap-4">
              {[
                {
                  name:
                    "Editorial",
                  caption:
                    "هادئ ومحتوى أولًا",
                  dark: true,
                },
                {
                  name:
                    "Commerce",
                  caption:
                    "بيع مباشر وواضح",
                  dark: false,
                },
                {
                  name:
                    "Studio",
                  caption:
                    "بصري وعلامة قوية",
                  dark: false,
                },
              ].map(
                (
                  theme,
                  index,
                ) => (
                  <article
                    key={
                      theme.name
                    }
                    className={[
                      "aspect-[4/5] overflow-hidden rounded-[17px] border p-4 shadow-sm",
                      theme.dark
                        ? "border-[#080b14] bg-[#080b14] text-white"
                        : "border-black/[0.08] bg-[#faf9f6] text-[#080b14]",
                    ].join(" ")}
                  >
                    <div className="flex items-center justify-between">
                      <div className="h-2 w-10 rounded-full bg-current opacity-20" />

                      <span className="text-[8px] opacity-35">
                        0{index + 1}
                      </span>
                    </div>

                    <div className="mt-5 aspect-[4/3] rounded-[9px] bg-current opacity-[0.08]" />

                    <div className="mt-4 grid grid-cols-2 gap-2">
                      <div className="aspect-square rounded-[7px] bg-current opacity-[0.07]" />
                      <div className="aspect-square rounded-[7px] bg-current opacity-[0.07]" />
                    </div>

                    <div className="mt-5">
                      <p
                        dir="ltr"
                        className="text-[10px] font-semibold"
                      >
                        {theme.name}
                      </p>

                      <p className="mt-1.5 text-[8px] opacity-45">
                        {theme.caption}
                      </p>
                    </div>
                  </article>
                ),
              )}
            </div>
          </div>
        </section>

        <section className="border-y border-black/[0.07] bg-[#ebe7de]">
          <div className="mx-auto max-w-[1380px] px-5 py-24 md:px-9 md:py-32">
            <div className="grid gap-14 lg:grid-cols-[.74fr_1.26fr]">
              <div>
                <p className="text-[12px] font-semibold text-[#9d723d]">
                  تخصيص حسب نشاطك
                </p>

                <h2 className="mt-5 max-w-[520px] text-[40px] font-bold leading-[1.2] tracking-[-0.04em] md:text-[52px]">
                  تفاصيل أقرب
                  لطبيعة تجارتك
                </h2>
              </div>

              <div className="border-t border-black/12">
                {activities.map(
                  (
                    activity,
                    index,
                  ) => (
                    <div
                      key={
                        activity.title
                      }
                      className="grid min-h-[108px] items-center gap-4 border-b border-black/12 py-5 sm:grid-cols-[45px_1fr_1.25fr]"
                    >
                      <span className="text-[10px] font-semibold text-black/28">
                        0{index + 1}
                      </span>

                      <h3 className="text-[18px] font-semibold">
                        {
                          activity.title
                        }
                      </h3>

                      <p className="text-[11px] leading-6 text-black/48">
                        {
                          activity.description
                        }
                      </p>
                    </div>
                  ),
                )}
              </div>
            </div>
          </div>
        </section>

        <section
          id="services"
          className="mx-auto max-w-[1380px] px-5 py-24 md:px-9 md:py-32"
        >
          <div className="grid gap-16 lg:grid-cols-[.92fr_1.08fr]">
            <div>
              <div className="flex size-12 items-center justify-center rounded-[13px] bg-[#eadfce]">
                <WandSparkles
                  size={20}
                  className="text-[#8e6737]"
                />
              </div>

              <p className="mt-7 text-[12px] font-semibold text-[#9d723d]">
                خدمات ركن الاحترافية
              </p>

              <h2 className="mt-5 max-w-[620px] text-[40px] font-bold leading-[1.2] tracking-[-0.04em] md:text-[52px]">
                وإذا بدك
                نبني البداية عنك
              </h2>

              <p className="mt-6 max-w-[560px] text-[14px] leading-8 text-black/52">
                تقدر تبني متجرك بنفسك
                أو تترك لفريق ركن
                تجهيز الهوية والواجهة
                وترتيب الأقسام والبداية التشغيلية
                بشكل احترافي.
              </p>

              <a
                href={OFOQ_URL}
                target="_blank"
                rel="noreferrer"
                className="mt-8 inline-flex items-center gap-2 text-[12px] font-semibold text-[#805d31]"
              >
                تعرف على خدمات ركن

                <ExternalLink
                  size={14}
                />
              </a>
            </div>

            <div className="space-y-3">
              {[
                {
                  icon:
                    LayoutTemplate,
                  title:
                    "تصميم وإعداد المتجر",
                  text:
                    "تجهيز الصفحة الرئيسية والثيم والخطوط والأقسام والهوية البصرية.",
                },
                {
                  icon:
                    Store,
                  title:
                    "نقل متجر قائم",
                  text:
                    "إذا كنت تبيع على منصة ثانية نرتب لك مسار انتقال بدل البدء من الصفر.",
                },
                {
                  icon:
                    Headphones,
                  title:
                    "مساعدة بالإعداد والتشغيل",
                  text:
                    "دعم في إعداد الأدوات الأساسية وترتيب بداية متجرك بشكل واضح.",
                },
              ].map(
                (item) => {
                  const Icon =
                    item.icon;

                  return (
                    <article
                      key={
                        item.title
                      }
                      className="flex gap-5 rounded-[17px] border border-black/[0.07] bg-[#faf9f5] p-6"
                    >
                      <div className="flex size-11 shrink-0 items-center justify-center rounded-[11px] bg-[#ede1cf]">
                        <Icon
                          size={18}
                          className="text-[#8b6333]"
                        />
                      </div>

                      <div>
                        <h3 className="text-[16px] font-semibold">
                          {
                            item.title
                          }
                        </h3>

                        <p className="mt-2 text-[11px] leading-6 text-black/47">
                          {item.text}
                        </p>
                      </div>
                    </article>
                  );
                },
              )}

              <article className="rounded-[17px] bg-[#080b14] p-7 text-white">
                <p className="text-[10px] font-semibold text-[#d1ad76]">
                  عرض الاشتراك الطويل
                </p>

                <h3 className="mt-2 text-[18px] font-semibold">
                  إعداد احترافي بدون رسوم إعداد
                </h3>

                <p className="mt-3 max-w-[560px] text-[11px] leading-6 text-white/50">
                  مع الخطط المؤهلة
                  عند الاشتراك لمدة سنتين.
                  تفاصيل العرض النهائية تظهر
                  بوضوح قبل الدفع.
                </p>
              </article>
            </div>
          </div>
        </section>

        <section
          id="start"
          className="bg-[#eeebe3]"
        >
          <div className="mx-auto max-w-[1380px] px-5 py-24 md:px-9 md:py-32">
            <div className="mx-auto max-w-[850px] text-center">
              <p className="text-[12px] font-semibold text-[#9d723d]">
                من الفكرة إلى المتجر
              </p>

              <h2 className="mt-5 text-[40px] font-bold leading-[1.2] tracking-[-0.04em] md:text-[54px]">
                نفهم مشروعك أول
                وبعدها نرتب لك
                البداية المناسبة
              </h2>

              <p className="mx-auto mt-6 max-w-[640px] text-[13px] leading-8 text-black/50">
                ما نطلب منك تختار
                بين عشرات الأشياء
                قبل ما تعرف شو فعلًا تحتاج.
              </p>
            </div>

            <div className="mt-14 grid gap-3 md:grid-cols-4">
              {journeySteps.map(
                (step) => (
                  <article
                    key={
                      step.number
                    }
                    className="min-h-[285px] rounded-[18px] border border-black/[0.07] bg-[#f9f7f2] p-7"
                  >
                    <span className="text-[11px] font-semibold text-[#9f743e]">
                      {step.number}
                    </span>

                    <h3 className="mt-16 text-[20px] font-semibold">
                      {step.title}
                    </h3>

                    <p className="mt-4 text-[11px] leading-7 text-black/48">
                      {
                        step.description
                      }
                    </p>
                  </article>
                ),
              )}
            </div>

            <div className="mt-12 flex justify-center">
              <PrimaryCta
                label="ابدأ رحلتك مع ركن"
              />
            </div>
          </div>
        </section>

        <section className="mx-auto max-w-[1380px] px-5 py-24 md:px-9 md:py-32">
          <div className="relative overflow-hidden rounded-[28px] bg-[#080b14] px-7 py-14 text-white md:px-12 md:py-20 lg:px-16">
            <div className="absolute -left-28 -top-40 size-[440px] rounded-full border border-[#d0aa70]/10" />
            <div className="absolute -left-4 -top-10 size-[250px] rounded-full border border-[#d0aa70]/10" />

            <div className="relative grid gap-10 lg:grid-cols-[1fr_auto] lg:items-end">
              <div>
                <p className="text-[12px] font-semibold text-[#d0aa70]">
                  ركن من أفق
                </p>

                <h2 className="mt-5 max-w-[780px] text-[41px] font-bold leading-[1.16] tracking-[-0.04em] md:text-[58px]">
                  جرّب بناء
                  متجرك الأول
                  على ركن.
                </h2>

                <p className="mt-6 max-w-[590px] text-[13px] leading-8 text-white/51">
                  أنشئ حسابك
                  وخلال خطوات بسيطة
                  نفهم مشروعك
                  ونرتب لك بداية واضحة.
                </p>
              </div>

              <div className="flex flex-wrap gap-3">
                <PrimaryCta
                  light
                  label="ابدأ متجرك الآن"
                />

                <a
                  href={OFOQ_URL}
                  target="_blank"
                  rel="noreferrer"
                  style={{
                    color:
                      "#ffffff",
                  }}
                  className="inline-flex h-[56px] items-center gap-2 rounded-[11px] border border-white/18 px-6 text-[12px] font-semibold transition hover:bg-white/[0.05]"
                >
                  زيارة موقع أفق

                  <ExternalLink
                    size={14}
                  />
                </a>
              </div>
            </div>
          </div>
        </section>
      </main>

      <footer className="border-t border-black/[0.07] bg-[#f0ede6]">
        <div className="mx-auto max-w-[1380px] px-5 py-16 md:px-9 md:py-20">
          <div className="grid gap-14 lg:grid-cols-[1.35fr_.65fr_.65fr]">
            <div>
              <div className="flex items-center gap-4">
                <div className="relative flex size-14 items-center justify-center overflow-hidden rounded-[15px] bg-[#070a13] text-white shadow-[0_10px_30px_rgba(7,10,19,.12)]">
                  <span className="relative z-10 text-[27px] font-bold">
                    ر
                  </span>

                  <span className="absolute -bottom-5 -left-3 size-12 rounded-full border border-[#d0aa70]/40" />
                </div>

                <div>
                  <div className="text-[31px] font-bold leading-none tracking-[-0.05em]">
                    ركن
                  </div>

                  <div
                    dir="ltr"
                    className="mt-2 flex items-center gap-1.5 text-[10px] font-semibold tracking-[0.14em] text-black/38"
                  >
                    <span className="font-normal tracking-normal text-black/28">
                      by
                    </span>

                    <span>
                      OFOQ
                    </span>
                  </div>
                </div>
              </div>

              <p className="mt-7 max-w-[560px] text-[13px] leading-8 text-black/50">
                ركن منصة تجارة إلكترونية تساعدك تبني
                وتشغّل وتطوّر متجرك من مكان واحد
                ضمن تجربة واضحة ومرنة تناسب نمو تجارتك.
              </p>

              <a
                href={OFOQ_URL}
                target="_blank"
                rel="noreferrer"
                className="mt-6 inline-flex items-center gap-2 text-[12px] font-semibold text-[#825f33] transition hover:text-[#5f421f]"
              >
                ofoqsy.com

                <ExternalLink
                  size={14}
                />
              </a>
            </div>

            <div>
              <p className="text-[14px] font-semibold">
                ركن
              </p>

              <div className="mt-6 space-y-4">
                <a
                  href="#management"
                  className="block text-[12px] text-black/52 transition hover:text-black"
                >
                  إدارة المتجر
                </a>

                <a
                  href="#specialty"
                  className="block text-[12px] text-black/52 transition hover:text-black"
                >
                  تخصيص متجرك
                </a>

                <a
                  href="#design"
                  className="block text-[12px] text-black/52 transition hover:text-black"
                >
                  تصميم المتجر
                </a>

                <a
                  href="#themes"
                  className="block text-[12px] text-black/52 transition hover:text-black"
                >
                  الثيمات
                </a>

                <a
                  href="#services"
                  className="block text-[12px] text-black/52 transition hover:text-black"
                >
                  خدمات ركن
                </a>
              </div>
            </div>

            <div>
              <p className="text-[14px] font-semibold">
                ابدأ
              </p>

              <div className="mt-6 space-y-4">
                <Link
                  to="/start"
                  className="block text-[12px] font-semibold text-[#080b14]"
                >
                  أنشئ متجرك الآن
                </Link>

                <Link
                  to="/start/login"
                  className="block text-[12px] text-black/52 transition hover:text-black"
                >
                  تسجيل الدخول
                </Link>

                <a
                  href="#start"
                  className="block text-[12px] text-black/52 transition hover:text-black"
                >
                  كيف تبدأ
                </a>

                <a
                  href={OFOQ_URL}
                  target="_blank"
                  rel="noreferrer"
                  className="block text-[12px] text-black/52 transition hover:text-black"
                >
                  عن أفق
                </a>
              </div>
            </div>
          </div>

          <div className="mt-16 flex flex-col gap-4 border-t border-black/[0.08] pt-7 text-[11px] text-black/40 sm:flex-row sm:items-center sm:justify-between">
            <span
              dir="ltr"
              className="font-semibold tracking-[0.08em]"
            >
              RUKN by OFOQ
            </span>

            <span>
              منصة لبناء وإدارة وتشغيل التجارة الإلكترونية
            </span>
          </div>
        </div>
      </footer>

      <div className="fixed bottom-5 left-5 z-40 hidden md:flex">
        <div className="flex items-center gap-3 rounded-full border border-black/[0.08] bg-white px-4 py-3.5 shadow-[0_15px_45px_rgba(7,10,19,.12)]">
          <MessageCircle
            size={16}
            className="text-[#9d723d]"
          />

          <div>
            <p className="text-[10px] font-semibold">
              تحتاج مساعدة؟
            </p>

            <p className="mt-0.5 text-[9px] text-black/40">
              فريق ركن معك
            </p>
          </div>
        </div>
      </div>

      <div className="fixed inset-x-3 bottom-3 z-50 md:hidden">
        <Link
          to="/start"
          style={{
            color:
              "#ffffff",
          }}
          className="flex h-[56px] items-center justify-center gap-2 rounded-[12px] bg-[#080b14] text-[12px] font-semibold shadow-2xl"
        >
          جرّب بناء متجرك

          <ArrowLeft
            size={15}
          />
        </Link>
      </div>
    </div>
  );
}