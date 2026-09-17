import { ArrowRight, FileText, RefreshCw } from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";
import { getStorefrontContentPage, type StorefrontContentPage as StorefrontContentPageModel } from "../data/storefrontApi";

export function StorefrontContentPage() {
  const { storeSlug = "", pageSlug = "" } = useParams();
  const [page, setPage] = useState<StorefrontContentPageModel | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      if (!storeSlug.trim() || !pageSlug.trim()) { setError("رابط الصفحة غير صالح."); setLoading(false); return; }
      setLoading(true); setError(null);
      try {
        const result = await getStorefrontContentPage(storeSlug, pageSlug);
        if (!cancelled) { setPage(result); if (result.seoTitle) document.title = result.seoTitle; }
      } catch { if (!cancelled) setError("الصفحة غير موجودة أو غير منشورة."); }
      finally { if (!cancelled) setLoading(false); }
    }
    void load();
    return () => { cancelled = true; };
  }, [pageSlug, storeSlug]);

  if (loading) return <div dir="rtl" className="flex min-h-screen items-center justify-center bg-[#f6f4ee] text-[#111513]"><div className="flex items-center gap-3 text-[13px] text-black/55"><RefreshCw size={17} className="animate-spin" />جاري تحميل الصفحة...</div></div>;

  if (error || !page) return <div dir="rtl" className="flex min-h-screen items-center justify-center bg-[#f6f4ee] p-6 text-[#111513]"><div className="w-full max-w-[560px] rounded-[22px] border border-black/[0.08] bg-white p-8 text-center"><FileText size={28} className="mx-auto text-black/28" /><h1 className="mt-5 text-[24px] font-semibold">الصفحة غير متاحة</h1><p className="mt-3 text-[12px] leading-7 text-black/48">{error}</p><Link to={`/store/${encodeURIComponent(storeSlug)}`} className="mt-6 inline-flex h-11 items-center gap-2 rounded-[10px] bg-[#10130f] px-5 text-[11px] font-semibold text-white"><ArrowRight size={15} />العودة للمتجر</Link></div></div>;

  return <div dir="rtl" className="min-h-screen bg-[#f6f4ee] text-[#111513]"><header className="border-b border-black/[0.07] bg-white"><div className="mx-auto flex h-[72px] max-w-[1120px] items-center justify-between px-5"><Link to={`/store/${encodeURIComponent(storeSlug)}`} className="text-[18px] font-bold tracking-[-0.04em]">المتجر</Link><Link to={`/store/${encodeURIComponent(storeSlug)}`} className="flex items-center gap-2 text-[11px] font-semibold text-black/55 hover:text-black"><ArrowRight size={14} />الرئيسية</Link></div></header><main className="mx-auto max-w-[900px] px-5 py-14 md:py-20"><div className="rounded-[24px] border border-black/[0.07] bg-white px-6 py-9 md:px-12 md:py-12"><p className="text-[10px] font-semibold text-[#9d723d]">صفحة تعريفية</p><h1 className="mt-3 text-[clamp(2rem,4vw,3.8rem)] font-semibold tracking-[-0.045em]">{page.title}</h1><article className="mt-9 whitespace-pre-wrap text-[14px] leading-[2.1] text-black/70">{page.body}</article></div></main></div>;
}
