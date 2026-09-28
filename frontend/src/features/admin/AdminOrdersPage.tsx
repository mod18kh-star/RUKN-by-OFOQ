import {
  Archive, ArrowLeft, Check, CheckCircle2, ChevronLeft, Clock3,
  CreditCard, FilterX, Package, RefreshCw, Search, Truck, X,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router";
import {
  AdminOperationApiError, changeMerchantOrderState,
  getMerchantOrderById, getMerchantOrders,
  type MerchantOrderDetail, type MerchantOrderSummary,
} from "./dashboard/adminDashboardApi";
import { readAdminStore } from "./store-setup/storeSetupStorage";
import { ManualPaymentReviewPanel } from "./payments/ManualPaymentReviewPanel";

type Tab = "attention" | "processing" | "shipping" | "archive" | "all";
type Sort = "latest" | "oldest" | "highest" | "lowest";
type Action = Parameters<typeof changeMerchantOrderState>[2];
const filters: { id: Tab; title: string }[] = [
  { id: "attention", title: "تحتاج إلى إجراء" },
  { id: "processing", title: "قيد التجهيز" },
  { id: "shipping", title: "التوصيل والاستلام" },
  { id: "archive", title: "الأرشيف" },
  { id: "all", title: "كل الطلبات المؤكدة" },
];
const safe = (value: string | null | undefined) => (value || "").toLowerCase();
const isArchive = (order: MerchantOrderSummary) => ["cancelled", "fulfilled", "refunded", "returned"]
  .includes(safe(order.orderStatus)) || ["cancelled", "delivered", "fulfilled", "returned"].includes(safe(order.fulfillmentStatus));
const isShipping = (order: MerchantOrderSummary) => ["readytoship", "shipped", "intransit"]
  .includes(safe(order.fulfillmentStatus));
const isProcessing = (order: MerchantOrderSummary) => !isArchive(order) && !isShipping(order)
  && ["confirmed", "processing", "paid"].includes(safe(order.orderStatus));
function inTab(order: MerchantOrderSummary, tab: Tab) {
  if (tab === "all") return true;
  if (tab === "archive") return isArchive(order);
  if (tab === "shipping") return isShipping(order);
  if (tab === "processing") return isProcessing(order);
  return isProcessing(order) || isShipping(order);
}
function statusText(order: MerchantOrderSummary) {
  const status = safe(order.orderStatus); const fulfillment = safe(order.fulfillmentStatus);
  if (status === "cancelled") return "ملغي";
  if (status === "returned" || status === "refunded") return "مرتجع";
  if (fulfillment === "delivered" || status === "fulfilled") return "تم التسليم";
  if (fulfillment === "intransit") return "قيد التوصيل";
  if (fulfillment === "shipped") return "تم الشحن";
  if (fulfillment === "readytoship") return "جاهز للتسليم";
  if (fulfillment === "processing") return "قيد التجهيز";
  if (status === "confirmed") return "مؤكد — بانتظار التجهيز";
  if (status === "paid") return "مدفوع — بانتظار التأكيد";
  return "يتطلب مراجعة";
}
const money = (amount: number, currency: string) => {
  try { return new Intl.NumberFormat("ar-SA", { style: "currency", currency }).format(amount); }
  catch { return `${amount.toLocaleString("ar-SA")} ${currency}`; }
};
const date = (utc: string) => new Intl.DateTimeFormat("ar-SA", { dateStyle: "medium", timeStyle: "short" }).format(new Date(utc));
const field = "h-11 w-full min-w-0 rounded-xl border border-[#dbdfdb] bg-white px-3 text-sm text-[#182820] outline-none focus:border-[#315F5B] focus:ring-2 focus:ring-[#315F5B]/15";
const button = "inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-[#264d46] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#18372f] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#264d46] disabled:cursor-not-allowed disabled:opacity-50";

export function AdminOrdersPage() {
  const store = readAdminStore();
  const tenantId = store?.tenantId ?? "";
  const [orders, setOrders] = useState<MerchantOrderSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [blocked, setBlocked] = useState(false);
  const dashboardView = new URLSearchParams(window.location.search).get("view");
  const [tab, setTab] = useState<Tab>("attention");
  const [query, setQuery] = useState("");
  const [sort, setSort] = useState<Sort>("latest");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [selected, setSelected] = useState<MerchantOrderDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [shippingCarrier, setShippingCarrier] = useState("");
  const [trackingNumber, setTrackingNumber] = useState("");
  const [notice, setNotice] = useState("");

  const load = useCallback(async () => {
    if (!tenantId) { setLoading(false); setError("لم يتم العثور على متجر حالي."); return; }
    setLoading(true); setError(""); setBlocked(false);
    try { setOrders(await getMerchantOrders(tenantId, 100)); }
    catch (caught) {
      if (caught instanceof AdminOperationApiError && caught.status === 403) setBlocked(true);
      setError(caught instanceof Error ? caught.message : "تعذر تحميل الطلبات.");
    } finally { setLoading(false); }
  }, [tenantId]);
  useEffect(() => { const id = window.setTimeout(() => { void load(); }, 0); return () => window.clearTimeout(id); }, [load]);

  const counts = useMemo(() => ({
    attention: orders.filter(x => inTab(x, "attention")).length,
    processing: orders.filter(x => inTab(x, "processing")).length,
    shipping: orders.filter(x => inTab(x, "shipping")).length,
    archive: orders.filter(x => inTab(x, "archive")).length,
  }), [orders]);
  const visible = useMemo(() => {
    const term = query.trim().toLowerCase();
    return orders.filter(order => inTab(order, tab) &&
      (dashboardView !== "paid" || safe(order.orderStatus) === "paid") &&
      (!from || order.createdAtUtc.slice(0, 10) >= from) &&
      (!to || order.createdAtUtc.slice(0, 10) <= to) &&
      (!term || [order.orderId, order.customerEmail ?? "", order.trackingNumber ?? "",
        order.orderStatus, order.fulfillmentStatus].some(value => value.toLowerCase().includes(term)))
    ).sort((a, b) => sort === "highest" ? b.totalAmount - a.totalAmount :
      sort === "lowest" ? a.totalAmount - b.totalAmount :
      sort === "oldest" ? a.createdAtUtc.localeCompare(b.createdAtUtc) : b.createdAtUtc.localeCompare(a.createdAtUtc));
  }, [orders, tab, query, sort, from, to, dashboardView]);
  async function open(id: string) {
    if (!tenantId) return;
    setDetailLoading(true); setError(""); setNotice("");
    try {
      const order = await getMerchantOrderById(tenantId, id);
      setSelected(order);
      setShippingCarrier(order.shippingCarrier ?? "");
      setTrackingNumber(order.trackingNumber ?? "");
    } catch (caught) { setError(caught instanceof Error ? caught.message : "تعذر تحميل التفاصيل."); }
    finally { setDetailLoading(false); }
  }
  async function advance(action: Action, title: string) {
    if (!tenantId || !selected || busy || !window.confirm(`تأكيد: ${title}؟`)) return;
    setBusy(true); setError(""); setNotice("");
    try {
      await changeMerchantOrderState(tenantId, selected.orderId, action,
        action === "ship" ? { shippingCarrier: shippingCarrier.trim(), trackingNumber: trackingNumber.trim() } : undefined);
      const updated = await getMerchantOrderById(tenantId, selected.orderId);
      setSelected(updated);
      setNotice(`تم تحديث الطلب: ${title}`);
      await load();
    } catch (caught) { setError(caught instanceof Error ? caught.message : "تعذر تحديث الطلب."); }
    finally { setBusy(false); }
  }
  const active = selected;
  const action: { id: Action; title: string } | null = active?.orderStatus.toLowerCase() === "paid"
    ? { id: "confirm", title: "تأكيد الطلب" }
    : active?.orderStatus.toLowerCase() === "confirmed"
      ? { id: "processing", title: "بدء تجهيز الطلب" }
      : active?.orderStatus.toLowerCase() === "processing" && active.fulfillmentStatus.toLowerCase() === "processing"
        ? { id: "ready-to-ship", title: "تجهيز الطلب اكتمل" }
        : active?.fulfillmentStatus.toLowerCase() === "readytoship"
          ? safe(active.shippingMethodType) === "pickup"
            ? { id: "collect", title: "تأكيد استلام العميل للطلب" }
            : { id: "ship", title: "تأكيد تسليم الطلب لشركة التوصيل" }
          : active?.fulfillmentStatus.toLowerCase() === "shipped"
            ? { id: "in-transit", title: "الطلب قيد التوصيل" }
            : active?.fulfillmentStatus.toLowerCase() === "intransit"
              ? { id: "deliver", title: "تأكيد تسليم الطلب للعميل" } : null;

  return <main dir="rtl" className="mx-auto max-w-[1440px] space-y-6 pb-14 text-[#17251e]">
    <header className="flex flex-col gap-4 border-b border-[#e1e7e1] pb-6 md:flex-row md:items-end md:justify-between">
      <div><span className="text-xs font-semibold tracking-wide text-[#315F5B]">العمليات اليومية</span>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight md:text-4xl">إدارة الطلبات</h1>
        <p className="mt-2 text-sm leading-7 text-[#56635b]">طلبات مؤكدة الدفع، من بدء التجهيز حتى التسليم. إثباتات الدفع لها قسم مراجعة مستقل.</p></div>
      <button type="button" className={button} onClick={() => void load()} disabled={loading}><RefreshCw size={17}/> تحديث الطلبات</button>
    </header>
    {tenantId && <ManualPaymentReviewPanel tenantId={tenantId} onChanged={() => void load()} />}
    {blocked && <div role="alert" className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm">أكمل التحقق الثنائي للوصول إلى بيانات الإدارة. <Link to="/admin/settings" className="font-semibold underline">إعدادات الحساب</Link></div>}
    {error && <p role="alert" className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">{error}</p>}
    {notice && <p role="status" className="rounded-xl bg-[#e4f1e9] p-4 text-sm text-[#1c5138]">{notice}</p>}
    <section aria-label="ملخص الطلبات" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      {([{ label: "تحتاج إلى إجراء", count: counts.attention, icon: Clock3, id: "attention" },
        { label: "قيد التجهيز", count: counts.processing, icon: Package, id: "processing" },
        { label: "التوصيل والاستلام", count: counts.shipping, icon: Truck, id: "shipping" },
        { label: "الأرشيف", count: counts.archive, icon: Archive, id: "archive" }] as const).map(item => <button key={item.id}
        type="button" onClick={() => setTab(item.id)} aria-pressed={tab === item.id}
        className={`flex items-center justify-between rounded-2xl border p-5 text-right transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#315F5B] ${tab === item.id ? "border-[#315F5B] bg-[#ebf2ed]" : "border-[#dce4df] bg-white hover:border-[#315F5B]/50"}`}>
        <div><p className="text-sm text-[#526159]">{item.label}</p><strong className="mt-2 block text-3xl tabular-nums">{item.count}</strong></div><item.icon className="text-[#315F5B]" size={22}/>
      </button>)}
    </section>
    <section className="overflow-hidden rounded-2xl border border-[#dce4df] bg-white shadow-sm">
      <div className="flex flex-wrap gap-2 border-b border-[#e6ebe7] p-4 sm:p-5" role="group" aria-label="تصفية حسب الحالة">
        {filters.map(item => <button key={item.id} type="button" aria-pressed={tab === item.id} onClick={() => setTab(item.id)}
          className={`min-h-10 rounded-lg px-4 py-2 text-sm font-medium ${tab === item.id ? "bg-[#264d46] text-white" : "bg-[#f3f6f4] text-[#34483b] hover:bg-[#e4eee7]"}`}>{item.title}</button>)}
      </div>
      <div className="grid gap-3 border-b border-[#e6ebe7] p-4 md:grid-cols-2 lg:grid-cols-5 sm:p-5">
        <label className="lg:col-span-2"><span className="mb-1 block text-xs font-medium">بحث في الطلبات المعروضة</span>
          <div className="relative"><Search size={18} className="absolute right-3 top-3.5 text-[#53665a]"/>
            <input className={`${field} pr-10`} value={query} onChange={e => setQuery(e.target.value)} placeholder="رقم الطلب، العميل، أو رقم التتبع"/></div></label>
        <label><span className="mb-1 block text-xs font-medium">من تاريخ</span><input type="date" className={field} value={from} onChange={e => setFrom(e.target.value)}/></label>
        <label><span className="mb-1 block text-xs font-medium">إلى تاريخ</span><input type="date" className={field} value={to} min={from || undefined} onChange={e => setTo(e.target.value)}/></label>
        <label><span className="mb-1 block text-xs font-medium">ترتيب</span><select className={field} value={sort} onChange={e => setSort(e.target.value as Sort)}>
          <option value="latest">الأحدث أولًا</option><option value="oldest">الأقدم أولًا</option>
          <option value="highest">القيمة الأعلى</option><option value="lowest">القيمة الأقل</option>
        </select></label>
        {(query || from || to) && <button className="inline-flex min-h-10 items-center gap-2 text-sm text-[#315F5B]" onClick={() => {setQuery("");setFrom("");setTo("");}}><FilterX size={16}/> مسح الفلاتر</button>}
      </div>
      {loading ? <div role="status" className="p-12 text-center text-sm text-[#526159]">جاري تحميل الطلبات…</div> : visible.length === 0 ?
        <div className="p-12 text-center"><CheckCircle2 size={28} className="mx-auto text-[#789687]"/><p className="mt-3 text-base font-medium">لا توجد طلبات ضمن هذه النتائج</p><p className="mt-2 text-sm text-[#6a7a70]">يمكنك تغيير الفلاتر أو مراجعة قسم إثباتات الدفع أعلاه.</p></div> :
        <div className="overflow-x-auto"><table className="w-full min-w-[780px] border-collapse text-right text-sm">
          <thead className="bg-[#f7f9f7] text-[#536359]"><tr>{["رقم الطلب", "العميل", "المرحلة", "الإجمالي", "التاريخ", ""].map((label,i)=><th key={i} className="px-5 py-4 font-medium">{label}</th>)}</tr></thead>
          <tbody className="divide-y divide-[#e8ede9]">{visible.map(order => <tr key={order.orderId} className="hover:bg-[#f8faf8]">
            <td className="px-5 py-4 font-semibold" dir="ltr">#{order.orderId.slice(0,8)}</td>
            <td className="px-5 py-4"><span className="block max-w-[210px] truncate" title={order.customerEmail ?? ""}>{order.customerEmail ?? "عميل"}</span><span className="text-xs text-[#66776a]">{order.totalQuantity} قطعة</span></td>
            <td className="px-5 py-4"><span className="inline-flex rounded-lg bg-[#ecf3ed] px-3 py-1.5 text-xs font-semibold text-[#234b38]">{statusText(order)}</span></td>
            <td className="px-5 py-4 font-semibold" dir="auto">{money(order.totalAmount,order.currency)}</td>
            <td className="px-5 py-4 text-[#596b5e]">{date(order.createdAtUtc)}</td>
            <td className="px-5 py-4"><button type="button" disabled={detailLoading} className="inline-flex min-h-10 items-center gap-1 rounded-lg px-3 text-[#264d46] underline-offset-4 hover:underline disabled:opacity-50" onClick={()=>void open(order.orderId)}>التفاصيل<ChevronLeft size={16}/></button></td>
          </tr>)}</tbody></table></div>}
      <p className="border-t border-[#e6ebe7] px-5 py-3 text-xs text-[#5e7063]">تُعرض آخر 100 طلب مؤكد من الخادم. البحث والفرز في هذه النسخة ينطبقان على الطلبات المحمّلة فقط؛ ستضاف صفحات النتائج والبحث الشامل في تحديث منفصل.</p>
    </section>
    {active && <div className="fixed inset-0 z-50 flex justify-end bg-[#0e221a]/55" role="presentation">
      <button type="button" className="absolute inset-0" aria-label="إغلاق التفاصيل" onClick={()=>setSelected(null)}/>
      <aside role="dialog" aria-modal="true" aria-label="تفاصيل الطلب" dir="rtl" className="relative z-10 flex h-full w-full max-w-2xl flex-col bg-[#f7f9f7] shadow-2xl">
        <header className="flex items-center justify-between border-b border-[#dce5de] bg-white px-5 py-4"><div><p className="text-xs text-[#607465]">تفاصيل الطلب</p><h2 className="text-xl font-semibold">#{active.orderId.slice(0,8)}</h2></div>
          <button type="button" onClick={()=>setSelected(null)} aria-label="إغلاق التفاصيل" className="flex size-11 items-center justify-center rounded-lg border border-[#dae1da]"><X size={20}/></button></header>
        <div className="flex-1 space-y-4 overflow-y-auto p-4 sm:p-6">
          <section className="grid gap-3 rounded-xl border border-[#e0e6e1] bg-white p-4 sm:grid-cols-2">
            {([ ["حالة الطلب",statusText(active)], ["العميل",active.customerEmail ?? "غير متوفر"], ["الإجمالي",money(active.totalAmount,active.currency)],
              ["المستلم",active.shippingRecipientName ?? "غير متوفر"],["رقم التواصل",active.shippingRecipientPhone ?? "غير متوفر"],
              ["العنوان", [active.shippingCity,active.shippingRegion,active.shippingAddressLine1,active.shippingAddressLine2].filter(Boolean).join("، ") || "الاستلام من المتجر / غير محدد"],
              ["طريقة التسليم",active.shippingMethodName ?? "غير متوفر"], ["رقم التتبع",active.trackingNumber ?? "لم يُحدد"] ] as [string,string][]).map(([label,value])=><div key={label}><p className="text-xs text-[#66786b]">{label}</p><p className="mt-1 break-words text-sm font-semibold">{value}</p></div>)}
          </section>
          <section className="rounded-xl border border-[#e0e6e1] bg-white p-4"><h3 className="text-base font-semibold">المنتجات</h3><div className="mt-3 divide-y divide-[#e8ede9]">{active.items.map(item=><div key={item.orderItemId} className="flex justify-between gap-4 py-3 text-sm"><span>{item.productName} · {item.variantName} × {item.quantity}</span><strong className="shrink-0" dir="auto">{money(item.lineTotal,item.currency)}</strong></div>)}</div></section>
          <section className="rounded-xl border border-[#e0e6e1] bg-white p-4"><h3 className="text-base font-semibold">سجل الإجراءات</h3><ol className="mt-3 space-y-3 text-sm">{[...active.timeline].sort((a,b)=>b.createdAtUtc.localeCompare(a.createdAtUtc)).map((entry,i)=><li key={`${entry.createdAtUtc}-${i}`} className="border-r-2 border-[#a2bcb0] pr-3"><strong>{entry.note || entry.fulfillmentStatus || entry.orderStatus}</strong><p className="text-xs text-[#697a6e]">{date(entry.createdAtUtc)}</p></li>)}</ol></section>
          {action?.id === "ship" && <section className="rounded-xl border border-[#d7e2da] bg-white p-4"><h3 className="font-semibold">معلومات الشحن</h3>
            <p className="mt-1 text-xs text-[#607164]">أدخل شركة التوصيل ورقم التتبع الفعلي قبل تحديث الحالة.</p>
            <div className="mt-3 grid gap-3 sm:grid-cols-2"><label className="text-sm">شركة التوصيل<input maxLength={120} className={`${field} mt-1`} value={shippingCarrier} onChange={e=>setShippingCarrier(e.target.value)}/></label>
              <label className="text-sm">رقم التتبع<input maxLength={200} className={`${field} mt-1`} value={trackingNumber} onChange={e=>setTrackingNumber(e.target.value)}/></label></div></section>}
          {error && <p role="alert" className="rounded-xl bg-red-50 p-3 text-sm text-red-800">{error}</p>}
          {notice && <p role="status" className="rounded-xl bg-green-50 p-3 text-sm text-green-800">{notice}</p>}
        </div>
        <footer className="flex items-center justify-between gap-3 border-t border-[#dce5de] bg-white px-5 py-4">
          <span className="text-xs text-[#627365]">تُسجل كل عملية في سجل الطلب.</span>
          {action && <button type="button" className={button} disabled={busy || (action.id === "ship" && (!shippingCarrier.trim() || !trackingNumber.trim()))}
            onClick={()=>void advance(action.id,action.title)}><Check size={17}/>{busy ? "جاري الحفظ…" : action.title}<ArrowLeft size={16}/></button>}
          {!action && <span className="inline-flex items-center gap-2 text-sm text-[#476655]"><CreditCard size={16}/> لا يوجد إجراء متاح لهذه المرحلة</span>}
        </footer>
      </aside>
    </div>}
  </main>;
}