import { useEffect, useRef, useState, type FormEvent } from "react";
import { Link, useLocation, useParams } from "react-router";
import { getAccessToken, getCurrentUser } from "../../features/auth/authSession";
import { CheckCircle2, Copy, Download, ExternalLink, FileUp, Landmark, RefreshCw, Wallet } from "lucide-react";
import {
  getManualPayment, getManualQr, listManualMethods, selectManualMethod, uploadManualReceipt,
  ManualHttpError, type ManualCustomerPayment, type ManualMethodSummary,
} from "../data/manualCheckoutApi";

type PaymentLocationState = {
  accountId?: string;
  customerPhone?: string;
  selectionError?: string;
};

const money = (amount: number, currency: string) =>
  `${amount.toLocaleString("ar-SA", { maximumFractionDigits: 2 })} ${currency}`;

export function StorefrontManualPaymentPage() {
  const { storeSlug = "", orderId = "" } = useParams<{ storeSlug: string; orderId: string }>();
  const location = useLocation();
  const incoming = location.state as PaymentLocationState | null;
  const selectedAccountId = incoming?.accountId ?? "";
  const selectedPhone = incoming?.customerPhone ?? "";
  const selectionError = incoming?.selectionError ?? "";
  const [payment, setPayment] = useState<ManualCustomerPayment | null>(null);
  const [methods, setMethods] = useState<ManualMethodSummary[]>([]);
  const [methodId, setMethodId] = useState(selectedAccountId);
  const [phone, setPhone] = useState(selectedPhone);
  const phoneEditedByCustomer = useRef(false);
  const [qrUrl, setQrUrl] = useState<string | null>(null);
  const [qrError, setQrError] = useState("");
  const [qrDownloadError, setQrDownloadError] = useState("");
  const [receipt, setReceipt] = useState<File | null>(null);
  const [reference, setReference] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [copyNotice, setCopyNotice] = useState("");
  const base = `/store/${encodeURIComponent(storeSlug)}`;

  // If this page is opened directly, prefill the contact number from this customer's account.
  // Do not overwrite a phone passed from checkout or typed on this page.
  useEffect(() => {
    if (!getAccessToken() || selectedPhone.trim()) return;
    let active = true;
    void getCurrentUser().then(user => {
      const registeredPhone = user.phoneNumber?.trim();
      if (active && registeredPhone && !phoneEditedByCustomer.current) {
        setPhone(previous => previous.trim() ? previous : registeredPhone);
      }
    }).catch(() => {
      // The customer can provide a phone manually when none is registered.
    });
    return () => { active = false; };
  }, [selectedPhone]);

  // Checkout already selected a payment account. The GET response contains the
  // FULL transfer details; a duplicate account choice must not be required.
  // If checkout couldn't save the selection, retry on the EXISTING order only.
  useEffect(() => {
    let active = true;
    void Promise.resolve().then(async () => {
      if (!active) return;
      setLoading(true);
      setError("");
      let current: ManualCustomerPayment | null = null;
      try {
        current = await getManualPayment(storeSlug, orderId);
      } catch (caught) {
        if (!(caught instanceof ManualHttpError && caught.status === 404)) throw caught;
      }
      let retryError = "";
      if (!current && selectedAccountId && selectedPhone) {
        try {
          current = await selectManualMethod(storeSlug, orderId, selectedAccountId, selectedPhone);
        } catch (caught) {
          retryError = caught instanceof Error ? caught.message : "تعذر تثبيت حساب التحويل.";
          // Another tab may have completed the same selection. Read instead of
          // creating another order or silently dropping the server failure.
          try { current = await getManualPayment(storeSlug, orderId); } catch { /* still needs selection */ }
        }
      }
      if (!active) return;
      if (current) {
        setPayment(current);
        return;
      }
      const available = await listManualMethods(storeSlug);
      if (!active) return;
      setMethods(available);
      setMethodId(old => old || available[0]?.id || "");
      if (retryError || selectionError) {
        setError(`لم نتمكن من عرض بيانات التحويل تلقائيًا: ${retryError || selectionError}. طلبك موجود ولم يتم الدفع؛ يمكنك إعادة المحاولة على الطلب نفسه.`);
      }
    }).catch(caught => {
      if (active) setError(caught instanceof Error ? caught.message : "تعذر تحميل بيانات الدفع. حاول تحديث الصفحة.");
    }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [storeSlug, orderId, selectedAccountId, selectedPhone, selectionError]);

  useEffect(() => {
    let active = true;
    let objectUrl: string | null = null;
    void Promise.resolve().then(() => {
      if (!active) return;
      setQrUrl(null);
      setQrError("");
      if (payment?.hasQr) void getManualQr(storeSlug, orderId).then(url => {
        if (!active) { if (url) URL.revokeObjectURL(url); return; }
        if (url) { objectUrl = url; setQrUrl(url); }
        else setQrError("لم نتمكن من جلب صورة QR من المتجر. استخدم بيانات الحساب المكتوبة أو تواصل مع التاجر.");
      }).catch(() => {
        if (active) setQrError("تعذر تحميل صورة QR. استخدم بيانات الحساب المكتوبة أو تواصل مع التاجر.");
      });
    });
    return () => { active = false; if (objectUrl) URL.revokeObjectURL(objectUrl); };
  }, [storeSlug, orderId, payment?.hasQr, payment?.accountId]);

  async function downloadQr() {
    if (!qrUrl) return;
    setQrDownloadError("");
    try {
      // qrUrl is a short-lived blob URL returned by an authenticated QR endpoint.
      // Use the validated bytes, not a remote link that might open instead of downloading.
      const response = await fetch(qrUrl);
      if (!response.ok) throw new Error("QR image unavailable");
      const image = await response.blob();
      const mime = image.type.split(";")[0].toLowerCase();
      if (!image.size || !["image/png", "image/jpeg"].includes(mime)) {
        throw new Error("Unsupported QR image format");
      }
      const extension = mime === "image/png" ? "png" : "jpg";
      const downloadUrl = URL.createObjectURL(image);
      const link = document.createElement("a");
      link.href = downloadUrl;
      link.download = `rukn-transfer-qr-${orderId.slice(0, 8)}.${extension}`;
      document.body.appendChild(link);
      try {
        link.click();
      } finally {
        link.remove();
        // Keep the URL valid long enough for the browser to start the download.
        window.setTimeout(() => URL.revokeObjectURL(downloadUrl), 30000);
      }
    } catch {
      setQrDownloadError("تعذر تحميل صورة QR. افتح الصورة بالحجم الكامل أو تواصل مع المتجر.");
    }
  }
  async function choose(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!methodId || busy) return;
    setBusy(true); setError("");
    try {
      setPayment(await selectManualMethod(storeSlug, orderId, methodId, phone.trim()));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "تعذر اختيار وسيلة الدفع. لن يتم إنشاء طلب جديد.");
    } finally { setBusy(false); }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!receipt || !payment || busy) return;
    setBusy(true); setError(""); setNotice("");
    try {
      await uploadManualReceipt(storeSlug, orderId, receipt, reference);
      setPayment(await getManualPayment(storeSlug, orderId));
      setReceipt(null); setReference("");
      setNotice("تم إرسال إيصال التحويل للتاجر. طلبك بانتظار مراجعة الدفع ولم يُسجّل مدفوعًا بعد.");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "تعذر رفع الإيصال. حاول مجددًا على نفس الطلب.");
    } finally { setBusy(false); }
  }

  async function copy(value: string, label: string) {
    try {
      await navigator.clipboard.writeText(value);
      setCopyNotice(`تم نسخ ${label}.`);
    } catch { setCopyNotice("تعذّر النسخ التلقائي. حدد الرقم وانسخه يدويًا."); }
  }

  function detail(label: string, value: string | null | undefined, canCopy = false) {
    if (!value?.trim()) return null;
    return <div className="rounded-xl border border-[#e6ece7] bg-[#f8faf8] px-4 py-3" key={label}>
      <p className="mb-2 text-xs font-medium text-[#64746a]">{label}</p>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <span dir={canCopy ? "ltr" : "auto"} className="min-w-0 break-all text-base font-semibold text-[#193c30]" style={{ unicodeBidi: "plaintext" }}>{value}</span>
        {canCopy && <button type="button" onClick={() => void copy(value, label)}
          className="flex min-h-10 shrink-0 items-center gap-1 rounded-lg border bg-white px-3 text-sm text-[#315F5B]">
          <Copy size={15} /> نسخ
        </button>}
      </div>
    </div>;
  }

  return <main dir="rtl" className="min-h-screen bg-[#faf9f7] px-4 py-10 text-[#21352a]">
    <div className="mx-auto max-w-2xl space-y-5">
      <Link to={base} className="text-sm text-[#527061]">العودة للمتجر</Link>
      <header>
        <p className="text-xs text-[#527061]">ركن · الطلب #{orderId.slice(0, 8)}</p>
        <h1 className="mt-2 text-2xl font-bold">بيانات التحويل وإثبات الدفع</h1>
        <p className="mt-2 text-sm leading-7 text-[#607166]">حوّل إلى حساب التاجر مباشرة، ثم أرفق صورة الإيصال. الدفع لا يُعتمد قبل التحقق من وصول المال.</p>
      </header>
      {error && <p role="alert" className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm leading-7 text-red-800">{error}</p>}
      {notice && <p role="status" className="rounded-xl bg-green-50 p-4 text-sm text-green-800">{notice}</p>}
      {loading ? <div role="status" className="rounded-2xl border bg-white p-6 text-sm">جاري عرض بيانات الحساب المحدد…</div> : !payment ?
        <form onSubmit={event => void choose(event)} className="space-y-4 rounded-2xl border bg-white p-6">
          <h2 className="text-lg font-semibold">إكمال اختيار حساب الدفع لهذا الطلب</h2>
          <p className="text-xs leading-6 text-[#64746a]">الطلب موجود بالفعل وغير مدفوع. أكمل اختيار الحساب هنا دون إنشاء طلب جديد.</p>
          {methods.map(method => <label key={method.id} className={`flex cursor-pointer items-center gap-3 rounded-xl border p-4 text-sm ${methodId === method.id ? "border-[#315F5B] bg-[#f4faf7]" : ""}`}>
            <input type="radio" name="manualPaymentAccount" checked={methodId === method.id} onChange={() => setMethodId(method.id)} />
            {method.kind === "bank" ? <Landmark size={19} /> : <Wallet size={19} />}
            <span className="min-w-0">{method.name}<small className="block text-black/50">{method.maskedReference}</small></span>
          </label>)}
          {methods.length === 0 && <p className="text-sm">لا توجد وسائل تحويل مفعّلة. تواصل مع المتجر ولا تنشئ طلبًا آخر.</p>}
          <label className="block text-sm">رقم الهاتف للتواصل بشأن الطلب
            <input required dir="ltr" type="tel" value={phone} onChange={event => { phoneEditedByCustomer.current = true; setPhone(event.target.value); }} maxLength={40}
              className="mt-2 block h-12 w-full rounded-xl border px-4" placeholder="رقم هاتفك" />
          </label>
          <button disabled={!methodId || busy || !methods.length} className="flex w-full min-h-12 items-center justify-center gap-2 rounded-xl bg-[#315F5B] p-3 font-semibold text-white disabled:opacity-50">
            <RefreshCw size={16} /> {busy ? "جاري تحميل الحساب…" : "عرض بيانات التحويل الآن"}
          </button>
        </form> : <>
          <section className="space-y-4 rounded-2xl border bg-white p-5 sm:p-7">
            <div className="flex flex-wrap items-center justify-between gap-2 border-b pb-4">
              <div><p className="text-xs text-[#64746a]">وسيلة الدفع المختارة</p><h2 className="mt-1 flex items-center gap-2 text-lg font-bold">
                {payment.accountKind === "bank" ? <Landmark size={20} /> : <Wallet size={20} />}{payment.accountName}</h2></div>
              <div><p className="text-xs text-[#64746a]">المبلغ المطلوب تحويله</p><strong className="mt-1 block text-lg">{money(payment.amount, payment.currency)}</strong></div>
            </div>
            {payment.status === "Approved" ? <p className="flex items-center gap-2 rounded-xl bg-green-50 p-3 text-sm text-green-800"><CheckCircle2 size={18} /> تم تأكيد وصول الدفع. الطلب قيد التجهيز.</p> :
              payment.status === "PendingReview" ? <p className="rounded-xl bg-amber-50 p-3 text-sm text-amber-900">تم استلام الإيصال، والدفع بانتظار مراجعة التاجر.</p> :
              payment.status === "Rejected" ? <p className="rounded-xl bg-red-50 p-3 text-sm text-red-800">رُفض إثبات الدفع: {payment.rejectionReason || "يرجى التواصل مع المتجر."} يمكنك رفع إيصال جديد للطلب نفسه.</p> :
              <p className="rounded-xl bg-amber-50 p-3 text-sm">حوّل المبلغ إلى الحساب الموضح أدناه، ثم أرفق الإيصال لإرساله إلى التاجر.</p>}
            {payment.status !== "Approved" && <>
              <h3 className="text-base font-bold">تفاصيل الحساب الذي ستحوّل إليه</h3>
              {payment.accountKind === "bank" ? <>
                {detail("اسم البنك", payment.bankName)}
                {detail("اسم صاحب الحساب / المستفيد", payment.accountHolder)}
                {detail("رقم IBAN", payment.iban, true)}
                {detail("رقم الحساب البنكي", payment.accountNumber, true)}
              </> : <>
                {detail("اسم المحفظة أو شركة الحوالة", payment.walletProvider)}
                {detail("اسم المستفيد", payment.accountHolder)}
                {detail("رقم المحفظة / الحوالة", payment.walletNumber, true)}
              </>}
              {payment.transferLink && <a href={payment.transferLink} rel="noopener noreferrer" target="_blank"
                className="block rounded-xl border border-[#315F5B] px-4 py-3 text-center text-sm font-semibold text-[#315F5B]">فتح رابط التحويل المعتمد</a>}
              {copyNotice && <p role="status" className="text-xs text-[#315F5B]">{copyNotice}</p>}
              <div className="rounded-2xl border border-dashed border-[#ccd8d1] bg-[#f7faf8] p-5 text-center">
                <h3 className="text-sm font-semibold">صورة QR للتحويل</h3>
                {qrUrl ? (
                  <div className="mt-3 space-y-3">
                    <img
                      src={qrUrl}
                      alt="رمز QR للحساب الذي اختاره العميل"
                      className="mx-auto max-h-64 max-w-full rounded-lg bg-white object-contain p-2"
                    />

                    <div className="flex flex-wrap justify-center gap-3">

                      <a
                        href={qrUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-[#315F5B] bg-white px-5 py-2 text-sm font-semibold text-[#315F5B]"
                      >
                        <ExternalLink size={16} /> عرض الصورة بحجم كامل
                      </a>

                      <button
                        type="button"
                        onClick={() => void downloadQr()}
                        className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-[#315F5B] px-5 py-2 text-sm font-semibold text-white"
                      >
                        <Download size={16} /> تحميل صورة QR
                      </button>

                    </div>

                    {qrDownloadError && (
                      <p role="alert" className="text-xs text-red-700">
                        {qrDownloadError}
                      </p>
                    )}

                  </div>
                ) :
                  <p className="mt-3 text-xs leading-6 text-[#64746a]">{qrError || (payment.hasQr ? "جاري تحميل صورة QR…" : "لم يضف التاجر صورة QR لهذا الحساب. استخدم رقم الحساب أو المحفظة المكتوب أعلاه.")}</p>}
              </div>
            </>}
          </section>
          {(payment.status === "AwaitingReceipt" || payment.status === "Rejected") &&
            <form onSubmit={event => void submit(event)} className="space-y-4 rounded-2xl border bg-white p-5 sm:p-7">
              <h2 className="flex items-center gap-2 text-lg font-semibold"><FileUp size={20} /> إرفاق صورة إيصال التحويل</h2>
              <p className="text-sm leading-7 text-[#64746a]">بعد التحويل، أرفق صورة الإيصال لتصل إلى لوحة التاجر للمراجعة.</p>
              <label className="block text-sm">صورة الإيصال (PNG أو JPG، حد أقصى 1 MB)
                <input type="file" accept="image/png,image/jpeg" required className="mt-2 block w-full rounded-xl border p-3"
                  onChange={event => setReceipt(event.target.files?.[0] || null)} /></label>
              <label className="block text-sm">رقم مرجع التحويل (اختياري)
                <input value={reference} onChange={event => setReference(event.target.value)} maxLength={100} className="mt-2 block h-11 w-full rounded-xl border px-3" /></label>
              <button disabled={!receipt || busy} className="w-full min-h-12 rounded-xl bg-[#315F5B] p-3 font-semibold text-white disabled:opacity-50">
                {busy ? "جاري إرسال الإيصال…" : "إرسال الإيصال للمراجعة"}</button>
              <p className="text-xs leading-6 text-black/55">رفع الإيصال لا يثبت وصول المال؛ يتأكد التاجر من حسابه أولًا ثم يعتمد الدفع.</p>
            </form>}
        </>}
    </div>
  </main>;
}
