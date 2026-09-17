import { authorizedApiFetch } from "../../auth/authSession";

export interface MerchantPlatformRequest {
  requestId: string;
  type: string;
  status: string;
  summary: string;
  payloadJson: string;
  requestedAtUtc: string;
  reviewedAtUtc: string | null;
  reviewReason: string | null;
}

export class MerchantRequestApiError extends Error {
  readonly status: number;
  readonly code: string | null;

  constructor(
    message: string,
    status: number,
    code: string | null,
  ) {
    super(message);
    this.name = "MerchantRequestApiError";
    this.status = status;
    this.code = code;
  }
}

async function request<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const response =
    await authorizedApiFetch(
      path,
      {
        ...init,
        headers: {
          "Content-Type": "application/json",
          ...(init?.headers ?? {}),
        },
      },
    );

  if (!response.ok) {
    let code: string | null = null;
    let message = "تعذر إرسال الطلب.";

    try {
      const body = await response.json() as {
        code?: unknown;
        message?: unknown;
      };

      if (typeof body.code === "string") {
        code = body.code;
      }

      if (
        typeof body.message === "string" &&
        body.message.trim()
      ) {
        message = body.message;
      }
    } catch {
      // Keep fallback.
    }

    const friendly: Record<string, string> = {
      platform_request_already_pending:
        "عندك طلب من نفس النوع بانتظار المراجعة.",
      request_reason_invalid:
        "اكتب سببًا واضحًا للطلب.",
      owner_email_invalid:
        "البريد الإلكتروني غير صالح.",
      store_change_empty:
        "حدد على الأقل معلومة واحدة تريد تغييرها.",
    };

    if (code && friendly[code]) {
      message = friendly[code];
    }

    throw new MerchantRequestApiError(
      message,
      response.status,
      code,
    );
  }

  return await response.json() as T;
}

export function getMyPlatformRequests(
  tenantId: string,
) {
  return request<MerchantPlatformRequest[]>(
    `/api/tenants/${encodeURIComponent(
      tenantId,
    )}/requests/mine`,
  );
}

export function submitRegistrationRequest(
  tenantId: string,
  input: {
    planCode: string;
    billingCycle: "Monthly" | "Annual";
  },
) {
  return request<MerchantPlatformRequest>(
    `/api/tenants/${encodeURIComponent(
      tenantId,
    )}/requests/registration`,
    {
      method: "POST",
      body: JSON.stringify(input),
    },
  );
}

export function submitPlanChangeRequest(
  tenantId: string,
  input: {
    planCode: string;
    billingCycle: "Monthly" | "Annual";
    reason: string;
  },
) {
  return request<MerchantPlatformRequest>(
    `/api/tenants/${encodeURIComponent(
      tenantId,
    )}/requests/plan-change`,
    {
      method: "POST",
      body: JSON.stringify(input),
    },
  );
}

export function submitStoreIdentityChangeRequest(
  tenantId: string,
  input: {
    name: string | null;
    slug: string | null;
    verticalCode: string | null;
    reason: string;
  },
) {
  return request<MerchantPlatformRequest>(
    `/api/tenants/${encodeURIComponent(
      tenantId,
    )}/requests/store-identity`,
    {
      method: "POST",
      body: JSON.stringify(input),
    },
  );
}

export function submitOwnerEmailChangeRequest(
  tenantId: string,
  input: {
    email: string;
    reason: string;
  },
) {
  return request<MerchantPlatformRequest>(
    `/api/tenants/${encodeURIComponent(
      tenantId,
    )}/requests/owner-email`,
    {
      method: "POST",
      body: JSON.stringify(input),
    },
  );
}
