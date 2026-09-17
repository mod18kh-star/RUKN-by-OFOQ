import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  ChevronLeft,
  Search,
  Store,
} from "lucide-react";

import { Link } from "react-router";

import {
  StatusBadge,
} from "./PlatformDashboardPage";

import {
  getPlatformStores,
  type PlatformStoreSummary,
} from "./platformApi";

type StatusFilter =
  | "all"
  | "Active"
  | "Draft"
  | "Suspended";

export function PlatformStoresPage() {
  const [stores, setStores] =
    useState<PlatformStoreSummary[]>([]);

  const [query, setQuery] =
    useState("");

  const [statusFilter, setStatusFilter] =
    useState<StatusFilter>("all");

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
                : "تعذر تحميل المتاجر.",
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

  const filtered = useMemo(() => {
    const normalized =
      query.trim().toLowerCase();

    return stores.filter((store) => {
      if (
        statusFilter !== "all" &&
        store.status !== statusFilter
      ) {
        return false;
      }

      if (!normalized) {
        return true;
      }

      return [
        store.name,
        store.slug,
        store.ownerEmail ?? "",
        store.primaryVertical ?? "",
        store.primaryVerticalCode ?? "",
      ].some((value) =>
        value
          .toLowerCase()
          .includes(normalized),
      );
    });
  }, [query, statusFilter, stores]);

  return (
    <div className="mx-auto max-w-[1420px] pb-8">
      <div>
        <p className="text-[9px] font-semibold tracking-[0.05em] text-[#91672f]">
          STORE OPERATIONS
        </p>
        <h1 className="mt-2 text-[31px] font-semibold tracking-[-0.045em] md:text-[36px]">
          المتاجر
        </h1>
        <p className="mt-3 max-w-[620px] text-[11px] leading-7 text-black/44">
          الوصول إلى كل متجر، معرفة حالته، وفتح ملفه التشغيلي من مكان واحد.
        </p>
      </div>

      <section className="mt-7 overflow-hidden rounded-[20px] border border-black/[0.065] bg-white shadow-[0_18px_60px_rgba(20,18,13,0.035)]">
        <div className="flex flex-col gap-3 border-b border-black/[0.06] p-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="relative w-full lg:max-w-[460px]">
            <Search
              size={14}
              className="absolute right-3.5 top-1/2 -translate-y-1/2 text-black/28"
            />
            <input
              value={query}
              onChange={(event) =>
                setQuery(event.target.value)
              }
              placeholder="ابحث بالاسم أو الرابط أو المالك أو النشاط"
              className="h-10 w-full rounded-[9px] border border-black/[0.08] bg-[#faf9f6] pr-10 pl-3 text-[10px] outline-none transition placeholder:text-black/25 focus:border-black/20"
            />
          </div>

          <div className="flex w-fit max-w-full gap-1 overflow-x-auto rounded-[9px] border border-black/[0.065] bg-[#f7f5f0] p-1">
            <FilterButton
              active={statusFilter === "all"}
              onClick={() =>
                setStatusFilter("all")
              }
            >
              الكل
            </FilterButton>
            <FilterButton
              active={statusFilter === "Active"}
              onClick={() =>
                setStatusFilter("Active")
              }
            >
              الفعالة
            </FilterButton>
            <FilterButton
              active={statusFilter === "Draft"}
              onClick={() =>
                setStatusFilter("Draft")
              }
            >
              قيد الإعداد
            </FilterButton>
            <FilterButton
              active={statusFilter === "Suspended"}
              onClick={() =>
                setStatusFilter("Suspended")
              }
            >
              الموقوفة
            </FilterButton>
          </div>
        </div>

        <div className="border-b border-black/[0.055] px-5 py-3 text-[8px] font-medium text-black/32">
          {loading
            ? "جاري التحميل"
            : `${filtered.length} متجر`}
        </div>

        {error ? (
          <div className="m-5 rounded-[11px] border border-red-200 bg-red-50 p-4 text-[10px] text-red-700">
            {error}
          </div>
        ) : null}

        {loading ? (
          <div className="p-14 text-center text-[10px] text-black/38">
            جاري تحميل المتاجر
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex min-h-[300px] items-center justify-center px-5">
            <div className="text-center">
              <Store
                size={22}
                className="mx-auto text-black/22"
              />
              <p className="mt-4 text-[11px] font-semibold">
                لا توجد نتائج
              </p>
              <p className="mt-1 text-[9px] text-black/34">
                غيّر البحث أو الفلتر الحالي
              </p>
            </div>
          </div>
        ) : (
          <div className="divide-y divide-black/[0.055]">
            {filtered.map((store) => (
              <Link
                key={store.tenantId}
                to={`/platform/stores/${store.tenantId}`}
                className="group grid gap-4 px-5 py-4 transition hover:bg-[#fbfaf7] md:grid-cols-[1.2fr_1fr_.75fr_auto_auto] md:items-center"
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

                <div>
                  <p className="text-[9px] font-medium text-black/55">
                    {store.primaryVerticalCode ?? "غير محدد"}
                  </p>
                  <p className="mt-1 text-[8px] text-black/28">
                    النشاط الأساسي
                  </p>
                </div>

                <StatusBadge value={store.status} />

                <div className="flex items-center gap-3 md:justify-end">
                  <span className="hidden text-[8px] text-black/30 xl:inline">
                    {new Intl.DateTimeFormat(
                      "ar-SA",
                      {
                        dateStyle: "medium",
                      },
                    ).format(
                      new Date(
                        store.createdAtUtc,
                      ),
                    )}
                  </span>
                  <span className="flex size-8 items-center justify-center rounded-full border border-black/[0.06] text-black/28 transition group-hover:border-black/12 group-hover:text-black/62">
                    <ChevronLeft size={13} />
                  </span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function FilterButton({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        "h-8 shrink-0 rounded-[7px] px-3 text-[8px] font-semibold transition",
        active
          ? "bg-white text-black shadow-[0_3px_12px_rgba(0,0,0,0.05)]"
          : "text-black/40 hover:text-black/70",
      ].join(" ")}
    >
      {children}
    </button>
  );
}
