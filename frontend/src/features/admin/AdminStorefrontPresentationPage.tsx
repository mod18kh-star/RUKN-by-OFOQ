import {
  Check,
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

import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";

import { parseVisualContent, type StorefrontVisualContent } from "../../storefront/config/visualContent";

import { getCommerceProfile, getCurrentTenantId } from "./products/productsApi";
import { readAdminStore } from "./store-setup/storeSetupStorage";
import {
  getStorefrontPresentation,
  updateStorefrontPresentation,
  uploadStorefrontAsset,
  type StorefrontPresentationSettings,
  type UpdateStorefrontPresentationInput,
} from "./storefront/storefrontPresentationApi";
import { STOREFRONT_FONTS } from "../../storefront/theme/fonts";
import {
  THEME_PRESETS,
  getThemePresetsForVertical,
  isThemeRecommendedForVertical,
  type ThemePreset,
} from "../../storefront/theme/themePresets";

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
  visualContent: StorefrontVisualContent;
}

type DesignTab = "identity" | "theme" | "home" | "editorial";
type PreviewFocus = "logo" | "cover" | "announcement" | "categories" | "products" |
  "heroEyebrow" | "heroTitle" | "heroDescription" | "heroCtaLabel" | "heroCtaHref" |
  "heroSecondaryImage" | "categoryEyebrow" | "productEyebrow" | "footerDescription" |
  "heroFeaturedCaption" | "heroStat1" | "heroStat2" | "heroStat3" | "heroNote1Title" | "heroNote1Body" | "heroNote2Title" | "heroNote2Body" | "heroNote3Title" | "heroNote3Body" | "flagshipBand1Title" | "flagshipBand1Body" | "flagshipBand2Title" | "flagshipBand2Body" | "flagshipBand3Title" | "flagshipBand3Body" | "smartBand1Title" | "smartBand1Body" | "smartBand2Title" | "smartBand2Body" | "smartBand3Title" | "smartBand3Body" | "smartBand4Title" | "smartBand4Body" | "heroShowcaseMode" | "productLayout" | "productCardStyle" | "categoryLayout" | null;
type AssetSlot = "logo" | "cover" | "showcase";

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
  { id: "editorial", label: "المحتوى", description: "الهيرو والعناوين والفوتر", icon: Type },
];

const previewEditorTargets: Record<Exclude<PreviewFocus, null>, { tab: DesignTab; label: string }> = {
  logo: { tab: "identity", label: "شعار المتجر" },
  cover: { tab: "identity", label: "صورة الغلاف" },
  announcement: { tab: "identity", label: "شريط الإعلان" },
  categories: { tab: "home", label: "عنوان الأقسام" },
  products: { tab: "home", label: "عنوان المنتجات" },
  heroEyebrow: { tab: "editorial", label: "عبارة الهيرو" },
  heroTitle: { tab: "editorial", label: "عنوان الهيرو" },
  heroDescription: { tab: "editorial", label: "وصف الهيرو" },
  heroCtaLabel: { tab: "editorial", label: "نص زر الهيرو" },
  heroCtaHref: { tab: "editorial", label: "رابط زر الهيرو" },
  heroSecondaryImage: { tab: "editorial", label: "الصورة الثانوية" },
  categoryEyebrow: { tab: "editorial", label: "عبارة الأقسام" },
  productEyebrow: { tab: "editorial", label: "عبارة المنتجات" },
  footerDescription: { tab: "editorial", label: "وصف الفوتر" },
  heroFeaturedCaption: { tab: "editorial", label: "وصف الصورة المميزة" },
  heroStat1: { tab: "editorial", label: "خطوة 1" },
  heroStat2: { tab: "editorial", label: "خطوة 2" },
  heroStat3: { tab: "editorial", label: "خطوة 3" },
  heroNote1Title: { tab: "editorial", label: "ملاحظة الهيرو 1 — العنوان" },
  heroNote1Body: { tab: "editorial", label: "ملاحظة الهيرو 1 — الوصف" },
  heroNote2Title: { tab: "editorial", label: "ملاحظة الهيرو 2 — العنوان" },
  heroNote2Body: { tab: "editorial", label: "ملاحظة الهيرو 2 — الوصف" },
  heroNote3Title: { tab: "editorial", label: "ملاحظة الهيرو 3 — العنوان" },
  heroNote3Body: { tab: "editorial", label: "ملاحظة الهيرو 3 — الوصف" },
  flagshipBand1Title: { tab: "editorial", label: "بطاقة 1 — العنوان" },
  flagshipBand1Body: { tab: "editorial", label: "بطاقة 1 — الوصف" },
  flagshipBand2Title: { tab: "editorial", label: "بطاقة 2 — العنوان" },
  flagshipBand2Body: { tab: "editorial", label: "بطاقة 2 — الوصف" },
  flagshipBand3Title: { tab: "editorial", label: "بطاقة 3 — العنوان" },
  flagshipBand3Body: { tab: "editorial", label: "بطاقة 3 — الوصف" },
  smartBand1Title: { tab: "editorial", label: "مربع 1 — العنوان" },
  smartBand1Body: { tab: "editorial", label: "مربع 1 — الوصف" },
  smartBand2Title: { tab: "editorial", label: "مربع 2 — العنوان" },
  smartBand2Body: { tab: "editorial", label: "مربع 2 — الوصف" },
  smartBand3Title: { tab: "editorial", label: "مربع 3 — العنوان" },
  smartBand3Body: { tab: "editorial", label: "مربع 3 — الوصف" },
  smartBand4Title: { tab: "editorial", label: "مربع 4 — العنوان" },
  smartBand4Body: { tab: "editorial", label: "مربع 4 — الوصف" },
  heroShowcaseMode: { tab: "editorial", label: "محتوى مساحة العرض الرئيسية" },
  productLayout: { tab: "home", label: "تخطيط المنتجات" },
  productCardStyle: { tab: "home", label: "شكل بطاقة المنتج" },
  categoryLayout: { tab: "home", label: "تصميم بطاقات الأقسام" },
};

