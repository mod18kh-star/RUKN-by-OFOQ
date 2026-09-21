export interface StorefrontPresentation {
  logoUrl: string | null;
  coverImageUrl: string | null;
  announcement: string | null;

  primaryColor:
    string | null;

  accentColor:
    string | null;

  themePresetCode: string;
  fontCode: string;

  showCategoriesOnHome: boolean;
  showProductsOnHome: boolean;

  categorySectionTitle: string;
  productSectionTitle: string;
  visualContentJson?: string;
}

export interface StorefrontSocialLink {
  platformCode: string;
  label: string | null;
  url: string;
  sortOrder: number;
}

export interface StorefrontContact {
  websiteUrl: string | null;
  whatsAppNumber: string | null;
  customerServicePhone: string | null;
  secondaryPhone: string | null;
  landlinePhone: string | null;
  physicalAddress: string | null;
  googleMapsUrl: string | null;
  commercialRegistrationNumber: string | null;
  socialLinks: StorefrontSocialLink[];
}

export const EMPTY_STOREFRONT_CONTACT: StorefrontContact = {
  websiteUrl: null,
  whatsAppNumber: null,
  customerServicePhone: null,
  secondaryPhone: null,
  landlinePhone: null,
  physicalAddress: null,
  googleMapsUrl: null,
  commercialRegistrationNumber: null,
  socialLinks: [],
};

export interface StorefrontInfo {
  tenantId: string;
  name: string;
  slug: string;

  vertical:
    string | null;

  verticalCode:
    string | null;

  presentation:
    StorefrontPresentation;

  contact:
    StorefrontContact;
}

export interface StorefrontCategory {
  categoryId: string;
  name: string;
  slug: string;

  parentCategoryId:
    string | null;

  sortOrder: number;

  imageUrl:
    string | null;
}

export interface StorefrontContentPage {
  id: string;
  title: string;
  slug: string;
  body: string;

  seoTitle:
    string | null;

  seoDescription:
    string | null;

  publishedAtUtc:
    string | null;

  pageKind:
    "Standard" | "Reviews" | "Statistics";

  heroImageUrl:
    string | null;
}

export interface StorefrontProductSummary {
  productId: string;
  name: string;
  slug: string;

  description:
    string | null;

  categoryId:
    string | null;

  price: number;
  currency: string;

  compareAtPrice:
    number | null;

  availableForSale:
    boolean;

  primaryImageUrl:
    string | null;

  primaryImageAltText:
    string | null;
}

export interface StorefrontProductPage {
  page: number;
  pageSize: number;

  totalCount: number;
  totalPages: number;

  items:
    StorefrontProductSummary[];
}

const rawApiBaseUrl =
  import.meta.env
    .VITE_API_BASE_URL
    ?.trim()
  ?? "";

export const storefrontApiIsConfigured =
  rawApiBaseUrl.length >
  0;

const apiBaseUrl =
  rawApiBaseUrl.replace(
    /\/+$/,
    "",
  );

function storefrontUrl(
  path: string,
) {
  return `${apiBaseUrl}${path}`;
}

async function fetchJson<T>(
  path: string,
): Promise<T> {
  const response =
    await fetch(
      storefrontUrl(
        path,
      ),
      {
        method:
          "GET",

        headers: {
          Accept:
            "application/json",
        },
      },
    );

  if (!response.ok) {
    throw new Error(
      `Storefront request failed with status ${response.status}.`,
    );
  }

  return response.json() as
    Promise<T>;
}

function storePath(
  storeSlug: string,
) {
  return `/api/storefront/${encodeURIComponent(
    storeSlug,
  )}`;
}

export function getStorefrontInfo(
  storeSlug: string,
) {
  return fetchJson<
    StorefrontInfo
  >(
    storePath(
      storeSlug,
    ),
  );
}

export function getStorefrontCategories(
  storeSlug: string,
) {
  return fetchJson<
    StorefrontCategory[]
  >(
    `${storePath(
      storeSlug,
    )}/categories`,
  );
}

