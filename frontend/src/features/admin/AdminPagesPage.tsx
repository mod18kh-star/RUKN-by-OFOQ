import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  BarChart3,
  Check,
  CircleHelp,
  ExternalLink,
  Eye,
  EyeOff,
  FileText,
  Globe2,
  Image as ImageIcon,
  Info,
  MessageCircleMore,
  PackageCheck,
  Pencil,
  Plus,
  RefreshCw,
  RotateCcw,
  ShoppingBag,
  Star,
  Trash2,
  Truck,
  Upload,
  Users,
  X,
} from "lucide-react";

import {
  createContentPage,
  createNavigationItem,
  deleteContentPage,
  deleteNavigationItem,
  getContentPages,
  getNavigationItems,
  updateContentPage,
  updateNavigationItem,
  uploadContentPageAsset,
  type ContentPage,
  type ContentPageKind,
  type NavigationItem,
} from "./catalog/catalogContentApi";

import {
  getCurrentTenantId,
} from "./products/productsApi";

import {
  readAdminStore,
} from "./store-setup/storeSetupStorage";

type PageTemplateId =
  | "custom"
  | "about"
  | "shipping"
  | "returns"
  | "faq"
  | "contact"
  | "reviews"
  | "statistics";

type FormState = {
  title: string;
  slug: string;
  body: string;
  seoTitle: string;
  seoDescription: string;
  heroImageUrl: string;
  pageKind: ContentPageKind;
  showCustomerCount: boolean;
  showCompletedOrderCount: boolean;
  showUnitsSold: boolean;
  showAverageRating: boolean;
  showReviewCount: boolean;
  showCountryCount: boolean;
  publish: boolean;
  showInHeader: boolean;
  templateId: PageTemplateId;
};

type PageTemplate = {
  id: PageTemplateId;
  title: string;
  description: string;
  suggestedTitle: string;
  suggestedSlug: string;
  suggestedBody: string;
  pageKind: ContentPageKind;
  icon: typeof FileText;
};

