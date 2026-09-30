import {
  AlertTriangle,
  ArrowUpLeft,
  Boxes,
  ChevronLeft,
  ChevronRight,
  Clock3,
  MapPin,
  PackageCheck,
  ReceiptText,
  RefreshCw,
  ShoppingBag,
  Sparkles,
  Store,
  TrendingUp,
  Truck,
  UserRound,
} from "lucide-react";

import type {
  LucideIcon,
} from "lucide-react";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

import {
  Link,
} from "react-router";

import {
  AdminOperationApiError,
  getMerchantDashboardSummary,
  getMerchantOrderById,
  getMerchantOrders,
  type MerchantOperationsDashboard,
  type MerchantOrderDetail,
  type MerchantOrderSummary,
} from "./dashboard/adminDashboardApi";

import {
  listManualReviews,
  type ManualReviewOrder,
} from "./payments/manualReviewApi";

import {
  readAdminStore,
} from "./store-setup/storeSetupStorage";

import "./dashboard/AdminDashboardV5.css";

type Tone =
  | "neutral"
  | "accent"
  | "success"
  | "warning"
  | "danger"
  | "info";

function normalize(
  value:
    | string
    | null
    | undefined,
) {
  return (
    value ??
    ""
  )
    .trim()
    .toLowerCase();
}

function formatNumberEn(
  value: number,
) {
  return new Intl.NumberFormat(
    "en-US",
    {
      maximumFractionDigits:
        0,
    },
  ).format(value);
}

function formatMoney(
  amount: number,
  currency:
    | string
    | null,
) {
  if (!currency) {
    return formatNumberEn(
      amount,
    );
  }

  try {
    return new Intl.NumberFormat(
      "en-US",
      {
        style:
          "currency",
        currency,
        maximumFractionDigits:
          0,
      },
    ).format(amount);
  } catch {
    return `${formatNumberEn(
      amount,
    )} ${currency}`;
  }
}

function formatDateTime(
  utc:
    | string
    | null
    | undefined,
) {
  if (!utc) {
    return "—";
  }

  const date =
    new Date(utc);

  if (
    Number.isNaN(
      date.getTime(),
    )
  ) {
    return "—";
  }

  return new Intl.DateTimeFormat(
    "ar-SA-u-nu-latn",
    {
      day:
        "2-digit",
      month:
        "short",
      year:
        "numeric",
      hour:
        "2-digit",
      minute:
        "2-digit",
    },
  ).format(date);
}

function relativeTime(
  utc:
    | string
    | null
    | undefined,
) {
  if (!utc) {
    return "—";
  }

  const date =
    new Date(utc);

  if (
    Number.isNaN(
      date.getTime(),
    )
  ) {
    return "—";
  }

  const minutes =
    Math.max(
      0,
      Math.floor(
        (
          Date.now() -
          date.getTime()
        ) /
          60000,
      ),
    );

  if (minutes < 1) {
    return "الآن";
  }

  if (minutes < 60) {
    return `منذ ${formatNumberEn(
      minutes,
    )} دقيقة`;
  }

  const hours =
    Math.floor(
      minutes /
        60,
    );

  if (hours < 24) {
    return `منذ ${formatNumberEn(
      hours,
    )} ساعة`;
  }

  return `منذ ${formatNumberEn(
    Math.floor(
      hours /
        24,
    ),
  )} يوم`;
}

function shortId(
  id: string,
) {
  return id
    .slice(
      0,
      8,
    )
    .toUpperCase();
}

function paymentMeta(
  order:
    MerchantOrderSummary,
): {
  label: string;
  tone: Tone;
} {
  const value =
    normalize(
      order.paymentStatus,
    );

  if (
    [
      "paid",
      "captured",
      "approved",
    ].includes(
      value,
    )
  ) {
    return {
      label:
        "مدفوع",
      tone:
        "success",
    };
  }

  if (
    [
      "failed",
      "rejected",
      "cancelled",
    ].includes(
      value,
    )
  ) {
    return {
      label:
        "فشل الدفع",
      tone:
        "danger",
    };
  }

  return {
    label:
      "بانتظار الدفع",
    tone:
      "warning",
  };
}

