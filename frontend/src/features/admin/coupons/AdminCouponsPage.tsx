import { useCallback, useEffect, useState, type FormEvent } from "react";
import { BarChart3, Check, Copy, Plus, RefreshCw, TicketPercent, X } from "lucide-react";
import { readAdminStore } from "../store-setup/storeSetupStorage";
import { getProducts, type Product } from "../products/productsApi";
import { getCategories, type AdminCategory } from "../catalog/catalogContentApi";
import {
  createCoupon, getCouponAnalytics, listCoupons, updateCoupon,
  type Coupon, type CouponAnalytics, type CouponInput, type CouponScope,
} from "./couponsApi";

const field = "min-h-11 w-full rounded-xl border border-black/10 bg-white px-3 py-2 text-sm outline-none focus:border-[#315F5B]";
const button = "min-h-10 rounded-xl border border-black/10 bg-white px-4 py-2 text-sm font-semibold disabled:opacity-40";
const initial: CouponInput = {
  code: "", name: "", type: "Percentage", value: 10, scope: "EntireStore", currency: "",
  includeDescendantCategories: true, minimumOrderAmount: null,
  maximumTotalUses: null, maximumUsesPerCustomer: 1, startsAtUtc: null, endsAtUtc: null,
  isEnabled: true, productIds: [], categoryIds: [],
};
const money = (value: number, currency: string) =>
  `${new Intl.NumberFormat("ar", {maximumFractionDigits: 2}).format(value)} ${currency}`;
const localDate = (value: string | null) => value ?
  new Date(new Date(value).getTime() - new Date(value).getTimezoneOffset() * 60000).toISOString().slice(0, 16) : "";
const utcDate = (value: string) => value ? new Date(value).toISOString() : null;
const scopeLabels: Record<CouponScope, string> = {
  EntireStore: "المتجر بالكامل", Categories: "أقسام محددة", Products: "منتجات محددة",
};
const fresh = (): CouponInput => ({...initial, productIds: [], categoryIds: []});

