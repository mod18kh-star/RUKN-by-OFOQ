import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  Image as ImageIcon,
  Save,
  X,
} from "lucide-react";

import type {
  AdminCategory,
} from "../catalog/catalogContentApi";

import {
  getProductImages,
  type Product,
  type UpdateProductInput,
} from "./productsApi";

interface EditProductDialogProps {
  open: boolean;
  busy: boolean;
  tenantId: string | null;
  product: Product | null;
  categories: AdminCategory[];
  onClose: () => void;
  onSave: (
    product: Product,
    input: UpdateProductInput,
  ) => Promise<void>;
}

interface FormState {
  name: string;
  slug: string;
  description: string;
  categoryId: string;
  primaryImageUrl: string;
  price: string;
  compareAtPrice: string;
  currency: string;
  sku: string;
  trackInventory: boolean;
  quantity: string;
  lowStockThreshold: string;
  continueSellingWhenOutOfStock: boolean;
}

function defaultVariant(
  product: Product,
) {
  return (
    product.variants.find(
      (variant) =>
        variant.isDefault,
    ) ??
    product.variants[0] ??
    null
  );
}

function stateFromProduct(
  product: Product,
): FormState {
  const variant =
    defaultVariant(
      product,
    );

  return {
    name:
      product.name,

    slug:
      product.slug,

    description:
      product.description ??
      "",

    categoryId:
      product.categoryId ??
      "",

    primaryImageUrl:
      "",

    price:
      String(
        product.price,
      ),

    compareAtPrice:
      product.compareAtPrice ===
      null
        ? ""
        : String(
            product.compareAtPrice,
          ),

    currency:
      product.currency,

    sku:
      variant?.sku ??
      "",

    trackInventory:
      variant?.trackInventory ??
      true,

    quantity:
      String(
        variant?.quantity ??
        0,
      ),

    lowStockThreshold:
      String(
        variant?.lowStockThreshold ??
        0,
      ),

    continueSellingWhenOutOfStock:
      variant
        ?.continueSellingWhenOutOfStock ??
      false,
  };
}

function categoryLabel(
  category: AdminCategory,
  categories: AdminCategory[],
) {
  const names = [
    category.name,
  ];

  let parentId =
    category.parentCategoryId;

  let guard =
    0;

  while (
    parentId &&
    guard < 8
  ) {
    const parent =
      categories.find(
        (item) =>
          item.categoryId ===
          parentId,
      );

    if (!parent) break;

    names.unshift(
      parent.name,
    );

    parentId =
      parent.parentCategoryId;

    guard +=
      1;
  }

  return names.join(
    " ← ",
  );
}

const inputClass =
  "h-12 w-full rounded-[10px] border border-black/[0.11] bg-white px-4 text-[13px] outline-none transition placeholder:text-black/25 focus:border-[#a77a43]/70";

const labelClass =
  "mb-2 block text-[11px] font-semibold text-black/62";