function previewButtonForeground(selected: string, background: string): string {
  if (!/^#[0-9a-fA-F]{6}$/.test(background)) return "#FFFFFF";
  const lum = (hex: string) => {
    const channels = [1, 3, 5].map(n => parseInt(hex.slice(n, n + 2), 16) / 255);
    const linear = channels.map(v => v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4);
    return linear[0] * .2126 + linear[1] * .7152 + linear[2] * .0722;
  };
  const white = 1.05 / (lum(background) + .05);
  const black = (lum(background) + .05) / .05;
  const fallback = white >= black ? "#FFFFFF" : "#000000";
  if (!/^#[0-9a-fA-F]{6}$/.test(selected)) return fallback;
  const a = lum(selected), b = lum(background);
  return (Math.max(a, b) + .05) / (Math.min(a, b) + .05) >= 4.5 ? selected : fallback;
}

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
  "mobile-flagship": {
    focus: "عرض فاخر ومفتوح للأجهزة الرائدة، مع صورة كبيرة ونص قوي ومساحات هادئة.",
    cards: "بطاقات بصرية أكبر بحركة خفيفة وتركيز على اسم الجهاز والسعر.",
    imagery: "صورة هيدر كبيرة وصور منتجات واسعة مع انتقالات ناعمة.",
    layout: "مصمم حصريًا لمتاجر الجوالات التي تريد حضورًا فاخرًا.",
    variants: [
      { id: "mf-champagne", name: "شامبانيا", primary: "#0b0d11", accent: "#c29a61" },
      { id: "mf-titanium", name: "تيتانيوم", primary: "#1c1f22", accent: "#8b989f" },
      { id: "mf-blue", name: "أزرق عميق", primary: "#111827", accent: "#6685d8" },
      { id: "mf-forest", name: "أخضر داكن", primary: "#101b18", accent: "#718f80" },
    ],
  },
  "mobile-smart-market": {
    focus: "بيع مباشر وواضح: بحث بارز، أقسام سهلة، ومنتجات يمكن قراءتها بسرعة.",
    cards: "بطاقات عملية ومريحة للمقارنة مع سعر وزر تفاصيل واضحين.",
    imagery: "صور مربعة نظيفة تناسب كتالوج الجوالات الكبير.",
    layout: "مصمم حصريًا لمتاجر الجوالات التي تركز على سرعة التصفح والشراء.",
    variants: [
      { id: "mm-blue", name: "أزرق تقني", primary: "#111827", accent: "#2563eb" },
      { id: "mm-green", name: "أخضر ذكي", primary: "#17231f", accent: "#23845d" },
      { id: "mm-orange", name: "برتقالي نشط", primary: "#272019", accent: "#d66b2d" },
      { id: "mm-violet", name: "بنفسجي تقني", primary: "#211d2b", accent: "#7358c6" },
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

function themeStoryForPreset(preset: ThemePreset): ThemeStory {
  const known = themeStories[preset.id];

  if (known) {
    return known;
  }

  const signature = preset.experience === "signature";

  return {
    focus: signature
      ? `هوية بصرية فاخرة تضع ${preset.productLabel ?? "المنتجات"} والصور في المقدمة.`
      : `واجهة تجارية واضحة تساعد العميل على الوصول إلى ${preset.productLabel ?? "المنتجات"} بسرعة.`,
    cards: signature
      ? "بطاقات واسعة بهدوء بصري ومساحات أكبر للصورة والتفاصيل."
      : "بطاقات عملية ومنظمة تساعد على المقارنة والشراء السريع.",
    imagery: signature
      ? "صور أكبر وتكوينات قصصية تناسب حضور العلامة."
      : "صور واضحة ومتناسقة تناسب الكتالوج والتصفح اليومي.",
    layout: `مصمم أساسًا لنشاط ${preset.verticalLabel ?? "المتجر"}.`,
    variants: [
      { id: `${preset.id}-base`, name: "الهوية الأساسية", primary: preset.ink, accent: preset.accent },
      { id: `${preset.id}-soft`, name: "هادئ", primary: preset.ink, accent: preset.inkSoft },
      { id: `${preset.id}-mono`, name: "محايد", primary: "#171817", accent: preset.accent },
    ],
  };
}

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
    visualContent: parseVisualContent(settings.visualContentJson),
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

function ThemePresetPreview({
  preset,
  storeName,
  featured = false,
}: {
  preset: ThemePreset;
  storeName: string;
  featured?: boolean;
}) {
  if (preset.id === "mobile-flagship") {
    return (
      <div
        className={`relative overflow-hidden rounded-[16px] p-4 text-white ${featured ? "h-48" : "h-36"}`}
        style={{ background: preset.ink }}
      >
        <div className="flex items-center justify-between border-b border-white/10 pb-2">
          <span className="text-[9px] font-semibold">{storeName}</span>
          <span className="h-2.5 w-8 rounded-full bg-white/15" />
        </div>
        <div className={`mt-4 grid items-center gap-4 ${featured ? "grid-cols-[1fr_118px]" : "grid-cols-[1fr_82px]"}`}>
          <div>
            <span className="block h-2 w-14 rounded-full" style={{ background: preset.accent }} />
            <span className="mt-2 block h-3 w-24 rounded bg-white/85" />
            <span className="mt-1.5 block h-2 w-20 rounded bg-white/25" />
          </div>
          <div className={`mx-auto rounded-[14px] border-2 border-white/50 bg-white/10 shadow-[0_18px_38px_rgba(0,0,0,.24)] ${featured ? "h-[112px] w-[62px]" : "h-[78px] w-[42px]"}`} />
        </div>
      </div>
    );
  }

  if (preset.id === "mobile-smart-market") {
    return (
      <div
        className={`rounded-[16px] p-4 ${featured ? "h-48" : "h-36"}`}
        style={{ background: preset.canvas, color: preset.ink }}
      >
        <div className="flex items-center gap-2">
          <span className="text-[9px] font-bold">{storeName}</span>
          <span className="h-5 flex-1 rounded-[6px] border border-black/5 bg-white" />
          <span className="size-5 rounded-[6px]" style={{ background: preset.accent }} />
        </div>
        <div className={`mt-4 grid grid-cols-3 ${featured ? "gap-3" : "gap-2"}`}>
          {[0, 1, 2].map((value) => (
            <div key={value} className="rounded-[7px] bg-white p-1.5 shadow-sm">
              <span className={`block rounded-[8px] ${featured ? "h-20" : "h-12"}`} style={{ background: preset.soft }} />
              <span className="mt-1.5 block h-1.5 w-4/5 rounded bg-black/15" />
              <span className="mt-1 block h-1.5 w-1/2 rounded" style={{ background: preset.accent }} />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (preset.experience === "signature") {
    return (
      <div
        className={`relative overflow-hidden rounded-[16px] border border-black/[0.05] ${featured ? "h-48" : "h-36"}`}
        style={{ background: preset.canvas, color: preset.ink }}
      >
        <div className="grid h-full grid-cols-[.9fr_1.1fr]">
          <div className="flex flex-col justify-center p-4">
            <span className="h-2 w-12 rounded-full" style={{ background: preset.accent }} />
            <span className="mt-3 block h-3 w-4/5 rounded" style={{ background: preset.ink, opacity: .88 }} />
            <span className="mt-2 block h-2 w-3/5 rounded" style={{ background: preset.ink, opacity: .20 }} />
            <span className="mt-1.5 block h-2 w-1/2 rounded" style={{ background: preset.ink, opacity: .12 }} />
          </div>
          <div className="m-2 overflow-hidden rounded-[12px]" style={{ background: preset.ink }}>
            <div className="h-full w-full opacity-80" style={{ background: `radial-gradient(circle at 35% 25%, ${preset.accent}66, transparent 42%), linear-gradient(145deg, ${preset.soft}, ${preset.ink})` }} />
          </div>
        </div>
      </div>
    );
  }

  if (preset.experience === "market") {
    return (
      <div
        className={`rounded-[16px] p-3 ${featured ? "h-48" : "h-36"}`}
        style={{ background: preset.canvas, color: preset.ink }}
      >
        <div className="flex items-center gap-2">
          <span className="text-[8px] font-bold">{storeName}</span>
          <span className="h-5 flex-1 rounded-[6px] border border-black/5" style={{ background: preset.surface }} />
          <span className="size-5 rounded-[6px]" style={{ background: preset.accent }} />
        </div>
        <div className="mt-3 grid grid-cols-3 gap-2">
          {[0, 1, 2].map((value) => (
            <div key={value} className="rounded-[8px] border border-black/[0.05] p-1.5" style={{ background: preset.surface }}>
              <span className={`block rounded-[7px] ${featured ? "h-20" : "h-12"}`} style={{ background: preset.soft }} />
              <span className="mt-1.5 block h-1.5 w-4/5 rounded" style={{ background: preset.ink, opacity: .18 }} />
              <span className="mt-1 block h-1.5 w-1/2 rounded" style={{ background: preset.accent }} />
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div
      className={`rounded-[14px] p-4 ${featured ? "h-44" : "h-32"}`}
      style={{ background: preset.canvas, color: preset.ink }}
    >
      <div className="flex items-center justify-between border-b pb-2" style={{ borderColor: `${preset.ink}18` }}>
        <span className="text-[9px] font-bold">{storeName}</span>
        <span className="size-3" style={{ background: preset.accent, borderRadius: preset.radius }} />
      </div>
      <div className="mt-3 grid grid-cols-3 gap-1.5">
        {[0, 1, 2].map((value) => (
          <span key={value} className={featured ? "h-20" : "h-12"} style={{ background: preset.surface, borderRadius: preset.radius }} />
        ))}
      </div>
    </div>
  );
}

export function AdminStorefrontPresentationPage() {
  const tenantId = getCurrentTenantId();
  const store = useMemo(() => readAdminStore(), []);
  const [form, setForm] = useState<FormState | null>(null);
  const [modalTab, setModalTab] = useState<"element" | "theme" | "map">("element");
  const [previewProducts, setPreviewProducts] = useState<Array<{ id: string; name: string }>>([]);
  const [activeTab, setActiveTab] = useState<DesignTab>("identity");
  const [loading, setLoading] = useState(Boolean(tenantId));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(tenantId ? null : "ما لقينا متجر مرتبط بالحساب.");
  const [saved, setSaved] = useState(false);
  const [previewFocus, setPreviewFocus] = useState<PreviewFocus>(null);
  const [requestedPreviewEdit, setRequestedPreviewEdit] = useState<PreviewFocus>(null);
  const [fullPreviewOpen, setFullPreviewOpen] = useState(false);
  const fullPreviewRef = useRef<HTMLIFrameElement | null>(null);
  const [previewSize, setPreviewSize] = useState<"desktop" | "mobile">("desktop");
  const [uploadingSlot, setUploadingSlot] = useState<AssetSlot | null>(null);
  const [commerceVerticalCode, setCommerceVerticalCode] = useState<string | null>(null);
  const logoInputRef = useRef<HTMLInputElement | null>(null);
  const coverInputRef = useRef<HTMLInputElement | null>(null);
  const showcaseInputRef = useRef<HTMLInputElement | null>(null);

  const storefrontVerticalCode = useMemo(() => {
    const source =
      commerceVerticalCode ??
      store?.verticalCode ??
      store?.verticalType ??
      null;

    if (!source) return null;

    const normalized = source
      .trim()
      .toLowerCase()
      .replaceAll("_", "-")
      .replaceAll(" ", "-");

    if (
      normalized === "mobilephones" ||
      normalized === "mobile-phones"
    ) {
      return "mobile-phones";
    }

    return normalized;
  }, [commerceVerticalCode, store]);

  const availableThemePresets = useMemo(
    () => getThemePresetsForVertical(storefrontVerticalCode),
    [storefrontVerticalCode],
  );

  const recommendedThemePresets = useMemo(
    () =>
      availableThemePresets.filter((preset) =>
        isThemeRecommendedForVertical(preset, storefrontVerticalCode),
      ),
    [availableThemePresets, storefrontVerticalCode],
  );

  const otherThemePresets = useMemo(
    () =>
      availableThemePresets.filter(
        (preset) => !isThemeRecommendedForVertical(preset, storefrontVerticalCode),
      ),
    [availableThemePresets, storefrontVerticalCode],
  );

  useEffect(() => {
    if (!tenantId) return;

    let cancelled = false;

    void getCommerceProfile(tenantId)
      .then((profile) => {
        if (cancelled) return;

        const primary =
          profile.verticals.find((vertical) => vertical.primary) ??
          profile.verticals[0] ??
          null;

        setCommerceVerticalCode(primary?.code ?? null);
      })
      .catch(() => {
        // Recommendation metadata must never block the design center.
        // The local store snapshot remains the fallback.
      });

    return () => {
      cancelled = true;
    };
  }, [tenantId]);

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

  function updateEditorial(key: keyof StorefrontVisualContent, value: string) {
    setSaved(false);
    setForm((current) => current ? {
      ...current,
      visualContent: { ...current.visualContent, [key]: value },
    } : current);
  }

  function applyThemePreset(preset: ThemePreset) {
    setSaved(false);
    setForm((current) =>
      current
        ? {
            ...current,
            themePresetCode: preset.id,
            primaryColor: preset.ink,
            accentColor: preset.accent,
            fontCode: preset.defaultFontId ?? current.fontCode,
          }
        : current,
    );
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
      visualContentJson: JSON.stringify(form.visualContent),
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
    setPreviewFocus(slot === "logo" ? "logo" : slot === "cover" ? "cover" : "heroShowcaseMode");

    try {
      const assetUrl = await uploadStorefrontAsset(tenantId, slot === "showcase" ? "cover" : slot, file);
      if (slot === "showcase") updateEditorial("heroShowcaseImage", assetUrl);
      else update(slot === "logo" ? "logoUrl" : "coverImageUrl", assetUrl);
    } catch (exception) {
      setError(
        exception instanceof Error ? exception.message : "تعذر رفع الصورة.",
      );
    } finally {
      setUploadingSlot(null);
      if (slot === "logo" && logoInputRef.current) logoInputRef.current.value = "";
      if (slot === "cover" && coverInputRef.current) coverInputRef.current.value = "";
      if (slot === "showcase" && showcaseInputRef.current) showcaseInputRef.current.value = "";
    }
  }

  useEffect(() => {
    if (!requestedPreviewEdit) return;
    const frame = window.requestAnimationFrame(() => {
      const input = document.querySelector<HTMLInputElement>(
        `[data-rukn-edit-target="${requestedPreviewEdit}"]`,
      );
      if (input) {
        input.scrollIntoView({ behavior: "smooth", block: "center" });
        input.focus({ preventScroll: true });
      }
      setRequestedPreviewEdit(null);
    });
    return () => window.cancelAnimationFrame(frame);
  }, [requestedPreviewEdit, activeTab]);

  useEffect(() => {
    function onPreviewSelection(event: MessageEvent) {
      if (event.origin !== window.location.origin ||
          event.source !== fullPreviewRef.current?.contentWindow) return;
      if (event.data?.type === "RUKN_VISUAL_PRODUCTS") {
        const incoming: unknown = event.data.products;
        if (Array.isArray(incoming)) setPreviewProducts(incoming.filter((item): item is {id: string; name: string} =>
          item && typeof item.id === "string" && typeof item.name === "string").slice(0, 80));
        return;
      }
      if (event.data?.type !== "RUKN_VISUAL_SELECT") return;
      const target: unknown = event.data.target;
      if (typeof target !== "string" ||
          !Object.prototype.hasOwnProperty.call(previewEditorTargets, target)) return;
      setPreviewFocus(target as Exclude<PreviewFocus, null>);
      setModalTab("element");
    }
    window.addEventListener("message", onPreviewSelection);
    return () => window.removeEventListener("message", onPreviewSelection);
  }, []);

  useEffect(() => {
    if (!fullPreviewOpen || !form) return;
    fullPreviewRef.current?.contentWindow?.postMessage({
      type: "RUKN_VISUAL_DRAFT",
      presentation: {
        logoUrl: form.logoUrl || null,
        coverImageUrl: form.coverImageUrl || null,
        announcement: form.announcement || null,
        primaryColor: form.primaryColor || null,
        accentColor: form.accentColor || null,
        themePresetCode: form.themePresetCode,
        fontCode: form.fontCode,
        showCategoriesOnHome: form.showCategoriesOnHome,
        showProductsOnHome: form.showProductsOnHome,
        categorySectionTitle: form.categorySectionTitle,
        productSectionTitle: form.productSectionTitle,
        visualContentJson: JSON.stringify(form.visualContent),
      },
    }, window.location.origin);
  }, [form, fullPreviewOpen, previewSize]);

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
  const activeThemeStory = themeStoryForPreset(theme);

  // Clicking an area in the preview reveals and focuses the existing editor field.
  // The preview never receives the merchant's authentication token or private data.
  function editFromPreview(target: Exclude<PreviewFocus, null>) {
    setPreviewFocus(target);
    setActiveTab(previewEditorTargets[target].tab);
    setRequestedPreviewEdit(target);
  }

  function previewControls(target: Exclude<PreviewFocus, null>) {
    return {
      role: "button" as const,
      tabIndex: 0,
      title: `تعديل ${previewEditorTargets[target].label}`,
      "aria-label": `تعديل ${previewEditorTargets[target].label}`,
      onClick: () => editFromPreview(target),
      onKeyDown: (event: KeyboardEvent<HTMLDivElement>) => {
        if (event.target !== event.currentTarget) return;
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          editFromPreview(target);
        }
      },
    };
  }

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
            <button type="button" onClick={() => setFullPreviewOpen(true)}
              className="inline-flex h-11 items-center gap-2 rounded-[9px] border border-[#315f5b]/25 bg-[#eef3ef] px-4 text-[10px] font-semibold text-[#193c30] transition hover:bg-[#e1ece4]">
              <Eye size={14} /> معاينة المتجر وتخصيصه
            </button>
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
                    data-rukn-edit-target="logo" value={form.logoUrl}
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
                    data-rukn-edit-target="cover" value={form.coverImageUrl}
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
                    data-rukn-edit-target="announcement" value={form.announcement}
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
              <section className="rounded-[20px] border border-black/[0.07] bg-white p-5 md:p-7">
                <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
                  <div>
                    <h2 className="text-[17px] font-semibold tracking-[-0.025em]">اختر شخصية الثيم</h2>
                    <p className="mt-1.5 max-w-2xl text-[10px] leading-6 text-black/42">
                      الثيمات الأنسب لنشاط متجرك تظهر أولًا، وكل ثيم يبقى قابلًا لتغيير الخط والألوان والمحتوى بعد الاختيار.
                    </p>
                  </div>
                  {storefrontVerticalCode ? (
                    <span className="rounded-full bg-[#edf5f2] px-3 py-1.5 text-[9px] font-bold text-[#315f5b]">
                      ثيمان موصى بهما لنشاط متجرك
                    </span>
                  ) : null}
                </div>

                {recommendedThemePresets.length > 0 ? (
                  <div className="mb-8">
                    <div className="mb-3 flex items-center gap-2">
                      <Sparkles size={14} className="text-[#9e7546]" />
                      <h3 className="text-[11px] font-bold text-black/70">موصى به لمتجرك</h3>
                    </div>
                    <div className="grid gap-4 xl:grid-cols-2">
                      {recommendedThemePresets.map((preset) => {
                        const active = form.themePresetCode === preset.id;
                        const story = themeStoryForPreset(preset);

                        return (
                          <button
                            key={preset.id}
                            type="button"
                            onClick={() => applyThemePreset(preset)}
                            className={`group relative overflow-hidden rounded-[18px] border bg-[#fcfbf8] p-3 text-right transition duration-300 ${active ? "border-[#8c6537] shadow-[0_18px_44px_rgba(79,58,34,.10)] ring-2 ring-[#8c6537]/10" : "border-black/[0.08] hover:-translate-y-0.5 hover:border-black/20 hover:shadow-[0_16px_38px_rgba(20,20,20,.07)]"}`}
                          >
                            <span className="absolute right-5 top-5 z-10 rounded-full border border-white/80 bg-white/94 px-3 py-1.5 text-[8px] font-bold text-[#315f5b] shadow-sm backdrop-blur">
                              موصى به لمتجرك
                            </span>
                            <ThemePresetPreview preset={preset} storeName={store?.name ?? "متجرك"} featured />
                            <div className="px-2 pb-2 pt-4">
                              <div className="flex items-center justify-between gap-3">
                                <div>
                                  <span className="text-[14px] font-bold tracking-[-0.02em]">{preset.name}</span>
                                  <p className="mt-1 text-[9px] font-semibold text-[#8c6537]">مصمم أساسًا لنشاط {preset.verticalLabel ?? "متجرك"}</p>
                                </div>
                                <div className="flex items-center gap-2">
                                  {preset.planBadge ? (
                                    <span className="rounded-full bg-[#eef2f6] px-2.5 py-1 text-[8px] font-bold text-[#42536a]">
                                      {preset.planBadge}
                                    </span>
                                  ) : null}
                                  {active ? <span className="flex size-7 items-center justify-center rounded-full bg-[#17221d] text-white"><Check size={14} /></span> : null}
                                </div>
                              </div>
                              <p className="mt-3 max-w-xl text-[10px] leading-6 text-black/46">{preset.description}</p>
                              <div className="mt-3 flex flex-wrap gap-2">
                                {story.variants.slice(0, 4).map((variant) => (
                                  <span key={variant.id} className="size-3.5 rounded-full border border-black/10 shadow-sm" style={{ background: variant.accent }} />
                                ))}
                              </div>
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  </div>
                ) : null}

                <div>
                  <div className="mb-3 flex items-center justify-between gap-3">
                    <h3 className="text-[11px] font-bold text-black/62">{recommendedThemePresets.length ? "بقية الثيمات" : "جميع الثيمات"}</h3>
                    <span className="text-[8px] text-black/35">كل الخيارات تبقى متاحة دائمًا</span>
                  </div>
                  <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                    {otherThemePresets.map((preset) => {
                      const active = form.themePresetCode === preset.id;
                      const story = themeStoryForPreset(preset);
                      return (
                        <button
                          key={preset.id}
                          type="button"
                          onClick={() => applyThemePreset(preset)}
                          className={`relative overflow-hidden rounded-[16px] border bg-white p-2.5 text-right transition ${active ? "border-[#8c6537] ring-2 ring-[#8c6537]/12" : "border-black/[0.08] hover:-translate-y-0.5 hover:border-black/20 hover:shadow-[0_12px_30px_rgba(20,20,20,.05)]"}`}
                        >
                          <ThemePresetPreview preset={preset} storeName={store?.name ?? "متجرك"} />
                          <div className="px-1.5 pb-1.5 pt-3">
                            <div className="flex items-center justify-between gap-2">
                              <span className="text-[11px] font-semibold">{preset.name}</span>
                              <div className="flex items-center gap-1.5">
                                {preset.planBadge ? (
                                  <span className="rounded-full bg-[#eef2f6] px-2 py-0.5 text-[7px] font-bold text-[#42536a]">
                                    {preset.planBadge}
                                  </span>
                                ) : null}
                                {active ? <Check size={13} className="text-[#76552f]" /> : null}
                              </div>
                            </div>
                            <p className="mt-1.5 text-[8px] leading-4 text-black/40">{preset.description}</p>
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
                <h2 className="text-[14px] font-semibold">تخصيص ألوان النصوص والأزرار</h2>
                <p className="mt-1 text-[10px] leading-6 text-black/45">تُعرض الألوان مباشرة في معاينة المتجر قبل حفظها.</p>
                <div className="mt-5 grid gap-4 sm:grid-cols-2">
                  {([ ["bodyTextColor", "لون النص الأساسي", theme.ink],
                      ["primaryButtonColor", "لون الزر الأساسي", previewPrimary],
                      ["primaryButtonTextColor", "لون الكتابة داخل الزر", "#FFFFFF"],
                      ["accentButtonTextColor", "لون الكتابة داخل الأزرار المميزة", "#FFFFFF"] ] as const).map(([key, label, fallback]) => (
                    <label key={key} className="space-y-2 text-[11px] font-semibold">{label}
                      <input type="color" className="block h-12 w-full cursor-pointer rounded-lg border border-black/10"
                        value={/^#[0-9a-fA-F]{6}$/.test(form.visualContent[key]) ? form.visualContent[key] : fallback}
                        onChange={event => updateEditorial(key, event.target.value)}/>
                    </label>
                  ))}
                </div>
                <div className="mt-4 rounded-xl border border-black/10 p-4" style={{background: theme.surface, color: form.visualContent.bodyTextColor || theme.ink}}>
                  <span className="mb-3 block text-xs">هذا مثال لنص الزر ولون الخط الأساسي.</span>
                  <span className="inline-flex rounded-lg px-5 py-3 text-xs font-semibold" style={{background: form.visualContent.primaryButtonColor || previewPrimary,
                    color: previewButtonForeground(form.visualContent.primaryButtonTextColor, form.visualContent.primaryButtonColor || previewPrimary)}}>إضافة إلى السلة</span>
                </div>
                <p className="mt-3 text-[10px] leading-5 text-black/50">في حال اختيار لونين متقاربين للنص وخلفيته، يستخدم الموقع تلقائيًا لونًا أوضح عند العرض.</p>
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

          {activeTab === "editorial" ? (
            <section className="rounded-[16px] border border-black/[0.07] bg-white p-5 md:p-7">
              <div className="mb-6 border-b border-black/[0.06] pb-5">
                <h2 className="text-[16px] font-semibold">نصوص الواجهة</h2>
                <p className="mt-2 text-[10px] leading-6 text-black/45">اضغط على العنصر في المعاينة، ثم حرّر النص هنا. يتم حفظ المحتوى على خادم المتجر بعد الضغط على حفظ التغييرات.</p>
              </div>
              <div className="grid gap-4">
                {([
                  ["heroEyebrow", "العبارة أعلى الهيرو", "text"],
                  ["heroTitle", "عنوان الهيرو الرئيسي", "textarea"],
                  ["heroDescription", "وصف الهيرو", "textarea"],
                  ["heroCtaLabel", "نص الزر الرئيسي", "text"],
                  ["heroCtaHref", "رابط الزر الداخلي (#products أو /store/...)", "text"],
                  ["heroSecondaryImage", "رابط الصورة الثانوية (HTTPS)", "text"],
                  ["categoryEyebrow", "العبارة أعلى عنوان الأقسام", "text"],
                  ["productEyebrow", "العبارة أعلى عنوان المنتجات", "text"],
                  ["footerDescription", "وصف المتجر في الفوتر", "textarea"],
                ] as const).map(([key, label, fieldType]) => (
                  <label key={key} className="block rounded-[12px] border border-black/[0.07] bg-[#fafaf7] p-4">
                    <span className={labelClass}>{label}</span>
                    {fieldType === "textarea" ? (
                      <textarea
                        data-rukn-edit-target={key}
                        value={form.visualContent[key]}
                        onFocus={() => setPreviewFocus(key)}
                        onChange={(event) => updateEditorial(key, event.target.value)}
                        rows={3}
                        maxLength={key === "heroTitle" ? 180 : 500}
                        className={`${inputClass} h-auto min-h-24 py-3 leading-6`}
                      />
                    ) : (
                      <input
                        data-rukn-edit-target={key}
                        dir={key.includes("Href") || key.includes("Image") ? "ltr" : "rtl"}
                        value={form.visualContent[key]}
                        onFocus={() => setPreviewFocus(key)}
                        onChange={(event) => updateEditorial(key, event.target.value)}
                        className={inputClass}
                        maxLength={key === "heroCtaHref" ? 300 : key === "heroSecondaryImage" ? 2048 : key === "heroCtaLabel" || key === "categoryEyebrow" || key === "productEyebrow" ? 80 : 100}
                      />
                    )}
                  </label>
                ))}
              </div>
            </section>
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
                    <input data-rukn-edit-target="categories" value={form.categorySectionTitle} onFocus={() => setPreviewFocus("categories")} onChange={(event) => update("categorySectionTitle", event.target.value)} className={inputClass} />
                    <TargetHint onClick={() => setPreviewFocus("categories")}>هذا هو عنوان قسم الأقسام داخل المعاينة</TargetHint>
                  </div>
                ) : null}

                <Toggle checked={form.showProductsOnHome} onChange={(value) => update("showProductsOnHome", value)} label="إظهار المنتجات في الرئيسية" description="إذا أوقفته يختفي عرض المنتجات من الصفحة الرئيسية فقط." />
                {form.showProductsOnHome ? (
                  <div className="rounded-[12px] border border-black/[0.07] bg-[#fafaf7] p-4">
                    <label className={labelClass}>عنوان قسم المنتجات</label>
                    <input data-rukn-edit-target="products" value={form.productSectionTitle} onFocus={() => setPreviewFocus("products")} onChange={(event) => update("productSectionTitle", event.target.value)} className={inputClass} />
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
                <p className="mt-1 text-[8px] text-black/35">انقر على أي صورة أو عنوان داخل المعاينة للانتقال مباشرة إلى حقل التعديل</p>
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
                <div {...previewControls("announcement")} className={`${sectionFocusClass("announcement")} cursor-pointer outline-offset-2 transition hover:outline hover:outline-2 hover:outline-[#9e7546] focus-visible:outline focus-visible:outline-2 focus-visible:outline-[#9e7546]`}>
                  <div className="px-4 py-2 text-center text-[8px] font-semibold" style={{ background: previewPrimary, color: theme.surface }}>
                    {form.announcement}
                  </div>
                </div>
              ) : (
                <div {...previewControls("announcement")} className="cursor-pointer border-b border-dashed border-black/15 px-4 py-2 text-center text-[9px] text-black/40 outline-offset-2 hover:bg-[#f1e9dc] focus-visible:outline focus-visible:outline-2 focus-visible:outline-[#9e7546]">
                  اضغط لإضافة شريط الإعلان
                </div>
              )}

              <div {...previewControls("logo")} className={`flex min-h-[64px] cursor-pointer items-center justify-between px-5 outline-offset-2 transition hover:outline hover:outline-2 hover:outline-[#9e7546] focus-visible:outline focus-visible:outline-2 focus-visible:outline-[#9e7546] ${sectionFocusClass("logo")}`} style={{ borderBottom: `1px solid ${previewPrimary}15` }}>
                {form.logoUrl.trim() ? (
                  <img src={form.logoUrl.trim()} alt="شعار المتجر" className="max-h-9 max-w-[130px] object-contain" />
                ) : (
                  <span className="text-[15px] font-bold">{store?.name ?? "اسم المتجر"}</span>
                )}
                <span className="text-[8px] opacity-45">الرئيسية · المجموعة · قصتنا</span>
              </div>

              <div {...previewControls("cover")} className={`${sectionFocusClass("cover")} cursor-pointer outline-offset-2 transition hover:outline hover:outline-2 hover:outline-[#9e7546] focus-visible:outline focus-visible:outline-2 focus-visible:outline-[#9e7546]`}>
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

              <div className="border-b border-black/[0.06] p-5">
                <div {...previewControls("heroEyebrow")} className={`cursor-pointer rounded px-2 py-1 text-[9px] opacity-65 hover:outline hover:outline-2 hover:outline-[#9e7546] ${sectionFocusClass("heroEyebrow")}`}>
                  {form.visualContent.heroEyebrow || "اضغط لإضافة العبارة الصغيرة"}
                </div>
                <div {...previewControls("heroTitle")} className={`mt-2 cursor-pointer rounded px-2 py-1 text-[19px] font-semibold leading-8 hover:outline hover:outline-2 hover:outline-[#9e7546] ${sectionFocusClass("heroTitle")}`}>
                  {form.visualContent.heroTitle || "اضغط لكتابة عنوان الهيرو"}
                </div>
                <div {...previewControls("heroDescription")} className={`mt-1 cursor-pointer rounded px-2 py-1 text-[10px] leading-5 opacity-65 hover:outline hover:outline-2 hover:outline-[#9e7546] ${sectionFocusClass("heroDescription")}`}>
                  {form.visualContent.heroDescription || "اضغط لإضافة وصف الهيرو"}
                </div>
                <div className="mt-3 flex flex-wrap items-center gap-2">
                  <div {...previewControls("heroCtaLabel")} className={`cursor-pointer rounded-full px-3 py-2 text-[9px] hover:outline hover:outline-2 hover:outline-[#9e7546] ${sectionFocusClass("heroCtaLabel")}`} style={{ background: form.visualContent.primaryButtonColor || previewPrimary, color: previewButtonForeground(form.visualContent.primaryButtonTextColor, form.visualContent.primaryButtonColor || previewPrimary) }}>
                    {form.visualContent.heroCtaLabel || "نص الزر الرئيسي"}
                  </div>
                  <div {...previewControls("heroCtaHref")} className={`cursor-pointer rounded-full border border-black/10 px-3 py-2 text-[8px] opacity-60 hover:outline hover:outline-2 hover:outline-[#9e7546] ${sectionFocusClass("heroCtaHref")}`}>
                    تعديل رابط الزر
                  </div>
                  <div {...previewControls("heroSecondaryImage")} className={`cursor-pointer rounded-full border border-black/10 px-3 py-2 text-[8px] opacity-60 hover:outline hover:outline-2 hover:outline-[#9e7546] ${sectionFocusClass("heroSecondaryImage")}`}>
                    الصورة الثانوية
                  </div>
                </div>
              </div>

              <div className="p-5">
                {form.showCategoriesOnHome ? (
                  <div {...previewControls("categories")} className={`mb-6 cursor-pointer rounded-[10px] outline-offset-2 transition hover:outline hover:outline-2 hover:outline-[#9e7546] focus-visible:outline focus-visible:outline-2 focus-visible:outline-[#9e7546] ${sectionFocusClass("categories")}`}>
                    <span className="inline-flex rounded-full bg-[#f1e9dc] px-2 py-1 text-[8px] text-[#7e5c34]">اضغط لتعديل القسم</span>
                    <p {...previewControls("categoryEyebrow")} className={`cursor-pointer rounded text-[7px] opacity-55 hover:outline hover:outline-2 hover:outline-[#9e7546] ${sectionFocusClass("categoryEyebrow")}`}>{form.visualContent.categoryEyebrow || "الأقسام"}</p>
                    <h3 className="mt-1 text-[17px] font-semibold">{form.categorySectionTitle || "تصفح الأقسام"}</h3>
                    <div className="mt-3 grid grid-cols-3 gap-2">
                      {[0, 1, 2].map((value) => <span key={value} className="h-16" style={{ background: theme.surface, borderRadius: theme.radius }} />)}
                    </div>
                  </div>
                ) : null}

                {form.showProductsOnHome ? (
                  <div {...previewControls("products")} className={`cursor-pointer rounded-[10px] outline-offset-2 transition hover:outline hover:outline-2 hover:outline-[#9e7546] focus-visible:outline focus-visible:outline-2 focus-visible:outline-[#9e7546] ${sectionFocusClass("products")}`}>
                    <span className="inline-flex rounded-full bg-[#f1e9dc] px-2 py-1 text-[8px] text-[#7e5c34]">اضغط لتعديل القسم</span>
                    <p {...previewControls("productEyebrow")} className={`cursor-pointer rounded text-[7px] opacity-55 hover:outline hover:outline-2 hover:outline-[#9e7546] ${sectionFocusClass("productEyebrow")}`}>{form.visualContent.productEyebrow || "المنتجات"}</p>
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

            <div {...previewControls("footerDescription")} className={`mt-3 cursor-pointer rounded-[12px] border border-black/[0.08] p-4 text-[10px] leading-6 hover:outline hover:outline-2 hover:outline-[#9e7546] ${sectionFocusClass("footerDescription")}`} style={{ background: previewPrimary, color: theme.surface }}>
              <p className="text-[8px] opacity-65">الفوتر — وصف المتجر</p>
              <p className="mt-1">{form.visualContent.footerDescription || "اضغط لتعديل وصف المتجر في الفوتر"}</p>
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
      {fullPreviewOpen && storefrontHref ? (
        <div className="fixed inset-0 z-[100] flex flex-col bg-[#f2f3f0] p-2 md:p-4" role="dialog" aria-modal="true" aria-label="محرر مظهر المتجر">
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2 rounded-xl border border-black/10 bg-white px-4 py-3">
            <div>
              <h2 className="text-sm font-semibold">محرر ركن البصري</h2>
              <p className="text-[11px] text-black/50">شاهد تعديلاتك مباشرة، ولن تتغير واجهة الزوار حتى تضغط حفظ.</p>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <button type="button" onClick={() => setPreviewSize("desktop")} aria-pressed={previewSize === "desktop"} className={`rounded-lg px-3 py-2 text-xs ${previewSize === "desktop" ? "bg-[#193c30] text-white" : "bg-[#f4f4f1]"}`}>كمبيوتر</button>
              <button type="button" onClick={() => setPreviewSize("mobile")} aria-pressed={previewSize === "mobile"} className={`rounded-lg px-3 py-2 text-xs ${previewSize === "mobile" ? "bg-[#193c30] text-white" : "bg-[#f4f4f1]"}`}>جوال</button>
              <button type="button" disabled={saving} onClick={() => void save()} className="rounded-lg bg-[#193c30] px-4 py-2 text-xs font-semibold text-white disabled:opacity-50">{saving ? "جارٍ الحفظ" : saved ? "تم الحفظ ✓" : "حفظ التغييرات"}</button>
              <button type="button" onClick={() => setFullPreviewOpen(false)} className="rounded-lg border border-black/10 px-4 py-2 text-xs">إغلاق ×</button>
            </div>
          </div>
          {error ? <p role="alert" className="mb-2 rounded-lg bg-red-50 px-3 py-2 text-xs text-red-700">{error}</p> : null}
          <div className="flex min-h-0 flex-1 flex-col-reverse gap-3 lg:flex-row-reverse">
            <aside dir="rtl" className="w-full shrink-0 overflow-y-auto rounded-xl border border-black/10 bg-white p-4 lg:w-[320px]">
              <nav className="mb-4 grid grid-cols-3 gap-1 rounded-lg bg-[#f2f3f0] p-1" aria-label="أدوات التصميم">
                {([ ["element", "العنصر"], ["theme", "الثيمات"], ["map", "الخريطة"] ] as const).map(([id, label]) => (
                  <button key={id} type="button" onClick={() => setModalTab(id)} className={`rounded-md px-2 py-2 text-[11px] ${modalTab === id ? "bg-white font-semibold shadow-sm" : "text-black/60"}`}>{label}</button>
                ))}
              </nav>
              {modalTab === "element" ? (
                <div className="space-y-4">
                  <p className="text-xs text-black/50">اضغط على أي عنصر قابل للتحرير داخل معاينة المتجر.</p>
                  {previewFocus ? (
                    <>
                      <h3 className="text-sm font-semibold">{previewEditorTargets[previewFocus].label}</h3>

                      {previewFocus === "heroShowcaseMode" ? (
                        <div className="space-y-4">
                          <label className={labelClass}>ماذا تعرض هذه المساحة؟</label>
                          <div className="grid grid-cols-2 gap-2">
                            {([ ["default", "تصميم الثيم"], ["image", "صورة"], ["text", "نص كبير"], ["product", "منتج واحد"], ["products", "عدة منتجات"] ] as const).map(([mode, label]) => (
                              <button key={mode} type="button" onClick={() => updateEditorial("heroShowcaseMode", mode)}
                                className={`rounded-lg border p-3 text-xs ${form.visualContent.heroShowcaseMode === mode ? "border-[#315F5B] bg-[#f1f7f3] font-semibold" : "border-black/10"}`}>{label}</button>
                            ))}
                          </div>
                          {form.visualContent.heroShowcaseMode === "text" ? <textarea maxLength={500} className={`${inputClass} min-h-28 py-3`}
                            value={form.visualContent.heroShowcaseText} onChange={(event) => updateEditorial("heroShowcaseText", event.target.value)}
                            placeholder="اكتب العبارة التي تريد إبرازها" /> : null}
                          {form.visualContent.heroShowcaseMode === "image" ? <>
                            <input dir="ltr" maxLength={2048} className={inputClass} placeholder="رابط صورة HTTPS"
                              value={form.visualContent.heroShowcaseImage} onChange={(event) => updateEditorial("heroShowcaseImage", event.target.value)} />
                            <input ref={showcaseInputRef} type="file" accept="image/png,image/jpeg,image/webp"
                              onChange={(event) => void handleAssetUpload("showcase", event.target.files?.[0] || null)} className="sr-only" />
                            <button type="button" disabled={uploadingSlot !== null} onClick={() => showcaseInputRef.current?.click()}
                              className="w-full rounded-lg border border-black/10 p-3 text-xs">{uploadingSlot === "showcase" ? "جارٍ رفع الصورة" : "رفع صورة من الجهاز"}</button>
                          </> : null}
                          {form.visualContent.heroShowcaseMode === "product" || form.visualContent.heroShowcaseMode === "products" ? <>
                            <p className="text-xs text-black/60">اختر المنتجات الحقيقية التي تريد إبرازها (حتى 4). لا يتم نسخ الأسعار أو المخزون إلى التصميم.</p>
                            {previewProducts.length ? previewProducts.map((product) => {
                              const selected = form.visualContent.featuredProductIds.split(",").filter(Boolean);
                              return <label key={product.id} className="flex cursor-pointer items-center gap-2 rounded-lg border border-black/10 p-3 text-xs">
                                <input type={form.visualContent.heroShowcaseMode === "product" ? "radio" : "checkbox"}
                                  name={form.visualContent.heroShowcaseMode === "product" ? "hero-featured" : undefined}
                                  checked={selected.includes(product.id)} onChange={(event) => {
                                    const ids = form.visualContent.heroShowcaseMode === "product" ? [product.id] :
                                      event.target.checked ? [...selected.filter((id) => id !== product.id), product.id].slice(0, 4) : selected.filter((id) => id !== product.id);
                                    updateEditorial("featuredProductIds", ids.join(","));
                                  }}/><span>{product.name}</span>
                              </label>;
                            }) : <p className="text-xs text-black/50">بانتظار تحميل المنتجات في معاينة المتجر…</p>}
                          </> : null}
                        </div>
                      ) : previewFocus === "productLayout" || previewFocus === "productCardStyle" ? (
                        <div className="space-y-4">
                          <label className={labelClass}>طريقة ترتيب المنتجات</label>
                          <div className="grid grid-cols-2 gap-2">{([ ["theme-default", "تخطيط الثيم"], ["grid-3", "شبكة منتجات"], ["featured-grid", "منتج مميز + شبكة"], ["horizontal", "عرض أفقي"], ["spotlight", "منتج بارز"] ] as const).map(([value, label]) => (
                            <button key={value} type="button" onClick={() => updateEditorial("productLayout", value)}
                              className={`rounded-lg border p-3 text-xs ${form.visualContent.productLayout === value ? "border-[#315F5B] bg-[#f1f7f3]" : "border-black/10"}`}>{label}</button>
                          ))}</div>
                          <label className={labelClass}>شكل بطاقة المنتج</label>
                          <div className="grid grid-cols-2 gap-2">{([ ["theme-default", "بطاقة الثيم"], ["minimal", "بسيطة"], ["editorial", "تحريرية"], ["commerce", "تجارية"], ["compact", "مدمجة"], ["technical", "تقنية"] ] as const).map(([value, label]) => (
                            <button key={value} type="button" onClick={() => updateEditorial("productCardStyle", value)}
                              className={`rounded-lg border p-3 text-xs ${form.visualContent.productCardStyle === value ? "border-[#315F5B] bg-[#f1f7f3]" : "border-black/10"}`}>{label}</button>
                          ))}</div>
                        </div>
                      ) : previewFocus === "categoryLayout" ? (
                        <div className="space-y-3"><label className={labelClass}>شكل بطاقات الأقسام</label>
                          {([ ["theme-default", "التصميم الأصلي للثيم"], ["grid", "بطاقات صور"], ["compact", "بطاقات أفقية"] ] as const).map(([value, label]) => (
                            <button key={value} type="button" onClick={() => updateEditorial("categoryCardLayout", value)}
                              className={`block w-full rounded-lg border p-3 text-right text-xs ${form.visualContent.categoryCardLayout === value ? "border-[#315F5B] bg-[#f1f7f3]" : "border-black/10"}`}>{label}</button>
                          ))}
                        </div>
                      ) : null}
                      {previewFocus === "heroShowcaseMode" || previewFocus === "productLayout" || previewFocus === "productCardStyle" || previewFocus === "categoryLayout" ? null : previewFocus === "logo" || previewFocus === "cover" ? (
                        <>
                          <input dir="ltr" className={inputClass} value={previewFocus === "logo" ? form.logoUrl : form.coverImageUrl} onChange={(event) => update(previewFocus === "logo" ? "logoUrl" : "coverImageUrl", event.target.value)} placeholder="رابط الصورة" aria-label="رابط الصورة" />
                          <button type="button" disabled={uploadingSlot !== null} onClick={() => previewFocus === "logo" ? logoInputRef.current?.click() : coverInputRef.current?.click()} className="w-full rounded-lg border border-black/10 px-3 py-2 text-xs">{uploadingSlot ? "جارٍ الرفع" : "رفع صورة من الجهاز"}</button>
                        </>
                      ) : previewFocus === "categories" || previewFocus === "products" || previewFocus === "announcement" ? (
                        <input className={inputClass} value={previewFocus === "categories" ? form.categorySectionTitle : previewFocus === "products" ? form.productSectionTitle : form.announcement} onChange={(event) => update(previewFocus === "categories" ? "categorySectionTitle" : previewFocus === "products" ? "productSectionTitle" : "announcement", event.target.value)} aria-label={previewEditorTargets[previewFocus].label} />
                      ) : (
                        <textarea dir={previewFocus === "heroCtaHref" || previewFocus === "heroSecondaryImage" ? "ltr" : "rtl"} className={`${inputClass} min-h-24 py-3`} value={form.visualContent[previewFocus]} onChange={(event) => updateEditorial(previewFocus, event.target.value)} aria-label={previewEditorTargets[previewFocus].label} />
                      )}
                      <p className="text-[11px] leading-5 text-black/45">يظهر التغيير فورًا في المعاينة، ثم احفظ لاعتماده في متجرك.</p>
                    </>
                  ) : <p className="rounded-lg bg-[#f2f3f0] p-4 text-xs text-black/60">اختر العنوان أو الصورة أو القسم من المتجر لعرض خصائصه هنا.</p>}
                </div>
              ) : null}
              {modalTab === "theme" ? (
                <div className="space-y-5">
                  <h3 className="text-sm font-semibold">الثيمات المتاحة</h3>
                  <div className="grid grid-cols-2 gap-2">
                    {availableThemePresets.map((preset) => (
                      <button type="button" key={preset.id} onClick={() => applyThemePreset(preset)} aria-pressed={form.themePresetCode === preset.id} className={`rounded-xl border p-2 text-right ${form.themePresetCode === preset.id ? "border-[#193c30] ring-1 ring-[#193c30]" : "border-black/10"}`}>
                        <div className="mb-2 flex h-14 overflow-hidden rounded-md" style={{ background: preset.surface }}><span className="h-full w-1/3" style={{ background: preset.ink }} /><span className="h-full w-2/3" style={{ background: preset.accent, opacity: .8 }} /></div>
                        <span className="text-[11px] font-semibold">{preset.name}</span>
                      </button>
                    ))}
                  </div>
                  <div><h3 className="mb-2 text-xs font-semibold">ألوان الهوية</h3>
                    <div className="grid grid-cols-2 gap-3">
                      {([ ["primaryColor", "الأساسي"], ["accentColor", "المميز"] ] as const).map(([key, label]) => (
                        <label key={key} className="space-y-2 text-[11px]">{label}<input type="color" className="block h-12 w-full cursor-pointer rounded-lg border border-black/10" value={/^#[0-9a-fA-F]{6}$/.test(form[key]) ? form[key] : key === "primaryColor" ? theme.ink : theme.accent} onChange={(event) => update(key, event.target.value)} /></label>
                      ))}
                    </div>
                  </div>
                  <div className="rounded-xl border border-black/10 bg-[#f8faf8] p-3">
                    <h3 className="mb-3 text-xs font-semibold">ألوان الخط والأزرار</h3>
                    <div className="grid grid-cols-2 gap-3">
                      {([ ["bodyTextColor", "لون النص الأساسي", theme.ink],
                          ["primaryButtonColor", "لون الزر الأساسي", previewPrimary],
                          ["primaryButtonTextColor", "لون الكتابة داخل الزر", "#FFFFFF"],
                          ["accentButtonTextColor", "كتابة الزر المميز", "#FFFFFF"] ] as const).map(([key, label, fallback]) => (
                        <label key={key} className="space-y-2 text-[11px]">{label}
                          <input type="color" className="block h-11 w-full rounded-lg border border-black/10"
                            value={/^#[0-9a-fA-F]{6}$/.test(form.visualContent[key]) ? form.visualContent[key] : fallback}
                            onChange={event => updateEditorial(key, event.target.value)}/>
                        </label>
                      ))}
                    </div>
                    <p className="mt-3 text-[10px] leading-5 text-black/55">يحافظ المتجر تلقائيًا على تباين مقروء لنص الأزرار إذا اختير لون غير واضح.</p>
                    <div className="mt-3 rounded-lg border border-black/10 bg-white p-3" style={{color: form.visualContent.bodyTextColor || theme.ink}}>
                      <p className="mb-2 text-xs">معاينة لون النص والزر</p>
                      <span className="inline-flex rounded-lg px-5 py-2 text-xs font-bold" style={{background: form.visualContent.primaryButtonColor || previewPrimary,
                        color: previewButtonForeground(form.visualContent.primaryButtonTextColor, form.visualContent.primaryButtonColor || previewPrimary)}}>إضافة إلى السلة</span>
                    </div>
                  </div>
                  <div><h3 className="mb-2 text-xs font-semibold">الخط</h3>
                    <div className="space-y-2">{STOREFRONT_FONTS.map((option) => <button type="button" key={option.id} onClick={() => update("fontCode", option.id)} aria-pressed={form.fontCode === option.id} className={`block w-full rounded-lg border px-3 py-3 text-right text-sm ${form.fontCode === option.id ? "border-[#193c30] bg-[#edf2ee]" : "border-black/10"}`}>{option.name}</button>)}</div>
                  </div>
                </div>
              ) : null}
              {modalTab === "map" ? (
                <div className="space-y-4"><h3 className="text-sm font-semibold">خريطة الصفحة الرئيسية</h3>
                  <p className="text-[11px] text-black/50">نقل الأقسام للأعلى أو الأسفل وتحديد ظهورها. رأس الصفحة والفوتر عناصر ثابتة.</p>
                  <div className="rounded-lg border border-black/10 bg-[#f8f8f6] px-3 py-3 text-xs">رأس الصفحة · ثابت</div>
                  {(form.visualContent.sectionOrder || "categories,products,banner,story").split(",").filter((section) => section === "categories" || section === "products").map((section, index, sections) => (
                    <div key={section} className="rounded-lg border border-black/10 p-3">
                      <div className="mb-2 flex items-center justify-between gap-2"><strong className="text-xs">{section === "categories" ? "الأقسام" : "المنتجات"}</strong><button type="button" onClick={() => update(section === "categories" ? "showCategoriesOnHome" : "showProductsOnHome", section === "categories" ? !form.showCategoriesOnHome : !form.showProductsOnHome)} className="rounded-lg bg-[#f2f3f0] px-2 py-1 text-[11px]">{(section === "categories" ? form.showCategoriesOnHome : form.showProductsOnHome) ? "إخفاء" : "إظهار"}</button></div>
                      <button type="button" disabled={index === 0} onClick={() => { const order = (form.visualContent.sectionOrder || "categories,products,banner,story").split(","); const at = order.indexOf(section); [order[at - 1], order[at]] = [order[at], order[at - 1]]; updateEditorial("sectionOrder", order.join(",")); }} className="me-2 rounded-lg border border-black/10 px-3 py-1.5 text-[11px] disabled:opacity-30">↑ للأعلى</button>
                      <button type="button" disabled={index === sections.length - 1} onClick={() => { const order = (form.visualContent.sectionOrder || "categories,products,banner,story").split(","); const at = order.indexOf(section); [order[at], order[at + 1]] = [order[at + 1], order[at]]; updateEditorial("sectionOrder", order.join(",")); }} className="rounded-lg border border-black/10 px-3 py-1.5 text-[11px] disabled:opacity-30">↓ للأسفل</button>
                    </div>
                  ))}
                  <div className="flex flex-wrap gap-2">
                    <button type="button" className="rounded-lg border border-black/10 px-3 py-2 text-xs" onClick={() => { setPreviewFocus("heroShowcaseMode"); setModalTab("element"); }}>محتوى الهيرو</button>
                    <button type="button" className="rounded-lg border border-black/10 px-3 py-2 text-xs" onClick={() => { setPreviewFocus("categoryLayout"); setModalTab("element"); }}>بطاقات الأقسام</button>
                    <button type="button" className="rounded-lg border border-black/10 px-3 py-2 text-xs" onClick={() => { setPreviewFocus("productLayout"); setModalTab("element"); }}>بطاقات المنتجات</button>
                  </div>
                  <div className="rounded-lg border border-black/10 bg-[#f8f8f6] px-3 py-3 text-xs">تذييل الصفحة · ثابت</div>
                  <p className="text-[11px] text-black/50">لتغيير شكل بطاقات المنتجات وتخطيطها، اضغط على أي بطاقة في المعاينة أو على زر «بطاقات المنتجات» أعلاه. ترتيب المنتجات فرديًا سيكون ضمن مرحلة مستقلة.</p>
                </div>
              ) : null}
            </aside>
            <div className="min-h-[260px] min-w-0 flex-1 overflow-auto rounded-xl border border-black/10 bg-[#deded9] p-2">
              <iframe
                ref={fullPreviewRef}
                title="المتجر الحقيقي — معاينة التعديلات"
                src={`${storefrontHref}?builderPreview=1`}
                onLoad={() => {
                  if (!form) return;
                  fullPreviewRef.current?.contentWindow?.postMessage({ type: "RUKN_VISUAL_DRAFT", presentation: { ...form, visualContentJson: JSON.stringify(form.visualContent) } }, window.location.origin);
                }}
                className="mx-auto block h-full min-h-[420px] border-0 bg-white shadow-[0_8px_40px_rgba(0,0,0,.09)]"
                style={{ width: previewSize === "mobile" ? "390px" : "100%", maxWidth: "100%" }}
              />
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
