import type {
  StorefrontConfig,
} from "../theme/theme.types";

import type {
  StorefrontCategory,
  StorefrontProductSummary,
} from "./storefrontApi";

export function resolveProductContent(
  config: StorefrontConfig,
  products:
    StorefrontProductSummary[],
  categories:
    StorefrontCategory[],
) {
  const section =
    config.productSection;

  let result =
    products.filter(
      (product) =>
        product.availableForSale,
    );

  if (
    section.sourceType ===
      "category" &&
    section.categorySlug
  ) {
    const category =
      categories.find(
        (item) =>
          item.slug ===
          section.categorySlug,
      );

    if (category) {
      result =
        result.filter(
          (product) =>
            product.categoryId ===
            category.categoryId,
        );
    }
    else {
      result = [];
    }
  }

  if (
    section.sourceType ===
    "manual"
  ) {
    const selected =
      new Set(
        section.manualProductIds,
      );

    result =
      result.filter(
        (product) =>
          selected.has(
            product.productId,
          ),
      );

    result.sort(
      (left, right) =>
        section.manualProductIds.indexOf(
          left.productId,
        ) -
        section.manualProductIds.indexOf(
          right.productId,
        ),
    );
  }

  return result.slice(
    0,
    Math.max(
      1,
      section.itemLimit,
    ),
  );
}

export function resolveCategoryContent(
  config: StorefrontConfig,
  categories:
    StorefrontCategory[],
) {
  const section =
    config.categorySection;

  let result =
    categories
      .filter(
        (category) =>
          category.parentCategoryId === null,
      )
      .sort(
        (left, right) =>
          left.sortOrder -
          right.sortOrder,
      );

  if (
    section.sourceType ===
    "manual"
  ) {
    const selected =
      new Set(
        section.manualCategorySlugs,
      );

    result =
      result.filter(
        (category) =>
          selected.has(
            category.slug,
          ),
      );

    result.sort(
      (left, right) =>
        section.manualCategorySlugs.indexOf(
          left.slug,
        ) -
        section.manualCategorySlugs.indexOf(
          right.slug,
        ),
    );
  }

  return result.slice(
    0,
    Math.max(
      1,
      section.itemLimit,
    ),
  );
}