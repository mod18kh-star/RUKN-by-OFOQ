import {
  AlertTriangle,
  ArrowUpLeft,
  ChevronLeft,
  Clock3,
  PackageCheck,
  RefreshCw,
  ShoppingBag,
  Store,
  TrendingUp,
} from "lucide-react";

import {
  useCallback,
  useEffect,
  useState,
} from "react";

import {
  Link,
} from "react-router";

import {
  AdminOperationApiError,
  getMerchantDashboardSummary,
  type MerchantOperationsDashboard,
} from "./dashboard/adminDashboardApi";

import {
  readAdminStore,
} from "./store-setup/storeSetupStorage";

function formatMoney(
  value: number,
  currency: string | null,
) {
  if (!currency) {
    return value.toLocaleString(
      "ar",
    );
  }

  try {
    return new Intl.NumberFormat(
      "ar",
      {
        style: "currency",
        currency,
        maximumFractionDigits: 0,
      },
    ).format(value);
  } catch {
    return `${value.toLocaleString(
      "ar",
    )} ${currency}`;
  }
}

function orderStatusLabel(
  status: string,
  fulfillmentStatus: string,
) {
  const fulfillment =
    fulfillmentStatus.toLowerCase();

  const orderStatus =
    status.toLowerCase();

  if (
    orderStatus === "cancelled" ||
    fulfillment === "cancelled"
  ) {
    return "ملغي";
  }

  if (
    fulfillment === "delivered" ||
    fulfillment === "fulfilled"
  ) {
    return "تم التوصيل";
  }

  if (
    fulfillment === "intransit"
  ) {
    return "قيد التوصيل";
  }

  if (
    fulfillment === "shipped"
  ) {
    return "تم الشحن";
  }

  if (
    fulfillment ===
    "readytoship"
  ) {
    return "جاهز للشحن";
  }

  if (
    fulfillment === "processing" ||
    orderStatus === "processing"
  ) {
    return "قيد التجهيز";
  }

  if (orderStatus === "paid") {
    return "مدفوع";
  }

  return "جديد";
}

