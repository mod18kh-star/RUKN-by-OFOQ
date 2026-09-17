import {
  Check,
  ExternalLink,
  Eye,
  Image as ImageIcon,
  LayoutTemplate,
  Palette,
  RefreshCw,
  Save,
  Sparkles,
  Type,
  Upload,
} from "lucide-react";

import { useEffect, useMemo, useRef, useState } from "react";

import { getCurrentTenantId } from "./products/productsApi";
import { readAdminStore } from "./store-setup/storeSetupStorage";
import {
  getStorefrontPresentation,
  updateStorefrontPresentation,
  uploadStorefrontAsset,
  type StorefrontPresentationSettings,
  type UpdateStorefrontPresentationInput,
} from "./storefront/storefrontPresentationApi";
import { STOREFRONT_FONTS } from "../../storefront/theme/fonts";
import { THEME_PRESETS } from "../../storefront/theme/themePresets";

interface FormState {
  logoUrl: string;
  coverImageUrl: string;
  announcement: string;
  primaryColor: string;
  accentColor: string;
  themePresetCode: string;
  fontCode: string;
  showCategoriesOnHome: boolean;
  showProductsOnHome: boolean;
  categorySectionTitle: string;
  productSectionTitle: string;
}

type DesignTab = "identity" | "theme" | "home";
type PreviewFocus = "logo" | "cover" | "announcement" | "categories" | "products" | null;
type AssetSlot = "logo" | "cover";

type ThemeVariant = {
  id: string;
  name: string;
  primary: string;
  accent: string;
};

type ThemeStory = {
  focus: string;
  cards: string;
  imagery: string;
  layout: string;
  variants: ThemeVariant[];
};

const tabs: Array<{
  id: DesignTab;
  label: string;
  description: string;
  icon: typeof Palette;
}> = [
  { id: "identity", label: "الهوية", description: "الشعار والصور والنصوص الأساسية", icon: Sparkles },
  { id: "theme", label: "الثيم والخط", description: "شكل المتجر ولوحات الألوان الجاهزة", icon: Palette },
  { id: "home", label: "الرئيسية", description: "أين يظهر النص وما الذي يُعرض", icon: LayoutTemplate },
];

const inputClass =
  "h-12 w-full rounded-[10px] border border-black/[0.1] bg-white px-3.5 text-[12px] outline-none transition placeholder:text-black/25 focus:border-[#9e7546] focus:ring-2 focus:ring-[#9e7546]/10";

const labelClass = "mb-2 block text-[11px] font-semibold text-black/66";

