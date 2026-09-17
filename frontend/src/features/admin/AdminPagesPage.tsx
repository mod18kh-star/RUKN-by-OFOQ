import {
  useCallback,
  useEffect,
  useState,
} from "react";

import {
  Check,
  Eye,
  EyeOff,
  FileText,
  Pencil,
  Plus,
  RefreshCw,
  Trash2,
  X,
} from "lucide-react";

import {
  createContentPage,
  deleteContentPage,
  getContentPages,
  updateContentPage,
  type ContentPage,
} from "./catalog/catalogContentApi";

import {
  getCurrentTenantId,
} from "./products/productsApi";

type FormState = {
  title: string;
  slug: string;
  body: string;
  seoTitle: string;
  seoDescription: string;
  publish: boolean;
};

const emptyForm: FormState = {
  title: "",
  slug: "",
  body: "",
  seoTitle: "",
  seoDescription: "",
  publish: true,
};

function createSlug(value: string) {
  return value
    .normalize("NFKC")
    .trim()
    .toLowerCase()
    .replace(/[^\p{L}\p{N}]+/gu, "-")
    .replace(/^-+|-+$/g, "")
    .replace(/-{2,}/g, "-");
}

export function AdminPagesPage() {
  const tenantId = getCurrentTenantId();
  const [pages, setPages] = useState<ContentPage[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<ContentPage | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [slugTouched, setSlugTouched] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!tenantId) {
      setError("ما لقينا متجر مرتبط بالحساب الحالي.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      setPages(await getContentPages(tenantId));
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر تحميل الصفحات.",
      );
    } finally {
      setLoading(false);
    }
  }, [tenantId]);

  useEffect(() => {
    const timer =
      window.setTimeout(
        () => {
          void load();
        },
        0,
      );

    return () => {
      window.clearTimeout(
        timer,
      );
    };
  }, [load]);

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setSlugTouched(false);
    setError(null);
    setDialogOpen(true);
  }

  function openEdit(page: ContentPage) {
    setEditing(page);
    setForm({
      title: page.title,
      slug: page.slug,
      body: page.body,
      seoTitle: page.seoTitle ?? "",
      seoDescription: page.seoDescription ?? "",
      publish: page.isPublished,
    });
    setSlugTouched(true);
    setError(null);
    setDialogOpen(true);
  }

  function closeDialog() {
    if (saving) return;
    setDialogOpen(false);
    setEditing(null);
    setForm(emptyForm);
    setError(null);
  }

  async function save() {
    if (!tenantId || saving) return;

    const title = form.title.trim();
    const slug = form.slug.trim();
    const body = form.body.trim();

    if (!title || !slug || !body) {
      setError("العنوان والرابط ومحتوى الصفحة مطلوبة.");
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const payload = {
        title,
        slug,
        body,
        seoTitle: form.seoTitle.trim() || null,
        seoDescription: form.seoDescription.trim() || null,
        publish: form.publish,
      };

      if (editing) {
        await updateContentPage(
          tenantId,
          editing.id,
          payload,
        );
      } else {
        await createContentPage(
          tenantId,
          payload,
        );
      }

      closeDialog();
      await load();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر حفظ الصفحة.",
      );
    } finally {
      setSaving(false);
    }
  }

  async function remove(page: ContentPage) {
    if (!tenantId || saving) return;

    const confirmed = window.confirm(
      `حذف صفحة «${page.title}»؟`,
    );

    if (!confirmed) return;

    setSaving(true);
    setError(null);

    try {
      await deleteContentPage(tenantId, page.id);
      await load();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر حذف الصفحة.",
      );
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
          <p className="mt-2 max-w-[700px] text-[12px] leading-7 text-black/45">
            أنشئ صفحات مثل من نحن، شركاؤنا، سياسة الشحن أو أي صفحة تعريفية. الصفحة المنشورة
            تصبح متاحة مباشرة داخل المتجر وتظهر في قائمة الصفحات.
          </p>
        </div>

        <button
          type="button"
          onClick={openCreate}
          className="inline-flex h-12 items-center gap-2 rounded-[10px] bg-[#080b14] px-5 text-[12px] font-semibold text-white"
        >
          <Plus size={17} />
          إضافة صفحة
        </button>
      </div>

      {error && !dialogOpen ? (
        <div className="mt-6 rounded-[12px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">
          {error}
        </div>
      ) : null}

      {loading ? (
        <div className="mt-7 flex min-h-[300px] items-center justify-center rounded-[18px] border border-black/[0.07] bg-white text-[11px] text-black/40">
          <RefreshCw size={16} className="ml-2 animate-spin" />
          جاري تحميل الصفحات...
        </div>
      ) : pages.length === 0 ? (
        <div className="mt-7 flex min-h-[330px] items-center justify-center rounded-[18px] border border-black/[0.07] bg-white p-6 text-center">
          <div>
            <div className="mx-auto flex size-14 items-center justify-center rounded-full bg-[#f0ece3]">
              <FileText size={22} className="text-black/42" />
            </div>
            <h2 className="mt-4 text-[18px] font-semibold">ما عندك صفحات إضافية بعد</h2>
            <p className="mt-2 text-[10px] leading-6 text-black/42">ابدأ بصفحة «من نحن» أو «سياسة الشحن».</p>
          </div>
        </div>
      ) : (
        <div className="mt-7 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {pages.map((page) => (
            <article key={page.id} className="rounded-[16px] border border-black/[0.07] bg-white p-5">
              <div className="flex items-start justify-between gap-4">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <h2 className="truncate text-[14px] font-semibold">{page.title}</h2>
                    <span className={`shrink-0 rounded-full px-2 py-1 text-[8px] font-semibold ${page.isPublished ? "bg-emerald-50 text-emerald-700" : "bg-[#f3eadc] text-[#8c642f]"}`}>
                      {page.isPublished ? "منشورة" : "مسودة"}
                    </span>
                  </div>
                  <p dir="ltr" className="mt-2 truncate text-left text-[9px] text-black/34">/pages/{page.slug}</p>
                </div>

                <div className="flex shrink-0 gap-1">
                  <button type="button" onClick={() => openEdit(page)} className="flex size-9 items-center justify-center rounded-[9px] border border-black/[0.08]" aria-label={`تعديل ${page.title}`}>
                    <Pencil size={14} />
                  </button>
                  <button type="button" onClick={() => void remove(page)} className="flex size-9 items-center justify-center rounded-[9px] border border-red-100 text-red-700" aria-label={`حذف ${page.title}`}>
                    <Trash2 size={14} />
                  </button>
                </div>
              </div>

              <p className="mt-4 line-clamp-3 min-h-[60px] whitespace-pre-line text-[10px] leading-6 text-black/46">
                {page.body}
              </p>
            </article>
          ))}
        </div>
      )}

      {dialogOpen ? (
        <div className="fixed inset-0 z-[100] flex items-end justify-center bg-black/35 p-0 backdrop-blur-[2px] md:items-center md:p-5">
          <div className="max-h-[94vh] w-full overflow-y-auto rounded-t-[22px] bg-[#f7f5ef] shadow-2xl md:max-w-[820px] md:rounded-[22px]">
            <div className="sticky top-0 z-10 flex items-center justify-between border-b border-black/[0.07] bg-[#f7f5ef]/95 px-6 py-5 backdrop-blur-xl">
              <div>
                <h2 className="text-[20px] font-semibold">{editing ? "تعديل الصفحة" : "صفحة جديدة"}</h2>
                <p className="mt-1 text-[10px] text-black/42">المحتوى المنشور يظهر للزوار داخل المتجر.</p>
              </div>
              <button type="button" onClick={closeDialog} className="flex size-9 items-center justify-center rounded-full border border-black/[0.08] bg-white">
                <X size={16} />
              </button>
            </div>

            <div className="space-y-5 p-6 md:p-8">
              <div className="grid gap-5 md:grid-cols-2">
                <label className="block">
                  <span className="mb-2 block text-[10px] font-semibold">عنوان الصفحة</span>
                  <input
                    value={form.title}
                    onChange={(event) => {
                      const value = event.target.value;
                      setForm((current) => ({
                        ...current,
                        title: value,
                        slug: slugTouched ? current.slug : createSlug(value),
                      }));
                    }}
                    className="h-12 w-full rounded-[10px] border border-black/[0.1] bg-white px-4 text-[12px] outline-none focus:border-[#a77a43]/70"
                    placeholder="من نحن"
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-[10px] font-semibold">رابط الصفحة</span>
                  <input
                    dir="ltr"
                    value={form.slug}
                    onChange={(event) => {
                      setSlugTouched(true);
                      setForm((current) => ({ ...current, slug: createSlug(event.target.value) }));
                    }}
                    className="h-12 w-full rounded-[10px] border border-black/[0.1] bg-white px-4 text-left text-[12px] outline-none focus:border-[#a77a43]/70"
                    placeholder="about-us"
                  />
                </label>
              </div>

              <label className="block">
                <span className="mb-2 block text-[10px] font-semibold">محتوى الصفحة</span>
                <textarea
                  value={form.body}
                  onChange={(event) => setForm((current) => ({ ...current, body: event.target.value }))}
                  className="min-h-[240px] w-full rounded-[12px] border border-black/[0.1] bg-white p-4 text-[12px] leading-7 outline-none focus:border-[#a77a43]/70"
                  placeholder="اكتب محتوى الصفحة هنا..."
                />
              </label>

              <div className="grid gap-5 md:grid-cols-2">
                <label className="block">
                  <span className="mb-2 block text-[10px] font-semibold">عنوان SEO <span className="font-normal text-black/35">اختياري</span></span>
                  <input value={form.seoTitle} onChange={(event) => setForm((current) => ({ ...current, seoTitle: event.target.value }))} className="h-12 w-full rounded-[10px] border border-black/[0.1] bg-white px-4 text-[11px] outline-none" />
                </label>
                <label className="block">
                  <span className="mb-2 block text-[10px] font-semibold">وصف SEO <span className="font-normal text-black/35">اختياري</span></span>
                  <input value={form.seoDescription} onChange={(event) => setForm((current) => ({ ...current, seoDescription: event.target.value }))} className="h-12 w-full rounded-[10px] border border-black/[0.1] bg-white px-4 text-[11px] outline-none" />
                </label>
              </div>

              <button
                type="button"
                onClick={() => setForm((current) => ({ ...current, publish: !current.publish }))}
                className="flex w-full items-center justify-between rounded-[12px] border border-black/[0.08] bg-white px-4 py-4 text-[11px]"
              >
                <span className="flex items-center gap-2">
                  {form.publish ? <Eye size={16} /> : <EyeOff size={16} />}
                  نشر الصفحة في المتجر
                </span>
                <span className={`rounded-full px-2 py-1 text-[8px] ${form.publish ? "bg-emerald-50 text-emerald-700" : "bg-[#f3eadc] text-[#8c642f]"}`}>
                  {form.publish ? "منشورة" : "مسودة"}
                </span>
              </button>

              {error ? <div className="rounded-[11px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">{error}</div> : null}

              <div className="flex items-center justify-end gap-3 border-t border-black/[0.07] pt-5">
                <button type="button" onClick={closeDialog} className="h-11 px-4 text-[10px] text-black/45">إلغاء</button>
                <button type="button" disabled={saving} onClick={() => void save()} className="inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#080b14] px-5 text-[11px] font-semibold text-white disabled:opacity-50">
                  {saving ? <RefreshCw size={14} className="animate-spin" /> : <Check size={14} />}
                  {editing ? "حفظ التعديل" : "إنشاء الصفحة"}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
