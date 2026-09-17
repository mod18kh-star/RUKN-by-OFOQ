import { authorizedApiFetch } from "../auth/authSession";

export interface PlatformMe {
  userId: string;
  role: string;
  backendRole: string;
  environment: string;
  mfaRequired: boolean;
}

export interface PlatformAdministrator {
  userId: string;
  email: string;
  status: string;
  emailVerified: boolean;
  addedAtUtc: string;
}

export interface PlatformAdministratorMutation {
  userId: string;
  email: string;
  alreadyAdministrator: boolean;
}

export interface PlatformStoreSummary {
  tenantId: string;
  name: string;
  slug: string;
  status: string;
  ownerUserId: string | null;
  ownerEmail: string | null;
  primaryVertical: string | null;
  primaryVerticalCode: string | null;
  planCode: string | null;
  planName: string | null;
  subscriptionStatus: string | null;
  billingCycle: string | null;
  pendingRequests: number;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface PlatformVerticalOption {
  vertical: string;
  code: string;
}

export interface PlatformPlanOption {
  code: string;
  name: string;
  rank: number;
}

export interface PlatformCapability {
  capability: string;
  defaultEnabled: boolean;
  effectiveEnabled: boolean;
  overrideValue: boolean | null;
}

export interface PlatformAuditEntry {
  auditEntryId: string;
  actorUserId: string;
  action: string;
  reason: string;
  oldValueJson: string | null;
  newValueJson: string | null;
  occurredAtUtc: string;
}

export interface PlatformStoreDetail
  extends PlatformStoreSummary {
  teamSize: number;
  productsCount: number;
  ordersCount: number;
  availablePlans: PlatformPlanOption[];
  availableVerticals: PlatformVerticalOption[];
  capabilities: PlatformCapability[];
  recentAudit: PlatformAuditEntry[];
}

export interface PlatformRequestSummary {
  requestId: string;
  tenantId: string;
  tenantName: string;
  type: string;
  status: string;
  summary: string;
  requestedByEmail: string | null;
  requestedAtUtc: string;
  reviewedAtUtc: string | null;
  reviewReason: string | null;
}

export interface PlatformRequestDetail
  extends PlatformRequestSummary {
  tenantSlug: string;
  payloadJson: string;
  requestedByUserId: string;
  reviewedByUserId: string | null;
  reviewedByEmail: string | null;
}

export interface PlatformRequestStats {
  pending: number;
  moreInfoRequested: number;
}

export class PlatformApiError extends Error {
  readonly status: number;
  readonly code: string | null;

