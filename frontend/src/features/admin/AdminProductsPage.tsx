import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  AlertTriangle,
  Boxes,
  Archive,
  Eye,
  EyeOff,
  Pencil,
  PackageCheck,
  PackagePlus,
  RefreshCw,
  RotateCcw,
  Search,
  ShieldAlert,
} from "lucide-react";

import {
  CreateProductDialog,
} from "./products/CreateProductDialog";

import {
  EditProductDialog,
} from "./products/EditProductDialog";

import {
  getCategories,
  type AdminCategory,
} from "./catalog/catalogContentApi";

import {
  archiveProduct,
  createProduct,
  getCurrentTenantId,
  getProducts,
  hideProduct,
  moveProductToDraft,
  ProductsApiError,
  publishProduct,
  setPrimaryProductImage,
  showProduct,
  updateProduct,
  updateProductInventory,
  type CreateProductInput,
  type Product,
  type UpdateProductInput,
} from "./products/productsApi";

type StatusFilter =
  | "all"
  | "draft"
  | "published"
  | "archived";

function normalizeStatus(
  status: string,
) {
  return status
    .trim()
    .toLowerCase();
}

function statusLabel(
  status: string,
) {
  const value =
    normalizeStatus(
      status,
    );

  if (
    value === "published"
  ) {
    return "منشور";
  }

  if (
    value === "archived"
  ) {
    return "مؤرشف";
  }

  return "مسودة";
}

function statusClasses(
  status: string,
) {
  const value =
    normalizeStatus(
      status,
    );

  if (
    value === "published"
  ) {
    return (
      "bg-emerald-50 " +
      "text-emerald-700 " +
      "border-emerald-100"
    );
  }

  if (
    value === "archived"
  ) {
    return (
      "bg-black/[0.04] " +
      "text-black/45 " +
      "border-black/[0.07]"
    );
  }

  return (
    "bg-[#f3eadc] " +
    "text-[#8c642f] " +
    "border-[#dfceb4]"
  );
}

