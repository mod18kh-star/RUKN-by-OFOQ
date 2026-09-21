import { useCallback, useEffect, useState, type FormEvent } from "react";
import { MapPin, PackageCheck, Plus, RefreshCw, Truck } from "lucide-react";
import { readAdminStore } from "../store-setup/storeSetupStorage";
import {
  createShippingMethod, listLocations, listShippingMethods, updateShippingMethod,
  type ShippingMethod, type ShippingMethodInput, type FulfillmentLocation,
} from "./shippingAdminApi";

const inputClass = "w-full min-h-11 rounded-xl border border-black/10 bg-white px-3 py-2.5 text-sm outline-none focus:border-[#315F5B]";
const initial: ShippingMethodInput = {
  code: "", name: "", type: "FlatRate", price: 0, currency: "SAR",
  minimumOrderAmount: null, maximumOrderAmount: null, pickupLocationId: null,
  sortOrder: 0, isEnabled: false,
};
const typeLabels: Record<string, string> = { FlatRate: "رسوم ثابتة", Free: "توصيل مجاني", Pickup: "استلام من المتجر" };
const currencyOptions = ["SYP", "SAR", "AED", "USD"];

export function AdminShippingPage() {
  const tenantId = readAdminStore()?.tenantId ?? null;
  const [methods, setMethods] = useState<ShippingMethod[]>([]);
  const [locations, setLocations] = useState<FulfillmentLocation[]>([]);
  const [form, setForm] = useState<ShippingMethodInput>(initial);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");

  const load = useCallback(async () => {
    if (!tenantId) { setLoading(false); return; }
    const [nextMethods, nextLocations] = await Promise.all([
      listShippingMethods(tenantId), listLocations(tenantId),
    ]);
    setMethods(nextMethods); setLocations(nextLocations); setLoading(false);
  }, [tenantId]);

  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        if (!tenantId) return;
        const [nextMethods, nextLocations] = await Promise.all([
          listShippingMethods(tenantId), listLocations(tenantId),
        ]);
        if (active) { setMethods(nextMethods); setLocations(nextLocations); }
      } catch (exception) { if (active) setError(exception instanceof Error ? exception.message : "تعذر تحميل إعدادات التوصيل."); }
      finally { if (active) setLoading(false); }
    })();
    return () => { active = false; };
  }, [tenantId]);

  function edit(method: ShippingMethod) {
    setEditingId(method.id);
    setForm({ code: method.code, name: method.name, type: method.type,
      price: method.price, currency: method.currency,
      minimumOrderAmount: method.minimumOrderAmount,
      maximumOrderAmount: method.maximumOrderAmount,
      pickupLocationId: method.pickupLocationId, sortOrder: method.sortOrder,
      isEnabled: method.isEnabled });
    setError(""); setNotice("");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!tenantId || busy) return;
    const cleanCode = form.code.trim().toLowerCase();
    const cleanName = form.name.trim();
    if (!/^[a-z0-9][a-z0-9_-]{1,59}$/.test(cleanCode) || !cleanName || cleanName.length > 160) {
      setError("أدخل رمزًا إنجليزيًا صحيحًا واسمًا واضحًا لطريقة التوصيل."); return;
    }
    if (!Number.isFinite(form.price) || form.price < 0 || !Number.isInteger(form.sortOrder) || form.sortOrder < 0) {
      setError("رسوم التوصيل وترتيبه يجب أن يكونا قيمتين صحيحتين غير سالبتين."); return;
    }
    if (form.type === "Pickup" && !locations.some(x => x.id === form.pickupLocationId && x.isActive)) {
      setError("حدد موقع استلام فعّالًا من مواقع المتجر."); return;
    }
    const input: ShippingMethodInput = { ...form, code: cleanCode, name: cleanName,
      isEnabled: form.type === "Pickup" ? form.isEnabled : false,
      pickupLocationId: form.type === "Pickup" ? form.pickupLocationId : null,
      price: form.type === "Free" || form.type === "Pickup" ? 0 : form.price };
    setBusy(true); setError(""); setNotice("");
    try {
      if (editingId) await updateShippingMethod(tenantId, { ...input, id: editingId });
      else await createShippingMethod(tenantId, input);
      await load(); setEditingId(null); setForm(initial);
      setNotice("حُفظت إعدادات التوصيل على السيرفر. تحقق من ظهور الطريقة للعميل بعد إعداد رحلة العنوان والتوصيل.");
    } catch (exception) { setError(exception instanceof Error ? exception.message : "تعذر حفظ طريقة التوصيل."); }
    finally { setBusy(false); }
  }

  async function toggle(method: ShippingMethod) {
    if (!tenantId || busy) return;
    if (!method.isEnabled && method.type !== "Pickup") {
      setError("تفعيل التوصيل إلى العنوان مؤجل حتى تكتمل رحلة العنوان والتحقق من الرسوم عند إتمام الطلب."); return;
    }
    setBusy(true); setError(""); setNotice("");
    try {
      await updateShippingMethod(tenantId, { ...method, isEnabled: !method.isEnabled });
      await load(); setNotice("تم تغيير حالة طريقة التوصيل وحفظها على السيرفر.");
    } catch (exception) { setError(exception instanceof Error ? exception.message : "تعذر تغيير حالة الطريقة."); }
    finally { setBusy(false); }
  }

  return <div dir="rtl" className="mx-auto max-w-[1180px] space-y-6 pb-12 text-[#192721]">
    <header className="flex flex-wrap items-end justify-between gap-4"><div>
      <p className="text-xs font-semibold text-[#315F5B]">RUKN · FULFILLMENT</p><h1 className="mt-2 text-3xl font-semibold">التوصيل والاستلام</h1>
      <p className="mt-2 text-sm leading-7 text-black/55">حدد طرق تسليم الطلبات ورسومها وفعل أو أوقف كل طريقة حسب حاجة متجرك.</p></div>
      <button type="button" disabled={loading || busy} onClick={() => void load().catch(() => setError("تعذر تحديث الطرق."))}
        className="flex min-h-11 items-center gap-2 rounded-xl border border-black/10 bg-white px-4 text-sm disabled:opacity-50"><RefreshCw size={16}/> تحديث</button></header>
    <div className="grid gap-4 md:grid-cols-3">
      <div className="rounded-2xl border border-black/10 bg-white p-5"><Truck size={22} className="text-[#315F5B]"/><h2 className="mt-3 font-semibold">توصيل المتجر</h2><p className="mt-2 text-sm leading-6 text-black/55">أنشئ خيار رسوم ثابتة أو توصيل مجاني، مع تطبيق الرسوم والتحقق منها على السيرفر.</p></div>
      <div className="rounded-2xl border border-black/10 bg-white p-5"><MapPin size={22} className="text-[#315F5B]"/><h2 className="mt-3 font-semibold">الاستلام</h2><p className="mt-2 text-sm leading-6 text-black/55">اربط الطريقة بنقطة استلام فعّالة موجودة في إعدادات مخزون المتجر.</p></div>
      <div className="rounded-2xl border border-amber-200 bg-amber-50 p-5"><PackageCheck size={22} className="text-amber-700"/><h2 className="mt-3 font-semibold">شركات الشحن</h2><p className="mt-2 text-sm leading-6 text-amber-950/75">إدارة الشركات وربط واجهاتها وتتبع الشحنات ليست مفعّلة بعد؛ لا تعتبر اسم طريقة التوصيل تكاملًا آليًا مع شركة.</p></div>
    </div>
    {error && <div role="alert" className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">{error}</div>}
    {notice && <div role="status" className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-900">{notice}</div>}
    <form onSubmit={event => void save(event)} className="rounded-2xl border border-black/10 bg-white p-5 md:p-7">
      <h2 className="text-xl font-semibold">{editingId ? "تعديل طريقة التوصيل" : "إضافة طريقة توصيل"}</h2>
      <div className="mt-5 grid gap-4 md:grid-cols-2">
        <label className="space-y-2 text-sm"><span>اسم الطريقة</span><input className={inputClass} maxLength={160} value={form.name} onChange={e => setForm(prev => ({...prev, name: e.target.value}))} required placeholder="مثال: توصيل داخل المدينة"/></label>
        <label className="space-y-2 text-sm"><span>رمز الطريقة (إنجليزي)</span><input className={inputClass} dir="ltr" maxLength={60} value={form.code} onChange={e => setForm(prev => ({...prev, code: e.target.value}))} required placeholder="delivery_city"/></label>
        <label className="space-y-2 text-sm"><span>النوع</span><select className={inputClass} value={form.type} onChange={e => setForm(prev => ({...prev, type: e.target.value as ShippingMethodInput["type"]}))}>
          <option value="FlatRate">توصيل برسوم ثابتة</option><option value="Free">توصيل مجاني</option><option value="Pickup">استلام من موقع المتجر</option></select></label>
        <label className="space-y-2 text-sm"><span>العملة</span><select className={inputClass} value={form.currency} onChange={e => setForm(prev => ({...prev, currency: e.target.value}))}>
          {currencyOptions.map(currency => <option key={currency} value={currency}>{currency}</option>)}</select></label>
        {form.type === "Pickup" ? <label className="space-y-2 text-sm md:col-span-2"><span>موقع الاستلام</span><select className={inputClass} required value={form.pickupLocationId ?? ""} onChange={e => setForm(prev => ({...prev, pickupLocationId: e.target.value || null}))}>
          <option value="">حدد الموقع</option>{locations.filter(location => location.isActive).map(location => <option key={location.id} value={location.id}>{location.name} — {location.city}</option>)}
        </select><span className="block text-xs text-black/50">إذا لم تجد موقعًا، أضفه من صفحة المخزون أولًا.</span></label> :
          <label className="space-y-2 text-sm"><span>رسوم التوصيل</span><input type="number" min={0} step="0.01" className={inputClass} value={form.type === "Free" ? 0 : form.price} disabled={form.type === "Free"} onChange={e => setForm(prev => ({...prev, price: Number(e.target.value)}))}/></label>}
        <label className="space-y-2 text-sm"><span>الترتيب في صفحة العميل</span><input type="number" min={0} step={1} className={inputClass} value={form.sortOrder} onChange={e => setForm(prev => ({...prev, sortOrder: Number(e.target.value)}))}/></label>
        <label className="flex items-center gap-3 rounded-xl bg-[#f5f6f2] p-3 text-sm"><input type="checkbox" checked={form.type === "Pickup" && form.isEnabled} disabled={form.type !== "Pickup"} onChange={e => setForm(prev => ({...prev, isEnabled: e.target.checked}))} className="size-4 accent-[#315F5B]"/> {form.type === "Pickup" ? "تفعيل الاستلام عند الحفظ" : "التوصيل إلى العنوان سيُحفظ متوقفًا حتى اكتمال إتمام الطلب"}</label>
      </div>
      <div className="mt-5 flex flex-wrap gap-2"><button disabled={busy || !tenantId || loading} className="flex min-h-11 items-center gap-2 rounded-xl bg-[#315F5B] px-5 text-sm font-semibold text-white disabled:opacity-50"><Plus size={16}/>{busy ? "جاري الحفظ…" : editingId ? "حفظ التعديل" : "إضافة الطريقة"}</button>
      {editingId && <button type="button" onClick={() => { setEditingId(null); setForm(initial); }} disabled={busy} className="rounded-xl border border-black/10 px-5 text-sm">إلغاء التعديل</button>}</div>
    </form>
    <section className="rounded-2xl border border-black/10 bg-white p-5 md:p-7"><h2 className="text-xl font-semibold">طرق التوصيل المسجلة</h2>
      {loading ? <p className="mt-5 text-sm">جاري التحميل…</p> : methods.length === 0 ? <p className="mt-5 text-sm text-black/55">لا توجد طرق مسجلة بعد.</p> :
      <div className="mt-5 grid gap-3 lg:grid-cols-2">{methods.map(method => <article key={method.id} className="rounded-xl border border-black/10 p-4">
        <div className="flex items-center justify-between gap-3"><h3 className="font-semibold">{method.name}</h3><span className={`rounded-full px-3 py-1 text-xs ${method.isEnabled ? "bg-emerald-100 text-emerald-800" : "bg-slate-100 text-slate-600"}`}>{method.isEnabled ? "مفعّلة" : "متوقفة"}</span></div>
        <p className="mt-2 text-xs text-black/50">{typeLabels[method.type] ?? method.type} · {method.price.toLocaleString("ar-SA")} {method.currency} · الترتيب {method.sortOrder}</p>
        <div className="mt-4 flex gap-2"><button type="button" disabled={busy} onClick={() => edit(method)} className="min-h-10 rounded-lg border border-black/10 px-4 text-sm disabled:opacity-50">تعديل</button>
          <button type="button" disabled={busy} onClick={() => void toggle(method)} className="min-h-10 rounded-lg bg-[#315F5B] px-4 text-sm text-white disabled:opacity-50">{method.isEnabled ? "إيقاف" : "تفعيل"}</button></div>
      </article>)}</div>}
    </section>
  </div>;
}
