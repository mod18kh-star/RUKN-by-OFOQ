import {
  apiUrl,
  authorizedApiFetch,
} from "../../auth/authSession";

export interface AdminCategory {
  categoryId: string;
  name: string;
  slug: string;
  parentCategoryId: string | null;
  sortOrder: number;
  isVisible: boolean;
  createdAtUtc: string;
  imageUrl: string | null;
}

export interface UpsertCategoryInput {
  name: string;
  slug: string;
  parentCategoryId: string | null;
  imageUrl: string | null;
  isVisible?: boolean;
}

export type ContentPageKind = "Standard" | "Reviews" | "Statistics";

export interface ContentPage {
  id: string;
  title: string;
  slug: string;
  body: string;
  seoTitle: string | null;
  seoDescription: string | null;
  isPublished: boolean;
  publishedAtUtc: string | null;
  createdAtUtc: string;
  pageKind: ContentPageKind;
  heroImageUrl: string | null;
  showCustomerCount: boolean;
  showCompletedOrderCount: boolean;
  showUnitsSold: boolean;
  showAverageRating: boolean;
  showReviewCount: boolean;
  showCountryCount: boolean;
}

export interface UpsertContentPageInput {
  title: string;
  slug: string;
  body: string;
  seoTitle: string | null;
  seoDescription: string | null;
  publish: boolean;
  pageKind: ContentPageKind;
  heroImageUrl: string | null;
  showCustomerCount: boolean;
  showCompletedOrderCount: boolean;
  showUnitsSold: boolean;
  showAverageRating: boolean;
  showReviewCount: boolean;
  showCountryCount: boolean;
}

export class CatalogContentApiError extends Error {
  readonly status: number;
  readonly code: string | null;

  constructor(
    message: string,
    status: number,
    code: string | null = null,
  ) {
    super(message);
    this.name = "CatalogContentApiError";
    this.status = status;
    this.code = code;
  }
}

async function throwApiError(
  response: Response,
): Promise<never> {
  let code: string | null = null;
  let message = "تعذر تنفيذ العملية.";

  try {
    const payload =
      (await response.json()) as {
        code?: unknown;
        message?: unknown;
        title?: unknown;
      };

    if (typeof payload.code === "string") {
      code = payload.code;
    }

    if (
      typeof payload.message === "string" &&
      payload.message.trim()
    ) {
      message = payload.message;
    } else if (
      typeof payload.title === "string" &&
      payload.title.trim()
    ) {
      message = payload.title;
    }
  } catch {
    // Status text is enough when the server did not send JSON.
  }

  if (response.status === 401) {
    message = "انتهت جلسة تسجيل الدخول. سجل دخولك من جديد.";
  }

  if (response.status === 403) {
    message = "ما عندك صلاحية لتنفيذ هذا الإجراء على المتجر الحالي.";
  }

  throw new CatalogContentApiError(
    message,
    response.status,
    code,
  );
}

function categoriesPath(
  tenantId: string,
) {
  return `/api/tenants/${encodeURIComponent(
    tenantId,
  )}/backoffice/categories`;
}

function pagesPath(
  tenantId: string,
) {
  return `/api/tenants/${encodeURIComponent(
    tenantId,
  )}/backoffice/pages`;
}

