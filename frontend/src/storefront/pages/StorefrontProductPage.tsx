import { ArrowRight, Check, PackageCheck, ShieldCheck, ShoppingBag } from "lucide-react";
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router";
import { getStorefrontProduct, type StorefrontVariant } from "../data/storefrontApi";
import { SmartImage } from "../components/SmartImage";

function money(amount: number, currency: string) {
  try { return new Intl.NumberFormat("ar-SA", { style: "currency", currency, maximumFractionDigits: Number.isInteger(amount) ? 0 : 2 }).format(amount); }
  catch { return `${amount.toLocaleString("ar-SA")} ${currency}`; }
}

export function StorefrontProductPage() {
  const { storeSlug = "", productSlug = "" } = useParams<{storeSlug:string;productSlug:string}>();
  const query = useQuery({ queryKey:["storefront-product",storeSlug,productSlug], queryFn:()=>getStorefrontProduct(storeSlug,productSlug), enabled:Boolean(storeSlug&&productSlug), retry:1 });
  const product = query.data;
  const saleVariants = useMemo(()=>product?.variants.filter((v)=>!v.isDefault)??[],[product]);
  const defaultVariant = product?.variants.find((v)=>v.isDefault) ?? product?.variants[0] ?? null;
  const [selectedId,setSelectedId]=useState<string | null>(null);
  const selected: StorefrontVariant | null = saleVariants.find((v)=>v.variantId===selectedId) ?? (saleVariants[0] ?? defaultVariant);
  const images = product?.images.length ? product.images : product ? [{imageId:"primary",url:product.primaryImageUrl,altText:product.primaryImageAltText,sortOrder:0,isPrimary:true}] : [];
  const [imageIndex,setImageIndex]=useState(0);

  if (query.isPending) return <div dir="rtl" className="mx-auto max-w-[1300px] px-5 py-24 text-center text-sm text-black/45">جاري تحميل المنتج...</div>;
  if (query.isError || !product) return <div dir="rtl" className="mx-auto max-w-[1000px] px-5 py-24 text-center"><p className="text-xl font-semibold">تعذر فتح المنتج</p><Link to={`/store/${storeSlug}`} className="mt-5 inline-flex items-center gap-2 text-sm underline">العودة للمتجر</Link></div>;

  const activeImage = images[Math.min(imageIndex,images.length-1)]?.url ?? product.primaryImageUrl;
  return <main dir="rtl" className="min-h-screen bg-[#f7f6f2] text-[#171817]">
    <div className="mx-auto max-w-[1450px] px-5 py-7 md:px-8">
      <Link to={`/store/${encodeURIComponent(storeSlug)}`} className="inline-flex items-center gap-2 text-[11px] text-black/50"><ArrowRight size={14}/> العودة للمتجر</Link>
      <div className="mt-7 grid gap-8 lg:grid-cols-[1.05fr_.95fr]">
        <section><div className="overflow-hidden bg-white"><SmartImage src={activeImage} alt={product.name} className="aspect-square w-full object-contain" sizes="(max-width:1024px) 100vw, 55vw"/></div>{images.length>1?<div className="mt-3 grid grid-cols-5 gap-2">{images.map((image,index)=><button key={image.imageId} type="button" onClick={()=>setImageIndex(index)} className={`overflow-hidden border bg-white ${imageIndex===index?"border-black":"border-black/10"}`}><SmartImage src={image.url} alt={image.altText??product.name} className="aspect-square w-full object-cover"/></button>)}</div>:null}</section>
        <section className="lg:sticky lg:top-8 lg:self-start"><p className="text-[11px] text-black/45">{product.categoryName??"منتج"}</p><h1 className="mt-3 text-[clamp(2.1rem,4vw,4.5rem)] font-semibold leading-[1.05] tracking-[-0.05em]">{product.name}</h1>{product.description?<p className="mt-5 max-w-2xl text-[14px] leading-8 text-black/60">{product.description}</p>:null}<div className="mt-6 flex items-baseline gap-3"><span className="text-2xl font-semibold">{money(selected?.price??product.price,selected?.currency??product.currency)}</span>{product.compareAtPrice?<span className="text-sm text-black/35 line-through">{money(product.compareAtPrice,product.currency)}</span>:null}</div>
          {saleVariants.length?<div className="mt-8"><p className="text-[11px] font-semibold">الخيارات المتوفرة</p><div className="mt-3 flex flex-wrap gap-2">{saleVariants.map((variant)=><button key={variant.variantId} type="button" disabled={!variant.availableForSale} onClick={()=>setSelectedId(variant.variantId)} className={`rounded-[10px] border px-4 py-3 text-[11px] font-semibold ${selected?.variantId===variant.variantId?"border-black bg-black text-white":"border-black/10 bg-white"} disabled:opacity-35`}>{variant.name}{variant.trackInventory&&variant.quantity!==null?<span className="mr-2 text-[9px] opacity-60">({variant.quantity})</span>:null}</button>)}</div></div>:null}
          <div className="mt-7 grid gap-2 sm:grid-cols-2"><button type="button" disabled className="inline-flex h-13 items-center justify-center gap-2 rounded-[10px] bg-black px-5 text-[12px] font-semibold text-white disabled:opacity-60"><ShoppingBag size={16}/> إضافة للسلة</button><button type="button" disabled className="h-13 rounded-[10px] border border-black/15 bg-white px-5 text-[12px] font-semibold disabled:opacity-60">اشتري الآن</button></div><p className="mt-2 text-[9px] text-black/35">سيتم ربط أزرار الشراء مع جلسة العميل والـCheckout في قسم التجارة، بدون زر وهمي.</p>
          <div className="mt-7 grid grid-cols-3 gap-2 text-center text-[9px] text-black/55"><div className="border border-black/8 bg-white p-3"><PackageCheck className="mx-auto mb-2" size={17}/> مخزون واضح</div><div className="border border-black/8 bg-white p-3"><ShieldCheck className="mx-auto mb-2" size={17}/> ضمان ومواصفات</div><div className="border border-black/8 bg-white p-3"><Check className="mx-auto mb-2" size={17}/> خيارات متعددة</div></div>
        </section>
      </div>
      {product.attributes.length?<section className="mt-14 border-t border-black/10 pt-10"><h2 className="text-2xl font-semibold">المواصفات</h2><div className="mt-5 grid gap-px overflow-hidden border border-black/10 bg-black/10 md:grid-cols-2">{product.attributes.map((attribute)=><div key={attribute.key} className="flex items-center justify-between gap-5 bg-white px-5 py-4"><span className="text-[11px] text-black/45">{attribute.label}</span><span className="text-[12px] font-semibold">{attribute.value}</span></div>)}</div></section>:null}
    </div>
  </main>;
}