function orderMeta(
  order:
    MerchantOrderSummary,
): {
  label: string;
  next: string;
  tone: Tone;
} {
  const orderStatus =
    normalize(
      order.orderStatus,
    );

  const fulfillment =
    normalize(
      order.fulfillmentStatus,
    );

  if (
    orderStatus ===
      "cancelled" ||
    fulfillment ===
      "cancelled"
  ) {
    return {
      label:
        "ملغي",
      next:
        "لا يوجد إجراء",
      tone:
        "danger",
    };
  }

  if (
    orderStatus ===
      "returned" ||
    orderStatus ===
      "refunded"
  ) {
    return {
      label:
        "مرتجع",
      next:
        "الطلب مؤرشف",
      tone:
        "danger",
    };
  }

  if (
    fulfillment ===
      "delivered" ||
    fulfillment ===
      "fulfilled"
  ) {
    return {
      label:
        "تم التسليم",
      next:
        "الطلب مكتمل",
      tone:
        "success",
    };
  }

  if (
    fulfillment ===
      "intransit"
  ) {
    return {
      label:
        "قيد التوصيل",
      next:
        "تأكيد التسليم",
      tone:
        "info",
    };
  }

  if (
    fulfillment ===
      "shipped"
  ) {
    return {
      label:
        "تم الشحن",
      next:
        "متابعة التوصيل",
      tone:
        "info",
    };
  }

  if (
    fulfillment ===
      "readytoship"
  ) {
    return {
      label:
        "جاهز للشحن",
      next:
        "تسليم شركة الشحن",
      tone:
        "warning",
    };
  }

  if (
    fulfillment ===
      "processing" ||
    orderStatus ===
      "processing"
  ) {
    return {
      label:
        "قيد التجهيز",
      next:
        "إنهاء التجهيز",
      tone:
        "accent",
    };
  }

  if (
    orderStatus ===
      "confirmed"
  ) {
    return {
      label:
        "مؤكد",
      next:
        "بدء التجهيز",
      tone:
        "accent",
    };
  }

  if (
    orderStatus ===
      "paid"
  ) {
    return {
      label:
        "مدفوع",
      next:
        "تأكيد الطلب",
      tone:
        "warning",
    };
  }

  return {
    label:
      "طلب جديد",
    next:
      "مراجعة الطلب",
    tone:
      "neutral",
  };
}

function destinationText(
  detail:
    MerchantOrderDetail
    | null,
) {
  if (!detail) {
    return "بيانات الوجهة غير متاحة";
  }

  if (
    normalize(
      detail.shippingMethodType,
    ) === "pickup"
  ) {
    return (
      detail.shippingMethodName ??
      "استلام من المتجر"
    );
  }

  const parts = [
    detail.shippingCity,
    detail.shippingRegion,
    detail.shippingCountryCode,
  ].filter(Boolean);

  return parts.length >
    0
    ? parts.join(
        "، ",
      )
    : detail.shippingMethodName ??
        "لم تحدد الوجهة";
}

function paymentMethodText(
  order:
    MerchantOrderSummary,
  review:
    ManualReviewOrder
    | undefined,
) {
  if (review) {
    return `تحويل يدوي · ${review.accountName}`;
  }

  return paymentMeta(
    order,
  ).label;
}

function productInitial(
  name: string,
) {
  const value =
    name.trim();

  return value
    ? value
        .slice(
          0,
          1,
        )
        .toUpperCase()
    : "P";
}

