import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  AlertTriangle,
  Archive,
  ArrowDownUp,
  Boxes,
  CheckCircle2,
  CircleAlert,
  Ellipsis,
  Eye,
  EyeOff,
  Image as ImageIcon,
  Layers3,
  PackageCheck,
  PackagePlus,
  Pencil,
  RefreshCw,
  RotateCcw,
  Search,
  ShieldAlert,
  SlidersHorizontal,
  Sparkles,
  Warehouse,
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
  getProductImages,
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
  type ProductImage,
  type UpdateProductInput,
} from "./products/productsApi";

import {
  getMerchantDashboardSummary,
  type MerchantDashboardTopProduct,
} from "./dashboard/adminDashboardApi";

import "./products/AdminProductsCardsV2.css";
import "./products/AdminProductsV4.css";
import "./products/AdminProductsV42.css";
import "./products/AdminProductsV43.css";

type StatusFilter =
  | "all"
  | "draft"
  | "published"
  | "archived";

type AttentionFilter =
  | "all"
  | "low-stock"
  | "hidden"
  | "no-image";

type SortMode =
  | "newest"
  | "name"
  | "price-high"
  | "stock-low";

type ProductImageState =
  | {
      state: "loading";
    }
  | {
      state: "ready";
      image: ProductImage | null;
    }
  | {
      state: "error";
    };

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

  if (value === "published") {
    return "منشور";
  }

  if (value === "archived") {
    return "مؤرشف";
  }

  return "مسودة";
}

function statusClass(
  status: string,
) {
  const value =
    normalizeStatus(
      status,
    );

  if (value === "published") {
    return "is-published";
  }

  if (value === "archived") {
    return "is-archived";
  }

  return "is-draft";
}

function formatMoney(
  value: number,
  currency: string,
) {
  try {
    return new Intl.NumberFormat(
      "en-US",
      {
        style: "currency",
        currency,
        maximumFractionDigits: 2,
      },
    ).format(value);
  } catch {
    return `${value} ${currency}`;
  }
}