const themeStories: Record<string, ThemeStory> = {
  editorial: {
    focus: "إبراز الصور الكبيرة والمنتج بصورة هادئة وحديثة.",
    cards: "بطاقات نظيفة ومسافات مريحة.",
    imagery: "صور طويلة أو تحريرية.",
    layout: "ممتاز للبراندات والأزياء والعطور الحديثة.",
    variants: [
      { id: "ed-amber", name: "كهرماني", primary: "#191916", accent: "#9c6b2f" },
      { id: "ed-forest", name: "غابة", primary: "#1a201d", accent: "#355847" },
      { id: "ed-ink", name: "حبر داكن", primary: "#14161a", accent: "#455886" },
      { id: "ed-rose", name: "وردي ترابي", primary: "#201819", accent: "#8b5e62" },
    ],
  },
  maison: {
    focus: "فخامة هادئة مع تركيز على الإحساس الراقي بالمتجر.",
    cards: "بطاقات رصينة بحواف مستقيمة تقريبًا.",
    imagery: "صور غلاف ناعمة ومشاهد قريبة للمنتج.",
    layout: "مناسب للعطور والساعات والمجوهرات.",
    variants: [
      { id: "ma-cocoa", name: "كاكاو", primary: "#211d18", accent: "#6b4e38" },
      { id: "ma-gold", name: "ذهبي هادئ", primary: "#241c14", accent: "#a1793d" },
      { id: "ma-plum", name: "برقوقي", primary: "#241a23", accent: "#7e5a7d" },
      { id: "ma-olive", name: "زيتوني فاخر", primary: "#202019", accent: "#6a7252" },
    ],
  },
  commerce: {
    focus: "كثافة منتجات وتنقل سريع وواضح.",
    cards: "بطاقات مرنة مناسبة للمتاجر العامة والمتعددة.",
    imagery: "صور مربعة أو موحدة لتسهيل المقارنة.",
    layout: "مناسب للمتاجر ذات الكتالوج الكبير.",
    variants: [
      { id: "co-green", name: "تجاري أخضر", primary: "#171817", accent: "#185744" },
      { id: "co-blue", name: "تجاري أزرق", primary: "#13202b", accent: "#2f6ea0" },
      { id: "co-red", name: "تجاري أحمر", primary: "#261717", accent: "#b04d43" },
      { id: "co-charcoal", name: "فحمي", primary: "#141414", accent: "#6f7479" },
    ],
  },
  studio: {
    focus: "شخصية إبداعية دافئة مع مرونة بصرية.",
    cards: "بطاقات ناعمة ومستديرة أكثر.",
    imagery: "صور قصصية أو ديكورية أو هدايا.",
    layout: "مناسب للهدايا والديكور والمنتجات الإبداعية.",
    variants: [
      { id: "st-moss", name: "طحلبي", primary: "#171917", accent: "#44594b" },
      { id: "st-peach", name: "خوخي", primary: "#251d18", accent: "#bc7861" },
      { id: "st-lilac", name: "ليلكي", primary: "#211b25", accent: "#8a73a5" },
      { id: "st-sand", name: "رملي", primary: "#272118", accent: "#9f7e56" },
    ],
  },
  technical: {
    focus: "وضوح المواصفات والمنتجات التقنية.",
    cards: "بطاقات منظمة مع مساحة للمعلومة.",
    imagery: "صور مباشرة وواضحة ومقارنة.",
    layout: "مناسب للإلكترونيات والأجهزة والمنتجات التقنية.",
    variants: [
      { id: "te-cyan", name: "تقني سماوي", primary: "#162021", accent: "#185563" },
      { id: "te-steel", name: "فولاذي", primary: "#162021", accent: "#69767b" },
      { id: "te-violet", name: "بنفسجي تقني", primary: "#1d1b29", accent: "#6b62ae" },
      { id: "te-orange", name: "نحاسي", primary: "#231d16", accent: "#c87432" },
    ],
  },
};

function toForm(settings: StorefrontPresentationSettings): FormState {
  return {
    logoUrl: settings.logoUrl ?? "",
    coverImageUrl: settings.coverImageUrl ?? "",
    announcement: settings.announcement ?? "",
    primaryColor: settings.primaryColor ?? "",
    accentColor: settings.accentColor ?? "",
    themePresetCode: settings.themePresetCode,
    fontCode: settings.fontCode,
    showCategoriesOnHome: settings.showCategoriesOnHome,
    showProductsOnHome: settings.showProductsOnHome,
    categorySectionTitle: settings.categorySectionTitle,
    productSectionTitle: settings.productSectionTitle,
  };
}

function TargetHint({
  children,
  onClick,
}: {
  children: React.ReactNode;
  onClick?: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="mt-2 inline-flex items-center gap-1.5 rounded-full bg-[#f2efe8] px-2.5 py-1 text-[8px] font-semibold text-[#7c5c35]"
    >
      <Eye size={10} />
      {children}
    </button>
  );
}

function Toggle({
  checked,
  onChange,
  label,
  description,
}: {
  checked: boolean;
  onChange: (next: boolean) => void;
  label: string;
  description: string;
}) {
  return (
    <button
      type="button"
      onClick={() => onChange(!checked)}
      className="flex w-full items-center justify-between gap-4 rounded-[12px] border border-black/[0.075] bg-white px-4 py-4 text-right transition hover:border-black/[0.14]"
    >
      <span>
        <span className="block text-[11px] font-semibold">{label}</span>
        <span className="mt-1 block text-[9px] leading-5 text-black/38">{description}</span>
      </span>
      <span
        className={`relative h-6 w-11 shrink-0 rounded-full transition ${checked ? "bg-[#17221d]" : "bg-black/10"}`}
      >
        <span
          className={`absolute top-1 size-4 rounded-full bg-white shadow-sm transition ${checked ? "right-6" : "right-1"}`}
        />
      </span>
    </button>
  );
}

