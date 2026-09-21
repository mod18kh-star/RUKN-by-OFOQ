import { authorizedApiFetch } from "../../features/auth/authSession";
import { getStorefrontInfo } from "./storefrontApi";

export class ManualHttpError extends Error {
  readonly status: number;
  constructor(message: string, status: number) { super(message); this.name = "ManualHttpError"; this.status = status; }
}

export interface ManualMethodSummary {
  id: string;
  kind: "bank" | "wallet";
  name: string;
  maskedReference: string;
}
export interface ManualCustomerPayment {
  orderId: string;
  status: "AwaitingReceipt" | "PendingReview" | "Approved" | "Rejected";
  accountId: string;
  accountName: string;
  accountKind: string;
  bankName: string | null;
  accountHolder: string | null;
  iban: string | null;
  accountNumber: string | null;
  walletProvider: string | null;
  walletNumber: string | null;
  transferLink: string | null;
  hasQr: boolean;
  amount: number;
  currency: string;
  rejectionReason: string | null;
  lastSubmittedAtUtc: string | null;
}

const isGuid = (value: string) => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
async function endpoint(storeSlug: string) {
  const info = await getStorefrontInfo(storeSlug);
  if (!isGuid(info.tenantId)) throw new Error("تعذر تحديد المتجر.");
  return `/api/tenants/${info.tenantId}/payments/manual-checkout`;
}
export async function parseManualResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let message = response.status === 403 ? "ليس لديك صلاحية الوصول لهذا الطلب." :
      response.status === 404 ? "لم يتم العثور على الطلب أو الحساب." :
      response.status === 413 ? "حجم صورة الإيصال كبير جدًا." :
      "تعذر تنفيذ العملية. حاول مجددًا.";
    try {
      const body = (await response.json()) as { message?: string };
      if (typeof body.message === "string" && body.message.length < 250) message = body.message;
    } catch { /* No internal errors exposed. */ }
    throw new ManualHttpError(message, response.status);
  }
  return (await response.json()) as T;
}
export async function listManualMethods(slug: string) {
  return parseManualResponse<ManualMethodSummary[]>(
    await authorizedApiFetch(`${await endpoint(slug)}/methods`),
  );
}
export async function selectManualMethod(slug: string, orderId: string, accountId: string, customerPhone: string) {
  if (!isGuid(orderId) || !isGuid(accountId)) throw new Error("معرّفات الطلب غير صالحة.");
  return parseManualResponse<ManualCustomerPayment>(await authorizedApiFetch(
    `${await endpoint(slug)}/orders/${orderId}/select`,
    { method: "POST", body: JSON.stringify({ accountId, customerPhone }) },
  ));
}
export async function getManualPayment(slug: string, orderId: string) {
  if (!isGuid(orderId)) throw new Error("رقم الطلب غير صالح.");
  return parseManualResponse<ManualCustomerPayment>(
    await authorizedApiFetch(`${await endpoint(slug)}/orders/${orderId}`),
  );
}
export async function uploadManualReceipt(slug: string, orderId: string, receipt: File, reference: string) {
  if (!isGuid(orderId)) throw new Error("رقم الطلب غير صالح.");
  if (receipt.size > 1024 * 1024 || receipt.size < 24 ||
      !["image/jpeg", "image/png"].includes(receipt.type))
    throw new Error("ارفع صورة إيصال PNG أو JPG بحجم أقصاه 1 MB.");
  const form = new FormData();
  form.append("receipt", receipt, "receipt" + (receipt.type === "image/png" ? ".png" : ".jpg"));
  form.append("transferReference", reference.trim());
  return parseManualResponse<{ orderId: string; receiptId: string; status: string }>(
    await authorizedApiFetch(`${await endpoint(slug)}/orders/${orderId}/receipt`,
      { method: "POST", body: form }),
  );
}
export async function getManualQr(slug: string, orderId: string) {
  const response = await authorizedApiFetch(`${await endpoint(slug)}/orders/${orderId}/qr`);
  if (!response.ok) return null;
  const image = await response.blob();
  if (image.size > 40960 || !["image/png", "image/jpeg"].includes(image.type)) return null;
  return URL.createObjectURL(image);
}
export interface CustomerManualOrder {
  orderId: string;
  status: ManualCustomerPayment["status"];
  amount: number;
  currency: string;
  rejectionReason: string | null;
  createdAtUtc: string;
}
export async function getMyManualOrders(slug: string) {
  return parseManualResponse<CustomerManualOrder[]>(await authorizedApiFetch(`${await endpoint(slug)}/orders/mine`));
}
