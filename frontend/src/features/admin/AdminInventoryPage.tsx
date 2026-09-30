import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  Archive,
  Boxes,
  Check,
  ChevronDown,
  ChevronUp,
  MapPin,
  PackagePlus,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Settings2,
  SlidersHorizontal,
  Warehouse,
  X,
} from "lucide-react";

import {
  authorizedApiFetch,
} from "../auth/authSession";

import {
  AdminPickupSetup,
} from "./AdminPickupSetup";

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
  publishProduct,
  setPrimaryProductImage,
  updateProduct,
  updateProductInventory,
  type CreateProductInput,
  type Product,
  type UpdateProductInput,
} from "./products/productsApi";

import "./AdminInventoryV2.css";

type Location = {
  id: string;
  code: string;
  name: string;
  phone: string | null;
  countryCode: string;
  city: string;
  region: string | null;
  line1: string;
  line2: string | null;
  isDefault: boolean;
  isActive: boolean;
};

type LocationForm =
  Omit<
    Location,
    "id"
  >;

type InventoryFilter =
  | "all"
  | "low"
  | "out"
  | "untracked";

type StockEditor = {
  productId: string;
  quantity: string;
  lowStockThreshold: string;
  trackInventory: boolean;
  continueSellingWhenOutOfStock: boolean;
};

const emptyLocation =
  (): LocationForm => ({
    code: "",
    name: "",
    phone: null,
    countryCode: "SA",
    city: "",
    region: null,
    line1: "",
    line2: null,
    isDefault: false,
    isActive: true,
  });

function normalizeStatus(
  value: string,
) {
  return value
    .trim()
    .toLowerCase();
}

