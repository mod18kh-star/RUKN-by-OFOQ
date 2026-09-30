import {
  AlertTriangle,
  ArrowLeft,
  BarChart3,
  CalendarDays,
  Check,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Clock3,
  CreditCard,
  Download,
  Eye,
  FilterX,
  MapPin,
  Package,
  PackageCheck,
  RefreshCw,
  Search,
  ShoppingBag,
  SlidersHorizontal,
  Store,
  Truck,
  UserRound,
  WalletCards,
} from "lucide-react";

import type {
  LucideIcon,
} from "lucide-react";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type CSSProperties,
  type ReactNode,
} from "react";

import {
  Link,
} from "react-router";

import {
  AdminOperationApiError,
  changeMerchantOrderState,
  getMerchantOrderById,
  getMerchantOrders,
  type MerchantOrderDetail,
  type MerchantOrderSummary,
} from "./dashboard/adminDashboardApi";

import {
  getProductImages,
} from "./products/productsApi";

import {
  readAdminStore,
} from "./store-setup/storeSetupStorage";

import {
  ManualPaymentReviewPanel,
} from "./payments/ManualPaymentReviewPanel";

import "./AdminOrdersV2.css";
import "./AdminOrdersV22.css";

type Tab =
  | "all"
  | "attention"
  | "processing"
  | "shipping"
  | "archive";

type Sort =
  | "latest"
  | "oldest"
  | "highest"
  | "lowest";

type Tone =
  | "neutral"
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "accent";

type Action =
  Parameters<
    typeof changeMerchantOrderState
  >[2];

const pageSize = 10;

const filters: {
  id: Tab;
  title: string;
}[] = [
  {
    id: "all",
    title: "جميع الطلبات",
  },
  {
    id: "attention",
    title: "تحتاج إجراء",
  },
  {
    id: "processing",
    title: "قيد التجهيز",
  },
  {
    id: "shipping",
    title: "الشحن والتسليم",
  },
  {
    id: "archive",
    title: "مكتملة / مؤرشفة",
  },
];

function safe(
  value:
    | string
    | null
    | undefined,
) {
  return (value ?? "")
    .trim()
    .toLowerCase();
}

function isArchive(
  order: MerchantOrderSummary,
) {
  return (
    [
      "cancelled",
      "fulfilled",
      "refunded",
      "returned",
    ].includes(
      safe(order.orderStatus),
    ) ||
    [
      "cancelled",
      "delivered",
      "fulfilled",
      "returned",
    ].includes(
      safe(order.fulfillmentStatus),
    )
  );
}

function isShipping(
  order: MerchantOrderSummary,
) {
  return [
    "readytoship",
    "shipped",
    "intransit",
  ].includes(
    safe(
      order.fulfillmentStatus,
    ),
  );
}

function isProcessing(
  order: MerchantOrderSummary,
) {
  return (
    !isArchive(order) &&
    !isShipping(order) &&
    [
      "confirmed",
      "processing",
      "paid",
    ].includes(
      safe(order.orderStatus),
    )
  );
}

function inTab(
  order: MerchantOrderSummary,
  tab: Tab,
) {
  if (tab === "all") {
    return true;
  }

  if (
    tab === "attention" &&
    safe(order.orderStatus) === "pending"
  ) {
    return true;
  }

  if (tab === "archive") {
    return isArchive(order);
  }

  if (tab === "shipping") {
    return isShipping(order);
  }

  if (tab === "processing") {
    return isProcessing(order);
  }

  return (
    isProcessing(order) ||
    isShipping(order)
  );
}

function statusMeta(
  order: MerchantOrderSummary,
): {
  text: string;
  tone: Tone;
} {
  const status =
    safe(order.orderStatus);

  const fulfillment =
    safe(order.fulfillmentStatus);

  if (status === "cancelled") {
    return {
      text: "ملغي",
      tone: "danger",
    };
  }

  if (
    status === "returned" ||
    status === "refunded"
  ) {
    return {
      text: "مرتجع",
      tone: "neutral",
    };
  }

  if (
    fulfillment === "delivered" ||
    status === "fulfilled"
  ) {
    return {
      text: "تم التسليم",
      tone: "success",
    };
  }

  if (
    fulfillment === "intransit"
  ) {
    return {
      text: "قيد التوصيل",
      tone: "info",
    };
  }

  if (
    fulfillment === "shipped"
  ) {
    return {
      text: "تم الشحن",
      tone: "info",
    };
  }

  if (
    fulfillment === "readytoship"
  ) {
    return {
      text: "جاهز للشحن",
      tone: "warning",
    };
  }

  if (
    fulfillment === "processing" ||
    status === "processing"
  ) {
    return {
      text: "قيد التجهيز",
      tone: "accent",
    };
  }

  if (status === "confirmed") {
    return {
      text: "بانتظار التجهيز",
      tone: "accent",
    };
  }

  if (status === "paid") {
    return {
      text: "بانتظار التأكيد",
      tone: "warning",
    };
  }

  return {
    text: "تحتاج مراجعة",
    tone: "neutral",
  };
}

function paymentMeta(
  order: MerchantOrderSummary,
): {
  text: string;
  tone: Tone;
} {
  const status =
    safe(order.paymentStatus);

  if (
    [
      "paid",
      "captured",
      "approved",
    ].includes(status)
  ) {
    return {
      text: "مدفوع",
      tone: "success",
    };
  }

  if (
    [
      "failed",
      "rejected",
    ].includes(status)
  ) {
    return {
      text: "فشل الدفع",
      tone: "danger",
    };
  }

  if (
    [
      "refunded",
      "refund",
    ].includes(status)
  ) {
    return {
      text: "مسترد",
      tone: "neutral",
    };
  }

  return {
    text: "قيد المراجعة",
    tone: "warning",
  };
}

