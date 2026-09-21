import {
  AlertTriangle,
  Box,
  CheckCircle2,
  ChevronLeft,
  CircleX,
  Clock3,
  PackageCheck,
  RefreshCw,
  Search,
  Truck,
  X,
} from "lucide-react";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import { Link } from "react-router";

import {
  AdminOperationApiError,
  getMerchantOrderById,
  getMerchantOrders,
  type MerchantOrderDetail,
  type MerchantOrderSummary,
} from "./dashboard/adminDashboardApi";

import { readAdminStore } from "./store-setup/storeSetupStorage";
import { ManualPaymentReviewPanel } from "./payments/ManualPaymentReviewPanel";

type StatusTab =
  | "all"
  | "processing"
  | "shipping"
  | "delivered"
  | "cancelled";

type Period = "day" | "week" | "month" | "all";

const periodLabels: Record<Period, string> = {
  day: "اليوم",
  week: "7 أيام",
  month: "30 يوم",
  all: "الكل",
};

function formatMoney(value: number, currency: string) {
  try {
    return new Intl.NumberFormat("ar", {
      style: "currency",
      currency,
      maximumFractionDigits: 2,
    }).format(value);
  } catch {
    return `${value.toLocaleString("ar")} ${currency}`;
  }
}

function statusLabel(order: MerchantOrderSummary) {
  const fulfillment = order.fulfillmentStatus.toLowerCase();
  const status = order.orderStatus.toLowerCase();

  if (status === "cancelled" || fulfillment === "cancelled") {
    return "ملغي";
  }
  if (fulfillment === "delivered" || fulfillment === "fulfilled") {
    return "تم التوصيل";
  }
  if (fulfillment === "intransit") {
    return "قيد التوصيل";
  }
  if (fulfillment === "shipped") {
    return "تم الشحن";
  }
  if (fulfillment === "readytoship") {
    return "جاهز للشحن";
  }
  if (fulfillment === "processing" || status === "processing") {
    return "قيد التجهيز";
  }
  if (status === "confirmed") {
    return "مؤكد";
  }
  if (status === "paid") {
    return "مدفوع";
  }
  return "جديد";
}

function matchesTab(order: MerchantOrderSummary, tab: StatusTab) {
  if (tab === "all") {
    return true;
  }

  const fulfillment = order.fulfillmentStatus.toLowerCase();
  const status = order.orderStatus.toLowerCase();

  if (tab === "cancelled") {
    return status === "cancelled" || fulfillment === "cancelled";
  }

  if (tab === "delivered") {
    return fulfillment === "delivered" || fulfillment === "fulfilled";
  }

  if (tab === "shipping") {
    return ["readytoship", "shipped", "intransit"].includes(fulfillment);
  }

  return (
    ["confirmed", "processing", "paid"].includes(status) ||
    fulfillment === "processing"
  );
}

function withinPeriod(createdAtUtc: string, period: Period) {
  if (period === "all") {
    return true;
  }

  const now = Date.now();
  const created = new Date(createdAtUtc).getTime();
  const days = period === "day" ? 1 : period === "week" ? 7 : 30;

  return now - created <= days * 24 * 60 * 60 * 1000;
}

