import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  Check,
  ChevronLeft,
  Eye,
  EyeOff,
  FolderTree,
  Home,
  Image as ImageIcon,
  Pencil,
  Plus,
  RefreshCw,
  X,
} from "lucide-react";

import {
  createCategory,
  getCategories,
  moveCategory,
  updateCategory,
  type AdminCategory,
} from "./catalog/catalogContentApi";

import {
  getCurrentTenantId,
} from "./products/productsApi";

type CategoryForm = {
  name: string;
  slug: string;
  parentCategoryId: string;
  imageUrl: string;
  isVisible: boolean;
};

const emptyForm: CategoryForm = {
  name: "",
  slug: "",
  parentCategoryId: "",
  imageUrl: "",
  isVisible: true,
};

function createSlug(value: string) {
  return value
    .normalize("NFKC")
    .trim()
    .toLowerCase()
    .replace(/[^^\p{L}\p{N}]+/gu, "-")
    .replace(/^-+|-+$/g, "")
    .replace(/-{2,}/g, "-");
}

function flattenCategories(
  categories: AdminCategory[],
) {
  const byParent =
    new Map<string | null, AdminCategory[]>();

  for (const category of categories) {
    const key = category.parentCategoryId;
    const group = byParent.get(key) ?? [];
    group.push(category);
    byParent.set(key, group);
  }

  for (const group of byParent.values()) {
    group.sort(
      (a, b) =>
        a.sortOrder - b.sortOrder ||
        a.name.localeCompare(b.name, "ar"),
    );
  }

  const result: Array<{
    category: AdminCategory;
    depth: number;
  }> = [];

  function walk(
    parentId: string | null,
    depth: number,
  ) {
    for (const category of byParent.get(parentId) ?? []) {
      result.push({ category, depth });
      walk(category.categoryId, depth + 1);
    }
  }

  walk(null, 0);

  return result;
}

function sortCategories(categories: AdminCategory[]) {
  return [...categories].sort(
    (a, b) =>
      a.sortOrder - b.sortOrder ||
      a.name.localeCompare(b.name, "ar"),
  );
}

function categoryBreadcrumbs(
  categories: AdminCategory[],
  categoryId: string | null,
) {
  const result: AdminCategory[] = [];
  let currentId = categoryId;
  let guard = 0;

  while (currentId && guard < 32) {
    const category = categories.find(
      (item) => item.categoryId === currentId,
    );

    if (!category) break;

    result.unshift(category);
    currentId = category.parentCategoryId;
    guard += 1;
  }

  return result;
}