const pageTemplates: PageTemplate[] = [
  {
    id: "about",
    title: "من نحن",
    description: "قصة المتجر، ما الذي يميّزكم ولماذا يثق العميل بكم.",
    suggestedTitle: "من نحن",
    suggestedSlug: "about-us",
    suggestedBody:
      "مرحبًا بك في متجرنا.\n\nبدأنا بهدف بسيط: أن نجعل تجربة اختيار وشراء المنتجات أوضح وأسهل، مع اهتمام حقيقي بالجودة وخدمة العميل.\n\nنختار ما نقدمه بعناية، ونحرص على أن تكون تفاصيل المنتجات والأسعار والسياسات واضحة قبل الشراء.\n\nرؤيتنا\nأن نبني تجربة تسوق موثوقة ومريحة، ونكون خيارًا يعود إليه العميل بثقة.",
    pageKind: "Standard",
    icon: Info,
  },
  {
    id: "shipping",
    title: "سياسة الشحن",
    description: "وضّح المدن، مدة التجهيز، وقت التوصيل ورسوم الشحن.",
    suggestedTitle: "سياسة الشحن",
    suggestedSlug: "shipping-policy",
    suggestedBody:
      "نجهّز الطلبات بعناية قبل تسليمها لشركة الشحن.\n\nمدة تجهيز الطلب\nيتم تجهيز الطلب عادة خلال المدة الموضحة للعميل عند الشراء.\n\nمدة التوصيل\nتختلف مدة التوصيل حسب المدينة وشركة الشحن المختارة.\n\nرسوم الشحن\nتظهر رسوم الشحن بشكل واضح قبل تأكيد الطلب.",
    pageKind: "Standard",
    icon: Truck,
  },
  {
    id: "returns",
    title: "الاستبدال والاسترجاع",
    description: "قالب واضح للشروط والمدة وحالة المنتج المقبولة.",
    suggestedTitle: "الاستبدال والاسترجاع",
    suggestedSlug: "returns",
    suggestedBody:
      "نريد أن تكون تجربة الشراء واضحة ومريحة.\n\nيمكن طلب الاستبدال أو الاسترجاع وفق الشروط والمدة الموضحة في هذه الصفحة.\n\nحالة المنتج\nيجب إعادة المنتج بحالته المناسبة ومع ملحقاته وتغليفه متى كان ذلك مطلوبًا.",
    pageKind: "Standard",
    icon: RotateCcw,
  },
  {
    id: "faq",
    title: "الأسئلة الشائعة",
    description: "إجابات مختصرة على أكثر الأسئلة التي تصل لخدمة العملاء.",
    suggestedTitle: "الأسئلة الشائعة",
    suggestedSlug: "faq",
    suggestedBody:
      "هل المنتجات أصلية؟\nنوضح حالة ومصدر كل منتج في تفاصيله.\n\nكيف أعرف حالة طلبي؟\nيمكن متابعة حالة الطلب من تفاصيل الطلب أو من خلال قنوات التواصل المتاحة.\n\nكيف أتواصل معكم؟\nاستخدم وسائل التواصل المنشورة في المتجر وسنرد عليك بأقرب وقت ممكن.",
    pageKind: "Standard",
    icon: CircleHelp,
  },
  {
    id: "contact",
    title: "تواصل معنا",
    description: "صفحة بسيطة لخدمة العملاء ومواعيد التواصل.",
    suggestedTitle: "تواصل معنا",
    suggestedSlug: "contact-us",
    suggestedBody:
      "يسعدنا تواصلك معنا.\n\nإذا كان استفسارك متعلقًا بطلب قائم، أرسل رقم الطلب مع رسالتك حتى نساعدك بشكل أسرع.\n\nخدمة العملاء\nأضف هنا رقم واتساب أو وسيلة التواصل الأساسية.\n\nساعات العمل\nأضف هنا أوقات خدمة العملاء وأيام العمل.",
    pageKind: "Standard",
    icon: MessageCircleMore,
  },
  {
    id: "reviews",
    title: "تقييمات العملاء",
    description: "يعرض التقييمات المنشورة الحقيقية من نظام التقييمات في المتجر.",
    suggestedTitle: "تجارب عملائنا",
    suggestedSlug: "reviews",
    suggestedBody:
      "آراء العملاء تساعدك على تكوين صورة أوضح عن تجربة الشراء والمنتجات.\n\nنعرض هنا التقييمات المنشورة والمعتمدة في المتجر بشكل مباشر.",
    pageKind: "Reviews",
    icon: Star,
  },
  {
    id: "statistics",
    title: "أرقام المتجر",
    description: "أرقام حقيقية من الطلبات والتقييمات، وأنت تحدد ما يظهر للزائر.",
    suggestedTitle: "متجرنا بالأرقام",
    suggestedSlug: "store-stats",
    suggestedBody:
      "نحب أن تكون تجربتنا واضحة، لذلك نشارك بعض الأرقام التي تعبّر عن نشاط المتجر وثقة عملائنا.",
    pageKind: "Statistics",
    icon: BarChart3,
  },
  {
    id: "custom",
    title: "صفحة مخصصة",
    description: "ابدأ من صفحة فارغة واكتب المحتوى بالطريقة التي تناسب متجرك.",
    suggestedTitle: "",
    suggestedSlug: "",
    suggestedBody: "",
    pageKind: "Standard",
    icon: FileText,
  },
];

const emptyForm: FormState = {
  title: "",
  slug: "",
  body: "",
  seoTitle: "",
  seoDescription: "",
  heroImageUrl: "",
  pageKind: "Standard",
  showCustomerCount: true,
  showCompletedOrderCount: true,
  showUnitsSold: true,
  showAverageRating: true,
  showReviewCount: true,
  showCountryCount: true,
  publish: true,
  showInHeader: true,
  templateId: "custom",
};

