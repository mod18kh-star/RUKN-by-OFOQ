import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  ArrowLeft,
  Building2,
  CirclePause,
  CirclePlay,
  Store,
} from "lucide-react";

import { Link } from "react-router";

import {
  getPlatformStores,
  type PlatformStoreSummary,
} from "./platformApi";

export function PlatformDashboardPage() {
  const [stores, setStores] =
    useState<PlatformStoreSummary[]>([]);

  const [loading, setLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    const timer = window.setTimeout(
      () => {
        void getPlatformStores()
          .then(setStores)
          .catch((exception: unknown) => {
            setError(
              exception instanceof Error
                ? exception.message
                : "تعذر تحميل بيانات المنصة.",
            );
          })
          .finally(() => {
            setLoading(false);
          });
      },
      0,
    );

    return () => {
      window.clearTimeout(timer);
    };
  }, []);

  const counts = useMemo(
    () => ({
      total: stores.length,
      active: stores.filter(
        (store) =>
          store.status === "Active",
      ).length,
      draft: stores.filter(
        (store) =>
          store.status === "Draft",
      ).length,
      suspended: stores.filter(
        (store) =>
          store.status === "Suspended",
      ).length,
    }),
    [stores],
  );

  return (
    <div className="mx-auto max-w-[1420px] pb-8">
      <section className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-[9px] font-semibold tracking-[0.05em] text-[#91672f]">
            RUKN PLATFORM
          </p>
          <h1 className="mt-2 text-[31px] font-semibold tracking-[-0.045em] md:text-[36px]">
            نظرة عامة
          </h1>
          <p className="mt-3 max-w-[660px] text-[11px] leading-7 text-black/44">
            صورة تشغيلية سريعة عن المتاجر الموجودة على ركن وحالتها الحالية.
          </p>
        </div>

        <Link
          to="/platform/stores"
          className="inline-flex h-11 w-fit items-center gap-2 rounded-[10px] bg-[#0a0d15] px-5 text-[10px] font-semibold text-white transition hover:bg-black"
        >
          عرض المتاجر
          <ArrowLeft size={13} />
        </Link>
      </section>

      {error ? (
        <div className="mt-5 rounded-[13px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">
          {error}
        </div>
      ) : null}

      <div className="mt-7 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <Metric
          title="كل المتاجر"
          value={loading ? "—" : counts.total}
          icon={Store}
          detail="إجمالي المتاجر المسجلة"
        />
        <Metric
          title="فعالة"
          value={loading ? "—" : counts.active}
          icon={CirclePlay}
          detail="جاهزة للتشغيل"
        />
        <Metric
          title="قيد الإعداد"
          value={loading ? "—" : counts.draft}
          icon={Building2}
          detail="لم يكتمل تشغيلها"
        />
        <Metric
          title="موقوفة"
          value={loading ? "—" : counts.suspended}
          icon={CirclePause}
          detail="موقوفة إداريًا"
        />
      </div>

      <section className="mt-5 overflow-hidden rounded-[20px] border border-black/[0.065] bg-white shadow-[0_18px_60px_rgba(20,18,13,0.035)]">
        <div className="flex items-center justify-between gap-4 border-b border-black/[0.06] px-6 py-5">
          <div>
            <p className="text-[9px] font-semibold text-[#91672f]">
              RECENT STORES
            </p>
            <h2 className="mt-1.5 text-[15px] font-semibold">
              أحدث المتاجر
            </h2>
          </div>

          <Link
            to="/platform/stores"
            className="text-[9px] font-semibold text-[#855f2d]"
          >
            عرض الكل
          </Link>
        </div>

        {loading ? (
          <div className="p-12 text-center text-[10px] text-black/38">
            جاري تحميل المتاجر
          </div>
        ) : stores.length === 0 ? (
          <div className="p-12 text-center text-[10px] text-black/38">
            لا يوجد متاجر حتى الآن
          </div>
        ) : (
          <div className="divide-y divide-black/[0.055]">
            {stores.slice(0, 6).map(
              (store) => (
                <Link
                  key={store.tenantId}
                  to={`/platform/stores/${store.tenantId}`}
                  className="grid gap-3 px-6 py-4 transition hover:bg-[#fbfaf7] sm:grid-cols-[1.25fr_1fr_.7fr_auto] sm:items-center"
                >
                  <div className="min-w-0">
                    <p className="truncate text-[11px] font-semibold">
                      {store.name}
                    </p>
                    <p
                      dir="ltr"
                      className="mt-1 truncate text-left text-[8px] text-black/34"
                    >
                      {store.slug}.ofoq.store
                    </p>
                  </div>

                  <p
                    dir="ltr"
                    className="truncate text-left text-[9px] text-black/44"
                  >
                    {store.ownerEmail ?? "—"}
                  </p>

                  <p className="text-[9px] text-black/44">
                    {store.primaryVerticalCode ?? "غير محدد"}
                  </p>

                  <StatusBadge value={store.status} />
                </Link>
              ),
            )}
          </div>
        )}
      </section>
    </div>
  );
}

function Metric({
  title,
  value,
  icon: Icon,
  detail,
}: {
  title: string;
  value: number | string;
  icon: typeof Store;
  detail: string;
}) {
  return (
    <div className="rounded-[18px] border border-black/[0.065] bg-[#fbfaf7] p-5 shadow-[0_14px_45px_rgba(20,18,13,0.025)]">
      <div className="flex items-center justify-between gap-4">
        <p className="text-[9px] font-semibold text-black/42">
          {title}
        </p>
        <div className="flex size-8 items-center justify-center rounded-[8px] bg-[#eee4d4] text-[#8b632f]">
          <Icon size={14} />
        </div>
      </div>
      <p className="mt-5 text-[27px] font-semibold tracking-[-0.05em]">
        {value}
      </p>
      <p className="mt-1 text-[8px] text-black/30">
        {detail}
      </p>
    </div>
  );
}

export function StatusBadge({
  value,
}: {
  value: string;
}) {
  const className =
    value === "Active"
      ? "border-emerald-200 bg-emerald-50 text-emerald-700"
      : value === "Suspended"
        ? "border-red-200 bg-red-50 text-red-700"
        : "border-amber-200 bg-amber-50 text-amber-700";

  const label =
    value === "Active"
      ? "فعال"
      : value === "Suspended"
        ? "موقوف"
        : "قيد الإعداد";

  return (
    <span
      className={`inline-flex w-fit items-center rounded-full border px-2.5 py-1 text-[8px] font-semibold ${className}`}
    >
      {label}
    </span>
  );
}
