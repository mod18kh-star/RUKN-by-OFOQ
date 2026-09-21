import {
  ArrowRight,
  BadgeCheck,
  BarChart3,
  FileText,
  Globe2,
  MessageCircleMore,
  PackageCheck,
  RefreshCw,
  ShoppingBag,
  Star,
  Users,
} from "lucide-react";

import {
  useEffect,
  useMemo,
} from "react";

import {
  useQuery,
} from "@tanstack/react-query";

import {
  Link,
  useParams,
} from "react-router";

import {
  SmartImage,
} from "../components/SmartImage";

import {
  StorefrontHeader,
} from "../components/StorefrontHeader";

import {
  StorefrontFooter,
} from "../components/StorefrontFooter";

import { parseVisualContent } from "../config/visualContent";

import {
  createLiveStorefrontConfig,
} from "../config/liveStorefrontConfig";

import {
  EMPTY_STOREFRONT_CONTACT,
  getStorefrontContentPage,
  getStorefrontInfo,
  getStorefrontPageStatistics,
  getStorefrontStoreReviews,
  type StorefrontPageStatistics,
} from "../data/storefrontApi";

import {
  createThemeStyle,
  resolveStorefrontConfig,
} from "../theme/themeEngine";

import {
  getThemePreset,
} from "../theme/themePresets";

function ContentBody({ body }: { body: string }) {
  const blocks = useMemo(
    () =>
      body
        .split(/\n\s*\n/g)
        .map((block) => block.trim())
        .filter(Boolean),
    [body],
  );

  if (blocks.length === 0) return null;

  return (
    <div className="space-y-8">
      {blocks.map((block, index) => {
        const lines = block.split("\n").map((line) => line.trim()).filter(Boolean);
        const firstLine = lines[0] ?? "";
        const rest = lines.slice(1).join("\n");
        const looksLikeHeading = lines.length > 1 && firstLine.length <= 55;

        if (looksLikeHeading) {
          return (
            <section key={`${index}-${firstLine}`} className="space-y-3">
              <h2 className="text-[clamp(1.2rem,2.2vw,1.65rem)] font-semibold tracking-[-0.035em] text-[var(--store-ink)]">{firstLine}</h2>
              <p className="whitespace-pre-line text-[15px] leading-[2.05] text-[var(--store-ink-soft)]">{rest}</p>
            </section>
          );
        }

        return (
          <p key={`${index}-${firstLine}`} className={`${index === 0 ? "text-[17px] leading-[2.05] text-[var(--store-ink)]/78" : "text-[15px] leading-[2.05] text-[var(--store-ink-soft)]"} whitespace-pre-line`}>
            {block}
          </p>
        );
      })}
    </div>
  );
}

function formatNumber(value: number) {
  return new Intl.NumberFormat("en-US", { maximumFractionDigits: 1 }).format(value);
}

function StatisticsGrid({ statistics }: { statistics: StorefrontPageStatistics }) {
  const metrics = [
    statistics.customerCount !== null ? { key: "customers", label: "عميل", value: formatNumber(statistics.customerCount), icon: Users } : null,
    statistics.completedOrderCount !== null ? { key: "orders", label: "طلب مكتمل", value: formatNumber(statistics.completedOrderCount), icon: PackageCheck } : null,
    statistics.unitsSold !== null ? { key: "sold", label: "منتج مباع", value: formatNumber(statistics.unitsSold), icon: ShoppingBag } : null,
    statistics.averageRating !== null ? { key: "rating", label: "متوسط التقييم", value: `${formatNumber(statistics.averageRating)} / 5`, icon: Star } : null,
    statistics.reviewCount !== null ? { key: "reviews", label: "تقييم منشور", value: formatNumber(statistics.reviewCount), icon: MessageCircleMore } : null,
    statistics.countryCount !== null ? { key: "countries", label: "دولة وصلنا إليها", value: formatNumber(statistics.countryCount), icon: Globe2 } : null,
  ].filter((item): item is NonNullable<typeof item> => Boolean(item));

  if (metrics.length === 0) return null;

  return (
    <section className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      {metrics.map((metric) => {
        const Icon = metric.icon;
        return (
          <div key={metric.key} className="rounded-[22px] border border-black/[0.07] bg-[var(--store-surface)] p-6 shadow-[0_16px_45px_rgba(20,25,25,0.045)]">
            <div className="flex size-11 items-center justify-center rounded-[13px] bg-[var(--store-soft)] text-[var(--store-accent)]"><Icon size={19} /></div>
            <p dir="ltr" className="mt-7 text-right text-[clamp(2rem,5vw,3.6rem)] font-semibold leading-none tracking-[-0.055em] tabular-nums">{metric.value}</p>
            <p className="mt-3 text-[12px] font-medium text-[var(--store-ink-soft)]">{metric.label}</p>
          </div>
        );
      })}
    </section>
  );
}

