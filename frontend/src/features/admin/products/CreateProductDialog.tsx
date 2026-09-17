import { useMemo, useState } from "react";
import { ArrowLeft, Image as ImageIcon, PackagePlus, Plus, Sparkles, Trash2, Upload, X } from "lucide-react";
import type { AdminCategory } from "../catalog/catalogContentApi";
import { readAdminStore } from "../store-setup/storeSetupStorage";
import {
  createProductOption,
  createProductOptionValue,
  createStructuredProductVariant,
  getCurrentTenantId,
  getProductAttributes,
  setProductAttributes,
  setProductImages,
  uploadProductAsset,
  type CreateProductInput,
  type Product,
} from "./productsApi";

interface CreateProductDialogProps {
  open: boolean;
  busy: boolean;
  categories: AdminCategory[];
  onClose: () => void;
  onCreate: (input: CreateProductInput) => Promise<Product>;
}

type FormState = {
  name: string; slug: string; description: string; categoryId: string;
  primaryImageUrl: string; secondaryImageUrl: string;
  price: string; compareAtPrice: string; currency: string; sku: string;
  trackInventory: boolean; quantity: string; lowStockThreshold: string;
  continueSellingWhenOutOfStock: boolean; publishImmediately: boolean;
};

type Dimension = { id: string; name: string };
type VariantRow = {
  id: string; values: Record<string, string>; sku: string; quantity: string;
  priceOverride: string; lowStockThreshold: string;
};

type SpecField = { key: string; label: string; type?: "text" | "number" | "boolean"; placeholder?: string };

const inputClass = "h-12 w-full rounded-[10px] border border-black/[0.11] bg-white px-4 text-[13px] outline-none transition placeholder:text-black/25 focus:border-[#a77a43]/70";
const labelClass = "mb-2 block text-[11px] font-semibold text-black/62";

function detectDefaultCurrency() {
  const country = window.localStorage.getItem("ofoq.onboarding.pricing-country");
  if (country === "SA") return "SAR";
  if (country === "AE") return "AED";
  return "USD";
}

function initialState(): FormState {
  return {
    name: "", slug: "", description: "", categoryId: "", primaryImageUrl: "", secondaryImageUrl: "",
    price: "", compareAtPrice: "", currency: detectDefaultCurrency(), sku: "", trackInventory: true,
    quantity: "0", lowStockThreshold: "5", continueSellingWhenOutOfStock: false, publishImmediately: true,
  };
}

function createSlug(value: string) {
  return value.normalize("NFKC").trim().toLowerCase().replace(/[^\p{L}\p{N}]+/gu, "-").replace(/^-+|-+$/g, "").replace(/-{2,}/g, "-");
}

function categoryLabel(category: AdminCategory, categories: AdminCategory[]) {
  const names = [category.name]; let parentId = category.parentCategoryId; let guard = 0;
  while (parentId && guard < 8) {
    const parent = categories.find((item) => item.categoryId === parentId); if (!parent) break;
    names.unshift(parent.name); parentId = parent.parentCategoryId; guard += 1;
  }
  return names.join(" ← ");
}

function uid() { return `${Date.now()}-${Math.random().toString(36).slice(2)}`; }

function presetDimensions(verticalCode?: string): Dimension[] {
  if (verticalCode === "mobile-phones") return [{ id: uid(), name: "اللون" }, { id: uid(), name: "السعة" }];
  if (verticalCode === "apparel" || verticalCode === "footwear") return [{ id: uid(), name: "اللون" }, { id: uid(), name: "المقاس" }];
  if (verticalCode === "perfumes") return [{ id: uid(), name: "الحجم" }, { id: uid(), name: "التركيز" }];
  return [{ id: uid(), name: "الخيار" }];
}