function shippingMeta(
  order: MerchantOrderSummary,
): {
  text: string;
  tone: Tone;
} {
  const fulfillment =
    safe(order.fulfillmentStatus);

  if (
    fulfillment === "delivered" ||
    fulfillment === "fulfilled"
  ) {
    return {
      text: "تم التسليم",
      tone: "success",
    };
  }

  if (
    fulfillment === "intransit"
  ) {
    return {
      text: "قيد التوصيل",
      tone: "info",
    };
  }

  if (
    fulfillment === "shipped"
  ) {
    return {
      text:
        order.shippingCarrier ||
        "تم الشحن",
      tone: "info",
    };
  }

  if (
    fulfillment === "readytoship"
  ) {
    return {
      text: "جاهز للشحن",
      tone: "warning",
    };
  }

  return {
    text: "لم يبدأ",
    tone: "neutral",
  };
}

function money(
  amount: number,
  currency: string,
) {
  try {
    return new Intl.NumberFormat(
      "en-US",
      {
        style: "currency",
        currency,
        maximumFractionDigits: 2,
      },
    ).format(amount);
  } catch {
    return `${amount.toLocaleString(
      "en-US",
    )} ${currency}`;
  }
}

function date(
  utc: string,
) {
  return new Intl.DateTimeFormat(
    "ar-SA-u-nu-latn",
    {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    },
  ).format(
    new Date(utc),
  );
}

function relativeTime(
  utc: string,
) {
  const timestamp =
    new Date(utc).getTime();

  if (
    Number.isNaN(timestamp)
  ) {
    return "—";
  }

  const minutes =
    Math.max(
      0,
      Math.floor(
        (
          Date.now() -
          timestamp
        ) /
          60000,
      ),
    );

  if (minutes < 1) {
    return "الآن";
  }

  if (minutes < 60) {
    return `منذ ${minutes.toLocaleString(
      "en-US",
    )} دقيقة`;
  }

  const hours =
    Math.floor(
      minutes / 60,
    );

  if (hours < 24) {
    return `منذ ${hours.toLocaleString(
      "en-US",
    )} ساعة`;
  }

  return `منذ ${Math.floor(
    hours / 24,
  ).toLocaleString(
    "en-US",
  )} يوم`;
}

function shortId(
  id: string,
) {
  return id
    .slice(0, 8)
    .toUpperCase();
}

function destination(
  order: MerchantOrderDetail,
) {
  if (
    safe(
      order.shippingMethodType,
    ) === "pickup"
  ) {
    return (
      order.shippingMethodName ??
      "استلام من المتجر"
    );
  }

  const parts = [
    order.shippingCity,
    order.shippingRegion,
  ].filter(Boolean);

  return (
    parts.join("، ") ||
    order.shippingMethodName ||
    "غير محددة"
  );
}

function csvCell(
  value:
    | string
    | number,
) {
  return `"${String(value).replace(
    /"/g,
    '""',
  )}"`;
}