export function AdminStorefrontPresentationPage() {
  const tenantId = getCurrentTenantId();
  const store = useMemo(() => readAdminStore(), []);
  const [form, setForm] = useState<FormState | null>(null);
  const [activeTab, setActiveTab] = useState<DesignTab>("identity");
  const [loading, setLoading] = useState(Boolean(tenantId));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(tenantId ? null : "ما لقينا متجر مرتبط بالحساب.");
  const [saved, setSaved] = useState(false);
  const [previewFocus, setPreviewFocus] = useState<PreviewFocus>(null);
  const [uploadingSlot, setUploadingSlot] = useState<AssetSlot | null>(null);
  const logoInputRef = useRef<HTMLInputElement | null>(null);
  const coverInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (!tenantId) return;
    let cancelled = false;

    void getStorefrontPresentation(tenantId)
      .then((result) => {
        if (!cancelled) setForm(toForm(result));
      })
      .catch((exception: unknown) => {
        if (!cancelled) {
          setError(
            exception instanceof Error
              ? exception.message
              : "تعذر تحميل إعدادات واجهة المتجر.",
          );
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [tenantId]);

  function update(key: keyof FormState, value: string | boolean) {
    setSaved(false);
    setForm((current) => (current ? { ...current, [key]: value } : current));
  }

  async function save() {
    if (!tenantId || !form || saving) return;

    const input: UpdateStorefrontPresentationInput = {
      logoUrl: form.logoUrl.trim() || null,
      coverImageUrl: form.coverImageUrl.trim() || null,
      announcement: form.announcement.trim() || null,
      primaryColor: form.primaryColor.trim() || null,
      accentColor: form.accentColor.trim() || null,
      themePresetCode: form.themePresetCode,
      fontCode: form.fontCode,
      showCategoriesOnHome: form.showCategoriesOnHome,
      showProductsOnHome: form.showProductsOnHome,
      categorySectionTitle: form.categorySectionTitle.trim(),
      productSectionTitle: form.productSectionTitle.trim(),
    };

    setSaving(true);
    setError(null);
    setSaved(false);

    try {
      const result = await updateStorefrontPresentation(tenantId, input);
      setForm(toForm(result));
      setSaved(true);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر حفظ إعدادات واجهة المتجر.",
      );
    } finally {
      setSaving(false);
    }
  }

  async function handleAssetUpload(slot: AssetSlot, file: File | null) {
    if (!tenantId || !file) return;

    setError(null);
    setSaved(false);
    setUploadingSlot(slot);
    setPreviewFocus(slot === "logo" ? "logo" : "cover");

    try {
      const assetUrl = await uploadStorefrontAsset(tenantId, slot, file);
      update(slot === "logo" ? "logoUrl" : "coverImageUrl", assetUrl);
    } catch (exception) {
      setError(
        exception instanceof Error ? exception.message : "تعذر رفع الصورة.",
      );
    } finally {
      setUploadingSlot(null);
      if (slot === "logo" && logoInputRef.current) logoInputRef.current.value = "";
      if (slot === "cover" && coverInputRef.current) coverInputRef.current.value = "";
    }
  }

  const storefrontHref = store?.slug ? `/store/${encodeURIComponent(store.slug)}` : null;

  if (loading) {
    return (
      <div className="flex min-h-[420px] items-center justify-center">
        <RefreshCw className="animate-spin text-black/35" size={20} />
      </div>
    );
  }

  if (!form) {
    return (
      <div className="rounded-[16px] border border-red-200 bg-red-50 p-6 text-[11px] leading-6 text-red-700">
        {error ?? "تعذر تحميل إعدادات واجهة المتجر."}
      </div>
    );
  }

  const theme = THEME_PRESETS.find((item) => item.id === form.themePresetCode) ?? THEME_PRESETS[0];
  const font = STOREFRONT_FONTS.find((item) => item.id === form.fontCode) ?? STOREFRONT_FONTS[0];
  const previewPrimary = form.primaryColor || theme.ink;
  const previewAccent = form.accentColor || theme.accent;
  const activeThemeStory = themeStories[form.themePresetCode] ?? themeStories.editorial;

  function sectionFocusClass(target: PreviewFocus) {
    return previewFocus === target
      ? "ring-2 ring-[#9e7546] shadow-[0_0_0_4px_rgba(158,117,70,.12)]"
      : "";
  }

  return (
    <div dir="rtl" className="mx-auto max-w-[1500px] pb-10">
      <header className="flex flex-col gap-5 border-b border-black/[0.075] pb-7 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <div className="mb-2 flex items-center gap-2">
            <span className="text-[10px] font-semibold text-[#946b3b]">تصميم المتجر</span>
            <span className="rounded-full bg-[#eee8dc] px-2.5 py-1 text-[8px] font-semibold text-[#6f5638]">محفوظ على السيرفر</span>
          </div>
          <h1 className="text-[31px] font-semibold tracking-[-0.045em]">الهوية والثيم وواجهة المتجر</h1>
          <p className="mt-2 max-w-[760px] text-[11px] leading-7 text-black/45">
            عدّل شكل متجرك من مكان واحد، وارفع الصور من جهازك، وشاهد أين يظهر كل نص داخل المعاينة.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {saved ? (
            <span className="inline-flex h-10 items-center gap-1.5 rounded-[9px] bg-[#e9f0ea] px-3 text-[9px] font-semibold text-[#315a49]">
              <Check size={13} /> تم الحفظ
            </span>
          ) : null}
          {storefrontHref ? (
            <a
              href={storefrontHref}
              target="_blank"
              rel="noreferrer"
              className="inline-flex h-11 items-center gap-2 rounded-[9px] border border-black/[0.09] bg-white px-4 text-[10px] font-semibold transition hover:bg-black/[0.025]"
            >
              <ExternalLink size={14} /> معاينة المتجر
            </a>
          ) : null}
          <button
            type="button"
            disabled={saving}
            onClick={() => void save()}
            className="inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#0d1110] px-5 text-[10px] font-semibold text-white disabled:opacity-40"
          >
            <Save size={14} /> {saving ? "جاري الحفظ..." : "حفظ التغييرات"}
          </button>
        </div>
      </header>

      {error ? (
        <div className="mt-5 rounded-[10px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">{error}</div>
      ) : null}

      <div className="mt-6 grid gap-2 rounded-[14px] border border-black/[0.07] bg-[#eeeee9] p-2 md:grid-cols-3">
        {tabs.map((tab) => {
          const Icon = tab.icon;
          const active = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-3 rounded-[10px] px-4 py-3.5 text-right transition ${active ? "bg-white shadow-[0_1px_3px_rgba(0,0,0,.05)]" : "hover:bg-white/55"}`}
            >
              <span className={`flex size-9 items-center justify-center rounded-[9px] ${active ? "bg-[#eee7da] text-[#7f5d35]" : "bg-white/70 text-black/45"}`}>
                <Icon size={16} />
              </span>
              <span>
                <span className="block text-[11px] font-semibold">{tab.label}</span>
                <span className="mt-0.5 block text-[8px] text-black/38">{tab.description}</span>
              </span>
            </button>
          );
        })}
      </div>

      <div className="mt-6 grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_520px]">
        <div className="min-w-0">
          {activeTab === "identity" ? (
            <section className="rounded-[16px] border border-black/[0.07] bg-white p-5 md:p-7">
              <div className="flex items-start gap-3 border-b border-black/[0.06] pb-5">
                <span className="flex size-10 items-center justify-center rounded-[10px] bg-[#f0e9dd] text-[#815f37]"><Sparkles size={17} /></span>
                <div>
                  <h2 className="text-[15px] font-semibold">هوية المتجر</h2>
                  <p className="mt-1 text-[9px] leading-5 text-black/40">ارفع الصور من جهازك أو استخدم رابطًا مباشرًا، مع توضيح مكان ظهور كل عنصر.</p>
                </div>
              </div>

              <div className="mt-6 grid gap-5 md:grid-cols-2">
                <div className="md:col-span-2 rounded-[14px] border border-black/[0.06] bg-[#faf8f4] p-4">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <label className={labelClass}>الشعار</label>
                      <p className="text-[9px] text-black/38">يظهر في رأس المتجر بدل اسم المتجر.</p>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        onClick={() => logoInputRef.current?.click()}
                        className="inline-flex h-10 items-center gap-2 rounded-[10px] border border-black/[0.08] bg-white px-4 text-[10px] font-semibold"
                      >
                        <Upload size={13} />
                        {uploadingSlot === "logo" ? "جاري الرفع..." : "رفع من الجهاز"}
                      </button>
                      <input
                        ref={logoInputRef}
                        type="file"
                        accept="image/png,image/jpeg,image/webp,image/svg+xml"
                        className="hidden"
                        onChange={(event) => void handleAssetUpload("logo", event.target.files?.[0] ?? null)}
                      />
                    </div>
                  </div>
                  <input
                    dir="ltr"
                    value={form.logoUrl}
                    onFocus={() => setPreviewFocus("logo")}
                    onChange={(event) => update("logoUrl", event.target.value)}
                    className={`${inputClass} mt-3 text-left`}
                    placeholder="https://cdn.example.com/logo.png"
                  />
                  <TargetHint onClick={() => setPreviewFocus("logo")}>هذا العنصر يظهر في رأس المتجر داخل المعاينة</TargetHint>
                </div>

                <div className="md:col-span-2 rounded-[14px] border border-black/[0.06] bg-[#faf8f4] p-4">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <label className={labelClass}>صورة الغلاف</label>
                      <p className="text-[9px] text-black/38">هذه الصورة هي البانر الأساسي في أعلى الصفحة الرئيسية.</p>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        onClick={() => coverInputRef.current?.click()}
                        className="inline-flex h-10 items-center gap-2 rounded-[10px] border border-black/[0.08] bg-white px-4 text-[10px] font-semibold"
                      >
                        <Upload size={13} />
                        {uploadingSlot === "cover" ? "جاري الرفع..." : "رفع من الجهاز"}
                      </button>
                      <input
                        ref={coverInputRef}
                        type="file"
                        accept="image/png,image/jpeg,image/webp"
                        className="hidden"
                        onChange={(event) => void handleAssetUpload("cover", event.target.files?.[0] ?? null)}
                      />
                    </div>
                  </div>
                  <input
                    dir="ltr"
                    value={form.coverImageUrl}
                    onFocus={() => setPreviewFocus("cover")}
                    onChange={(event) => update("coverImageUrl", event.target.value)}
                    className={`${inputClass} mt-3 text-left`}
                    placeholder="https://cdn.example.com/cover.jpg"
                  />
                  <TargetHint onClick={() => setPreviewFocus("cover")}>هذه الصورة تظهر كعرض رئيسي في أعلى الصفحة</TargetHint>
                </div>

                <div>
                  <label className={labelClass}>اللون الأساسي</label>
                  <div className="flex gap-2">
                    <input type="color" value={form.primaryColor || theme.ink} onChange={(event) => update("primaryColor", event.target.value)} className="h-12 w-13 rounded-[9px] border border-black/[0.09] bg-white p-1" />
                    <input dir="ltr" value={form.primaryColor} onChange={(event) => update("primaryColor", event.target.value)} className={`${inputClass} text-left`} placeholder={theme.ink} />
                  </div>
                </div>
                <div>
                  <label className={labelClass}>لون التمييز</label>
                  <div className="flex gap-2">
                    <input type="color" value={form.accentColor || theme.accent} onChange={(event) => update("accentColor", event.target.value)} className="h-12 w-13 rounded-[9px] border border-black/[0.09] bg-white p-1" />
                    <input dir="ltr" value={form.accentColor} onChange={(event) => update("accentColor", event.target.value)} className={`${inputClass} text-left`} placeholder={theme.accent} />
                  </div>
                </div>
                <div className="md:col-span-2 rounded-[14px] border border-black/[0.06] bg-[#faf8f4] p-4">
                  <label className={labelClass}>شريط الإعلان</label>
                  <input
                    value={form.announcement}
                    onFocus={() => setPreviewFocus("announcement")}
                    onChange={(event) => update("announcement", event.target.value)}
                    className={inputClass}
                    placeholder="مثال: شحن مجاني للطلبات فوق 300 ر.س"
                  />
                  <div className="mt-2 flex flex-wrap gap-2 text-[8px] text-black/45">
                    <span className="rounded-full bg-[#f4f1eb] px-2.5 py-1">يمكنك تغيير هذا النص متى شئت</span>
                    <TargetHint onClick={() => setPreviewFocus("announcement")}>انقر لرؤية مكان الشريط داخل المعاينة</TargetHint>
                  </div>
                </div>
              </div>
            </section>
          ) : null}

          {activeTab === "theme" ? (
            <div className="space-y-5">
              <section className="rounded-[16px] border border-black/[0.07] bg-white p-5 md:p-7">
                <div className="mb-5">
                  <h2 className="text-[15px] font-semibold">اختر شخصية الثيم</h2>
                  <p className="mt-1 text-[9px] leading-5 text-black/40">كل ثيم له أسلوبه: شكل أساسي، شخصية بطاقات، طريقة عرض الصور، وتركيز مختلف على المنتج.</p>
                </div>
                <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                  {THEME_PRESETS.map((preset) => {
                    const active = form.themePresetCode === preset.id;
                    const story = themeStories[preset.id] ?? activeThemeStory;
                    return (
                      <button
                        key={preset.id}
                        type="button"
                        onClick={() => update("themePresetCode", preset.id)}
                        className={`overflow-hidden rounded-[13px] border p-2 text-right transition ${active ? "border-[#8c6537] ring-2 ring-[#8c6537]/12" : "border-black/[0.08] hover:border-black/20"}`}
                      >
                        <div className="h-24 rounded-[9px] p-3" style={{ background: preset.canvas, color: preset.ink }}>
                          <div className="flex items-center justify-between border-b pb-2" style={{ borderColor: `${preset.ink}18` }}>
                            <span className="text-[9px] font-bold">{store?.name ?? "متجرك"}</span>
                            <span className="size-3" style={{ background: preset.accent, borderRadius: preset.radius }} />
                          </div>
                          <div className="mt-3 grid grid-cols-3 gap-1.5">
                            {[0, 1, 2].map((value) => <span key={value} className="h-8" style={{ background: preset.surface, borderRadius: preset.radius }} />)}
                          </div>
                        </div>
                        <div className="px-1 pb-1 pt-3">
                          <div className="flex items-center justify-between gap-2">
                            <span className="text-[10px] font-semibold">{preset.name}</span>
                            {active ? <Check size={13} className="text-[#76552f]" /> : null}
                          </div>
                          <p className="mt-1 text-[8px] leading-4 text-black/38">{preset.description}</p>
                          <div className="mt-2 flex flex-wrap gap-1.5">
                            {story.variants.slice(0, 4).map((variant) => (
                              <span key={variant.id} className="size-3 rounded-full border border-black/10" style={{ background: variant.accent }} />
                            ))}
                          </div>
                        </div>
                      </button>
                    );
                  })}
                </div>
              </section>

              <section className="rounded-[16px] border border-black/[0.07] bg-white p-5 md:p-7">
                <div className="mb-5 grid gap-3 md:grid-cols-[minmax(0,1fr)_270px]">
                  <div>
                    <h2 className="text-[14px] font-semibold">لوحات ألوان هذا الثيم</h2>
                    <p className="mt-1 text-[9px] text-black/38">اختر لوحة جاهزة مرتبطة بهذا الثيم، ثم عدّل الألوان بحرية حسب هوية المتجر.</p>
                  </div>
                  <div className="rounded-[12px] border border-black/[0.06] bg-[#faf8f4] p-4 text-[9px] leading-6 text-black/55">
                    <p><span className="font-semibold text-black">التركيز:</span> {activeThemeStory.focus}</p>
                    <p><span className="font-semibold text-black">البطاقات:</span> {activeThemeStory.cards}</p>
                    <p><span className="font-semibold text-black">الصور:</span> {activeThemeStory.imagery}</p>
                    <p><span className="font-semibold text-black">يناسب:</span> {activeThemeStory.layout}</p>
                  </div>
                </div>
                <div className="grid gap-3 md:grid-cols-2">
                  {activeThemeStory.variants.map((variant) => {
                    const active = (form.primaryColor || theme.ink) === variant.primary && (form.accentColor || theme.accent) === variant.accent;
                    return (
                      <button
                        key={variant.id}
                        type="button"
                        onClick={() => {
                          update("primaryColor", variant.primary);
                          update("accentColor", variant.accent);
                        }}
                        className={`rounded-[12px] border p-4 text-right transition ${active ? "border-[#8c6537] bg-[#fbf8f2]" : "border-black/[0.08] hover:border-black/20"}`}
                      >
                        <div className="flex items-center justify-between gap-3">
                          <span className="text-[11px] font-semibold">{variant.name}</span>
                          <div className="flex gap-2">
                            <span className="size-4 rounded-full border border-black/10" style={{ background: variant.primary }} />
                            <span className="size-4 rounded-full border border-black/10" style={{ background: variant.accent }} />
                          </div>
                        </div>
                        <div className="mt-3 flex gap-2">
                          <span className="h-10 flex-1 rounded-[10px]" style={{ background: variant.primary }} />
                          <span className="h-10 flex-1 rounded-[10px]" style={{ background: variant.accent }} />
                        </div>
                      </button>
                    );
                  })}
                </div>
              </section>

              <section className="rounded-[16px] border border-black/[0.07] bg-white p-5 md:p-7">
                <div className="mb-5 flex items-center gap-3">
                  <span className="flex size-9 items-center justify-center rounded-[9px] bg-[#efeee9]"><Type size={16} /></span>
                  <div>
                    <h2 className="text-[14px] font-semibold">نوع الخط</h2>
                    <p className="mt-1 text-[9px] text-black/38">اختيار الخط يغيّر شخصية النصوص في المتجر.</p>
                  </div>
                </div>
                <div className="grid gap-3 sm:grid-cols-2">
                  {STOREFRONT_FONTS.map((fontOption) => {
                    const active = form.fontCode === fontOption.id;
                    return (
                      <button
                        key={fontOption.id}
                        type="button"
                        onClick={() => update("fontCode", fontOption.id)}
                        className={`rounded-[11px] border px-4 py-4 text-right transition ${active ? "border-[#8c6537] bg-[#fbf8f2]" : "border-black/[0.08] hover:border-black/20"}`}
                      >
                        <span className="block text-[17px] font-semibold" style={{ fontFamily: fontOption.family }}>ركن يصنع حضور متجرك</span>
                        <span className="mt-2 block text-[8px] text-black/38">{fontOption.name}</span>
                      </button>
                    );
                  })}
                </div>
              </section>
            </div>
          ) : null}

          {activeTab === "home" ? (
            <section className="rounded-[16px] border border-black/[0.07] bg-white p-5 md:p-7">
              <div className="mb-6">
                <h2 className="text-[15px] font-semibold">العرض الأساسي للرئيسية</h2>
                <p className="mt-1 text-[9px] leading-5 text-black/40">كل حقل بالأسفل مربوط بما يظهر في المعاينة. اضغط على زر الرؤية لتعرف مكان كل نص مباشرة.</p>
              </div>

              <div className="space-y-4">
                <Toggle checked={form.showCategoriesOnHome} onChange={(value) => update("showCategoriesOnHome", value)} label="إظهار الأقسام في الرئيسية" description="إذا أوقفته يختفي قسم تصفح الأقسام من الصفحة الرئيسية." />
                {form.showCategoriesOnHome ? (
                  <div className="rounded-[12px] border border-black/[0.07] bg-[#fafaf7] p-4">
                    <label className={labelClass}>عنوان قسم الأقسام</label>
                    <input value={form.categorySectionTitle} onFocus={() => setPreviewFocus("categories")} onChange={(event) => update("categorySectionTitle", event.target.value)} className={inputClass} />
                    <TargetHint onClick={() => setPreviewFocus("categories")}>هذا هو عنوان قسم الأقسام داخل المعاينة</TargetHint>
                  </div>
                ) : null}

                <Toggle checked={form.showProductsOnHome} onChange={(value) => update("showProductsOnHome", value)} label="إظهار المنتجات في الرئيسية" description="إذا أوقفته يختفي عرض المنتجات من الصفحة الرئيسية فقط." />
                {form.showProductsOnHome ? (
                  <div className="rounded-[12px] border border-black/[0.07] bg-[#fafaf7] p-4">
                    <label className={labelClass}>عنوان قسم المنتجات</label>
                    <input value={form.productSectionTitle} onFocus={() => setPreviewFocus("products")} onChange={(event) => update("productSectionTitle", event.target.value)} className={inputClass} />
                    <TargetHint onClick={() => setPreviewFocus("products")}>هذا هو عنوان قسم المنتجات داخل المعاينة</TargetHint>
                  </div>
                ) : null}
              </div>
            </section>
          ) : null}
        </div>

        <aside className="xl:sticky xl:top-[90px]">
          <div className="rounded-[17px] border border-black/[0.075] bg-white p-4 shadow-[0_14px_35px_rgba(17,21,19,.05)]">
            <div className="mb-4 flex items-center justify-between">
              <div>
                <p className="text-[11px] font-semibold">معاينة مباشرة</p>
                <p className="mt-1 text-[8px] text-black/35">انقر على حقول النص أو أزرار العين لتحديد موضعها</p>
              </div>
              <Eye size={15} className="text-black/35" />
            </div>

            <div
              className="overflow-hidden border"
              style={{
                background: theme.canvas,
                color: previewPrimary,
                borderColor: `${previewPrimary}18`,
                borderRadius: theme.radius,
                fontFamily: font.family,
              }}
            >
              {form.announcement.trim() ? (
                <div className={sectionFocusClass("announcement")}>
                  <div className="px-4 py-2 text-center text-[8px] font-semibold" style={{ background: previewPrimary, color: theme.surface }}>
                    {form.announcement}
                  </div>
                </div>
              ) : null}

              <div className={`flex min-h-[64px] items-center justify-between px-5 ${sectionFocusClass("logo")}`} style={{ borderBottom: `1px solid ${previewPrimary}15` }}>
                {form.logoUrl.trim() ? (
                  <img src={form.logoUrl.trim()} alt="شعار المتجر" className="max-h-9 max-w-[130px] object-contain" />
                ) : (
                  <span className="text-[15px] font-bold">{store?.name ?? "اسم المتجر"}</span>
                )}
                <span className="text-[8px] opacity-45">الرئيسية　المجموعة　قصتنا</span>
              </div>

              <div className={sectionFocusClass("cover")}>
                {form.coverImageUrl.trim() ? (
                  <img src={form.coverImageUrl.trim()} alt="غلاف المتجر" className="h-[190px] w-full object-cover" />
                ) : (
                  <div className="flex h-[190px] items-center justify-center" style={{ background: theme.soft }}>
                    <div className="text-center opacity-35">
                      <ImageIcon size={26} className="mx-auto" />
                      <p className="mt-2 text-[8px]">صورة العرض الرئيسية</p>
                    </div>
                  </div>
                )}
              </div>

              <div className="p-5">
                {form.showCategoriesOnHome ? (
                  <div className={`mb-6 rounded-[10px] ${sectionFocusClass("categories")}`}>
                    <p className="text-[7px] opacity-45">الأقسام</p>
                    <h3 className="mt-1 text-[17px] font-semibold">{form.categorySectionTitle || "تصفح الأقسام"}</h3>
                    <div className="mt-3 grid grid-cols-3 gap-2">
                      {[0, 1, 2].map((value) => <span key={value} className="h-16" style={{ background: theme.surface, borderRadius: theme.radius }} />)}
                    </div>
                  </div>
                ) : null}

                {form.showProductsOnHome ? (
                  <div className={`rounded-[10px] ${sectionFocusClass("products")}`}>
                    <p className="text-[7px] opacity-45">مختارات</p>
                    <h3 className="mt-1 text-[17px] font-semibold">{form.productSectionTitle || "منتجات المتجر"}</h3>
                    <div className="mt-3 grid grid-cols-3 gap-2">
                      {[0, 1, 2].map((value) => (
                        <div key={value}>
                          <span className="block h-20" style={{ background: theme.soft, borderRadius: theme.radius }} />
                          <span className="mt-2 block h-1.5 w-2/3" style={{ background: `${previewPrimary}26` }} />
                          <span className="mt-1.5 block h-1.5 w-1/3" style={{ background: previewAccent }} />
                        </div>
                      ))}
                    </div>
                  </div>
                ) : null}
              </div>
            </div>

            <div className="mt-4 rounded-[12px] bg-[#f3f2ed] p-3 text-[8px] text-black/48">
              <div className="flex items-center justify-between">
                <span>{theme.name} · {font.name}</span>
                <div className="flex gap-2">
                  <span className="size-3 rounded-full" style={{ background: previewPrimary }} />
                  <span className="size-3 rounded-full" style={{ background: previewAccent }} />
                </div>
              </div>
              <p className="mt-2 leading-5">{activeThemeStory.focus}</p>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