function specFields(verticalCode?: string): SpecField[] {
  switch (verticalCode) {
    case "mobile-phones": return [
      { key: "brand", label: "العلامة التجارية", placeholder: "Apple" },
      { key: "model", label: "الموديل", placeholder: "iPhone 17 Pro" },
      { key: "storage-gb", label: "السعة الأساسية GB", type: "number" },
      { key: "ram-gb", label: "الذاكرة RAM GB", type: "number" },
      { key: "color", label: "اللون الأساسي" },
      { key: "screen-size-inch", label: "حجم الشاشة (إنش)", type: "number" },
      { key: "warranty-months", label: "الضمان بالأشهر", type: "number" },
      { key: "release-year", label: "سنة الإصدار", type: "number" },
      { key: "dual-sim", label: "Dual SIM", type: "boolean" },
      { key: "condition", label: "الحالة", placeholder: "New / Used / Refurbished" },
    ];
    case "electronics": return [
      { key: "brand", label: "العلامة التجارية" }, { key: "model", label: "الموديل" },
      { key: "warranty-months", label: "الضمان بالأشهر", type: "number" },
    ];
    case "apparel": return [
      { key: "brand", label: "العلامة التجارية" }, { key: "gender", label: "الفئة" },
      { key: "material", label: "الخامة" }, { key: "fit", label: "القصة / Fit" },
    ];
    case "footwear": return [
      { key: "brand", label: "العلامة التجارية" }, { key: "gender", label: "الفئة" }, { key: "material", label: "الخامة" },
    ];
    default: return [{ key: "brand", label: "العلامة التجارية" }, { key: "model", label: "الموديل" }];
  }
}

