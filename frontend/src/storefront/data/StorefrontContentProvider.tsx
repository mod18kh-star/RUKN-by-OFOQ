import type {
  PropsWithChildren,
} from "react";

import {
  useQuery,
} from "@tanstack/react-query";

import {
  useParams,
} from "react-router";

import {
  resolveCategoryContent,
  resolveProductContent,
} from "./contentSourceResolver";

import {
  DEMO_STOREFRONT_CATEGORIES,
  DEMO_STOREFRONT_PRODUCTS,
} from "./demoCatalog";

import {
  getStorefrontCategories,
  getStorefrontProducts,
  storefrontApiIsConfigured,
} from "./storefrontApi";

import {
  StorefrontContentContext,
  type RuntimeProduct,
} from "./storefrontContent";

import type {
  StorefrontConfig,
} from "../theme/theme.types";

export function StorefrontContentProvider({
  config,
  children,
}: PropsWithChildren<{
  config:
    StorefrontConfig;
}>) {
  const params =
    useParams<{
      storeSlug:
        string;
    }>();

  const storeSlug =
    params.storeSlug ??
    "demo";

  const useLiveApi =
    storefrontApiIsConfigured &&
    storeSlug !==
      "demo";

  const productSource =
    config.productSection
      .sourceType;

  const requestedItemLimit =
    clampPageSize(
      config.productSection
        .itemLimit,
    );

  const requestedPageSize =
    productSource ===
    "manual"
      ? 100
      : requestedItemLimit;

  const requestedCategory =
    productSource ===
      "category" &&
    config.productSection
      .categorySlug
      .trim()
      ? config.productSection
          .categorySlug
          .trim()
      : undefined;

  const categoriesQuery =
    useQuery({
      queryKey: [
        "storefront-runtime",
        storeSlug,
        "categories",
      ],

      queryFn: () =>
        getStorefrontCategories(
          storeSlug,
        ),

      enabled:
        useLiveApi,

      retry:
        1,

      staleTime:
        5 * 60_000,
    });

  const productsQuery =
    useQuery({
      queryKey: [
        "storefront-runtime",
        storeSlug,
        "products",
        productSource,
        requestedCategory ??
          "",
        requestedPageSize,
      ],

      queryFn: () =>
        getStorefrontProducts(
          storeSlug,
          {
            page:
              1,

            pageSize:
              requestedPageSize,

            category:
              requestedCategory,
          },
        ),

      enabled:
        useLiveApi,

      retry:
        1,

      staleTime:
        60_000,
    });

  const rawCategories =
    useLiveApi
      ? (
          categoriesQuery.data ??
          []
        )
      : DEMO_STOREFRONT_CATEGORIES;

  const rawProducts =
    useLiveApi
      ? (
          productsQuery.data
            ?.items ??
          []
        )
      : DEMO_STOREFRONT_PRODUCTS;

  const categories =
    resolveCategoryContent(
      config,
      rawCategories,
    );

  const selectedProducts =
    resolveProductContent(
      config,
      rawProducts,
      rawCategories,
    );

  const categoryNames =
    new Map(
      rawCategories.map(
        (category) => [
          category.categoryId,
          category.name,
        ],
      ),
    );

  const products:
    RuntimeProduct[] =
    selectedProducts.map(
      (product) => ({
        productId:
          product.productId,

        slug:
          product.slug,

        name:
          product.name,

        category:
          product.categoryId
            ? (
                categoryNames.get(
                  product.categoryId,
                ) ??
                "بدون تصنيف"
              )
            : "بدون تصنيف",

        price:
          formatMoney(
            product.price,
            product.currency,
          ),

        compareAtPrice:
          product.compareAtPrice !==
          null
            ? formatMoney(
                product.compareAtPrice,
                product.currency,
              )
            : undefined,

        image:
          product.primaryImageUrl,

        primaryImage:
          product.primaryImageUrl,

        badge:
          product.compareAtPrice !==
          null
            ? "عرض"
            : undefined,
      }),
    );

  const isLoading =
    useLiveApi &&
    (
      categoriesQuery.isPending ||
      productsQuery.isPending
    );

  const hasError =
    useLiveApi &&
    (
      categoriesQuery.isError ||
      productsQuery.isError
    );

  return (
    <StorefrontContentContext.Provider
      value={{
        products,
        categories,
        usingDemoData:
          !useLiveApi,
        isLoading,
        hasError,
      }}
    >
      {children}
    </StorefrontContentContext.Provider>
  );
}

function clampPageSize(
  value: number,
) {
  if (
    !Number.isFinite(
      value,
    )
  ) {
    return 8;
  }

  return Math.min(
    100,
    Math.max(
      1,
      Math.round(
        value,
      ),
    ),
  );
}

function formatMoney(
  amount: number,
  currency: string,
) {
  try {
    return new Intl.NumberFormat(
      "ar-SA",
      {
        style:
          "currency",

        currency,

        maximumFractionDigits:
          Number.isInteger(
            amount,
          )
            ? 0
            : 2,
      },
    ).format(
      amount,
    );
  }
  catch {
    return `${amount.toLocaleString(
      "ar-SA",
    )} ${currency}`;
  }
}