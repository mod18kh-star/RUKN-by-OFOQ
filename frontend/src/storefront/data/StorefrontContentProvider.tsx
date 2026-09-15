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
      storeSlug: string;
    }>();

  const storeSlug =
    params.storeSlug ??
    "demo";

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
        storefrontApiIsConfigured,

      retry:
        false,

      staleTime:
        30_000,
    });

  const productsQuery =
    useQuery({
      queryKey: [
        "storefront-runtime",
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

  const rawCategories =
    categoriesQuery.data ??
    DEMO_STOREFRONT_CATEGORIES;

  const rawProducts =
    productsQuery.data
      ?.items ??
    DEMO_STOREFRONT_PRODUCTS;

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

  const usingDemoData =
    !storefrontApiIsConfigured ||
    categoriesQuery.isError ||
    productsQuery.isError;

  return (
    <StorefrontContentContext.Provider
      value={{
        products,
        categories,
        usingDemoData,
      }}
    >
      {children}
    </StorefrontContentContext.Provider>
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