function formatMoney(
  value: number,
  currency: string,
) {
  try {
    return new Intl.NumberFormat(
      "ar",
      {
        style: "currency",
        currency,
        maximumFractionDigits:
          2,
      },
    ).format(value);
  } catch {
    return `${value} ${currency}`;
  }
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

export function AdminProductsPage() {
  const tenantId =
    getCurrentTenantId();

  const [products, setProducts] =
    useState<Product[]>([]);

  const [categories, setCategories] =
    useState<AdminCategory[]>([]);

  const [loading, setLoading] =
    useState(true);

  const [creating, setCreating] =
    useState(false);

  const [changingProductId, setChangingProductId] =
    useState<string | null>(null);

  const [editingProduct, setEditingProduct] =
    useState<Product | null>(null);

  const [savingEdit, setSavingEdit] =
    useState(false);

  const [createOpen, setCreateOpen] =
    useState(false);

  const [query, setQuery] =
    useState("");

  const [status, setStatus] =
    useState<StatusFilter>(
      "all",
    );

  const [error, setError] =
    useState<{
      message: string;
      status: number | null;
    } | null>(null);

  const loadProducts =
    useCallback(
      async () => {
        if (!tenantId) {
          setProducts([]);
          setCategories([]);
          setError({
            message:
              "ما لقينا متجر مرتبط بلوحة الإدارة. كمّل إعداد المتجر أولًا.",
            status: null,
          });

          setLoading(false);
          return;
        }

        setLoading(true);
        setError(null);

        try {
          const [
            productResult,
            categoryResult,
          ] =
            await Promise.all([
              getProducts(
                tenantId,
              ),
              getCategories(
                tenantId,
              ),
            ]);

          setProducts(
            productResult,
          );

          setCategories(
            categoryResult,
          );
        } catch (exception) {
          if (
            exception instanceof
            ProductsApiError
          ) {
            setError({
              message:
                exception.message,
              status:
                exception.status,
            });
          } else {
            setError({
              message:
                "تعذر الاتصال بالخادم. تأكد أن Backend شغال.",
              status: null,
            });
          }
        } finally {
          setLoading(false);
        }
      },
      [tenantId],
    );

  useEffect(
    () => {
      const timer =
        window.setTimeout(
          () => {
            void loadProducts();
          },
          0,
        );

      return () => {
        window.clearTimeout(
          timer,
        );
      };
    },
    [loadProducts],
  );

  const filtered =
    useMemo(
      () => {
        const normalizedQuery =
          query
            .trim()
            .toLowerCase();

        return products.filter(
          (product) => {
            const matchesStatus =
              status === "all" ||
              normalizeStatus(
                product.status,
              ) === status;

            if (
              !matchesStatus
            ) {
              return false;
            }

            if (
              !normalizedQuery
            ) {
              return true;
            }

            const variant =
              defaultVariant(
                product,
              );

            return [
              product.name,
              product.slug,
              variant?.sku ?? "",
            ].some(
              (value) =>
                value
                  .toLowerCase()
                  .includes(
                    normalizedQuery,
                  ),
            );
          },
        );
      },
      [
        products,
        query,
        status,
      ],
    );

  const publishedCount =
    products.filter(
      (product) =>
        normalizeStatus(
          product.status,
        ) === "published",
    ).length;

  const draftCount =
    products.filter(
      (product) =>
        normalizeStatus(
          product.status,
        ) === "draft",
    ).length;

  const lowStockCount =
    products.filter(
      (product) => {
        const variant =
          defaultVariant(
            product,
          );

        return Boolean(
          variant &&
          variant.trackInventory &&
          variant.quantity <=
            variant.lowStockThreshold,
        );
      },
    ).length;

  async function handleCreate(
    input:
      CreateProductInput,
  ) {
    if (!tenantId) {
      throw new Error(
        "كمّل إعداد المتجر أولًا.",
      );
    }

    setCreating(true);

    try {
      const created =
        await createProduct(
          tenantId,
          input,
        );

      let finalProduct =
        created;

      if (
        input.publishImmediately
      ) {
        try {
          finalProduct =
            await publishProduct(
              tenantId,
              created.productId,
            );
        } catch (exception) {
          setProducts(
            (current) => [
              created,
              ...current,
            ],
          );

          throw new Error(
            exception instanceof Error
              ? `تم حفظ المنتج كمسودة، لكن تعذر نشره: ${exception.message}`
              : "تم حفظ المنتج كمسودة، لكن تعذر نشره.",
            {
              cause:
                exception,
            },
          );
        }
      }

      setProducts(
        (current) => [
          finalProduct,
          ...current,
        ],
      );

      return finalProduct;
    } finally {
      setCreating(false);
    }
  }

  async function handleEdit(
    product: Product,
    input:
      UpdateProductInput,
  ) {
    if (!tenantId) {
      throw new Error(
        "ما لقينا متجر مرتبط بالحساب.",
      );
    }

    setSavingEdit(
      true,
    );

    try {
      await updateProduct(
        tenantId,
        product.productId,
        input,
      );

      await updateProductInventory(
        tenantId,
        product.productId,
        input,
      );

      await setPrimaryProductImage(
        tenantId,
        product.productId,
        input.primaryImageUrl,
        input.name,
      );

      await loadProducts();
    } finally {
      setSavingEdit(
        false,
      );
    }
  }

  async function changeState(
    product: Product,
    action:
      | "publish"
      | "draft"
      | "archive"
      | "show"
      | "hide",
  ) {
    if (!tenantId) return;

    setChangingProductId(
      product.productId,
    );

    setError(null);

    try {
      let updated:
        Product;

      if (action === "publish") {
        updated =
          await publishProduct(
            tenantId,
            product.productId,
          );
      } else if (action === "draft") {
        updated =
          await moveProductToDraft(
            tenantId,
            product.productId,
          );
      } else if (action === "archive") {
        updated =
          await archiveProduct(
            tenantId,
            product.productId,
          );
      } else if (action === "show") {
        updated =
          await showProduct(
            tenantId,
            product.productId,
          );
      } else {
        updated =
          await hideProduct(
            tenantId,
            product.productId,
          );
      }

      setProducts(
        (current) =>
          current.map(
            (item) =>
              item.productId ===
              updated.productId
                ? updated
                : item,
          ),
      );
    } catch (exception) {
      setError({
        message:
          exception instanceof Error
            ? exception.message
            : "تعذر تغيير حالة المنتج.",
        status:
          exception instanceof ProductsApiError
            ? exception.status
            : null,
      });
    } finally {
      setChangingProductId(
        null,
      );
    }
  }

  return (
    <div
      dir="rtl"
      className="mx-auto max-w-[1380px]"
    >
      <div className="flex flex-wrap items-end justify-between gap-5">
        <div>
          <p className="text-[11px] font-semibold text-[#9d723d]">
            الكتالوج
          </p>

          <h1 className="mt-2 text-[31px] font-semibold tracking-[-0.04em]">
            المنتجات
          </h1>

          <p className="mt-2 max-w-[620px] text-[12px] leading-7 text-black/45">
            أضف منتجاتك وعدّل الاسم والوصف
            والسعر والصورة والقسم والمخزون
            والنشر والظهور من مكان واحد.
          </p>
        </div>

        <button
          type="button"
          onClick={() =>
            setCreateOpen(
              true,
            )
          }
          style={{
            color:
              "#ffffff",
          }}
          className="inline-flex h-12 items-center gap-2.5 rounded-[10px] bg-[#080b14] px-5 text-[12px] font-semibold shadow-[0_10px_28px_rgba(8,11,20,.12)]"
        >
          <PackagePlus
            size={17}
          />
          إضافة منتج
        </button>
      </div>

      <div className="mt-8 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {[
          {
            label:
              "كل المنتجات",
            value:
              products.length,
          },
          {
            label:
              "منشورة",
            value:
              publishedCount,
          },
          {
            label:
              "مسودات",
            value:
              draftCount,
          },
          {
            label:
              "مخزون يحتاج انتباه",
            value:
              lowStockCount,
          },
        ].map(
          (metric) => (
            <div
              key={
                metric.label
              }
              className="rounded-[15px] border border-black/[0.07] bg-white p-5"
            >
              <p className="text-[10px] text-black/43">
                {metric.label}
              </p>

              <p className="mt-3 text-[26px] font-semibold tracking-[-0.04em]">
                {metric.value}
              </p>
            </div>
          ),
        )}
      </div>

      <div className="mt-6 rounded-[17px] border border-black/[0.07] bg-white">
        <div className="flex flex-col gap-4 border-b border-black/[0.07] p-4 md:flex-row md:items-center md:justify-between">
          <div className="relative w-full md:max-w-[360px]">
            <Search
              size={16}
              className="pointer-events-none absolute right-4 top-1/2 -translate-y-1/2 text-black/32"
            />

            <input
              value={query}
              onChange={(
                event,
              ) =>
                setQuery(
                  event.target
                    .value,
                )
              }
              className="h-11 w-full rounded-[9px] border border-black/[0.09] bg-[#f8f7f3] pr-11 pl-4 text-[11px] outline-none placeholder:text-black/28 focus:border-[#a77a43]/60"
              placeholder="ابحث بالاسم أو الرابط أو SKU"
            />
          </div>

          <div className="flex flex-wrap items-center gap-2">
            {(
              [
                [
                  "all",
                  "الكل",
                ],
                [
                  "draft",
                  "مسودة",
                ],
                [
                  "published",
                  "منشور",
                ],
                [
                  "archived",
                  "مؤرشف",
                ],
              ] as const
            ).map(
              ([
                value,
                label,
              ]) => (
                <button
                  key={value}
                  type="button"
                  onClick={() =>
                    setStatus(
                      value,
                    )
                  }
                  className={[
                    "h-9 rounded-[8px] px-3.5 text-[10px] font-semibold transition",
                    status ===
                    value
                      ? "bg-[#080b14] text-white"
                      : "bg-[#f3f2ed] text-black/48 hover:text-black",
                  ].join(" ")}
                >
                  {label}
                </button>
              ),
            )}

            <button
              type="button"
              onClick={() =>
                void loadProducts()
              }
              className="flex size-9 items-center justify-center rounded-[8px] border border-black/[0.08]"
              aria-label="تحديث"
            >
              <RefreshCw
                size={14}
              />
            </button>
          </div>
        </div>

        {loading ? (
          <div className="flex min-h-[320px] items-center justify-center">
            <div className="text-center">
              <RefreshCw
                size={20}
                className="mx-auto animate-spin text-black/28"
              />

              <p className="mt-3 text-[11px] text-black/42">
                جاري تحميل المنتجات...
              </p>
            </div>
          </div>
        ) : error ? (
          <div className="flex min-h-[330px] items-center justify-center p-6">
            <div className="max-w-[520px] text-center">
              <div className="mx-auto flex size-12 items-center justify-center rounded-full bg-[#f1e8dc]">
                {error.status ===
                403 ? (
                  <ShieldAlert
                    size={20}
                    className="text-[#956a34]"
                  />
                ) : (
                  <AlertTriangle
                    size={20}
                    className="text-[#956a34]"
                  />
                )}
              </div>

              <h2 className="mt-5 text-[18px] font-semibold">
                {error.status ===
                403
                  ? "الحساب يحتاج إكمال الحماية"
                  : "ما قدرنا نحمّل المنتجات"}
              </h2>

              <p className="mt-3 text-[11px] leading-7 text-black/48">
                {error.message}
              </p>

              {error.status ===
              403 ? (
                <p className="mt-3 text-[10px] leading-6 text-black/38">
                  ما رح نخفف حماية لوحة الإدارة.
                  الخطوة القادمة رح نكمل إعداد MFA
                  وبعدها تشتغل إدارة المنتجات فعليًا.
                </p>
              ) : null}

              <button
                type="button"
                onClick={() =>
                  void loadProducts()
                }
                className="mt-6 h-10 rounded-[8px] border border-black/12 px-4 text-[10px] font-semibold"
              >
                إعادة المحاولة
              </button>
            </div>
          </div>
        ) : filtered.length ===
          0 ? (
          <div className="flex min-h-[350px] items-center justify-center p-6">
            <div className="max-w-[430px] text-center">
              <div className="mx-auto flex size-14 items-center justify-center rounded-full bg-[#f0ece3]">
                <Boxes
                  size={22}
                  className="text-black/40"
                />
              </div>

              <h2 className="mt-5 text-[19px] font-semibold">
                {products.length ===
                0
                  ? "أضف أول منتج"
                  : "ما لقينا نتائج"}
              </h2>

              <p className="mt-3 text-[11px] leading-7 text-black/46">
                {products.length ===
                0
                  ? "خلينا نبدأ بالمعلومات الأساسية وبعدها نكمل الصور والخيارات والمواصفات."
                  : "جرّب تغيّر البحث أو حالة المنتج."}
              </p>

              {products.length ===
              0 ? (
                <button
                  type="button"
                  onClick={() =>
                    setCreateOpen(
                      true,
                    )
                  }
                  style={{
                    color:
                      "#ffffff",
                  }}
                  className="mt-6 inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#080b14] px-5 text-[11px] font-semibold"
                >
                  <PackagePlus
                    size={15}
                  />
                  إضافة أول منتج
                </button>
              ) : null}
            </div>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[850px] border-collapse text-right">
              <thead>
                <tr className="border-b border-black/[0.07] bg-[#faf9f6]">
                  <th className="px-5 py-4 text-[10px] font-semibold text-black/42">
                    المنتج
                  </th>

                  <th className="px-5 py-4 text-[10px] font-semibold text-black/42">
                    السعر
                  </th>

                  <th className="px-5 py-4 text-[10px] font-semibold text-black/42">
                    SKU
                  </th>

                  <th className="px-5 py-4 text-[10px] font-semibold text-black/42">
                    المخزون
                  </th>

                  <th className="px-5 py-4 text-[10px] font-semibold text-black/42">
                    الحالة
                  </th>

                  <th className="px-5 py-4 text-[10px] font-semibold text-black/42">
                    الظهور
                  </th>

                  <th className="px-5 py-4 text-[10px] font-semibold text-black/42">
                    الإجراءات
                  </th>
                </tr>
              </thead>

              <tbody>
                {filtered.map(
                  (product) => {
                    const variant =
                      defaultVariant(
                        product,
                      );

                    const lowStock =
                      Boolean(
                        variant &&
                        variant.trackInventory &&
                        variant.quantity <=
                          variant.lowStockThreshold,
                      );

                    return (
                      <tr
                        key={
                          product.productId
                        }
                        className="border-b border-black/[0.06] last:border-b-0 hover:bg-[#fbfaf7]"
                      >
                        <td className="px-5 py-4">
                          <div className="flex items-center gap-3">
                            <div className="flex size-11 shrink-0 items-center justify-center rounded-[10px] bg-[#efebe2]">
                              <Boxes
                                size={17}
                                className="text-[#8d6a3e]"
                              />
                            </div>

                            <div>
                              <p className="text-[12px] font-semibold">
                                {
                                  product.name
                                }
                              </p>

                              <p
                                dir="ltr"
                                className="mt-1 text-left text-[9px] text-black/35"
                              >
                                /{
                                  product.slug
                                }
                              </p>
                            </div>
                          </div>
                        </td>

                        <td className="px-5 py-4">
                          <p className="text-[11px] font-semibold">
                            {formatMoney(
                              product.price,
                              product.currency,
                            )}
                          </p>

                          {product.compareAtPrice !==
                          null ? (
                            <p className="mt-1 text-[9px] text-black/35 line-through">
                              {formatMoney(
                                product.compareAtPrice,
                                product.currency,
                              )}
                            </p>
                          ) : null}
                        </td>

                        <td
                          dir="ltr"
                          className="px-5 py-4 text-left text-[10px] text-black/55"
                        >
                          {variant?.sku ??
                            "—"}
                        </td>

                        <td className="px-5 py-4">
                          {variant?.trackInventory ? (
                            <div>
                              <p
                                className={[
                                  "text-[11px] font-semibold",
                                  lowStock
                                    ? "text-amber-700"
                                    : "",
                                ].join(" ")}
                              >
                                {
                                  variant.quantity
                                }
                              </p>

                              {lowStock ? (
                                <p className="mt-1 text-[9px] text-amber-700/70">
                                  مخزون منخفض
                                </p>
                              ) : null}
                            </div>
                          ) : (
                            <span className="text-[9px] text-black/35">
                              غير متتبع
                            </span>
                          )}
                        </td>

                        <td className="px-5 py-4">
                          <span
                            className={[
                              "inline-flex rounded-full border px-3 py-1.5 text-[9px] font-semibold",
                              statusClasses(
                                product.status,
                              ),
                            ].join(" ")}
                          >
                            {statusLabel(
                              product.status,
                            )}
                          </span>
                        </td>

                        <td className="px-5 py-4">
                          <span
                            className={[
                              "inline-flex items-center gap-2 text-[10px] font-medium",
                              product.isVisible
                                ? "text-emerald-700"
                                : "text-black/38",
                            ].join(" ")}
                          >
                            <span
                              className={[
                                "size-2 rounded-full",
                                product.isVisible
                                  ? "bg-emerald-500"
                                  : "bg-black/15",
                              ].join(" ")}
                            />

                            {product.isVisible
                              ? "ظاهر"
                              : "مخفي"}
                          </span>
                        </td>
                        <td className="px-5 py-4">
                          <div className="flex flex-wrap items-center gap-2">
                            <button
                              type="button"
                              disabled={changingProductId === product.productId || savingEdit}
                              onClick={() => setEditingProduct(product)}
                              className="inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-black/[0.09] bg-white px-3 text-[9px] font-semibold disabled:opacity-40"
                            >
                              <Pencil size={13} />
                              تعديل
                            </button>

                            {normalizeStatus(product.status) !== "published" ? (
                              <button
                                type="button"
                                disabled={changingProductId === product.productId}
                                onClick={() => void changeState(product, "publish")}
                                className="inline-flex h-8 items-center gap-1.5 rounded-[7px] bg-[#080b14] px-3 text-[9px] font-semibold text-white disabled:opacity-40"
                              >
                                <PackageCheck size={13} />
                                نشر
                              </button>
                            ) : (
                              <button
                                type="button"
                                disabled={changingProductId === product.productId}
                                onClick={() => void changeState(product, "draft")}
                                className="inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-black/[0.09] bg-white px-3 text-[9px] font-semibold disabled:opacity-40"
                              >
                                <RotateCcw size={13} />
                                لمسودة
                              </button>
                            )}

                            {normalizeStatus(product.status) === "published" ? (
                              <button
                                type="button"
                                disabled={changingProductId === product.productId}
                                onClick={() =>
                                  void changeState(
                                    product,
                                    product.isVisible ? "hide" : "show",
                                  )
                                }
                                className="inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-black/[0.09] bg-white px-3 text-[9px] font-semibold disabled:opacity-40"
                              >
                                {product.isVisible ? <EyeOff size={13} /> : <Eye size={13} />}
                                {product.isVisible ? "إخفاء" : "إظهار"}
                              </button>
                            ) : null}

                            {normalizeStatus(product.status) !== "archived" ? (
                              <button
                                type="button"
                                disabled={changingProductId === product.productId}
                                onClick={() => void changeState(product, "archive")}
                                className="inline-flex size-8 items-center justify-center rounded-[7px] border border-black/[0.09] bg-white text-black/45 disabled:opacity-40"
                                aria-label="أرشفة المنتج"
                                title="أرشفة المنتج"
                              >
                                <Archive size={13} />
                              </button>
                            ) : null}
                          </div>
                        </td>
                      </tr>
                    );
                  },
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <CreateProductDialog
        open={createOpen}
        busy={creating}
        categories={categories}
        onClose={() =>
          setCreateOpen(
            false,
          )
        }
        onCreate={
          handleCreate
        }
      />

      <EditProductDialog
        key={
          editingProduct?.productId ??
          "no-product"
        }
        open={Boolean(
          editingProduct,
        )}
        busy={savingEdit}
        tenantId={tenantId}
        product={
          editingProduct
        }
        categories={
          categories
        }
        onClose={() =>
          setEditingProduct(
            null,
          )
        }
        onSave={
          handleEdit
        }
      />
    </div>
  );
}