export function AdminDashboardPage() {
  const store =
    readAdminStore();

  const [
    summary,
    setSummary,
  ] =
    useState<MerchantOperationsDashboard | null>(
      null,
    );

  const [
    latestOrders,
    setLatestOrders,
  ] =
    useState<
      MerchantOrderSummary[]
    >([]);

  const [
    orderDetails,
    setOrderDetails,
  ] =
    useState<
      Record<
        string,
        MerchantOrderDetail
      >
    >({});

  const [
    manualReviews,
    setManualReviews,
  ] =
    useState<
      ManualReviewOrder[]
    >([]);

  const [
    loading,
    setLoading,
  ] =
    useState(true);

  const [
    error,
    setError,
  ] =
    useState<
      string |
      null
    >(null);

  const [
    activeOrderIndex,
    setActiveOrderIndex,
  ] =
    useState(0);

  const [
    carouselPaused,
    setCarouselPaused,
  ] =
    useState(false);

  const load =
    useCallback(
      async () => {
        if (
          !store?.tenantId
        ) {
          setLoading(
            false,
          );

          setError(
            "لم يتم العثور على متجر مرتبط بالحساب الحالي.",
          );

          return;
        }

        setLoading(
          true,
        );

        setError(
          null,
        );

        try {
          const [
            dashboardResult,
            orderRows,
            reviewRows,
          ] =
            await Promise.all([
              getMerchantDashboardSummary(
                store.tenantId,
                8,
              ),

              getMerchantOrders(
                store.tenantId,
                8,
              ),

              listManualReviews(
                store.tenantId,
              ).catch(
                () => [],
              ),
            ]);

          const sortedOrders =
            [
              ...orderRows,
            ]
              .sort(
                (
                  a,
                  b,
                ) =>
                  b.createdAtUtc.localeCompare(
                    a.createdAtUtc,
                  ),
              )
              .slice(
                0,
                6,
              );

          setSummary(
            dashboardResult,
          );

          setLatestOrders(
            sortedOrders,
          );

          setManualReviews(
            reviewRows,
          );

          setActiveOrderIndex(
            0,
          );

          const details =
            await Promise.all(
              sortedOrders.map(
                async (
                  order,
                ) => {
                  try {
                    return await getMerchantOrderById(
                      store.tenantId,
                      order.orderId,
                    );
                  } catch {
                    return null;
                  }
                },
              ),
            );

          const next:
            Record<
              string,
              MerchantOrderDetail
            > =
              {};

          details.forEach(
            (
              detail,
            ) => {
              if (
                detail
              ) {
                next[
                  detail.orderId
                ] =
                  detail;
              }
            },
          );

          setOrderDetails(
            next,
          );
        } catch (
          caught
        ) {
          if (
            caught instanceof
              AdminOperationApiError &&
            caught.status ===
              403
          ) {
            setError(
              "الجلسة الحالية تحتاج تحققًا أمنيًا إضافيًا للوصول إلى بيانات التشغيل.",
            );
          } else {
            setError(
              caught instanceof
                Error
                ? caught.message
                : "تعذر تحميل الصفحة الرئيسية.",
            );
          }
        } finally {
          setLoading(
            false,
          );
        }
      },
      [
        store?.tenantId,
      ],
    );

  useEffect(
    () => {
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
    },
    [
      load,
    ],
  );

  useEffect(
    () => {
      if (
        carouselPaused ||
        latestOrders.length <=
          1
      ) {
        return;
      }

      const timer =
        window.setInterval(
          () => {
            setActiveOrderIndex(
              (
                current,
              ) =>
                (
                  current +
                  1
                ) %
                latestOrders.length,
            );
          },
          5500,
        );

      return () =>
        window.clearInterval(
          timer,
        );
    },
    [
      carouselPaused,
      latestOrders.length,
    ],
  );

  const reviewByOrder =
    useMemo(
      () =>
        new Map(
          manualReviews.map(
            (
              review,
            ) => [
              review.orderId,
              review,
            ],
          ),
        ),
      [
        manualReviews,
      ],
    );

  const safeIndex =
    latestOrders.length
      ? activeOrderIndex %
        latestOrders.length
      : 0;

  const activeOrder =
    latestOrders[
      safeIndex
    ] ??
    null;

  const activeDetail =
    activeOrder
      ? orderDetails[
          activeOrder.orderId
        ] ??
        null
      : null;

  const activeReview =
    activeOrder
      ? reviewByOrder.get(
          activeOrder.orderId,
        )
      : undefined;

  const readiness =
    summary?.readiness
      .percentage ??
    0;

  const dateLabel =
    new Intl.DateTimeFormat(
      "ar-SA-u-nu-latn",
      {
        weekday:
          "long",
        day:
          "2-digit",
        month:
          "long",
        year:
          "numeric",
      },
    ).format(
      new Date(),
    );

  const topProducts =
    summary?.topProducts
      .slice(
        0,
        5,
      ) ??
    [];

  const topUnits =
    topProducts.reduce(
      (
        total,
        product,
      ) =>
        total +
        product.quantitySold,
      0,
    );

  const maxSold =
    Math.max(
      1,
      ...topProducts.map(
        (
          product,
        ) =>
          product.quantitySold,
      ),
    );

  function previousOrder() {
    if (
      !latestOrders.length
    ) {
      return;
    }

    setActiveOrderIndex(
      (
        current,
      ) =>
        (
          current -
          1 +
          latestOrders.length
        ) %
        latestOrders.length,
    );
  }

  function nextOrder() {
    if (
      !latestOrders.length
    ) {
      return;
    }

    setActiveOrderIndex(
      (
        current,
      ) =>
        (
          current +
          1
        ) %
        latestOrders.length,
    );
  }

  return (
    <div
      dir="rtl"
      className="rukn-d5"
    >
      <section className="rukn-d5-intro">
        <div>
          <div className="rukn-d5-date">
            <Sparkles
              size={16}
            />

            {dateLabel}
          </div>

          <h1>
            مرحبًا بك
            {store?.name
              ? ` في ${store.name}`
              : ""}
          </h1>

          <p>
            كل ما يحتاج قرارًا اليوم في مساحة واحدة،
            مرتبة حسب الأولوية بدل كثرة الصناديق.
          </p>
        </div>

        <div className="rukn-d5-intro-actions">
          <button
            type="button"
            onClick={() =>
              void load()
            }
            disabled={
              loading
            }
            aria-label="تحديث البيانات"
          >
            <RefreshCw
              size={18}
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
            >
              <ArrowUpLeft
                size={17}
              />

              فتح المتجر
            </Link>
          ) : null}
        </div>
      </section>

      {error ? (
        <div className="rukn-d5-error">
          <AlertTriangle
            size={18}
          />

          <span>
            {error}
          </span>

          <button
            type="button"
            onClick={() =>
              void load()
            }
          >
            إعادة المحاولة
          </button>
        </div>
      ) : null}

      <section className="rukn-d5-kpis">
        <Metric
          to="/admin/orders"
          icon={ShoppingBag}
          label="الطلبات المفتوحة"
          value={
            loading
              ? "—"
              : formatNumberEn(
                  summary?.openOrders ??
                    0,
                )
          }
          note="طلبات تنتظر خطوة تشغيلية"
          tone="green"
          featured
        />

        <Metric
          to="/admin/orders?view=paid"
          icon={ReceiptText}
          label="تأكيد الدفع"
          value={
            loading
              ? "—"
              : formatNumberEn(
                  summary
                    ?.paidOrdersAwaitingConfirmation ??
                    0,
                )
          }
          note="دفعات تنتظر المراجعة"
          tone="amber"
        />

        <Metric
          to="/admin/inventory?filter=low-stock"
          icon={PackageCheck}
          label="المخزون"
          value={
            loading
              ? "—"
              : formatNumberEn(
                  summary
                    ?.lowStockVariants ??
                    0,
                )
          }
          note="منتجات وصلت حد التنبيه"
          tone="red"
        />

        <Metric
          href="#abandoned-carts"
          icon={Store}
          label="السلات المتروكة"
          value={
            loading
              ? "—"
              : formatNumberEn(
                  summary
                    ?.abandonedCarts ??
                    0,
                )
          }
          note="فرص شراء لم تكتمل"
          tone="plain"
        />
      </section>

      <section className="rukn-d5-grid">
        <section className="rukn-d5-module rukn-d5-orders">
          <ModuleHeader
            kicker="التشغيل الآن"
            title="مركز الطلبات"
            description="طلب واحد في الواجهة، وباقي الطلبات قريبة بدون ازدحام."
            link="/admin/orders"
            linkText="كل الطلبات"
          />

          {loading ? (
            <Loading
              count={4}
            />
          ) : !activeOrder ? (
            <Empty
              icon={ShoppingBag}
              title="لا توجد طلبات بعد"
              text="أول طلب جديد سيظهر هنا مع الدفع والتنفيذ والتوصيل."
            />
          ) : (
            <div className="rukn-d5-order-body">
              <article
                className="rukn-d5-order-focus"
                onMouseEnter={() =>
                  setCarouselPaused(
                    true,
                  )
                }
                onMouseLeave={() =>
                  setCarouselPaused(
                    false,
                  )
                }
              >
                <div className="rukn-d5-order-head">
                  <div className="rukn-d5-order-number">
                    <span />

                    <div>
                      <small>
                        الطلب الحالي
                      </small>

                      <strong
                        dir="ltr"
                      >
                        #
                        {shortId(
                          activeOrder.orderId,
                        )}
                      </strong>

                      <p>
                        {relativeTime(
                          activeOrder.createdAtUtc,
                        )}
                      </p>
                    </div>
                  </div>

                  <div className="rukn-d5-order-controls">
                    <Status
                      tone={
                        orderMeta(
                          activeOrder,
                        )
                          .tone
                      }
                    >
                      {
                        orderMeta(
                          activeOrder,
                        )
                          .label
                      }
                    </Status>

                    <button
                      type="button"
                      onClick={
                        previousOrder
                      }
                      aria-label="الطلب السابق"
                    >
                      <ChevronRight
                        size={17}
                      />
                    </button>

                    <strong>
                      {formatNumberEn(
                        safeIndex +
                          1,
                      )}
                      /
                      {formatNumberEn(
                        latestOrders.length,
                      )}
                    </strong>

                    <button
                      type="button"
                      onClick={
                        nextOrder
                      }
                      aria-label="الطلب التالي"
                    >
                      <ChevronLeft
                        size={17}
                      />
                    </button>
                  </div>
                </div>

                <div className="rukn-d5-order-summary">
                  <div className="customer">
                    <span>
                      <UserRound
                        size={20}
                      />
                    </span>

                    <div>
                      <small>
                        العميل
                      </small>

                      <strong>
                        {activeReview
                          ?.customerName ||
                          activeOrder
                            .customerEmail ||
                          "عميل المتجر"}
                      </strong>

                      <p>
                        {activeOrder
                          .customerEmail ??
                          activeReview
                            ?.customerPhone ??
                          "بيانات التواصل داخل الطلب"}
                      </p>
                    </div>
                  </div>

                  <div>
                    <small>
                      إجمالي الطلب
                    </small>

                    <strong
                      dir="ltr"
                      className="money"
                    >
                      {formatMoney(
                        activeOrder.totalAmount,
                        activeOrder.currency,
                      )}
                    </strong>

                    <p>
                      {formatNumberEn(
                        activeOrder.totalQuantity,
                      )}
                      {" "}
                      قطعة
                    </p>
                  </div>

                  <div>
                    <small>
                      الدفع
                    </small>

                    <Status
                      tone={
                        paymentMeta(
                          activeOrder,
                        )
                          .tone
                      }
                    >
                      {
                        paymentMeta(
                          activeOrder,
                        )
                          .label
                      }
                    </Status>

                    <p>
                      {paymentMethodText(
                        activeOrder,
                        activeReview,
                      )}
                    </p>
                  </div>

                  <div>
                    <small>
                      الإجراء التالي
                    </small>

                    <strong>
                      {
                        orderMeta(
                          activeOrder,
                        )
                          .next
                      }
                    </strong>

                    <p>
                      من تفاصيل الطلب
                    </p>
                  </div>
                </div>

                <div className="rukn-d5-order-details">
                  <Fact
                    icon={MapPin}
                    label="الوجهة"
                    value={
                      destinationText(
                        activeDetail,
                      )
                    }
                  />

                  <Fact
                    icon={Truck}
                    label="الشحن"
                    value={
                      activeDetail
                        ?.shippingCarrier ||
                      activeOrder
                        .shippingCarrier ||
                      "لم يبدأ الشحن بعد"
                    }
                  />

                  <Fact
                    icon={Clock3}
                    label="تاريخ الطلب"
                    value={
                      formatDateTime(
                        activeOrder.createdAtUtc,
                      )
                    }
                  />
                </div>

                <footer className="rukn-d5-order-footer">
                  <div>
                    {latestOrders.map(
                      (
                        order,
                        index,
                      ) => (
                        <button
                          key={
                            order.orderId
                          }
                          type="button"
                          aria-label={`عرض الطلب ${index + 1}`}
                          className={
                            index ===
                            safeIndex
                              ? "active"
                              : ""
                          }
                          onClick={() =>
                            setActiveOrderIndex(
                              index,
                            )
                          }
                        />
                      ),
                    )}
                  </div>

                  <Link
                    to={`/admin/orders?search=${encodeURIComponent(
                      activeOrder.orderId,
                    )}`}
                  >
                    فتح تفاصيل الطلب

                    <ChevronLeft
                      size={16}
                    />
                  </Link>
                </footer>
              </article>

              <div className="rukn-d5-order-stream">
                {latestOrders.map(
                  (
                    order,
                    index,
                  ) => {
                    const stage =
                      orderMeta(
                        order,
                      );

                    const review =
                      reviewByOrder.get(
                        order.orderId,
                      );

                    return (
                      <button
                        type="button"
                        key={
                          order.orderId
                        }
                        className={
                          index ===
                          safeIndex
                            ? "active"
                            : ""
                        }
                        onClick={() =>
                          setActiveOrderIndex(
                            index,
                          )
                        }
                      >
                        <span>
                          <strong
                            dir="ltr"
                          >
                            #
                            {shortId(
                              order.orderId,
                            )}
                          </strong>

                          <small
                            data-tone={
                              stage.tone
                            }
                          >
                            {
                              stage.label
                            }
                          </small>
                        </span>

                        <p>
                          {review
                            ?.customerName ||
                            order
                              .customerEmail ||
                            "عميل المتجر"}
                        </p>

                        <div>
                          <strong
                            dir="ltr"
                          >
                            {formatMoney(
                              order.totalAmount,
                              order.currency,
                            )}
                          </strong>

                          <small>
                            {relativeTime(
                              order.createdAtUtc,
                            )}
                          </small>
                        </div>
                      </button>
                    );
                  },
                )}
              </div>
            </div>
          )}
        </section>

        <aside className="rukn-d5-module rukn-d5-attention">
          <div className="rukn-d5-attention-head">
            <span>
              نظرة سريعة
            </span>

            <h2>
              يحتاج انتباهك
            </h2>

            <p>
              أهم الإشارات التشغيلية الحالية بدون أرقام مصطنعة.
            </p>
          </div>

          <div className="rukn-d5-readiness">
            <div>
              <span>
                جاهزية المتجر
              </span>

              <strong>
                {loading
                  ? "—"
                  : `${formatNumberEn(
                      readiness,
                    )}%`}
              </strong>
            </div>

            <div>
              <i
                style={{
                  width: `${readiness}%`,
                }}
              />
            </div>

            <p>
              {summary
                ?.readiness
                .state ??
                "جارٍ تحميل حالة المتجر."}
            </p>
          </div>

          <AttentionRow
            tone="amber"
            label="دفعات تنتظر التأكيد"
            value={
              loading
                ? "—"
                : formatNumberEn(
                    summary
                      ?.paidOrdersAwaitingConfirmation ??
                      0,
                  )
            }
            to="/admin/orders?view=paid"
          />

          <AttentionRow
            tone="red"
            label="مخزون عند حد التنبيه"
            value={
              loading
                ? "—"
                : formatNumberEn(
                    summary
                      ?.lowStockVariants ??
                      0,
                  )
            }
            to="/admin/inventory?filter=low-stock"
          />

          <AttentionRow
            tone="green"
            label="سلات لم تكتمل"
            value={
              loading
                ? "—"
                : formatNumberEn(
                    summary
                      ?.abandonedCarts ??
                      0,
                  )
            }
            href="#abandoned-carts"
          />
        </aside>

        <section className="rukn-d5-module rukn-d5-products">
          <ModuleHeader
            kicker="الأداء الفعلي"
            title="أفضل المنتجات"
            description="الترتيب مبني على الوحدات المباعة والمبيعات المسجلة."
            link="/admin/products"
            linkText="كل المنتجات"
          />

          {loading ? (
            <Loading
              count={5}
            />
          ) : !topProducts.length ? (
            <Empty
              icon={TrendingUp}
              title="لا توجد مبيعات كافية"
              text="يظهر ترتيب المنتجات بعد تسجيل مبيعات فعلية."
              compact
            />
          ) : (
            <div className="rukn-d5-product-list">
              {topProducts.map(
                (
                  product,
                  index,
                ) => {
                  const share =
                    topUnits >
                    0
                      ? Math.round(
                          (
                            product.quantitySold /
                            topUnits
                          ) *
                            100,
                        )
                      : 0;

                  const performance =
                    Math.max(
                      5,
                      Math.round(
                        (
                          product.quantitySold /
                          maxSold
                        ) *
                          100,
                      ),
                    );

                  return (
                    <article
                      key={`${product.productId}-${product.currency}`}
                      className={
                        index ===
                        0
                          ? "leader"
                          : ""
                      }
                    >
                      <span className="rank">
                        {String(
                          index +
                            1,
                        ).padStart(
                          2,
                          "0",
                        )}
                      </span>

                      <span className="avatar">
                        {productInitial(
                          product.productName,
                        )}
                      </span>

                      <div className="copy">
                        <div>
                          <strong>
                            {
                              product.productName
                            }
                          </strong>

                          <span>
                            {formatNumberEn(
                              product.quantitySold,
                            )}
                            {" "}
                            مباع
                          </span>
                        </div>

                        <div className="track">
                          <i
                            style={{
                              width: `${performance}%`,
                            }}
                          />
                        </div>

                        <small>
                          حصة Top 5:
                          {" "}
                          {formatNumberEn(
                            share,
                          )}
                          %
                        </small>
                      </div>

                      <strong
                        dir="ltr"
                        className="sales"
                      >
                        {formatMoney(
                          product.capturedSales,
                          product.currency,
                        )}
                      </strong>
                    </article>
                  );
                },
              )}
            </div>
          )}
        </section>

        <section className="rukn-d5-module rukn-d5-stock">
          <ModuleHeader
            kicker="المخزون"
            title="الأقرب للنفاد"
            description="الحالات التي تحتاج تدخلاً قبل بقية المخزون."
            link="/admin/inventory?filter=low-stock"
            linkText="إدارة المخزون"
          />

          {loading ? (
            <Loading
              count={4}
            />
          ) : !summary
              ?.lowStock
              .length ? (
            <Empty
              icon={PackageCheck}
              title="المخزون بحالة جيدة"
              text="لا توجد منتجات ضمن حد المخزون المنخفض."
              compact
            />
          ) : (
            <div className="rukn-d5-stock-list">
              {summary.lowStock
                .slice(
                  0,
                  4,
                )
                .map(
                  (
                    item,
                  ) => {
                    const out =
                      item.quantity <=
                      0;

                    return (
                      <article
                        key={
                          item.variantId
                        }
                      >
                        <span
                          className={
                            out
                              ? "danger"
                              : ""
                          }
                        >
                          <Boxes
                            size={18}
                          />
                        </span>

                        <div>
                          <strong>
                            {
                              item.productName
                            }
                          </strong>

                          <p>
                            {
                              item.variantName
                            }

                            {item.sku
                              ? ` · ${item.sku}`
                              : ""}
                          </p>
                        </div>

                        <div>
                          <strong
                            className={
                              out
                                ? "danger"
                                : ""
                            }
                          >
                            {formatNumberEn(
                              item.quantity,
                            )}
                          </strong>

                          <small>
                            تنبيه عند
                            {" "}
                            {formatNumberEn(
                              item.lowStockThreshold,
                            )}
                          </small>
                        </div>
                      </article>
                    );
                  },
                )}
            </div>
          )}
        </section>

        <section
          id="abandoned-carts"
          className="rukn-d5-module rukn-d5-carts"
        >
          <ModuleHeader
            kicker="فرص غير مكتملة"
            title="السلات المتروكة"
            description="أحدث السلات التي توقفت قبل إكمال الشراء."
          />

          {loading ? (
            <Loading
              count={4}
            />
          ) : !summary
              ?.abandonedCartItems
              .length ? (
            <Empty
              icon={Store}
              title="لا توجد سلات متروكة"
              text="لا توجد فرص شراء متوقفة ضمن نافذة الخمول الحالية."
              compact
            />
          ) : (
            <div className="rukn-d5-cart-list">
              {summary.abandonedCartItems
                .slice(
                  0,
                  4,
                )
                .map(
                  (
                    cart,
                  ) => (
                    <article
                      key={
                        cart.cartId
                      }
                    >
                      <span>
                        <ShoppingBag
                          size={18}
                        />
                      </span>

                      <div>
                        <strong>
                          {cart.customerUserId
                            ? "عميل مسجل"
                            : "زائر"}
                        </strong>

                        <small
                          dir="ltr"
                        >
                          #
                          {shortId(
                            cart.cartId,
                          )}
                        </small>
                      </div>

                      <div>
                        <strong>
                          {formatNumberEn(
                            cart.totalQuantity,
                          )}
                          {" "}
                          عناصر
                        </strong>

                        <small>
                          {relativeTime(
                            cart.lastActivityAtUtc,
                          )}
                        </small>
                      </div>

                      <strong
                        dir="ltr"
                        className="total"
                      >
                        {formatMoney(
                          cart.totalAmount,
                          cart.currency,
                        )}
                      </strong>
                    </article>
                  ),
                )}
            </div>
          )}
        </section>
      </section>
    </div>
  );
}