export function AdminCouponsPage() {
  const tenantId = readAdminStore()?.tenantId ?? null;
  const [coupons, setCoupons] = useState<Coupon[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<AdminCategory[]>([]);
  const [form, setForm] = useState<CouponInput>(fresh);
  const [editId, setEditId] = useState<string | null>(null);
  const [detailsId, setDetailsId] = useState<string | null>(null);
  const [analytics, setAnalytics] = useState<CouponAnalytics | null>(null);
  const [loading, setLoading] = useState(Boolean(tenantId));
  const [busy, setBusy] = useState(false);
  const [detailsBusy, setDetailsBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [search, setSearch] = useState("");
  const [targetSearch, setTargetSearch] = useState("");
  const [showForm, setShowForm] = useState(false);
  const activeCoupon = coupons.find(x => x.id === detailsId) ?? null;
  // Currency must come from this store's catalog, not a hard-coded country default.
  // A store with mixed product currencies needs an explicit pricing policy first.
  const storeCurrencies = Array.from(new Set(products.flatMap(product => [
    product.currency,
    ...product.variants.filter(v => v.isEnabled && v.priceOverrideCurrency).map(v => v.priceOverrideCurrency!),
  ]).filter(currency => /^[A-Z]{3}$/.test(currency))));
  const storeCurrency = storeCurrencies.length === 1 ? storeCurrencies[0] : null;

  const load = useCallback(async () => {
    if (!tenantId) {setLoading(false); return;}
    const [nextCoupons, nextProducts, nextCategories] = await Promise.all([
      listCoupons(tenantId), getProducts(tenantId), getCategories(tenantId),
    ]);
    setCoupons(nextCoupons); setProducts(nextProducts); setCategories(nextCategories);
    setLoading(false);
  }, [tenantId]);
  useEffect(() => {
    let live = true;
    if (!tenantId) return;
    void Promise.all([listCoupons(tenantId), getProducts(tenantId), getCategories(tenantId)])
      .then(([c, p, k]) => { if (live) {setCoupons(c); setProducts(p); setCategories(k);} })
      .catch(e => {if (live) setError(e instanceof Error ? e.message : "تعذر تحميل بيانات الكوبونات.");})
      .finally(() => {if (live) setLoading(false);});
    return () => {live = false;};
  }, [tenantId]);

  const patch = <K extends keyof CouponInput>(key: K, value: CouponInput[K]) =>
    setForm(current => ({...current, [key]: value}));
  function begin(coupon?: Coupon) {
    setForm(coupon ? {...coupon, productIds: [...coupon.productIds], categoryIds: [...coupon.categoryIds]} : {...fresh(), currency: storeCurrency ?? ""});
    setEditId(coupon?.id ?? null); setTargetSearch(""); setShowForm(true);
    setError(""); setNotice("");
    window.scrollTo({top: 0, behavior: "smooth"});
  }
  function closeForm() {setShowForm(false); setEditId(null); setError("");}
  function toggleTarget(id: string, fieldName: "productIds" | "categoryIds") {
    setForm(current => {
      const ids = current[fieldName];
      return {...current, [fieldName]: ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id]};
    });
  }
  function validate(): string | null {
    const code = form.code.trim().toUpperCase();
    if (!/^[A-Z0-9][A-Z0-9_-]{1,59}$/.test(code)) return "رمز الكوبون: 2–60 حرفًا إنجليزيًا أو رقمًا أو شرطة، بدون فراغات.";
    if (!form.name.trim() || form.name.trim().length > 160) return "أدخل اسمًا واضحًا للكوبون (حتى 160 حرفًا).";
    if (!Number.isFinite(form.value) || form.value <= 0 || (form.type === "Percentage" && form.value > 100)) return "قيمة الخصم يجب أن تكون موجبة، والنسبة لا تتجاوز 100%.";
    if (form.type === "FixedAmount" && form.value > 100000000) return "قيمة الخصم الثابت كبيرة جدًا.";
    if (!storeCurrency) return storeCurrencies.length === 0
      ? "لا يمكن تحديد عملة المتجر قبل إضافة منتج بسعر وعملة. أضف منتجًا أولًا."
      : "المتجر يحتوي على منتجات بعملات مختلفة. وحّد عملة المنتجات قبل إنشاء كوبون على المتجر.";
    if (editId && form.currency !== storeCurrency) return "عملة الكوبون القديم مختلفة عن عملة المتجر الحالية؛ أنشئ كوبونًا جديدًا بدل تغيير تاريخ كوبون مستخدم.";
    if (form.minimumOrderAmount !== null && (!Number.isFinite(form.minimumOrderAmount) || form.minimumOrderAmount < 0)) return "الحد الأدنى للطلب غير صالح.";
    for (const limit of [form.maximumTotalUses, form.maximumUsesPerCustomer]) {
      if (limit !== null && (!Number.isSafeInteger(limit) || limit <= 0)) return "حد الاستخدام يجب أن يكون عددًا صحيحًا موجبًا.";
    }
    if (form.scope === "Categories" && !form.categoryIds.length) return "حدد قسمًا واحدًا على الأقل.";
    if (form.scope === "Products" && !form.productIds.length) return "حدد منتجًا واحدًا على الأقل.";
    if (form.startsAtUtc && form.endsAtUtc && form.startsAtUtc >= form.endsAtUtc) return "تاريخ الانتهاء يجب أن يكون بعد البداية.";
    return null;
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!tenantId || busy) return;
    const invalid = validate(); if (invalid) {setError(invalid); return;}
    const input: CouponInput = {...form, currency: storeCurrency!, code: form.code.trim().toUpperCase(), name: form.name.trim(),
      productIds: form.scope === "Products" ? form.productIds : [], categoryIds: form.scope === "Categories" ? form.categoryIds : [],
      includeDescendantCategories: form.scope === "Categories" && form.includeDescendantCategories};
    setBusy(true); setError(""); setNotice("");
    try {
      if (editId) await updateCoupon(tenantId, editId, input);
      else await createCoupon(tenantId, input);
      await load(); setShowForm(false); setEditId(null);
      setNotice(editId ? "تم تحديث الكوبون." : "تم إنشاء الكوبون. يمكن إيقافه من قائمة الكوبونات.");
    } catch (e) {setError(e instanceof Error ? e.message : "تعذر حفظ الكوبون.");}
    finally {setBusy(false);}
  }
  async function toggle(coupon: Coupon) {
    if (!tenantId || busy) return;
    setBusy(true); setError(""); setNotice("");
    try {
      await updateCoupon(tenantId, coupon.id, {...coupon, isEnabled: !coupon.isEnabled});
      await load(); setNotice(coupon.isEnabled ? "تم إيقاف الكوبون." : "تم تفعيل الكوبون.");
    } catch (e) {setError(e instanceof Error ? e.message : "تعذر تغيير حالة الكوبون.");}
    finally {setBusy(false);}
  }
  async function details(id: string) {
    if (!tenantId) return;
    setDetailsId(id); setAnalytics(null); setDetailsBusy(true); setError("");
    try {setAnalytics(await getCouponAnalytics(tenantId, id));}
    catch (e) {setError(e instanceof Error ? e.message : "تعذر جلب إحصائيات الكوبون.");}
    finally {setDetailsBusy(false);}
  }
  const visible = coupons.filter(c => `${c.code} ${c.name}`.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase()));
  return <main dir="rtl" className="mx-auto max-w-[1160px] space-y-6 pb-16 text-[#172a27]">
    <header className="flex flex-wrap items-center justify-between gap-4">
      <div><p className="text-xs font-bold tracking-wider text-[#315F5B]">RUKN · DISCOUNTS</p><h1 className="mt-2 text-3xl font-semibold">الكوبونات والخصومات</h1><p className="mt-2 text-sm text-black/55">إنشاء كوبونات للمحل بالكامل أو للأقسام أو المنتجات، مع حدود الاستخدام والتواريخ والتقارير.</p></div>
      <button type="button" className="flex min-h-11 items-center gap-2 rounded-xl bg-[#315F5B] px-5 text-sm font-bold text-white" onClick={() => showForm ? closeForm() : begin()}>{showForm ? <X size={17}/> : <Plus size={17}/>} {showForm ? "إغلاق" : "إنشاء كوبون"}</button>
    </header>
    {error && <p role="alert" className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">{error}</p>}
    {notice && <p role="status" className="rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-800">{notice}</p>}
    {!tenantId && <p className="rounded-xl bg-amber-50 p-4 text-sm">اختر متجرًا من لوحة الإدارة أولًا.</p>}
    {showForm && <form onSubmit={e => void save(e)} className="space-y-5 rounded-2xl border border-black/10 bg-white p-5 md:p-7">
      <div className="flex items-center justify-between"><h2 className="text-xl font-semibold">{editId ? "تعديل الكوبون" : "كوبون جديد"}</h2><button type="button" className={button} onClick={closeForm}>إلغاء</button></div>
      <div className="grid gap-4 md:grid-cols-2">
        <label className="space-y-2 text-sm"><span className="font-semibold">رمز الكوبون</span><input className={field} dir="ltr" maxLength={60} value={form.code} onChange={e => patch("code", e.target.value.toUpperCase())} placeholder="RUKN20" required /></label>
        <label className="space-y-2 text-sm"><span className="font-semibold">اسم الحملة</span><input className={field} maxLength={160} value={form.name} onChange={e => patch("name", e.target.value)} placeholder="خصم بداية الموسم" required /></label>
        <label className="space-y-2 text-sm"><span className="font-semibold">نوع الخصم</span><select className={field} value={form.type} onChange={e => patch("type", e.target.value as CouponInput["type"])}><option value="Percentage">نسبة مئوية %</option><option value="FixedAmount">مبلغ ثابت</option></select></label>
        <label className="space-y-2 text-sm"><span className="font-semibold">القيمة {form.type === "Percentage" ? "%" : form.currency}</span><input className={field} type="number" min="0.01" max={form.type === "Percentage" ? 100 : undefined} step="0.01" value={form.value} onChange={e => patch("value", Number(e.target.value))} required /></label>
        <label className="space-y-2 text-sm"><span className="font-semibold">عملة الكوبون (تلقائيًا من أسعار المتجر)</span><input readOnly className={`${field} bg-[#f4f6f3]`} value={storeCurrency ?? "غير محددة"} /><span className="block text-xs text-black/45">{storeCurrency ? `يُحفظ الخصم بعملة المتجر ${storeCurrency}؛ لا حاجة لاختيار العملة.` : storeCurrencies.length > 1 ? "توجد عملات مختلفة في المنتجات؛ يجب توحيدها قبل إنشاء كوبون للمتجر." : "أضف منتجًا يتضمن سعرًا وعملة أولًا."}</span></label>
        <label className="space-y-2 text-sm"><span className="font-semibold">نطاق الكوبون</span><select className={field} value={form.scope} onChange={e => patch("scope", e.target.value as CouponScope)}><option value="EntireStore">المتجر بالكامل</option><option value="Categories">قسم أو عدة أقسام</option><option value="Products">منتج أو عدة منتجات</option></select></label>
      </div>
      {form.scope !== "EntireStore" && <section className="space-y-3 rounded-xl border border-black/10 bg-[#f9faf8] p-4"><div className="flex flex-wrap items-center justify-between gap-3"><h3 className="text-sm font-bold">{form.scope === "Products" ? "المنتجات المستهدفة" : "الأقسام المستهدفة"}</h3><span className="text-xs text-black/50">المحدد: {form.scope === "Products" ? form.productIds.length : form.categoryIds.length}</span></div><input className={field} value={targetSearch} onChange={e => setTargetSearch(e.target.value)} placeholder="بحث عن الاسم…"/><div className="grid max-h-52 gap-2 overflow-y-auto md:grid-cols-2">{form.scope === "Products" ? products.filter(p => p.name.includes(targetSearch)).map(p => <label key={p.productId} className="flex min-h-10 items-center gap-2 rounded-lg bg-white p-2 text-sm"><input type="checkbox" checked={form.productIds.includes(p.productId)} onChange={() => toggleTarget(p.productId, "productIds")}/><span>{p.name}</span></label>) : categories.filter(c => c.name.includes(targetSearch)).map(c => <label key={c.categoryId} className="flex min-h-10 items-center gap-2 rounded-lg bg-white p-2 text-sm"><input type="checkbox" checked={form.categoryIds.includes(c.categoryId)} onChange={() => toggleTarget(c.categoryId, "categoryIds")}/><span>{c.name}</span></label>)}</div>{form.scope === "Categories" && <label className="flex items-center gap-2 text-xs"><input type="checkbox" checked={form.includeDescendantCategories} onChange={e => patch("includeDescendantCategories", e.target.checked)}/>تطبيق الخصم أيضًا على الأقسام الفرعية</label>}</section>}
      <div className="grid gap-4 md:grid-cols-2">
        <label className="space-y-2 text-sm"><span className="font-semibold">إجمالي الاستخدامات المسموحة (فارغ = بلا حد)</span><input className={field} type="number" min="1" step="1" value={form.maximumTotalUses ?? ""} onChange={e => patch("maximumTotalUses", e.target.value === "" ? null : Number(e.target.value))} placeholder="مثال: 100"/></label>
        <label className="space-y-2 text-sm"><span className="font-semibold">عدد الاستخدامات لكل عميل (فارغ = بلا حد)</span><input className={field} type="number" min="1" step="1" value={form.maximumUsesPerCustomer ?? ""} onChange={e => patch("maximumUsesPerCustomer", e.target.value === "" ? null : Number(e.target.value))} placeholder="1"/></label>
        <label className="space-y-2 text-sm"><span className="font-semibold">أقل قيمة للطلب (اختياري)</span><input className={field} type="number" min="0" step="0.01" value={form.minimumOrderAmount ?? ""} onChange={e => patch("minimumOrderAmount", e.target.value === "" ? null : Number(e.target.value))} /></label>
        <label className="space-y-2 text-sm"><span className="font-semibold">بداية العرض (اختياري)</span><input className={field} type="datetime-local" value={localDate(form.startsAtUtc)} onChange={e => patch("startsAtUtc", utcDate(e.target.value))}/></label>
        <label className="space-y-2 text-sm"><span className="font-semibold">نهاية العرض (اختياري)</span><input className={field} type="datetime-local" value={localDate(form.endsAtUtc)} onChange={e => patch("endsAtUtc", utcDate(e.target.value))}/></label>
        {editId && <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isEnabled} onChange={e => patch("isEnabled", e.target.checked)}/>الكوبون مفعّل</label>}
      </div>
      <div className="flex flex-wrap gap-2"><button type="submit" disabled={busy} className="min-h-11 rounded-xl bg-[#315F5B] px-8 text-sm font-semibold text-white disabled:opacity-40">{busy ? "جارٍ الحفظ…" : "حفظ الكوبون"}</button><button type="button" className={button} onClick={closeForm}>إلغاء</button></div>
      <p className="text-xs leading-6 text-black/50">التوفر والحدود والسعر تُتحقق منها على السيرفر وقت إتمام الطلب، وليس من الواجهة فقط.</p>
    </form>}
    <section className="rounded-2xl border border-black/10 bg-white p-5 md:p-7"><div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-xl font-semibold">الكوبونات ({coupons.length})</h2><div className="flex gap-2"><input className={field} value={search} onChange={e => setSearch(e.target.value)} placeholder="ابحث عن كوبون"/><button type="button" className={button} disabled={busy || loading} onClick={() => {void load().catch(e => setError(e instanceof Error ? e.message : "تعذر التحديث."));}} aria-label="تحديث"><RefreshCw size={17}/></button></div></div>
      {loading ? <p className="mt-5 text-sm text-black/50">جارٍ تحميل البيانات…</p> : visible.length === 0 ? <p className="mt-5 text-sm text-black/50">لا توجد كوبونات مطابقة.</p> : <div className="mt-5 grid gap-3 lg:grid-cols-2">{visible.map(c => <article key={c.id} className="rounded-xl border border-black/10 p-4"><div className="flex items-start justify-between gap-3"><div><span dir="ltr" className="inline-flex items-center gap-1 font-bold"><TicketPercent size={17}/>{c.code}</span><p className="mt-1 text-sm text-black/60">{c.name}</p></div><span className={`rounded-full px-3 py-1 text-xs ${c.isEnabled ? "bg-emerald-50 text-emerald-800" : "bg-slate-100 text-slate-600"}`}>{c.isEnabled ? "مفعّل" : "متوقف"}</span></div><p className="mt-3 text-sm">{c.type === "Percentage" ? `${c.value}%` : money(c.value, c.currency)} · {scopeLabels[c.scope]}</p><p className="mt-1 text-xs text-black/45">{c.maximumTotalUses ? `حتى ${c.maximumTotalUses} استخدامات` : "بلا حد إجمالي"}{c.endsAtUtc ? ` · ينتهي ${new Date(c.endsAtUtc).toLocaleDateString("ar")}` : ""}</p><div className="mt-4 flex flex-wrap gap-2"><button type="button" className={button} onClick={() => begin(c)}>تعديل</button><button type="button" className={button} disabled={busy} onClick={() => void toggle(c)}>{c.isEnabled ? "إيقاف" : "تفعيل"}</button><button type="button" className={button} onClick={() => void details(c.id)}><BarChart3 size={14} className="inline"/> التفاصيل</button><button type="button" className={button} onClick={() => {void navigator.clipboard.writeText(c.code).then(() => setNotice("تم نسخ الكود.")).catch(() => setError("تعذر النسخ."));}}><Copy size={14} className="inline"/> نسخ</button></div></article>)}</div>}
    </section>
    {activeCoupon && <section className="rounded-2xl border border-black/10 bg-white p-5 md:p-7"><div className="flex items-center justify-between gap-3"><h2 className="text-xl font-semibold">تقرير الكوبون: <span dir="ltr">{activeCoupon.code}</span></h2><button type="button" className={button} onClick={() => {setDetailsId(null);setAnalytics(null);}}><X size={16}/></button></div>{detailsBusy ? <p className="mt-4 text-sm">جارٍ تحميل التقرير…</p> : analytics ? <><div className="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">{[{name: "مرات الاستخدام", value: `${analytics.usageCount}${activeCoupon.maximumTotalUses ? ` / ${activeCoupon.maximumTotalUses}` : ""}`},{name: "عملاء مختلفون", value: analytics.uniqueCustomers},{name: "طلبات مدفوعة/قيد التجهيز", value: analytics.paidOrderCount},{name: "طلبات بانتظار الدفع", value: analytics.pendingOrderCount},{name: "طلبات ملغاة", value: analytics.cancelledOrderCount},{name: "قيمة طلبات بانتظار الدفع (ليست مبيعات محصّلة)", value: money(analytics.pendingRevenue, activeCoupon.currency)},{name: "المبيعات للطلبات المدفوعة", value: money(analytics.paidRevenue, activeCoupon.currency)},{name: "خصومات الطلبات المدفوعة", value: money(analytics.paidDiscountTotal, activeCoupon.currency)}].map(item => <div key={item.name} className="rounded-xl bg-[#f3f6f3] p-4"><p className="text-xs text-black/55">{item.name}</p><p className="mt-2 text-lg font-bold">{item.value}</p></div>)}</div><p className="mt-4 text-xs leading-6 text-black/50">المبيعات المبيّنة تخص طلبات الكوبون التي وصلت إلى حالة مدفوعة/مؤكدة؛ ليست صافي الربح ولا دليلًا على مبيعات إضافية سبّبها الكوبون. تُستبعد الطلبات الملغاة من هذا الإجمالي.</p><h3 className="mt-6 font-semibold">آخر استخدامات الكوبون</h3><div className="mt-3 overflow-x-auto"><table className="w-full min-w-[550px] text-right text-xs"><thead className="bg-[#f3f6f3]"><tr><th className="p-3">الطلب</th><th className="p-3">التاريخ</th><th className="p-3">الحالة</th><th className="p-3">قيمة الطلب</th><th className="p-3">الخصم</th></tr></thead><tbody>{analytics.recentUses.map(item => <tr key={item.orderId} className="border-b border-black/5"><td className="p-3" dir="ltr">{item.orderId.slice(0, 8)}</td><td className="p-3">{new Date(item.redeemedAtUtc).toLocaleString("ar")}</td><td className="p-3">{item.status}</td><td className="p-3">{money(item.orderAmount, activeCoupon.currency)}</td><td className="p-3">{money(item.discountAmount, activeCoupon.currency)}</td></tr>)}</tbody></table></div></> : <p className="mt-4 text-sm text-red-700">تعذر تحميل التقرير.</p>}</section>}
    <p className="text-xs text-black/45"><Check size={13} className="inline"/> تُحفظ تغييرات الكوبونات في قاعدة بيانات المتجر. لا نغيّر نظام الدفع السوري أو الطلبات أثناء هذه المرحلة.</p>
  </main>;
}
