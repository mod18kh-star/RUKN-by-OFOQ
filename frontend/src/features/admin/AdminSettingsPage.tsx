import {
  ArrowLeft,
  Building2,
  CheckCircle2,
  Clock3,
  KeyRound,
  Mail,
  RefreshCw,
  ShieldCheck,
  SlidersHorizontal,
  XCircle,
} from "lucide-react";

import {
  Link,
} from "react-router";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  getMyPlatformRequests,
  submitOwnerEmailChangeRequest,
  submitPlanChangeRequest,
  submitStoreIdentityChangeRequest,
  type MerchantPlatformRequest,
} from "./requests/merchantRequestsApi";

import {
  readAdminStore,
} from "./store-setup/storeSetupStorage";

import {
  VERTICAL_OPTIONS,
} from "./store-setup/verticalCatalog";

import {
  StoreContactSettings,
} from "./store-profile/StoreContactSettings";


type RequestKind =
  | "plan"
  | "identity"
  | "email"
  | null;

const statusLabels: Record<string, string> = {
  Pending: "قيد المراجعة",
  MoreInfoRequested: "مطلوب معلومات",
  Approved: "تمت الموافقة",
  Rejected: "مرفوض",
};

const typeLabels: Record<string, string> = {
  StoreRegistration: "اعتماد المتجر",
  PlanChange: "تغيير الباقة",
  StoreIdentityChange: "تغيير بيانات المتجر",
  OwnerEmailChange: "تغيير بريد المالك",
};

