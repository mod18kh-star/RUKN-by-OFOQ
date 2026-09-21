import { authorizedApiFetch } from "../../auth/authSession";
import { parseManualResponse } from "../../../storefront/data/manualCheckoutApi";

export interface ManualReviewOrder {
  orderId: string;
  status: "AwaitingReceipt" | "PendingReview" | "Approved" | "Rejected";
  customerName: string;
  customerPhone: string;
  customerUserId: string;
  accountId: string;
  accountName: string;
  amount: number;
  currency: string;
  receiptId: string | null;
  receiptStatus: string | null;
  rejectionReason: string | null;
  updatedAtUtc: string;
}
const base = (tenantId: string) => `/api/tenants/${encodeURIComponent(tenantId)}/backoffice/manual-payments`;
export async function listManualReviews(tenantId: string) {
  return parseManualResponse<ManualReviewOrder[]>(await authorizedApiFetch(`${base(tenantId)}/`));
}
export async function reviewManualPayment(tenantId: string, orderId: string, approve: boolean, reason: string | null) {
  return parseManualResponse<{ orderId: string; status: string; orderStatus: string }>(
    await authorizedApiFetch(`${base(tenantId)}/${encodeURIComponent(orderId)}/review`,
      { method: "POST", body: JSON.stringify({ approve, reason }) }),
  );
}
export async function fetchReviewReceipt(tenantId: string, orderId: string) {
  const response = await authorizedApiFetch(`${base(tenantId)}/${encodeURIComponent(orderId)}/receipt`);
  if (!response.ok) throw new Error("تعذر فتح صورة الإيصال أو انتهت صلاحية الجلسة.");
  const blob = await response.blob();
  if (blob.size > 1024 * 1024 || !["image/png", "image/jpeg"].includes(blob.type))
    throw new Error("صيغة صورة الإيصال غير مقبولة.");
  return URL.createObjectURL(blob);
}