const statisticOptions: Array<{
  key: keyof Pick<
    FormState,
    | "showCustomerCount"
    | "showCompletedOrderCount"
    | "showUnitsSold"
    | "showAverageRating"
    | "showReviewCount"
    | "showCountryCount"
  >;
  label: string;
  description: string;
  icon: typeof Users;
}> = [
  { key: "showCustomerCount", label: "عدد العملاء", description: "عملاء لديهم طلبات مكتملة.", icon: Users },
  { key: "showCompletedOrderCount", label: "الطلبات المكتملة", description: "الطلبات التي تم تسليمها أو إكمالها.", icon: PackageCheck },
  { key: "showUnitsSold", label: "المنتجات المباعة", description: "إجمالي القطع في الطلبات المكتملة.", icon: ShoppingBag },
  { key: "showAverageRating", label: "متوسط التقييم", description: "متوسط التقييمات المنشورة.", icon: Star },
  { key: "showReviewCount", label: "عدد التقييمات", description: "عدد التقييمات المنشورة في المتجر.", icon: MessageCircleMore },
  { key: "showCountryCount", label: "عدد الدول", description: "الدول التي وصلت إليها الطلبات المكتملة.", icon: Globe2 },
];

function createSlug(value: string) {
  return value
    .normalize("NFKC")
    .trim()
    .toLowerCase()
    .replace(/[^\p{L}\p{N}]+/gu, "-")
    .replace(/^-+|-+$/g, "")
    .replace(/-{2,}/g, "-");
}

function pageHeaderNavigation(
  navigation: NavigationItem[],
  pageId: string,
) {
  return navigation.find(
    (item) =>
      item.location === "Header" &&
      item.type === "Page" &&
      item.targetId === pageId,
  );
}

function pageKindLabel(kind: ContentPageKind) {
  if (kind === "Reviews") return "تقييمات حية";
  if (kind === "Statistics") return "إحصائيات حية";
  return "صفحة محتوى";
}

