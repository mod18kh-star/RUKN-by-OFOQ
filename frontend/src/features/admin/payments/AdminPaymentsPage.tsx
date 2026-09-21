import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Building2, Check, ChevronLeft, CreditCard, ImagePlus, Landmark, Plus, RefreshCw, ShieldCheck, Wallet, X } from "lucide-react";
import { readAdminStore } from "../store-setup/storeSetupStorage";
import { createSandboxProvider, getProviderAccounts, setSandboxProviderState, type ProviderAccount } from "./paymentAdminApi";
import { getManualAccount, listManualAccounts, loadManualQr, saveManualAccount, setManualAccountState, type ManualAccountInput, type ManualPaymentAccount } from "./manualPaymentApi";

const input = "w-full min-h-11 rounded-xl border border-[#dae0dd] bg-white px-4 py-2.5 text-sm text-[#152a27] outline-none transition focus:border-[#315F5B] focus:ring-2 focus:ring-[#315F5B]/10";
const empty: ManualAccountInput = { kind: "bank", displayName: "", bankName: "", accountHolder: "", iban: "", accountNumber: "", walletProvider: "", walletNumber: "", transferLink: "", qrBase64: null, removeQr: false };
type Choice = "bank" | "wallet" | "gateway" | null;

export function AdminPaymentsPage() {
  const tenantId = readAdminStore()?.tenantId ?? null;
  const [choice, setChoice] = useState<Choice>(null);
  const [editing, setEditing] = useState<string | null>(null);
  const [form, setForm] = useState<ManualAccountInput>({ ...empty });
  const [manual, setManual] = useState<ManualPaymentAccount[]>([]);
  const [gateways, setGateways] = useState<ProviderAccount[]>([]);
  const [loading, setLoading] = useState(Boolean(tenantId));
  const [busy, setBusy] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [providerCode, setProviderCode] = useState("");
  const [providerName, setProviderName] = useState("");
  const [qrPreview, setQrPreview] = useState("");
  const [savedQr, setSavedQr] = useState(false);

  const reload = useCallback(async () => {
    if (!tenantId) { setLoading(false); return; }
    const [accounts, providers] = await Promise.all([listManualAccounts(tenantId), getProviderAccounts(tenantId)]);
    setManual(accounts); setGateways(providers); setLoading(false);
  }, [tenantId]);

  useEffect(() => {
    let alive = true;
    if (!tenantId) return;
    void Promise.all([listManualAccounts(tenantId), getProviderAccounts(tenantId)])
      .then(([accounts, providers]) => { if (alive) { setManual(accounts); setGateways(providers); } })
      .catch(() => { if (alive) setError("تعذر تحميل حسابات الدفع. تحقق من تشغيل الخادم وتطبيق Migration الخاصة بهذه المرحلة."); })
      .finally(() => { if (alive) setLoading(false); });
    return () => { alive = false; };
  }, [tenantId]);

  function begin(kind: Choice) {
    setChoice(kind); setEditing(null); setForm({ ...empty, kind: kind === "wallet" ? "wallet" : "bank" });
    setQrPreview(""); setSavedQr(false); setError(""); setNotice("");
  }

  async function edit(account: ManualPaymentAccount) {
    if (!tenantId || busy) return;
    setBusy("edit"); setError(""); setNotice("");
    try {
      const details = await getManualAccount(tenantId, account.id);
      setForm({ kind: details.kind, displayName: details.displayName, bankName: details.bankName ?? "",
        accountHolder: details.accountHolder ?? "", iban: details.iban ?? "", accountNumber: details.accountNumber ?? "",
        walletProvider: details.walletProvider ?? "", walletNumber: details.walletNumber ?? "",
        transferLink: details.transferLink ?? "", qrBase64: null, removeQr: false });
      setEditing(account.id); setChoice(account.kind); setSavedQr(account.hasQr); setQrPreview("");
    } catch (e) { setError(e instanceof Error ? e.message : "تعذر فتح الحساب."); }
    finally { setBusy(""); }
  }

  async function upload(file?: File) {
    if (!file) return;
    if (!["image/png", "image/jpeg"].includes(file.type) || file.size > 40 * 1024) {
      setError("صورة QR يجب أن تكون PNG أو JPEG وبحجم لا يتجاوز 40 كيلوبايت."); return;
    }
    try {
      const image = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onerror = () => reject(new Error("تعذر قراءة الصورة."));
        reader.onload = () => resolve(String(reader.result ?? ""));
        reader.readAsDataURL(file);
      });
      const base64 = image.slice(image.indexOf(",") + 1);
      setForm(old => ({ ...old, qrBase64: base64, removeQr: false }));
      setQrPreview(image); setSavedQr(true); setError("");
    } catch { setError("تعذر قراءة صورة QR المختارة."); }
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!tenantId || busy) return;
    setBusy("save"); setError(""); setNotice("");
    try {
      await saveManualAccount(tenantId, form, editing ?? undefined);
      await reload(); begin(null);
      setNotice("حُفظ الحساب بحالة متوقفة. يمكنك تفعيله من بطاقته بعد مراجعة البيانات.");
    } catch (e) { setError(e instanceof Error ? e.message : "تعذر حفظ الحساب."); }
    finally { setBusy(""); }
  }

  async function toggle(account: ManualPaymentAccount) {
    if (!tenantId || busy) return;
    setBusy(account.id); setError(""); setNotice("");
    try {
      await setManualAccountState(tenantId, account.id, !account.isEnabled);
      await reload(); setNotice("تم تحديث حالة وسيلة الدفع داخل إعدادات المتجر. الربط بصفحة العميل يحتاج مرحلة مستقلة.");
    } catch (e) { setError(e instanceof Error ? e.message : "تعذر تحديث حالة الحساب."); }
    finally { setBusy(""); }
  }

  async function showQr(account: ManualPaymentAccount) {
    if (!tenantId || busy) return;
    setBusy("qr"); setError("");
    try { const image = await loadManualQr(tenantId, account.id); setQrPreview(image); setSavedQr(false); setEditing(null); setChoice(null); }
    catch (e) { setError(e instanceof Error ? e.message : "تعذر عرض الصورة."); }
    finally { setBusy(""); }
  }

  async function addGateway(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!tenantId || busy) return;
    setBusy("gateway"); setError(""); setNotice("");
    try {
      if (!/^[a-z][a-z0-9_-]{1,39}$/.test(providerCode.trim().toLowerCase()) || !providerName.trim())
        throw new Error("أدخل رمز مزود صحيحًا واسمًا واضحًا.");
      await createSandboxProvider(tenantId, providerCode.trim().toLowerCase(), providerName.trim());
      await reload(); setProviderCode(""); setProviderName(""); setNotice("أُنشئ حساب البوابة التجريبي بحالة متوقفة. لم يتم تفعيل تحصيل أي أموال.");
    } catch (e) { setError(e instanceof Error ? e.message : "تعذر إنشاء حساب البوابة."); }
    finally { setBusy(""); }
  }

  async function toggleGateway(account: ProviderAccount) {
    if (!tenantId || busy || account.environment !== "Sandbox") return;
    setBusy(account.accountId); setError(""); setNotice("");
    try { await setSandboxProviderState(tenantId, account, !account.isEnabled); await reload(); setNotice("حُفظت حالة بوابة الاختبار."); }
    catch (e) { setError(e instanceof Error ? e.message : "تعذر تحديث حساب الاختبار."); }
    finally { setBusy(""); }
  }

  return <main dir="rtl" className="mx-auto max-w-[1160px] space-y-6 pb-16 text-[#172a27]">
    <header className="flex flex-wrap items-center justify-between gap-4">
      <div><p className="text-xs font-semibold tracking-[.1em] text-[#315F5B]">RUKN · MERCHANT PAYMENTS</p>
        <h1 className="mt-2 text-3xl font-semibold">وسائل دفع المتجر</h1>
        <p className="mt-2 text-sm leading-7 text-black/60">أضف الحسابات التي تستقبل مدفوعات عملائك مباشرة، وتحكم بحالة كل وسيلة.</p></div>
      <button type="button" disabled={loading || !!busy} onClick={() => void reload().catch(() => setError("تعذر تحديث القائمة."))}
        className="flex min-h-11 items-center gap-2 rounded-xl border border-black/10 bg-white px-4 text-sm disabled:opacity-50"><RefreshCw size={16}/> تحديث</button>
    </header>
    <div className="rounded-2xl border border-[#d7e9df] bg-[#f2faf5] p-4 text-sm leading-7 text-[#315F5B]">
      <ShieldCheck size={19} className="mb-2"/>الحسابات هنا تخص متجرك فقط. التفعيل الحالي يحفظ إعدادات التاجر؛ ظهور الوسيلة في صفحة دفع العميل ورفع إيصالات التحويل يحتاجان إكمال ربط آمن واختباره قبل التشغيل التجاري.
    </div>
    {error && <div role="alert" className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">{error}</div>}
    {notice && <div role="status" className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-900">{notice}</div>}

    <section className="rounded-2xl border border-black/10 bg-white p-5 md:p-7">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div><h2 className="text-xl font-semibold">حسابات الدفع</h2><p className="mt-1 text-sm text-black/50">أظهر لعملائك لاحقًا طرق التحويل المناسبة لمتجرك.</p></div>
        <button type="button" onClick={() => begin(choice ? null : "bank")} className="flex min-h-11 items-center gap-2 rounded-xl bg-[#315F5B] px-5 text-sm font-semibold text-white">
          {choice ? <X size={17}/> : <Plus size={17}/>} {choice ? "إغلاق الإضافة" : "إضافة وسيلة دفع"}
        </button>
      </div>
      {loading ? <p className="mt-5 text-sm text-black/50">جارٍ تحميل الحسابات…</p> : manual.length === 0 ?
        <p className="mt-5 rounded-xl bg-[#f5f7f5] p-5 text-sm text-black/60">لم تُضف حسابات تحويل يدوي بعد.</p> :
        <div className="mt-5 grid gap-3 lg:grid-cols-2">{manual.map(account =>
          <article key={account.id} className="rounded-xl border border-[#e5eae6] p-4">
            <div className="flex items-start justify-between gap-3"><div className="flex items-center gap-3"><div className="rounded-lg bg-[#edf6f0] p-3 text-[#315F5B]">{account.kind === "bank" ? <Landmark size={21}/> : <Wallet size={21}/>}</div>
              <div><h3 className="font-semibold">{account.displayName}</h3><p className="mt-1 text-xs text-black/50">{account.kind === "bank" ? "حساب بنكي" : "محفظة / تحويل يدوي"} · <span dir="ltr">{account.maskedReference}</span></p></div></div>
              <span className={`rounded-full px-2.5 py-1 text-xs ${account.isEnabled ? "bg-emerald-100 text-emerald-800" : "bg-slate-100 text-slate-600"}`}>{account.isEnabled ? "مفعّل بالإعدادات" : "متوقف"}</span></div>
            <div className="mt-4 flex flex-wrap gap-2"><button type="button" disabled={!!busy} onClick={() => void edit(account)} className="min-h-10 rounded-lg border border-black/10 px-4 text-sm disabled:opacity-50">تعديل البيانات</button>
              <button type="button" disabled={!!busy} onClick={() => void toggle(account)} className="min-h-10 rounded-lg bg-[#315F5B] px-4 text-sm text-white disabled:opacity-50">{busy === account.id ? "جارٍ الحفظ…" : account.isEnabled ? "إيقاف" : "تفعيل"}</button>
              {account.hasQr && <button type="button" disabled={!!busy} onClick={() => void showQr(account)} className="min-h-10 rounded-lg border border-black/10 px-4 text-sm">صورة QR</button>}
            </div>
          </article>)}</div>}
    </section>

    {choice && <section className="rounded-2xl border border-black/10 bg-white p-5 md:p-7">
      <div className="flex flex-wrap items-center justify-between gap-2"><div><h2 className="text-xl font-semibold">{editing ? "تعديل وسيلة الدفع" : "إضافة وسيلة دفع"}</h2>
        <p className="mt-1 text-sm text-black/50">اختر نوع الحساب ثم أكمل بياناته.</p></div><button type="button" onClick={() => begin(null)} className="rounded-lg border border-black/10 p-2" aria-label="إغلاق"><X size={18}/></button></div>
      {!editing && <div className="mt-5 grid gap-3 md:grid-cols-3">{([
        { id: "bank", title: "حساب بنكي", desc: "IBAN واسم البنك والمستفيد", icon: <Landmark size={22}/> },
        { id: "wallet", title: "محفظة / حوالة", desc: "رقم المحفظة وبيانات المستفيد", icon: <Wallet size={22}/> },
        { id: "gateway", title: "بوابة دفع", desc: "ربط تجريبي؛ الإنتاج لاحقًا", icon: <CreditCard size={22}/> },
      ] as const).map(option => <button key={option.id} type="button" onClick={() => begin(option.id)}
        className={`min-h-[108px] rounded-xl border p-4 text-right transition ${choice === option.id ? "border-[#315F5B] bg-[#f1f8f4]" : "border-black/10 hover:bg-[#f8faf8]"}`}>
        <span className="text-[#315F5B]">{option.icon}</span><span className="mt-2 block font-semibold">{option.title}</span><span className="mt-1 block text-xs text-black/50">{option.desc}</span></button>)}</div>}

      {choice !== "gateway" ? <form onSubmit={event => void submit(event)} className="mt-6 space-y-5">
        <div className="grid gap-4 md:grid-cols-2">
          <label className="space-y-2 text-sm"><span>اسم الوسيلة الذي يظهر لك في لوحة الإدارة *</span><input className={input} maxLength={120} value={form.displayName} required placeholder="مثال: الحساب البنكي الرئيسي" onChange={e => setForm(s => ({ ...s, displayName: e.target.value }))}/></label>
          {choice === "bank" ? <>
            <label className="space-y-2 text-sm"><span>اسم البنك *</span><input className={input} maxLength={120} required value={form.bankName} onChange={e => setForm(s => ({ ...s, bankName: e.target.value }))}/></label>
            <label className="space-y-2 text-sm"><span>اسم صاحب الحساب *</span><input className={input} maxLength={120} required value={form.accountHolder} onChange={e => setForm(s => ({ ...s, accountHolder: e.target.value }))}/></label>
            <label className="space-y-2 text-sm"><span>رقم الآيبان IBAN (إذا كان مدعومًا)</span><input className={input} dir="ltr" autoComplete="off" maxLength={40} value={form.iban} onChange={e => setForm(s => ({ ...s, iban: e.target.value }))}/></label>
            <label className="space-y-2 text-sm"><span>رقم الحساب / مرجع التحويل (إن وجد)</span><input className={input} dir="ltr" autoComplete="off" maxLength={80} value={form.accountNumber} onChange={e => setForm(s => ({ ...s, accountNumber: e.target.value }))}/></label>
          </> : <>
            <label className="space-y-2 text-sm"><span>اسم المحفظة أو شركة الحوالات *</span><input className={input} maxLength={120} required value={form.walletProvider} placeholder="مثال: شام كاش" onChange={e => setForm(s => ({ ...s, walletProvider: e.target.value }))}/></label>
            <label className="space-y-2 text-sm"><span>اسم المستفيد *</span><input className={input} maxLength={120} required value={form.accountHolder} onChange={e => setForm(s => ({ ...s, accountHolder: e.target.value }))}/></label>
            <label className="space-y-2 text-sm"><span>رقم المحفظة / التحويل *</span><input className={input} dir="ltr" maxLength={80} required value={form.walletNumber} onChange={e => setForm(s => ({ ...s, walletNumber: e.target.value }))}/></label>
          </>}
          <label className="space-y-2 text-sm md:col-span-2"><span>رابط تحويل مباشر (اختياري)</span><input className={input} type="url" dir="ltr" maxLength={500} value={form.transferLink} placeholder="https://" onChange={e => setForm(s => ({ ...s, transferLink: e.target.value }))}/>
            <span className="block text-xs leading-6 text-black/50">ليس هناك رابط تحويل موحّد لكل البنوك. تُقبل فقط نطاقات HTTPS المعتمدة من إدارة ركن؛ لا يُعرض الرابط للعميل قبل اعتماد التكامل.</span></label>
        </div>
        <div className="rounded-xl border border-dashed border-[#b9cac2] bg-[#f8faf8] p-4">
          <div className="flex flex-wrap items-center gap-3"><ImagePlus size={22} className="text-[#315F5B]"/><div className="flex-1"><h3 className="text-sm font-semibold">صورة QR أو باركود التحويل</h3><p className="mt-1 text-xs text-black/50">اختيارية · PNG أو JPEG · حتى 40 كيلوبايت · معاينة خاصة بالتاجر</p></div></div>
          <input aria-label="رفع صورة QR" className="mt-3 block w-full text-sm" type="file" accept="image/png,image/jpeg" onChange={e => void upload(e.target.files?.[0])}/>
          {(qrPreview || savedQr) && <div className="mt-3 flex flex-wrap items-center gap-3">{qrPreview && <img src={qrPreview} alt="معاينة QR" className="h-24 w-24 rounded-lg border object-contain"/>}
            <span className="text-xs text-black/60">{qrPreview ? "الصورة المحددة" : "توجد صورة QR محفوظة"}</span>
            <button type="button" className="rounded-lg border border-black/10 px-3 py-2 text-xs" onClick={() => { setQrPreview(""); setSavedQr(false); setForm(s => ({ ...s, qrBase64: null, removeQr: true })); }}>إزالة الصورة</button></div>}
        </div>
        <div className="flex flex-wrap items-center gap-3"><button type="submit" disabled={!tenantId || !!busy} className="flex min-h-11 items-center gap-2 rounded-xl bg-[#315F5B] px-6 text-sm font-semibold text-white disabled:opacity-50"><Check size={16}/>{busy === "save" ? "جارٍ الحفظ…" : editing ? "حفظ التعديلات" : "حفظ حساب جديد"}</button>
          <span className="text-xs text-black/50">بعد أي تعديل جوهري يعود الحساب لحالة متوقف لحين تفعيله يدويًا.</span></div>
      </form> : <div className="mt-6 space-y-4">
        <p className="rounded-xl bg-amber-50 p-4 text-sm leading-7 text-amber-950">ربط بوابات الإنتاج وروابط الدفع الحقيقية يحتاج اتفاقًا مع المزود وتكامل API آمنًا. إنشاء الحساب هنا للتجربة فقط، ولا يجعله وسيلة دفع حقيقية للعميل.</p>
        <form onSubmit={event => void addGateway(event)} className="grid gap-3 md:grid-cols-2"><label className="space-y-2 text-sm">رمز مزود الاختبار<input className={input} dir="ltr" required maxLength={40} autoComplete="off" value={providerCode} onChange={e => setProviderCode(e.target.value)} placeholder="provider_code"/></label>
          <label className="space-y-2 text-sm">اسم حساب الاختبار<input className={input} required maxLength={200} value={providerName} onChange={e => setProviderName(e.target.value)} placeholder="حساب بوابة تجريبي"/></label>
          <button disabled={!tenantId || !!busy} className="min-h-11 rounded-xl bg-[#315F5B] px-5 text-sm text-white disabled:opacity-50">إضافة حساب تجريبي متوقف</button></form>
        <div className="space-y-2">{gateways.map(account => <div key={account.accountId} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-black/10 p-3 text-sm"><div><span className="font-semibold">{account.displayName}</span><span className="mr-2 text-xs text-black/50">{account.environment === "Sandbox" ? "تجريبي" : "إنتاج · عرض فقط"}</span></div>
          {account.environment === "Sandbox" ? <button type="button" disabled={!!busy || (!account.isEnabled && !account.hasCredentials)} onClick={() => void toggleGateway(account)} className="rounded-lg border border-black/10 px-3 py-2 disabled:opacity-50">{account.isEnabled ? "إيقاف الاختبار" : "تفعيل الاختبار بعد الربط"}</button> : <span className="text-xs text-amber-800">تفعيل الإنتاج غير متاح هنا</span>}</div>)}</div>
      </div>}
    </section>}
    {!choice && qrPreview && <div role="dialog" aria-label="عرض صورة QR الخاصة بالتاجر" className="rounded-2xl border border-black/10 bg-white p-5"><div className="flex items-center justify-between"><h2 className="font-semibold">صورة QR المحفوظة</h2><button type="button" onClick={() => setQrPreview("")} aria-label="إغلاق الصورة"><X size={19}/></button></div><img src={qrPreview} alt="صورة QR للحساب" className="mx-auto mt-4 max-h-64 max-w-full object-contain"/></div>}
    <footer className="flex items-center gap-2 text-xs text-black/40"><Building2 size={15}/> لا تختزن ركن أموال مشتريات المتاجر في هذه المرحلة. <ChevronLeft size={13}/></footer>
  </main>;
}
