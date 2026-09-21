import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";
import { getMyManualOrders, type CustomerManualOrder } from "../data/manualCheckoutApi";

const labels: Record<CustomerManualOrder["status"], string> = {
  AwaitingReceipt: "بانتظار رفع إيصال", PendingReview: "بانتظار مراجعة التاجر",
  Approved: "تم تأكيد الدفع · قيد التجهيز", Rejected: "إثبات الدفع مرفوض · يحتاج تصحيحًا",
};
export function CustomerManualOrdersSection() {
  const { storeSlug = "" } = useParams<{ storeSlug: string }>();
  const [orders, setOrders] = useState<CustomerManualOrder[]>([]);
  const [error, setError] = useState("");
  useEffect(() => {
    let active = true;
    void getMyManualOrders(storeSlug).then(items => { if (active) setOrders(items); })
      .catch(() => { if (active) setError("تعذر تحميل طلبات الدفع اليدوي."); });
    return () => { active = false; };
  }, [storeSlug]);
  return <section className="space-y-3 rounded-2xl border border-black/10 bg-white p-5" dir="rtl">
    <h3 className="text-base font-semibold">طلبات الدفع والتحويل</h3>
    <p className="text-xs leading-6 text-black/55">تابع إثباتات التحويل، وعدّل الإيصال إذا طلب المتجر ذلك.</p>
    {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
    {orders.length === 0 && !error && <p className="text-sm text-black/45">لا توجد طلبات تحويل يدوي لهذا المتجر حاليًا.</p>}
    {orders.map(order => <Link key={order.orderId} to={`/store/${encodeURIComponent(storeSlug)}/orders/${order.orderId}/payment`}
      className="block space-y-2 rounded-xl border px-4 py-3 text-sm transition hover:border-[#315F5B]">
      <div className="flex flex-wrap items-center justify-between gap-3"><strong>طلب #{order.orderId.slice(0, 8)}</strong><span>{order.amount.toLocaleString("ar-SA")} {order.currency}</span></div>
      <p className={order.status === "Rejected" ? "text-red-700" : "text-[#315F5B]"}>{labels[order.status]}</p>
      {order.status === "Rejected" && order.rejectionReason && <p className="text-xs text-red-700">{order.rejectionReason}</p>}
    </Link>)}
  </section>;
}
