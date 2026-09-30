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
export interface CustomerOrderLine {
  productId: string;
  productName: string;
  variantName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}
export interface CustomerManualOrder {
  orderId: string;
  status: "AwaitingMethod" | "AwaitingReceipt" | "PendingReview" | "Approved" | "Rejected" | "Cancelled";
  orderStatus: string;
  fulfillmentStatus: string;
  amount: number;
  currency: string;
  rejectionReason: string | null;
  createdAtUtc: string;
  items: CustomerOrderLine[];
}
// Editing a checkout cancels the UNPAID order and restores its product quantities.
// The server rechecks receipt/payment state and holds order + stock locks atomically.
export async function restoreUnpaidOrderToCart(slug: string, orderId: string) {
  if (!isGuid(orderId)) throw new Error("رقم الطلب غير صالح.");
  return parseManualResponse<{ restoredCartId: string; cancelledOrderId: string }>(
    await authorizedApiFetch(`${await endpoint(slug)}/orders/${orderId}/restore-cart`, {
      method: "POST",
      body: JSON.stringify({ confirmNoTransfer: true }),
    }),
  );
}

export async function getMyManualOrders(slug: string) {
  return parseManualResponse<CustomerManualOrder[]>(await authorizedApiFetch(`${await endpoint(slug)}/orders/mine`));
}

// A manual draft is NOT an order. Full account details are available for the
// customer to review before proof is submitted, but the backend never creates
// an order or consumes coupon usage until the atomic /draft/submit operation.
export interface ManualDraftAccount {
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
  accountUpdatedAtUtc: string;
}

export async function getManualDraftAccount(slug: string, accountId: string) {
  if (!isGuid(accountId)) throw new Error("وسيلة الدفع غير صالحة.");
  return parseManualResponse<ManualDraftAccount>(
    await authorizedApiFetch(`${await endpoint(slug)}/draft/accounts/${accountId}`),
  );
}

export async function getManualDraftQr(
  slug: string,
  accountId: string,
) {
  if (!isGuid(accountId)) {
    return null;
  }

  const response =
    await authorizedApiFetch(
      `${await endpoint(slug)}/draft/accounts/${accountId}/qr`,
    );

  if (!response.ok) {
    return null;
  }

  const buffer =
    await response.arrayBuffer();

  if (
    buffer.byteLength === 0 ||
    buffer.byteLength > 40960
  ) {
    return null;
  }

  const bytes =
    new Uint8Array(buffer);

  const responseMime = (
    response.headers.get("content-type") ?? ""
  )
    .split(";")[0]
    .trim()
    .toLowerCase();

  const isPng =
    bytes.length >= 8 &&
    bytes[0] === 0x89 &&
    bytes[1] === 0x50 &&
    bytes[2] === 0x4e &&
    bytes[3] === 0x47 &&
    bytes[4] === 0x0d &&
    bytes[5] === 0x0a &&
    bytes[6] === 0x1a &&
    bytes[7] === 0x0a;

  const isJpeg =
    bytes.length >= 3 &&
    bytes[0] === 0xff &&
    bytes[1] === 0xd8 &&
    bytes[2] === 0xff;

  const mime =
    responseMime === "image/png" ||
    responseMime === "image/jpeg"
      ? responseMime
      : isPng
        ? "image/png"
        : isJpeg
          ? "image/jpeg"
          : null;

  if (!mime) {
    return null;
  }

  return URL.createObjectURL(
    new Blob(
      [buffer],
      { type: mime },
    ),
  );
}

export async function submitManualDraftReceipt(
  slug: string,
  draft: import("./customerCheckoutDraft").ManualCheckoutDraft,
  accountUpdatedAtUtc: string,
  receipt: File,
  reference: string,
) {
  if (receipt.size < 24 || receipt.size > 1024 * 1024 ||
      !["image/png", "image/jpeg"].includes(receipt.type)) {
    throw new Error("ارفع إيصال PNG أو JPG بحجم لا يتجاوز 1 MB.");
  }
  const form = new FormData();
  form.append("receipt", receipt, receipt.type === "image/png" ? "receipt.png" : "receipt.jpg");
  form.append("transferReference", reference.trim());
  form.append("cartId", draft.cartId);
  form.append("shippingMethodId", draft.shippingMethodId);
  form.append("customerAddressId", draft.customerAddressId ?? "");
  form.append("accountId", draft.paymentAccountId);
  form.append("accountUpdatedAtUtc", accountUpdatedAtUtc);
  form.append("customerPhone", draft.customerPhone);
  form.append("couponCode", draft.couponCode ?? "");
  form.append("expectedAmount", String(draft.expectedAmount));
  form.append("expectedCurrency", draft.expectedCurrency);
  form.append("expectedLines", JSON.stringify(draft.lines));
  form.append("idempotencyKey", draft.idempotencyKey);
  return parseManualResponse<{ orderId: string; status: string; idempotentReplay: boolean }>(
    await authorizedApiFetch(`${await endpoint(slug)}/draft/submit`, { method: "POST", body: form }),
  );
}
