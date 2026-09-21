import { authorizedApiFetch } from "../../auth/authSession";

export interface ProviderAccount {
  accountId: string;
  providerCode: string;
  displayName: string;
  environment: "Sandbox" | "Production";
  isEnabled: boolean;
  hasCredentials: boolean;
  credentialsVersion: number;
  createdAtUtc: string;
  wallets: { capabilityId: string; walletType: string; isEnabled: boolean }[];
}

const base = (tenantId: string) =>
  `/api/tenants/${encodeURIComponent(tenantId)}/payments/provider-accounts`;

async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const message = response.status === 401 ? "يرجى تسجيل الدخول مجددًا." :
      response.status === 403 ? "ليس لديك صلاحية لإدارة حسابات الدفع. تحقق من صلاحياتك والمصادقة الثنائية." :
      response.status === 409 ? "تعذر تنفيذ العملية بسبب تعارض أو إعداد غير مكتمل. حدّث الصفحة وتحقق من الحساب." :
      response.status === 400 ? "البيانات غير مقبولة. راجعها وحاول مجددًا." :
      "تعذر الاتصال بخدمة الدفع. حاول مجددًا.";
    throw new Error(message);
  }
  return (await response.json()) as T;
}

export async function getProviderAccounts(tenantId: string) {
  return parse<ProviderAccount[]>(await authorizedApiFetch(`${base(tenantId)}/`));
}

export async function createSandboxProvider(tenantId: string, providerCode: string, displayName: string) {
  return parse<ProviderAccount>(await authorizedApiFetch(`${base(tenantId)}/`, {
    method: "POST",
    body: JSON.stringify({ providerCode, displayName, environment: "Sandbox" }),
  }));
}

export async function setSandboxProviderState(tenantId: string, account: ProviderAccount, enabled: boolean) {
  if (account.environment !== "Sandbox") {
    throw new Error("لا يمكن تفعيل بوابات الإنتاج من صفحة التجارب.");
  }
  if (enabled && !account.hasCredentials) {
    throw new Error("أضف بيانات ربط صالحة على السيرفر قبل تفعيل بوابة الاختبار.");
  }
  return parse<ProviderAccount>(await authorizedApiFetch(
    `${base(tenantId)}/${encodeURIComponent(account.accountId)}/state`,
    { method: "PUT", body: JSON.stringify({ enabled }) },
  ));
}