  constructor(
    message: string,
    status: number,
    code: string | null = null,
  ) {
    super(message);
    this.name = "PlatformApiError";
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
          "Content-Type":
            "application/json",
          ...(init?.headers ?? {}),
        },
      },
    );

  if (!response.ok) {
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
      // Keep the fallback message.
    }

    if (response.status === 401) {
      message =
        "جلسة تسجيل الدخول غير صالحة. سجل دخولك من جديد.";
    }

    if (response.status === 403) {
      message =
        "هذا الحساب لا يملك صلاحية Super Admin.";
    }

    const messages: Record<string, string> = {
      platform_action_reason_required:
        "اكتب سبب الإجراء قبل الحفظ.",
      store_slug_already_exists:
        "رابط المتجر مستخدم لمتجر آخر.",
      owner_email_already_exists:
        "البريد مستخدم بحساب آخر.",
      owner_account_not_found:
        "لا يوجد حساب مستخدم فعال بهذا البريد. أنشئ الحساب أولًا ثم اربطه كمالك.",
      platform_request_completed:
        "هذا الطلب أُغلق ولا يمكن تعديل قراره.",
      platform_user_not_found:
        "لا يوجد حساب ركن مسجل بهذا البريد. أنشئ الحساب أولًا ثم أضفه لفريق المنصة.",
      platform_user_not_active:
        "هذا الحساب غير فعال حاليًا ولا يمكن منحه صلاحية Super Admin.",
      platform_administrator_email_invalid:
        "تأكد من كتابة البريد الإلكتروني بشكل صحيح.",
    };

    if (code && messages[code]) {
      message = messages[code];
    }

    throw new PlatformApiError(
      message,
      response.status,
      code,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function getPlatformMe() {
  return request<PlatformMe>(
    "/api/platform/me",
  );
}

export function bootstrapFirstPlatformAdmin() {
  return request<{
    role: string;
    alreadyConfigured: boolean;
  }>(
    "/api/platform/bootstrap/first-admin",
    { method: "POST" },
  );
}

export function getPlatformAdministrators() {
  return request<PlatformAdministrator[]>(
    "/api/platform/administrators",
  );
}

export function addPlatformAdministrator(
  email: string,
) {
  return request<PlatformAdministratorMutation>(
    "/api/platform/administrators",
    {
      method: "POST",
      body: JSON.stringify({
        email,
      }),
    },
  );
}

export function getPlatformStores() {
  return request<PlatformStoreSummary[]>(
    "/api/platform/stores",
  );
}

export function getPlatformStore(
  tenantId: string,
) {
  return request<PlatformStoreDetail>(
    `/api/platform/stores/${encodeURIComponent(
      tenantId,
    )}`,
  );
}

export function suspendPlatformStore(
  tenantId: string,
  reason: string,
) {
  return request<{
    tenantId: string;
    status: string;
  }>(
    `/api/platform/stores/${encodeURIComponent(
      tenantId,
    )}/suspend`,
    {
      method: "POST",
      body: JSON.stringify({ reason }),
    },
  );
}

export function activatePlatformStore(
  tenantId: string,
  reason: string,
) {
  return request<{
    tenantId: string;
    status: string;
  }>(
    `/api/platform/stores/${encodeURIComponent(
      tenantId,
    )}/activate`,
    {
      method: "POST",
      body: JSON.stringify({ reason }),
    },
  );
}

export function updatePlatformStoreIdentity(
  tenantId: string,
  input: {
    name: string;
    slug: string;
    reason: string;
  },
) {
  return request<{
    tenantId: string;
    name: string;
    slug: string;
  }>(
    `/api/platform/stores/${encodeURIComponent(
      tenantId,
    )}/identity`,
    {
      method: "PUT",
      body: JSON.stringify(input),
    },
  );
}

export function changePlatformStoreVertical(
  tenantId: string,
  input: {
    verticalCode: string;
    reason: string;
  },
) {
  return request<{
    tenantId: string;
    vertical: string;
    verticalCode: string;
  }>(
    `/api/platform/stores/${encodeURIComponent(
      tenantId,
    )}/primary-vertical`,
    {
      method: "PUT",
      body: JSON.stringify(input),
    },
  );
}

export function setPlatformStoreCapability(
  tenantId: string,
  capability: string,
  input: {
    isEnabled: boolean | null;
    reason: string;
  },
) {
  return request<PlatformCapability>(
    `/api/platform/stores/${encodeURIComponent(
      tenantId,
    )}/capabilities/${encodeURIComponent(
      capability,
    )}`,
    {
      method: "PUT",
      body: JSON.stringify(input),
    },
  );
}

export function setPlatformOwnerEmail(
  tenantId: string,
  input: {
    email: string;
    reason: string;
  },
) {
  return request<{
    tenantId: string;
    ownerUserId: string;
    ownerEmail: string;
  }>(
    `/api/platform/stores/${encodeURIComponent(
      tenantId,
    )}/owner-email`,
    {
      method: "PUT",
      body: JSON.stringify(input),
    },
  );
}

export function setPlatformStoreSubscription(
  tenantId: string,
  input: {
    planCode: string;
    billingCycle: "Monthly" | "Annual";
    status: "Active" | "Suspended";
    reason: string;
  },
) {
  return request<{
    planCode: string;
    planName: string;
    billingCycle: string;
    status: string;
    startedAtUtc: string;
    endsAtUtc: string | null;
  }>(
    `/api/platform/stores/${encodeURIComponent(
      tenantId,
    )}/subscription`,
    {
      method: "PUT",
      body: JSON.stringify(input),
    },
  );
}

export function getPlatformRequestStats() {
  return request<PlatformRequestStats>(
    "/api/platform/requests/stats",
  );
}

export function getPlatformRequests(
  filters?: {
    status?: string;
    type?: string;
    search?: string;
  },
) {
  const params = new URLSearchParams();

  if (filters?.status) {
    params.set("status", filters.status);
  }

  if (filters?.type) {
    params.set("type", filters.type);
  }

  if (filters?.search?.trim()) {
    params.set("search", filters.search.trim());
  }

  const query = params.toString();

  return request<PlatformRequestSummary[]>(
    `/api/platform/requests${query ? `?${query}` : ""}`,
  );
}

export function getPlatformRequest(
  requestId: string,
) {
  return request<PlatformRequestDetail>(
    `/api/platform/requests/${encodeURIComponent(
      requestId,
    )}`,
  );
}

export function approvePlatformRequest(
  requestId: string,
  reason: string,
) {
  return request<{
    requestId: string;
    status: string;
  }>(
    `/api/platform/requests/${encodeURIComponent(
      requestId,
    )}/approve`,
    {
      method: "POST",
      body: JSON.stringify({ reason }),
    },
  );
}

export function rejectPlatformRequest(
  requestId: string,
  reason: string,
) {
  return request<{
    requestId: string;
    status: string;
  }>(
    `/api/platform/requests/${encodeURIComponent(
      requestId,
    )}/reject`,
    {
      method: "POST",
      body: JSON.stringify({ reason }),
    },
  );
}

export function requestMoreInfoForPlatformRequest(
  requestId: string,
  reason: string,
) {
  return request<{
    requestId: string;
    status: string;
  }>(
    `/api/platform/requests/${encodeURIComponent(
      requestId,
    )}/request-info`,
    {
      method: "POST",
      body: JSON.stringify({ reason }),
    },
  );
}
