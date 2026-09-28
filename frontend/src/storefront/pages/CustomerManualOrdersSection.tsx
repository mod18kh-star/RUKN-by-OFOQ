import { useEffect, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router";
import { Star } from "lucide-react";
import { getMyManualOrders, type CustomerManualOrder, type CustomerOrderLine } from "../data/manualCheckoutApi";
import { submitVerifiedProductReview } from "../data/customerReviewsApi";

const paymentLabels: Record<CustomerManualOrder["status"], string> = {
  AwaitingMethod: "بانتظار اختيار وسيلة الدفع",
  AwaitingReceipt: "بانتظار رفع إيصال التحويل",
  PendingReview: "بانتظار مراجعة إثبات الدفع",
  Approved: "تم تأكيد الدفع",
  Rejected: "إثبات الدفع مرفوض · يمكنك إعادة رفعه",
  Cancelled: "طلب ملغى",
};
const shippingLabels: Record<string, string> = {
  Unfulfilled: "لم يبدأ التجهيز", Processing: "قيد التجهيز",
  ReadyToShip: "جاهز للإرسال أو الاستلام", Shipped: "تم الشحن",
  InTransit: "قيد التوصيل", Delivered: "تم التسليم", Fulfilled: "تم التسليم",
  Cancelled: "ملغى",
};
const isDelivered = (o: CustomerManualOrder) =>
  o.orderStatus.toLowerCase() === "fulfilled" &&
  ["delivered", "fulfilled"].includes(o.fulfillmentStatus.toLowerCase());

function ProductReviewForm({ storeSlug, item }: { storeSlug: string; item: CustomerOrderLine }) {
  const [rating, setRating] = useState(0);
  const [body, setBody] = useState("");
  const [busy, setBusy] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!rating || busy || sent) return;
    setBusy(true);
    setError("");
    try {
      await submitVerifiedProductReview(storeSlug, item.productId, rating, body);
      setSent(true);
    } catch (caught) {
      const raw = caught instanceof Error ? caught.message : "";
      setError(/already reviewed/i.test(raw) ? "سبق أن قيّمت هذا المنتج. يمكن إضافة تقييم واحد لكل منتج." :
        /delivered|verified purchase/i.test(raw) ? "التقييم متاح بعد تسجيل تسليم طلبك فقط." :
        "تعذر إرسال التقييم. تأكد من حالة الطلب ثم حاول مجددًا.");
    } finally { setBusy(false); }
  }

  return <form onSubmit={(event) => void submit(event)} className="mt-3 rounded-xl border border-black/10 bg-[#fafbf8] p-3">
    <p className="text-sm font-medium text-[#243b30]">تقييم {item.productName}</p>
    {sent ? <p role="status" className="mt-2 text-sm text-emerald-800">شكرًا لك! تم إرسال تقييمك للمراجعة.</p> : <>
      <fieldset className="mt-2 flex items-center gap-2" aria-label={`تقييم ${item.productName}`} disabled={busy}>
        <legend className="sr-only">اختر من نجمة إلى خمس نجوم</legend>
        {[1, 2, 3, 4, 5].map(value => <button type="button" key={value} aria-label={`${value} من 5 نجوم`}
          aria-pressed={rating === value} onClick={() => setRating(value)}
          className="rounded-md p-1 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#315F5B]">
          <Star size={23} fill={value <= rating ? "#AC782C" : "none"} color={value <= rating ? "#AC782C" : "#87958B"} />
        </button>)}
      </fieldset>
      <label className="mt-3 block text-xs text-[#43584A]">رأيك في المنتج (اختياري)
        <textarea value={body} onChange={event => setBody(event.target.value)} maxLength={4000} rows={2}
          className="mt-2 w-full rounded-lg border border-black/15 bg-white p-3 text-sm focus:border-[#315F5B] focus:outline-none"
          placeholder="كيف كانت تجربتك مع المنتج؟" />
      </label>
      {error && <p role="alert" className="mt-2 text-xs text-red-700">{error}</p>}
      <button type="submit" disabled={!rating || busy} className="mt-2 rounded-lg bg-[#234d43] px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" style={{color:"#fff"}}>
        {busy ? "جاري الإرسال..." : "إرسال التقييم"}
      </button>
    </>}
  </form>;
}

export function CustomerManualOrdersSection() {
  const { storeSlug = "" } = useParams<{ storeSlug: string }>();
  const [orders, setOrders] = useState<CustomerManualOrder[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let active = true;
    void getMyManualOrders(storeSlug).then(items => { if (active) setOrders(items); })
      .catch(() => { if (active) setError("تعذر تحميل طلباتك. حاول تحديث الصفحة."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [storeSlug]);
  return <section className="space-y-3 rounded-2xl border border-black/10 bg-white p-5" dir="rtl">
    <h3 className="text-base font-semibold">طلباتك ومشترياتك</h3>
    <p className="text-xs leading-6 text-black/55">يمكنك استكمال دفع الطلبات المحفوظة، ومتابعة حالة التجهيز، وتقييم المنتجات بعد استلامها.</p>
    {loading && <p className="text-sm text-black/60">جار تحميل طلباتك...</p>}
    {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
    {!loading && orders.length === 0 && !error && <p className="text-sm text-black/50">لا توجد طلبات لهذا المتجر حتى الآن.</p>}
    {orders.map(order => <article key={order.orderId} className="rounded-xl border border-black/10 px-4 py-4 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <strong>طلب #{order.orderId.slice(0, 8)}</strong>
        <span className="font-semibold">{order.amount.toLocaleString("ar-SA")} {order.currency}</span>
      </div>
      <p className={`mt-2 text-sm ${order.status === "Rejected" ? "text-red-700" : "text-[#315F5B]"}`}>{paymentLabels[order.status] ?? "طلب محفوظ"} · {shippingLabels[order.fulfillmentStatus] ?? order.fulfillmentStatus}</p>
      <ul className="mt-3 divide-y divide-black/5 rounded-lg bg-[#f8faf8] px-3">
        {order.items.map(item => <li key={item.productId} className="flex justify-between gap-3 py-2">
          <span>{item.productName}{item.variantName && item.variantName !== item.productName ? ` — ${item.variantName}` : ""} × {item.quantity}</span>
          <span className="shrink-0">{item.lineTotal.toLocaleString("ar-SA")} {order.currency}</span>
        </li>)}
      </ul>
      {order.status === "Rejected" && order.rejectionReason && <p className="mt-2 text-xs text-red-700">{order.rejectionReason}</p>}
      {order.orderStatus.toLowerCase() === "pending" && <Link to={`/store/${encodeURIComponent(storeSlug)}/orders/${order.orderId}/payment`}
        className="mt-3 inline-flex rounded-lg bg-[#234d43] px-4 py-2 font-semibold text-white" style={{color:"#fff"}}>متابعة دفع الطلب</Link>}
      {isDelivered(order) && <div className="mt-3 border-t border-black/10 pt-3">
        <p className="font-semibold">قيّم المنتجات التي استلمتها</p>
        {order.items.map(item => <ProductReviewForm key={item.productId} storeSlug={storeSlug} item={item} />)}
      </div>}
    </article>)}
  </section>;
}