export async function getCategories(
  tenantId: string,
) {
  const response =
    await authorizedApiFetch(
      `${categoriesPath(tenantId)}/`,
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as AdminCategory[];
}

export async function createCategory(
  tenantId: string,
  input: UpsertCategoryInput,
) {
  const response =
    await authorizedApiFetch(
      `${categoriesPath(tenantId)}/`,
      {
        method: "POST",
        body: JSON.stringify({
          name: input.name,
          slug: input.slug,
          parentCategoryId:
            input.parentCategoryId,
          position: null,
          sortOrder: null,
          imageUrl: input.imageUrl,
        }),
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as AdminCategory;
}

export async function updateCategory(
  tenantId: string,
  categoryId: string,
  input: UpsertCategoryInput,
) {
  const response =
    await authorizedApiFetch(
      `${categoriesPath(tenantId)}/${encodeURIComponent(
        categoryId,
      )}`,
      {
        method: "PUT",
        body: JSON.stringify({
          name: input.name,
          slug: input.slug,
          isVisible:
            input.isVisible ?? true,
          imageUrl: input.imageUrl,
        }),
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as AdminCategory;
}

export async function moveCategory(
  tenantId: string,
  categoryId: string,
  parentCategoryId: string | null,
) {
  const response =
    await authorizedApiFetch(
      `${categoriesPath(tenantId)}/${encodeURIComponent(
        categoryId,
      )}/placement`,
      {
        method: "PUT",
        body: JSON.stringify({
          parentCategoryId,
          position: null,
        }),
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as AdminCategory;
}

export async function getContentPages(
  tenantId: string,
) {
  const response =
    await authorizedApiFetch(
      pagesPath(tenantId),
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as ContentPage[];
}

export async function createContentPage(
  tenantId: string,
  input: UpsertContentPageInput,
) {
  const response =
    await authorizedApiFetch(
      pagesPath(tenantId),
      {
        method: "POST",
        body: JSON.stringify(input),
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as ContentPage;
}

export async function updateContentPage(
  tenantId: string,
  pageId: string,
  input: UpsertContentPageInput,
) {
  const response =
    await authorizedApiFetch(
      `${pagesPath(tenantId)}/${encodeURIComponent(
        pageId,
      )}`,
      {
        method: "PUT",
        body: JSON.stringify(input),
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as ContentPage;
}


export async function uploadContentPageAsset(
  tenantId: string,
  file: File,
) {
  const formData = new FormData();
  formData.set("file", file);

  const response = await authorizedApiFetch(
    `${pagesPath(tenantId)}/asset`,
    {
      method: "POST",
      body: formData,
      headers: undefined,
    },
  );

  if (!response.ok) {
    return throwApiError(response);
  }

  const payload = (await response.json()) as {
    assetUrl: string;
  };

  return apiUrl(payload.assetUrl);
}

export async function deleteContentPage(
  tenantId: string,
  pageId: string,
) {
  const response =
    await authorizedApiFetch(
      `${pagesPath(tenantId)}/${encodeURIComponent(
        pageId,
      )}`,
      {
        method: "DELETE",
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }
}

export interface NavigationItem {
  id: string;
  location: "Header" | "Footer";
  type: "Page" | "Category" | "Product" | "External";
  label: string;
  targetId: string | null;
  externalUrl: string | null;
  parentItemId: string | null;
  sortOrder: number;
  isVisible: boolean;
}

export interface UpsertNavigationItemInput {
  location: "Header" | "Footer";
  type: "Page" | "Category" | "Product" | "External";
  label: string;
  targetId: string | null;
  externalUrl: string | null;
  parentItemId: string | null;
  position: number | null;
  isVisible: boolean;
}

function navigationPath(
  tenantId: string,
) {
  return `/api/tenants/${encodeURIComponent(
    tenantId,
  )}/backoffice/navigation`;
}

export async function getNavigationItems(
  tenantId: string,
) {
  const response =
    await authorizedApiFetch(
      navigationPath(tenantId),
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as NavigationItem[];
}

export async function createNavigationItem(
  tenantId: string,
  input: UpsertNavigationItemInput,
) {
  const response =
    await authorizedApiFetch(
      navigationPath(tenantId),
      {
        method: "POST",
        body: JSON.stringify(input),
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as NavigationItem;
}

export async function updateNavigationItem(
  tenantId: string,
  navigationItemId: string,
  input: UpsertNavigationItemInput,
) {
  const response =
    await authorizedApiFetch(
      `${navigationPath(tenantId)}/${encodeURIComponent(
        navigationItemId,
      )}`,
      {
        method: "PUT",
        body: JSON.stringify(input),
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as NavigationItem;
}

export async function deleteNavigationItem(
  tenantId: string,
  navigationItemId: string,
) {
  const response =
    await authorizedApiFetch(
      `${navigationPath(tenantId)}/${encodeURIComponent(
        navigationItemId,
      )}`,
      {
        method: "DELETE",
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }
}
