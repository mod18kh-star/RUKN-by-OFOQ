import { authorizedApiFetch } from "../../auth/authSession";

export interface ManualPaymentAccount {
  id: string;
  kind: "bank" | "wallet";
  displayName: string;
  maskedReference: string;
  hasQr: boolean;
  isEnabled: boolean;
  updatedAtUtc: string;
}

export interface ManualPaymentDetails extends ManualPaymentAccount {
  bankName?: string | null;
  accountHolder?: string | null;
  iban?: string | null;
  accountNumber?: string | null;
  walletProvider?: string | null;
  walletNumber?: string | null;
  transferLink?: string | null;
}

export interface ManualAccountInput {
  kind: "bank" | "wallet";
  displayName: string;
  bankName: string;
  accountHolder: string;
  iban: string;
  accountNumber: string;
  walletProvider: string;
  walletNumber: string;
  transferLink: string;
  qrBase64: string | null;
  removeQr: boolean;
}

const root = (tenantId: string) =>
  `/api/tenants/${encodeURIComponent(tenantId)}/payments/manual-accounts`;

async function result<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const message = response.status === 401 ? "انتهت الجلسة؛ سجل الدخول مجددًا." :
      response.status === 403 ? "هذه العملية تتطلب صلاحية إدارة مدفوعات المتجر وتسجيل الدخول مع المصادقة الثنائية." :
      response.status === 404 ? "الحساب غير موجود في هذا المتجر." :
      response.status === 400 ? "البيانات غير مقبولة. تحقق من رقم الحساب ورابط التحويل وحجم صورة QR." :
      response.status === 409 ? "الحساب غير مكتمل أو تجاوزت الحد المسموح به؛ راجع البيانات." :
      "تعذر إتمام العملية؛ جرّب مجددًا.";
    throw new Error(message);
  }
  return response.json() as Promise<T>;
}

export async function listManualAccounts(tenantId: string) {
  return result<ManualPaymentAccount[]>(await authorizedApiFetch(`${root(tenantId)}/`));
}

export async function getManualAccount(tenantId: string, id: string) {
  return result<ManualPaymentDetails>(await authorizedApiFetch(`${root(tenantId)}/${encodeURIComponent(id)}`));
}

export async function saveManualAccount(tenantId: string, data: ManualAccountInput, id?: string) {
  return result<ManualPaymentAccount>(await authorizedApiFetch(
    id ? `${root(tenantId)}/${encodeURIComponent(id)}` : `${root(tenantId)}/`,
    { method: id ? "PUT" : "POST", body: JSON.stringify(data) },
  ));
}

export async function setManualAccountState(tenantId: string, id: string, enabled: boolean) {
  return result<ManualPaymentAccount>(await authorizedApiFetch(
    `${root(tenantId)}/${encodeURIComponent(id)}/state`,
    { method: "PUT", body: JSON.stringify({ enabled }) },
  ));
}

export async function loadManualQr(tenantId: string, id: string): Promise<string> {
  const response = await authorizedApiFetch(`${root(tenantId)}/${encodeURIComponent(id)}/qr`);
  if (!response.ok) throw new Error("تعذر تحميل صورة QR لهذا الحساب.");
  const blob = await response.blob();
  if (blob.size > 41000 || !["image/png", "image/jpeg"].includes(blob.type)) throw new Error("صيغة صورة QR غير مدعومة.");
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(new Error("تعذر قراءة صورة QR."));
    reader.onload = () => resolve(String(reader.result ?? ""));
    reader.readAsDataURL(blob);
  });
}
