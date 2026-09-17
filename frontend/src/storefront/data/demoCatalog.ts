import type {
  StorefrontCategory,
  StorefrontProductSummary,
} from "./storefrontApi";

export const DEMO_STOREFRONT_CATEGORIES:
  StorefrontCategory[] = [
  {
    categoryId:
      "00000000-0000-0000-0000-000000000101",

    name:
      "الأزياء",

    slug:
      "fashion",

    parentCategoryId:
      null,

    sortOrder:
      1,

    imageUrl:
      null,
  },

  {
    categoryId:
      "00000000-0000-0000-0000-000000000102",

    name:
      "الإكسسوارات",

    slug:
      "accessories",

    parentCategoryId:
      null,

    sortOrder:
      2,

    imageUrl:
      null,
  },

  {
    categoryId:
      "00000000-0000-0000-0000-000000000103",

    name:
      "الأحذية",

    slug:
      "shoes",

    parentCategoryId:
      null,

    sortOrder:
      3,

    imageUrl:
      null,
  },

  {
    categoryId:
      "00000000-0000-0000-0000-000000000104",

    name:
      "العطور",

    slug:
      "fragrance",

    parentCategoryId:
      null,

    sortOrder:
      4,

    imageUrl:
      null,
  },
];

export const DEMO_STOREFRONT_PRODUCTS:
  StorefrontProductSummary[] = [
  {
    productId:
      "00000000-0000-0000-0000-000000001001",

    name:
      "ساعة Atelier 01",

    slug:
      "atelier-01",

    description:
      null,

    categoryId:
      "00000000-0000-0000-0000-000000000102",

    price:
      1490,

    currency:
      "SAR",

    compareAtPrice:
      null,

    availableForSale:
      true,

    primaryImageUrl:
      "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=900&q=88",

    primaryImageAltText:
      "ساعة Atelier 01",
  },

  {
    productId:
      "00000000-0000-0000-0000-000000001002",

    name:
      "Runner 02",

    slug:
      "runner-02",

    description:
      null,

    categoryId:
      "00000000-0000-0000-0000-000000000103",

    price:
      620,

    currency:
      "SAR",

    compareAtPrice:
      760,

    availableForSale:
      true,

    primaryImageUrl:
      "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=900&q=88",

    primaryImageAltText:
      "Runner 02",
  },

  {
    productId:
      "00000000-0000-0000-0000-000000001003",

    name:
      "No. 04 Eau de Parfum",

    slug:
      "no-04-eau-de-parfum",

    description:
      null,

    categoryId:
      "00000000-0000-0000-0000-000000000104",

    price:
      440,

    currency:
      "SAR",

    compareAtPrice:
      null,

    availableForSale:
      true,

    primaryImageUrl:
      "https://images.unsplash.com/photo-1541643600914-78b084683601?auto=format&fit=crop&w=900&q=88",

    primaryImageAltText:
      "No. 04 Eau de Parfum",
  },

  {
    productId:
      "00000000-0000-0000-0000-000000001004",

    name:
      "Leather Carry",

    slug:
      "leather-carry",

    description:
      null,

    categoryId:
      "00000000-0000-0000-0000-000000000102",

    price:
      880,

    currency:
      "SAR",

    compareAtPrice:
      null,

    availableForSale:
      true,

    primaryImageUrl:
      "https://images.unsplash.com/photo-1584917865442-de89df76afd3?auto=format&fit=crop&w=900&q=88",

    primaryImageAltText:
      "Leather Carry",
  },

  {
    productId:
      "00000000-0000-0000-0000-000000001005",

    name:
      "Essential Linen",

    slug:
      "essential-linen",

    description:
      null,

    categoryId:
      "00000000-0000-0000-0000-000000000101",

    price:
      390,

    currency:
      "SAR",

    compareAtPrice:
      null,

    availableForSale:
      true,

    primaryImageUrl:
      "https://images.unsplash.com/photo-1529139574466-a303027c1d8b?auto=format&fit=crop&w=900&q=88",

    primaryImageAltText:
      "Essential Linen",
  },

  {
    productId:
      "00000000-0000-0000-0000-000000001006",

    name:
      "Soft Structure",

    slug:
      "soft-structure",

    description:
      null,

    categoryId:
      "00000000-0000-0000-0000-000000000101",

    price:
      510,

    currency:
      "SAR",

    compareAtPrice:
      620,

    availableForSale:
      true,

    primaryImageUrl:
      "https://images.unsplash.com/photo-1483985988355-763728e1935b?auto=format&fit=crop&w=900&q=88",

    primaryImageAltText:
      "Soft Structure",
  },
];