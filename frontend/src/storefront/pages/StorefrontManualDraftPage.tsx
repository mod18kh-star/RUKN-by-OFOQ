import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { getCustomerCart } from "../data/customerCartApi";
import { StorefrontPageBrand } from "../components/StorefrontPageBrand";
import {
  getManualDraftAccount, getManualDraftQr, submitManualDraftReceipt,
  type ManualDraftAccount,
} from "../data/manualCheckoutApi";
import {
  clearActiveCheckoutOrderId, clearManualCheckoutDraft, readManualCheckoutDraft, type ManualCheckoutDraft,
} from "../data/customerCheckoutDraft";

function matchesDraft(cart: Awaited<ReturnType<typeof getCustomerCart>>, draft: ManualCheckoutDraft): boolean {
  if (!cart || cart.id !== draft.cartId || cart.currency !== draft.expectedCurrency ||
      cart.items.length !== draft.lines.length) return false;
  return cart.items.every(item => draft.lines.some(line =>
    line.productVariantId === item.productVariantId &&
    line.quantity === item.quantity && line.unitPrice === item.unitPrice));
}

export function StorefrontManualDraftPage() {
  const { storeSlug = "" } = useParams<{ storeSlug: string }>();
  const navigate = useNavigate();
  const base = `/store/${encodeURIComponent(storeSlug)}`;
  const [draft] = useState(() => readManualCheckoutDraft(storeSlug));
  const [account, setAccount] = useState<ManualDraftAccount | null>(null);
  const [qrUrl, setQrUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [stale, setStale] = useState(false);
  const [receipt, setReceipt] = useState<File | null>(null);
  const [reference, setReference] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    let qr: string | null = null;
    async function load() {
      if (!draft) { setLoading(false); return; }
      try {
        const cart = await getCustomerCart(storeSlug);
        if (!matchesDraft(cart, draft)) {
          if (active) setStale(true);
          return;
        }
        const selected = await getManualDraftAccount(storeSlug, draft.paymentAccountId);
        if (!active) return;
        setAccount(selected);
        if (selected.hasQr) {
          qr = await getManualDraftQr(storeSlug, selected.accountId);
          if (active) setQrUrl(qr);
        }
      } catch (caught) {
        if (active) setError(caught instanceof Error ? caught.message : "تعذر عرض بيانات التحويل.");
      } finally { if (active) setLoading(false); }
    }
    void load();
    return () => { active = false; if (qr) URL.revokeObjectURL(qr); };
  }, [draft, storeSlug]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!draft || !receipt || !account || busy || stale) return;
    setBusy(true); setError("");
    try {
      const result = await submitManualDraftReceipt(storeSlug, draft, account.accountUpdatedAtUtc, receipt, reference);
      // The backend committed order + receipt + stock transition + cart conversion
      // in one transaction. The next cart is independent and starts empty.
      clearManualCheckoutDraft(storeSlug);
      clearActiveCheckoutOrderId(storeSlug);
      window.dispatchEvent(new CustomEvent("rukn:cart-updated", { detail: { storeSlug } }));
      navigate(`${base}/orders/${result.orderId}/payment`, { replace: true });
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "تعذر إرسال إثبات الدفع؛ لم يتم اعتماد الطلب.");
    } finally { setBusy(false); }
  }

  const money = draft ? `${draft.expectedAmount.toLocaleString("ar-SA", { maximumFractionDigits: 2 })} ${draft.expectedCurrency}` : "";
  return <main dir="rtl" className="min-h-screen bg-[#faf9f7] px-4 py-10 text-[#21352a]">
    <div className="mx-auto max-w-2xl space-y-5">
      <Link to={`${base}/cart`} className="text-sm text-[#527061]">العودة إلى السلة وتعديل المشتريات</Link>
      <header>
        <p className="text-xs text-[#527061]">
          <StorefrontPageBrand storeSlug={storeSlug} /> · مسودة إتمام الشراء
        </p>
        <h1 className="mt-2 text-2xl font-bold">بيانات التحويل وإثبات الدفع</h1>
        <p className="mt-2 text-sm leading-7 text-[#607166]">
          لم يُرسَل أي طلب للتاجر بعد. يمكنك العودة وتعديل السلة؛ لا تصبح مشترياتك طلبًا مستقلًا إلا عند إرسال إثبات الدفع.
        </p>
      </header>
      {error && <p role="alert" className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">{error}</p>}
      {!draft && <div className="rounded-xl border bg-white p-5 text-sm">مسودة الشراء غير موجودة. <Link className="underline" to={`${base}/checkout`}>ارجع إلى إتمام الطلب</Link>.</div>}
      {stale && <div role="alert" className="rounded-xl border border-amber-300 bg-amber-50 p-5 text-sm leading-7 text-amber-950">
        تغيّرت مشترياتك أثناء مراجعة بيانات التحويل. ارجع إلى إتمام الطلب لمراجعة المبلغ الجديد. إذا حوّلت مبلغًا بالفعل، تواصل مع المتجر قبل إجراء تحويل آخر.
        <Link to={`${base}/checkout`} className="mt-3 block underline">تحديث مسودة الشراء</Link>
      </div>}
      {loading && <div className="rounded-xl border bg-white p-5 text-sm">جاري التحقق من السلة وبيانات التحويل…</div>}
      {!loading && !stale && account && draft && <>
        <section className="space-y-3 rounded-2xl border bg-white p-5 sm:p-7">
          <div className="flex items-center justify-between gap-3 border-b pb-4">
            <div><p className="text-xs text-[#64746a]">وسيلة الدفع</p><h2 className="text-lg font-bold">{account.accountName}</h2></div>
            <div><p className="text-xs text-[#64746a]">المبلغ المطلوب</p><strong>{money}</strong></div>
          </div>

          {([
            ["اسم البنك", account.bankName], ["اسم المستفيد", account.accountHolder],
            ["رقم IBAN", account.iban], ["رقم الحساب", account.accountNumber],
            ["شركة المحفظة", account.walletProvider], ["رقم المحفظة", account.walletNumber],
          ] as const).filter(([, value]) => Boolean(value)).map(([label, value]) =>
            <div className="rounded-xl border bg-[#f8faf8] p-3" key={label}>
              <p className="mb-1 text-xs text-[#64746a]">{label}</p>
              <span dir="auto" className="break-all font-medium">{value}</span>
              <button type="button" className="mr-3 text-xs underline" onClick={() => void navigator.clipboard.writeText(value ?? "")}>نسخ</button>
            </div>)}
          {account.transferLink && <a href={account.transferLink} rel="noopener noreferrer" target="_blank" className="block text-sm underline">فتح رابط التحويل</a>}
          {account.hasQr && (
            <div className="rounded-2xl border border-dashed border-[#ccd8d1] bg-[#f7faf8] p-5 text-center">
              <h3 className="text-sm font-semibold">
                صورة QR للتحويل
              </h3>

              {qrUrl ? (
                <div className="mt-4">
                  <img
                    src={qrUrl}
                    alt="رمز QR للتحويل"
                    className="mx-auto max-h-64 max-w-full rounded-xl bg-white object-contain p-2"
                  />

                  <a
                    href={qrUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="mt-4 inline-flex min-h-10 items-center justify-center rounded-xl border border-[#315F5B] bg-white px-5 py-2 text-sm font-semibold text-[#315F5B]"
                  >
                    عرض الصورة بحجم كامل
                  </a>
                </div>
              ) : (
                <p className="mt-3 text-xs leading-6 text-[#64746a]">
                  تعذر تحميل صورة QR المحفوظة لهذا الحساب.
                  يمكنك استخدام بيانات التحويل المكتوبة أعلاه أو إعادة تحميل الصفحة.
                </p>
              )}
            </div>
          )}
        </section>
        <form onSubmit={event => void submit(event)} className="space-y-4 rounded-2xl border bg-white p-5 sm:p-7">
          <h2 className="text-lg font-semibold">إرسال إثبات الدفع للتاجر</h2>
          <label className="block text-sm">صورة الإيصال (PNG أو JPG، حد أقصى 1 MB)
            <input type="file" accept="image/png,image/jpeg" required className="mt-2 block w-full rounded-xl border p-3"
              onChange={event => setReceipt(event.target.files?.[0] ?? null)} /></label>
          <label className="block text-sm">رقم مرجع التحويل (اختياري)
            <input value={reference} onChange={event => setReference(event.target.value)} maxLength={100} className="mt-2 block h-11 w-full rounded-xl border px-4" /></label>
          <button disabled={!receipt || busy} className="w-full min-h-12 rounded-xl bg-[#315F5B] p-3 font-semibold text-white disabled:opacity-50">
            {busy ? "جارٍ إرسال الطلب والإيصال…" : "إرسال الطلب وإثبات الدفع للمراجعة"}
          </button>
          <p className="text-xs leading-6 text-black/55">إرسال الإيصال لا يثبت وصول المال؛ يراجع التاجر التحويل قبل اعتماد الدفع.</p>
        </form>
      </>}
    </div>
  </main>;
}