export function AdminOrdersPage() {
  const store =
    readAdminStore();

  const tenantId =
    store?.tenantId ?? "";

  const [
    orders,
    setOrders,
  ] =
    useState<
      MerchantOrderSummary[]
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
    useState("");

  const [
    blocked,
    setBlocked,
  ] =
    useState(false);

  const dashboardView =
    new URLSearchParams(
      window.location.search,
    ).get("view");

  const [
    tab,
    setTab,
  ] =
    useState<Tab>(
      dashboardView === "paid"
        ? "attention"
        : "all",
    );

  const [
    query,
    setQuery,
  ] =
    useState("");

  const [
    sort,
    setSort,
  ] =
    useState<Sort>(
      "latest",
    );

  const [
    from,
    setFrom,
  ] =
    useState("");

  const [
    to,
    setTo,
  ] =
    useState("");

  const [
    selected,
    setSelected,
  ] =
    useState<
      MerchantOrderDetail |
      null
    >(null);

  const [
    detailLoading,
    setDetailLoading,
  ] =
    useState(false);

  const [
    busy,
    setBusy,
  ] =
    useState(false);

  const [
    shippingCarrier,
    setShippingCarrier,
  ] =
    useState("");

  const [
    trackingNumber,
    setTrackingNumber,
  ] =
    useState("");

  const [
    notice,
    setNotice,
  ] =
    useState("");

  const [
    paymentReviewOpen,
    setPaymentReviewOpen,
  ] =
    useState(false);

  const [
    page,
    setPage,
  ] =
    useState(1);

  const [
    productImages,
    setProductImages,
  ] =
    useState<
      Record<
        string,
        string
      >
    >({});

  const load =
    useCallback(
      async () => {
        if (!tenantId) {
          setLoading(false);

          setError(
            "لم يتم العثور على متجر حالي.",
          );

          return;
        }

        setLoading(true);
        setError("");
        setBlocked(false);

        try {
          setOrders(
            await getMerchantOrders(
              tenantId,
              100,
            ),
          );
        } catch (caught) {
          if (
            caught instanceof
              AdminOperationApiError &&
            caught.status === 403
          ) {
            setBlocked(true);
          }

          setError(
            caught instanceof Error
              ? caught.message
              : "تعذر تحميل الطلبات.",
          );
        } finally {
          setLoading(false);
        }
      },
      [
        tenantId,
      ],
    );

  useEffect(
    () => {
      const id =
        window.setTimeout(
          () => {
            void load();
          },
          0,
        );

      return () =>
        window.clearTimeout(id);
    },
    [
      load,
    ],
  );

  const counts =
    useMemo(
      () => ({
        all:
          orders.length,

        attention:
          orders.filter(
            (order) =>
              inTab(
                order,
                "attention",
              ),
          ).length,

        processing:
          orders.filter(
            (order) =>
              inTab(
                order,
                "processing",
              ),
          ).length,

        shipping:
          orders.filter(
            (order) =>
              inTab(
                order,
                "shipping",
              ),
          ).length,

        archive:
          orders.filter(
            (order) =>
              inTab(
                order,
                "archive",
              ),
          ).length,
      }),
      [
        orders,
      ],
    );

  const visible =
    useMemo(
      () => {
        const term =
          query
            .trim()
            .toLowerCase();

        return orders
          .filter(
            (order) =>
              inTab(
                order,
                tab,
              ) &&
              (
                dashboardView !==
                  "paid" ||
                safe(
                  order.orderStatus,
                ) === "paid"
              ) &&
              (
                !from ||
                order.createdAtUtc.slice(
                  0,
                  10,
                ) >= from
              ) &&
              (
                !to ||
                order.createdAtUtc.slice(
                  0,
                  10,
                ) <= to
              ) &&
              (
                !term ||
                [
                  order.orderId,
                  order.customerEmail ?? "",
                  order.trackingNumber ?? "",
                  order.shippingCarrier ?? "",
                  order.orderStatus,
                  order.paymentStatus,
                  order.fulfillmentStatus,
                ].some(
                  (value) =>
                    value
                      .toLowerCase()
                      .includes(term),
                )
              ),
          )
          .sort(
            (a, b) =>
              sort === "highest"
                ? b.totalAmount -
                  a.totalAmount
                : sort === "lowest"
                  ? a.totalAmount -
                    b.totalAmount
                  : sort === "oldest"
                    ? a.createdAtUtc.localeCompare(
                        b.createdAtUtc,
                      )
                    : b.createdAtUtc.localeCompare(
                        a.createdAtUtc,
                      ),
          );
      },
      [
        orders,
        tab,
        query,
        sort,
        from,
        to,
        dashboardView,
      ],
    );

  const pageCount =
    Math.max(
      1,
      Math.ceil(
        visible.length /
          pageSize,
      ),
    );

  const safePage =
    Math.min(
      page,
      pageCount,
    );

  const pageRows =
    visible.slice(
      (
        safePage -
        1
      ) *
        pageSize,
      safePage *
        pageSize,
    );

  const lastSevenDays =
    useMemo(
      () => {
        const result: {
          key: string;
          label: string;
          count: number;
        }[] = [];

        for (
          let offset = 6;
          offset >= 0;
          offset--
        ) {
          const current =
            new Date();

          current.setHours(
            0,
            0,
            0,
            0,
          );

          current.setDate(
            current.getDate() -
              offset,
          );

          const key =
            current
              .toISOString()
              .slice(0, 10);

          const label =
            new Intl.DateTimeFormat(
              "ar-SA-u-nu-latn",
              {
                weekday: "short",
              },
            ).format(current);

          result.push({
            key,
            label,
            count:
              orders.filter(
                (order) =>
                  order.createdAtUtc.slice(
                    0,
                    10,
                  ) === key,
              ).length,
          });
        }

        return result;
      },
      [
        orders,
      ],
    );

  const maxDailyOrders =
    Math.max(
      1,
      ...lastSevenDays.map(
        (item) =>
          item.count,
      ),
    );

  const paymentBreakdown =
    useMemo(
      () => {
        let paid = 0;
        let pending = 0;
        let issue = 0;

        for (
          const order
          of orders
        ) {
          const value =
            safe(
              order.paymentStatus,
            );

          if (
            [
              "paid",
              "captured",
              "approved",
            ].includes(value)
          ) {
            paid += 1;
          } else if (
            [
              "failed",
              "rejected",
              "refunded",
              "cancelled",
            ].includes(value)
          ) {
            issue += 1;
          } else {
            pending += 1;
          }
        }

        const total =
          Math.max(
            1,
            orders.length,
          );

        return {
          paid,
          pending,
          issue,

          paidPercent:
            (
              paid /
              total
            ) *
            100,

          pendingPercent:
            (
              pending /
              total
            ) *
            100,
        };
      },
      [
        orders,
      ],
    );

  const donutStyle:
    CSSProperties = {
      background:
        `conic-gradient(` +
        `#127052 0 ${paymentBreakdown.paidPercent}%, ` +
        `#d39a3a ${paymentBreakdown.paidPercent}% ` +
        `${paymentBreakdown.paidPercent + paymentBreakdown.pendingPercent}%, ` +
        `#d95f5f ${paymentBreakdown.paidPercent + paymentBreakdown.pendingPercent}% 100%)`,
    };

  async function loadOrderImages(
    order: MerchantOrderDetail,
  ) {
    const productIds =
      Array.from(
        new Set(
          order.items
            .map(
              (item) =>
                item.productId,
            )
            .filter(Boolean),
        ),
      );

    const entries =
      await Promise.all(
        productIds.map(
          async (
            productId,
          ) => {
            try {
              const result =
                await getProductImages(
                  tenantId,
                  productId,
                );

              const image =
                [...result.images]
                  .sort(
                    (a, b) => {
                      if (
                        a.isPrimary !==
                        b.isPrimary
                      ) {
                        return a.isPrimary
                          ? -1
                          : 1;
                      }

                      return (
                        a.sortOrder -
                        b.sortOrder
                      );
                    },
                  )[0];

              return [
                productId,
                image?.url ?? "",
              ] as const;
            } catch {
              return [
                productId,
                "",
              ] as const;
            }
          },
        ),
      );

    const next:
      Record<
        string,
        string
      > = {};

    for (
      const [
        productId,
        imageUrl,
      ]
      of entries
    ) {
      if (imageUrl) {
        next[
          productId
        ] =
          imageUrl;
      }
    }

    setProductImages(
      next,
    );
  }

  async function open(
    id: string,
  ) {
    if (!tenantId) {
      return;
    }

    setDetailLoading(true);
    setError("");
    setNotice("");
    setProductImages({});

    try {
      const order =
        await getMerchantOrderById(
          tenantId,
          id,
        );

      setSelected(order);

      setShippingCarrier(
        order.shippingCarrier ??
          "",
      );

      setTrackingNumber(
        order.trackingNumber ??
          "",
      );

      void loadOrderImages(
        order,
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر تحميل التفاصيل.",
      );
    } finally {
      setDetailLoading(false);
    }
  }

  async function advance(
    action: Action,
    title: string,
  ) {
    if (
      !tenantId ||
      !selected ||
      busy ||
      !window.confirm(
        `تأكيد: ${title}؟`,
      )
    ) {
      return;
    }

    setBusy(true);
    setError("");
    setNotice("");

    try {
      await changeMerchantOrderState(
        tenantId,
        selected.orderId,
        action,

        action === "ship"
          ? {
              shippingCarrier:
                shippingCarrier.trim(),

              trackingNumber:
                trackingNumber.trim(),
            }
          : undefined,
      );

      const updated =
        await getMerchantOrderById(
          tenantId,
          selected.orderId,
        );

      setSelected(updated);

      setNotice(
        `تم تحديث الطلب: ${title}`,
      );

      await load();
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر تحديث الطلب.",
      );
    } finally {
      setBusy(false);
    }
  }

  function exportCsv() {
    const rows = [
      [
        "رقم الطلب",
        "العميل",
        "حالة الطلب",
        "حالة الدفع",
        "الشحن",
        "الكمية",
        "الإجمالي",
        "العملة",
        "التاريخ",
      ],

      ...visible.map(
        (order) => [
          order.orderId,
          order.customerEmail ?? "",
          statusMeta(order).text,
          paymentMeta(order).text,
          shippingMeta(order).text,
          order.totalQuantity,
          order.totalAmount,
          order.currency,
          order.createdAtUtc,
        ],
      ),
    ];

    const content =
      "\uFEFF" +
      rows
        .map(
          (row) =>
            row
              .map(csvCell)
              .join(","),
        )
        .join("\r\n");

    const blob =
      new Blob(
        [
          content,
        ],
        {
          type:
            "text/csv;charset=utf-8",
        },
      );

    const url =
      URL.createObjectURL(
        blob,
      );

    const anchor =
      document.createElement(
        "a",
      );

    anchor.href = url;

    anchor.download =
      `rukn-orders-${new Date()
        .toISOString()
        .slice(
          0,
          10,
        )}.csv`;

    anchor.click();

    URL.revokeObjectURL(
      url,
    );
  }

  function chooseTab(
    next: Tab,
  ) {
    setTab(next);
    setPage(1);
  }

  function changeQuery(
    value: string,
  ) {
    setQuery(value);
    setPage(1);
  }

  function changeSort(
    value: Sort,
  ) {
    setSort(value);
    setPage(1);
  }

  function changeFrom(
    value: string,
  ) {
    setFrom(value);
    setPage(1);
  }

  function changeTo(
    value: string,
  ) {
    setTo(value);
    setPage(1);
  }

  function clearFilters() {
    setQuery("");
    setFrom("");
    setTo("");
    setPage(1);
  }

  const active =
    selected;

  const action: {
    id: Action;
    title: string;
  } | null =
    active
      ?.orderStatus
      .toLowerCase() ===
    "paid"
      ? {
          id: "confirm",
          title: "تأكيد الطلب",
        }
      : active
            ?.orderStatus
            .toLowerCase() ===
          "confirmed"
        ? {
            id: "processing",
            title: "بدء تجهيز الطلب",
          }
        : active
                ?.orderStatus
                .toLowerCase() ===
              "processing" &&
            active
              .fulfillmentStatus
              .toLowerCase() ===
              "processing"
          ? {
              id:
                "ready-to-ship",
              title:
                "اكتمل التجهيز",
            }
          : active
                ?.fulfillmentStatus
                .toLowerCase() ===
              "readytoship"
            ? safe(
                active.shippingMethodType,
              ) === "pickup"
              ? {
                  id: "collect",
                  title:
                    "تأكيد الاستلام",
                }
              : {
                  id: "ship",
                  title:
                    "تأكيد الشحن",
                }
            : active
                  ?.fulfillmentStatus
                  .toLowerCase() ===
                "shipped"
              ? {
                  id:
                    "in-transit",
                  title:
                    "قيد التوصيل",
                }
              : active
                    ?.fulfillmentStatus
                    .toLowerCase() ===
                  "intransit"
                ? {
                    id: "deliver",
                    title:
                      "تأكيد التسليم",
                  }
                : null;

  return (
    <main
      dir="rtl"
      className="rukn-orders-v2"
    >
      <header className="rukn-orders-v2-head">
        <div>
          <span>
            مركز العمليات
          </span>

          <h1>
            إدارة الطلبات
          </h1>

          <p>
            متابعة الطلبات من لحظة التأكيد حتى التجهيز والشحن والتسليم.
          </p>
        </div>

        <div className="rukn-orders-v2-head-actions">
          <button
            type="button"
            className="secondary"
            onClick={() =>
              setPaymentReviewOpen(
                (current) =>
                  !current,
              )
            }
          >
            <WalletCards
              size={17}
            />

            مراجعات الدفع
          </button>

          <button
            type="button"
            className="secondary"
            onClick={
              exportCsv
            }
            disabled={
              visible.length === 0
            }
          >
            <Download
              size={17}
            />

            تصدير
          </button>

          <button
            type="button"
            className="primary"
            onClick={() =>
              void load()
            }
            disabled={
              loading
            }
          >
            <RefreshCw
              size={17}
              className={
                loading
                  ? "animate-spin"
                  : ""
              }
            />

            تحديث
          </button>
        </div>
      </header>

      {blocked ? (
        <div className="rukn-orders-v2-alert warning">
          <AlertTriangle
            size={18}
          />

          <span>
            أكمل التحقق الثنائي للوصول إلى بيانات الإدارة.
          </span>

          <Link to="/admin/settings">
            إعدادات الحساب
          </Link>
        </div>
      ) : null}

      {error ? (
        <div
          role="alert"
          className="rukn-orders-v2-alert error"
        >
          <AlertTriangle
            size={18}
          />

          {error}
        </div>
      ) : null}

      {notice ? (
        <div
          role="status"
          className="rukn-orders-v2-alert success"
        >
          <CheckCircle2
            size={18}
          />

          {notice}
        </div>
      ) : null}

      {paymentReviewOpen &&
      tenantId ? (
        <section className="rukn-orders-v2-manual-review">
          <ManualPaymentReviewPanel
            tenantId={
              tenantId
            }
            onChanged={() =>
              void load()
            }
          />
        </section>
      ) : null}

      <section className="rukn-orders-v2-kpis">
        <Metric
          icon={ShoppingBag}
          label="إجمالي الطلبات"
          value={counts.all}
          note="ضمن آخر الطلبات المحملة"
          tone="green"
          active={
            tab === "all"
          }
          onClick={() =>
            chooseTab("all")
          }
        />

        <Metric
          icon={Clock3}
          label="تحتاج إجراء"
          value={
            counts.attention
          }
          note="تحتاج متابعة تشغيلية"
          tone="amber"
          active={
            tab ===
            "attention"
          }
          onClick={() =>
            chooseTab(
              "attention",
            )
          }
        />

        <Metric
          icon={Package}
          label="قيد التجهيز"
          value={
            counts.processing
          }
          note="داخل مرحلة التنفيذ"
          tone="blue"
          active={
            tab ===
            "processing"
          }
          onClick={() =>
            chooseTab(
              "processing",
            )
          }
        />

        <Metric
          icon={Truck}
          label="الشحن والتسليم"
          value={
            counts.shipping
          }
          note="جاهز أو في الطريق"
          tone="plain"
          active={
            tab ===
            "shipping"
          }
          onClick={() =>
            chooseTab(
              "shipping",
            )
          }
        />
      </section>

      <section className="rukn-orders-v2-filter-shell">
        <div className="rukn-orders-v2-search-row">
          <div className="rukn-orders-v2-search">
            <Search
              size={18}
            />

            <input
              value={query}
              onChange={(
                event,
              ) =>
                changeQuery(
                  event.target.value,
                )
              }
              placeholder="ابحث برقم الطلب، العميل، رقم التتبع أو شركة الشحن..."
            />
          </div>

          <div className="rukn-orders-v2-filter-actions">
            <label>
              <CalendarDays
                size={15}
              />

              <input
                type="date"
                value={from}
                onChange={(
                  event,
                ) =>
                  changeFrom(
                    event.target.value,
                  )
                }
                aria-label="من تاريخ"
              />
            </label>

            <label>
              <CalendarDays
                size={15}
              />

              <input
                type="date"
                value={to}
                min={
                  from ||
                  undefined
                }
                onChange={(
                  event,
                ) =>
                  changeTo(
                    event.target.value,
                  )
                }
                aria-label="إلى تاريخ"
              />
            </label>

            <label>
              <SlidersHorizontal
                size={15}
              />

              <select
                value={sort}
                onChange={(
                  event,
                ) =>
                  changeSort(
                    event.target
                      .value as Sort,
                  )
                }
              >
                <option value="latest">
                  الأحدث أولًا
                </option>

                <option value="oldest">
                  الأقدم أولًا
                </option>

                <option value="highest">
                  القيمة الأعلى
                </option>

                <option value="lowest">
                  القيمة الأقل
                </option>
              </select>
            </label>

            {query ||
            from ||
            to ? (
              <button
                type="button"
                className="clear"
                onClick={
                  clearFilters
                }
              >
                <FilterX
                  size={15}
                />

                مسح
              </button>
            ) : null}
          </div>
        </div>

        <div className="rukn-orders-v2-tabs">
          {filters.map(
            (item) => (
              <button
                key={item.id}
                type="button"
                aria-pressed={
                  tab ===
                  item.id
                }
                className={
                  tab === item.id
                    ? "active"
                    : ""
                }
                onClick={() =>
                  chooseTab(
                    item.id,
                  )
                }
              >
                <span>
                  {item.title}
                </span>

                <strong>
                  {counts[
                    item.id
                  ].toLocaleString(
                    "en-US",
                  )}
                </strong>
              </button>
            ),
          )}
        </div>
      </section>

      <section className="rukn-orders-v2-workspace">
        <section className="rukn-orders-v2-table-shell">
          <div className="rukn-orders-v2-table-tools">
            <div>
              <strong>
                الطلبات
              </strong>

              <span>
                عرض
                {" "}
                {visible.length.toLocaleString(
                  "en-US",
                )}
                {" "}
                نتيجة
              </span>
            </div>

            <div>
              <span className="rukn-orders-v2-live-dot" />

              بيانات تشغيلية مباشرة
            </div>
          </div>

          {loading ? (
            <div className="rukn-orders-v2-loading">
              {Array.from({
                length: 7,
              }).map(
                (
                  _,
                  index,
                ) => (
                  <span
                    key={index}
                  />
                ),
              )}
            </div>
          ) : visible.length ===
            0 ? (
            <div className="rukn-orders-v2-empty">
              <CheckCircle2
                size={26}
              />

              <strong>
                لا توجد طلبات ضمن هذه النتائج
              </strong>

              <p>
                غيّر الحالة أو التاريخ أو البحث لعرض طلبات أخرى.
              </p>
            </div>
          ) : (
            <>
              <div className="rukn-orders-v2-table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>
                        رقم الطلب
                      </th>

                      <th>
                        العميل
                      </th>

                      <th>
                        المنتجات
                      </th>

                      <th>
                        الإجمالي
                      </th>

                      <th>
                        الدفع
                      </th>

                      <th>
                        الشحن
                      </th>

                      <th>
                        الحالة
                      </th>

                      <th>
                        التاريخ
                      </th>

                      <th>
                        الإجراء
                      </th>
                    </tr>
                  </thead>

                  <tbody>
                    {pageRows.map(
                      (order) => {
                        const status =
                          statusMeta(
                            order,
                          );

                        const payment =
                          paymentMeta(
                            order,
                          );

                        const shipping =
                          shippingMeta(
                            order,
                          );

                        const activeRow =
                          selected
                            ?.orderId ===
                          order.orderId;

                        return (
                          <tr
                            key={
                              order.orderId
                            }
                            className={
                              activeRow
                                ? "selected"
                                : ""
                            }
                          >
                            <td>
                              <div className="rukn-orders-v2-order-id">
                                <span
                                  dir="ltr"
                                >
                                  #
                                  {shortId(
                                    order.orderId,
                                  )}
                                </span>

                                <small>
                                  {relativeTime(
                                    order.createdAtUtc,
                                  )}
                                </small>
                              </div>
                            </td>

                            <td>
                              <div className="rukn-orders-v2-customer">
                                <span>
                                  <UserRound
                                    size={15}
                                  />
                                </span>

                                <div>
                                  <strong>
                                    {order.customerEmail
                                      ? order.customerEmail.split(
                                          "@",
                                        )[0]
                                      : "عميل المتجر"}
                                  </strong>

                                  <small>
                                    {order.customerEmail ??
                                      "لا يوجد بريد"}
                                  </small>
                                </div>
                              </div>
                            </td>

                            <td>
                              <div className="rukn-orders-v2-items-count">
                                <Package
                                  size={15}
                                />

                                <strong>
                                  {order.totalQuantity.toLocaleString(
                                    "en-US",
                                  )}
                                </strong>

                                <span>
                                  قطعة
                                </span>
                              </div>
                            </td>

                            <td
                              dir="ltr"
                              className="rukn-orders-v2-money"
                            >
                              {money(
                                order.totalAmount,
                                order.currency,
                              )}
                            </td>

                            <td>
                              <Status
                                tone={
                                  payment.tone
                                }
                              >
                                <CreditCard
                                  size={13}
                                />

                                {
                                  payment.text
                                }
                              </Status>
                            </td>

                            <td>
                              <Status
                                tone={
                                  shipping.tone
                                }
                              >
                                <Truck
                                  size={13}
                                />

                                {
                                  shipping.text
                                }
                              </Status>
                            </td>

                            <td>
                              <Status
                                tone={
                                  status.tone
                                }
                              >
                                {
                                  status.text
                                }
                              </Status>
                            </td>

                            <td>
                              <div className="rukn-orders-v2-date">
                                <strong>
                                  {date(
                                    order.createdAtUtc,
                                  )}
                                </strong>
                              </div>
                            </td>

                            <td>
                              <button
                                type="button"
                                className="rukn-orders-v2-view"
                                disabled={
                                  detailLoading
                                }
                                onClick={() =>
                                  void open(
                                    order.orderId,
                                  )
                                }
                              >
                                <Eye
                                  size={15}
                                />

                                عرض
                              </button>
                            </td>
                          </tr>
                        );
                      },
                    )}
                  </tbody>
                </table>
              </div>

              <footer className="rukn-orders-v2-pagination">
                <span>
                  {(
                    (
                      safePage -
                      1
                    ) *
                      pageSize +
                    1
                  ).toLocaleString(
                    "en-US",
                  )}
                  {" - "}
                  {Math.min(
                    safePage *
                      pageSize,
                    visible.length,
                  ).toLocaleString(
                    "en-US",
                  )}
                  {" من "}
                  {visible.length.toLocaleString(
                    "en-US",
                  )}
                </span>

                <div>
                  <button
                    type="button"
                    aria-label="الصفحة السابقة"
                    disabled={
                      safePage <= 1
                    }
                    onClick={() =>
                      setPage(
                        (current) =>
                          Math.max(
                            1,
                            current -
                              1,
                          ),
                      )
                    }
                  >
                    <ChevronRight
                      size={16}
                    />
                  </button>

                  {Array.from({
                    length:
                      Math.min(
                        pageCount,
                        5,
                      ),
                  }).map(
                    (
                      _,
                      index,
                    ) => {
                      const value =
                        index + 1;

                      return (
                        <button
                          key={value}
                          type="button"
                          className={
                            value ===
                            safePage
                              ? "active"
                              : ""
                          }
                          onClick={() =>
                            setPage(
                              value,
                            )
                          }
                        >
                          {value}
                        </button>
                      );
                    },
                  )}

                  {pageCount >
                  5 ? (
                    <span>
                      …
                    </span>
                  ) : null}

                  <button
                    type="button"
                    aria-label="الصفحة التالية"
                    disabled={
                      safePage >=
                      pageCount
                    }
                    onClick={() =>
                      setPage(
                        (current) =>
                          Math.min(
                            pageCount,
                            current +
                              1,
                          ),
                      )
                    }
                  >
                    <ChevronLeft
                      size={16}
                    />
                  </button>
                </div>
              </footer>
            </>
          )}
        </section>

        <aside
          className={`rukn-orders-v2-detail ${active ? "is-open" : ""}`}
          aria-hidden={!active}
        >
          {!active ? (
            <div className="rukn-orders-v2-detail-empty">
              <span>
                <Eye
                  size={23}
                />
              </span>

              <strong>
                تفاصيل الطلب
              </strong>

              <p>
                اختر أي طلب من الجدول لعرض العميل والدفع والشحن ومراحل التنفيذ.
              </p>
            </div>
          ) : (
            <>
              <header className="rukn-orders-v2-detail-head">
                <button
                  type="button"
                  className="rukn-orders-v22-detail-close"
                  aria-label="إغلاق تفاصيل الطلب"
                  onClick={() =>
                    setSelected(null)
                  }
                >
                  ×
                </button>

                <Status
                  tone={
                    statusMeta(
                      active,
                    ).tone
                  }
                >
                  {
                    statusMeta(
                      active,
                    ).text
                  }
                </Status>

                <h2
                  dir="ltr"
                >
                  #
                  {shortId(
                    active.orderId,
                  )}
                </h2>

                <p>
                  {date(
                    active.createdAtUtc,
                  )}
                </p>
              </header>

              <div className="rukn-orders-v2-detail-scroll">
                <section className="rukn-orders-v2-detail-section">
                  <h3>
                    معلومات العميل
                  </h3>

                  <div className="rukn-orders-v2-person">
                    <span>
                      <UserRound
                        size={19}
                      />
                    </span>

                    <div>
                      <strong>
                        {active.shippingRecipientName ??
                          active.customerEmail?.split(
                            "@",
                          )[0] ??
                          "عميل المتجر"}
                      </strong>

                      <p>
                        {active.customerEmail ??
                          "لا يوجد بريد"}
                      </p>

                      <small
                        dir="ltr"
                      >
                        {active.shippingRecipientPhone ??
                          "—"}
                      </small>
                    </div>
                  </div>
                </section>

                <section className="rukn-orders-v2-detail-section">
                  <h3>
                    معلومات الطلب
                  </h3>

                  <div className="rukn-orders-v2-detail-grid">
                    <DetailFact
                      label="الإجمالي"
                      value={
                        money(
                          active.totalAmount,
                          active.currency,
                        )
                      }
                    />

                    <DetailFact
                      label="عدد المنتجات"
                      value={`${active.totalQuantity.toLocaleString(
                        "en-US",
                      )} قطعة`}
                    />

                    <DetailFact
                      label="حالة الدفع"
                      value={
                        paymentMeta(
                          active,
                        ).text
                      }
                    />

                    <DetailFact
                      label="حالة التنفيذ"
                      value={
                        statusMeta(
                          active,
                        ).text
                      }
                    />
                  </div>
                </section>

                <section className="rukn-orders-v2-detail-section">
                  <h3>
                    معلومات الشحن
                  </h3>

                  <div className="rukn-orders-v2-detail-lines">
                    <DetailLine
                      icon={MapPin}
                      label="الوجهة"
                      value={
                        destination(
                          active,
                        )
                      }
                    />

                    <DetailLine
                      icon={Truck}
                      label="طريقة التسليم"
                      value={
                        active.shippingMethodName ??
                        "غير محددة"
                      }
                    />

                    <DetailLine
                      icon={
                        PackageCheck
                      }
                      label="شركة الشحن"
                      value={
                        active.shippingCarrier ??
                        "لم تبدأ عملية الشحن"
                      }
                    />

                    <DetailLine
                      icon={Store}
                      label="رقم التتبع"
                      value={
                        active.trackingNumber ??
                        "غير متوفر"
                      }
                    />
                  </div>
                </section>

                <section className="rukn-orders-v2-detail-section">
                  <h3>
                    المنتجات
                  </h3>

                  <div className="rukn-orders-v2-detail-products">
                    {active.items.map(
                      (item) => {
                        const imageUrl =
                          productImages[
                            item.productId
                          ];

                        return (
                          <article
                            key={
                              item.orderItemId
                            }
                          >
                            <span className="rukn-orders-v2-product-thumb">
                              {imageUrl ? (
                                <img
                                  src={
                                    imageUrl
                                  }
                                  alt=""
                                />
                              ) : (
                                <Package
                                  size={16}
                                />
                              )}
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
                                {" × "}
                                {item.quantity.toLocaleString(
                                  "en-US",
                                )}
                              </p>
                            </div>

                            <strong
                              dir="ltr"
                            >
                              {money(
                                item.lineTotal,
                                item.currency,
                              )}
                            </strong>
                          </article>
                        );
                      },
                    )}
                  </div>
                </section>

                <section className="rukn-orders-v2-detail-section">
                  <h3>
                    مراحل التنفيذ
                  </h3>

                  <ol className="rukn-orders-v2-timeline">
                    {[...active.timeline]
                      .sort(
                        (
                          a,
                          b,
                        ) =>
                          b.createdAtUtc.localeCompare(
                            a.createdAtUtc,
                          ),
                      )
                      .slice(0, 6)
                      .map(
                        (
                          entry,
                          index,
                        ) => (
                          <li
                            key={`${entry.createdAtUtc}-${index}`}
                          >
                            <i />

                            <div>
                              <strong>
                                {entry.note ||
                                  entry.fulfillmentStatus ||
                                  entry.orderStatus}
                              </strong>

                              <span>
                                {date(
                                  entry.createdAtUtc,
                                )}
                              </span>
                            </div>
                          </li>
                        ),
                      )}
                  </ol>
                </section>

                {action?.id ===
                "ship" ? (
                  <section className="rukn-orders-v2-detail-section">
                    <h3>
                      بيانات الشحن
                    </h3>

                    <div className="rukn-orders-v2-shipping-fields">
                      <label>
                        <span>
                          شركة التوصيل
                        </span>

                        <input
                          maxLength={120}
                          value={
                            shippingCarrier
                          }
                          onChange={(
                            event,
                          ) =>
                            setShippingCarrier(
                              event.target.value,
                            )
                          }
                        />
                      </label>

                      <label>
                        <span>
                          رقم التتبع
                        </span>

                        <input
                          maxLength={200}
                          value={
                            trackingNumber
                          }
                          onChange={(
                            event,
                          ) =>
                            setTrackingNumber(
                              event.target.value,
                            )
                          }
                        />
                      </label>
                    </div>
                  </section>
                ) : null}
              </div>

              <footer className="rukn-orders-v2-detail-footer">
                {action ? (
                  <button
                    type="button"
                    className="primary"
                    disabled={
                      busy ||
                      (
                        action.id ===
                          "ship" &&
                        (
                          !shippingCarrier.trim() ||
                          !trackingNumber.trim()
                        )
                      )
                    }
                    onClick={() =>
                      void advance(
                        action.id,
                        action.title,
                      )
                    }
                  >
                    <Check
                      size={17}
                    />

                    {busy
                      ? "جاري الحفظ..."
                      : action.title}

                    <ArrowLeft
                      size={16}
                    />
                  </button>
                ) : (
                  <span>
                    <CheckCircle2
                      size={16}
                    />

                    لا يوجد إجراء مطلوب حاليًا
                  </span>
                )}
              </footer>
            </>
          )}
        </aside>
      </section>

      <section className="rukn-orders-v2-insights">
        <article className="rukn-orders-v2-insight">
          <header>
            <div>
              <span>
                مراحل الطلبات
              </span>

              <h2>
                توزيع التنفيذ
              </h2>
            </div>

            <BarChart3
              size={19}
            />
          </header>

          <StageRow
            label="تحتاج إجراء"
            value={
              counts.attention
            }
            total={counts.all}
            tone="amber"
          />

          <StageRow
            label="قيد التجهيز"
            value={
              counts.processing
            }
            total={counts.all}
            tone="green"
          />

          <StageRow
            label="الشحن والتسليم"
            value={
              counts.shipping
            }
            total={counts.all}
            tone="blue"
          />

          <StageRow
            label="مكتملة / مؤرشفة"
            value={
              counts.archive
            }
            total={counts.all}
            tone="gray"
          />
        </article>

        <article className="rukn-orders-v2-insight">
          <header>
            <div>
              <span>
                آخر 7 أيام
              </span>

              <h2>
                نشاط الطلبات
              </h2>
            </div>

            <CalendarDays
              size={19}
            />
          </header>

          <div className="rukn-orders-v2-bars">
            {lastSevenDays.map(
              (item) => (
                <div
                  key={
                    item.key
                  }
                >
                  <span>
                    {item.count > 0
                      ? item.count.toLocaleString(
                          "en-US",
                        )
                      : ""}
                  </span>

                  <i
                    style={{
                      height:
                        `${Math.max(
                          8,
                          (
                            item.count /
                            maxDailyOrders
                          ) *
                            100,
                        )}%`,
                    }}
                  />

                  <small>
                    {
                      item.label
                    }
                  </small>
                </div>
              ),
            )}
          </div>
        </article>

        <article className="rukn-orders-v2-insight">
          <header>
            <div>
              <span>
                الطلبات المحملة
              </span>

              <h2>
                حالات الدفع
              </h2>
            </div>

            <WalletCards
              size={19}
            />
          </header>

          <div className="rukn-orders-v2-payment-chart">
            <div
              className="rukn-orders-v2-donut"
              style={
                donutStyle
              }
            >
              <div>
                <strong>
                  {counts.all.toLocaleString(
                    "en-US",
                  )}
                </strong>

                <span>
                  طلب
                </span>
              </div>
            </div>

            <div className="rukn-orders-v2-payment-legend">
              <Legend
                tone="green"
                label="مدفوع"
                value={
                  paymentBreakdown.paid
                }
              />

              <Legend
                tone="amber"
                label="قيد المراجعة"
                value={
                  paymentBreakdown.pending
                }
              />

              <Legend
                tone="red"
                label="مشكلة / استرداد"
                value={
                  paymentBreakdown.issue
                }
              />
            </div>
          </div>
        </article>
      </section>

      <p className="rukn-orders-v2-footnote">
        تعرض الصفحة آخر 100 طلب محمّل من الخادم. البحث والتصفية والتصدير يعملون على هذه المجموعة الحالية.
      </p>
    </main>
  );
}

