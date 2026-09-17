import { authorizedApiFetch } from "../../auth/authSession";

import type {
  ApiProblem,
  CommerceVerticalResponse,
  CreateTenantResponse,
} from "./storeSetup.types";

export class StoreSetupApiError
  extends Error {
  readonly status:
    number;

  readonly code?:
    string;

  constructor(
    status: number,
    message: string,
    code?: string,
  ) {
    super(
      message,
    );

    this.name =
      "StoreSetupApiError";

    this.status =
      status;

    this.code =
      code;
  }
}

export function storeSetupApiConfigured() {
  return Boolean(
    (
      import.meta.env
        .VITE_API_BASE_URL ??
      ""
    ).trim(),
  );
}


export async function checkTenantSlugAvailability(
  slug: string,
) {
  return request<{
    slug: string;
    available: boolean;
  }>(
    `/api/tenants/slug-availability/${encodeURIComponent(
      slug,
    )}`,
    {
      method: "GET",
    },
  );
}

export async function createTenant(
  input: {
    name: string;
    slug: string;
  },
) {
  return request<CreateTenantResponse>(
    "/api/tenants/",
    {
      method:
        "POST",

      body:
        JSON.stringify(
          input,
        ),
    },
  );
}

export async function configurePrimaryVertical(
  tenantId: string,
  verticalType: string,
) {
  return request<CommerceVerticalResponse>(
    `/api/tenants/${encodeURIComponent(
      tenantId,
    )}/onboarding/commerce/vertical`,
    {
      method:
        "PUT",

      body:
        JSON.stringify({
          verticalType,
          enabled:
            true,
          primary:
            true,
        }),
    },
  );
}

async function request<T>(
  path: string,
  init: RequestInit,
): Promise<T> {
  const response =
    await authorizedApiFetch(
      path,
      {
        ...init,

        headers: {
          "Content-Type":
            "application/json",

          Accept:
            "application/json",

          ...(init.headers ??
            {}),
        },
      },
    );

  if (!response.ok) {
    const problem =
      await tryReadProblem(
        response,
      );

    throw new StoreSetupApiError(
      response.status,
      problem.message ??
        defaultMessage(
          response.status,
        ),
      problem.code,
    );
  }

  return await response.json() as T;
}

async function tryReadProblem(
  response: Response,
): Promise<ApiProblem> {
  try {
    return await response.json() as ApiProblem;
  }
  catch {
    return {};
  }
}

function defaultMessage(
  status: number,
) {
  if (status === 401) {
    return "يجب تسجيل الدخول قبل إنشاء المتجر.";
  }

  if (status === 403) {
    return "لا تملك صلاحية تنفيذ هذه العملية.";
  }

  if (status === 409) {
    return "تعذر الحفظ بسبب تعارض في البيانات.";
  }

  return "تعذر الاتصال بالخادم. حاول مرة أخرى.";
}