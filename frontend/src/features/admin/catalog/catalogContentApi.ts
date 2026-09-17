import {
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
}

export interface UpsertContentPageInput {
  title: string;
  slug: string;
  body: string;
  seoTitle: string | null;
  seoDescription: string | null;
  publish: boolean;
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