export function getStorefrontProducts(
  storeSlug: string,
  options?: {
    search?: string;
    category?: string;
    page?: number;
    pageSize?: number;
  },
) {
  const params =
    new URLSearchParams();

  if (
    options?.search
  ) {
    params.set(
      "search",
      options.search,
    );
  }

  if (
    options?.category
  ) {
    params.set(
      "category",
      options.category,
    );
  }

  params.set(
    "page",
    String(
      options?.page ??
        1,
    ),
  );

  params.set(
    "pageSize",
    String(
      options?.pageSize ??
        24,
    ),
  );

  return fetchJson<
    StorefrontProductPage
  >(
    `${storePath(
      storeSlug,
    )}/products?${params.toString()}`,
  );
}

export function getStorefrontContentPage(
  storeSlug: string,
  pageSlug: string,
) {
  return fetchJson<
    StorefrontContentPage
  >(
    `${storePath(
      storeSlug,
    )}/pages/${encodeURIComponent(
      pageSlug,
    )}`,
  );
}


export interface StorefrontPageStatistics {
  customerCount: number | null;
  completedOrderCount: number | null;
  unitsSold: number | null;
  averageRating: number | null;
  reviewCount: number | null;
  countryCount: number | null;
}

export interface StorefrontReview {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  rating: number;
  body: string | null;
  isVerifiedPurchase: boolean;
  merchantReply: string | null;
  createdAtUtc: string;
}

export interface StorefrontReviewPage {
  averageRating: number;
  reviewCount: number;
  reviews: StorefrontReview[];
}

export function getStorefrontPageStatistics(
  storeSlug: string,
  pageSlug: string,
) {
  return fetchJson<StorefrontPageStatistics>(
    `${storePath(storeSlug)}/pages/${encodeURIComponent(pageSlug)}/statistics`,
  );
}

export function getStorefrontStoreReviews(
  storeSlug: string,
  take = 24,
) {
  const params = new URLSearchParams({ take: String(take) });
  return fetchJson<StorefrontReviewPage>(
    `${storePath(storeSlug)}/reviews?${params.toString()}`,
  );
}

export interface StorefrontProductImage {
  imageId: string;
  url: string;
  altText: string | null;
  sortOrder: number;
  isPrimary: boolean;
}

export interface StorefrontVariant {
  variantId: string;
  name: string;
  sku: string;
  isDefault: boolean;
  price: number;
  currency: string;
  trackInventory: boolean;
  quantity: number | null;
  continueSellingWhenOutOfStock: boolean;
  availableForSale: boolean;
}

export interface StorefrontProductAttribute {
  key: string;
  label: string;
  valueType: string;
  value: string;
}

export interface StorefrontProductDetail {
  productId: string;
  name: string;
  slug: string;
  description: string | null;
  categoryId: string | null;
  categoryName: string | null;
  categorySlug: string | null;
  price: number;
  currency: string;
  compareAtPrice: number | null;
  availableForSale: boolean;
  primaryImageUrl: string;
  primaryImageAltText: string | null;
  images: StorefrontProductImage[];
  variants: StorefrontVariant[];
  attributes: StorefrontProductAttribute[];
}

export function getStorefrontProduct(
  storeSlug: string,
  productSlug: string,
) {
  return fetchJson<StorefrontProductDetail>(
    `${storePath(storeSlug)}/products/${encodeURIComponent(productSlug)}`,
  );
}

export interface StorefrontNavigationItem {
  id: string;
  location: string;
  type: string;
  label: string;
  targetId: string | null;
  externalUrl: string | null;
  parentItemId: string | null;
  sortOrder: number;
  href: string;
}

export function getStorefrontNavigation(
  storeSlug: string,
  location: "Header" | "Footer" = "Header",
) {
  const params = new URLSearchParams({ location });

  return fetchJson<StorefrontNavigationItem[]>(
    `${storePath(storeSlug)}/navigation?${params.toString()}`,
  );
}
