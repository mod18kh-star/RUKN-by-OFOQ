import { useQuery } from "@tanstack/react-query";
import { Star } from "lucide-react";
import { getStorefrontProductReviews } from "../data/storefrontApi";

export function StorefrontProductReviews({ storeSlug, productSlug }: { storeSlug: string; productSlug: string }) {
  const query = useQuery({
    queryKey: ["storefront-product-reviews", storeSlug, productSlug],
    queryFn: () => getStorefrontProductReviews(storeSlug, productSlug),
    enabled: Boolean(storeSlug && productSlug),
    staleTime: 60_000,
    retry: 1,
  });
  const data = query.data;
  return <section dir="rtl" className="mt-12 border-t border-black/10 pt-8" aria-label="تقييمات المشترين">
    <div className="flex flex-wrap items-end justify-between gap-3">
      <div><p className="text-xs font-semibold text-[var(--store-muted)]">آراء المشترين</p>
        <h2 className="mt-2 text-2xl font-semibold">تقييمات المنتج</h2></div>
      {data && data.reviewCount > 0 && <p className="text-sm font-medium text-[var(--store-ink)]">
        {data.averageRating.toFixed(1)} من 5 · {data.reviewCount.toLocaleString("ar-SA")} تقييم
      </p>}
    </div>
    {query.isPending && <p className="mt-4 text-sm text-[var(--store-muted)]">جار تحميل التقييمات...</p>}
    {query.isError && <p className="mt-4 text-sm text-[var(--store-muted)]">تعذر عرض التقييمات حاليًا.</p>}
    {data && data.reviewCount === 0 && <p className="mt-4 text-sm text-[var(--store-muted)]">لا توجد تقييمات منشورة لهذا المنتج بعد.</p>}
    {data && data.reviews.length > 0 && <div className="mt-5 grid gap-3 md:grid-cols-2">
      {data.reviews.map(review => <article key={review.id} className="rounded-xl border border-black/10 bg-white p-4 text-[#23362c]">
        <div className="flex items-center justify-between gap-2">
          <span className="flex items-center gap-1" aria-label={`${review.rating} من 5 نجوم`}>
            {[1, 2, 3, 4, 5].map(value => <Star key={value} size={16} color={value <= review.rating ? "#AC782C" : "#9AA49C"}
              fill={value <= review.rating ? "#AC782C" : "none"} />)}
          </span>
          {review.isVerifiedPurchase && <span className="rounded-full bg-emerald-50 px-2 py-1 text-xs text-emerald-800">شراء موثّق</span>}
        </div>
        {review.body && <p className="mt-3 whitespace-pre-wrap break-words text-sm leading-7">{review.body}</p>}
        {review.merchantReply && <div className="mt-3 rounded-lg bg-[#f3f6f3] p-3 text-sm"><strong>رد المتجر</strong><p className="mt-1 whitespace-pre-wrap break-words">{review.merchantReply}</p></div>}
        <p className="mt-3 text-xs text-[#66766C]">{new Date(review.createdAtUtc).toLocaleDateString("ar-SA")}</p>
      </article>)}
    </div>}
  </section>;
}
