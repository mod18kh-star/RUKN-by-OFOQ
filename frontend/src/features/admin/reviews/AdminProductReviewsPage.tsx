import { useCallback, useEffect, useMemo, useState } from "react";
import { Check, EyeOff, MessageSquare, RefreshCw, Star } from "lucide-react";
import { authorizedApiFetch } from "../../auth/authSession";
import { readAdminStore } from "../store-setup/storeSetupStorage";

interface MerchantReview {
  id: string;
  productId: string;
  orderId: string;
  rating: number;
  body: string | null;
  isVerifiedPurchase: boolean;
  status: string;
  merchantReply: string | null;
  createdAtUtc: string;
}

type Filter = "Pending" | "Published" | "Hidden" | "All";
const statuses: Record<Filter, string> = {
  Pending: "بانتظار المراجعة", Published: "منشورة", Hidden: "مخفية", All: "كل التقييمات",
};

export function AdminProductReviewsPage() {
  const tenantId = readAdminStore()?.tenantId ?? "";
  const [reviews, setReviews] = useState<MerchantReview[]>([]);
  const [filter, setFilter] = useState<Filter>("Pending");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [draftReplies, setDraftReplies] = useState<Record<string, string>>({});
  const base = `/api/tenants/${encodeURIComponent(tenantId)}/backoffice/reviews`;

  const load = useCallback(async () => {
    if (!tenantId) { setError("لا يوجد متجر محدد."); setLoading(false); return; }
    setLoading(true); setError("");
    try {
      const response = await authorizedApiFetch(`${base}?take=100`);
      if (!response.ok) throw new Error(response.status === 403 ? "ليس لديك صلاحية إدارة التقييمات." : "تعذر تحميل التقييمات.");
      const rows = (await response.json()) as MerchantReview[];
      setReviews(rows);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "تعذر تحميل التقييمات.");
    } finally { setLoading(false); }
  }, [base, tenantId]);

  useEffect(() => {
    const id = window.setTimeout(() => { void load(); }, 0);
    return () => window.clearTimeout(id);
  }, [load]);

  const visible = useMemo(() => reviews.filter(review => filter === "All" || review.status === filter), [reviews, filter]);
  async function perform(review: MerchantReview, action: "publish" | "hide" | "reply") {
    if (busy) return;
    setBusy(review.id); setError(""); setMessage("");
    try {
      const response = await authorizedApiFetch(`${base}/${encodeURIComponent(review.id)}/${action}`, {
        method: action === "reply" ? "PUT" : "POST",
        ...(action === "reply" ? { body: JSON.stringify({ reply: (draftReplies[review.id] ?? review.merchantReply ?? "").trim() || null }) } : {}),
      });
      if (!response.ok) throw new Error(response.status === 403 ? "لا تملك صلاحية تنفيذ العملية." : "تعذر حفظ التعديل. حاول مجددًا.");
      setMessage(action === "publish" ? "تم نشر التقييم." : action === "hide" ? "تم إخفاء التقييم." : "تم حفظ رد المتجر.");
      await load();
    } catch (caught) { setError(caught instanceof Error ? caught.message : "تعذر تنفيذ العملية."); }
    finally { setBusy(null); }
  }

  return <main dir="rtl" className="mx-auto max-w-[1180px] space-y-5 pb-14 text-[#17251e]">
    <header className="flex flex-wrap items-end justify-between gap-3 border-b border-[#e1e7e1] pb-5">
      <div><p className="text-xs font-semibold text-[#315F5B]">خدمة العملاء</p>
        <h1 className="mt-2 text-3xl font-semibold">تقييمات المنتجات</h1>
        <p className="mt-2 text-sm text-[#59675d]">راجع تقييمات المشترين الموثّقة، وانشرها أو أخفها، وأضف ردًا من المتجر.</p></div>
      <button type="button" onClick={() => void load()} disabled={loading} className="inline-flex min-h-10 items-center gap-2 rounded-xl border border-black/15 bg-white px-4 text-sm disabled:opacity-50"><RefreshCw size={16}/> تحديث</button>
    </header>
    {error && <p role="alert" className="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-800">{error}</p>}
    {message && <p role="status" className="rounded-xl bg-emerald-50 p-3 text-sm text-emerald-800">{message}</p>}
    <div role="group" aria-label="حالة التقييم" className="flex flex-wrap gap-2">
      {(["Pending", "Published", "Hidden", "All"] as const).map(status => <button type="button" key={status} aria-pressed={filter === status} onClick={() => setFilter(status)}
        className={`min-h-10 rounded-lg px-4 text-sm font-semibold ${filter === status ? "bg-[#264d46] text-white" : "border border-black/10 bg-white text-[#264d46]"}`}>{statuses[status]} ({status === "All" ? reviews.length : reviews.filter(review => review.status === status).length})</button>)}
    </div>
    {loading && <p className="text-sm text-[#536359]">جاري تحميل التقييمات...</p>}
    {!loading && visible.length === 0 && <p className="rounded-xl border border-black/10 bg-white p-6 text-sm text-[#637369]">لا توجد تقييمات في هذا القسم.</p>}
    <div className="grid gap-4 lg:grid-cols-2">{visible.map(review => <article key={review.id} className="min-w-0 space-y-3 rounded-xl border border-black/10 bg-white p-5">
      <div className="flex items-center justify-between gap-3">
        <span className="flex items-center gap-1" aria-label={`${review.rating} من خمس نجوم`}>{[1,2,3,4,5].map(value => <Star key={value} size={16} color={value <= review.rating ? "#AC782C" : "#9AA49C"} fill={value <= review.rating ? "#AC782C" : "none"}/>)}</span>
        <span className="rounded-full bg-[#edf5f0] px-2 py-1 text-xs text-[#2a6349]">{review.isVerifiedPurchase ? "شراء موثّق" : "غير موثّق"}</span>
      </div>
      <p className="text-xs text-[#637369]">منتج #{review.productId.slice(0, 8)} · طلب #{review.orderId.slice(0, 8)}</p>
      {review.body && <p className="whitespace-pre-wrap break-words text-sm leading-7">{review.body}</p>}
      <p className="text-xs text-[#66766C]">{new Date(review.createdAtUtc).toLocaleDateString("ar-SA")}</p>
      <label className="block text-xs font-semibold text-[#4b6254]">رد المتجر
        <textarea maxLength={2000} rows={2} disabled={Boolean(busy)} value={draftReplies[review.id] ?? review.merchantReply ?? ""}
          onChange={event => setDraftReplies(prev => ({...prev, [review.id]:event.target.value}))}
          placeholder="اكتب ردًا مناسبًا للعميل..." className="mt-2 w-full rounded-lg border border-black/15 bg-white p-3 text-sm font-normal focus:border-[#315F5B] focus:outline-none"/>
      </label>
      <div className="flex flex-wrap gap-2">
        <button type="button" disabled={Boolean(busy)} onClick={() => void perform(review,"reply")} className="inline-flex min-h-10 items-center gap-2 rounded-lg border border-black/15 px-3 text-sm disabled:opacity-50"><MessageSquare size={15}/> حفظ الرد</button>
        {review.status !== "Published" && <button type="button" disabled={Boolean(busy)} onClick={() => void perform(review,"publish")} className="inline-flex min-h-10 items-center gap-2 rounded-lg bg-[#264d46] px-3 text-sm font-semibold text-white disabled:opacity-50"><Check size={15}/> نشر</button>}
        {review.status !== "Hidden" && <button type="button" disabled={Boolean(busy)} onClick={() => void perform(review,"hide")} className="inline-flex min-h-10 items-center gap-2 rounded-lg border border-red-200 px-3 text-sm font-semibold text-red-700 disabled:opacity-50"><EyeOff size={15}/> إخفاء</button>}
      </div>
    </article>)}</div>
  </main>;
}
