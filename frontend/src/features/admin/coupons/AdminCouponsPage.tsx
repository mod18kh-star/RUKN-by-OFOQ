import {
  BarChart3,
  CalendarClock,
  Check,
  CheckCircle2,
  CirclePause,
  Copy,
  FilterX,
  Layers3,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Store,
  Tag,
  TicketPercent,
  Users,
  X,
  type LucideIcon,
} from "lucide-react";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";

import {
  readAdminStore,
} from "../store-setup/storeSetupStorage";

import {
  getProducts,
  type Product,
} from "../products/productsApi";

import {
  getCategories,
  type AdminCategory,
} from "../catalog/catalogContentApi";

import {
  createCoupon,
  getCouponAnalytics,
  listCoupons,
  updateCoupon,
  type Coupon,
  type CouponAnalytics,
  type CouponInput,
  type CouponScope,
} from "./couponsApi";

import "./AdminCouponsV2.css";

type CouponState =
  | "active"
  | "scheduled"
  | "paused"
  | "expired";

type CouponFilter =
  | "all"
  | CouponState;

type ScopeFilter =
  | "all"
  | CouponScope;

const initial: CouponInput = {
  code: "",
  name: "",
  type: "Percentage",
  value: 10,
  scope: "EntireStore",
  currency: "",
  includeDescendantCategories: true,
  minimumOrderAmount: null,
  maximumTotalUses: null,
  maximumUsesPerCustomer: 1,
  startsAtUtc: null,
  endsAtUtc: null,
  isEnabled: true,
  productIds: [],
  categoryIds: [],
};

const scopeLabels: Record<
  CouponScope,
  string
> = {
  EntireStore: "المتجر بالكامل",
  Categories: "أقسام محددة",
  Products: "منتجات محددة",
};

const fresh = (): CouponInput => ({
  ...initial,
  productIds: [],
  categoryIds: [],
});

function money(
  value: number,
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
    ).format(value);
  } catch {
    return `${value.toLocaleString(
      "en-US",
    )} ${currency}`;
  }
}

function number(
  value: number,
) {
  return value.toLocaleString(
    "en-US",
  );
}

function localDate(
  value: string | null,
) {
  if (!value) {
    return "";
  }

  const date =
    new Date(value);

  return new Date(
    date.getTime() -
      date.getTimezoneOffset() *
        60000,
  )
    .toISOString()
    .slice(
      0,
      16,
    );
}

function utcDate(
  value: string,
) {
  return value
    ? new Date(value).toISOString()
    : null;
}

function formatDate(
  value: string | null,
  withTime = false,
) {
  if (!value) {
    return "بدون تاريخ";
  }

  const date =
    new Date(value);

  if (
    Number.isNaN(
      date.getTime(),
    )
  ) {
    return "—";
  }

  return new Intl.DateTimeFormat(
    "ar-SA-u-nu-latn",
    withTime
      ? {
          day: "2-digit",
          month: "short",
          year: "numeric",
          hour: "2-digit",
          minute: "2-digit",
        }
      : {
          day: "2-digit",
          month: "short",
          year: "numeric",
        },
  ).format(date);
}

function couponState(
  coupon: Coupon,
): CouponState {
  if (!coupon.isEnabled) {
    return "paused";
  }

  const now =
    Date.now();

  if (
    coupon.startsAtUtc &&
    new Date(
      coupon.startsAtUtc,
    ).getTime() > now
  ) {
    return "scheduled";
  }

  if (
    coupon.endsAtUtc &&
    new Date(
      coupon.endsAtUtc,
    ).getTime() < now
  ) {
    return "expired";
  }

  return "active";
}

function stateMeta(
  state: CouponState,
) {
  if (state === "active") {
    return {
      label: "نشط الآن",
      tone: "success",
    };
  }

  if (state === "scheduled") {
    return {
      label: "مجدول",
      tone: "info",
    };
  }

  if (state === "expired") {
    return {
      label: "منتهي",
      tone: "neutral",
    };
  }

  return {
    label: "متوقف",
    tone: "warning",
  };
}

function discountLabel(
  coupon: Coupon,
) {
  return coupon.type ===
    "Percentage"
    ? `${number(
        coupon.value,
      )}%`
    : money(
        coupon.value,
        coupon.currency,
      );
}

function couponToInput(
  coupon: Coupon,
): CouponInput {
  return {
    code:
      coupon.code,
    name:
      coupon.name,
    type:
      coupon.type,
    value:
      coupon.value,
    scope:
      coupon.scope,
    currency:
      coupon.currency,
    includeDescendantCategories:
      coupon.includeDescendantCategories,
    minimumOrderAmount:
      coupon.minimumOrderAmount,
    maximumTotalUses:
      coupon.maximumTotalUses,
    maximumUsesPerCustomer:
      coupon.maximumUsesPerCustomer,
    startsAtUtc:
      coupon.startsAtUtc,
    endsAtUtc:
      coupon.endsAtUtc,
    isEnabled:
      coupon.isEnabled,
    productIds: [
      ...coupon.productIds,
    ],
    categoryIds: [
      ...coupon.categoryIds,
    ],
  };
}

function targetSummary(
  coupon: Coupon,
) {
  if (
    coupon.scope ===
    "EntireStore"
  ) {
    return "كل المنتجات";
  }

  if (
    coupon.scope ===
    "Products"
  ) {
    return `${number(
      coupon.productIds.length,
    )} منتج`;
  }

  return `${number(
    coupon.categoryIds.length,
  )} قسم`;
}

function validityLabel(
  coupon: Coupon,
) {
  if (
    !coupon.startsAtUtc &&
    !coupon.endsAtUtc
  ) {
    return "بدون مدة محددة";
  }

  if (
    coupon.startsAtUtc &&
    coupon.endsAtUtc
  ) {
    return `${formatDate(
      coupon.startsAtUtc,
    )} — ${formatDate(
      coupon.endsAtUtc,
    )}`;
  }

  if (coupon.startsAtUtc) {
    return `يبدأ ${formatDate(
      coupon.startsAtUtc,
    )}`;
  }

  return `ينتهي ${formatDate(
    coupon.endsAtUtc,
  )}`;
}

