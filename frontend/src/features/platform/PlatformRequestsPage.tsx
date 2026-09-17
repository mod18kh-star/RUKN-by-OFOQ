import {
  Check,
  ChevronLeft,
  Clock3,
  FileText,
  LoaderCircle,
  MessageSquareMore,
  Search,
  ShieldCheck,
  X,
} from "lucide-react";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  Link,
} from "react-router";

import {
  approvePlatformRequest,
  getPlatformRequest,
  getPlatformRequests,
  rejectPlatformRequest,
  requestMoreInfoForPlatformRequest,
  type PlatformRequestDetail,
  type PlatformRequestSummary,
} from "./platformApi";

type StatusFilter =
  | "all"
  | "Pending"
  | "MoreInfoRequested"
  | "Approved"
  | "Rejected";

type TypeFilter =
  | "all"
  | "StoreRegistration"
  | "PlanChange"
  | "StoreIdentityChange"
  | "OwnerEmailChange";

type Decision =
  | "approve"
  | "reject"
  | "more-info"
  | null;

const typeLabels: Record<string, string> = {
  StoreRegistration: "تسجيل متجر",
  PlanChange: "تغيير الباقة",
  StoreIdentityChange: "تغيير بيانات المتجر",
  OwnerEmailChange: "تغيير بريد المالك",
};

const statusLabels: Record<string, string> = {
  Pending: "بانتظار المراجعة",
  MoreInfoRequested: "مطلوب معلومات",
  Approved: "تمت الموافقة",
  Rejected: "مرفوض",
};

