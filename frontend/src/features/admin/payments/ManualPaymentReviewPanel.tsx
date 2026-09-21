import { useCallback, useEffect, useState } from "react";
import { CheckCircle2, Download, FileImage, Phone, RefreshCw, XCircle } from "lucide-react";
import { fetchReviewReceipt, listManualReviews, reviewManualPayment, type ManualReviewOrder } from "./manualReviewApi";

type ReviewTab = "PendingReview" | "Approved" | "Rejected";
export function ManualPaymentReviewPanel({ tenantId, onChanged }: { tenantId: string; onChanged?: () => void }) {
  const [tab, setTab] = useState<ReviewTab>("PendingReview");
  const [rows, setRows] = useState<ManualReviewOrder[]>([]);
  const [busy, setBusy] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [rejectId, setRejectId] = useState("");
  const [reason, setReason] = useState("");
  const [preview, setPreview] = useState<string | null>(null);
  const [previewOrderId, setPreviewOrderId] = useState<string | null>(null);
  const load = useCallback(async () => {
    if (!tenantId) { setLoading(false); return; }
    setLoading(true);
    try { setRows(await listManualReviews(tenantId)); setError(""); }
    catch (caught) { setError(caught instanceof Error ? caught.message : "تعذر تحميل المدفوعات."); }
    finally { setLoading(false); }
  }, [tenantId]);
  useEffect(() => {
    let active = true;
    // Initial fetch is asynchronous; interactive refresh still uses load().
    void listManualReviews(tenantId)
      .then(nextRows => {
        if (!active) return;
        setRows(nextRows);
        setError("");
      })
      .catch(caught => {
        if (active) setError(caught instanceof Error ? caught.message : "تعذر تحميل المدفوعات.");
      })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [tenantId]);
  useEffect(() => () => { if (preview) URL.revokeObjectURL(preview); }, [preview]);
  async function showReceipt(orderId: string) {
    if (busy) return; setBusy(orderId); setError("");
    try {
      const receiptUrl = await fetchReviewReceipt(tenantId, orderId);
      setPreview(receiptUrl);
      setPreviewOrderId(orderId);
    }
    catch (caught) { setError(caught instanceof Error ? caught.message : "تعذر فتح الإيصال."); }
    finally { setBusy(""); }
  }
  async function downloadReceipt(orderId: string, openReceiptUrl?: string) {
    if (busy) return;
    setBusy(orderId);
    setError("");
    let temporaryUrl: string | null = null;
    try {
      // The authenticated API returns an in-memory blob URL, not a public receipt link.
      const receiptUrl = openReceiptUrl ?? (temporaryUrl = await fetchReviewReceipt(tenantId, orderId));
      const response = await fetch(receiptUrl);
      if (!response.ok) throw new Error("تعذر قراءة صورة الإيصال.");
      const image = await response.blob();
      if (image.size === 0 || image.size > 1024 * 1024 || !["image/png", "image/jpeg"].includes(image.type)) {
        throw new Error("صيغة صورة الإيصال غير مدعومة.");
      }
      const extension = image.type === "image/png" ? "png" : "jpg";
      const link = document.createElement("a");
      link.href = receiptUrl;
      link.download = `rukn-transfer-receipt-${orderId.slice(0, 8)}.${extension}`;
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "تعذر تحميل صورة الإيصال.");
    } finally {
      // Leave time for the browser download to start before revoking a temporary blob URL.
      if (temporaryUrl !== null) {
        const objectUrlToRevoke = temporaryUrl;
        window.setTimeout(() => URL.revokeObjectURL(objectUrlToRevoke), 60_000);
      }
      setBusy("");
    }
  }
  async function submitReview(order: ManualReviewOrder, approve: boolean) {
    if (busy || order.status !== "PendingReview") return;
    if (!approve && (!reason.trim() || reason.length > 500)) {
      setError("يجب إدخال سبب رفض واضح لا يتجاوز 500 حرف."); return;
    }
    if (approve && !window.confirm("هل تحققت من وصول المبلغ إلى حساب المتجر ومطابقة قيمة الطلب؟")) return;
    setBusy(order.orderId); setError(""); setNotice("");
    try {
      await reviewManualPayment(tenantId, order.orderId, approve, approve ? null : reason.trim());
      setRejectId(""); setReason("");
      setNotice(approve ? "تم تأكيد الدفع، وانتقل الطلب إلى قيد التجهيز." : "تم رفض الإثبات مع الاحتفاظ بالطلب وإتاحة إرسال إيصال جديد.");
      await load(); onChanged?.();
    } catch (caught) { setError(caught instanceof Error ? caught.message : "تعذرت مراجعة الإيصال."); }
    finally { setBusy(""); }
  }
  return <section dir="rtl" className="mt-6 space-y-4 rounded-2xl border border-black/10 bg-white p-4 sm:p-6">
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div><p className="text-xs font-semibold text-[#315F5B]">مدفوعات المتجر</p>
        <h2 className="mt-1 text-lg font-semibold">مراجعة إثباتات التحويل</h2>
        <p className="mt-2 text-xs leading-6 text-black/55">لا تؤكد الدفع إلا بعد التحقق من وصول المال إلى حسابك. رفض الإيصال لا يلغي الطلب.</p></div>
      <button type="button" disabled={!!busy} onClick={() => void load()} className="flex items-center gap-2 rounded-xl border px-4 py-2 text-sm"><RefreshCw size={15}/> تحديث</button>
    </div>
    <div className="flex flex-wrap gap-2">{([
      ["PendingReview", "بانتظار تأكيد الدفع"], ["Approved", "المؤكدة / قيد التجهيز"], ["Rejected", "الإثباتات المرفوضة"],
    ] as const).map(([value, label]) => <button key={value} type="button" onClick={() => setTab(value)}
      className={`rounded-xl px-4 py-2 text-sm ${tab === value ? "bg-[#315F5B] text-white" : "border bg-white text-[#315F5B]"}`}>
      {label} ({rows.filter(r => r.status === value).length})</button>)}</div>
    {error && <div role="alert" className="rounded-xl bg-red-50 p-4 text-sm text-red-800">{error}</div>}
    {notice && <div role="status" className="rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800">{notice}</div>}
    {loading ? <p className="text-sm">جاري تحميل الطلبات…</p> :
      rows.filter(r => r.status === tab).length === 0 ? <p className="rounded-xl bg-[#f7f8f6] p-5 text-sm text-black/50">لا توجد طلبات في هذا القسم.</p> :
      <div className="grid gap-3 lg:grid-cols-2">{rows.filter(r => r.status === tab).map(order =>
        <article key={order.orderId} className="space-y-3 rounded-xl border border-black/10 p-4">
          <div className="flex items-start justify-between gap-3"><div className="min-w-0"><h3 className="font-semibold">طلب #{order.orderId.slice(0, 8)}</h3>
            <p className="mt-1 text-sm text-black/60">{order.customerName} · {order.accountName}</p></div>
            <strong className="shrink-0 text-sm">{order.amount.toLocaleString("ar-SA")} {order.currency}</strong></div>
          <p className="break-all text-xs text-black/50">رقم الطلب: {order.orderId}</p>
          <div className="flex flex-wrap items-center gap-3 text-sm"><span>رقم العميل: <b dir="ltr">{order.customerPhone || "غير متوفر"}</b></span>
            {order.customerPhone && <a href={`tel:${order.customerPhone.replace(/[^+\d]/g, "")}`}
              className="inline-flex items-center gap-1 rounded-lg border px-2 py-1 text-[#315F5B]"><Phone size={14}/> اتصال</a>}</div>
          {order.receiptId && <div className="flex flex-wrap items-center gap-2">
            <button type="button" disabled={!!busy} onClick={() => void showReceipt(order.orderId)}
              className="inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm text-[#315F5B] disabled:opacity-50"><FileImage size={17}/> عرض الإيصال</button>
            <button type="button" disabled={!!busy} onClick={() => void downloadReceipt(order.orderId)}
              className="inline-flex items-center gap-2 rounded-lg bg-[#315F5B] px-3 py-2 text-sm font-semibold text-white disabled:opacity-50"><Download size={17}/> تحميل الإيصال</button>
          </div>}
          {order.status === "Rejected" && <p className="rounded-lg bg-red-50 p-3 text-sm text-red-800">سبب الرفض: {order.rejectionReason}</p>}
          {order.status === "PendingReview" && <>
            <div className="flex flex-wrap gap-2">
              <button type="button" disabled={!!busy} onClick={() => void submitReview(order, true)} className="inline-flex items-center gap-2 rounded-lg bg-[#315F5B] px-4 py-2 text-sm text-white disabled:opacity-50"><CheckCircle2 size={16}/> تأكيد وصول الدفع</button>
              <button type="button" disabled={!!busy} onClick={() => { setRejectId(rejectId === order.orderId ? "" : order.orderId); setReason(""); }} className="inline-flex items-center gap-2 rounded-lg border border-red-200 px-4 py-2 text-sm text-red-700"><XCircle size={16}/> رفض الإثبات</button>
            </div>
            {rejectId === order.orderId && <div className="space-y-2"><label className="block text-sm">سبب الرفض الذي سيظهر للعميل
              <textarea required maxLength={500} rows={2} value={reason} onChange={e => setReason(e.target.value)} className="mt-2 block w-full rounded-xl border p-3" /></label>
              <button type="button" disabled={!!busy || !reason.trim()} onClick={() => void submitReview(order, false)} className="rounded-lg bg-red-700 px-4 py-2 text-sm text-white disabled:opacity-50">تسجيل الرفض</button>
            </div>}
          </>}
        </article>)}</div>}
    {preview && <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4" role="dialog" aria-modal="true" aria-label="صورة الإيصال">
      <div className="max-h-[90vh] w-full max-w-2xl space-y-3 overflow-auto rounded-2xl bg-white p-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <strong>صورة الإيصال — لا تؤكد الدفع قبل التحقق من البنك</strong>
          <div className="flex flex-wrap items-center gap-2">
            {previewOrderId && <button type="button" disabled={!!busy} onClick={() => void downloadReceipt(previewOrderId, preview)}
              className="inline-flex items-center gap-2 rounded-lg bg-[#315F5B] px-3 py-2 text-sm font-semibold text-white disabled:opacity-50"><Download size={16}/> تحميل الإيصال</button>}
            <button type="button" onClick={() => { setPreview(null); setPreviewOrderId(null); }} className="rounded-lg border px-3 py-2 text-sm">إغلاق ×</button>
          </div>
        </div>
        <img src={preview} alt="إيصال التحويل المرفق من العميل" className="mx-auto max-h-[68vh] w-auto max-w-full object-contain" />
      </div></div>}
  </section>;
}