function orderStateLabel(
  value: string,
) {
  const state =
    value
      .trim()
      .toLowerCase();

  if (
    state.includes(
      "cancel",
    )
  ) {
    return "ملغي";
  }

  if (
    state.includes(
      "pending",
    )
  ) {
    return "بانتظار الدفع";
  }

  if (
    state.includes(
      "paid",
    ) ||
    state.includes(
      "confirm",
    ) ||
    state.includes(
      "processing",
    )
  ) {
    return "مدفوع / قيد التنفيذ";
  }

  return value;
}

export function AdminCouponsPage() {
  const tenantId =
    readAdminStore()
      ?.tenantId ??
    null;

  const [
    coupons,
    setCoupons,
  ] =
    useState<
      Coupon[]
    >([]);

  const [
    products,
    setProducts,
  ] =
    useState<
      Product[]
    >([]);

  const [
    categories,
    setCategories,
  ] =
    useState<
      AdminCategory[]
    >([]);

  const [
    form,
    setForm,
  ] =
    useState<CouponInput>(
      fresh,
    );

  const [
    editId,
    setEditId,
  ] =
    useState<
      string |
      null
    >(null);

  const [
    editorOpen,
    setEditorOpen,
  ] =
    useState(false);

  const [
    detailsId,
    setDetailsId,
  ] =
    useState<
      string |
      null
    >(null);

  const [
    analytics,
    setAnalytics,
  ] =
    useState<
      CouponAnalytics |
      null
    >(null);

  const [
    loading,
    setLoading,
  ] =
    useState(
      Boolean(
        tenantId,
      ),
    );

  const [
    busy,
    setBusy,
  ] =
    useState(false);

  const [
    detailsBusy,
    setDetailsBusy,
  ] =
    useState(false);

  const [
    error,
    setError,
  ] =
    useState("");

  const [
    notice,
    setNotice,
  ] =
    useState("");

  const [
    search,
    setSearch,
  ] =
    useState("");

  const [
    stateFilter,
    setStateFilter,
  ] =
    useState<CouponFilter>(
      "all",
    );

  const [
    scopeFilter,
    setScopeFilter,
  ] =
    useState<ScopeFilter>(
      "all",
    );

  const [
    targetSearch,
    setTargetSearch,
  ] =
    useState("");

  const activeCoupon =
    useMemo(
      () =>
        coupons.find(
          (coupon) =>
            coupon.id ===
            detailsId,
        ) ??
        null,
      [
        coupons,
        detailsId,
      ],
    );

  const storeCurrencies =
    useMemo(
      () =>
        Array.from(
          new Set(
            products
              .flatMap(
                (
                  product,
                ) => [
                  product.currency,

                  ...product.variants
                    .filter(
                      (
                        variant,
                      ) =>
                        variant.isEnabled &&
                        variant.priceOverrideCurrency,
                    )
                    .map(
                      (
                        variant,
                      ) =>
                        variant.priceOverrideCurrency!,
                    ),
                ],
              )
              .filter(
                (
                  currency,
                ) =>
                  /^[A-Z]{3}$/.test(
                    currency,
                  ),
              ),
          ),
        ),
      [
        products,
      ],
    );

  const storeCurrency =
    storeCurrencies.length ===
    1
      ? storeCurrencies[0]
      : null;

  const load =
    useCallback(
      async () => {
        if (!tenantId) {
          setCoupons([]);
          setProducts([]);
          setCategories([]);
          setLoading(false);
          return;
        }

        setLoading(true);
        setError("");

        try {
          const [
            nextCoupons,
            nextProducts,
            nextCategories,
          ] =
            await Promise.all([
              listCoupons(
                tenantId,
              ),

              getProducts(
                tenantId,
              ),

              getCategories(
                tenantId,
              ),
            ]);

          setCoupons(
            nextCoupons,
          );

          setProducts(
            nextProducts,
          );

          setCategories(
            nextCategories,
          );
        } catch (caught) {
          setError(
            caught instanceof Error
              ? caught.message
              : "تعذر تحميل بيانات الكوبونات.",
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
        !editorOpen &&
        !detailsId
      ) {
        return;
      }

      const current =
        document.body.style.overflow;

      document.body.style.overflow =
        "hidden";

      return () => {
        document.body.style.overflow =
          current;
      };
    },
    [
      editorOpen,
      detailsId,
    ],
  );

  const counts =
    useMemo(
      () => {
        const result = {
          all:
            coupons.length,
          active:
            0,
          scheduled:
            0,
          paused:
            0,
          expired:
            0,
        };

        for (
          const coupon
          of coupons
        ) {
          result[
            couponState(
              coupon,
            )
          ] += 1;
        }

        return result;
      },
      [
        coupons,
      ],
    );

  const visible =
    useMemo(
      () => {
        const term =
          search
            .trim()
            .toLowerCase();

        return coupons.filter(
          (
            coupon,
          ) => {
            const state =
              couponState(
                coupon,
              );

            if (
              stateFilter !==
                "all" &&
              state !==
                stateFilter
            ) {
              return false;
            }

            if (
              scopeFilter !==
                "all" &&
              coupon.scope !==
                scopeFilter
            ) {
              return false;
            }

            if (!term) {
              return true;
            }

            return [
              coupon.code,
              coupon.name,
              scopeLabels[
                coupon.scope
              ],
            ].some(
              (
                value,
              ) =>
                value
                  .toLowerCase()
                  .includes(
                    term,
                  ),
            );
          },
        );
      },
      [
        coupons,
        search,
        stateFilter,
        scopeFilter,
      ],
    );

  const patch =
    <K extends keyof CouponInput>(
      key: K,
      value:
        CouponInput[K],
    ) => {
      setForm(
        (
          current,
        ) => ({
          ...current,
          [key]:
            value,
        }),
      );
    };

  function begin(
    coupon?: Coupon,
  ) {
    setForm(
      coupon
        ? couponToInput(
            coupon,
          )
        : {
            ...fresh(),
            currency:
              storeCurrency ??
              "",
          },
    );

    setEditId(
      coupon?.id ??
        null,
    );

    setTargetSearch("");
    setDetailsId(null);
    setAnalytics(null);
    setError("");
    setNotice("");
    setEditorOpen(true);
  }

  function closeEditor() {
    setEditorOpen(false);
    setEditId(null);
    setError("");
  }

  function toggleTarget(
    id: string,
    fieldName:
      | "productIds"
      | "categoryIds",
  ) {
    setForm(
      (
        current,
      ) => {
        const ids =
          current[
            fieldName
          ];

        return {
          ...current,

          [fieldName]:
            ids.includes(id)
              ? ids.filter(
                  (
                    currentId,
                  ) =>
                    currentId !==
                    id,
                )
              : [
                  ...ids,
                  id,
                ],
        };
      },
    );
  }

  function validate():
    | string
    | null {
    const code =
      form.code
        .trim()
        .toUpperCase();

    if (
      !/^[A-Z0-9][A-Z0-9_-]{1,59}$/.test(
        code,
      )
    ) {
      return "رمز الكوبون: 2–60 حرفًا إنجليزيًا أو رقمًا أو شرطة، بدون فراغات.";
    }

    if (
      !form.name.trim() ||
      form.name.trim().length >
        160
    ) {
      return "أدخل اسمًا واضحًا للكوبون (حتى 160 حرفًا).";
    }

    if (
      !Number.isFinite(
        form.value,
      ) ||
      form.value <= 0 ||
      (
        form.type ===
          "Percentage" &&
        form.value > 100
      )
    ) {
      return "قيمة الخصم يجب أن تكون موجبة، والنسبة لا تتجاوز 100%.";
    }

    if (
      form.type ===
        "FixedAmount" &&
      form.value >
        100000000
    ) {
      return "قيمة الخصم الثابت كبيرة جدًا.";
    }

    if (!storeCurrency) {
      return storeCurrencies.length ===
        0
        ? "لا يمكن تحديد عملة المتجر قبل إضافة منتج بسعر وعملة. أضف منتجًا أولًا."
        : "المتجر يحتوي على منتجات بعملات مختلفة. وحّد عملة المنتجات قبل إنشاء كوبون على المتجر.";
    }

    if (
      editId &&
      form.currency !==
        storeCurrency
    ) {
      return "عملة الكوبون القديم مختلفة عن عملة المتجر الحالية؛ أنشئ كوبونًا جديدًا بدل تغيير تاريخ كوبون مستخدم.";
    }

    if (
      form.minimumOrderAmount !==
        null &&
      (
        !Number.isFinite(
          form.minimumOrderAmount,
        ) ||
        form.minimumOrderAmount <
          0
      )
    ) {
      return "الحد الأدنى للطلب غير صالح.";
    }

    for (
      const limit
      of [
        form.maximumTotalUses,
        form.maximumUsesPerCustomer,
      ]
    ) {
      if (
        limit !== null &&
        (
          !Number.isSafeInteger(
            limit,
          ) ||
          limit <= 0
        )
      ) {
        return "حد الاستخدام يجب أن يكون عددًا صحيحًا موجبًا.";
      }
    }

    if (
      form.scope ===
        "Categories" &&
      !form.categoryIds.length
    ) {
      return "حدد قسمًا واحدًا على الأقل.";
    }

    if (
      form.scope ===
        "Products" &&
      !form.productIds.length
    ) {
      return "حدد منتجًا واحدًا على الأقل.";
    }

    if (
      form.startsAtUtc &&
      form.endsAtUtc &&
      form.startsAtUtc >=
        form.endsAtUtc
    ) {
      return "تاريخ الانتهاء يجب أن يكون بعد البداية.";
    }

    return null;
  }

  async function save(
    event:
      FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    if (
      !tenantId ||
      busy
    ) {
      return;
    }

    const invalid =
      validate();

    if (invalid) {
      setError(
        invalid,
      );

      return;
    }

    const input:
      CouponInput = {
      ...form,

      currency:
        storeCurrency!,

      code:
        form.code
          .trim()
          .toUpperCase(),

      name:
        form.name
          .trim(),

      productIds:
        form.scope ===
          "Products"
          ? form.productIds
          : [],

      categoryIds:
        form.scope ===
          "Categories"
          ? form.categoryIds
          : [],

      includeDescendantCategories:
        form.scope ===
          "Categories" &&
        form.includeDescendantCategories,
    };

    setBusy(true);
    setError("");
    setNotice("");

    try {
      const editing =
        Boolean(
          editId,
        );

      if (editId) {
        await updateCoupon(
          tenantId,
          editId,
          input,
        );
      } else {
        await createCoupon(
          tenantId,
          input,
        );
      }

      await load();

      setEditorOpen(false);
      setEditId(null);

      setNotice(
        editing
          ? "تم تحديث الكوبون."
          : "تم إنشاء الكوبون بنجاح.",
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر حفظ الكوبون.",
      );
    } finally {
      setBusy(false);
    }
  }

  async function toggle(
    coupon: Coupon,
  ) {
    if (
      !tenantId ||
      busy
    ) {
      return;
    }

    const nextState =
      coupon.isEnabled
        ? "إيقاف"
        : "تفعيل";

    if (
      !window.confirm(
        `${nextState} الكوبون ${coupon.code}؟`,
      )
    ) {
      return;
    }

    setBusy(true);
    setError("");
    setNotice("");

    try {
      await updateCoupon(
        tenantId,
        coupon.id,
        {
          ...couponToInput(
            coupon,
          ),

          isEnabled:
            !coupon.isEnabled,
        },
      );

      await load();

      setNotice(
        coupon.isEnabled
          ? "تم إيقاف الكوبون."
          : "تم تفعيل الكوبون.",
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر تغيير حالة الكوبون.",
      );
    } finally {
      setBusy(false);
    }
  }

  async function details(
    id: string,
  ) {
    if (!tenantId) {
      return;
    }

    setEditorOpen(false);
    setEditId(null);
    setDetailsId(id);
    setAnalytics(null);
    setDetailsBusy(true);
    setError("");

    try {
      setAnalytics(
        await getCouponAnalytics(
          tenantId,
          id,
        ),
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر جلب إحصائيات الكوبون.",
      );
    } finally {
      setDetailsBusy(false);
    }
  }

  function closeDetails() {
    setDetailsId(null);
    setAnalytics(null);
  }

  async function copyCode(
    code: string,
  ) {
    try {
      await navigator.clipboard.writeText(
        code,
      );

      setNotice(
        `تم نسخ الكود ${code}.`,
      );
    } catch {
      setError(
        "تعذر نسخ الكود.",
      );
    }
  }

  function resetFilters() {
    setSearch("");
    setStateFilter(
      "all",
    );
    setScopeFilter(
      "all",
    );
  }

  return (
    <main
      dir="rtl"
      className="rukn-coupons-v2"
    >
      <header className="rukn-coupons-v2-head">
        <div>
          <span>
            إدارة العروض
          </span>

          <h1>
            الكوبونات والخصومات
          </h1>

          <p>
            أنشئ العروض، حدّد نطاقها ومدة صلاحيتها، وراجع استخدامها الفعلي من مكان واحد.
          </p>
        </div>

        <button
          type="button"
          className="rukn-coupons-v2-create"
          onClick={() =>
            begin()
          }
        >
          <Plus
            size={17}
          />

          إنشاء كوبون
        </button>
      </header>

      {error ? (
        <div
          role="alert"
          className="rukn-coupons-v2-alert is-error"
        >
          {error}
        </div>
      ) : null}

      {notice ? (
        <div
          role="status"
          className="rukn-coupons-v2-alert is-success"
        >
          <Check
            size={15}
          />

          {notice}
        </div>
      ) : null}

      {!tenantId ? (
        <div className="rukn-coupons-v2-alert is-warning">
          اختر متجرًا من لوحة الإدارة أولًا.
        </div>
      ) : null}

      <section className="rukn-coupons-v2-metrics">
        <Metric
          icon={
            TicketPercent
          }
          title="كل الكوبونات"
          value={
            counts.all
          }
          note="إجمالي القائمة الحالية"
          active={
            stateFilter ===
            "all"
          }
          onClick={() =>
            setStateFilter(
              "all",
            )
          }
        />

        <Metric
          icon={
            CheckCircle2
          }
          title="نشطة الآن"
          value={
            counts.active
          }
          note="متاحة للاستخدام"
          tone="success"
          active={
            stateFilter ===
            "active"
          }
          onClick={() =>
            setStateFilter(
              "active",
            )
          }
        />

        <Metric
          icon={
            CalendarClock
          }
          title="مجدولة"
          value={
            counts.scheduled
          }
          note="ستبدأ لاحقًا"
          tone="info"
          active={
            stateFilter ===
            "scheduled"
          }
          onClick={() =>
            setStateFilter(
              "scheduled",
            )
          }
        />

        <Metric
          icon={
            CirclePause
          }
          title="غير نشطة"
          value={
            counts.paused +
            counts.expired
          }
          note={`${number(
            counts.paused,
          )} متوقف · ${number(
            counts.expired,
          )} منتهي`}
          tone="warning"
          active={
            stateFilter ===
              "paused" ||
            stateFilter ===
              "expired"
          }
          onClick={() =>
            setStateFilter(
              "paused",
            )
          }
        />
      </section>

      <section className="rukn-coupons-v2-workspace">
        <div className="rukn-coupons-v2-toolbar">
          <div className="rukn-coupons-v2-search">
            <Search
              size={17}
            />

            <input
              value={
                search
              }
              onChange={(
                event,
              ) =>
                setSearch(
                  event.target.value,
                )
              }
              placeholder="ابحث باسم الحملة أو كود الكوبون..."
            />
          </div>

          <div className="rukn-coupons-v2-toolbar-actions">
            <select
              value={
                stateFilter
              }
              onChange={(
                event,
              ) =>
                setStateFilter(
                  event.target
                    .value as
                    CouponFilter,
                )
              }
              aria-label="حالة الكوبون"
            >
              <option value="all">
                كل الحالات
              </option>

              <option value="active">
                نشط الآن
              </option>

              <option value="scheduled">
                مجدول
              </option>

              <option value="paused">
                متوقف
              </option>

              <option value="expired">
                منتهي
              </option>
            </select>

            <select
              value={
                scopeFilter
              }
              onChange={(
                event,
              ) =>
                setScopeFilter(
                  event.target
                    .value as
                    ScopeFilter,
                )
              }
              aria-label="نطاق الكوبون"
            >
              <option value="all">
                كل النطاقات
              </option>

              <option value="EntireStore">
                المتجر بالكامل
              </option>

              <option value="Categories">
                أقسام
              </option>

              <option value="Products">
                منتجات
              </option>
            </select>

            {search ||
            stateFilter !==
              "all" ||
            scopeFilter !==
              "all" ? (
              <button
                type="button"
                className="rukn-coupons-v2-clear"
                onClick={
                  resetFilters
                }
              >
                <FilterX
                  size={15}
                />

                مسح
              </button>
            ) : null}

            <button
              type="button"
              className="rukn-coupons-v2-refresh"
              disabled={
                loading ||
                busy
              }
              onClick={() =>
                void load()
              }
              aria-label="تحديث الكوبونات"
              title="تحديث"
            >
              <RefreshCw
                size={16}
                className={
                  loading
                    ? "is-spinning"
                    : ""
                }
              />
            </button>
          </div>
        </div>

        <div className="rukn-coupons-v2-result-bar">
          <span>
            عرض
            {" "}
            <strong>
              {number(
                visible.length,
              )}
            </strong>
            {" "}
            من
            {" "}
            {number(
              coupons.length,
            )}
            {" "}
            كوبون
          </span>

          <span>
            البيانات والتقارير من نظام الكوبونات الفعلي
          </span>
        </div>

        {loading ? (
          <div className="rukn-coupons-v2-loading">
            {Array.from({
              length: 5,
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
        ) : visible.length ===
          0 ? (
          <div className="rukn-coupons-v2-empty">
            <span>
              <TicketPercent
                size={22}
              />
            </span>

            <h2>
              {coupons.length ===
              0
                ? "ما عندك كوبونات حتى الآن"
                : "لا توجد نتائج بهذه الفلاتر"}
            </h2>

            <p>
              {coupons.length ===
              0
                ? "أنشئ أول كوبون وحدد الخصم والنطاق والمدة وحدود الاستخدام."
                : "غيّر البحث أو الفلاتر لعرض كوبونات أخرى."}
            </p>

            {coupons.length ===
            0 ? (
              <button
                type="button"
                onClick={() =>
                  begin()
                }
              >
                <Plus
                  size={15}
                />

                إنشاء أول كوبون
              </button>
            ) : null}
          </div>
        ) : (
          <div className="rukn-coupons-v2-table-wrap">
            <table>
              <thead>
                <tr>
                  <th>
                    الكوبون
                  </th>

                  <th>
                    الخصم
                  </th>

                  <th>
                    النطاق
                  </th>

                  <th>
                    الصلاحية
                  </th>

                  <th>
                    حدود الاستخدام
                  </th>

                  <th>
                    الحالة
                  </th>

                  <th>
                    الإجراءات
                  </th>
                </tr>
              </thead>

              <tbody>
                {visible.map(
                  (
                    coupon,
                  ) => {
                    const state =
                      couponState(
                        coupon,
                      );

                    const meta =
                      stateMeta(
                        state,
                      );

                    return (
                      <tr
                        key={
                          coupon.id
                        }
                      >
                        <td>
                          <div className="rukn-coupons-v2-code">
                            <span>
                              <TicketPercent
                                size={15}
                              />
                            </span>

                            <div>
                              <strong
                                dir="ltr"
                              >
                                {
                                  coupon.code
                                }
                              </strong>

                              <small>
                                {
                                  coupon.name
                                }
                              </small>
                            </div>
                          </div>
                        </td>

                        <td>
                          <div className="rukn-coupons-v2-discount">
                            <strong
                              dir="ltr"
                            >
                              {discountLabel(
                                coupon,
                              )}
                            </strong>

                            <span>
                              {coupon.type ===
                              "Percentage"
                                ? "نسبة مئوية"
                                : "مبلغ ثابت"}
                            </span>
                          </div>
                        </td>

                        <td>
                          <div className="rukn-coupons-v2-scope">
                            <strong>
                              {
                                scopeLabels[
                                  coupon.scope
                                ]
                              }
                            </strong>

                            <small>
                              {targetSummary(
                                coupon,
                              )}
                            </small>
                          </div>
                        </td>

                        <td>
                          <div className="rukn-coupons-v2-validity">
                            <CalendarClock
                              size={14}
                            />

                            <span>
                              {validityLabel(
                                coupon,
                              )}
                            </span>
                          </div>
                        </td>

                        <td>
                          <div className="rukn-coupons-v2-limits">
                            <strong>
                              {coupon.maximumTotalUses
                                ? `${number(
                                    coupon.maximumTotalUses,
                                  )} إجمالي`
                                : "بلا حد إجمالي"}
                            </strong>

                            <small>
                              {coupon.maximumUsesPerCustomer
                                ? `${number(
                                    coupon.maximumUsesPerCustomer,
                                  )} لكل عميل`
                                : "بلا حد للعميل"}
                            </small>
                          </div>
                        </td>

                        <td>
                          <span
                            className="rukn-coupons-v2-status"
                            data-tone={
                              meta.tone
                            }
                          >
                            <i />

                            {
                              meta.label
                            }
                          </span>
                        </td>

                        <td>
                          <div className="rukn-coupons-v2-actions">
                            <button
                              type="button"
                              className="details"
                              onClick={() =>
                                void details(
                                  coupon.id,
                                )
                              }
                            >
                              <BarChart3
                                size={14}
                              />

                              التفاصيل
                            </button>

                            <button
                              type="button"
                              title="تعديل"
                              aria-label={`تعديل ${coupon.code}`}
                              onClick={() =>
                                begin(
                                  coupon,
                                )
                              }
                            >
                              <Pencil
                                size={14}
                              />
                            </button>

                            <button
                              type="button"
                              title={
                                coupon.isEnabled
                                  ? "إيقاف"
                                  : "تفعيل"
                              }
                              aria-label={
                                coupon.isEnabled
                                  ? `إيقاف ${coupon.code}`
                                  : `تفعيل ${coupon.code}`
                              }
                              disabled={
                                busy
                              }
                              onClick={() =>
                                void toggle(
                                  coupon,
                                )
                              }
                            >
                              {coupon.isEnabled ? (
                                <CirclePause
                                  size={14}
                                />
                              ) : (
                                <CheckCircle2
                                  size={14}
                                />
                              )}
                            </button>

                            <button
                              type="button"
                              title="نسخ الكود"
                              aria-label={`نسخ كود ${coupon.code}`}
                              onClick={() =>
                                void copyCode(
                                  coupon.code,
                                )
                              }
                            >
                              <Copy
                                size={14}
                              />
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  },
                )}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <p className="rukn-coupons-v2-footnote">
        <Check
          size={13}
        />

        التحقق النهائي من صلاحية الكوبون وحدوده يتم على الخادم وقت إتمام الطلب.
      </p>

      {editorOpen ? (
        <div className="rukn-coupons-v2-layer">
          <button
            type="button"
            className="rukn-coupons-v2-backdrop"
            aria-label="إغلاق محرر الكوبون"
            onClick={
              closeEditor
            }
          />

          <aside className="rukn-coupons-v2-drawer is-editor">
            <form
              onSubmit={(
                event,
              ) =>
                void save(
                  event,
                )
              }
            >
              <header className="rukn-coupons-v2-drawer-head">
                <div>
                  <span>
                    {editId
                      ? "تعديل العرض"
                      : "عرض جديد"}
                  </span>

                  <h2>
                    {editId
                      ? "تعديل الكوبون"
                      : "إنشاء كوبون"}
                  </h2>

                  <p>
                    اضبط الخصم والنطاق والصلاحية بدون مغادرة الصفحة.
                  </p>
                </div>

                <button
                  type="button"
                  className="close"
                  aria-label="إغلاق"
                  onClick={
                    closeEditor
                  }
                >
                  <X
                    size={17}
                  />
                </button>
              </header>

              <div className="rukn-coupons-v2-drawer-body">
                <section className="rukn-coupons-v2-form-section">
                  <header>
                    <Tag
                      size={15}
                    />

                    <div>
                      <h3>
                        أساسيات الكوبون
                      </h3>

                      <p>
                        الرمز والاسم وطريقة احتساب الخصم.
                      </p>
                    </div>
                  </header>

                  <div className="rukn-coupons-v2-form-grid">
                    <label>
                      <span>
                        رمز الكوبون
                      </span>

                      <input
                        dir="ltr"
                        maxLength={
                          60
                        }
                        value={
                          form.code
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "code",
                            event.target.value.toUpperCase(),
                          )
                        }
                        placeholder="RUKN20"
                        required
                      />
                    </label>

                    <label>
                      <span>
                        اسم الحملة
                      </span>

                      <input
                        maxLength={
                          160
                        }
                        value={
                          form.name
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "name",
                            event.target.value,
                          )
                        }
                        placeholder="خصم بداية الموسم"
                        required
                      />
                    </label>

                    <label>
                      <span>
                        نوع الخصم
                      </span>

                      <select
                        value={
                          form.type
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "type",
                            event.target
                              .value as
                              CouponInput["type"],
                          )
                        }
                      >
                        <option value="Percentage">
                          نسبة مئوية %
                        </option>

                        <option value="FixedAmount">
                          مبلغ ثابت
                        </option>
                      </select>
                    </label>

                    <label>
                      <span>
                        القيمة
                        {" "}
                        {form.type ===
                        "Percentage"
                          ? "%"
                          : form.currency}
                      </span>

                      <input
                        dir="ltr"
                        type="number"
                        min="0.01"
                        max={
                          form.type ===
                          "Percentage"
                            ? 100
                            : undefined
                        }
                        step="0.01"
                        value={
                          form.value
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "value",
                            Number(
                              event.target.value,
                            ),
                          )
                        }
                        required
                      />
                    </label>
                  </div>

                  <div className="rukn-coupons-v2-currency">
                    <Store
                      size={15}
                    />

                    <div>
                      <span>
                        عملة الخصم
                      </span>

                      <strong
                        dir="ltr"
                      >
                        {storeCurrency ??
                          "غير محددة"}
                      </strong>
                    </div>

                    <small>
                      {storeCurrency
                        ? "مأخوذة تلقائيًا من عملة أسعار المتجر."
                        : storeCurrencies.length >
                            1
                          ? "توجد أكثر من عملة في المنتجات."
                          : "أضف منتجًا بسعر وعملة أولًا."}
                    </small>
                  </div>
                </section>

                <section className="rukn-coupons-v2-form-section">
                  <header>
                    <Layers3
                      size={15}
                    />

                    <div>
                      <h3>
                        نطاق الخصم
                      </h3>

                      <p>
                        أين يمكن للعميل استخدام هذا الكوبون؟
                      </p>
                    </div>
                  </header>

                  <label>
                    <span>
                      نطاق الكوبون
                    </span>

                    <select
                      value={
                        form.scope
                      }
                      onChange={(
                        event,
                      ) =>
                        patch(
                          "scope",
                          event.target
                            .value as
                            CouponScope,
                        )
                      }
                    >
                      <option value="EntireStore">
                        المتجر بالكامل
                      </option>

                      <option value="Categories">
                        قسم أو عدة أقسام
                      </option>

                      <option value="Products">
                        منتج أو عدة منتجات
                      </option>
                    </select>
                  </label>

                  {form.scope !==
                  "EntireStore" ? (
                    <div className="rukn-coupons-v2-targets">
                      <div className="rukn-coupons-v2-target-head">
                        <strong>
                          {form.scope ===
                          "Products"
                            ? "المنتجات المستهدفة"
                            : "الأقسام المستهدفة"}
                        </strong>

                        <span>
                          المحدد:
                          {" "}
                          {number(
                            form.scope ===
                              "Products"
                              ? form.productIds.length
                              : form.categoryIds.length,
                          )}
                        </span>
                      </div>

                      <div className="rukn-coupons-v2-target-search">
                        <Search
                          size={14}
                        />

                        <input
                          value={
                            targetSearch
                          }
                          onChange={(
                            event,
                          ) =>
                            setTargetSearch(
                              event.target.value,
                            )
                          }
                          placeholder="بحث بالاسم..."
                        />
                      </div>

                      <div className="rukn-coupons-v2-target-list">
                        {form.scope ===
                        "Products"
                          ? products
                              .filter(
                                (
                                  product,
                                ) =>
                                  product.name.includes(
                                    targetSearch,
                                  ),
                              )
                              .map(
                                (
                                  product,
                                ) => (
                                  <label
                                    key={
                                      product.productId
                                    }
                                    className="target"
                                  >
                                    <input
                                      type="checkbox"
                                      checked={
                                        form.productIds.includes(
                                          product.productId,
                                        )
                                      }
                                      onChange={() =>
                                        toggleTarget(
                                          product.productId,
                                          "productIds",
                                        )
                                      }
                                    />

                                    <span>
                                      {
                                        product.name
                                      }
                                    </span>
                                  </label>
                                ),
                              )
                          : categories
                              .filter(
                                (
                                  category,
                                ) =>
                                  category.name.includes(
                                    targetSearch,
                                  ),
                              )
                              .map(
                                (
                                  category,
                                ) => (
                                  <label
                                    key={
                                      category.categoryId
                                    }
                                    className="target"
                                  >
                                    <input
                                      type="checkbox"
                                      checked={
                                        form.categoryIds.includes(
                                          category.categoryId,
                                        )
                                      }
                                      onChange={() =>
                                        toggleTarget(
                                          category.categoryId,
                                          "categoryIds",
                                        )
                                      }
                                    />

                                    <span>
                                      {
                                        category.name
                                      }
                                    </span>
                                  </label>
                                ),
                              )}
                      </div>

                      {form.scope ===
                      "Categories" ? (
                        <label className="rukn-coupons-v2-check">
                          <input
                            type="checkbox"
                            checked={
                              form.includeDescendantCategories
                            }
                            onChange={(
                              event,
                            ) =>
                              patch(
                                "includeDescendantCategories",
                                event.target.checked,
                              )
                            }
                          />

                          تطبيق الخصم أيضًا على الأقسام الفرعية
                        </label>
                      ) : null}
                    </div>
                  ) : null}
                </section>

                <section className="rukn-coupons-v2-form-section">
                  <header>
                    <CalendarClock
                      size={15}
                    />

                    <div>
                      <h3>
                        الشروط والصلاحية
                      </h3>

                      <p>
                        حدود الاستخدام والطلب ومدة العرض.
                      </p>
                    </div>
                  </header>

                  <div className="rukn-coupons-v2-form-grid">
                    <label>
                      <span>
                        إجمالي الاستخدامات
                      </span>

                      <input
                        dir="ltr"
                        type="number"
                        min="1"
                        step="1"
                        value={
                          form.maximumTotalUses ??
                          ""
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "maximumTotalUses",
                            event.target.value ===
                              ""
                              ? null
                              : Number(
                                  event.target.value,
                                ),
                          )
                        }
                        placeholder="بلا حد"
                      />
                    </label>

                    <label>
                      <span>
                        لكل عميل
                      </span>

                      <input
                        dir="ltr"
                        type="number"
                        min="1"
                        step="1"
                        value={
                          form.maximumUsesPerCustomer ??
                          ""
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "maximumUsesPerCustomer",
                            event.target.value ===
                              ""
                              ? null
                              : Number(
                                  event.target.value,
                                ),
                          )
                        }
                        placeholder="1"
                      />
                    </label>

                    <label>
                      <span>
                        أقل قيمة للطلب
                      </span>

                      <input
                        dir="ltr"
                        type="number"
                        min="0"
                        step="0.01"
                        value={
                          form.minimumOrderAmount ??
                          ""
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "minimumOrderAmount",
                            event.target.value ===
                              ""
                              ? null
                              : Number(
                                  event.target.value,
                                ),
                          )
                        }
                        placeholder="اختياري"
                      />
                    </label>

                    <div className="rukn-coupons-v2-form-spacer" />

                    <label>
                      <span>
                        بداية العرض
                      </span>

                      <input
                        type="datetime-local"
                        value={
                          localDate(
                            form.startsAtUtc,
                          )
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "startsAtUtc",
                            utcDate(
                              event.target.value,
                            ),
                          )
                        }
                      />
                    </label>

                    <label>
                      <span>
                        نهاية العرض
                      </span>

                      <input
                        type="datetime-local"
                        value={
                          localDate(
                            form.endsAtUtc,
                          )
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "endsAtUtc",
                            utcDate(
                              event.target.value,
                            ),
                          )
                        }
                      />
                    </label>
                  </div>

                  {editId ? (
                    <label className="rukn-coupons-v2-switch">
                      <input
                        type="checkbox"
                        checked={
                          form.isEnabled
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "isEnabled",
                            event.target.checked,
                          )
                        }
                      />

                      <span>
                        الكوبون مفعّل
                      </span>
                    </label>
                  ) : null}
                </section>

                {error ? (
                  <div className="rukn-coupons-v2-drawer-error">
                    {error}
                  </div>
                ) : null}
              </div>

              <footer className="rukn-coupons-v2-drawer-footer">
                <button
                  type="button"
                  className="secondary"
                  disabled={
                    busy
                  }
                  onClick={
                    closeEditor
                  }
                >
                  إلغاء
                </button>

                <button
                  type="submit"
                  className="primary"
                  disabled={
                    busy ||
                    !storeCurrency
                  }
                >
                  <Check
                    size={16}
                  />

                  {busy
                    ? "جاري الحفظ..."
                    : editId
                      ? "حفظ التعديلات"
                      : "إنشاء الكوبون"}
                </button>
              </footer>
            </form>
          </aside>
        </div>
      ) : null}

      {activeCoupon ? (
        <div className="rukn-coupons-v2-layer">
          <button
            type="button"
            className="rukn-coupons-v2-backdrop"
            aria-label="إغلاق تقرير الكوبون"
            onClick={
              closeDetails
            }
          />

          <aside className="rukn-coupons-v2-drawer is-report">
            <header className="rukn-coupons-v2-drawer-head">
              <div>
                <span>
                  تقرير الكوبون
                </span>

                <h2
                  dir="ltr"
                >
                  {
                    activeCoupon.code
                  }
                </h2>

                <p>
                  {
                    activeCoupon.name
                  }
                </p>
              </div>

              <button
                type="button"
                className="close"
                aria-label="إغلاق"
                onClick={
                  closeDetails
                }
              >
                <X
                  size={17}
                />
              </button>
            </header>

            <div className="rukn-coupons-v2-drawer-body">
              {detailsBusy ? (
                <div className="rukn-coupons-v2-report-loading">
                  <RefreshCw
                    size={20}
                    className="is-spinning"
                  />

                  جاري تحميل بيانات الكوبون...
                </div>
              ) : analytics ? (
                <>
                  <section className="rukn-coupons-v2-report-hero">
                    <div>
                      <span>
                        المبيعات المدفوعة
                      </span>

                      <strong
                        dir="ltr"
                      >
                        {money(
                          analytics.paidRevenue,
                          activeCoupon.currency,
                        )}
                      </strong>
                    </div>

                    <span
                      className="rukn-coupons-v2-status"
                      data-tone={
                        stateMeta(
                          couponState(
                            activeCoupon,
                          ),
                        ).tone
                      }
                    >
                      <i />

                      {
                        stateMeta(
                          couponState(
                            activeCoupon,
                          ),
                        ).label
                      }
                    </span>
                  </section>

                  <section className="rukn-coupons-v2-report-metrics">
                    <ReportMetric
                      title="مرات الاستخدام"
                      value={
                        activeCoupon.maximumTotalUses
                          ? `${number(
                              analytics.usageCount,
                            )} / ${number(
                              activeCoupon.maximumTotalUses,
                            )}`
                          : number(
                              analytics.usageCount,
                            )
                      }
                    />

                    <ReportMetric
                      title="عملاء مختلفون"
                      value={
                        number(
                          analytics.uniqueCustomers,
                        )
                      }
                    />

                    <ReportMetric
                      title="طلبات مدفوعة"
                      value={
                        number(
                          analytics.paidOrderCount,
                        )
                      }
                    />

                    <ReportMetric
                      title="إجمالي الخصومات"
                      value={
                        money(
                          analytics.paidDiscountTotal,
                          activeCoupon.currency,
                        )
                      }
                    />
                  </section>

                  <section className="rukn-coupons-v2-report-secondary">
                    <div>
                      <span>
                        بانتظار الدفع
                      </span>

                      <strong>
                        {number(
                          analytics.pendingOrderCount,
                        )}
                      </strong>

                      <small
                        dir="ltr"
                      >
                        {money(
                          analytics.pendingRevenue,
                          activeCoupon.currency,
                        )}
                      </small>
                    </div>

                    <div>
                      <span>
                        طلبات ملغاة
                      </span>

                      <strong>
                        {number(
                          analytics.cancelledOrderCount,
                        )}
                      </strong>
                    </div>
                  </section>

                  <p className="rukn-coupons-v2-report-note">
                    المبيعات هنا تخص طلبات استخدمت هذا الكوبون ووصلت إلى حالة مدفوعة أو مؤكدة؛ ليست صافي الربح ولا تعني أن الكوبون وحده تسبب بهذه المبيعات.
                  </p>

                  <section className="rukn-coupons-v2-uses">
                    <header>
                      <div>
                        <h3>
                          آخر الاستخدامات
                        </h3>

                        <p>
                          أحدث الطلبات المرتبطة بالكوبون.
                        </p>
                      </div>

                      <Users
                        size={16}
                      />
                    </header>

                    {analytics.recentUses.length ===
                    0 ? (
                      <div className="rukn-coupons-v2-no-uses">
                        لا توجد استخدامات مسجلة لهذا الكوبون بعد.
                      </div>
                    ) : (
                      <div className="rukn-coupons-v2-uses-table">
                        <table>
                          <thead>
                            <tr>
                              <th>
                                الطلب
                              </th>

                              <th>
                                الحالة
                              </th>

                              <th>
                                قيمة الطلب
                              </th>

                              <th>
                                الخصم
                              </th>

                              <th>
                                التاريخ
                              </th>
                            </tr>
                          </thead>

                          <tbody>
                            {analytics.recentUses.map(
                              (
                                item,
                              ) => (
                                <tr
                                  key={
                                    item.orderId
                                  }
                                >
                                  <td
                                    dir="ltr"
                                  >
                                    #
                                    {item.orderId
                                      .slice(
                                        0,
                                        8,
                                      )
                                      .toUpperCase()}
                                  </td>

                                  <td>
                                    {orderStateLabel(
                                      item.status,
                                    )}
                                  </td>

                                  <td
                                    dir="ltr"
                                  >
                                    {money(
                                      item.orderAmount,
                                      activeCoupon.currency,
                                    )}
                                  </td>

                                  <td
                                    dir="ltr"
                                  >
                                    {money(
                                      item.discountAmount,
                                      activeCoupon.currency,
                                    )}
                                  </td>

                                  <td>
                                    {formatDate(
                                      item.redeemedAtUtc,
                                      true,
                                    )}
                                  </td>
                                </tr>
                              ),
                            )}
                          </tbody>
                        </table>
                      </div>
                    )}
                  </section>
                </>
              ) : (
                <div className="rukn-coupons-v2-no-uses">
                  تعذر تحميل تقرير الكوبون.
                </div>
              )}
            </div>
          </aside>
        </div>
      ) : null}
    </main>
  );
}

function Metric({
  icon: Icon,
  title,
  value,
  note,
  tone = "plain",
  active,
  onClick,
}: {
  icon: LucideIcon;
  title: string;
  value: number;
  note: string;
  tone?:
    | "plain"
    | "success"
    | "info"
    | "warning";
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      className="rukn-coupons-v2-metric"
      data-tone={
        tone
      }
      data-active={
        active
      }
      onClick={
        onClick
      }
    >
      <span className="icon">
        <Icon
          size={18}
        />
      </span>

      <span className="content">
        <span>
          {title}
        </span>

        <strong>
          {number(
            value,
          )}
        </strong>

        <small>
          {note}
        </small>
      </span>
    </button>
  );
}

function ReportMetric({
  title,
  value,
}: {
  title: string;
  value: string;
}) {
  return (
    <div>
      <span>
        {title}
      </span>

      <strong>
        {value}
      </strong>
    </div>
  );
}