export function AdminCategoriesPage() {
  const tenantId = getCurrentTenantId();
  const [categories, setCategories] = useState<AdminCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<AdminCategory | null>(null);
  const [form, setForm] = useState<CategoryForm>(emptyForm);
  const [slugTouched, setSlugTouched] = useState(false);
  const [activeParentId, setActiveParentId] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!tenantId) {
      setError("ما لقينا متجر مرتبط بالحساب الحالي.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      setCategories(await getCategories(tenantId));
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر تحميل الأقسام.",
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

  const flattened = useMemo(
    () => flattenCategories(categories),
    [categories],
  );

  const currentCategories = useMemo(
    () =>
      sortCategories(
        categories.filter(
          (category) =>
            category.parentCategoryId === activeParentId,
        ),
      ),
    [activeParentId, categories],
  );

  const breadcrumbs = useMemo(
    () => categoryBreadcrumbs(categories, activeParentId),
    [activeParentId, categories],
  );

  const childCountByCategory = useMemo(() => {
    const counts = new Map<string, number>();

    for (const category of categories) {
      if (!category.parentCategoryId) continue;
      counts.set(
        category.parentCategoryId,
        (counts.get(category.parentCategoryId) ?? 0) + 1,
      );
    }

    return counts;
  }, [categories]);

  function openCreate() {
    setEditing(null);
    setForm({
      ...emptyForm,
      parentCategoryId: activeParentId ?? "",
    });
    setSlugTouched(false);
    setError(null);
    setDialogOpen(true);
  }

  function openEdit(category: AdminCategory) {
    setEditing(category);
    setForm({
      name: category.name,
      slug: category.slug,
      parentCategoryId: category.parentCategoryId ?? "",
      imageUrl: category.imageUrl ?? "",
      isVisible: category.isVisible,
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

    const name = form.name.trim();
    const slug = form.slug.trim();
    const imageUrl = form.imageUrl.trim() || null;
    const parentCategoryId = form.parentCategoryId || null;

    if (!name || !slug) {
      setError("اسم القسم والرابط مطلوبان.");
      return;
    }

    if (imageUrl) {
      try {
        const url = new URL(imageUrl);
        if (url.protocol !== "http:" && url.protocol !== "https:") {
          throw new Error();
        }
      } catch {
        setError("رابط صورة القسم لازم يكون رابط HTTP أو HTTPS مباشر.");
        return;
      }
    }

    setSaving(true);
    setError(null);

    try {
      if (editing) {
        await updateCategory(
          tenantId,
          editing.categoryId,
          {
            name,
            slug,
            parentCategoryId,
            imageUrl,
            isVisible: form.isVisible,
          },
        );

        if (parentCategoryId !== editing.parentCategoryId) {
          await moveCategory(
            tenantId,
            editing.categoryId,
            parentCategoryId,
          );
        }
      } else {
        await createCategory(
          tenantId,
          {
            name,
            slug,
            parentCategoryId,
            imageUrl,
            isVisible: true,
          },
        );
      }

      closeDialog();
      await load();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر حفظ القسم.",
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div dir="rtl" className="mx-auto max-w-[1380px]">
      <div className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <p className="text-[11px] font-semibold text-[#9d723d]">الكتالوج</p>
          <h1 className="mt-2 text-[31px] font-semibold tracking-[-0.04em]">الأقسام</h1>
          <p className="mt-2 max-w-[700px] text-[12px] leading-7 text-black/45">
            رتّب المنتجات ضمن أقسام رئيسية وفرعية. كل قسم يمكن أن يحتوي أقسامًا أخرى أو منتجات،
            وصورته تظهر في واجهة المتجر عندما تكون موجودة.
          </p>
        </div>

        <button
          type="button"
          onClick={openCreate}
          className="inline-flex h-12 items-center gap-2 rounded-[10px] bg-[#080b14] px-5 text-[12px] font-semibold text-white"
        >
          <Plus size={17} />
          إضافة قسم
        </button>
      </div>

      <div className="mt-7 rounded-[18px] border border-black/[0.07] bg-white">
        <div className="flex items-center justify-between border-b border-black/[0.07] px-5 py-4">
          <div className="min-w-0">
            <p className="text-[12px] font-semibold">هيكل الأقسام</p>
            <div className="mt-1 flex flex-wrap items-center gap-1.5 text-[9px] text-black/42">
              <button
                type="button"
                onClick={() => setActiveParentId(null)}
                className="inline-flex items-center gap-1 hover:text-black"
              >
                <Home size={11} />
                الأقسام الرئيسية
              </button>

              {breadcrumbs.map((category) => (
                <span key={category.categoryId} className="inline-flex items-center gap-1.5">
                  <ChevronLeft size={10} className="text-black/25" />
                  <button
                    type="button"
                    onClick={() => setActiveParentId(category.categoryId)}
                    className="max-w-[180px] truncate hover:text-black"
                  >
                    {category.name}
                  </button>
                </span>
              ))}

              <span className="mr-1 text-black/28">•</span>
              <span>{currentCategories.length} داخل المستوى الحالي</span>
            </div>
          </div>
          <button
            type="button"
            onClick={() => void load()}
            className="flex size-9 items-center justify-center rounded-[9px] border border-black/[0.08]"
            aria-label="تحديث"
          >
            <RefreshCw size={14} className={loading ? "animate-spin" : ""} />
          </button>
        </div>

        {error && !dialogOpen ? (
          <div className="m-5 rounded-[12px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">
            {error}
          </div>
        ) : null}

        {loading ? (
          <div className="flex min-h-[280px] items-center justify-center text-[11px] text-black/40">
            جاري تحميل الأقسام...
          </div>
        ) : currentCategories.length === 0 ? (
          <div className="flex min-h-[320px] items-center justify-center p-6 text-center">
            <div>
              <div className="mx-auto flex size-14 items-center justify-center rounded-full bg-[#f0ece3]">
                <FolderTree size={22} className="text-black/42" />
              </div>
              <h2 className="mt-4 text-[18px] font-semibold">
                {activeParentId ? "لا توجد أقسام داخل هذا القسم" : "ابدأ بأول قسم"}
              </h2>
              <p className="mt-2 text-[10px] leading-6 text-black/42">
                {activeParentId
                  ? "أضف قسمًا فرعيًا هنا، أو ارجع للمستوى السابق من المسار بالأعلى."
                  : "مثال: جوالات ← جوالات آبل. الأقسام الفرعية ستبقى داخل القسم الأب ولن تظهر خارجه."}
              </p>
              {activeParentId ? (
                <button
                  type="button"
                  onClick={openCreate}
                  className="mt-5 inline-flex h-10 items-center gap-2 rounded-[9px] bg-[#080b14] px-4 text-[10px] font-semibold text-white"
                >
                  <Plus size={14} />
                  إضافة قسم فرعي هنا
                </button>
              ) : null}
            </div>
          </div>
        ) : (
          <div className="divide-y divide-black/[0.055]">
            {currentCategories.map((category) => {
              const childCount = childCountByCategory.get(category.categoryId) ?? 0;

              return (
              <div
                key={category.categoryId}
                role="button"
                tabIndex={0}
                onClick={() => setActiveParentId(category.categoryId)}
                onKeyDown={(event) => {
                  if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    setActiveParentId(category.categoryId);
                  }
                }}
                className="flex cursor-pointer items-center gap-4 px-5 py-4 transition hover:bg-black/[0.018]"
              >
                <div className="flex size-14 shrink-0 items-center justify-center overflow-hidden rounded-[12px] border border-black/[0.07] bg-[#f5f3ed]">
                  {category.imageUrl ? (
                    <img
                      src={category.imageUrl}
                      alt={category.name}
                      className="h-full w-full object-cover"
                    />
                  ) : (
                    <ImageIcon size={18} className="text-black/25" />
                  )}
                </div>

                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="truncate text-[12px] font-semibold">{category.name}</p>
                    <span className={`rounded-full px-2 py-1 text-[8px] font-semibold ${category.isVisible ? "bg-emerald-50 text-emerald-700" : "bg-black/[0.05] text-black/40"}`}>
                      {category.isVisible ? "ظاهر" : "مخفي"}
                    </span>
                    <span className="rounded-full bg-[#f3eadc] px-2 py-1 text-[8px] text-[#8c642f]">
                      {childCount > 0
                        ? `${childCount} قسم فرعي`
                        : "فتح القسم"}
                    </span>
                  </div>
                  <p dir="ltr" className="mt-1 truncate text-left text-[9px] text-black/34">
                    /{category.slug}
                  </p>
                </div>

                <div className="flex shrink-0 items-center gap-2">
                  <ChevronLeft size={15} className="text-black/28" />
                  <button
                    type="button"
                    onClick={(event) => {
                      event.stopPropagation();
                      openEdit(category);
                    }}
                    className="flex size-9 shrink-0 items-center justify-center rounded-[9px] border border-black/[0.08] hover:bg-black/[0.025]"
                    aria-label={`تعديل ${category.name}`}
                  >
                    <Pencil size={14} />
                  </button>
                </div>
              </div>
              );
            })}
          </div>
        )}
      </div>

      {dialogOpen ? (
        <div className="fixed inset-0 z-[100] flex items-end justify-center bg-black/35 p-0 backdrop-blur-[2px] md:items-center md:p-5">
          <div className="max-h-[92vh] w-full overflow-y-auto rounded-t-[22px] bg-[#f7f5ef] shadow-2xl md:max-w-[720px] md:rounded-[22px]">
            <div className="sticky top-0 z-10 flex items-center justify-between border-b border-black/[0.07] bg-[#f7f5ef]/95 px-6 py-5 backdrop-blur-xl">
              <div>
                <h2 className="text-[20px] font-semibold">{editing ? "تعديل القسم" : "إضافة قسم"}</h2>
                <p className="mt-1 text-[10px] text-black/42">حدّد مكان القسم وصورته وظهوره في المتجر.</p>
              </div>
              <button type="button" onClick={closeDialog} className="flex size-9 items-center justify-center rounded-full border border-black/[0.08] bg-white">
                <X size={16} />
              </button>
            </div>

            <div className="space-y-5 p-6 md:p-8">
              <div className="grid gap-5 md:grid-cols-2">
                <label className="block">
                  <span className="mb-2 block text-[10px] font-semibold">اسم القسم</span>
                  <input
                    value={form.name}
                    onChange={(event) => {
                      const value = event.target.value;
                      setForm((current) => ({
                        ...current,
                        name: value,
                        slug: slugTouched ? current.slug : createSlug(value),
                      }));
                    }}
                    className="h-12 w-full rounded-[10px] border border-black/[0.1] bg-white px-4 text-[12px] outline-none focus:border-[#a77a43]/70"
                    placeholder="مثال: أحذية رياضية"
                  />
                </label>

                <label className="block">
                  <span className="mb-2 block text-[10px] font-semibold">رابط القسم</span>
                  <input
                    dir="ltr"
                    value={form.slug}
                    onChange={(event) => {
                      setSlugTouched(true);
                      setForm((current) => ({
                        ...current,
                        slug: createSlug(event.target.value),
                      }));
                    }}
                    className="h-12 w-full rounded-[10px] border border-black/[0.1] bg-white px-4 text-left text-[12px] outline-none focus:border-[#a77a43]/70"
                    placeholder="sports-shoes"
                  />
                </label>
              </div>

              <label className="block">
                <span className="mb-2 block text-[10px] font-semibold">القسم الأب</span>
                <select
                  value={form.parentCategoryId}
                  onChange={(event) => setForm((current) => ({ ...current, parentCategoryId: event.target.value }))}
                  className="h-12 w-full rounded-[10px] border border-black/[0.1] bg-white px-4 text-[11px] outline-none"
                >
                  <option value="">قسم رئيسي</option>
                  {flattened
                    .filter(({ category }) => category.categoryId !== editing?.categoryId)
                    .map(({ category, depth }) => (
                      <option key={category.categoryId} value={category.categoryId}>
                        {`${"— ".repeat(depth)}${category.name}`}
                      </option>
                    ))}
                </select>
              </label>

              <label className="block">
                <span className="mb-2 block text-[10px] font-semibold">رابط صورة القسم <span className="font-normal text-black/35">اختياري</span></span>
                <input
                  dir="ltr"
                  value={form.imageUrl}
                  onChange={(event) => setForm((current) => ({ ...current, imageUrl: event.target.value }))}
                  className="h-12 w-full rounded-[10px] border border-black/[0.1] bg-white px-4 text-left text-[11px] outline-none focus:border-[#a77a43]/70"
                  placeholder="https://example.com/category.jpg"
                />
                <p className="mt-2 text-[9px] leading-5 text-black/36">استخدم رابطًا مباشرًا للصورة بصيغة JPG أو PNG أو WEBP أو من CDN.</p>
              </label>

              {form.imageUrl.trim() ? (
                <div className="overflow-hidden rounded-[14px] border border-black/[0.08] bg-white p-3">
                  <img src={form.imageUrl.trim()} alt="معاينة صورة القسم" className="h-48 w-full rounded-[10px] object-cover" />
                </div>
              ) : null}

              {editing ? (
                <button
                  type="button"
                  onClick={() => setForm((current) => ({ ...current, isVisible: !current.isVisible }))}
                  className="flex w-full items-center justify-between rounded-[12px] border border-black/[0.08] bg-white px-4 py-4 text-[11px]"
                >
                  <span className="flex items-center gap-2">
                    {form.isVisible ? <Eye size={16} /> : <EyeOff size={16} />}
                    إظهار القسم في المتجر
                  </span>
                  <span className={`rounded-full px-2 py-1 text-[8px] ${form.isVisible ? "bg-emerald-50 text-emerald-700" : "bg-black/[0.05] text-black/45"}`}>
                    {form.isVisible ? "مفعّل" : "موقوف"}
                  </span>
                </button>
              ) : null}

              {error ? (
                <div className="rounded-[11px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">{error}</div>
              ) : null}

              <div className="flex items-center justify-end gap-3 border-t border-black/[0.07] pt-5">
                <button type="button" onClick={closeDialog} className="h-11 px-4 text-[10px] text-black/45">إلغاء</button>
                <button
                  type="button"
                  disabled={saving}
                  onClick={() => void save()}
                  className="inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#080b14] px-5 text-[11px] font-semibold text-white disabled:opacity-50"
                >
                  {saving ? <RefreshCw size={14} className="animate-spin" /> : <Check size={14} />}
                  {editing ? "حفظ التعديل" : "إضافة القسم"}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