function formatDate(
  value: string,
) {
  const date =
    new Date(value);

  if (
    Number.isNaN(
      date.getTime(),
    )
  ) {
    return "—";
  }

  return new Intl.DateTimeFormat(
    "ar-SA-u-nu-latn",
    {
      day: "numeric",
      month: "short",
      year: "numeric",
    },
  ).format(date);
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

function discountPercentage(
  product: Product,
) {
  if (
    product.compareAtPrice === null ||
    product.compareAtPrice <=
      product.price ||
    product.compareAtPrice <= 0
  ) {
    return null;
  }

  return Math.round(
    (
      1 -
      product.price /
        product.compareAtPrice
    ) *
      100,
  );
}

function inventoryMeta(
  product: Product,
) {
  const variant =
    defaultVariant(
      product,
    );

  if (
    !variant ||
    !variant.trackInventory
  ) {
    return {
      tracked: false,
      quantity: null,
      label: "غير متتبع",
      tone: "untracked",
      progress: 0,
    };
  }

  const quantity =
    variant.quantity;

  const threshold =
    Math.max(
      variant.lowStockThreshold,
      0,
    );

  if (quantity <= 0) {
    return {
      tracked: true,
      quantity,
      label: "نفد المخزون",
      tone: "out",
      progress: 0,
    };
  }

  if (
    quantity <=
    threshold
  ) {
    const target =
      Math.max(
        threshold,
        1,
      );

    return {
      tracked: true,
      quantity,
      label: "مخزون منخفض",
      tone: "low",
      progress:
        Math.min(
          100,
          Math.max(
            8,
            (
              quantity /
              target
            ) *
              100,
          ),
        ),
    };
  }

  const healthyTarget =
    Math.max(
      threshold * 3,
      20,
    );

  return {
    tracked: true,
    quantity,
    label: "متوفر",
    tone: "healthy",
    progress:
      Math.min(
        100,
        Math.max(
          28,
          (
            quantity /
            healthyTarget
          ) *
            100,
        ),
      ),
  };
}

function ProductSkeleton() {
  return (
    <article className="rukn-products-v2-card is-skeleton">
      <div className="rukn-products-v2-media">
        <div className="rukn-admin-skeleton absolute inset-0" />
      </div>

      <div className="rukn-products-v2-card-body">
        <div className="rukn-admin-skeleton h-3 w-[58%]" />
        <div className="rukn-admin-skeleton mt-3 h-2 w-[38%]" />

        <div className="rukn-products-v2-skeleton-metrics">
          <div className="rukn-admin-skeleton h-10" />
          <div className="rukn-admin-skeleton h-10" />
          <div className="rukn-admin-skeleton h-10" />
        </div>

        <div className="rukn-admin-skeleton mt-4 h-8 w-full" />
      </div>
    </article>
  );
}
export function AdminProductsPage() {
  const tenantId =
    getCurrentTenantId();

  const [products, setProducts] =
    useState<Product[]>([]);

  const [categories, setCategories] =
    useState<AdminCategory[]>([]);

  const [
    productImageStates,
    setProductImageStates,
  ] =
    useState<
      Record<
        string,
        ProductImageState
      >
    >({});

  const [loading, setLoading] =
    useState(true);
  const [
    salesByProduct,
    setSalesByProduct,
  ] =
    useState<
      Record<
        string,
        MerchantDashboardTopProduct
      >
    >({});

  const [creating, setCreating] =
    useState(false);

  const [
    changingProductId,
    setChangingProductId,
  ] =
    useState<string | null>(
      null,
    );

  const [
    editingProduct,
    setEditingProduct,
  ] =
    useState<Product | null>(
      null,
    );

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

  const [
    attention,
    setAttention,
  ] =
    useState<AttentionFilter>(
      "all",
    );

  const [
    categoryFilter,
    setCategoryFilter,
  ] =
    useState("all");

  const [sortMode, setSortMode] =
    useState<SortMode>(
      "newest",
    );

  const [
    openActionsId,
    setOpenActionsId,
  ] =
    useState<string | null>(
      null,
    );

  const [error, setError] =
    useState<{
      message: string;
      status: number | null;
    } | null>(null);

  const loadProductImage =
    useCallback(
      async (
        productId: string,
      ) => {
        if (!tenantId) {
          return;
        }

        setProductImageStates(
          (current) => ({
            ...current,
            [productId]: {
              state: "loading",
            },
          }),
        );

        try {
          const result =
            await getProductImages(
              tenantId,
              productId,
            );

          const sorted =
            [...result.images].sort(
              (a, b) => {
                if (
                  a.isPrimary !==
                  b.isPrimary
                ) {
                  return a.isPrimary
                    ? -1
                    : 1;
                }

                return (
                  a.sortOrder -
                  b.sortOrder
                );
              },
            );

          setProductImageStates(
            (current) => ({
              ...current,
              [productId]: {
                state: "ready",
                image:
                  sorted[0] ??
                  null,
              },
            }),
          );
        } catch {
          setProductImageStates(
            (current) => ({
              ...current,
              [productId]: {
                state: "error",
              },
            }),
          );
        }
      },
      [tenantId],
    );

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
          try {
            const dashboard =
              await getMerchantDashboardSummary(
                tenantId,
                8,
              );

            setSalesByProduct(
              Object.fromEntries(
                dashboard.topProducts.map(
                  (item) => [
                    item.productId,
                    item,
                  ],
                ),
              ),
            );
          } catch {
            setSalesByProduct({});
          }

          const initialImages =
            Object.fromEntries(
              productResult.map(
                (product) => [
                  product.productId,
                  {
                    state:
                      "loading",
                  } satisfies ProductImageState,
                ],
              ),
            );

          setProductImageStates(
            initialImages,
          );

          void Promise.all(
            productResult.map(
              (product) =>
                loadProductImage(
                  product.productId,
                ),
            ),
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
      [
        tenantId,
        loadProductImage,
      ],
    );

  useEffect(() => {
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
  }, [loadProducts]);

  const categoryById =
    useMemo(
      () =>
        new Map(
          categories.map(
            (category) => [
              category.categoryId,
              category,
            ],
          ),
        ),
      [categories],
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

  const hiddenCount =
    products.filter(
      (product) =>
        !product.isVisible,
    ).length;

  const lowStockCount =
    products.filter(
      (product) => {
        const inventory =
          inventoryMeta(
            product,
          );

        return (
          inventory.tone ===
            "low" ||
          inventory.tone ===
            "out"
        );
      },
    ).length;

  const noImageCount =
    products.filter(
      (product) => {
        const imageState =
          productImageStates[
            product.productId
          ];

        return (
          imageState?.state ===
            "ready" &&
          imageState.image ===
            null
        );
      },
    ).length;

  const needsAttentionCount =
    products.filter(
      (product) => {
        const inventory =
          inventoryMeta(
            product,
          );

        const imageState =
          productImageStates[
            product.productId
          ];

        return (
          normalizeStatus(
            product.status,
          ) === "draft" ||
          !product.isVisible ||
          inventory.tone ===
            "low" ||
          inventory.tone ===
            "out" ||
          (
            imageState?.state ===
              "ready" &&
            imageState.image ===
              null
          )
        );
      },
    ).length;

  const filtered =
    useMemo(() => {
      const normalizedQuery =
        query
          .trim()
          .toLowerCase();

      const next =
        products.filter(
          (product) => {
            const productStatus =
              normalizeStatus(
                product.status,
              );

            const matchesStatus =
              status === "all" ||
              productStatus ===
                status;

            if (!matchesStatus) {
              return false;
            }

            if (
              categoryFilter !==
                "all" &&
              product.categoryId !==
                categoryFilter
            ) {
              return false;
            }

            const inventory =
              inventoryMeta(
                product,
              );

            const imageState =
              productImageStates[
                product.productId
              ];

            if (
              attention ===
                "low-stock" &&
              inventory.tone !==
                "low" &&
              inventory.tone !==
                "out"
            ) {
              return false;
            }

            if (
              attention ===
                "hidden" &&
              product.isVisible
            ) {
              return false;
            }

            if (
              attention ===
                "no-image" &&
              !(
                imageState?.state ===
                  "ready" &&
                imageState.image ===
                  null
              )
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

            const categoryName =
              product.categoryId
                ? categoryById.get(
                    product.categoryId,
                  )?.name ?? ""
                : "";

            return [
              product.name,
              product.slug,
              variant?.sku ?? "",
              categoryName,
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

      next.sort(
        (a, b) => {
          if (
            sortMode ===
            "name"
          ) {
            return a.name.localeCompare(
              b.name,
              "ar",
            );
          }

          if (
            sortMode ===
            "price-high"
          ) {
            return (
              b.price -
              a.price
            );
          }

          if (
            sortMode ===
            "stock-low"
          ) {
            const aStock =
              inventoryMeta(
                a,
              ).quantity ??
              Number.MAX_SAFE_INTEGER;

            const bStock =
              inventoryMeta(
                b,
              ).quantity ??
              Number.MAX_SAFE_INTEGER;

            return (
              aStock -
              bStock
            );
          }

          return (
            new Date(
              b.createdAtUtc,
            ).getTime() -
            new Date(
              a.createdAtUtc,
            ).getTime()
          );
        },
      );

      return next;
    }, [
      products,
      query,
      status,
      attention,
      categoryFilter,
      sortMode,
      categoryById,
      productImageStates,
    ]);

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

          void loadProductImage(
            created.productId,
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

      void loadProductImage(
        finalProduct.productId,
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

    setSavingEdit(true);

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
      setSavingEdit(false);
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
    if (!tenantId) {
      return;
    }

    setChangingProductId(
      product.productId,
    );

    setOpenActionsId(null);
    setError(null);

    try {
      let updated: Product;

      if (
        action === "publish"
      ) {
        updated =
          await publishProduct(
            tenantId,
            product.productId,
          );
      } else if (
        action === "draft"
      ) {
        updated =
          await moveProductToDraft(
            tenantId,
            product.productId,
          );
      } else if (
        action === "archive"
      ) {
        updated =
          await archiveProduct(
            tenantId,
            product.productId,
          );
      } else if (
        action === "show"
      ) {
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
          exception instanceof
          ProductsApiError
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
      className="rukn-products-pro rukn-products-v4 mx-auto max-w-[1500px]"
    >
      <section className="rukn-products-hero">
        <div className="relative z-10 flex flex-col gap-6 xl:flex-row xl:items-end xl:justify-between">
          <div className="max-w-[760px]">
            <div className="rukn-products-eyebrow">
              <Sparkles size={13} />
              مركز إدارة الكتالوج
            </div>

            <h1 className="mt-4 text-[32px] font-semibold tracking-[-0.045em] md:text-[38px]">
              المنتجات
            </h1>

            <p className="rukn-products-muted mt-3 max-w-[690px] text-[12px] leading-7 md:text-[13px]">
              راقب حالة منتجاتك وصورها وأسعارها ومخزونها من شاشة واحدة،
              ووصل مباشرة لما يحتاج تدخل بدل البحث داخل عشرات الصفوف.
            </p>

            <div className="mt-5 flex flex-wrap items-center gap-2">
              <span className="rukn-products-insight">
                <CircleAlert size={13} />
                {needsAttentionCount} يحتاج انتباه
              </span>

              <span className="rukn-products-insight">
                <Warehouse size={13} />
                {lowStockCount} مخزون منخفض
              </span>

              <span className="rukn-products-insight">
                <EyeOff size={13} />
                {hiddenCount} غير ظاهر
              </span>
            </div>
          </div>

          <button
            type="button"
            onClick={() =>
              setCreateOpen(true)
            }
            className="rukn-products-primary-action"
          >
            <PackagePlus size={17} />
            إضافة منتج
          </button>
        </div>
      </section>

      <section className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <button
          type="button"
          onClick={() => {
            setStatus("all");
            setAttention("all");
          }}
          className="rukn-products-metric"
        >
          <span className="rukn-products-metric-icon">
            <Layers3 size={16} />
          </span>

          <span>
            <span className="rukn-products-metric-label">
              كل المنتجات
            </span>

            <strong>
              {products.length}
            </strong>

            <small>
              إجمالي الكتالوج الحالي
            </small>
          </span>
        </button>

        <button
          type="button"
          onClick={() =>
            setStatus(
              "published",
            )
          }
          className="rukn-products-metric"
        >
          <span className="rukn-products-metric-icon is-success">
            <CheckCircle2 size={16} />
          </span>

          <span>
            <span className="rukn-products-metric-label">
              منشورة
            </span>

            <strong>
              {publishedCount}
            </strong>

            <small>
              جاهزة للبيع حاليًا
            </small>
          </span>
        </button>

        <button
          type="button"
          onClick={() => {
            setAttention(
              "low-stock",
            );
            setStatus("all");
          }}
          className="rukn-products-metric"
        >
          <span className="rukn-products-metric-icon is-warning">
            <Warehouse size={16} />
          </span>

          <span>
            <span className="rukn-products-metric-label">
              المخزون
            </span>

            <strong>
              {lowStockCount}
            </strong>

            <small>
              يحتاج تدخل قريب
            </small>
          </span>
        </button>

        <button
          type="button"
          onClick={() => {
            setStatus("draft");
            setAttention("all");
          }}
          className="rukn-products-metric"
        >
          <span className="rukn-products-metric-icon is-neutral">
            <Pencil size={16} />
          </span>

          <span>
            <span className="rukn-products-metric-label">
              المسودات
            </span>

            <strong>
              {draftCount}
            </strong>

            <small>
              لم تُنشر بعد
            </small>
          </span>
        </button>
      </section>

      <section className="rukn-products-workspace mt-5">
        <div className="rukn-products-toolbar">
          <div className="relative min-w-0 flex-1">
            <Search
              size={16}
              className="rukn-products-search-icon"
            />

            <input
              value={query}
              onChange={(event) =>
                setQuery(
                  event.target.value,
                )
              }
              className="rukn-products-search"
              placeholder="ابحث بالاسم، SKU، الرابط أو القسم..."
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
                  "published",
                  "منشور",
                ],
                [
                  "draft",
                  "مسودة",
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
                    "rukn-products-filter-chip",
                    status === value
                      ? "is-active"
                      : "",
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
              className="rukn-products-icon-action"
              aria-label="تحديث المنتجات"
              title="تحديث"
            >
              <RefreshCw size={15} />
            </button>
          </div>
        </div>

        <div className="rukn-products-secondary-toolbar">
          <div className="flex flex-wrap items-center gap-2">
            <span className="rukn-products-toolbar-label">
              <SlidersHorizontal size={13} />
              تصفية سريعة
            </span>

            <button
              type="button"
              onClick={() =>
                setAttention(
                  "all",
                )
              }
              className={[
                "rukn-products-mini-filter",
                attention === "all"
                  ? "is-active"
                  : "",
              ].join(" ")}
            >
              الكل
            </button>

            <button
              type="button"
              onClick={() =>
                setAttention(
                  "low-stock",
                )
              }
              className={[
                "rukn-products-mini-filter",
                attention === "low-stock"
                  ? "is-active"
                  : "",
              ].join(" ")}
            >
              مخزون منخفض
              {lowStockCount > 0 ? (
                <span>
                  {lowStockCount}
                </span>
              ) : null}
            </button>

            <button
              type="button"
              onClick={() =>
                setAttention(
                  "hidden",
                )
              }
              className={[
                "rukn-products-mini-filter",
                attention === "hidden"
                  ? "is-active"
                  : "",
              ].join(" ")}
            >
              غير ظاهر
              {hiddenCount > 0 ? (
                <span>
                  {hiddenCount}
                </span>
              ) : null}
            </button>

            <button
              type="button"
              onClick={() =>
                setAttention(
                  "no-image",
                )
              }
              className={[
                "rukn-products-mini-filter",
                attention === "no-image"
                  ? "is-active"
                  : "",
              ].join(" ")}
            >
              بدون صورة
              {noImageCount > 0 ? (
                <span>
                  {noImageCount}
                </span>
              ) : null}
            </button>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <select
              value={
                categoryFilter
              }
              onChange={(event) =>
                setCategoryFilter(
                  event.target.value,
                )
              }
              className="rukn-products-select"
              aria-label="فلترة حسب القسم"
            >
              <option value="all">
                كل الأقسام
              </option>

              {categories.map(
                (category) => (
                  <option
                    key={
                      category.categoryId
                    }
                    value={
                      category.categoryId
                    }
                  >
                    {category.name}
                  </option>
                ),
              )}
            </select>

            <label className="rukn-products-sort-wrap">
              <ArrowDownUp size={13} />

              <select
                value={sortMode}
                onChange={(event) =>
                  setSortMode(
                    event.target
                      .value as
                      SortMode,
                  )
                }
                className="rukn-products-sort"
                aria-label="ترتيب المنتجات"
              >
                <option value="newest">
                  الأحدث
                </option>

                <option value="name">
                  الاسم
                </option>

                <option value="price-high">
                  السعر الأعلى
                </option>

                <option value="stock-low">
                  الأقل مخزونًا
                </option>
              </select>
            </label>
          </div>
        </div>

        <div className="rukn-products-result-bar">
          <span>
            عرض
            {" "}
            <strong>
              {filtered.length}
            </strong>
            {" "}
            من
            {" "}
            {products.length}
            {" "}
            منتج
          </span>

          {attention !== "all" ||
          status !== "all" ||
          categoryFilter !==
            "all" ||
          query ? (
            <button
              type="button"
              onClick={() => {
                setQuery("");
                setStatus("all");
                setAttention("all");
                setCategoryFilter(
                  "all",
                );
              }}
            >
              مسح الفلاتر
            </button>
          ) : null}
        </div>

        {loading ? (
          <div className="p-3">
            {Array.from({
              length: 6,
            }).map((_, index) => (
              <ProductSkeleton
                key={index}
              />
            ))}
          </div>
        ) : error ? (
          <div className="rukn-products-state">
            <div className="rukn-products-state-icon is-warning">
              {error.status ===
              403 ? (
                <ShieldAlert
                  size={22}
                />
              ) : (
                <AlertTriangle
                  size={22}
                />
              )}
            </div>

            <h2>
              {error.status === 403
                ? "الحساب يحتاج إكمال الحماية"
                : "تعذر تحميل المنتجات"}
            </h2>

            <p>
              {error.message}
            </p>

            <button
              type="button"
              onClick={() =>
                void loadProducts()
              }
              className="rukn-products-secondary-action"
            >
              <RefreshCw size={14} />
              إعادة المحاولة
            </button>
          </div>
        ) : filtered.length ===
          0 ? (
          <div className="rukn-products-state">
            <div className="rukn-products-state-icon">
              <Boxes size={22} />
            </div>

            <h2>
              {products.length === 0
                ? "ابدأ أول منتج في متجرك"
                : "لا توجد نتائج بهذه الفلاتر"}
            </h2>

            <p>
              {products.length === 0
                ? "أضف المنتج ثم أكمل صوره ومخزونه ونشره من مكان واحد."
                : "جرّب مسح أحد الفلاتر أو البحث بكلمة أخرى."}
            </p>

            {products.length === 0 ? (
              <button
                type="button"
                onClick={() =>
                  setCreateOpen(true)
                }
                className="rukn-products-primary-action"
              >
                <PackagePlus
                  size={15}
                />
                إضافة أول منتج
              </button>
            ) : null}
          </div>
        ) : (
          <div className="rukn-products-v2-grid">
            {filtered.map(
              (product) => {
                const variant =
                  defaultVariant(
                    product,
                  );

                const category =
                  product.categoryId
                    ? categoryById.get(
                        product.categoryId,
                      )
                    : null;

                const inventory =
                  inventoryMeta(
                    product,
                  );

                const discount =
                  discountPercentage(
                    product,
                  );

                const imageState =
                  productImageStates[
                    product.productId
                  ];

                const image =
                  imageState?.state ===
                  "ready"
                    ? imageState.image
                    : null;

                const busy =
                  changingProductId ===
                    product.productId ||
                  savingEdit;

                const sales =
                  salesByProduct[
                    product.productId
                  ] ??
                  null;

                const productStatus =
                  normalizeStatus(
                    product.status,
                  );

                const demandUnits =
                  sales?.quantitySold ??
                  null;

                const demandLevel =
                  demandUnits === null
                    ? "unknown"
                    : demandUnits >= 10
                      ? "high"
                      : demandUnits >= 3
                        ? "medium"
                        : "low";

                const demandLabel =
                  demandLevel === "high"
                    ? "إقبال مرتفع"
                    : demandLevel === "medium"
                      ? "إقبال مقبول"
                      : demandLevel === "low"
                        ? "إقبال منخفض"
                        : "غير مصنف";

                return (
                  <article
                    key={product.productId}
                    className="rukn-products-v2-card rukn-products-v43-card"
                  >
                    <div className="rukn-products-v2-media rukn-products-v43-media">
                      {imageState?.state ===
                      "loading" ? (
                        <div className="rukn-admin-skeleton absolute inset-0" />
                      ) : null}

                      {image ? (
                        <img
                          src={image.url}
                          alt={
                            image.altText ??
                            product.name
                          }
                          loading="lazy"
                          onError={(
                            event,
                          ) => {
                            event.currentTarget.style.display =
                              "none";
                          }}
                        />
                      ) : (
                        <div className="rukn-products-v43-empty">
                          <span>
                            <ImageIcon
                              size={23}
                            />
                          </span>

                          <strong>
                            بدون صورة
                          </strong>

                          <small>
                            أضف صورة لعرض أفضل
                          </small>
                        </div>
                      )}

                      <div className="rukn-products-v43-badges">
                        <span
                          className={[
                            "rukn-products-v43-status",
                            statusClass(
                              product.status,
                            ),
                          ].join(" ")}
                        >
                          <i />

                          {statusLabel(
                            product.status,
                          )}

                          {productStatus ===
                            "published" &&
                          !product.isVisible
                            ? " · مخفي"
                            : ""}
                        </span>

                        <span
                          className="rukn-products-v43-demand"
                          data-level={
                            demandLevel
                          }
                          title="مؤشر مبني على بيانات الوحدات المباعة المتاحة"
                        >
                          <i />

                          {demandLabel}
                        </span>
                      </div>

                      {discount !== null ? (
                        <span className="rukn-products-v43-discount">
                          -
                          {discount}
                          %
                        </span>
                      ) : null}
                    </div>

                    <div className="rukn-products-v43-body">
                      <div className="rukn-products-v43-head">
                        <div className="rukn-products-v43-name">
                          <span>
                            {category?.name ??
                              "بدون قسم"}
                          </span>

                          <h3>
                            {product.name}
                          </h3>

                          <p dir="ltr">
                            <small>
                              SKU
                            </small>

                            {variant?.sku ??
                              "—"}
                          </p>
                        </div>

                        <div className="rukn-products-v43-price">
                          <strong dir="ltr">
                            {formatMoney(
                              product.price,
                              product.currency,
                            )}
                          </strong>

                          {product.compareAtPrice !==
                          null ? (
                            <span dir="ltr">
                              {formatMoney(
                                product.compareAtPrice,
                                product.currency,
                              )}
                            </span>
                          ) : null}
                        </div>
                      </div>

                      <div
                        className={[
                          "rukn-products-v43-stock",
                          inventory.tone,
                        ].join(" ")}
                      >
                        <span className="rukn-products-v43-stock-icon">
                          <PackageCheck
                            size={13}
                          />
                        </span>

                        <strong>
                          {inventory.tracked
                            ? `${inventory.quantity} وحدة`
                            : "غير متتبع"}
                        </strong>

                        <small>
                          {inventory.label}
                        </small>
                      </div>

                      <div className="rukn-products-v43-performance">
                        <div>
                          <span>
                            مباع
                          </span>

                          <strong>
                            {sales
                              ? sales.quantitySold
                              : "—"}
                          </strong>
                        </div>

                        <div>
                          <span>
                            المبيعات
                          </span>

                          <strong dir="ltr">
                            {sales
                              ? formatMoney(
                                  sales.capturedSales,
                                  sales.currency,
                                )
                              : "—"}
                          </strong>
                        </div>

                        <div>
                          <span>
                            الخيارات
                          </span>

                          <strong>
                            {product.variants.length}
                          </strong>
                        </div>
                      </div>

                      <div className="rukn-products-v43-meta">
                        <span>
                          أضيف
                        </span>

                        <strong>
                          {formatDate(
                            product.createdAtUtc,
                          )}
                        </strong>
                      </div>

                      <div className="rukn-products-v43-actions">
                        <button
                          type="button"
                          disabled={busy}
                          onClick={() =>
                            setEditingProduct(
                              product,
                            )
                          }
                          className="rukn-products-v43-edit"
                        >
                          <Pencil
                            size={14}
                          />

                          تعديل المنتج
                        </button>

                        {productStatus !==
                        "published" ? (
                          <button
                            type="button"
                            disabled={busy}
                            onClick={() =>
                              void changeState(
                                product,
                                "publish",
                              )
                            }
                            className="rukn-products-v43-quick"
                          >
                            <PackageCheck
                              size={14}
                            />

                            نشر
                          </button>
                        ) : !product.isVisible ? (
                          <button
                            type="button"
                            disabled={busy}
                            onClick={() =>
                              void changeState(
                                product,
                                "show",
                              )
                            }
                            className="rukn-products-v43-quick"
                          >
                            <Eye
                              size={14}
                            />

                            إظهار
                          </button>
                        ) : null}

                        <div className="rukn-products-v43-more-wrap">
                          <button
                            type="button"
                            disabled={busy}
                            onClick={() =>
                              setOpenActionsId(
                                (
                                  current,
                                ) =>
                                  current ===
                                  product.productId
                                    ? null
                                    : product.productId,
                              )
                            }
                            className="rukn-products-v43-more"
                            aria-label="إجراءات إضافية"
                            title="إجراءات إضافية"
                          >
                            <Ellipsis
                              size={17}
                            />
                          </button>

                          {openActionsId ===
                          product.productId ? (
                            <div className="rukn-products-actions-menu rukn-products-v43-menu">
                              {productStatus ===
                              "published" ? (
                                <button
                                  type="button"
                                  onClick={() =>
                                    void changeState(
                                      product,
                                      "draft",
                                    )
                                  }
                                >
                                  <RotateCcw
                                    size={13}
                                  />

                                  إرجاع لمسودة
                                </button>
                              ) : null}

                              {productStatus ===
                              "published" ? (
                                <button
                                  type="button"
                                  onClick={() =>
                                    void changeState(
                                      product,
                                      product.isVisible
                                        ? "hide"
                                        : "show",
                                    )
                                  }
                                >
                                  {product.isVisible ? (
                                    <EyeOff
                                      size={13}
                                    />
                                  ) : (
                                    <Eye
                                      size={13}
                                    />
                                  )}

                                  {product.isVisible
                                    ? "إخفاء من المتجر"
                                    : "إظهار في المتجر"}
                                </button>
                              ) : null}

                              {productStatus !==
                              "archived" ? (
                                <button
                                  type="button"
                                  onClick={() =>
                                    void changeState(
                                      product,
                                      "archive",
                                    )
                                  }
                                >
                                  <Archive
                                    size={13}
                                  />

                                  أرشفة المنتج
                                </button>
                              ) : null}
                            </div>
                          ) : null}
                        </div>
                      </div>
                    </div>
                  </article>
                );
              },
            )}
          </div>
        )}
      </section>

      <CreateProductDialog
        open={createOpen}
        busy={creating}
        categories={categories}
        onClose={() =>
          setCreateOpen(false)
        }
        onCreate={handleCreate}
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
        product={editingProduct}
        categories={categories}
        onClose={() =>
          setEditingProduct(null)
        }
        onSave={handleEdit}
      />
    </div>
  );
}