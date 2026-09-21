import { authorizedApiFetch } from "../../auth/authSession";

export type CouponType = "Percentage" | "FixedAmount";
export type CouponScope = "EntireStore" | "Categories" | "Products";

export interface Coupon {
  id: string;
  code: string;
  name: string;
  type: CouponType;
  value: number;
  scope: CouponScope;
  currency: string;
  includeDescendantCategories: boolean;
  minimumOrderAmount: number | null;
  maximumTotalUses: number | null;
  maximumUsesPerCustomer: number | null;
  startsAtUtc: string | null;
  endsAtUtc: string | null;
  isEnabled: boolean;
  productIds: string[];
  categoryIds: string[];
}
export type CouponInput = Omit<Coupon, "id">;

export interface CouponUse {
  orderId: string;
  redeemedAtUtc: string;
  discountAmount: number;
  orderAmount: number;
  status: string;
}
export interface CouponAnalytics {
  couponId: string;
  usageCount: number;
  uniqueCustomers: number;
  paidOrderCount: number;
  pendingOrderCount: number;
  cancelledOrderCount: number;
  paidRevenue: number;
  paidDiscountTotal: number;
  pendingRevenue: number;
  recentUses: CouponUse[];
}
const base = (tenantId: string) =>
  `/api/tenants/${encodeURIComponent(tenantId)}/backoffice/coupons`;

async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const payload = await response.json().catch(() => null) as { message?: unknown; title?: unknown } | null;
    const message = typeof payload?.message === "string" ? payload.message :
      typeof payload?.title === "string" ? payload.title :
      response.status === 401 ? "انتهت الجلسة؛ يرجى تسجيل الدخول." :
      response.status === 403 ? "لا تملك صلاحية إدارة كوبونات هذا المتجر." :
      "تعذر تنفيذ العملية؛ يرجى المحاولة مجددًا.";
    throw new Error(message);
  }
  return await response.json() as T;
}
export async function listCoupons(tenantId: string) {
  return parse<Coupon[]>(await authorizedApiFetch(base(tenantId)));
}
export async function createCoupon(tenantId: string, input: CouponInput) {
  return parse<Coupon>(await authorizedApiFetch(base(tenantId), {method: "POST", body: JSON.stringify(input)}));
}
export async function updateCoupon(tenantId: string, id: string, input: CouponInput) {
  return parse<Coupon>(await authorizedApiFetch(`${base(tenantId)}/${encodeURIComponent(id)}`, {method: "PUT", body: JSON.stringify(input)}));
}
export async function getCouponAnalytics(tenantId: string, id: string) {
  return parse<CouponAnalytics>(await authorizedApiFetch(`${base(tenantId)}/${encodeURIComponent(id)}/analytics`));
}