function Metric({
  icon: Icon,
  label,
  value,
  note,
  tone,
  active,
  onClick,
}: {
  icon: LucideIcon;
  label: string;
  value: number;
  note: string;
  tone:
    | "green"
    | "amber"
    | "blue"
    | "plain";
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      className="rukn-orders-v2-metric"
      data-tone={tone}
      data-active={active}
      onClick={onClick}
    >
      <span className="icon">
        <Icon
          size={21}
        />
      </span>

      <div>
        <span>
          {label}
        </span>

        <strong>
          {value.toLocaleString(
            "en-US",
          )}
        </strong>

        <small>
          {note}
        </small>
      </div>

      <ChevronLeft
        size={17}
      />
    </button>
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
      className="rukn-orders-v2-status"
      data-tone={tone}
    >
      {children}
    </span>
  );
}

function DetailFact({
  label,
  value,
}: {
  label: string;
  value: string;
}) {
  return (
    <div>
      <span>
        {label}
      </span>

      <strong>
        {value}
      </strong>
    </div>
  );
}

function DetailLine({
  icon: Icon,
  label,
  value,
}: {
  icon: LucideIcon;
  label: string;
  value: string;
}) {
  return (
    <div>
      <Icon
        size={16}
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

function StageRow({
  label,
  value,
  total,
  tone,
}: {
  label: string;
  value: number;
  total: number;
  tone:
    | "green"
    | "amber"
    | "blue"
    | "gray";
}) {
  const percent =
    total > 0
      ? Math.round(
          (
            value /
            total
          ) *
            100,
        )
      : 0;

  return (
    <div className="rukn-orders-v2-stage">
      <div>
        <span>
          {label}
        </span>

        <strong>
          {value.toLocaleString(
            "en-US",
          )}
        </strong>
      </div>

      <div>
        <i
          data-tone={tone}
          style={{
            width:
              `${percent}%`,
          }}
        />
      </div>

      <small>
        {percent.toLocaleString(
          "en-US",
        )}
        %
      </small>
    </div>
  );
}

function Legend({
  tone,
  label,
  value,
}: {
  tone:
    | "green"
    | "amber"
    | "red";
  label: string;
  value: number;
}) {
  return (
    <div className="rukn-orders-v2-legend">
      <i
        data-tone={tone}
      />

      <span>
        {label}
      </span>

      <strong>
        {value.toLocaleString(
          "en-US",
        )}
      </strong>
    </div>
  );
}