export function StorefrontContentPage() {
  const { storeSlug = "", pageSlug = "" } = useParams();

  const storeQuery = useQuery({
    queryKey: ["storefront-runtime", storeSlug, "info"],
    queryFn: () => getStorefrontInfo(storeSlug),
    enabled: Boolean(storeSlug.trim()),
    retry: 1,
    staleTime: 5 * 60_000,
  });

  const pageQuery = useQuery({
    queryKey: ["storefront-runtime", storeSlug, "page", pageSlug],
    queryFn: () => getStorefrontContentPage(storeSlug, pageSlug),
    enabled: Boolean(storeSlug.trim() && pageSlug.trim()),
    retry: 1,
    staleTime: 60_000,
  });

  const page = pageQuery.data ?? null;
  const store = storeQuery.data ?? null;

  const reviewsQuery = useQuery({
    queryKey: ["storefront-runtime", storeSlug, "store-reviews"],
    queryFn: () => getStorefrontStoreReviews(storeSlug, 30),
    enabled: Boolean(storeSlug.trim() && page?.pageKind === "Reviews"),
    retry: 1,
    staleTime: 60_000,
  });

  const statisticsQuery = useQuery({
    queryKey: ["storefront-runtime", storeSlug, "page-statistics", pageSlug],
    queryFn: () => getStorefrontPageStatistics(storeSlug, pageSlug),
    enabled: Boolean(storeSlug.trim() && pageSlug.trim() && page?.pageKind === "Statistics"),
    retry: 1,
    staleTime: 60_000,
  });

  const config = useMemo(
    () => store ? resolveStorefrontConfig(createLiveStorefrontConfig(store)) : null,
    [store],
  );

  useEffect(() => {
    if (!page) return;
    document.title = page.seoTitle?.trim() ? page.seoTitle : page.title;

    if (page.seoDescription?.trim()) {
      let meta = document.querySelector<HTMLMetaElement>('meta[name="description"]');
      if (!meta) {
        meta = document.createElement("meta");
        meta.name = "description";
        document.head.append(meta);
      }
      meta.content = page.seoDescription;
    }
  }, [page]);

  if (storeQuery.isPending || pageQuery.isPending) {
    return (
      <div dir="rtl" className="flex min-h-screen items-center justify-center bg-[#f6f4ee] text-[#111513]">
        <div className="flex items-center gap-3 text-[13px] text-black/55"><RefreshCw size={17} className="animate-spin" />جاري تحميل الصفحة...</div>
      </div>
    );
  }

  if (storeQuery.isError || pageQuery.isError || !page || !config) {
    return (
      <div dir="rtl" className="flex min-h-screen items-center justify-center bg-[#f6f4ee] p-6 text-[#111513]">
        <div className="w-full max-w-[560px] rounded-[22px] border border-black/[0.08] bg-white p-8 text-center">
          <FileText size={28} className="mx-auto text-black/28" />
          <h1 className="mt-5 text-[24px] font-semibold">الصفحة غير متاحة</h1>
          <p className="mt-3 text-[12px] leading-7 text-black/48">الصفحة غير موجودة، غير منشورة، أو تعذر تحميل بيانات المتجر.</p>
          <Link to={`/store/${encodeURIComponent(storeSlug)}`} className="mt-6 inline-flex h-11 items-center gap-2 rounded-[10px] bg-[#10130f] px-5 text-[11px] font-semibold text-white"><ArrowRight size={15} />العودة للمتجر</Link>
        </div>
      </div>
    );
  }

  const theme = getThemePreset(config.themeId);
  const flagship = config.themeId === "mobile-flagship" || theme.experience === "signature";
  const smartMarket = config.themeId === "mobile-smart-market" || theme.experience === "market";
  const smartDataPending = (page.pageKind === "Reviews" && reviewsQuery.isPending) || (page.pageKind === "Statistics" && statisticsQuery.isPending);

  return (
    <div dir="rtl" className="min-h-screen bg-[var(--store-canvas)] text-[var(--store-ink)]" style={createThemeStyle(config)}>
      <StorefrontHeader storeSlug={storeSlug} storeName={config.storeName} logoUrl={config.logoUrl} announcement={config.announcement} themeId={config.themeId} />

      <main className="store-container py-10 md:py-16 lg:py-20">
        <div className="mb-8 flex items-center gap-2 text-[11px] font-medium text-[var(--store-muted)]">
          <Link to={`/store/${encodeURIComponent(storeSlug)}`} className="transition hover:text-[var(--store-ink)]">الرئيسية</Link><span>/</span><span>{page.title}</span>
        </div>

        <article className={`overflow-hidden border border-black/[0.07] ${flagship ? "rounded-[34px] bg-[var(--store-surface)] shadow-[0_28px_90px_rgba(23,22,18,0.07)]" : smartMarket ? "rounded-[22px] bg-white shadow-[0_20px_65px_rgba(26,42,69,0.07)]" : "rounded-[24px] bg-[var(--store-surface)]"}`}>
          {page.heroImageUrl ? (
            <div className="relative aspect-[16/6] min-h-[260px] overflow-hidden border-b border-black/[0.06] bg-[var(--store-soft)]">
              <SmartImage src={page.heroImageUrl} alt={page.title} priority className="absolute inset-0 h-full w-full object-cover" />
              <div className="absolute inset-0 bg-gradient-to-t from-black/30 via-black/5 to-transparent" />
            </div>
          ) : null}

          <div className={`border-b border-black/[0.06] ${flagship ? "px-7 py-12 md:px-14 md:py-16 lg:px-20" : "px-7 py-10 md:px-12 md:py-14"}`}>
            <p className="text-[11px] font-semibold tracking-[0.06em] text-[var(--store-accent)]">
              {page.pageKind === "Reviews" ? "تجارب موثّقة" : page.pageKind === "Statistics" ? "متجرنا بالأرقام" : flagship ? "قصتنا وتفاصيلنا" : "معلومات المتجر"}
            </p>
            <h1 className="mt-4 max-w-[900px] text-[clamp(2.5rem,6vw,5.4rem)] font-semibold leading-[1.08] tracking-[-0.055em]">{page.title}</h1>
            {page.seoDescription ? <p className="mt-5 max-w-[760px] text-[14px] leading-8 text-[var(--store-ink-soft)]">{page.seoDescription}</p> : null}
          </div>

          <div className={`${flagship ? "px-7 py-12 md:px-14 md:py-16 lg:px-20" : "px-7 py-10 md:px-12 md:py-14"} mx-auto max-w-[1120px] space-y-12`}>
            <ContentBody body={page.body} />

            {smartDataPending ? (
              <div className="flex min-h-[180px] items-center justify-center rounded-[20px] border border-black/[0.06] bg-[var(--store-soft)] text-[12px] text-[var(--store-muted)]"><RefreshCw size={16} className="ml-2 animate-spin" />جاري تحميل بيانات المتجر...</div>
            ) : null}

            {page.pageKind === "Statistics" && statisticsQuery.data ? <StatisticsGrid statistics={statisticsQuery.data} /> : null}

            {page.pageKind === "Reviews" && reviewsQuery.data ? (
              <section>
                <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
                  <div><p className="text-[12px] font-semibold text-[var(--store-accent)]">{formatNumber(reviewsQuery.data.averageRating)} / 5</p><h2 className="mt-2 text-[clamp(1.8rem,4vw,3rem)] font-semibold tracking-[-0.04em]">ما يقوله عملاؤنا</h2></div>
                  <p className="text-[11px] text-[var(--store-muted)]"><span dir="ltr" className="tabular-nums">{formatNumber(reviewsQuery.data.reviewCount)}</span> تقييم منشور</p>
                </div>

                {reviewsQuery.data.reviews.length === 0 ? (
                  <div className="rounded-[20px] border border-black/[0.07] bg-[var(--store-soft)] p-8 text-center text-[12px] text-[var(--store-muted)]">لا توجد تقييمات منشورة حتى الآن.</div>
                ) : (
                  <div className="grid gap-4 md:grid-cols-2">
                    {reviewsQuery.data.reviews.map((review) => (
                      <article key={review.id} className="rounded-[22px] border border-black/[0.07] bg-[var(--store-surface)] p-6 shadow-[0_16px_45px_rgba(20,25,25,0.045)]">
                        <div className="flex items-start justify-between gap-4">
                          <div>
                            <div className="flex items-center gap-1 text-[var(--store-accent)]">{Array.from({ length: 5 }).map((_, index) => <Star key={index} size={15} fill={index < review.rating ? "currentColor" : "none"} className={index < review.rating ? "" : "opacity-25"} />)}</div>
                            <Link to={`/store/${encodeURIComponent(storeSlug)}/products/${encodeURIComponent(review.productSlug)}`} className="mt-3 block text-[13px] font-semibold hover:underline">{review.productName}</Link>
                          </div>
                          {review.isVerifiedPurchase ? <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2.5 py-1 text-[9px] font-semibold text-emerald-700"><BadgeCheck size={12} />شراء موثّق</span> : null}
                        </div>
                        {review.body ? <p className="mt-5 whitespace-pre-line text-[13px] leading-8 text-[var(--store-ink-soft)]">{review.body}</p> : <p className="mt-5 text-[12px] text-[var(--store-muted)]">قيّم العميل المنتج بدون تعليق مكتوب.</p>}
                        {review.merchantReply ? <div className="mt-5 rounded-[14px] bg-[var(--store-soft)] p-4"><p className="text-[9px] font-semibold text-[var(--store-accent)]">رد المتجر</p><p className="mt-2 text-[11px] leading-6 text-[var(--store-ink-soft)]">{review.merchantReply}</p></div> : null}
                      </article>
                    ))}
                  </div>
                )}
              </section>
            ) : null}

            {page.pageKind === "Statistics" && statisticsQuery.isError ? <div className="rounded-[16px] border border-black/[0.07] bg-[var(--store-soft)] p-5 text-[11px] text-[var(--store-muted)]"><BarChart3 size={17} className="mb-2" />تعذر تحميل أرقام المتجر حاليًا.</div> : null}
            {page.pageKind === "Reviews" && reviewsQuery.isError ? <div className="rounded-[16px] border border-black/[0.07] bg-[var(--store-soft)] p-5 text-[11px] text-[var(--store-muted)]"><MessageCircleMore size={17} className="mb-2" />تعذر تحميل تقييمات المتجر حاليًا.</div> : null}
          </div>
        </article>
      </main>

      <StorefrontFooter
        storeSlug={storeSlug}
        storeName={config.storeName}
        logoUrl={config.logoUrl}
        themeId={config.themeId}
        contact={storeQuery.data?.contact ?? EMPTY_STOREFRONT_CONTACT}
        description={parseVisualContent(storeQuery.data?.presentation.visualContentJson).footerDescription}
      />
    </div>
  );
}