export function AdminOrdersPage() {
  const store = readAdminStore();
  const [orders, setOrders] = useState<MerchantOrderSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [securityBlocked, setSecurityBlocked] = useState(false);
  const [tab, setTab] = useState<StatusTab>("all");
  const [period, setPeriod] = useState<Period>("month");
  const [query, setQuery] = useState("");
  const [selected, setSelected] = useState<MerchantOrderDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const load = useCallback(async () => {
    if (!store?.tenantId) {
      setLoading(false);
      setError("لم يتم العثور على متجر حالي.");
      return;
    }

    setLoading(true);
    setError(null);
    setSecurityBlocked(false);

    try {
      const result = await getMerchantOrders(store.tenantId, 100);
      setOrders(result);
    } catch (exception) {
      if (exception instanceof AdminOperationApiError && exception.status === 403) {
        setSecurityBlocked(true);
      }

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

  const periodOrders = useMemo(
    () => orders.filter((order) => withinPeriod(order.createdAtUtc, period)),
    [orders, period],
  );

  const filtered = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    return periodOrders.filter((order) => {
      if (!matchesTab(order, tab)) {
        return false;
      }

      if (!normalizedQuery) {
        return true;
      }

      return [order.orderId, order.customerEmail ?? "", order.trackingNumber ?? ""]
        .join(" ")
        .toLowerCase()
        .includes(normalizedQuery);
    });
  }, [periodOrders, query, tab]);

  const counts = useMemo(
    () => ({
      total: periodOrders.length,
      processing: periodOrders.filter((order) => matchesTab(order, "processing")).length,
      shipping: periodOrders.filter((order) => matchesTab(order, "shipping")).length,
      delivered: periodOrders.filter((order) => matchesTab(order, "delivered")).length,
      cancelled: periodOrders.filter((order) => matchesTab(order, "cancelled")).length,
    }),
    [periodOrders],
  );

  async function openOrder(orderId: string) {
    if (!store?.tenantId) {
      return;
    }

    setDetailLoading(true);
    setError(null);

    try {
      const detail = await getMerchantOrderById(store.tenantId, orderId);
      setSelected(detail);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر تحميل تفاصيل الطلب.",
      );
    } finally {
      setDetailLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-[1320px] pb-12">
      <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-[9px] font-semibold text-[#9a713f]">ORDERS</p>
          <h1 className="mt-2 text-[31px] font-semibold tracking-[-0.045em]">
            الطلبات
          </h1>
          <p className="mt-2 max-w-[620px] text-[10px] leading-6 text-black/43">
            تابع تجهيز الطلبات والشحن والتسليم من مكان واحد، بدون خلطها مع صفحة التقارير.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <select
            value={period}
            onChange={(event) => setPeriod(event.target.value as Period)}
            className="h-10 rounded-[10px] border border-black/[0.08] bg-white px-3 text-[9px] outline-none"
          >
            {(Object.keys(periodLabels) as Period[]).map((key) => (
              <option key={key} value={key}>
                {periodLabels[key]}
              </option>
            ))}
          </select>
          <button
            type="button"
            onClick={() => void load()}
            className="flex size-10 items-center justify-center rounded-[10px] border border-black/[0.08] bg-white text-black/45"
            aria-label="تحديث الطلبات"
          >
            <RefreshCw size={14} />
          </button>
        </div>
      </div>

      {store?.tenantId && <ManualPaymentReviewPanel tenantId={store.tenantId} onChanged={() => void load()} />}

      {securityBlocked ? (
        <div className="mt-5 flex flex-col gap-4 rounded-[15px] border border-[#e5cda7] bg-[#fffaf1] p-5 md:flex-row md:items-center md:justify-between">
          <div className="flex items-start gap-3">
            <AlertTriangle size={17} className="mt-0.5 text-[#9a713f]" />
            <div>
              <p className="text-[10px] font-semibold text-[#4b3419]">
                الطلبات محمية بالتحقق بخطوتين
              </p>
              <p className="mt-1 text-[9px] text-[#7b6547]">
                أكمل حماية الحساب أولًا، وبعدها ستظهر الطلبات وتفاصيلها هنا.
              </p>
            </div>
          </div>
          <Link
            to="/admin/settings"
            className="inline-flex h-9 items-center justify-center rounded-[9px] bg-[#171c19] px-4 text-[9px] font-semibold text-white"
          >
            إعدادات الأمان
          </Link>
        </div>
      ) : null}

      {error && !securityBlocked ? (
        <div className="mt-5 rounded-[12px] border border-red-200 bg-red-50 px-4 py-3 text-[9px] text-red-700">
          {error}
        </div>
      ) : null}

      <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
        <OrderMetric label="إجمالي الطلبات" value={counts.total} icon={Box} />
        <OrderMetric label="قيد التجهيز" value={counts.processing} icon={Clock3} />
        <OrderMetric label="في الشحن" value={counts.shipping} icon={Truck} />
        <OrderMetric label="تم التسليم" value={counts.delivered} icon={CheckCircle2} />
        <OrderMetric label="ملغاة" value={counts.cancelled} icon={CircleX} />
      </div>

      <section className="mt-5 overflow-hidden rounded-[17px] border border-black/[0.07] bg-white">
        <div className="flex flex-col gap-4 border-b border-black/[0.06] p-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-wrap gap-1.5">
            {([
              ["all", "الكل"],
              ["processing", "قيد التجهيز"],
              ["shipping", "الشحن والتوصيل"],
              ["delivered", "المسلمة"],
              ["cancelled", "الملغاة"],
            ] as Array<[StatusTab, string]>).map(([value, label]) => (
              <button
                key={value}
                type="button"
                onClick={() => setTab(value)}
                className={`h-8 rounded-[8px] px-3 text-[8px] font-semibold transition ${
                  tab === value
                    ? "bg-[#151a17] text-white"
                    : "bg-[#f5f4f0] text-black/48 hover:text-black/70"
                }`}
              >
                {label}
              </button>
            ))}
          </div>

          <label className="flex h-9 w-full items-center gap-2 rounded-[9px] border border-black/[0.07] bg-[#fafaf8] px-3 lg:w-[280px]">
            <Search size={13} className="text-black/30" />
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="رقم الطلب، العميل أو رقم التتبع"
              className="min-w-0 flex-1 bg-transparent text-[9px] outline-none placeholder:text-black/28"
            />
          </label>
        </div>

        {loading ? (
          <div className="p-12 text-center text-[9px] text-black/35">
            جاري تحميل الطلبات
          </div>
        ) : filtered.length === 0 ? (
          <div className="p-14 text-center">
            <PackageCheck className="mx-auto text-black/15" size={26} />
            <p className="mt-3 text-[10px] font-semibold text-black/55">
              لا توجد طلبات ضمن هذا الفلتر
            </p>
            <p className="mt-1 text-[8px] text-black/32">
              غيّر الفترة أو الحالة لعرض نتائج أخرى.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[860px] border-collapse text-right">
              <thead>
                <tr className="border-b border-black/[0.055] bg-[#fbfbf9] text-[8px] text-black/35">
                  <th className="px-5 py-3 font-medium">الطلب</th>
                  <th className="px-5 py-3 font-medium">العميل</th>
                  <th className="px-5 py-3 font-medium">الحالة</th>
                  <th className="px-5 py-3 font-medium">المبلغ</th>
                  <th className="px-5 py-3 font-medium">التاريخ</th>
                  <th className="px-5 py-3 font-medium" />
                </tr>
              </thead>
              <tbody>
                {filtered.map((order) => (
                  <tr
                    key={order.orderId}
                    className="border-b border-black/[0.045] last:border-0 hover:bg-black/[0.012]"
                  >
                    <td className="px-5 py-4 text-[9px] font-semibold">
                      #{order.orderId.slice(0, 8)}
                    </td>
                    <td className="px-5 py-4">
                      <p className="text-[9px] text-black/68">
                        {order.customerEmail ?? "عميل"}
                      </p>
                      <p className="mt-0.5 text-[8px] text-black/30">
                        {order.totalQuantity} قطعة
                      </p>
                    </td>
                    <td className="px-5 py-4">
                      <span className="rounded-full bg-[#f1efe9] px-2.5 py-1 text-[8px] font-semibold text-black/56">
                        {statusLabel(order)}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-[9px] font-semibold">
                      {formatMoney(order.totalAmount, order.currency)}
                    </td>
                    <td className="px-5 py-4 text-[8px] text-black/38">
                      {new Intl.DateTimeFormat("ar-SA", {
                        dateStyle: "medium",
                        timeStyle: "short",
                      }).format(new Date(order.createdAtUtc))}
                    </td>
                    <td className="px-5 py-4 text-left">
                      <button
                        type="button"
                        disabled={detailLoading}
                        onClick={() => void openOrder(order.orderId)}
                        className="inline-flex items-center gap-1 text-[8px] font-semibold text-black/52 disabled:opacity-40"
                      >
                        التفاصيل
                        <ChevronLeft size={12} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="border-t border-black/[0.05] px-5 py-3 text-[8px] text-black/28">
          تعرض الصفحة حتى آخر 100 طلب من الخادم الحالي. التقارير التفصيلية ستبقى في قسم مستقل.
        </div>
      </section>

      {selected ? (
        <OrderDetailPanel order={selected} onClose={() => setSelected(null)} />
      ) : null}
    </div>
  );
}

function OrderMetric({
  label,
  value,
  icon: Icon,
}: {
  label: string;
  value: number;
  icon: typeof Box;
}) {
  return (
    <article className="rounded-[14px] border border-black/[0.07] bg-white p-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="text-[8px] text-black/35">{label}</p>
          <p className="mt-2 text-[22px] font-semibold tracking-[-0.04em]">
            {value}
          </p>
        </div>
        <div className="flex size-8 items-center justify-center rounded-[9px] bg-[#f3f1eb] text-black/38">
          <Icon size={15} />
        </div>
      </div>
    </article>
  );
}

function OrderDetailPanel({
  order,
  onClose,
}: {
  order: MerchantOrderDetail;
  onClose: () => void;
}) {
  return (
    <div className="fixed inset-0 z-50 flex justify-end bg-black/30 backdrop-blur-[2px]">
      <button
        type="button"
        aria-label="إغلاق تفاصيل الطلب"
        onClick={onClose}
        className="absolute inset-0 cursor-default"
      />
      <aside className="relative z-10 h-full w-full max-w-[620px] overflow-y-auto bg-[#f7f6f2] shadow-2xl">
        <div className="sticky top-0 z-20 flex items-center justify-between border-b border-black/[0.07] bg-[#f7f6f2]/95 px-6 py-5 backdrop-blur-xl">
          <div>
            <p className="text-[8px] font-semibold text-[#9a713f]">ORDER DETAIL</p>
            <h2 className="mt-1 text-[19px] font-semibold">
              طلب #{order.orderId.slice(0, 8)}
            </h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex size-9 items-center justify-center rounded-full border border-black/[0.08] bg-white"
          >
            <X size={15} />
          </button>
        </div>

        <div className="space-y-4 p-6">
          <section className="grid gap-3 rounded-[15px] border border-black/[0.07] bg-white p-4 sm:grid-cols-2">
            <Detail label="الحالة" value={statusLabel(order)} />
            <Detail label="الدفع" value={order.paymentStatus} />
            <Detail label="العميل" value={order.customerEmail ?? "غير متوفر"} />
            <Detail
              label="الإجمالي"
              value={formatMoney(order.totalAmount, order.currency)}
            />
            <Detail label="شركة الشحن" value={order.shippingCarrier ?? "لم تحدد"} />
            <Detail label="رقم التتبع" value={order.trackingNumber ?? "لم يضاف"} />
          </section>

          <section className="overflow-hidden rounded-[15px] border border-black/[0.07] bg-white">
            <div className="border-b border-black/[0.055] px-4 py-3">
              <h3 className="text-[10px] font-semibold">عناصر الطلب</h3>
            </div>
            <div className="divide-y divide-black/[0.05]">
              {order.items.map((item) => (
                <div
                  key={item.orderItemId}
                  className="flex items-center justify-between gap-4 px-4 py-3"
                >
                  <div className="min-w-0">
                    <p className="truncate text-[9px] font-semibold">
                      {item.productName}
                    </p>
                    <p className="mt-1 text-[8px] text-black/35">
                      {item.variantName} · {item.quantity} × {formatMoney(item.unitPrice, item.currency)}
                    </p>
                  </div>
                  <span className="shrink-0 text-[9px] font-semibold">
                    {formatMoney(item.lineTotal, item.currency)}
                  </span>
                </div>
              ))}
            </div>
          </section>

          <section className="rounded-[15px] border border-black/[0.07] bg-white p-4">
            <h3 className="text-[10px] font-semibold">سجل الطلب</h3>
            <div className="mt-4 space-y-4">
              {order.timeline.length === 0 ? (
                <p className="text-[8px] text-black/35">لا توجد أحداث مسجلة بعد.</p>
              ) : (
                [...order.timeline]
                  .sort(
                    (a, b) =>
                      new Date(b.createdAtUtc).getTime() -
                      new Date(a.createdAtUtc).getTime(),
                  )
                  .map((entry, index) => (
                    <div key={`${entry.createdAtUtc}-${index}`} className="flex gap-3">
                      <div className="mt-1 size-2 shrink-0 rounded-full bg-[#b58a4e]" />
                      <div>
                        <p className="text-[9px] font-semibold">
                          {entry.fulfillmentStatus || entry.orderStatus}
                        </p>
                        {entry.note ? (
                          <p className="mt-1 text-[8px] leading-4 text-black/42">
                            {entry.note}
                          </p>
                        ) : null}
                        <p className="mt-1 text-[7px] text-black/28">
                          {new Intl.DateTimeFormat("ar-SA", {
                            dateStyle: "medium",
                            timeStyle: "short",
                          }).format(new Date(entry.createdAtUtc))}
                        </p>
                      </div>
                    </div>
                  ))
              )}
            </div>
          </section>

          {order.cancellationReason ? (
            <section className="rounded-[15px] border border-red-100 bg-red-50 p-4">
              <p className="text-[9px] font-semibold text-red-700">سبب الإلغاء</p>
              <p className="mt-2 text-[9px] leading-5 text-red-700/75">
                {order.cancellationReason}
              </p>
            </section>
          ) : null}
        </div>
      </aside>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[8px] text-black/32">{label}</p>
      <p className="mt-1.5 text-[9px] font-semibold text-black/70">{value}</p>
    </div>
  );
}
