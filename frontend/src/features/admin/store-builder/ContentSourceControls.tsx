import type {
  Dispatch,
  SetStateAction,
} from "react";

import {
  Database,
} from "lucide-react";

import {
  useQuery,
} from "@tanstack/react-query";

import {
  DEMO_STOREFRONT_CATEGORIES,
  DEMO_STOREFRONT_PRODUCTS,
} from "../../../storefront/data/demoCatalog";

import {
  getStorefrontCategories,
  getStorefrontProducts,
  storefrontApiIsConfigured,
} from "../../../storefront/data/storefrontApi";

import type {
  StorefrontConfig,
} from "../../../storefront/theme/theme.types";

import {
  BuilderPanel,
} from "./BuilderControls";

export function ContentSourceControls({
  config,
  setConfig,
}: {
  config:
    StorefrontConfig;

  setConfig:
    Dispatch<
      SetStateAction<StorefrontConfig>
    >;
}) {
  const storeSlug =
    "demo";

  const categoriesQuery =
    useQuery({
      queryKey: [
        "storefront-content-source",
        storeSlug,
        "categories",
      ],

      queryFn: () =>
        getStorefrontCategories(
          storeSlug,
        ),

      enabled:
        storefrontApiIsConfigured,

      retry:
        false,

      staleTime:
        30_000,
    });

  const productsQuery =
    useQuery({
      queryKey: [
        "storefront-content-source",
        storeSlug,
        "products",
      ],

      queryFn: () =>
        getStorefrontProducts(
          storeSlug,
          {
            page:
              1,

            pageSize:
              100,
          },
        ),

      enabled:
        storefrontApiIsConfigured,

      retry:
        false,

      staleTime:
        30_000,
    });

  const categories =
    categoriesQuery.data ??
    DEMO_STOREFRONT_CATEGORIES;

  const products =
    productsQuery.data
      ?.items ??
    DEMO_STOREFRONT_PRODUCTS;

  const usingDemoData =
    !storefrontApiIsConfigured ||
    categoriesQuery.isError ||
    productsQuery.isError;

  function toggleProduct(
    productId: string,
  ) {
    setConfig(
      (current) => {
        const selected =
          current.productSection
            .manualProductIds;

        const exists =
          selected.includes(
            productId,
          );

        return {
          ...current,

          productSection: {
            ...current.productSection,

            manualProductIds:
              exists
                ? selected.filter(
                    (id) =>
                      id !==
                      productId,
                  )
                : [
                    ...selected,
                    productId,
                  ],
          },
        };
      },
    );
  }

  function setProductPosition(
    productId: string,
    requestedPosition: number,
  ) {
    setConfig(
      (current) => {
        const selected =
          [
            ...current.productSection
              .manualProductIds,
          ];

        const currentIndex =
          selected.indexOf(
            productId,
          );

        if (
          currentIndex < 0
        ) {
          return current;
        }

        const boundedPosition =
          Math.min(
            Math.max(
              requestedPosition,
              1,
            ),
            selected.length,
          );

        selected.splice(
          currentIndex,
          1,
        );

        selected.splice(
          boundedPosition - 1,
          0,
          productId,
        );

        return {
          ...current,

          productSection: {
            ...current.productSection,

            manualProductIds:
              selected,
          },
        };
      },
    );
  }
  function toggleCategory(
    slug: string,
  ) {
    setConfig(
      (current) => {
        const selected =
          current.categorySection
            .manualCategorySlugs;

        const exists =
          selected.includes(
            slug,
          );

        return {
          ...current,

          categorySection: {
            ...current.categorySection,

            manualCategorySlugs:
              exists
                ? selected.filter(
                    (item) =>
                      item !==
                      slug,
                  )
                : [
                    ...selected,
                    slug,
                  ],
          },
        };
      },
    );
  }

  return (
    <>
      <BuilderPanel
        title="مصدر منتجات القسم"
        description="حدد من أين يأتي محتوى قسم المنتجات. يتم استخدام كتالوج المتجر الحقيقي عند ربط عنوان الـAPI."
        action={
          <Database
            size={17}
            className="text-black/35"
          />
        }
      >
        <div className="mb-4 flex items-center justify-between gap-4 rounded-[9px] bg-[#f5f5f1] px-4 py-3">
          <div>
            <p className="text-[10px] font-semibold">
              مصدر البيانات
            </p>

            <p className="mt-1 text-[9px] text-[var(--ink-muted)]">
              {usingDemoData
                ? "يتم عرض بيانات تجريبية في المعاينة حاليا"
                : "متصل بكتالوج المتجر"}
            </p>
          </div>

          <span
            className={[
              "rounded-full px-2.5 py-1 text-[9px] font-semibold",
              usingDemoData
                ? "bg-[#eee9df] text-[#725b32]"
                : "bg-[#e4eee8] text-[#315a49]",
            ].join(" ")}
          >
            {usingDemoData
              ? "Demo"
              : "Live"}
          </span>
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <SourceChoice
            title="كل المنتجات"
            description="يعرض المنتجات المتاحة من الكتالوج."
            selected={
              config.productSection.sourceType ===
              "catalog"
            }
            onClick={() =>
              setConfig(
                (current) => ({
                  ...current,

                  productSection: {
                    ...current.productSection,

                    sourceType:
                      "catalog",
                  },
                }),
              )
            }
          />

          <SourceChoice
            title="تصنيف محدد"
            description="يعرض منتجات تصنيف واحد فقط."
            selected={
              config.productSection.sourceType ===
              "category"
            }
            onClick={() =>
              setConfig(
                (current) => ({
                  ...current,

                  productSection: {
                    ...current.productSection,

                    sourceType:
                      "category",
                  },
                }),
              )
            }
          />

          <SourceChoice
            title="اختيار يدوي"
            description="اختر المنتجات بنفسك."
            selected={
              config.productSection.sourceType ===
              "manual"
            }
            onClick={() =>
              setConfig(
                (current) => ({
                  ...current,

                  productSection: {
                    ...current.productSection,

                    sourceType:
                      "manual",
                  },
                }),
              )
            }
          />
        </div>

        {config.productSection.sourceType ===
        "category" ? (
          <div className="mt-4">
            <label className="mb-2 block text-[10px] font-semibold">
              التصنيف
            </label>

            <select
              value={
                config.productSection.categorySlug
              }
              onChange={(event) =>
                setConfig(
                  (current) => ({
                    ...current,

                    productSection: {
                      ...current.productSection,

                      categorySlug:
                        event.target.value,
                    },
                  }),
                )
              }
              className="h-11 w-full rounded-[8px] border border-black/[0.1] bg-white px-3 text-[11px] outline-none"
            >
              <option value="">
                اختر تصنيفا
              </option>

              {categories.map(
                (category) => (
                  <option
                    key={
                      category.categoryId
                    }
                    value={
                      category.slug
                    }
                  >
                    {category.name}
                  </option>
                ),
              )}
            </select>
          </div>
        ) : null}

        {config.productSection.sourceType ===
        "manual" ? (
          <div className="mt-4">
            <div className="mb-3 flex items-end justify-between gap-4">
              <div>
                <p className="text-[10px] font-semibold">
                  المنتجات المختارة
                </p>

                <p className="mt-1 text-[9px] leading-5 text-[var(--ink-muted)]">
                  اختر المنتجات ثم حدد رقم ظهور كل منتج. الرقم 1 يظهر أولا.
                </p>
              </div>

              <span className="rounded-full bg-[#f0f0ec] px-2.5 py-1 text-[9px] font-semibold text-[var(--ink-muted)]">
                {
                  config.productSection
                    .manualProductIds.length
                } مختار
              </span>
            </div>

            <div className="max-h-[380px] space-y-2 overflow-y-auto rounded-[9px] border border-black/[0.08] bg-white p-2">
              {products.map(
                (product) => {
                  const selected =
                    config.productSection.manualProductIds.includes(
                      product.productId,
                    );

                  const position =
                    config.productSection.manualProductIds.indexOf(
                      product.productId,
                    ) + 1;

                  return (
                    <div
                      key={
                        product.productId
                      }
                      className={[
                        "flex items-center gap-3 rounded-[8px] border p-2.5 transition",
                        selected
                          ? "border-[#315a49]/25 bg-[#edf3ef]"
                          : "border-black/[0.06] bg-white",
                      ].join(" ")}
                    >
                      <button
                        type="button"
                        onClick={() =>
                          toggleProduct(
                            product.productId,
                          )
                        }
                        className="flex min-w-0 flex-1 items-center gap-3 text-right"
                      >
                        <img
                          src={product.primaryImageUrl ?? undefined}
                          alt=""
                          className="size-11 shrink-0 rounded-[6px] object-cover"
                        />

                        <span className="min-w-0 flex-1">
                          <span className="block truncate text-[10px] font-semibold">
                            {product.name}
                          </span>

                          <span className="mt-0.5 block text-[9px] text-[var(--ink-muted)]">
                            {product.price}{" "}
                            {product.currency}
                          </span>
                        </span>

                        <span
                          className={[
                            "flex size-5 shrink-0 items-center justify-center rounded-full border text-[10px]",
                            selected
                              ? "border-[#315a49] bg-[#315a49] text-white"
                              : "border-black/15",
                          ].join(" ")}
                        >
                          {selected
                            ? "✓"
                            : ""}
                        </span>
                      </button>

                      {selected ? (
                        <label className="flex shrink-0 items-center gap-2 border-r border-black/[0.08] pr-3">
                          <span className="text-[9px] text-[var(--ink-muted)]">
                            الترتيب
                          </span>

                          <input
                            type="number"
                            min={1}
                            max={
                              config.productSection
                                .manualProductIds.length
                            }
                            value={
                              position
                            }
                            onChange={(event) =>
                              setProductPosition(
                                product.productId,
                                Number(
                                  event.target.value,
                                ) || 1,
                              )
                            }
                            className="h-9 w-14 rounded-[7px] border border-black/[0.12] bg-white px-2 text-center text-[10px] font-semibold outline-none focus:border-[#315a49]"
                            aria-label={`ترتيب ظهور ${product.name}`}
                          />
                        </label>
                      ) : null}
                    </div>
                  );
                },
              )}
            </div>

            {config.productSection.manualProductIds.length >
            0 ? (
              <div className="mt-3 rounded-[8px] border border-black/[0.07] bg-[#fafaf7] p-3">
                <p className="mb-2 text-[9px] font-semibold text-[var(--ink-muted)]">
                  ترتيب الظهور الحالي
                </p>

                <div className="flex flex-wrap gap-2">
                  {config.productSection.manualProductIds.map(
                    (
                      productId,
                      index,
                    ) => {
                      const product =
                        products.find(
                          (item) =>
                            item.productId ===
                            productId,
                        );

                      if (!product) {
                        return null;
                      }

                      return (
                        <span
                          key={
                            productId
                          }
                          className="inline-flex items-center gap-2 rounded-full bg-white px-3 py-1.5 text-[9px] font-semibold shadow-[0_0_0_1px_rgba(0,0,0,.06)]"
                        >
                          <span className="flex size-4 items-center justify-center rounded-full bg-[#315a49] text-[8px] text-white">
                            {index + 1}
                          </span>

                          {
                            product.name
                          }
                        </span>
                      );
                    },
                  )}
                </div>
              </div>
            ) : null}
          </div>
        ) : null}
        <div className="mt-4">
          <label className="mb-2 block text-[10px] font-semibold">
            عدد المنتجات في القسم
          </label>

          <select
            value={
              config.productSection.itemLimit
            }
            onChange={(event) =>
              setConfig(
                (current) => ({
                  ...current,

                  productSection: {
                    ...current.productSection,

                    itemLimit:
                      Number(
                        event.target.value,
                      ),
                  },
                }),
              )
            }
            className="h-11 w-full rounded-[8px] border border-black/[0.1] bg-white px-3 text-[11px]"
          >
            <option value={4}>
              4 منتجات
            </option>

            <option value={6}>
              6 منتجات
            </option>

            <option value={8}>
              8 منتجات
            </option>

            <option value={12}>
              12 منتجا
            </option>
          </select>
        </div>
      </BuilderPanel>

      {config.categorySection.enabled ? (
        <BuilderPanel
          title="مصدر التصنيفات"
          description="حدد هل يظهر كل تصنيفات المتجر أو مجموعة تختارها بنفسك."
        >
          <div className="grid gap-3 sm:grid-cols-2">
            <SourceChoice
              title="كل التصنيفات"
              description="يستخدم ترتيب التصنيفات الموجود في الكتالوج."
              selected={
                config.categorySection.sourceType ===
                "all"
              }
              onClick={() =>
                setConfig(
                  (current) => ({
                    ...current,

                    categorySection: {
                      ...current.categorySection,

                      sourceType:
                        "all",
                    },
                  }),
                )
              }
            />

            <SourceChoice
              title="اختيار يدوي"
              description="اختر التصنيفات التي تريد إبرازها."
              selected={
                config.categorySection.sourceType ===
                "manual"
              }
              onClick={() =>
                setConfig(
                  (current) => ({
                    ...current,

                    categorySection: {
                      ...current.categorySection,

                      sourceType:
                        "manual",
                    },
                  }),
                )
              }
            />
          </div>

          {config.categorySection.sourceType ===
          "manual" ? (
            <div className="mt-4 grid gap-2 sm:grid-cols-2">
              {categories.map(
                (category) => {
                  const selected =
                    config.categorySection.manualCategorySlugs.includes(
                      category.slug,
                    );

                  return (
                    <button
                      key={
                        category.categoryId
                      }
                      type="button"
                      onClick={() =>
                        toggleCategory(
                          category.slug,
                        )
                      }
                      className={[
                        "flex items-center justify-between rounded-[8px] border px-3 py-3 text-right text-[10px] font-semibold transition",
                        selected
                          ? "border-[#315a49]/25 bg-[#edf3ef]"
                          : "border-black/[0.07] bg-white",
                      ].join(" ")}
                    >
                      <span>
                        {
                          category.name
                        }
                      </span>

                      <span
                        className={[
                          "flex size-5 items-center justify-center rounded-full border text-[9px]",
                          selected
                            ? "border-[#315a49] bg-[#315a49] text-white"
                            : "border-black/15",
                        ].join(" ")}
                      >
                        {selected
                          ? "✓"
                          : ""}
                      </span>
                    </button>
                  );
                },
              )}
            </div>
          ) : null}

          <div className="mt-4">
            <label className="mb-2 block text-[10px] font-semibold">
              عدد التصنيفات المعروضة
            </label>

            <select
              value={
                config.categorySection.itemLimit
              }
              onChange={(event) =>
                setConfig(
                  (current) => ({
                    ...current,

                    categorySection: {
                      ...current.categorySection,

                      itemLimit:
                        Number(
                          event.target.value,
                        ),
                    },
                  }),
                )
              }
              className="h-11 w-full rounded-[8px] border border-black/[0.1] bg-white px-3 text-[11px]"
            >
              <option value={4}>
                4 تصنيفات
              </option>

              <option value={6}>
                6 تصنيفات
              </option>

              <option value={8}>
                8 تصنيفات
              </option>

              <option value={12}>
                12 تصنيفا
              </option>
            </select>
          </div>
        </BuilderPanel>
      ) : null}
    </>
  );
}

function SourceChoice({
  title,
  description,
  selected,
  onClick,
}: {
  title: string;
  description: string;
  selected: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={
        onClick
      }
      className={[
        "min-h-[105px] rounded-[10px] border p-4 text-right transition",
        selected
          ? "border-[#315a49]/30 bg-[#edf3ef]"
          : "border-black/[0.08] bg-white hover:bg-black/[0.02]",
      ].join(" ")}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-[11px] font-semibold">
            {title}
          </p>

          <p className="mt-1.5 text-[9px] leading-5 text-[var(--ink-muted)]">
            {description}
          </p>
        </div>

        <span
          className={[
            "mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-full border text-[9px]",
            selected
              ? "border-[#315a49] bg-[#315a49] text-white"
              : "border-black/15",
          ].join(" ")}
        >
          {selected
            ? "✓"
            : ""}
        </span>
      </div>
    </button>
  );
}