export function EditProductDialog({
  open,
  busy,
  tenantId,
  product,
  categories,
  onClose,
  onSave,
}: EditProductDialogProps) {
  const [
    form,
    setForm,
  ] =
    useState<FormState | null>(
      product
        ? stateFromProduct(
            product,
          )
        : null,
    );

  const [
    loadingImage,
    setLoadingImage,
  ] =
    useState(
      Boolean(
        open &&
        product &&
        tenantId,
      ),
    );

  const [
    error,
    setError,
  ] =
    useState<string | null>(
      null,
    );

  useEffect(
    () => {
      if (
        !open ||
        !product ||
        !tenantId
      ) {
        return;
      }

      let cancelled =
        false;

      void getProductImages(
        tenantId,
        product.productId,
      )
        .then(
          (result) => {
            if (
              cancelled
            ) {
              return;
            }

            const primary =
              result.images.find(
                (image) =>
                  image.isPrimary,
              ) ??
              result.images[0] ??
              null;

            setForm(
              (current) =>
                current
                  ? {
                      ...current,
                      primaryImageUrl:
                        primary?.url ??
                        "",
                    }
                  : current,
            );
          },
        )
        .catch(
          (exception: unknown) => {
            if (
              cancelled
            ) {
              return;
            }

            setError(
              exception instanceof Error
                ? exception.message
                : "تعذر تحميل صورة المنتج.",
            );
          },
        )
        .finally(
          () => {
            if (
              !cancelled
            ) {
              setLoadingImage(
                false,
              );
            }
          },
        );

      return () => {
        cancelled =
          true;
      };
    },
    [
      open,
      product,
      tenantId,
    ],
  );

  const visibleCategories =
    useMemo(
      () =>
        categories.filter(
          (category) =>
            category.isVisible,
        ),
      [
        categories,
      ],
    );

  if (
    !open ||
    !product ||
    !form
  ) {
    return null;
  }

  function update(
    key: keyof FormState,
    value:
      string |
      boolean,
  ) {
    setForm(
      (current) =>
        current
          ? {
              ...current,
              [key]:
                value,
            }
          : current,
    );
  }

  async function submit() {
    const currentForm =
      form;

    const currentProduct =
      product;

    if (
      busy ||
      !currentForm ||
      !currentProduct
    ) {
      return;
    }

    const price =
      Number(
        currentForm.price,
      );

    const compareAtPrice =
      currentForm.compareAtPrice.trim()
        ? Number(
            currentForm.compareAtPrice,
          )
        : null;

    const quantity =
      currentForm.trackInventory
        ? Number(
            currentForm.quantity,
          )
        : 0;

    const lowStockThreshold =
      currentForm.trackInventory
        ? Number(
            currentForm.lowStockThreshold,
          )
        : 0;

    if (
      !currentForm.name.trim() ||
      !currentForm.slug.trim() ||
      !currentForm.sku.trim() ||
      !currentForm.categoryId
    ) {
      setError(
        "الاسم والرابط وSKU والقسم مطلوبة.",
      );

      return;
    }

    if (
      !Number.isFinite(
        price,
      ) ||
      price < 0
    ) {
      setError(
        "تأكد من سعر المنتج.",
      );

      return;
    }

    if (
      compareAtPrice !==
        null &&
      (
        !Number.isFinite(
          compareAtPrice,
        ) ||
        compareAtPrice < 0
      )
    ) {
      setError(
        "تأكد من السعر قبل الخصم.",
      );

      return;
    }

    if (
      !Number.isInteger(
        quantity,
      ) ||
      quantity < 0
    ) {
      setError(
        "كمية المخزون غير صحيحة.",
      );

      return;
    }

    if (
      !Number.isInteger(
        lowStockThreshold,
      ) ||
      lowStockThreshold < 0
    ) {
      setError(
        "حد المخزون المنخفض غير صحيح.",
      );

      return;
    }

    setError(
      null,
    );

    try {
      await onSave(
        currentProduct,
        {
          name:
            currentForm.name.trim(),

          slug:
            currentForm.slug
              .trim()
              .toLowerCase(),

          description:
            currentForm.description.trim() ||
            null,

          categoryId:
            currentForm.categoryId ||
            null,

          price,

          compareAtPrice,

          currency:
            currentForm.currency
              .trim()
              .toUpperCase(),

          sku:
            currentForm.sku.trim(),

          trackInventory:
            currentForm.trackInventory,

          quantity,

          lowStockThreshold,

          continueSellingWhenOutOfStock:
            currentForm
              .continueSellingWhenOutOfStock,

          primaryImageUrl:
            currentForm.primaryImageUrl.trim() ||
            null,
        },
      );

      onClose();
    } catch (
      exception
    ) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر حفظ تعديلات المنتج.",
      );
    }
  }

  return (
    <div
      dir="rtl"
      className="fixed inset-0 z-[100] flex items-center justify-center bg-black/35 p-4 backdrop-blur-[2px]"
      onMouseDown={(
        event,
      ) => {
        if (
          event.target ===
          event.currentTarget &&
          !busy
        ) {
          onClose();
        }
      }}
    >
      <div className="max-h-[92vh] w-full max-w-[920px] overflow-hidden rounded-[20px] border border-black/[0.08] bg-[#f8f7f3] shadow-[0_35px_100px_rgba(0,0,0,.22)]">
        <div className="flex items-center justify-between border-b border-black/[0.08] bg-white px-6 py-5">
          <div>
            <p className="text-[10px] font-semibold text-[#9d723d]">
              إدارة المنتج
            </p>

            <h2 className="mt-1 text-[20px] font-semibold">
              تعديل {product.name}
            </h2>
          </div>

          <button
            type="button"
            disabled={busy}
            onClick={onClose}
            className="flex size-10 items-center justify-center rounded-[9px] border border-black/[0.08] bg-white disabled:opacity-40"
            aria-label="إغلاق"
          >
            <X size={17} />
          </button>
        </div>

        <div className="max-h-[calc(92vh-150px)] overflow-y-auto p-6">
          <div className="grid gap-5 md:grid-cols-2">
            <div>
              <label className={labelClass}>اسم المنتج</label>
              <input
                value={form.name}
                onChange={(event) => update("name", event.target.value)}
                className={inputClass}
              />
            </div>

            <div>
              <label className={labelClass}>رابط المنتج</label>
              <input
                dir="ltr"
                value={form.slug}
                onChange={(event) => update("slug", event.target.value)}
                className={`${inputClass} text-left`}
              />
            </div>

            <div className="md:col-span-2">
              <label className={labelClass}>الوصف</label>
              <textarea
                value={form.description}
                onChange={(event) => update("description", event.target.value)}
                className="min-h-[130px] w-full resize-y rounded-[10px] border border-black/[0.11] bg-white p-4 text-[13px] leading-7 outline-none focus:border-[#a77a43]/70"
              />
            </div>

            <div>
              <label className={labelClass}>القسم</label>
              <select
                value={form.categoryId}
                onChange={(event) => update("categoryId", event.target.value)}
                className={inputClass}
              >
                <option value="">اختر القسم</option>
                {visibleCategories.map((category) => (
                  <option
                    key={category.categoryId}
                    value={category.categoryId}
                  >
                    {categoryLabel(category, categories)}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className={labelClass}>SKU</label>
              <input
                dir="ltr"
                maxLength={64}
                value={form.sku}
                onChange={(event) => update("sku", event.target.value)}
                className={`${inputClass} text-left`}
              />
            </div>

            <div>
              <label className={labelClass}>السعر</label>
              <input
                dir="ltr"
                type="number"
                min="0"
                step="0.01"
                value={form.price}
                onChange={(event) => update("price", event.target.value)}
                className={`${inputClass} text-left`}
              />
            </div>

            <div>
              <label className={labelClass}>السعر قبل الخصم</label>
              <input
                dir="ltr"
                type="number"
                min="0"
                step="0.01"
                value={form.compareAtPrice}
                onChange={(event) => update("compareAtPrice", event.target.value)}
                className={`${inputClass} text-left`}
              />
            </div>

            <div>
              <label className={labelClass}>العملة</label>
              <select
                value={form.currency}
                onChange={(event) => update("currency", event.target.value)}
                className={inputClass}
              >
                <option value="SAR">SAR — ريال سعودي</option>
                <option value="AED">AED — درهم إماراتي</option>
                <option value="USD">USD — دولار أمريكي</option>
              </select>
            </div>

            <div className="md:col-span-2">
              <label className={labelClass}>صورة المنتج الرئيسية</label>
              <div className="grid gap-4 md:grid-cols-[1fr_190px]">
                <div>
                  <input
                    dir="ltr"
                    value={form.primaryImageUrl}
                    onChange={(event) => update("primaryImageUrl", event.target.value)}
                    className={`${inputClass} text-left`}
                    placeholder="https://cdn.example.com/product.jpg"
                  />
                  <p className="mt-2 text-[9px] leading-5 text-black/38">
                    تقدر تغيّر الصورة أو تمسح الرابط لإزالة صور المنتج الحالية.
                  </p>
                </div>

                <div className="flex h-[155px] items-center justify-center overflow-hidden rounded-[12px] border border-black/[0.08] bg-white">
                  {loadingImage ? (
                    <p className="text-[10px] text-black/35">جاري تحميل الصورة...</p>
                  ) : form.primaryImageUrl.trim() ? (
                    <img
                      src={form.primaryImageUrl.trim()}
                      alt="معاينة صورة المنتج"
                      className="h-full w-full object-cover"
                    />
                  ) : (
                    <div className="text-center text-black/28">
                      <ImageIcon size={22} className="mx-auto" />
                      <p className="mt-2 text-[9px]">بدون صورة</p>
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="md:col-span-2 border-t border-black/[0.07] pt-5">
              <label className="flex items-center justify-between gap-4">
                <div>
                  <p className="text-[11px] font-semibold">تتبع المخزون</p>
                  <p className="mt-1 text-[9px] text-black/38">
                    فعّله حتى تتابع الكمية والتنبيه على المخزون المنخفض.
                  </p>
                </div>

                <input
                  type="checkbox"
                  checked={form.trackInventory}
                  onChange={(event) => update("trackInventory", event.target.checked)}
                  className="size-4"
                />
              </label>
            </div>

            {form.trackInventory ? (
              <>
                <div>
                  <label className={labelClass}>الكمية الحالية</label>
                  <input
                    dir="ltr"
                    type="number"
                    min="0"
                    step="1"
                    value={form.quantity}
                    onChange={(event) => update("quantity", event.target.value)}
                    className={`${inputClass} text-left`}
                  />
                </div>

                <div>
                  <label className={labelClass}>تنبيه عند كمية</label>
                  <input
                    dir="ltr"
                    type="number"
                    min="0"
                    step="1"
                    value={form.lowStockThreshold}
                    onChange={(event) => update("lowStockThreshold", event.target.value)}
                    className={`${inputClass} text-left`}
                  />
                </div>

                <label className="md:col-span-2 flex items-center gap-3 rounded-[10px] border border-black/[0.08] bg-white p-4">
                  <input
                    type="checkbox"
                    checked={form.continueSellingWhenOutOfStock}
                    onChange={(event) => update("continueSellingWhenOutOfStock", event.target.checked)}
                    className="size-4"
                  />

                  <div>
                    <p className="text-[11px] font-semibold">
                      السماح بالبيع عند نفاد المخزون
                    </p>
                    <p className="mt-1 text-[9px] text-black/38">
                      استخدمه فقط إذا كنت قادرًا على توفير المنتج بعد الطلب.
                    </p>
                  </div>
                </label>
              </>
            ) : null}
          </div>

          {error ? (
            <div className="mt-5 rounded-[10px] border border-red-200 bg-red-50 p-4 text-[10px] leading-6 text-red-700">
              {error}
            </div>
          ) : null}
        </div>

        <div className="flex items-center justify-between gap-3 border-t border-black/[0.08] bg-white px-6 py-4">
          <p className="text-[9px] text-black/35">
            النشر والظهور والأرشفة تظل متاحة من جدول المنتجات.
          </p>

          <button
            type="button"
            disabled={busy || loadingImage}
            onClick={() => void submit()}
            className="inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#080b14] px-5 text-[11px] font-semibold text-white disabled:opacity-40"
          >
            <Save size={15} />
            {busy ? "جاري الحفظ..." : "حفظ التعديلات"}
          </button>
        </div>
      </div>
    </div>
  );
}
