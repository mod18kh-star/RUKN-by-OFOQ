import {
  ChevronLeft,
  FolderTree,
  PackageOpen,
} from "lucide-react";

import {
  useMemo,
} from "react";

import {
  useQuery,
} from "@tanstack/react-query";

import {
  Link,
  useParams,
} from "react-router";

import {
  ProductCard,
} from "../components/ProductCard";

import {
  SmartImage,
} from "../components/SmartImage";

import {
  StorefrontHeader,
} from "../components/StorefrontHeader";

import {
  StorefrontFooter,
} from "../components/StorefrontFooter";

import {
  loadStorefrontConfig,
} from "../config/storefrontConfig";

import { parseVisualContent } from "../config/visualContent";

import {
  createLiveStorefrontConfig,
} from "../config/liveStorefrontConfig";

import {
  DEMO_STOREFRONT_CATEGORIES,
  DEMO_STOREFRONT_PRODUCTS,
} from "../data/demoCatalog";

import {
  EMPTY_STOREFRONT_CONTACT,
  getStorefrontCategories,
  getStorefrontInfo,
  getStorefrontProducts,
  storefrontApiIsConfigured,
  type StorefrontCategory,
  type StorefrontProductSummary,
} from "../data/storefrontApi";

import {
  createThemeStyle,
  resolveStorefrontConfig,
} from "../theme/themeEngine";

import {
  formatStorefrontMoney,
} from "../utils/money";

function sortCategories(
  categories: StorefrontCategory[],
) {
  return [...categories].sort(
    (left, right) =>
      left.sortOrder - right.sortOrder ||
      left.name.localeCompare(right.name, "ar"),
  );
}

function buildBreadcrumbs(
  categories: StorefrontCategory[],
  category: StorefrontCategory,
) {
  const result: StorefrontCategory[] = [];
  let current: StorefrontCategory | undefined = category;
  let guard = 0;

  while (current && guard < 32) {
    result.unshift(current);

    current = current.parentCategoryId
      ? categories.find(
          (item) =>
            item.categoryId === current?.parentCategoryId,
        )
      : undefined;

    guard += 1;
  }

  return result;
}