export function CreateProductDialog({ open, busy, categories, onClose, onCreate }: CreateProductDialogProps) {
  const tenantId = getCurrentTenantId();
  const store = useMemo(() => readAdminStore(), []);
  const verticalCode = store?.verticalCode;
  const [form, setForm] = useState<FormState>(initialState);
  const [slugTouched, setSlugTouched] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [primaryFile, setPrimaryFile] = useState<File | null>(null);
  const [secondaryFile, setSecondaryFile] = useState<File | null>(null);
  const [hasVariants, setHasVariants] = useState(false);
  const [dimensions, setDimensions] = useState<Dimension[]>(() => presetDimensions(verticalCode));
  const [rows, setRows] = useState<VariantRow[]>([]);
  const [specValues, setSpecValues] = useState<Record<string, string>>({});
  const specs = useMemo(() => specFields(verticalCode), [verticalCode]);
  const visibleCategories = useMemo(() => categories.filter((category) => category.isVisible), [categories]);

  const valid = Boolean(form.name.trim() && form.slug.trim() && form.sku.trim() && form.sku.trim().length <= 64 && form.categoryId && Number(form.price) >= 0 && form.currency.trim().length === 3 && (form.primaryImageUrl.trim() || primaryFile));

  if (!open) return null;
  function update(key: keyof FormState, value: string | boolean) { setForm((current) => ({ ...current, [key]: value })); }
  function changeName(value: string) { setForm((current) => ({ ...current, name: value, slug: slugTouched ? current.slug : createSlug(value) })); }
  function close() { if (busy) return; setForm(initialState()); setSlugTouched(false); setError(null); setPrimaryFile(null); setSecondaryFile(null); setHasVariants(false); setDimensions(presetDimensions(verticalCode)); setRows([]); setSpecValues({}); onClose(); }
  function addDimension() { if (dimensions.length >= 3) return; setDimensions((current) => [...current, { id: uid(), name: "خاصية جديدة" }]); }
  function addRow() { const values = Object.fromEntries(dimensions.map((d) => [d.id, ""])); setRows((current) => [...current, { id: uid(), values, sku: "", quantity: "0", priceOverride: "", lowStockThreshold: "5" }]); }
  function updateRow(id: string, patch: Partial<VariantRow>) { setRows((current) => current.map((row) => row.id === id ? { ...row, ...patch } : row)); }

  async function submit() {
    if (!valid || busy || !tenantId) return;
    setError(null);
    try {
      let primaryUrl = form.primaryImageUrl.trim();
      let secondaryUrl = form.secondaryImageUrl.trim();
      if (primaryFile) primaryUrl = await uploadProductAsset(tenantId, primaryFile);
      if (secondaryFile) secondaryUrl = await uploadProductAsset(tenantId, secondaryFile);
      if (!primaryUrl) throw new Error("الصورة الأساسية مطلوبة لأنها صورة بطاقة المنتج في المتجر.");

      if (hasVariants) {
        if (rows.length === 0) throw new Error("أضف تركيبة واحدة على الأقل للخيارات.");
        for (const row of rows) {
          if (!row.sku.trim()) throw new Error("كل تركيبة تحتاج SKU خاص بها.");
          if (dimensions.some((d) => !row.values[d.id]?.trim())) throw new Error("أكمل قيم اللون/المقاس/السعة لكل تركيبة.");
          if (!Number.isInteger(Number(row.quantity)) || Number(row.quantity) < 0) throw new Error("كمية كل تركيبة يجب أن تكون رقمًا صحيحًا موجبًا أو صفر.");
        }
      }

      const created = await onCreate({
        name: form.name.trim(), slug: form.slug.trim(), description: form.description.trim() || null,
        categoryId: form.categoryId, price: Number(form.price), compareAtPrice: form.compareAtPrice.trim() ? Number(form.compareAtPrice) : null,
        currency: form.currency.trim().toUpperCase(), sku: form.sku.trim(), trackInventory: form.trackInventory,
        quantity: Number(form.quantity), lowStockThreshold: Number(form.lowStockThreshold), continueSellingWhenOutOfStock: form.continueSellingWhenOutOfStock,
        primaryImageUrl: primaryUrl, publishImmediately: form.publishImmediately,
      });

      const imageInputs = [{ url: primaryUrl, altText: form.name.trim(), isPrimary: true }];
      if (secondaryUrl) imageInputs.push({ url: secondaryUrl, altText: `${form.name.trim()} - صورة إضافية`, isPrimary: false });
      await setProductImages(tenantId, created.productId, imageInputs);

      const attributeResponse = await getProductAttributes(tenantId, created.productId);
      const supported = new Set(attributeResponse.attributes.map((a) => a.key));
      const values = Object.entries(specValues).filter(([key, value]) => supported.has(key) && value.trim()).map(([key, value]) => ({ key, value: value.trim() }));
      if (values.length) await setProductAttributes(tenantId, created.productId, values);

      if (hasVariants && rows.length) {
        const optionValueIds = new Map<string, string>();
        for (let dIndex = 0; dIndex < dimensions.length; dIndex += 1) {
          const dimension = dimensions[dIndex];
          const option = await createProductOption(tenantId, created.productId, dimension.name.trim(), dIndex);
          const uniqueValues = [...new Set(rows.map((row) => row.values[dimension.id].trim()).filter(Boolean))];
          for (let vIndex = 0; vIndex < uniqueValues.length; vIndex += 1) {
            const value = uniqueValues[vIndex];
            const createdValue = await createProductOptionValue(tenantId, created.productId, option.optionId, value, vIndex);
            optionValueIds.set(`${dimension.id}:${value}`, createdValue.valueId);
          }
        }
        for (const row of rows) {
          const selections = dimensions.map((dimension) => optionValueIds.get(`${dimension.id}:${row.values[dimension.id].trim()}`)).filter((value): value is string => Boolean(value));
          const variantName = dimensions.map((dimension) => row.values[dimension.id].trim()).join(" / ");
          await createStructuredProductVariant(tenantId, created.productId, {
            name: variantName, sku: row.sku.trim(), priceOverride: row.priceOverride.trim() ? Number(row.priceOverride) : null,
            trackInventory: true, quantity: Number(row.quantity), lowStockThreshold: Number(row.lowStockThreshold || 5),
            continueSellingWhenOutOfStock: form.continueSellingWhenOutOfStock, optionValueIds: selections,
          });
        }
      }

      close();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "تعذر إضافة المنتج وتفاصيله.");
    }
  }

  return (
    <div dir="rtl" className="fixed inset-0 z-[100] flex items-end justify-center bg-black/35 backdrop-blur-[2px] md:items-center md:p-5">
      <div className="max-h-[95vh] w-full overflow-y-auto rounded-t-[22px] bg-[#f6f4ee] shadow-2xl md:max-w-[1050px] md:rounded-[22px]">
        <div className="sticky top-0 z-10 flex items-center justify-between border-b border-black/[0.07] bg-[#f6f4ee]/95 px-6 py-5 backdrop-blur-xl md:px-8">
          <div className="flex items-center gap-3"><div className="flex size-10 items-center justify-center rounded-[10px] bg-[#080b14] text-white"><PackagePlus size={18}/></div><div><h2 className="text-[20px] font-semibold">إضافة منتج متكامل</h2><p className="mt-1 text-[10px] text-black/43">صورة البطاقة، التفاصيل، المواصفات، والخيارات بمخزون مستقل.</p></div></div>
          <button type="button" onClick={close} disabled={busy} className="flex size-10 items-center justify-center rounded-full border border-black/[0.09] bg-white"><X size={17}/></button>
        </div>

        <div className="p-6 md:p-8">
          <section><p className="mb-4 text-[12px] font-semibold">1. المعلومات الأساسية</p><div className="grid gap-5 md:grid-cols-2">
            <label className="md:col-span-2"><span className={labelClass}>اسم المنتج</span><input autoFocus value={form.name} onChange={(e)=>changeName(e.target.value)} className={inputClass} placeholder="مثال: آيفون 17 برو"/></label>
            <label><span className={labelClass}>القسم</span><select value={form.categoryId} onChange={(e)=>update("categoryId",e.target.value)} className={inputClass}><option value="">اختر القسم</option>{visibleCategories.map((c)=><option key={c.categoryId} value={c.categoryId}>{categoryLabel(c,categories)}</option>)}</select></label>
            <label><span className={labelClass}>رابط المنتج</span><input dir="ltr" value={form.slug} onChange={(e)=>{setSlugTouched(true);update("slug",createSlug(e.target.value));}} className={`${inputClass} text-left`}/></label>
            <label><span className={labelClass}>SKU الأساسي</span><input dir="ltr" maxLength={64} value={form.sku} onChange={(e)=>update("sku",e.target.value)} className={`${inputClass} text-left`} placeholder="IPH17-BASE"/></label>
            <label><span className={labelClass}>العملة</span><select value={form.currency} onChange={(e)=>update("currency",e.target.value)} className={inputClass}><option value="SAR">SAR — ريال سعودي</option><option value="AED">AED — درهم إماراتي</option><option value="USD">USD — دولار أمريكي</option></select></label>
            <label><span className={labelClass}>السعر الأساسي</span><input dir="ltr" type="number" min="0" step="0.01" value={form.price} onChange={(e)=>update("price",e.target.value)} className={`${inputClass} text-left`}/></label>
            <label><span className={labelClass}>السعر قبل الخصم</span><input dir="ltr" type="number" min="0" step="0.01" value={form.compareAtPrice} onChange={(e)=>update("compareAtPrice",e.target.value)} className={`${inputClass} text-left`}/></label>
            <label className="md:col-span-2"><span className={labelClass}>الوصف</span><textarea value={form.description} onChange={(e)=>update("description",e.target.value)} className="min-h-[115px] w-full rounded-[10px] border border-black/[0.11] bg-white p-4 text-[12px] leading-7 outline-none" placeholder="وصف يظهر داخل صفحة المنتج وتحت البطاقة بشكل مختصر..."/></label>
          </div></section>

          <div className="my-7 border-t border-black/[0.07]"/>
          <section><div className="mb-4"><p className="text-[12px] font-semibold">2. صور المنتج</p><p className="mt-1 text-[9px] text-black/40">الصورة الأساسية تظهر على بطاقة المنتج. الصورة الإضافية تظهر داخل صفحة التفاصيل وعند تمرير المؤشر في بعض الثيمات.</p></div><div className="grid gap-4 md:grid-cols-2">
            {[{kind:"primary" as const,label:"الصورة الأساسية — مطلوبة",file:primaryFile,setFile:setPrimaryFile,url:form.primaryImageUrl,key:"primaryImageUrl" as const},{kind:"secondary" as const,label:"الصورة الداخلية الإضافية — اختيارية",file:secondaryFile,setFile:setSecondaryFile,url:form.secondaryImageUrl,key:"secondaryImageUrl" as const}].map((item)=><div key={item.kind} className="rounded-[14px] border border-black/[0.07] bg-white p-4"><p className="text-[10px] font-semibold">{item.label}</p><label className="mt-3 flex h-24 cursor-pointer items-center justify-center rounded-[10px] border border-dashed border-black/15 bg-[#faf9f5] text-[10px] text-black/45"><input type="file" accept="image/*" className="hidden" onChange={(e)=>item.setFile(e.target.files?.[0]??null)}/>{item.file?<span>{item.file.name}</span>:<span className="inline-flex items-center gap-2"><Upload size={14}/> رفع من الجهاز</span>}</label><input dir="ltr" value={item.url} onChange={(e)=>update(item.key,e.target.value)} className={`${inputClass} mt-3 text-left`} placeholder="أو رابط صورة مباشر"/>{(item.file||item.url)?<div className="mt-3 flex items-center gap-2 text-[9px] text-[#5d4b2f]"><ImageIcon size={13}/>{item.kind==="primary"?"هذه هي صورة الكرت الخارجية":"تظهر داخل صفحة المنتج"}</div>:null}</div>)}
          </div></section>

          <div className="my-7 border-t border-black/[0.07]"/>
          <section><div className="mb-4 flex items-end justify-between gap-3"><div><p className="text-[12px] font-semibold">3. المواصفات حسب نوع المتجر</p><p className="mt-1 text-[9px] text-black/40">ركن يعرض حقول مناسبة لنشاطك بدل نموذج واحد لكل المتاجر.</p></div><span className="rounded-full bg-[#eee7da] px-3 py-1 text-[9px] font-semibold text-[#79572f]">{verticalCode ?? "general"}</span></div><div className="grid gap-4 md:grid-cols-2">{specs.map((field)=><label key={field.key}><span className={labelClass}>{field.label}</span>{field.type==="boolean"?<select value={specValues[field.key]??""} onChange={(e)=>setSpecValues((c)=>({...c,[field.key]:e.target.value}))} className={inputClass}><option value="">غير محدد</option><option value="true">نعم</option><option value="false">لا</option></select>:<input type={field.type==="number"?"number":"text"} value={specValues[field.key]??""} onChange={(e)=>setSpecValues((c)=>({...c,[field.key]:e.target.value}))} className={inputClass} placeholder={field.placeholder}/>}</label>)}</div><div className="mt-4 rounded-[12px] border border-dashed border-[#b78a52]/35 bg-[#fbf7f0] p-4"><div className="flex gap-3"><Sparkles size={16} className="mt-0.5 text-[#9b6d35]"/><div><p className="text-[10px] font-semibold">مساعد وصف ومواصفات بالذكاء الاصطناعي</p><p className="mt-1 text-[9px] leading-5 text-black/42">مكانه صار محدد ضمن المنتج. الربط الحقيقي بمزود AI سيكون بعد رفع GitHub حتى لا نضع مفتاحًا سريًا داخل الواجهة أو السكربت.</p></div></div></div></section>

          <div className="my-7 border-t border-black/[0.07]"/>
          <section><div className="flex items-center justify-between gap-4"><div><p className="text-[12px] font-semibold">4. هل للمنتج ألوان / مقاسات / سعات متعددة؟</p><p className="mt-1 text-[9px] text-black/40">كل تركيبة لها SKU ومخزون مستقل، وتظهر للعميل من نفس صفحة المنتج.</p></div><button type="button" onClick={()=>setHasVariants((v)=>!v)} className={`rounded-full px-4 py-2 text-[10px] font-semibold ${hasVariants?"bg-[#101614] text-white":"border border-black/10 bg-white"}`}>{hasVariants?"مفعّل":"تفعيل الخيارات"}</button></div>
            {hasVariants?<div className="mt-5 space-y-5"><div className="rounded-[14px] border border-black/[0.07] bg-white p-4"><div className="mb-3 flex items-center justify-between"><p className="text-[10px] font-semibold">خصائص التغيير</p><button type="button" onClick={addDimension} disabled={dimensions.length>=3} className="inline-flex items-center gap-1 text-[9px] font-semibold text-[#7f5d35] disabled:opacity-30"><Plus size={12}/> إضافة خاصية</button></div><div className="grid gap-3 md:grid-cols-3">{dimensions.map((d)=><input key={d.id} value={d.name} onChange={(e)=>setDimensions((current)=>current.map((x)=>x.id===d.id?{...x,name:e.target.value}:x))} className={inputClass} placeholder="مثال: اللون"/>)}</div></div>
              <div className="space-y-3">{rows.map((row,index)=><div key={row.id} className="rounded-[14px] border border-black/[0.07] bg-white p-4"><div className="mb-3 flex items-center justify-between"><p className="text-[10px] font-semibold">تركيبة {index+1}</p><button type="button" onClick={()=>setRows((c)=>c.filter((x)=>x.id!==row.id))} className="text-red-500"><Trash2 size={14}/></button></div><div className="grid gap-3 md:grid-cols-3">{dimensions.map((d)=><label key={d.id}><span className={labelClass}>{d.name}</span><input value={row.values[d.id]??""} onChange={(e)=>updateRow(row.id,{values:{...row.values,[d.id]:e.target.value}})} className={inputClass} placeholder={d.name==="اللون"?"برتقالي":d.name==="السعة"?"256 GB":"القيمة"}/></label>)}</div><div className="mt-3 grid gap-3 md:grid-cols-4"><label><span className={labelClass}>SKU</span><input dir="ltr" value={row.sku} onChange={(e)=>updateRow(row.id,{sku:e.target.value})} className={`${inputClass} text-left`}/></label><label><span className={labelClass}>المخزون</span><input type="number" min="0" value={row.quantity} onChange={(e)=>updateRow(row.id,{quantity:e.target.value})} className={inputClass}/></label><label><span className={labelClass}>سعر مختلف (اختياري)</span><input type="number" min="0" value={row.priceOverride} onChange={(e)=>updateRow(row.id,{priceOverride:e.target.value})} className={inputClass}/></label><label><span className={labelClass}>تنبيه المخزون</span><input type="number" min="0" value={row.lowStockThreshold} onChange={(e)=>updateRow(row.id,{lowStockThreshold:e.target.value})} className={inputClass}/></label></div></div>)}<button type="button" onClick={addRow} className="flex h-12 w-full items-center justify-center gap-2 rounded-[12px] border border-dashed border-black/15 bg-white text-[10px] font-semibold"><Plus size={14}/> إضافة لون / سعة / مقاس جديد</button></div>
            </div>:<div className="mt-5 grid gap-4 md:grid-cols-2"><label><span className={labelClass}>الكمية الحالية</span><input type="number" min="0" value={form.quantity} onChange={(e)=>update("quantity",e.target.value)} className={inputClass}/></label><label><span className={labelClass}>تنبيه عند</span><input type="number" min="0" value={form.lowStockThreshold} onChange={(e)=>update("lowStockThreshold",e.target.value)} className={inputClass}/></label></div>}
          </section>

          <div className="my-7 border-t border-black/[0.07]"/>
          <section><p className="text-[11px] font-semibold">حالة المنتج</p><div className="mt-3 grid gap-3 md:grid-cols-2"><button type="button" onClick={()=>update("publishImmediately",true)} className={`rounded-[12px] border p-4 text-right ${form.publishImmediately?"border-[#080b14] bg-[#080b14] text-white":"bg-white"}`}><p className="text-[11px] font-semibold">نشر مباشرة</p><p className="mt-1 text-[9px] opacity-60">يظهر للعميل بعد الحفظ.</p></button><button type="button" onClick={()=>update("publishImmediately",false)} className={`rounded-[12px] border p-4 text-right ${!form.publishImmediately?"border-[#a77a43] bg-[#f4eadc]":"bg-white"}`}><p className="text-[11px] font-semibold">حفظ كمسودة</p><p className="mt-1 text-[9px] opacity-60">أكمله ثم انشره لاحقًا.</p></button></div></section>

          {error?<div className="mt-6 rounded-[11px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">{error}</div>:null}
          <div className="mt-7 flex items-center justify-between border-t border-black/[0.07] pt-5"><button type="button" onClick={close} className="h-11 px-3 text-[10px] text-black/42">إلغاء</button><button type="button" disabled={!valid||busy} onClick={()=>void submit()} className="inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#080b14] px-5 text-[11px] font-semibold text-white disabled:opacity-40">{busy?"جاري بناء المنتج...":"حفظ المنتج وكل خياراته"}<ArrowLeft size={15}/></button></div>
        </div>
      </div>
    </div>
  );
}