export function AdminPagesPage() {
  const tenantId = getCurrentTenantId();
  const store = readAdminStore();
  const [pages, setPages] = useState<ContentPage[]>([]);
  const [navigation, setNavigation] = useState<NavigationItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uploadingImage, setUploadingImage] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<ContentPage | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [slugTouched, setSlugTouched] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const storefrontBaseUrl = useMemo(
    () => store?.slug ? `/store/${encodeURIComponent(store.slug)}` : null,
    [store?.slug],
  );

  const load = useCallback(async () => {
    if (!tenantId) {
      setError("ما لقينا متجر مرتبط بالحساب الحالي.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const [contentPages, navigationItems] = await Promise.all([
        getContentPages(tenantId),
        getNavigationItems(tenantId),
      ]);
      setPages(contentPages);
      setNavigation(navigationItems);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "تعذر تحميل الصفحات.");
    } finally {
      setLoading(false);
    }
  }, [tenantId]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setSlugTouched(false);
    setError(null);
    setDialogOpen(true);
  }

  function applyTemplate(template: PageTemplate) {
    setSlugTouched(template.id !== "custom");
    setForm((current) => ({
      ...current,
      templateId: template.id,
      title: template.suggestedTitle,
      slug: template.suggestedSlug,
      body: template.suggestedBody,
      seoTitle: template.suggestedTitle,
      seoDescription: template.description,
      pageKind: template.pageKind,
    }));
  }

  function openEdit(page: ContentPage) {
    setEditing(page);
    setForm({
      title: page.title,
      slug: page.slug,
      body: page.body,
      seoTitle: page.seoTitle ?? "",
      seoDescription: page.seoDescription ?? "",
      heroImageUrl: page.heroImageUrl ?? "",
      pageKind: page.pageKind,
      showCustomerCount: page.showCustomerCount,
      showCompletedOrderCount: page.showCompletedOrderCount,
      showUnitsSold: page.showUnitsSold,
      showAverageRating: page.showAverageRating,
      showReviewCount: page.showReviewCount,
      showCountryCount: page.showCountryCount,
      publish: page.isPublished,
      showInHeader: Boolean(pageHeaderNavigation(navigation, page.id)?.isVisible),
      templateId: page.pageKind === "Reviews" ? "reviews" : page.pageKind === "Statistics" ? "statistics" : "custom",
    });
    setSlugTouched(true);
    setError(null);
    setDialogOpen(true);
  }

  function closeDialog() {
    if (saving || uploadingImage) return;
    setDialogOpen(false);
    setEditing(null);
    setForm(emptyForm);
    setError(null);
  }

  async function handleImageUpload(file: File | null) {
    if (!tenantId || !file || uploadingImage) return;
    setUploadingImage(true);
    setError(null);

    try {
      const url = await uploadContentPageAsset(tenantId, file);
      setForm((current) => ({ ...current, heroImageUrl: url }));
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "تعذر رفع الصورة.");
    } finally {
      setUploadingImage(false);
    }
  }

  async function syncHeaderNavigation(
    page: ContentPage,
    showInHeader: boolean,
  ) {
    if (!tenantId) return;
    const existing = pageHeaderNavigation(navigation, page.id);

    if (showInHeader) {
      const input = {
        location: "Header" as const,
        type: "Page" as const,
        label: page.title,
        targetId: page.id,
        externalUrl: null,
        parentItemId: null,
        position: null,
        isVisible: true,
      };

      if (existing) await updateNavigationItem(tenantId, existing.id, input);
      else await createNavigationItem(tenantId, input);
      return;
    }

    if (existing) await deleteNavigationItem(tenantId, existing.id);
  }

  async function save() {
    if (!tenantId || saving) return;

    const title = form.title.trim();
    const slug = form.slug.trim();
    if (!title || !slug) {
      setError("عنوان الصفحة والرابط مطلوبان.");
      return;
    }

    if (form.pageKind === "Statistics" &&
        !statisticOptions.some((option) => Boolean(form[option.key]))) {
      setError("اختر رقمًا واحدًا على الأقل ليظهر في صفحة الإحصائيات.");
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const payload = {
        title,
        slug,
        body: form.body.trim(),
        seoTitle: form.seoTitle.trim() || null,
        seoDescription: form.seoDescription.trim() || null,
        publish: form.publish,
        pageKind: form.pageKind,
        heroImageUrl: form.heroImageUrl.trim() || null,
        showCustomerCount: form.showCustomerCount,
        showCompletedOrderCount: form.showCompletedOrderCount,
        showUnitsSold: form.showUnitsSold,
        showAverageRating: form.showAverageRating,
        showReviewCount: form.showReviewCount,
        showCountryCount: form.showCountryCount,
      };

      const savedPage = editing
        ? await updateContentPage(tenantId, editing.id, payload)
        : await createContentPage(tenantId, payload);

      await syncHeaderNavigation(savedPage, form.publish && form.showInHeader);
      setDialogOpen(false);
      setEditing(null);
      setForm(emptyForm);
      await load();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "تعذر حفظ الصفحة.");
    } finally {
      setSaving(false);
    }
  }

  async function remove(page: ContentPage) {
    if (!tenantId || saving) return;
    if (!window.confirm(`حذف صفحة «${page.title}»؟`)) return;

    setSaving(true);
    setError(null);
    try {
      const links = navigation.filter((item) => item.type === "Page" && item.targetId === page.id);
      for (const item of links) await deleteNavigationItem(tenantId, item.id);
      await deleteContentPage(tenantId, page.id);
      await load();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "تعذر حذف الصفحة.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div dir="rtl" className="mx-auto max-w-[1380px]">
      <div className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <p className="text-[11px] font-semibold text-[#9d723d]">محتوى المتجر</p>
          <h1 className="mt-2 text-[31px] font-semibold tracking-[-0.04em]">الصفحات</h1>
          <p className="mt-2 max-w-[790px] text-[12px] leading-7 text-black/45">
            أنشئ صفحات عادية أو صفحات ذكية مرتبطة ببيانات المتجر، وأضف لها صورة وهوية تناسب الثيم.
          </p>
        </div>

        <button type="button" onClick={openCreate} className="inline-flex h-12 items-center gap-2 rounded-[10px] bg-[#080b14] px-5 text-[12px] font-semibold text-white transition hover:-translate-y-0.5">
          <Plus size={17} />
          إضافة صفحة
        </button>
      </div>

      <div className="mt-6 grid gap-3 md:grid-cols-3">
        <div className="rounded-[16px] border border-black/[0.07] bg-white p-5"><p className="text-[10px] text-black/38">إجمالي الصفحات</p><p dir="ltr" className="mt-2 text-right text-[24px] font-semibold tabular-nums">{pages.length}</p></div>
        <div className="rounded-[16px] border border-black/[0.07] bg-white p-5"><p className="text-[10px] text-black/38">الصفحات المنشورة</p><p dir="ltr" className="mt-2 text-right text-[24px] font-semibold tabular-nums">{pages.filter((page) => page.isPublished).length}</p></div>
        <div className="rounded-[16px] border border-black/[0.07] bg-white p-5"><p className="text-[10px] text-black/38">الصفحات الذكية</p><p dir="ltr" className="mt-2 text-right text-[24px] font-semibold tabular-nums">{pages.filter((page) => page.pageKind !== "Standard").length}</p></div>
      </div>

      {error && !dialogOpen ? <div className="mt-6 rounded-[12px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">{error}</div> : null}

      {loading ? (
        <div className="mt-7 flex min-h-[300px] items-center justify-center rounded-[18px] border border-black/[0.07] bg-white text-[11px] text-black/40"><RefreshCw size={16} className="ml-2 animate-spin" />جاري تحميل الصفحات...</div>
      ) : pages.length === 0 ? (
        <div className="mt-7 flex min-h-[330px] items-center justify-center rounded-[18px] border border-black/[0.07] bg-white p-6 text-center">
          <div><div className="mx-auto flex size-14 items-center justify-center rounded-full bg-[#f0ece3]"><FileText size={22} className="text-black/42" /></div><h2 className="mt-4 text-[18px] font-semibold">ابدأ بأول صفحة للمتجر</h2><p className="mt-2 text-[10px] leading-6 text-black/42">اختر قالب محتوى، تقييمات العملاء أو أرقام المتجر.</p></div>
        </div>
      ) : (
        <div className="mt-7 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {pages.map((page) => {
            const headerLink = pageHeaderNavigation(navigation, page.id);
            const previewHref = storefrontBaseUrl ? `${storefrontBaseUrl}/pages/${encodeURIComponent(page.slug)}` : null;
            return (
              <article key={page.id} className="group overflow-hidden rounded-[18px] border border-black/[0.07] bg-white transition hover:-translate-y-0.5 hover:border-black/[0.13] hover:shadow-[0_18px_45px_rgba(25,30,25,0.06)]">
                {page.heroImageUrl ? <img src={page.heroImageUrl} alt="" className="h-36 w-full object-cover" /> : null}
                <div className="p-5">
                  <div className="flex items-start justify-between gap-4">
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <h2 className="truncate text-[15px] font-semibold">{page.title}</h2>
                        <span className={`shrink-0 rounded-full px-2 py-1 text-[8px] font-semibold ${page.isPublished ? "bg-emerald-50 text-emerald-700" : "bg-[#f3eadc] text-[#8c642f]"}`}>{page.isPublished ? "منشورة" : "مسودة"}</span>
                        <span className="rounded-full bg-[#f5f1e8] px-2 py-1 text-[8px] font-semibold text-[#76552d]">{pageKindLabel(page.pageKind)}</span>
                        {headerLink?.isVisible ? <span className="rounded-full bg-sky-50 px-2 py-1 text-[8px] font-semibold text-sky-700">في القائمة</span> : null}
                      </div>
                      <p dir="ltr" className="mt-2 truncate text-left text-[9px] text-black/34">/pages/{page.slug}</p>
                    </div>
                    <div className="flex shrink-0 gap-1">
                      {previewHref && page.isPublished ? <a href={previewHref} target="_blank" rel="noreferrer" className="flex size-9 items-center justify-center rounded-[9px] border border-black/[0.08] transition hover:bg-black/[0.035]" aria-label={`فتح ${page.title}`}><ExternalLink size={14} /></a> : null}
                      <button type="button" onClick={() => openEdit(page)} className="flex size-9 items-center justify-center rounded-[9px] border border-black/[0.08] transition hover:bg-black/[0.035]" aria-label={`تعديل ${page.title}`}><Pencil size={14} /></button>
                      <button type="button" onClick={() => void remove(page)} className="flex size-9 items-center justify-center rounded-[9px] border border-red-100 text-red-700 transition hover:bg-red-50" aria-label={`حذف ${page.title}`}><Trash2 size={14} /></button>
                    </div>
                  </div>
                  <p className="mt-4 line-clamp-3 min-h-[63px] whitespace-pre-line text-[11px] leading-7 text-black/48">{page.body || (page.pageKind === "Reviews" ? "يعرض التقييمات المنشورة مباشرة من المتجر." : "يعرض أرقام المتجر المختارة مباشرة من البيانات الحقيقية.")}</p>
                  <div className="mt-5 flex items-center justify-between border-t border-black/[0.055] pt-4 text-[9px] text-black/35"><span>{page.isPublished ? "متاحة للزوار" : "غير ظاهرة للزوار"}</span>{previewHref && page.isPublished ? <a href={previewHref} target="_blank" rel="noreferrer" className="font-semibold text-black/60 hover:text-black">معاينة الصفحة</a> : null}</div>
                </div>
              </article>
            );
          })}
        </div>
      )}

      {dialogOpen ? (
        <div className="fixed inset-0 z-[100] flex items-end justify-center bg-black/40 p-0 backdrop-blur-[3px] md:items-center md:p-5">
          <div className="max-h-[95vh] w-full overflow-y-auto rounded-t-[24px] bg-[#f7f5ef] shadow-2xl md:max-w-[1000px] md:rounded-[24px]">
            <div className="sticky top-0 z-10 flex items-center justify-between border-b border-black/[0.07] bg-[#f7f5ef]/95 px-6 py-5 backdrop-blur-xl">
              <div><h2 className="text-[21px] font-semibold">{editing ? "تعديل الصفحة" : "إنشاء صفحة"}</h2><p className="mt-1 text-[10px] text-black/42">اختر قالبًا مرتبًا ثم عدّل النص والصورة وما تريد عرضه.</p></div>
              <button type="button" onClick={closeDialog} className="flex size-9 items-center justify-center rounded-full border border-black/[0.08] bg-white"><X size={16} /></button>
            </div>

            <div className="space-y-6 p-6 md:p-8">
              {!editing ? (
                <section>
                  <div className="mb-3"><h3 className="text-[13px] font-semibold">اختر نقطة بداية</h3><p className="mt-1 text-[9px] text-black/38">التقييمات والإحصائيات صفحات حية مرتبطة بالبيانات الفعلية، وبقية القوالب محتوى حر.</p></div>
                  <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                    {pageTemplates.map((template) => {
                      const Icon = template.icon;
                      const active = form.templateId === template.id;
                      return (
                        <button key={template.id} type="button" onClick={() => applyTemplate(template)} className={`rounded-[15px] border p-4 text-right transition ${active ? "border-[#9d723d]/60 bg-[#fffaf1] shadow-[0_10px_28px_rgba(80,60,32,0.06)]" : "border-black/[0.07] bg-white hover:border-black/[0.14]"}`}>
                          <div className="flex size-10 items-center justify-center rounded-[11px] bg-[#f1ede4] text-[#76552d]"><Icon size={18} /></div>
                          <h4 className="mt-3 text-[12px] font-semibold">{template.title}</h4>
                          <p className="mt-1 text-[9px] leading-5 text-black/40">{template.description}</p>
                        </button>
                      );
                    })}
                  </div>
                </section>
              ) : null}

              <div className="grid gap-5 md:grid-cols-2">
                <label className="block"><span className="mb-2 block text-[10px] font-semibold">عنوان الصفحة</span><input value={form.title} onChange={(event) => { const value = event.target.value; setForm((current) => ({ ...current, title: value, slug: slugTouched ? current.slug : createSlug(value) })); }} className="h-12 w-full rounded-[11px] border border-black/[0.1] bg-white px-4 text-[13px] outline-none focus:border-[#a77a43]/70" placeholder="من نحن" /></label>
                <label className="block"><span className="mb-2 block text-[10px] font-semibold">رابط الصفحة</span><input dir="ltr" value={form.slug} onChange={(event) => { setSlugTouched(true); setForm((current) => ({ ...current, slug: createSlug(event.target.value) })); }} className="h-12 w-full rounded-[11px] border border-black/[0.1] bg-white px-4 text-left text-[12px] outline-none focus:border-[#a77a43]/70" placeholder="about-us" /></label>
              </div>

              <section className="rounded-[16px] border border-black/[0.07] bg-white p-5">
                <div className="flex flex-wrap items-center justify-between gap-4">
                  <div><h3 className="text-[12px] font-semibold">صورة الصفحة</h3><p className="mt-1 text-[9px] text-black/38">اختيارية، وتظهر كبانر كبير داخل الصفحة. يمكنك رفعها من الجهاز أو لصق رابط.</p></div>
                  <label className="inline-flex h-10 cursor-pointer items-center gap-2 rounded-[9px] border border-black/[0.09] px-4 text-[10px] font-semibold transition hover:bg-black/[0.025]">
                    {uploadingImage ? <RefreshCw size={14} className="animate-spin" /> : <Upload size={14} />}
                    {uploadingImage ? "جاري الرفع..." : "رفع صورة"}
                    <input type="file" accept="image/png,image/jpeg,image/webp" className="hidden" disabled={uploadingImage} onChange={(event) => void handleImageUpload(event.target.files?.[0] ?? null)} />
                  </label>
                </div>
                <div className="mt-4 grid gap-4 md:grid-cols-[1fr_220px]">
                  <input dir="ltr" value={form.heroImageUrl} onChange={(event) => setForm((current) => ({ ...current, heroImageUrl: event.target.value }))} className="h-12 w-full rounded-[11px] border border-black/[0.1] bg-[#faf9f5] px-4 text-left text-[10px] outline-none focus:border-[#a77a43]/70" placeholder="أو رابط صورة مباشر" />
                  <div className="flex h-28 items-center justify-center overflow-hidden rounded-[12px] border border-black/[0.07] bg-[#f4f2ec]">
                    {form.heroImageUrl ? <img src={form.heroImageUrl} alt="معاينة" className="h-full w-full object-cover" /> : <ImageIcon size={24} className="text-black/20" />}
                  </div>
                </div>
              </section>

              <label className="block">
                <div className="mb-2 flex items-center justify-between gap-4"><span className="text-[10px] font-semibold">النص التعريفي للصفحة <span className="font-normal text-black/35">اختياري</span></span><span className="text-[9px] text-black/30">استخدم سطرًا فارغًا للفصل بين الفقرات</span></div>
                <textarea value={form.body} onChange={(event) => setForm((current) => ({ ...current, body: event.target.value }))} className="min-h-[220px] w-full rounded-[14px] border border-black/[0.1] bg-white p-5 text-[13px] leading-8 outline-none focus:border-[#a77a43]/70" placeholder={form.pageKind === "Reviews" ? "مقدمة قصيرة قبل التقييمات..." : form.pageKind === "Statistics" ? "مقدمة قصيرة قبل الأرقام..." : "اكتب محتوى الصفحة هنا..."} />
              </label>

              {form.pageKind === "Statistics" ? (
                <section className="rounded-[16px] border border-[#d8ccb8] bg-[#fffaf1] p-5">
                  <div><h3 className="text-[13px] font-semibold">ما الذي تريد إظهاره؟</h3><p className="mt-1 text-[9px] leading-5 text-black/42">الأرقام تأتي من بيانات المتجر الحقيقية، ولن يظهر أي عنصر تقوم بإيقافه.</p></div>
                  <div className="mt-4 grid gap-3 md:grid-cols-2 lg:grid-cols-3">
                    {statisticOptions.map((option) => {
                      const Icon = option.icon;
                      const active = Boolean(form[option.key]);
                      return (
                        <button key={option.key} type="button" onClick={() => setForm((current) => ({ ...current, [option.key]: !current[option.key] }))} className={`rounded-[13px] border p-4 text-right transition ${active ? "border-[#9d723d]/45 bg-white shadow-[0_8px_24px_rgba(80,60,32,0.05)]" : "border-black/[0.07] bg-white/55 opacity-65"}`}>
                          <div className="flex items-start justify-between gap-3"><div className="flex size-9 items-center justify-center rounded-[10px] bg-[#f1ede4] text-[#76552d]"><Icon size={16} /></div><span className={`rounded-full px-2 py-1 text-[8px] font-semibold ${active ? "bg-emerald-50 text-emerald-700" : "bg-black/[0.04] text-black/40"}`}>{active ? "ظاهر" : "مخفي"}</span></div>
                          <p className="mt-3 text-[11px] font-semibold">{option.label}</p><p className="mt-1 text-[9px] leading-5 text-black/38">{option.description}</p>
                        </button>
                      );
                    })}
                  </div>
                </section>
              ) : null}

              {form.pageKind === "Reviews" ? (
                <div className="rounded-[14px] border border-sky-100 bg-sky-50/60 p-4 text-[10px] leading-6 text-sky-900/70">
                  صفحة التقييمات تعرض التقييمات <strong>المنشورة فقط</strong> من نظام التقييمات الموجود عندك، مع اسم المنتج والتقييم ورد المتجر إن وُجد. لا يتم إنشاء تقييمات وهمية.
                </div>
              ) : null}

              <div className="grid gap-5 md:grid-cols-2">
                <label className="block"><span className="mb-2 block text-[10px] font-semibold">عنوان SEO <span className="font-normal text-black/35">اختياري</span></span><input value={form.seoTitle} onChange={(event) => setForm((current) => ({ ...current, seoTitle: event.target.value }))} className="h-12 w-full rounded-[11px] border border-black/[0.1] bg-white px-4 text-[11px] outline-none focus:border-[#a77a43]/70" /></label>
                <label className="block"><span className="mb-2 block text-[10px] font-semibold">وصف SEO <span className="font-normal text-black/35">اختياري</span></span><input value={form.seoDescription} onChange={(event) => setForm((current) => ({ ...current, seoDescription: event.target.value }))} className="h-12 w-full rounded-[11px] border border-black/[0.1] bg-white px-4 text-[11px] outline-none focus:border-[#a77a43]/70" /></label>
              </div>

              <div className="grid gap-3 md:grid-cols-2">
                <button type="button" onClick={() => setForm((current) => ({ ...current, publish: !current.publish, showInHeader: !current.publish ? current.showInHeader : false }))} className="flex w-full items-center justify-between rounded-[13px] border border-black/[0.08] bg-white px-4 py-4 text-[11px]"><span className="flex items-center gap-2">{form.publish ? <Eye size={16} /> : <EyeOff size={16} />}نشر الصفحة في المتجر</span><span className={`rounded-full px-2 py-1 text-[8px] ${form.publish ? "bg-emerald-50 text-emerald-700" : "bg-[#f3eadc] text-[#8c642f]"}`}>{form.publish ? "منشورة" : "مسودة"}</span></button>
                <button type="button" disabled={!form.publish} onClick={() => setForm((current) => ({ ...current, showInHeader: !current.showInHeader }))} className="flex w-full items-center justify-between rounded-[13px] border border-black/[0.08] bg-white px-4 py-4 text-[11px] disabled:cursor-not-allowed disabled:opacity-45"><span className="flex items-center gap-2"><PackageCheck size={16} />إظهارها في القائمة الرئيسية</span><span className={`rounded-full px-2 py-1 text-[8px] ${form.showInHeader && form.publish ? "bg-sky-50 text-sky-700" : "bg-black/[0.04] text-black/40"}`}>{form.showInHeader && form.publish ? "ظاهرة" : "مخفية"}</span></button>
              </div>

              {error ? <div className="rounded-[11px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">{error}</div> : null}

              <div className="flex items-center justify-end gap-3 border-t border-black/[0.07] pt-5">
                <button type="button" onClick={closeDialog} className="h-11 px-4 text-[10px] text-black/45">إلغاء</button>
                <button type="button" disabled={saving || uploadingImage} onClick={() => void save()} className="inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#080b14] px-5 text-[11px] font-semibold text-white disabled:opacity-50">{saving ? <RefreshCw size={14} className="animate-spin" /> : <Check size={14} />}{editing ? "حفظ التعديل" : "إنشاء الصفحة"}</button>
              </div>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
