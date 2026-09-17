export interface MerchantAnalyticsCurrency {
  currency: string;
  paidOrders: number;
  capturedSales: number;
  averageOrderValue: number;
}

export interface MerchantAnalyticsTopProduct {
  productId: string;
  productName: string;
  currency: string;
  quantitySold: number;
  capturedSales: number;
}

export interface MerchantAnalytics {
  fromUtc: string;
  toUtc: string;
  totalOrders: number;
  cancelledOrders: number;
  sales: MerchantAnalyticsCurrency[];
  topProducts: MerchantAnalyticsTopProduct[];
}

export interface MerchantOrderSummary {
  orderId: string;
  customerUserId: string;
  customerEmail: string | null;
  orderStatus: string;
  paymentStatus: string;
  fulfillmentStatus: string;
  currency: string;
  totalQuantity: number;
  totalAmount: number;
  shippingCarrier: string | null;
  trackingNumber: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface MerchantOrderItem {
  orderItemId: string;
  productId: string;
  productVariantId: string;
  productName: string;
  variantName: string;
  sku: string;
  unitPrice: number;
  currency: string;
  quantity: number;
  lineTotal: number;
}

export interface MerchantOrderTimelineEntry {
  type: string;
  orderStatus: string;
  fulfillmentStatus: string;
  note: string | null;
  createdAtUtc: string;
}

export interface MerchantOrderDetail extends MerchantOrderSummary {
  cancellationReason: string | null;
  shippedAtUtc: string | null;
  deliveredAtUtc: string | null;
  cancelledAtUtc: string | null;
  items: MerchantOrderItem[];
  timeline: MerchantOrderTimelineEntry[];
}

export class AdminOperationApiError extends Error {
  readonly status: number;
  readonly code: string | null;

  constructor(
    message: string,
    status: number,
    code: string | null = null,
  ) {
    super(message);
    this.name = "AdminOperationApiError";
    this.status = status;
    this.code = code;
  }
}

const apiBaseUrl = (
  import.meta.env.VITE_API_BASE_URL ?? ""
).replace(/\/+$/, "");

function getHeaders() {
  const token =
    window.localStorage.getItem("ofoq.access-token") ?? "";

  return {
    "Content-Type": "application/json",
    ...(token
      ? {
          Authorization: `Bearer ${token}`,
        }
      : {}),
  };
}

async function throwApiError(response: Response): Promise<never> {
  let code: string | null = null;
  let message = "تعذر تنفيذ العملية.";

  try {
    const payload = (await response.json()) as {
      code?: unknown;
      message?: unknown;
      title?: unknown;
    };

    if (typeof payload.code === "string") {
      code = payload.code;
    }

    if (typeof payload.message === "string" && payload.message.trim()) {
      message = payload.message;
    } else if (typeof payload.title === "string" && payload.title.trim()) {
      message = payload.title;
    }
  } catch {
    // HTTP status is enough when the body is not JSON.
  }

  if (response.status === 401) {
    message = "انتهت جلسة تسجيل الدخول. سجل دخولك من جديد.";
  }

  if (response.status === 403) {
    message = "أكمل حماية الحساب بالتحقق بخطوتين للوصول إلى بيانات التشغيل.";
  }

  throw new AdminOperationApiError(message, response.status, code);
}

function tenantBackofficeUrl(tenantId: string) {
  return `${apiBaseUrl}/api/tenants/${encodeURIComponent(tenantId)}/backoffice`;
}

export async function getMerchantAnalytics(
  tenantId: string,
  fromUtc: string,
  toUtc: string,
): Promise<MerchantAnalytics> {
  const params = new URLSearchParams({
    fromUtc,
    toUtc,
    topProducts: "5",
  });

  const response = await fetch(
    `${tenantBackofficeUrl(tenantId)}/analytics/summary?${params.toString()}`,
    {
      method: "GET",
      credentials: "include",
      headers: getHeaders(),
    },
  );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as MerchantAnalytics;
}

export async function getMerchantOrders(
  tenantId: string,
  take = 100,
): Promise<MerchantOrderSummary[]> {
  const params = new URLSearchParams({
    take: String(take),
  });

  const response = await fetch(
    `${tenantBackofficeUrl(tenantId)}/orders/?${params.toString()}`,
    {
      method: "GET",
      credentials: "include",
      headers: getHeaders(),
    },
  );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as MerchantOrderSummary[];
}

export async function getMerchantOrderById(
  tenantId: string,
  orderId: string,
): Promise<MerchantOrderDetail> {
  const response = await fetch(
    `${tenantBackofficeUrl(tenantId)}/orders/${encodeURIComponent(orderId)}`,
    {
      method: "GET",
      credentials: "include",
      headers: getHeaders(),
    },
  );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as MerchantOrderDetail;
}

export interface MerchantDashboardReadiness {
  percentage: number;
  state: string;
  storeStatus: string;
}

export interface MerchantDashboardRecentOrder {
  orderId: string;
  status: string;
  fulfillmentStatus: string;
  currency: string;
  totalAmount: number;
  createdAtUtc: string;
}

export interface MerchantDashboardLowStock {
  productId: string;
  variantId: string;
  productName: string;
  variantName: string;
  sku: string;
  quantity: number;
  lowStockThreshold: number;
}

export interface MerchantDashboardAbandonedCart {
  cartId: string;
  customerUserId: string | null;
  currency: string | null;
  totalQuantity: number;
  totalAmount: number;
  lastActivityAtUtc: string;
}

export interface MerchantDashboardTopProduct {
  productId: string;
  productName: string;
  currency: string;
  quantitySold: number;
  capturedSales: number;
}

export interface MerchantOperationsDashboard {
  readiness: MerchantDashboardReadiness;
  openOrders: number;
  pendingOrders: number;
  paidOrdersAwaitingConfirmation: number;
  lowStockVariants: number;
  abandonedCarts: number;
  abandonedAfterMinutes: number;
  recentOrders: MerchantDashboardRecentOrder[];
  lowStock: MerchantDashboardLowStock[];
  abandonedCartItems: MerchantDashboardAbandonedCart[];
  topProducts: MerchantDashboardTopProduct[];
}

export async function getMerchantDashboardSummary(
  tenantId: string,
  take = 8,
): Promise<MerchantOperationsDashboard> {
  const params = new URLSearchParams({
    take: String(take),
  });

  const response = await fetch(
    `${tenantBackofficeUrl(
      tenantId,
    )}/dashboard/summary?${params.toString()}`,
    {
      method: "GET",
      credentials: "include",
      headers: getHeaders(),
    },
  );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as MerchantOperationsDashboard;
}