function formatNumber(
  value: number,
) {
  return new Intl.NumberFormat(
    "en-US",
    {
      maximumFractionDigits: 0,
    },
  ).format(value);
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

function productInventory(
  product: Product,
) {
  const tracked =
    product.variants.filter(
      (variant) =>
        variant.trackInventory,
    );

  const quantity =
    tracked.reduce(
      (
        total,
        variant,
      ) =>
        total +
        variant.quantity,
      0,
    );

  const low =
    tracked.some(
      (variant) =>
        variant.quantity <=
        variant.lowStockThreshold,
    );

  const out =
    tracked.some(
      (variant) =>
        variant.quantity <= 0,
    );

  return {
    tracked,
    quantity,
    low,
    out,
    untracked:
      tracked.length === 0,
  };
}

async function responseError(
  response: Response,
) {
  try {
    const body =
      (await response.json()) as {
        message?: string;
        title?: string;
      };

    return (
      body.message ||
      body.title ||
      `HTTP ${response.status}`
    );
  } catch {
    return `تعذر تنفيذ العملية (${response.status}).`;
  }
}

export function AdminInventoryPage() {
  const tenantId =
    getCurrentTenantId();

  const [
    locations,
    setLocations,
  ] =
    useState<Location[]>([]);

  const [
    products,
    setProducts,
  ] =
    useState<Product[]>([]);

  const [
    categories,
    setCategories,
  ] =
    useState<
      AdminCategory[]
    >([]);

  const [
    loading,
    setLoading,
  ] =
    useState(
      Boolean(
        tenantId,
      ),
    );

  const [
    error,
    setError,
  ] =
    useState("");

  const [
    notice,
    setNotice,
  ] =
    useState("");

  const [
    query,
    setQuery,
  ] =
    useState("");

  const [
    filter,
    setFilter,
  ] =
    useState<InventoryFilter>(
      () => {
        const value =
          new URLSearchParams(
            window.location.search,
          ).get(
            "filter",
          );

        return value ===
          "low-stock"
          ? "low"
          : "all";
      },
    );

  const [
    createOpen,
    setCreateOpen,
  ] =
    useState(false);

  const [
    creating,
    setCreating,
  ] =
    useState(false);

  const [
    editingProduct,
    setEditingProduct,
  ] =
    useState<Product | null>(
      null,
    );

  const [
    savingProduct,
    setSavingProduct,
  ] =
    useState(false);

  const [
    stockEditor,
    setStockEditor,
  ] =
    useState<StockEditor | null>(
      null,
    );

  const [
    savingStock,
    setSavingStock,
  ] =
    useState(false);

  const [
    archivingProductId,
    setArchivingProductId,
  ] =
    useState<string | null>(
      null,
    );

  const [
    locationsExpanded,
    setLocationsExpanded,
  ] =
    useState(false);

  const [
    pickupExpanded,
    setPickupExpanded,
  ] =
    useState(false);

  const [
    locationFormOpen,
    setLocationFormOpen,
  ] =
    useState(false);

  const [
    editingLocationId,
    setEditingLocationId,
  ] =
    useState<string | null>(
      null,
    );

  const [
    locationForm,
    setLocationForm,
  ] =
    useState<LocationForm>(
      emptyLocation,
    );

  const [
    savingLocation,
    setSavingLocation,
  ] =
    useState(false);

  const locationsUrl =
    `/api/tenants/${encodeURIComponent(
      tenantId ?? "",
    )}/backoffice/fulfillment/locations`;

  const refresh =
    useCallback(
      async () => {
        if (!tenantId) {
          setLoading(
            false,
          );

          setError(
            "لم يتم تحديد المتجر الحالي.",
          );

          return;
        }

        setLoading(
          true,
        );

        setError("");

        try {
          const [
            locationResponse,
            productResult,
            categoryResult,
          ] =
            await Promise.all([
              authorizedApiFetch(
                `/api/tenants/${encodeURIComponent(
                  tenantId,
                )}/backoffice/fulfillment/locations`,
              ),

              getProducts(
                tenantId,
              ),

              getCategories(
                tenantId,
              ),
            ]);

          if (
            !locationResponse.ok
          ) {
            throw new Error(
              await responseError(
                locationResponse,
              ),
            );
          }

          setLocations(
            (
              await locationResponse.json()
            ) as Location[],
          );

          setProducts(
            productResult,
          );

          setCategories(
            categoryResult,
          );
        } catch (
          caught
        ) {
          setError(
            caught instanceof Error
              ? caught.message
              : "تعذر تحميل المخزون.",
          );
        } finally {
          setLoading(
            false,
          );
        }
      },
      [
        tenantId,
      ],
    );

  useEffect(
    () => {
      const timer =
        window.setTimeout(
          () => {
            void refresh();
          },
          0,
        );

      return () => {
        window.clearTimeout(
          timer,
        );
      };
    },
    [
      refresh,
    ],
  );

  const activeProducts =
    useMemo(
      () =>
        products.filter(
          (product) =>
            normalizeStatus(
              product.status,
            ) !==
            "archived",
        ),
      [
        products,
      ],
    );

  const metrics =
    useMemo(
      () => {
        let totalUnits =
          0;

        let low =
          0;

        let out =
          0;

        let untracked =
          0;

        for (
          const product
          of activeProducts
        ) {
          const inventory =
            productInventory(
              product,
            );

          totalUnits +=
            inventory.quantity;

          if (
            inventory.low
          ) {
            low +=
              1;
          }

          if (
            inventory.out
          ) {
            out +=
              1;
          }

          if (
            inventory.untracked
          ) {
            untracked +=
              1;
          }
        }

        return {
          products:
            activeProducts.length,
          totalUnits,
          low,
          out,
          untracked,
        };
      },
      [
        activeProducts,
      ],
    );

  const filteredProducts =
    useMemo(
      () => {
        const needle =
          query
            .trim()
            .toLowerCase();

        return activeProducts
          .filter(
            (
              product,
            ) => {
              const inventory =
                productInventory(
                  product,
                );

              if (
                filter ===
                  "low" &&
                !inventory.low
              ) {
                return false;
              }

              if (
                filter ===
                  "out" &&
                !inventory.out
              ) {
                return false;
              }

              if (
                filter ===
                  "untracked" &&
                !inventory.untracked
              ) {
                return false;
              }

              if (
                !needle
              ) {
                return true;
              }

              return [
                product.name,
                product.slug,
                ...product.variants.flatMap(
                  (
                    variant,
                  ) => [
                    variant.name,
                    variant.sku,
                  ],
                ),
              ].some(
                (
                  value,
                ) =>
                  value
                    .toLowerCase()
                    .includes(
                      needle,
                    ),
              );
            },
          )
          .sort(
            (
              a,
              b,
            ) => {
              const aInventory =
                productInventory(
                  a,
                );

              const bInventory =
                productInventory(
                  b,
                );

              if (
                aInventory.out !==
                bInventory.out
              ) {
                return aInventory.out
                  ? -1
                  : 1;
              }

              if (
                aInventory.low !==
                bInventory.low
              ) {
                return aInventory.low
                  ? -1
                  : 1;
              }

              return a.name.localeCompare(
                b.name,
                "ar",
              );
            },
          );
      },
      [
        activeProducts,
        filter,
        query,
      ],
    );

  async function handleCreate(
    input:
      CreateProductInput,
  ) {
    if (!tenantId) {
      throw new Error(
        "لم يتم تحديد المتجر الحالي.",
      );
    }

    setCreating(
      true,
    );

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
        finalProduct =
          await publishProduct(
            tenantId,
            created.productId,
          );
      }

      setProducts(
        (
          current,
        ) => [
          finalProduct,
          ...current.filter(
            (
              item,
            ) =>
              item.productId !==
              finalProduct.productId,
          ),
        ],
      );

      setNotice(
        "تمت إضافة المنتج إلى المخزون.",
      );

      return finalProduct;
    } finally {
      setCreating(
        false,
      );
    }
  }

  async function handleProductEdit(
    product: Product,
    input:
      UpdateProductInput,
  ) {
    if (!tenantId) {
      throw new Error(
        "لم يتم تحديد المتجر الحالي.",
      );
    }

    setSavingProduct(
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

      await refresh();

      setNotice(
        "تم حفظ تعديلات المنتج.",
      );
    } finally {
      setSavingProduct(
        false,
      );
    }
  }

  function openStockEditor(
    product: Product,
  ) {
    const variant =
      defaultVariant(
        product,
      );

    if (!variant) {
      setError(
        "هذا المنتج لا يحتوي متغيرًا يمكن تعديل مخزونه.",
      );

      return;
    }

    setError("");
    setNotice("");

    setStockEditor({
      productId:
        product.productId,

      quantity:
        String(
          variant.quantity,
        ),

      lowStockThreshold:
        String(
          variant.lowStockThreshold,
        ),

      trackInventory:
        variant.trackInventory,

      continueSellingWhenOutOfStock:
        variant.continueSellingWhenOutOfStock,
    });
  }

  async function saveStock(
    product: Product,
  ) {
    if (
      !tenantId ||
      !stockEditor ||
      stockEditor.productId !==
        product.productId
    ) {
      return;
    }

    const quantity =
      Number(
        stockEditor.quantity,
      );

    const threshold =
      Number(
        stockEditor.lowStockThreshold,
      );

    if (
      stockEditor.trackInventory &&
      (
        !Number.isInteger(
          quantity,
        ) ||
        quantity < 0
      )
    ) {
      setError(
        "الكمية يجب أن تكون رقمًا صحيحًا يساوي صفر أو أكثر.",
      );

      return;
    }

    if (
      stockEditor.trackInventory &&
      (
        !Number.isInteger(
          threshold,
        ) ||
        threshold < 0
      )
    ) {
      setError(
        "حد التنبيه يجب أن يكون رقمًا صحيحًا يساوي صفر أو أكثر.",
      );

      return;
    }

    setSavingStock(
      true,
    );

    setError("");

    try {
      await updateProductInventory(
        tenantId,
        product.productId,
        {
          trackInventory:
            stockEditor.trackInventory,

          quantity:
            stockEditor.trackInventory
              ? quantity
              : 0,

          lowStockThreshold:
            stockEditor.trackInventory
              ? threshold
              : 0,

          continueSellingWhenOutOfStock:
            stockEditor.continueSellingWhenOutOfStock,
        },
      );

      setStockEditor(
        null,
      );

      setNotice(
        "تم تحديث المخزون.",
      );

      await refresh();
    } catch (
      caught
    ) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر تحديث المخزون.",
      );
    } finally {
      setSavingStock(
        false,
      );
    }
  }

  async function archive(
    product: Product,
  ) {
    if (!tenantId) {
      return;
    }

    const confirmed =
      window.confirm(
        `أرشفة "${product.name}"؟ سيختفي من المخزون النشط ويمكن الاحتفاظ بسجلات الطلبات القديمة بأمان.`,
      );

    if (!confirmed) {
      return;
    }

    setArchivingProductId(
      product.productId,
    );

    setError("");

    try {
      await archiveProduct(
        tenantId,
        product.productId,
      );

      setProducts(
        (
          current,
        ) =>
          current.filter(
            (
              item,
            ) =>
              item.productId !==
              product.productId,
          ),
      );

      setNotice(
        "تمت أرشفة المنتج.",
      );
    } catch (
      caught
    ) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر أرشفة المنتج.",
      );
    } finally {
      setArchivingProductId(
        null,
      );
    }
  }

  function openCreateLocation() {
    setEditingLocationId(
      null,
    );

    setLocationForm(
      emptyLocation(),
    );

    setLocationFormOpen(
      true,
    );

    setError("");
  }

  function openEditLocation(
    location: Location,
  ) {
    const {
      id,
      ...values
    } =
      location;

    setEditingLocationId(
      id,
    );

    setLocationForm(
      values,
    );

    setLocationFormOpen(
      true,
    );

    setError("");
  }

  async function saveLocation() {
    if (
      savingLocation ||
      !tenantId
    ) {
      return;
    }

    if (
      !locationForm.code.trim() ||
      !locationForm.name.trim() ||
      !locationForm.city.trim() ||
      !locationForm.line1.trim() ||
      !/^[A-Za-z]{2}$/.test(
        locationForm.countryCode,
      )
    ) {
      setError(
        "أكمل اسم الموقع ورمزه والمدينة والعنوان ورمز الدولة.",
      );

      return;
    }

    setSavingLocation(
      true,
    );

    setError("");

    try {
      const response =
        await authorizedApiFetch(
          editingLocationId
            ? `${locationsUrl}/${encodeURIComponent(
                editingLocationId,
              )}`
            : locationsUrl,
          {
            method:
              editingLocationId
                ? "PUT"
                : "POST",

            body:
              JSON.stringify({
                ...locationForm,

                code:
                  locationForm.code
                    .trim(),

                name:
                  locationForm.name
                    .trim(),

                countryCode:
                  locationForm.countryCode
                    .trim()
                    .toUpperCase(),

                city:
                  locationForm.city
                    .trim(),

                line1:
                  locationForm.line1
                    .trim(),
              }),
          },
        );

      if (
        !response.ok
      ) {
        throw new Error(
          await responseError(
            response,
          ),
        );
      }

      setLocationFormOpen(
        false,
      );

      setNotice(
        "تم حفظ موقع التخزين.",
      );

      await refresh();
    } catch (
      caught
    ) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر حفظ الموقع.",
      );
    } finally {
      setSavingLocation(
        false,
      );
    }
  }

  return (
    <div
      dir="rtl"
      className="rukn-inv-v2"
    >
      <header className="rukn-inv-v2-head">
        <div>
          <span>
            إدارة المتجر
          </span>

          <h1>
            المخزون
          </h1>

          <p>
            راقب الكميات وعدّل المخزون والمنتجات من مكان واحد.
          </p>
        </div>

        <div className="rukn-inv-v2-head-actions">
          <button
            type="button"
            className="rukn-inv-v2-refresh"
            onClick={() =>
              void refresh()
            }
            disabled={
              loading
            }
          >
            <RefreshCw
              size={17}
              className={
                loading
                  ? "animate-spin"
                  : ""
              }
            />

            تحديث
          </button>

          <button
            type="button"
            className="rukn-inv-v2-create"
            onClick={() =>
              setCreateOpen(
                true,
              )
            }
          >
            <PackagePlus
              size={17}
            />

            إضافة منتج
          </button>
        </div>
      </header>

      {error ? (
        <div
          role="alert"
          className="rukn-inv-v2-alert error"
        >
          {error}
        </div>
      ) : null}

      {notice ? (
        <div
          role="status"
          className="rukn-inv-v2-alert success"
        >
          <Check
            size={15}
          />

          {notice}
        </div>
      ) : null}

      <section className="rukn-inv-v2-metrics">
        <button
          type="button"
          data-active={
            filter === "all"
          }
          onClick={() =>
            setFilter(
              "all",
            )
          }
        >
          <span>
            المنتجات
          </span>

          <strong>
            {formatNumber(
              metrics.products,
            )}
          </strong>

          <small>
            منتج نشط
          </small>
        </button>

        <div>
          <span>
            إجمالي الوحدات
          </span>

          <strong>
            {formatNumber(
              metrics.totalUnits,
            )}
          </strong>

          <small>
            في المخزون المتتبع
          </small>
        </div>

        <button
          type="button"
          data-tone="warning"
          data-active={
            filter === "low"
          }
          onClick={() =>
            setFilter(
              "low",
            )
          }
        >
          <span>
            منخفض
          </span>

          <strong>
            {formatNumber(
              metrics.low,
            )}
          </strong>

          <small>
            يحتاج انتباه
          </small>
        </button>

        <button
          type="button"
          data-tone="danger"
          data-active={
            filter === "out"
          }
          onClick={() =>
            setFilter(
              "out",
            )
          }
        >
          <span>
            نافد
          </span>

          <strong>
            {formatNumber(
              metrics.out,
            )}
          </strong>

          <small>
            كمية صفر
          </small>
        </button>
      </section>

      <section className="rukn-inv-v2-products">
        <div className="rukn-inv-v2-toolbar">
          <div className="rukn-inv-v2-search">
            <Search
              size={16}
            />

            <input
              value={
                query
              }
              onChange={(
                event,
              ) =>
                setQuery(
                  event.target.value,
                )
              }
              placeholder="ابحث باسم المنتج أو SKU..."
            />
          </div>

          <div className="rukn-inv-v2-filters">
            <SlidersHorizontal
              size={15}
            />

            {[
              {
                id:
                  "all" as const,
                label:
                  "الكل",
              },
              {
                id:
                  "low" as const,
                label:
                  "منخفض",
              },
              {
                id:
                  "out" as const,
                label:
                  "نافد",
              },
              {
                id:
                  "untracked" as const,
                label:
                  "غير متتبع",
              },
            ].map(
              (
                item,
              ) => (
                <button
                  key={
                    item.id
                  }
                  type="button"
                  className={
                    filter ===
                    item.id
                      ? "active"
                      : ""
                  }
                  onClick={() =>
                    setFilter(
                      item.id,
                    )
                  }
                >
                  {
                    item.label
                  }
                </button>
              ),
            )}
          </div>
        </div>

        <div className="rukn-inv-v2-table-head">
          <span>
            المنتج
          </span>

          <span>
            SKU
          </span>

          <span>
            المخزون
          </span>

          <span>
            حد التنبيه
          </span>

          <span>
            الحالة
          </span>

          <span>
            الإجراءات
          </span>
        </div>

        <div className="rukn-inv-v2-list">
          {loading ? (
            Array.from({
              length:
                5,
            }).map(
              (
                _,
                index,
              ) => (
                <div
                  key={
                    index
                  }
                  className="rukn-inv-v2-skeleton"
                />
              ),
            )
          ) : filteredProducts.length ===
            0 ? (
            <div className="rukn-inv-v2-empty">
              <Boxes
                size={24}
              />

              <strong>
                لا توجد منتجات مطابقة
              </strong>

              <p>
                غيّر البحث أو الفلتر، أو أضف منتجًا جديدًا.
              </p>

              <button
                type="button"
                onClick={() =>
                  setCreateOpen(
                    true,
                  )
                }
              >
                <Plus
                  size={15}
                />

                إضافة منتج
              </button>
            </div>
          ) : (
            filteredProducts.map(
              (
                product,
              ) => {
                const variant =
                  defaultVariant(
                    product,
                  );

                const inventory =
                  productInventory(
                    product,
                  );

                const editing =
                  stockEditor
                    ?.productId ===
                  product.productId;

                const busyArchive =
                  archivingProductId ===
                  product.productId;

                return (
                  <article
                    key={
                      product.productId
                    }
                    className="rukn-inv-v2-product"
                  >
                    <div className="rukn-inv-v2-row">
                      <div className="rukn-inv-v2-product-name">
                        <span className="rukn-inv-v2-product-icon">
                          <Boxes
                            size={17}
                          />
                        </span>

                        <div>
                          <strong>
                            {
                              product.name
                            }
                          </strong>

                          <small>
                            {product.variants.length >
                            1
                              ? `${formatNumber(
                                  product.variants.length,
                                )} خيارات`
                              : variant
                                  ?.name ||
                                "المنتج الرئيسي"}
                          </small>
                        </div>
                      </div>

                      <div
                        dir="ltr"
                        className="rukn-inv-v2-sku"
                      >
                        {variant
                          ?.sku ||
                          "—"}
                      </div>

                      <div className="rukn-inv-v2-quantity">
                        <strong>
                          {inventory.untracked
                            ? "—"
                            : formatNumber(
                                inventory.quantity,
                              )}
                        </strong>

                        <small>
                          {product.variants.length >
                            1
                            ? "إجمالي الخيارات"
                            : "وحدة"}
                        </small>
                      </div>

                      <div className="rukn-inv-v2-threshold">
                        {variant
                          ?.trackInventory
                          ? formatNumber(
                              variant.lowStockThreshold,
                            )
                          : "—"}
                      </div>

                      <InventoryStatus
                        inventory={
                          inventory
                        }
                      />

                      <div className="rukn-inv-v2-actions">
                        <button
                          type="button"
                          className="stock"
                          disabled={
                            !variant ||
                            normalizeStatus(
                              product.status,
                            ) ===
                              "archived"
                          }
                          onClick={() =>
                            editing
                              ? setStockEditor(
                                  null,
                                )
                              : openStockEditor(
                                  product,
                                )
                          }
                        >
                          <Settings2
                            size={14}
                          />

                          مخزون
                        </button>

                        <button
                          type="button"
                          onClick={() =>
                            setEditingProduct(
                              product,
                            )
                          }
                        >
                          <Pencil
                            size={14}
                          />

                          تعديل
                        </button>

                        <button
                          type="button"
                          className="archive"
                          disabled={
                            busyArchive
                          }
                          onClick={() =>
                            void archive(
                              product,
                            )
                          }
                        >
                          <Archive
                            size={14}
                          />

                          {busyArchive
                            ? "..."
                            : "أرشفة"}
                        </button>
                      </div>
                    </div>

                    {editing &&
                    stockEditor ? (
                      <div className="rukn-inv-v2-stock-editor">
                        <div className="rukn-inv-v2-stock-editor-title">
                          <div>
                            <strong>
                              تعديل سريع للمخزون
                            </strong>

                            <p>
                              {product.variants.length >
                              1
                                ? "التعديل السريع يخص المتغير الرئيسي. عدّل بقية الخيارات من إدارة المنتج."
                                : "حدّث الكمية وحد التنبيه بدون فتح محرر المنتج الكامل."}
                            </p>
                          </div>

                          <button
                            type="button"
                            aria-label="إغلاق"
                            onClick={() =>
                              setStockEditor(
                                null,
                              )
                            }
                          >
                            <X
                              size={16}
                            />
                          </button>
                        </div>

                        <div className="rukn-inv-v2-stock-fields">
                          <label className="toggle">
                            <input
                              type="checkbox"
                              checked={
                                stockEditor.trackInventory
                              }
                              onChange={(
                                event,
                              ) =>
                                setStockEditor(
                                  (
                                    current,
                                  ) =>
                                    current
                                      ? {
                                          ...current,
                                          trackInventory:
                                            event
                                              .target
                                              .checked,
                                        }
                                      : current,
                                )
                              }
                            />

                            <span>
                              تتبع المخزون
                            </span>
                          </label>

                          <label>
                            <span>
                              الكمية
                            </span>

                            <input
                              type="number"
                              min="0"
                              disabled={
                                !stockEditor.trackInventory
                              }
                              value={
                                stockEditor.quantity
                              }
                              onChange={(
                                event,
                              ) =>
                                setStockEditor(
                                  (
                                    current,
                                  ) =>
                                    current
                                      ? {
                                          ...current,
                                          quantity:
                                            event
                                              .target
                                              .value,
                                        }
                                      : current,
                                )
                              }
                            />
                          </label>

                          <label>
                            <span>
                              تنبيه عند
                            </span>

                            <input
                              type="number"
                              min="0"
                              disabled={
                                !stockEditor.trackInventory
                              }
                              value={
                                stockEditor.lowStockThreshold
                              }
                              onChange={(
                                event,
                              ) =>
                                setStockEditor(
                                  (
                                    current,
                                  ) =>
                                    current
                                      ? {
                                          ...current,
                                          lowStockThreshold:
                                            event
                                              .target
                                              .value,
                                        }
                                      : current,
                                )
                              }
                            />
                          </label>

                          <label className="toggle">
                            <input
                              type="checkbox"
                              disabled={
                                !stockEditor.trackInventory
                              }
                              checked={
                                stockEditor.continueSellingWhenOutOfStock
                              }
                              onChange={(
                                event,
                              ) =>
                                setStockEditor(
                                  (
                                    current,
                                  ) =>
                                    current
                                      ? {
                                          ...current,
                                          continueSellingWhenOutOfStock:
                                            event
                                              .target
                                              .checked,
                                        }
                                      : current,
                                )
                              }
                            />

                            <span>
                              البيع عند النفاد
                            </span>
                          </label>

                          <button
                            type="button"
                            className="save"
                            disabled={
                              savingStock
                            }
                            onClick={() =>
                              void saveStock(
                                product,
                              )
                            }
                          >
                            <Check
                              size={15}
                            />

                            {savingStock
                              ? "جاري الحفظ..."
                              : "حفظ المخزون"}
                          </button>
                        </div>
                      </div>
                    ) : null}
                  </article>
                );
              },
            )
          )}
        </div>
      </section>

      <section className="rukn-inv-v2-locations">
        <button
          type="button"
          className="rukn-inv-v2-section-toggle"
          onClick={() =>
            setLocationsExpanded(
              (
                current,
              ) =>
                !current,
            )
          }
        >
          <div>
            <span className="icon">
              <Warehouse
                size={17}
              />
            </span>

            <div>
              <strong>
                مواقع التخزين والاستلام
              </strong>

              <p>
                {formatNumber(
                  locations.filter(
                    (
                      location,
                    ) =>
                      location.isActive,
                  ).length,
                )}
                {" "}
                مواقع نشطة
              </p>
            </div>
          </div>

          {locationsExpanded ? (
            <ChevronUp
              size={17}
            />
          ) : (
            <ChevronDown
              size={17}
            />
          )}
        </button>

        {!locationsExpanded &&
        locations.length >
          0 ? (
          <div className="rukn-inv-v2-location-preview">
            {locations
              .slice(
                0,
                3,
              )
              .map(
                (
                  location,
                ) => (
                  <span
                    key={
                      location.id
                    }
                  >
                    <MapPin
                      size={13}
                    />

                    {
                      location.name
                    }

                    {location.isDefault
                      ? " · رئيسي"
                      : ""}
                  </span>
                ),
              )}
          </div>
        ) : null}

        {locationsExpanded ? (
          <div className="rukn-inv-v2-location-content">
            <div className="rukn-inv-v2-location-toolbar">
              <p>
                أضف المستودعات أو الفروع وحدد الموقع الرئيسي.
              </p>

              <button
                type="button"
                onClick={
                  openCreateLocation
                }
              >
                <Plus
                  size={15}
                />

                إضافة موقع
              </button>
            </div>

            <div className="rukn-inv-v2-location-grid">
              {locations.map(
                (
                  location,
                ) => (
                  <article
                    key={
                      location.id
                    }
                  >
                    <span>
                      <Warehouse
                        size={17}
                      />
                    </span>

                    <div>
                      <strong>
                        {
                          location.name
                        }
                      </strong>

                      <p>
                        {location.city}
                        {" · "}
                        {
                          location.line1
                        }
                      </p>

                      <small>
                        {location.isDefault
                          ? "الموقع الرئيسي · "
                          : ""}
                        {location.isActive
                          ? "نشط"
                          : "معطّل"}
                      </small>
                    </div>

                    <button
                      type="button"
                      aria-label={`تعديل ${location.name}`}
                      onClick={() =>
                        openEditLocation(
                          location,
                        )
                      }
                    >
                      <Pencil
                        size={15}
                      />
                    </button>
                  </article>
                ),
              )}

              {!loading &&
              locations.length ===
                0 ? (
                <div className="rukn-inv-v2-no-location">
                  لا توجد مواقع تخزين بعد.
                </div>
              ) : null}
            </div>
          </div>
        ) : null}
      </section>

      <section className="rukn-inv-v2-pickup">
        <button
          type="button"
          className="rukn-inv-v2-section-toggle"
          onClick={() =>
            setPickupExpanded(
              (
                current,
              ) =>
                !current,
            )
          }
        >
          <div>
            <span className="icon">
              <MapPin
                size={17}
              />
            </span>

            <div>
              <strong>
                إعدادات الاستلام من المتجر
              </strong>

              <p>
                العملة، نوع السعر، ورسوم الاستلام
              </p>
            </div>
          </div>

          {pickupExpanded ? (
            <ChevronUp
              size={17}
            />
          ) : (
            <ChevronDown
              size={17}
            />
          )}
        </button>

        {pickupExpanded &&
        tenantId ? (
          <div className="rukn-inv-v2-pickup-content">
            <AdminPickupSetup
              tenantId={
                tenantId
              }
              locations={
                locations
              }
            />
          </div>
        ) : null}
      </section>

      <CreateProductDialog
        open={
          createOpen
        }
        busy={
          creating
        }
        categories={
          categories
        }
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
          editingProduct
            ?.productId ??
          "no-product"
        }
        open={Boolean(
          editingProduct,
        )}
        busy={
          savingProduct
        }
        tenantId={
          tenantId
        }
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
          handleProductEdit
        }
      />

      {locationFormOpen ? (
        <div className="rukn-inv-v2-modal-layer">
          <button
            type="button"
            aria-label="إغلاق"
            className="rukn-inv-v2-modal-backdrop"
            onClick={() =>
              !savingLocation &&
              setLocationFormOpen(
                false,
              )
            }
          />

          <div className="rukn-inv-v2-location-modal">
            <header>
              <div>
                <span>
                  موقع التخزين
                </span>

                <h2>
                  {editingLocationId
                    ? "تعديل الموقع"
                    : "إضافة موقع"}
                </h2>
              </div>

              <button
                type="button"
                disabled={
                  savingLocation
                }
                onClick={() =>
                  setLocationFormOpen(
                    false,
                  )
                }
              >
                <X
                  size={17}
                />
              </button>
            </header>

            <div className="rukn-inv-v2-location-form">
              <label>
                <span>
                  اسم الموقع
                </span>

                <input
                  value={
                    locationForm.name
                  }
                  onChange={(
                    event,
                  ) =>
                    setLocationForm(
                      (
                        current,
                      ) => ({
                        ...current,
                        name:
                          event
                            .target
                            .value,
                      }),
                    )
                  }
                  placeholder="المتجر الرئيسي"
                />
              </label>

              <label>
                <span>
                  رمز الموقع
                </span>

                <input
                  dir="ltr"
                  value={
                    locationForm.code
                  }
                  onChange={(
                    event,
                  ) =>
                    setLocationForm(
                      (
                        current,
                      ) => ({
                        ...current,
                        code:
                          event
                            .target
                            .value,
                      }),
                    )
                  }
                  placeholder="MAIN"
                />
              </label>

              <label>
                <span>
                  المدينة
                </span>

                <input
                  value={
                    locationForm.city
                  }
                  onChange={(
                    event,
                  ) =>
                    setLocationForm(
                      (
                        current,
                      ) => ({
                        ...current,
                        city:
                          event
                            .target
                            .value,
                      }),
                    )
                  }
                />
              </label>

              <label>
                <span>
                  رمز الدولة
                </span>

                <input
                  dir="ltr"
                  maxLength={
                    2
                  }
                  value={
                    locationForm.countryCode
                  }
                  onChange={(
                    event,
                  ) =>
                    setLocationForm(
                      (
                        current,
                      ) => ({
                        ...current,
                        countryCode:
                          event
                            .target
                            .value,
                      }),
                    )
                  }
                />
              </label>

              <label className="wide">
                <span>
                  العنوان
                </span>

                <input
                  value={
                    locationForm.line1
                  }
                  onChange={(
                    event,
                  ) =>
                    setLocationForm(
                      (
                        current,
                      ) => ({
                        ...current,
                        line1:
                          event
                            .target
                            .value,
                      }),
                    )
                  }
                />
              </label>

              <label className="check">
                <input
                  type="checkbox"
                  checked={
                    locationForm.isDefault
                  }
                  onChange={(
                    event,
                  ) =>
                    setLocationForm(
                      (
                        current,
                      ) => ({
                        ...current,
                        isDefault:
                          event
                            .target
                            .checked,
                      }),
                    )
                  }
                />

                الموقع الرئيسي
              </label>

              <label className="check">
                <input
                  type="checkbox"
                  checked={
                    locationForm.isActive
                  }
                  onChange={(
                    event,
                  ) =>
                    setLocationForm(
                      (
                        current,
                      ) => ({
                        ...current,
                        isActive:
                          event
                            .target
                            .checked,
                      }),
                    )
                  }
                />

                الموقع نشط
              </label>
            </div>

            <footer>
              <button
                type="button"
                className="cancel"
                disabled={
                  savingLocation
                }
                onClick={() =>
                  setLocationFormOpen(
                    false,
                  )
                }
              >
                إلغاء
              </button>

              <button
                type="button"
                className="save"
                disabled={
                  savingLocation
                }
                onClick={() =>
                  void saveLocation()
                }
              >
                <Check
                  size={15}
                />

                {savingLocation
                  ? "جاري الحفظ..."
                  : "حفظ الموقع"}
              </button>
            </footer>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function InventoryStatus({
  inventory,
}: {
  inventory:
    ReturnType<
      typeof productInventory
    >;
}) {
  if (
    inventory.untracked
  ) {
    return (
      <span className="rukn-inv-v2-status neutral">
        غير متتبع
      </span>
    );
  }

  if (
    inventory.out
  ) {
    return (
      <span className="rukn-inv-v2-status danger">
        نافد
      </span>
    );
  }

  if (
    inventory.low
  ) {
    return (
      <span className="rukn-inv-v2-status warning">
        منخفض
      </span>
    );
  }

  return (
    <span className="rukn-inv-v2-status success">
      متوفر
    </span>
  );
}