export function StorefrontCategoryPage() {
  const {
    storeSlug = "",
    categorySlug = "",
  } = useParams<{
    storeSlug: string;
    categorySlug: string;
  }>();

  const isDemo =
    storeSlug === "demo";

  const useLiveApi =
    !isDemo &&
    storefrontApiIsConfigured;

  const storeQuery =
    useQuery({
      queryKey: [
        "storefront-category",
        storeSlug,
        "info",
      ],
      queryFn: () =>
        getStorefrontInfo(storeSlug),
      enabled: useLiveApi,
      retry: 1,
      staleTime: 5 * 60_000,
    });

  const categoriesQuery =
    useQuery({
      queryKey: [
        "storefront-category",
        storeSlug,
        "categories",
      ],
      queryFn: () =>
        getStorefrontCategories(storeSlug),
      enabled: useLiveApi,
      retry: 1,
      staleTime: 5 * 60_000,
    });

  const productsQuery =
    useQuery({
      queryKey: [
        "storefront-category",
        storeSlug,
        categorySlug,
        "products",
      ],
      queryFn: () =>
        getStorefrontProducts(
          storeSlug,
          {
            category: categorySlug,
            page: 1,
            pageSize: 100,
          },
        ),
      enabled:
        useLiveApi &&
        Boolean(categorySlug),
      retry: 1,
      staleTime: 60_000,
    });

  const categories =
    useMemo(
      () =>
        useLiveApi
          ? categoriesQuery.data ?? []
          : DEMO_STOREFRONT_CATEGORIES,
      [categoriesQuery.data, useLiveApi],
    );

  const category =
    categories.find(
      (item) =>
        item.slug === categorySlug,
    );

  const children =
    useMemo(
      () =>
        category
          ? sortCategories(
              categories.filter(
                (item) =>
                  item.parentCategoryId ===
                  category.categoryId,
              ),
            )
          : [],
      [categories, category],
    );

  const products: StorefrontProductSummary[] =
    useLiveApi
      ? productsQuery.data?.items ?? []
      : category
        ? DEMO_STOREFRONT_PRODUCTS.filter(
            (product) =>
              product.categoryId ===
                category.categoryId &&
              product.availableForSale,
          )
        : [];

  const config =
    useLiveApi && storeQuery.data
      ? resolveStorefrontConfig(
          createLiveStorefrontConfig(
            storeQuery.data,
          ),
        )
      : resolveStorefrontConfig(
          loadStorefrontConfig(),
        );

  const breadcrumbs =
    useMemo(
      () =>
        category
          ? buildBreadcrumbs(
              categories,
              category,
            )
          : [],
      [categories, category],
    );

  const isPending =
    useLiveApi &&
    (
      storeQuery.isPending ||
      categoriesQuery.isPending ||
      productsQuery.isPending
    );

  const hasError =
    useLiveApi &&
    (
      storeQuery.isError ||
      categoriesQuery.isError ||
      productsQuery.isError
    );

  if (isPending) {
    return (
      <div
        dir="rtl"
        className="flex min-h-screen items-center justify-center bg-[#f7f6f2] text-sm text-black/45"
      >
        جاري فتح القسم...
      </div>
    );
  }

  if (hasError || !category) {
    return (
      <div
        dir="rtl"
        className="flex min-h-screen items-center justify-center bg-[#f7f6f2] px-5 text-center"
      >
        <div>
          <h1 className="text-xl font-semibold">
            تعذر فتح القسم
          </h1>
          <Link
            to={`/store/${encodeURIComponent(storeSlug)}`}
            className="mt-5 inline-flex text-sm underline"
          >
            العودة للمتجر
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div
      dir="rtl"
      data-store-theme={config.themeId}
      style={createThemeStyle(config)}
      className="min-h-screen bg-[var(--store-canvas)] font-[var(--store-font)] text-[var(--store-ink)]"
    >
      <StorefrontHeader
        storeSlug={storeSlug}
        storeName={config.storeName}
        logoUrl={config.logoUrl}
        announcement={config.announcement}
        themeId={config.themeId}
      />

      <main className="store-container py-10 md:py-14">
        <nav className="flex flex-wrap items-center gap-1.5 text-[10px] text-[var(--store-muted)]">
          <Link
            to={`/store/${encodeURIComponent(storeSlug)}`}
            className="hover:text-[var(--store-ink)]"
          >
            المتجر
          </Link>

          {breadcrumbs.map(
            (item) => (
              <span
                key={item.categoryId}
                className="inline-flex items-center gap-1.5"
              >
                <ChevronLeft size={11} />
                <Link
                  to={`/store/${encodeURIComponent(storeSlug)}/categories/${encodeURIComponent(item.slug)}`}
                  className="hover:text-[var(--store-ink)]"
                >
                  {item.name}
                </Link>
              </span>
            ),
          )}
        </nav>

        <header className="mt-8 border-b border-black/[0.08] pb-8">
          <p className="text-[10px] font-semibold text-[var(--store-accent)]">
            القسم الحالي
          </p>
          <h1 className="mt-2 text-[clamp(2rem,4vw,4.2rem)] font-semibold tracking-[-0.05em]">
            {category.name}
          </h1>
          <p className="mt-3 text-[11px] text-[var(--store-muted)]">
            {children.length > 0
              ? `${children.length} أقسام داخلية`
              : `${products.length} منتجات داخل القسم`}
          </p>
        </header>

        {children.length > 0 ? (
          <section className="py-10 md:py-12">
            <div className="mb-5 flex items-center gap-2">
              <FolderTree size={17} />
              <h2 className="text-[17px] font-semibold">
                الأقسام الداخلية
              </h2>
            </div>

            <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4">
              {children.map(
                (child) => (
                  <Link
                    key={child.categoryId}
                    to={`/store/${encodeURIComponent(storeSlug)}/categories/${encodeURIComponent(child.slug)}`}
                    className="group overflow-hidden rounded-[var(--store-radius)] border border-black/[0.08] bg-[var(--store-surface)]"
                  >
                    <div className="aspect-[4/3] overflow-hidden bg-[var(--store-soft)]">
                      {child.imageUrl ? (
                        <SmartImage
                          src={child.imageUrl}
                          alt={child.name}
                          className="h-full w-full object-cover transition duration-500 group-hover:scale-[1.02]"
                        />
                      ) : (
                        <div className="flex h-full items-center justify-center text-[var(--store-muted)]">
                          <FolderTree size={26} />
                        </div>
                      )}
                    </div>
                    <div className="flex items-center justify-between gap-3 p-4">
                      <span className="text-[12px] font-semibold">
                        {child.name}
                      </span>
                      <ChevronLeft
                        size={14}
                        className="text-[var(--store-muted)]"
                      />
                    </div>
                  </Link>
                ),
              )}
            </div>
          </section>
        ) : null}

        {products.length > 0 ? (
          <section className={children.length > 0 ? "border-t border-black/[0.08] py-10 md:py-12" : "py-10 md:py-12"}>
            <h2 className="mb-6 text-[17px] font-semibold">
              المنتجات
            </h2>

            <div className="grid grid-cols-2 gap-x-4 gap-y-8 md:grid-cols-3 lg:grid-cols-4">
              {products.map(
                (product) => (
                  <ProductCard
                    key={product.productId}
                    name={product.name}
                    category={category.name}
                    price={formatStorefrontMoney(product.price, product.currency)}
                    compareAtPrice={
                      product.compareAtPrice !== null
                        ? formatStorefrontMoney(
                            product.compareAtPrice,
                            product.currency,
                          )
                        : undefined
                    }
                    description={product.description ?? undefined}
                    href={`/store/${encodeURIComponent(storeSlug)}/products/${encodeURIComponent(product.slug)}`}
                    image={product.primaryImageUrl ?? ""}
                    badge={product.compareAtPrice !== null ? "عرض" : undefined}
                    variant={config.productCardStyle}
                  />
                ),
              )}
            </div>
          </section>
        ) : null}

        {children.length === 0 && products.length === 0 ? (
          <section className="flex min-h-[320px] items-center justify-center border-t border-black/[0.08] text-center">
            <div>
              <PackageOpen
                size={30}
                className="mx-auto text-[var(--store-muted)]"
              />
              <h2 className="mt-4 text-[17px] font-semibold">
                هذا القسم فارغ حاليًا
              </h2>
              <p className="mt-2 text-[11px] text-[var(--store-muted)]">
                لا توجد أقسام داخلية أو منتجات منشورة هنا بعد.
              </p>
            </div>
          </section>
        ) : null}
      </main>

      <StorefrontFooter
        storeSlug={storeSlug}
        storeName={config.storeName}
        logoUrl={config.logoUrl}
        themeId={config.themeId}
        contact={storeQuery.data?.contact ?? EMPTY_STOREFRONT_CONTACT}
        description={parseVisualContent(storeQuery.data?.presentation.visualContentJson).footerDescription}
      />
    </div>
  );
}