function Metric({
  to,
  href,
  icon: Icon,
  label,
  value,
  note,
  tone,
  featured = false,
}: {
  to?: string;
  href?: string;
  icon: LucideIcon;
  label: string;
  value: string | number;
  note: string;
  tone:
    | "green"
    | "amber"
    | "red"
    | "plain";
  featured?: boolean;
}) {
  const content = (
    <>
      <span className="icon">
        <Icon
          size={20}
        />
      </span>

      <div>
        <span>
          {label}
        </span>

        <strong>
          {value}
        </strong>

        <small>
          {note}
        </small>
      </div>

      <ChevronLeft
        size={17}
        className="arrow"
      />
    </>
  );

  const className =
    [
      "rukn-d5-metric",
      featured
        ? "featured"
        : "",
    ]
      .filter(Boolean)
      .join(" ");

  if (href) {
    return (
      <a
        href={href}
        data-tone={tone}
        className={className}
      >
        {content}
      </a>
    );
  }

  return (
    <Link
      to={to ?? "/admin"}
      data-tone={tone}
      className={className}
    >
      {content}
    </Link>
  );
}

function ModuleHeader({
  kicker,
  title,
  description,
  link,
  linkText,
}: {
  kicker: string;
  title: string;
  description: string;
  link?: string;
  linkText?: string;
}) {
  return (
    <header className="rukn-d5-module-head">
      <div>
        <span>
          {kicker}
        </span>

        <h2>
          {title}
        </h2>

        <p>
          {description}
        </p>
      </div>

      {link &&
      linkText ? (
        <Link
          to={link}
        >
          {linkText}

          <ChevronLeft
            size={16}
          />
        </Link>
      ) : null}
    </header>
  );
}