export function AdminSettingsPage() {
  const store = readAdminStore();
  const [requests, setRequests] =
    useState<MerchantPlatformRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [kind, setKind] = useState<RequestKind>(null);
  const [busy, setBusy] = useState(false);

  const [planCode, setPlanCode] = useState("pro");
  const [billingCycle, setBillingCycle] =
    useState<"Monthly" | "Annual">("Annual");
  const [reason, setReason] = useState("");
  const [name, setName] = useState(store?.name ?? "");
  const [slug, setSlug] = useState(store?.slug ?? "");
  const [verticalCode, setVerticalCode] = useState(
    store?.verticalCode ?? "",
  );
  const [email, setEmail] = useState("");

  const load = useCallback(async () => {
    if (!store?.tenantId) {
      setLoading(false);
      return;
    }

    try {
      const result = await getMyPlatformRequests(
        store.tenantId,
      );
      setRequests(result);
      setError(null);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر تحميل الطلبات.",
      );
    } finally {
      setLoading(false);
    }
  }, [store?.tenantId]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void load();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [load]);

  const pendingCount = useMemo(
    () =>
      requests.filter(
        (item) => item.status === "Pending",
      ).length,
    [requests],
  );

  async function submit() {
    if (!store?.tenantId || !kind) {
      return;
    }

    const cleanReason = reason.trim();

    if (cleanReason.length < 3) {
      setError("اكتب سببًا واضحًا للطلب.");
      return;
    }

    setBusy(true);
    setError(null);

    try {
      if (kind === "plan") {
        await submitPlanChangeRequest(
          store.tenantId,
          {
            planCode,
            billingCycle,
            reason: cleanReason,
          },
        );
      } else if (kind === "identity") {
        await submitStoreIdentityChangeRequest(
          store.tenantId,
          {
            name: name.trim() || null,
            slug: slug.trim() || null,
            verticalCode: verticalCode || null,
            reason: cleanReason,
          },
        );
      } else {
        await submitOwnerEmailChangeRequest(
          store.tenantId,
          {
            email: email.trim(),
            reason: cleanReason,
          },
        );
      }

      setKind(null);
      setReason("");
      setEmail("");
      await load();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر إرسال الطلب.",
      );
    } finally {
      setBusy(false);
    }
  }

  if (!store) {
    return (
      <div className="rounded-[16px] border border-black/[0.08] bg-white p-8 text-[11px]">
        لم يتم العثور على متجر حالي.
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-[1180px] pb-10">
      <div className="flex flex-col gap-5 md:flex-row md:items-end md:justify-between">
        <div>
          <p className="text-[9px] font-semibold text-[#9a713f]">
            STORE SETTINGS
          </p>
          <h1 className="mt-2 text-[31px] font-semibold tracking-[-0.045em]">
            الإعدادات والطلبات
          </h1>
          <p className="mt-3 max-w-[680px] text-[11px] leading-7 text-black/44">
            التغييرات الحساسة لا تُنفذ مباشرة. أرسل الطلب وسيصل إلى إدارة ركن للمراجعة والموافقة.
          </p>
        </div>

        <div className="rounded-[13px] border border-black/[0.07] bg-white px-4 py-3">
          <p className="text-[8px] text-black/35">طلبات قيد المراجعة</p>
          <p className="mt-1 text-[19px] font-semibold">{pendingCount}</p>
        </div>
      </div>

      {error ? (
        <div className="mt-5 rounded-[12px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">
          {error}
        </div>
      ) : null}

      <div className="mt-7 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <ActionCard
          icon={SlidersHorizontal}
          title="تغيير الباقة"
          description="اطلب ترقية أو تغيير الباقة وطريقة الفوترة."
          onClick={() => setKind("plan")}
        />
        <ActionCard
          icon={Building2}
          title="تغيير بيانات المتجر"
          description="الاسم أو الرابط أو النشاط الأساسي بعد موافقة الإدارة."
          onClick={() => setKind("identity")}
        />
        <ActionCard
          icon={Mail}
          title="تغيير بريد المالك"
          description="طلب أمني لتغيير بريد حساب المالك بعد المراجعة."
          onClick={() => setKind("email")}
        />

        <Link
          to="/security/setup?returnTo=%2Fadmin%2Fsettings"
          className="group rounded-[16px] border border-black/[0.07] bg-white p-5 transition hover:border-black/15"
        >
          <div className="flex size-9 items-center justify-center rounded-[10px] bg-[#f2ede4] text-[#78592f]">
            <KeyRound size={16} />
          </div>
          <h3 className="mt-4 text-[11px] font-semibold">
            أمان الحساب
          </h3>
          <p className="mt-2 text-[9px] leading-5 text-black/40">
            إدارة التحقق بخطوتين والوصول الآمن إلى لوحة المتجر.
          </p>
        </Link>
      </div>

      <StoreContactSettings tenantId={store.tenantId} />

      <section className="mt-6 overflow-hidden rounded-[18px] border border-black/[0.065] bg-white">
        <div className="flex items-center justify-between gap-4 border-b border-black/[0.06] px-5 py-4">
          <div>
            <p className="text-[9px] font-semibold">طلباتك للإدارة</p>
            <p className="mt-1 text-[8px] text-black/34">
              آخر الطلبات والقرارات المتعلقة بهذا المتجر
            </p>
          </div>
          <button
            type="button"
            onClick={() => void load()}
            className="flex size-9 items-center justify-center rounded-[9px] border border-black/[0.07] text-black/40"
          >
            <RefreshCw size={13} />
          </button>
        </div>

        {loading ? (
          <div className="p-10 text-center text-[9px] text-black/35">
            جاري تحميل الطلبات
          </div>
        ) : requests.length === 0 ? (
          <div className="p-10 text-center text-[9px] text-black/35">
            لا يوجد طلبات بعد
          </div>
        ) : (
          <div className="divide-y divide-black/[0.055]">
            {requests.map((item) => (
              <div
                key={item.requestId}
                className="grid gap-3 px-5 py-4 md:grid-cols-[1fr_.8fr_.8fr] md:items-center"
              >
                <div>
                  <p className="text-[10px] font-semibold">
                    {typeLabels[item.type] ?? item.type}
                  </p>
                  <p className="mt-1 text-[8px] text-black/34">
                    {item.summary}
                  </p>
                </div>
                <RequestStatus value={item.status} />
                <div className="text-[8px] text-black/34 md:text-left">
                  {new Intl.DateTimeFormat("ar-SA", {
                    dateStyle: "medium",
                  }).format(new Date(item.requestedAtUtc))}
                </div>
                {item.reviewReason ? (
                  <p className="md:col-span-3 rounded-[9px] bg-[#f7f5f0] px-3 py-2 text-[9px] leading-5 text-black/55">
                    ملاحظة الإدارة: {item.reviewReason}
                  </p>
                ) : null}
              </div>
            ))}
          </div>
        )}
      </section>

      {kind ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/35 p-4 backdrop-blur-[2px]">
          <div className="w-full max-w-[560px] rounded-[18px] border border-black/[0.08] bg-[#f8f6f1] p-6 shadow-2xl">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-[9px] font-semibold text-[#9a713f]">
                  REQUEST TO RUKN
                </p>
                <h2 className="mt-2 text-[20px] font-semibold">
                  {kind === "plan"
                    ? "طلب تغيير الباقة"
                    : kind === "identity"
                      ? "طلب تغيير بيانات المتجر"
                      : "طلب تغيير بريد المالك"}
                </h2>
              </div>
              <button
                type="button"
                onClick={() => setKind(null)}
                className="text-[10px] text-black/40"
              >
                إلغاء
              </button>
            </div>

            <div className="mt-6 space-y-4">
              {kind === "plan" ? (
                <>
                  <Field label="الباقة المطلوبة">
                    <select
                      value={planCode}
                      onChange={(event) => setPlanCode(event.target.value)}
                      className="h-10 w-full rounded-[10px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/20"
                    >
                      <option value="business">Business</option>
                      <option value="pro">Pro</option>
                      <option value="extra">Extra</option>
                    </select>
                  </Field>
                  <Field label="دورة الفوترة">
                    <select
                      value={billingCycle}
                      onChange={(event) =>
                        setBillingCycle(
                          event.target.value as "Monthly" | "Annual",
                        )
                      }
                      className="h-10 w-full rounded-[10px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/20"
                    >
                      <option value="Monthly">شهري</option>
                      <option value="Annual">سنوي</option>
                    </select>
                  </Field>
                </>
              ) : kind === "identity" ? (
                <>
                  <Field label="اسم المتجر">
                    <input
                      value={name}
                      onChange={(event) => setName(event.target.value)}
                      className="h-10 w-full rounded-[10px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/20"
                    />
                  </Field>
                  <Field label="الرابط">
                    <input
                      dir="ltr"
                      value={slug}
                      onChange={(event) => setSlug(event.target.value)}
                      className="h-10 w-full rounded-[10px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/20 text-left"
                    />
                  </Field>
                  <Field label="النشاط الأساسي">
                    <select
                      value={verticalCode}
                      onChange={(event) => setVerticalCode(event.target.value)}
                      className="h-10 w-full rounded-[10px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/20"
                    >
                      <option value="">بدون تغيير</option>
                      {VERTICAL_OPTIONS.map((item) => (
                        <option key={item.code} value={item.code}>
                          {item.label}
                        </option>
                      ))}
                    </select>
                  </Field>
                </>
              ) : (
                <Field label="البريد الجديد للمالك">
                  <input
                    type="email"
                    dir="ltr"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    placeholder="owner@example.com"
                    className="h-10 w-full rounded-[10px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/20 text-left"
                  />
                </Field>
              )}

              <Field label="سبب الطلب">
                <textarea
                  value={reason}
                  onChange={(event) => setReason(event.target.value)}
                  placeholder="اكتب السبب باختصار حتى تقدر الإدارة تراجع الطلب بسرعة"
                  className="min-h-[100px] w-full resize-none rounded-[10px] border border-black/[0.09] bg-white p-3 text-[10px] leading-6 outline-none"
                />
              </Field>
            </div>

            <button
              type="button"
              onClick={() => void submit()}
              disabled={busy}
              className="mt-6 flex h-11 w-full items-center justify-center gap-2 rounded-[9px] bg-[#0a0d15] text-[10px] font-semibold text-white disabled:opacity-50"
            >
              <ShieldCheck size={14} />
              {busy ? "جاري الإرسال..." : "إرسال للمراجعة"}
              <ArrowLeft size={13} />
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function ActionCard({
  icon: Icon,
  title,
  description,
  onClick,
}: {
  icon: typeof Mail;
  title: string;
  description: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="group rounded-[17px] border border-black/[0.065] bg-white p-5 text-right transition hover:-translate-y-0.5 hover:shadow-[0_15px_40px_rgba(0,0,0,0.05)]"
    >
      <div className="flex size-9 items-center justify-center rounded-[9px] bg-[#eee4d4] text-[#8b632f]">
        <Icon size={15} />
      </div>
      <p className="mt-5 text-[12px] font-semibold">{title}</p>
      <p className="mt-2 text-[9px] leading-6 text-black/40">{description}</p>
    </button>
  );
}

function RequestStatus({ value }: { value: string }) {
  const Icon =
    value === "Approved"
      ? CheckCircle2
      : value === "Rejected"
        ? XCircle
        : Clock3;

  const classes =
    value === "Approved"
      ? "text-emerald-700"
      : value === "Rejected"
        ? "text-red-700"
        : value === "MoreInfoRequested"
          ? "text-blue-700"
          : "text-amber-700";

  return (
    <span className={`inline-flex items-center gap-1.5 text-[9px] font-semibold ${classes}`}>
      <Icon size={12} />
      {statusLabels[value] ?? value}
    </span>
  );
}

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-[9px] font-semibold text-black/50">
        {label}
      </span>
      {children}
    </label>
  );
}