export function PlatformRequestsPage() {
  const [requests, setRequests] =
    useState<PlatformRequestSummary[]>([]);
  const [selected, setSelected] =
    useState<PlatformRequestDetail | null>(null);
  const [selectedId, setSelectedId] =
    useState<string | null>(null);
  const [status, setStatus] =
    useState<StatusFilter>("Pending");
  const [type, setType] =
    useState<TypeFilter>("all");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [decision, setDecision] = useState<Decision>(null);
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);

    try {
      const result = await getPlatformRequests({
        status: status === "all" ? undefined : status,
        type: type === "all" ? undefined : type,
        search,
      });

      setRequests(result);
      setError(null);

      if (
        selectedId &&
        !result.some((item) => item.requestId === selectedId)
      ) {
        setSelectedId(null);
        setSelected(null);
      }
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر تحميل الطلبات.",
      );
    } finally {
      setLoading(false);
    }
  }, [search, selectedId, status, type]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void load();
    }, 180);

    return () => window.clearTimeout(timer);
  }, [load]);

  useEffect(() => {
    if (!selectedId) {
      return;
    }

    void getPlatformRequest(selectedId)
      .then((result) => {
        setSelected(result);
        setError(null);
      })
      .catch((exception: unknown) => {
        setError(
          exception instanceof Error
            ? exception.message
            : "تعذر فتح الطلب.",
        );
      })
      .finally(() => setDetailLoading(false));
  }, [selectedId]);

  const counts = useMemo(
    () => ({
      visible: requests.length,
      urgent: requests.filter(
        (item) => item.status === "Pending",
      ).length,
    }),
    [requests],
  );

  function openRequest(requestId: string) {
    setSelected(null);
    setDetailLoading(true);
    setSelectedId(requestId);
  }

  async function submitDecision() {
    if (!selected || !decision) {
      return;
    }

    const cleanReason = reason.trim();

    if (cleanReason.length < 2) {
      setError("اكتب ملاحظة واضحة للقرار.");
      return;
    }

    setBusy(true);
    setError(null);

    try {
      if (decision === "approve") {
        await approvePlatformRequest(
          selected.requestId,
          cleanReason,
        );
      } else if (decision === "reject") {
        await rejectPlatformRequest(
          selected.requestId,
          cleanReason,
        );
      } else {
        await requestMoreInfoForPlatformRequest(
          selected.requestId,
          cleanReason,
        );
      }

      const refreshed = await getPlatformRequest(
        selected.requestId,
      );

      setSelected(refreshed);
      setReason("");
      setDecision(null);
      await load();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر حفظ القرار.",
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-[1500px] pb-10">
      <div className="flex flex-col gap-5 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <p className="text-[9px] font-semibold tracking-[0.05em] text-[#91672f]">
            REQUEST CENTER
          </p>
          <h1 className="mt-2 text-[31px] font-semibold tracking-[-0.045em] md:text-[36px]">
            الطلبات والموافقات
          </h1>
          <p className="mt-3 max-w-[720px] text-[11px] leading-7 text-black/44">
            كل طلب حساس يرسله صاحب المتجر يصل إلى هنا قبل تنفيذ التغيير على المنصة.
          </p>
        </div>

        <div className="flex gap-2">
          <SmallMetric
            label="ضمن الفلتر"
            value={loading ? "—" : counts.visible}
          />
          <SmallMetric
            label="تحتاج قرار"
            value={loading ? "—" : counts.urgent}
          />
        </div>
      </div>

      {error ? (
        <div className="mt-5 rounded-[13px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">
          {error}
        </div>
      ) : null}

      <section className="mt-7 overflow-hidden rounded-[20px] border border-black/[0.065] bg-white shadow-[0_18px_60px_rgba(20,18,13,0.035)]">
        <div className="grid min-h-[650px] xl:grid-cols-[minmax(0,1fr)_430px]">
          <div className="min-w-0 border-black/[0.06] xl:border-l">
            <div className="border-b border-black/[0.06] p-4">
              <div className="flex flex-col gap-3 lg:flex-row lg:items-center">
                <div className="relative flex-1">
                  <Search
                    size={14}
                    className="absolute right-3.5 top-1/2 -translate-y-1/2 text-black/28"
                  />
                  <input
                    value={search}
                    onChange={(event) => setSearch(event.target.value)}
                    placeholder="ابحث بالمتجر أو البريد أو نوع الطلب"
                    className="h-10 w-full rounded-[9px] border border-black/[0.08] bg-[#faf9f6] pr-10 pl-3 text-[10px] outline-none transition placeholder:text-black/25 focus:border-black/20"
                  />
                </div>

                <select
                  value={type}
                  onChange={(event) =>
                    setType(event.target.value as TypeFilter)
                  }
                  className="h-10 rounded-[9px] border border-black/[0.08] bg-[#faf9f6] px-3 text-[9px] outline-none"
                >
                  <option value="all">كل الأنواع</option>
                  <option value="StoreRegistration">تسجيل المتاجر</option>
                  <option value="PlanChange">الباقات</option>
                  <option value="StoreIdentityChange">بيانات المتجر</option>
                  <option value="OwnerEmailChange">بريد المالك</option>
                </select>
              </div>

              <div className="mt-3 flex max-w-full gap-1 overflow-x-auto rounded-[9px] bg-[#f6f4ef] p-1">
                {(
                  [
                    ["all", "الكل"],
                    ["Pending", "بانتظار المراجعة"],
                    ["MoreInfoRequested", "مطلوب معلومات"],
                    ["Approved", "موافق عليها"],
                    ["Rejected", "مرفوضة"],
                  ] as [StatusFilter, string][]
                ).map(([value, label]) => (
                  <button
                    key={value}
                    type="button"
                    onClick={() => setStatus(value)}
                    className={[
                      "h-8 shrink-0 rounded-[7px] px-3 text-[8px] font-semibold transition",
                      status === value
                        ? "bg-white text-black shadow-[0_3px_12px_rgba(0,0,0,0.05)]"
                        : "text-black/40 hover:text-black/70",
                    ].join(" ")}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>

            {loading ? (
              <div className="flex min-h-[420px] items-center justify-center text-[10px] text-black/35">
                <LoaderCircle size={16} className="ml-2 animate-spin" />
                جاري تحميل الطلبات
              </div>
            ) : requests.length === 0 ? (
              <div className="flex min-h-[420px] items-center justify-center px-6 text-center">
                <div>
                  <FileText size={24} className="mx-auto text-black/18" />
                  <p className="mt-4 text-[11px] font-semibold">
                    لا توجد طلبات ضمن هذا الفلتر
                  </p>
                  <p className="mt-1 text-[9px] text-black/34">
                    الطلبات الجديدة ستظهر هنا مباشرة
                  </p>
                </div>
              </div>
            ) : (
              <div className="divide-y divide-black/[0.055]">
                {requests.map((item) => (
                  <button
                    key={item.requestId}
                    type="button"
                    onClick={() => openRequest(item.requestId)}
                    className={[
                      "grid w-full gap-3 px-5 py-4 text-right transition md:grid-cols-[1.1fr_.8fr_.75fr_auto] md:items-center",
                      selectedId === item.requestId
                        ? "bg-[#f5f0e7]"
                        : "hover:bg-[#fbfaf7]",
                    ].join(" ")}
                  >
                    <div className="min-w-0">
                      <p className="truncate text-[11px] font-semibold">
                        {item.tenantName}
                      </p>
                      <p className="mt-1 truncate text-[8px] text-black/35">
                        {item.summary}
                      </p>
                    </div>

                    <div>
                      <p className="text-[9px] font-medium text-black/60">
                        {typeLabels[item.type] ?? item.type}
                      </p>
                      <p
                        dir="ltr"
                        className="mt-1 truncate text-left text-[8px] text-black/30"
                      >
                        {item.requestedByEmail ?? "—"}
                      </p>
                    </div>

                    <div>
                      <RequestStatus value={item.status} />
                      <p className="mt-1 text-[8px] text-black/27">
                        {formatDate(item.requestedAtUtc)}
                      </p>
                    </div>

                    <ChevronLeft size={14} className="text-black/25" />
                  </button>
                ))}
              </div>
            )}
          </div>

          <aside className="bg-[#fbfaf7]">
            {!selectedId ? (
              <div className="flex h-full min-h-[520px] items-center justify-center p-8 text-center">
                <div>
                  <ShieldCheck size={26} className="mx-auto text-[#a67a3f]" />
                  <p className="mt-4 text-[12px] font-semibold">
                    اختر طلبًا للمراجعة
                  </p>
                  <p className="mx-auto mt-2 max-w-[280px] text-[9px] leading-6 text-black/38">
                    ستظهر هنا البيانات المطلوبة وسجل القرار قبل تنفيذ أي تغيير على المتجر.
                  </p>
                </div>
              </div>
            ) : detailLoading || !selected ? (
              <div className="flex min-h-[520px] items-center justify-center text-[10px] text-black/35">
                <LoaderCircle size={16} className="ml-2 animate-spin" />
                جاري فتح الطلب
              </div>
            ) : (
              <RequestDetail
                request={selected}
                decision={decision}
                setDecision={setDecision}
                reason={reason}
                setReason={setReason}
                busy={busy}
                submitDecision={submitDecision}
              />
            )}
          </aside>
        </div>
      </section>
    </div>
  );
}

function RequestDetail({
  request,
  decision,
  setDecision,
  reason,
  setReason,
  busy,
  submitDecision,
}: {
  request: PlatformRequestDetail;
  decision: Decision;
  setDecision: (value: Decision) => void;
  reason: string;
  setReason: (value: string) => void;
  busy: boolean;
  submitDecision: () => Promise<void>;
}) {
  const payload = parsePayload(request.payloadJson);
  const closed =
    request.status === "Approved" ||
    request.status === "Rejected";

  return (
    <div className="p-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-[8px] font-semibold text-[#9b713a]">
            {typeLabels[request.type] ?? request.type}
          </p>
          <h2 className="mt-2 text-[19px] font-semibold tracking-[-0.03em]">
            {request.tenantName}
          </h2>
          <p dir="ltr" className="mt-1 text-left text-[8px] text-black/32">
            {request.tenantSlug}.ofoq.store
          </p>
        </div>
        <RequestStatus value={request.status} />
      </div>

      <Link
        to={`/platform/stores/${request.tenantId}`}
        className="mt-5 inline-flex items-center gap-2 text-[9px] font-semibold text-[#865f2c]"
      >
        فتح ملف المتجر
        <ChevronLeft size={12} />
      </Link>

      <div className="mt-6 space-y-3 border-y border-black/[0.06] py-5">
        <InfoRow label="الطلب" value={request.summary} />
        <InfoRow label="صاحب الطلب" value={request.requestedByEmail ?? "—"} ltr />
        <InfoRow label="أُرسل" value={formatDateTime(request.requestedAtUtc)} />
      </div>

      <div className="mt-5">
        <p className="text-[9px] font-semibold">التغيير المطلوب</p>
        <div className="mt-3 space-y-2 rounded-[12px] border border-black/[0.065] bg-white p-4">
          {Object.entries(payload).length === 0 ? (
            <p className="text-[9px] text-black/38">لا توجد تفاصيل إضافية.</p>
          ) : (
            Object.entries(payload).map(([key, value]) => (
              <InfoRow
                key={key}
                label={payloadLabel(key)}
                value={String(value ?? "—")}
                ltr={key.toLowerCase().includes("email")}
              />
            ))
          )}
        </div>
      </div>

      {request.reviewReason ? (
        <div className="mt-5 rounded-[12px] border border-black/[0.065] bg-white p-4">
          <p className="text-[8px] font-semibold text-black/35">آخر ملاحظة إدارية</p>
          <p className="mt-2 text-[10px] leading-6 text-black/65">
            {request.reviewReason}
          </p>
        </div>
      ) : null}

      {!closed ? (
        <div className="mt-6">
          <p className="text-[9px] font-semibold">قرار المراجعة</p>
          <div className="mt-3 grid grid-cols-3 gap-2">
            <DecisionButton
              active={decision === "approve"}
              onClick={() => setDecision("approve")}
              icon={Check}
              label="موافقة"
            />
            <DecisionButton
              active={decision === "more-info"}
              onClick={() => setDecision("more-info")}
              icon={MessageSquareMore}
              label="معلومات"
            />
            <DecisionButton
              active={decision === "reject"}
              onClick={() => setDecision("reject")}
              icon={X}
              label="رفض"
            />
          </div>

          {decision ? (
            <div className="mt-4">
              <textarea
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                placeholder={
                  decision === "approve"
                    ? "سبب الموافقة أو ملاحظة داخلية"
                    : decision === "reject"
                      ? "سبب الرفض الذي يوضح القرار"
                      : "ما المعلومات الإضافية المطلوبة؟"
                }
                className="min-h-[96px] w-full resize-none rounded-[11px] border border-black/[0.09] bg-white p-3 text-[10px] leading-6 outline-none placeholder:text-black/25 focus:border-black/20"
              />

              <button
                type="button"
                onClick={() => void submitDecision()}
                disabled={busy}
                className="mt-3 flex h-10 w-full items-center justify-center gap-2 rounded-[9px] bg-[#0a0d15] text-[10px] font-semibold text-white disabled:opacity-50"
              >
                {busy ? <LoaderCircle size={14} className="animate-spin" /> : <ShieldCheck size={14} />}
                حفظ القرار
              </button>
            </div>
          ) : null}
        </div>
      ) : (
        <div className="mt-6 flex items-center gap-2 rounded-[11px] border border-black/[0.065] bg-white px-4 py-3 text-[9px] text-black/45">
          <Clock3 size={13} />
          هذا الطلب مغلق وتم حفظ القرار في سجل الإدارة.
        </div>
      )}
    </div>
  );
}

function RequestStatus({ value }: { value: string }) {
  const classes =
    value === "Approved"
      ? "border-emerald-200 bg-emerald-50 text-emerald-700"
      : value === "Rejected"
        ? "border-red-200 bg-red-50 text-red-700"
        : value === "MoreInfoRequested"
          ? "border-blue-200 bg-blue-50 text-blue-700"
          : "border-amber-200 bg-amber-50 text-amber-700";

  return (
    <span className={`inline-flex w-fit rounded-full border px-2.5 py-1 text-[8px] font-semibold ${classes}`}>
      {statusLabels[value] ?? value}
    </span>
  );
}

function DecisionButton({
  active,
  onClick,
  icon: Icon,
  label,
}: {
  active: boolean;
  onClick: () => void;
  icon: typeof Check;
  label: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        "flex h-10 items-center justify-center gap-1.5 rounded-[9px] border text-[8px] font-semibold transition",
        active
          ? "border-[#0a0d15] bg-[#0a0d15] text-white"
          : "border-black/[0.08] bg-white text-black/50 hover:border-black/20 hover:text-black",
      ].join(" ")}
    >
      <Icon size={13} />
      {label}
    </button>
  );
}

function SmallMetric({
  label,
  value,
}: {
  label: string;
  value: number | string;
}) {
  return (
    <div className="min-w-[110px] rounded-[13px] border border-black/[0.065] bg-white px-4 py-3">
      <p className="text-[8px] text-black/35">{label}</p>
      <p className="mt-1 text-[18px] font-semibold tracking-[-0.04em]">{value}</p>
    </div>
  );
}

function InfoRow({
  label,
  value,
  ltr = false,
}: {
  label: string;
  value: string;
  ltr?: boolean;
}) {
  return (
    <div className="flex items-start justify-between gap-5 text-[9px]">
      <span className="shrink-0 text-black/34">{label}</span>
      <span dir={ltr ? "ltr" : undefined} className={ltr ? "text-left font-medium" : "text-left font-medium"}>
        {value}
      </span>
    </div>
  );
}

function parsePayload(value: string): Record<string, unknown> {
  try {
    const parsed = JSON.parse(value) as unknown;
    return typeof parsed === "object" && parsed !== null
      ? (parsed as Record<string, unknown>)
      : {};
  } catch {
    return {};
  }
}

function payloadLabel(key: string) {
  const labels: Record<string, string> = {
    PlanCode: "الباقة",
    BillingCycle: "الدفع",
    Reason: "سبب الطلب",
    Name: "الاسم الجديد",
    Slug: "الرابط الجديد",
    VerticalCode: "النشاط الجديد",
    Email: "البريد الجديد",
  };

  return labels[key] ?? key;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("ar-SA", {
    dateStyle: "medium",
  }).format(new Date(value));
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("ar-SA", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