export function AdminDashboardPage() {
  const store =
    readAdminStore();

  const [summary, setSummary] =
    useState<MerchantOperationsDashboard | null>(
      null,
    );

  const [loading, setLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(
      null,
    );

  const load =
    useCallback(
      async () => {
        if (!store?.tenantId) {
          setLoading(false);
          setError(
            "لم يتم العثور على متجر مرتبط بالحساب الحالي.",
          );
          return;
        }

        setLoading(true);
        setError(null);

        try {
          const result =
            await getMerchantDashboardSummary(
              store.tenantId,
              8,
            );

          setSummary(result);
        } catch (caught) {
          if (
            caught instanceof
              AdminOperationApiError &&
            caught.status === 403
          ) {
            setError(
              "الجلسة الحالية لا تحقق متطلبات الأمان للإدارة. سجل الدخول بكلمة المرور وأكمل التحقق بخطوتين.",
            );
          } else {
            setError(
              caught instanceof Error
                ? caught.message
                : "تعذر تحميل لوحة التشغيل.",
            );
          }
        } finally {
          setLoading(false);
        }
      },
      [store?.tenantId],
    );

  useEffect(() => {
    const timer =
      window.setTimeout(
        () => {
          void load();
        },
        0,
      );

    return () =>
      window.clearTimeout(
        timer,
      );
  }, [load]);

  const readiness =
    summary?.readiness.percentage ??
    0;

  const storeStatus =
    (
      summary?.readiness
        .storeStatus ??
      store?.status ??
      "Draft"
    ).toLowerCase();

  const isActive =
    storeStatus === "active";

  const isSuspended =
    storeStatus === "suspended";

  const dateLabel =
    new Intl.DateTimeFormat(
      "ar-SA",
      {
        weekday: "long",
        day: "numeric",
        month: "long",
      },
    ).format(new Date());

  return (
    <div className="mx-auto max-w-[1320px] pb-12">
      <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-[10px] text-black/38">
            {dateLabel}
          </p>

          <h1 className="mt-2 text-[32px] font-semibold tracking-[-0.045em] text-[#0b0f0d]">
            الرئيسية
          </h1>

          <p className="mt-2 max-w-[620px] text-[11px] leading-6 text-black/45">
            بيانات تشغيلية مباشرة من الخادم: الجاهزية والطلبات والمخزون والسلات المتروكة.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() =>
              void load()
            }
            disabled={loading}
            className="inline-flex size-10 items-center justify-center rounded-[10px] border border-black/[0.09] bg-white text-black/45 disabled:opacity-40"
            aria-label="تحديث"
          >
            <RefreshCw
              size={14}
              className={
                loading
                  ? "animate-spin"
                  : ""
              }
            />
          </button>

          {store?.slug ? (
            <Link
              to={`/store/${store.slug}`}
              className="inline-flex h-10 items-center justify-center gap-2 rounded-[10px] border border-black/[0.09] bg-white px-4 text-[10px] font-semibold transition hover:border-black/20"
            >
              فتح المتجر
              <ArrowUpLeft
                size={14}
              />
            </Link>
          ) : null}
        </div>
      </div>

      <section className="mt-7 overflow-hidden rounded-[18px] border border-black/[0.07] bg-[#101512] text-white shadow-[0_18px_50px_rgba(14,19,16,0.08)]">
        <div className="grid gap-8 p-6 md:grid-cols-[1fr_320px] md:p-7">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <span className="rounded-full border border-white/12 bg-white/[0.06] px-3 py-1 text-[9px] font-semibold text-white/70">
                جاهزية المتجر
              </span>

              <span
                className={`rounded-full px-3 py-1 text-[9px] font-semibold ${
                  isSuspended
                    ? "bg-red-400/15 text-red-200"
                    : isActive
                      ? "bg-[#d7b174]/15 text-[#ead3aa]"
                      : "bg-white/[0.08] text-white/65"
                }`}
              >
                {isSuspended
                  ? "المتجر موقوف"
                  : isActive
                    ? "المتجر فعال"
                    : "قيد الإعداد"}
              </span>
            </div>

            <div className="mt-6 flex items-end gap-3">
              <span className="text-[52px] font-semibold leading-none tracking-[-0.065em]">
                {loading
                  ? "—"
                  : `${readiness}%`}
              </span>

              <p className="mb-1 max-w-[420px] text-[10px] leading-5 text-white/45">
                {summary?.readiness.state ??
                  "يتم احتساب الجاهزية من بيانات المتجر الحقيقية على الخادم."}
              </p>
            </div>

            <div className="mt-6 h-1.5 overflow-hidden rounded-full bg-white/10">
              <div
                className="h-full rounded-full bg-[#d7b174] transition-[width] duration-500"
                style={{
                  width:
                    `${readiness}%`,
                }}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-2">
            <CompactStat
              label="طلبات مفتوحة"
              value={
                summary?.openOrders ??
                0
              }
            />
            <CompactStat
              label="بانتظار الإجراء"
              value={
                summary?.pendingOrders ??
                0
              }
            />
            <CompactStat
              label="مخزون منخفض"
              value={
                summary?.lowStockVariants ??
                0
              }
            />
            <CompactStat
              label="سلات متروكة"
              value={
                summary?.abandonedCarts ??
                0
              }
            />
          </div>
        </div>
      </section>

      {error ? (
        <div className="mt-4 flex items-start gap-3 rounded-[13px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] leading-5 text-red-700">
          <AlertTriangle
            size={15}
            className="mt-0.5 shrink-0"
          />
          <div className="flex-1">
            {error}
          </div>
          <button
            type="button"
            onClick={() =>
              void load()
            }
            className="shrink-0 font-semibold underline underline-offset-4"
          >
            إعادة المحاولة
          </button>
        </div>
      ) : null}

      <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          icon={ShoppingBag}
          label="الطلبات المفتوحة"
          value={
            loading
              ? "—"
              : String(
                  summary?.openOrders ??
                    0,
                )
          }
          detail="طلبات تحتاج متابعة تشغيلية"
        />

        <MetricCard
          icon={Clock3}
          label="مدفوع بانتظار التأكيد"
          value={
            loading
              ? "—"
              : String(
                  summary
                    ?.paidOrdersAwaitingConfirmation ??
                    0,
                )
          }
          detail="من بيانات الدفع والطلب الفعلية"
        />

        <MetricCard
          icon={PackageCheck}
          label="تنبيهات المخزون"
          value={
            loading
              ? "—"
              : String(
                  summary?.lowStockVariants ??
                    0,
                )
          }
          detail="متغيرات وصلت لحد المخزون المنخفض"
        />

        <MetricCard
          icon={Store}
          label="السلات المتروكة"
          value={
            loading
              ? "—"
              : String(
                  summary?.abandonedCarts ??
                    0,
                )
          }
          detail={
            summary
              ? `بعد ${summary.abandonedAfterMinutes} دقيقة من عدم النشاط`
              : "محسوبة من نشاط السلات الحقيقي"
          }
        />
      </div>

      <div className="mt-5 grid gap-5 xl:grid-cols-[1.35fr_.65fr]">
        <section className="overflow-hidden rounded-[17px] border border-black/[0.065] bg-white">
          <SectionHeader
            title="آخر الطلبات"
            subtitle="أحدث الطلبات المسجلة فعليًا"
            link="/admin/orders"
          />

          {loading ? (
            <EmptyState
              text="جاري تحميل الطلبات…"
            />
          ) : !summary?.recentOrders.length ? (
            <EmptyState
              text="لا توجد طلبات بعد."
            />
          ) : (
            <div className="divide-y divide-black/[0.055]">
              {summary.recentOrders.map(
                (order) => (
                  <div
                    key={order.orderId}
                    className="grid gap-3 px-5 py-4 sm:grid-cols-[1fr_auto_auto] sm:items-center"
                  >
                    <div>
                      <p
                        dir="ltr"
                        className="text-left text-[9px] font-semibold"
                      >
                        #
                        {order.orderId.slice(
                          0,
                          8,
                        )}
                      </p>

                      <p className="mt-1 text-[8px] text-black/35">
                        {new Intl.DateTimeFormat(
                          "ar-SA",
                          {
                            dateStyle:
                              "medium",
                            timeStyle:
                              "short",
                          },
                        ).format(
                          new Date(
                            order.createdAtUtc,
                          ),
                        )}
                      </p>
                    </div>

                    <span className="w-fit rounded-full bg-[#f1f0eb] px-3 py-1 text-[8px] font-semibold text-black/55">
                      {orderStatusLabel(
                        order.status,
                        order.fulfillmentStatus,
                      )}
                    </span>

                    <p
                      dir="ltr"
                      className="text-left text-[10px] font-semibold"
                    >
                      {formatMoney(
                        order.totalAmount,
                        order.currency,
                      )}
                    </p>
                  </div>
                ),
              )}
            </div>
          )}
        </section>

        <section className="overflow-hidden rounded-[17px] border border-black/[0.065] bg-white">
          <SectionHeader
            title="المخزون المنخفض"
            subtitle="أكثر العناصر احتياجًا للمراجعة"
            link="/admin/products"
          />

          {loading ? (
            <EmptyState
              text="جاري تحميل المخزون…"
            />
          ) : !summary?.lowStock.length ? (
            <EmptyState
              text="لا توجد تنبيهات مخزون حاليًا."
            />
          ) : (
            <div className="divide-y divide-black/[0.055]">
              {summary.lowStock.slice(
                0,
                5,
              ).map(
                (item) => (
                  <div
                    key={item.variantId}
                    className="px-5 py-4"
                  >
                    <div className="flex items-center justify-between gap-3">
                      <div className="min-w-0">
                        <p className="truncate text-[9px] font-semibold">
                          {item.productName}
                        </p>
                        <p className="mt-1 truncate text-[8px] text-black/34">
                          {item.variantName}
                          {item.sku
                            ? ` · ${item.sku}`
                            : ""}
                        </p>
                      </div>

                      <span className="shrink-0 rounded-full bg-[#f7eee0] px-2.5 py-1 text-[8px] font-semibold text-[#765327]">
                        {item.quantity}
                      </span>
                    </div>

                    <p className="mt-2 text-[8px] text-black/32">
                      حد التنبيه:{" "}
                      {item.lowStockThreshold}
                    </p>
                  </div>
                ),
              )}
            </div>
          )}
        </section>
      </div>

      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        <section className="overflow-hidden rounded-[17px] border border-black/[0.065] bg-white">
          <SectionHeader
            title="السلات المتروكة"
            subtitle="نشاط حقيقي لم يتحول إلى طلب"
          />

          {loading ? (
            <EmptyState
              text="جاري تحميل السلات…"
            />
          ) : !summary?.abandonedCartItems.length ? (
            <EmptyState
              text="لا توجد سلات متروكة ضمن النافذة الحالية."
            />
          ) : (
            <div className="divide-y divide-black/[0.055]">
              {summary.abandonedCartItems
                .slice(
                  0,
                  5,
                )
                .map(
                  (cart) => (
                    <div
                      key={cart.cartId}
                      className="flex items-center justify-between gap-4 px-5 py-4"
                    >
                      <div>
                        <p
                          dir="ltr"
                          className="text-left text-[9px] font-semibold"
                        >
                          #
                          {cart.cartId.slice(
                            0,
                            8,
                          )}
                        </p>
                        <p className="mt-1 text-[8px] text-black/34">
                          {cart.totalQuantity} عناصر · آخر نشاط{" "}
                          {new Intl.DateTimeFormat(
                            "ar-SA",
                            {
                              dateStyle:
                                "short",
                              timeStyle:
                                "short",
                            },
                          ).format(
                            new Date(
                              cart.lastActivityAtUtc,
                            ),
                          )}
                        </p>
                      </div>

                      <p
                        dir="ltr"
                        className="text-left text-[9px] font-semibold"
                      >
                        {formatMoney(
                          cart.totalAmount,
                          cart.currency,
                        )}
                      </p>
                    </div>
                  ),
                )}
            </div>
          )}
        </section>

        <section className="overflow-hidden rounded-[17px] border border-black/[0.065] bg-white">
          <SectionHeader
            title="أفضل المنتجات"
            subtitle="من المبيعات الملتقطة فعليًا"
          />

          {loading ? (
            <EmptyState
              text="جاري تحميل الأداء…"
            />
          ) : !summary?.topProducts.length ? (
            <EmptyState
              text="لا توجد مبيعات كافية للترتيب بعد."
            />
          ) : (
            <div className="divide-y divide-black/[0.055]">
              {summary.topProducts
                .slice(
                  0,
                  5,
                )
                .map(
                  (
                    product,
                    index,
                  ) => (
                    <div
                      key={`${product.productId}-${product.currency}`}
                      className="grid grid-cols-[32px_1fr_auto] items-center gap-3 px-5 py-4"
                    >
                      <span className="text-[10px] font-semibold text-black/30">
                        {String(
                          index + 1,
                        ).padStart(
                          2,
                          "0",
                        )}
                      </span>
                      <div className="min-w-0">
                        <p className="truncate text-[9px] font-semibold">
                          {product.productName}
                        </p>
                        <p className="mt-1 text-[8px] text-black/34">
                          {product.quantitySold} قطعة
                        </p>
                      </div>
                      <p
                        dir="ltr"
                        className="text-left text-[9px] font-semibold"
                      >
                        {formatMoney(
                          product.capturedSales,
                          product.currency,
                        )}
                      </p>
                    </div>
                  ),
                )}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

function CompactStat({
  label,
  value,
}: {
  label: string;
  value: number;
}) {
  return (
    <div className="rounded-[12px] border border-white/[0.07] bg-white/[0.035] px-3.5 py-3">
      <p className="text-[8px] leading-4 text-white/40">
        {label}
      </p>
      <p className="mt-2 text-[20px] font-semibold tracking-[-0.03em]">
        {value}
      </p>
    </div>
  );
}

function MetricCard({
  icon: Icon,
  label,
  value,
  detail,
}: {
  icon: typeof TrendingUp;
  label: string;
  value: string;
  detail: string;
}) {
  return (
    <div className="rounded-[15px] border border-black/[0.065] bg-white p-5">
      <div className="flex items-center justify-between">
        <div className="flex size-8 items-center justify-center rounded-[9px] bg-[#f1f0eb] text-black/55">
          <Icon
            size={14}
          />
        </div>
      </div>
      <p className="mt-5 text-[9px] text-black/38">
        {label}
      </p>
      <p className="mt-1 text-[24px] font-semibold tracking-[-0.04em]">
        {value}
      </p>
      <p className="mt-2 text-[8px] leading-4 text-black/30">
        {detail}
      </p>
    </div>
  );
}

function SectionHeader({
  title,
  subtitle,
  link,
}: {
  title: string;
  subtitle: string;
  link?: string;
}) {
  return (
    <div className="flex items-center justify-between gap-4 border-b border-black/[0.06] px-5 py-4">
      <div>
        <h2 className="text-[10px] font-semibold">
          {title}
        </h2>
        <p className="mt-1 text-[8px] text-black/32">
          {subtitle}
        </p>
      </div>

      {link ? (
        <Link
          to={link}
          className="flex items-center gap-1 text-[8px] font-semibold text-black/45 hover:text-black"
        >
          عرض الكل
          <ChevronLeft
            size={12}
          />
        </Link>
      ) : null}
    </div>
  );
}

function EmptyState({
  text,
}: {
  text: string;
}) {
  return (
    <div className="p-10 text-center text-[9px] text-black/34">
      {text}
    </div>
  );
}
