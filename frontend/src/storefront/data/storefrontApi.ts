export interface StorefrontInfo {
  name: string;
  slug: string;

  vertical:
    string | null;

  verticalCode:
    string | null;
}

export interface StorefrontCategory {
  categoryId: string;
  name: string;
  slug: string;

  parentCategoryId:
    string | null;

  sortOrder: number;
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
    string;

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