function Status({
  tone,
  children,
}: {
  tone: Tone;
  children: ReactNode;
}) {
  return (
    <span
      className="rukn-d5-status"
      data-tone={tone}
    >
      <i />

      {children}
    </span>
  );
}

function Fact({
  icon: Icon,
  label,
  value,
}: {
  icon: LucideIcon;
  label: string;
  value: string;
}) {
  return (
    <div className="rukn-d5-fact">
      <Icon
        size={17}
      />

      <span>
        {label}
      </span>

      <strong>
        {value}
      </strong>
    </div>
  );
}

function AttentionRow({
  tone,
  label,
  value,
  to,
  href,
}: {
  tone:
    | "green"
    | "amber"
    | "red";
  label: string;
  value: string;
  to?: string;
  href?: string;
}) {
  const content = (
    <>
      <span
        className="dot"
        data-tone={tone}
      />

      <div>
        <span>
          {label}
        </span>

        <strong>
          {value}
        </strong>
      </div>

      <ChevronLeft
        size={17}
      />
    </>
  );

  if (href) {
    return (
      <a
        href={href}
        className="rukn-d5-attention-row"
      >
        {content}
      </a>
    );
  }

  return (
    <Link
      to={to ?? "/admin"}
      className="rukn-d5-attention-row"
    >
      {content}
    </Link>
  );
}

function Empty({
  icon: Icon,
  title,
  text,
  compact = false,
}: {
  icon: LucideIcon;
  title: string;
  text: string;
  compact?: boolean;
}) {
  return (
    <div
      className={[
        "rukn-d5-empty",
        compact
          ? "compact"
          : "",
      ].join(" ")}
    >
      <Icon
        size={24}
      />

      <strong>
        {title}
      </strong>

      <p>
        {text}
      </p>
    </div>
  );
}

function Loading({
  count,
}: {
  count: number;
}) {
  return (
    <div className="rukn-d5-loading">
      {Array.from({
        length:
          count,
      }).map(
        (
          _,
          index,
        ) => (
          <span
            key={
              index
            }
          />
        ),
      )}
    </div>
  );
}