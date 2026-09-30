import {
  Check,
  CirclePause,
  MapPin,
  PackageCheck,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  Store,
  Truck,
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
  createShippingMethod,
  listLocations,
  listShippingMethods,
  updateShippingMethod,
  type FulfillmentLocation,
  type ShippingMethod,
  type ShippingMethodInput,
} from "./shippingAdminApi";

import "./AdminShippingV2.css";

type MethodFilter =
  | "all"
  | "delivery"
  | "pickup";

const initial: ShippingMethodInput = {
  code: "",
  name: "",
  type: "FlatRate",
  price: 0,
  currency: "SAR",
  minimumOrderAmount: null,
  maximumOrderAmount: null,
  pickupLocationId: null,
  sortOrder: 0,
  isEnabled: false,
};

const typeLabels: Record<
  ShippingMethod["type"],
  string
> = {
  FlatRate: "رسوم ثابتة",
  Free: "توصيل مجاني",
  Pickup: "استلام من المتجر",
};

const currencyOptions = [
  "SAR",
  "AED",
  "USD",
  "SYP",
];

function fresh(
  type: ShippingMethod["type"] =
    "FlatRate",
): ShippingMethodInput {
  return {
    ...initial,
    type,
    isEnabled:
      type === "Pickup",
  };
}

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

function methodFee(
  method: ShippingMethod,
) {
  if (
    method.type === "Free" ||
    method.type === "Pickup"
  ) {
    return "مجاني";
  }

  return money(
    method.price,
    method.currency,
  );
}

function orderRange(
  method: ShippingMethod,
) {
  if (
    method.minimumOrderAmount ===
      null &&
    method.maximumOrderAmount ===
      null
  ) {
    return "كل قيم الطلبات";
  }

  if (
    method.minimumOrderAmount !==
      null &&
    method.maximumOrderAmount !==
      null
  ) {
    return `${money(
      method.minimumOrderAmount,
      method.currency,
    )} — ${money(
      method.maximumOrderAmount,
      method.currency,
    )}`;
  }

  if (
    method.minimumOrderAmount !==
    null
  ) {
    return `من ${money(
      method.minimumOrderAmount,
      method.currency,
    )}`;
  }

  return `حتى ${money(
    method.maximumOrderAmount!,
    method.currency,
  )}`;
}

function stateMeta(
  method: ShippingMethod,
) {
  if (method.isEnabled) {
    return {
      label: "مفعّلة",
      tone: "active",
    };
  }

  return {
    label: "متوقفة",
    tone: "paused",
  };
}

export function AdminShippingPage() {
  const tenantId =
    readAdminStore()
      ?.tenantId ??
    null;

  const [
    methods,
    setMethods,
  ] =
    useState<
      ShippingMethod[]
    >([]);

  const [
    locations,
    setLocations,
  ] =
    useState<
      FulfillmentLocation[]
    >([]);

  const [
    form,
    setForm,
  ] =
    useState<
      ShippingMethodInput
    >(
      fresh,
    );

  const [
    editingId,
    setEditingId,
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
    query,
    setQuery,
  ] =
    useState("");

  const [
    filter,
    setFilter,
  ] =
    useState<MethodFilter>(
      "all",
    );

  const activeLocations =
    useMemo(
      () =>
        locations.filter(
          (
            location,
          ) =>
            location.isActive,
        ),
      [
        locations,
      ],
    );

  const locationById =
    useMemo(
      () =>
        new Map(
          locations.map(
            (
              location,
            ) => [
              location.id,
              location,
            ],
          ),
        ),
      [
        locations,
      ],
    );

  const load =
    useCallback(
      async () => {
        if (!tenantId) {
          setMethods([]);
          setLocations([]);
          setLoading(false);
          return;
        }

        setLoading(true);
        setError("");

        try {
          const [
            nextMethods,
            nextLocations,
          ] =
            await Promise.all([
              listShippingMethods(
                tenantId,
              ),

              listLocations(
                tenantId,
              ),
            ]);

          setMethods(
            nextMethods,
          );

          setLocations(
            nextLocations,
          );
        } catch (caught) {
          setError(
            caught instanceof Error
              ? caught.message
              : "تعذر تحميل إعدادات التوصيل والاستلام.",
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
      if (!editorOpen) {
        return;
      }

      const previous =
        document.body.style.overflow;

      document.body.style.overflow =
        "hidden";

      return () => {
        document.body.style.overflow =
          previous;
      };
    },
    [
      editorOpen,
    ],
  );

  const deliveryCount =
    useMemo(
      () =>
        methods.filter(
          (
            method,
          ) =>
            method.type !==
            "Pickup",
        ).length,
      [
        methods,
      ],
    );

  const pickupCount =
    useMemo(
      () =>
        methods.filter(
          (
            method,
          ) =>
            method.type ===
            "Pickup",
        ).length,
      [
        methods,
      ],
    );

  const activePickupCount =
    useMemo(
      () =>
        methods.filter(
          (
            method,
          ) =>
            method.type ===
              "Pickup" &&
            method.isEnabled,
        ).length,
      [
        methods,
      ],
    );

  const visible =
    useMemo(
      () => {
        const term =
          query
            .trim()
            .toLowerCase();

        return [
          ...methods,
        ]
          .filter(
            (
              method,
            ) => {
              if (
                filter ===
                  "delivery" &&
                method.type ===
                  "Pickup"
              ) {
                return false;
              }

              if (
                filter ===
                  "pickup" &&
                method.type !==
                  "Pickup"
              ) {
                return false;
              }

              if (!term) {
                return true;
              }

              const location =
                method.pickupLocationId
                  ? locationById.get(
                      method.pickupLocationId,
                    )
                  : null;

              return [
                method.name,
                method.code,
                typeLabels[
                  method.type
                ],
                location?.name ??
                  "",
                location?.city ??
                  "",
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
          )
          .sort(
            (
              left,
              right,
            ) =>
              left.sortOrder -
                right.sortOrder ||
              left.name.localeCompare(
                right.name,
                "ar",
              ),
          );
      },
      [
        methods,
        query,
        filter,
        locationById,
      ],
    );

  function patch<
    K extends keyof ShippingMethodInput
  >(
    key: K,
    value:
      ShippingMethodInput[K],
  ) {
    setForm(
      (
        current,
      ) => ({
        ...current,
        [key]:
          value,
      }),
    );
  }

  function beginCreate(
    type: ShippingMethod["type"] =
      "FlatRate",
  ) {
    const next =
      fresh(
        type,
      );

    if (
      type ===
        "Pickup" &&
      activeLocations.length >
        0
    ) {
      next.pickupLocationId =
        activeLocations[0].id;
    }

    setForm(
      next,
    );

    setEditingId(
      null,
    );

    setError("");
    setNotice("");
    setEditorOpen(true);
  }

  function beginEdit(
    method: ShippingMethod,
  ) {
    setForm({
      code:
        method.code,
      name:
        method.name,
      type:
        method.type,
      price:
        method.price,
      currency:
        method.currency,
      minimumOrderAmount:
        method.minimumOrderAmount,
      maximumOrderAmount:
        method.maximumOrderAmount,
      pickupLocationId:
        method.pickupLocationId,
      sortOrder:
        method.sortOrder,
      isEnabled:
        method.isEnabled,
    });

    setEditingId(
      method.id,
    );

    setError("");
    setNotice("");
    setEditorOpen(true);
  }

  function closeEditor() {
    if (busy) {
      return;
    }

    setEditorOpen(false);
    setEditingId(null);
    setError("");
  }

  function validate():
    | string
    | null {
    const cleanCode =
      form.code
        .trim()
        .toLowerCase();

    const cleanName =
      form.name.trim();

    if (
      !/^[a-z0-9][a-z0-9_-]{1,59}$/.test(
        cleanCode,
      )
    ) {
      return "رمز الطريقة يجب أن يكون من 2 إلى 60 حرفًا إنجليزيًا أو رقمًا، بدون فراغات.";
    }

    if (
      !cleanName ||
      cleanName.length >
        160
    ) {
      return "أدخل اسمًا واضحًا لطريقة التوصيل أو الاستلام.";
    }

    if (
      !currencyOptions.includes(
        form.currency,
      )
    ) {
      return "عملة الطريقة غير مدعومة في هذه الشاشة.";
    }

    if (
      !Number.isFinite(
        form.price,
      ) ||
      form.price < 0
    ) {
      return "رسوم التوصيل يجب أن تكون صفرًا أو قيمة موجبة.";
    }

    if (
      !Number.isSafeInteger(
        form.sortOrder,
      ) ||
      form.sortOrder < 0
    ) {
      return "ترتيب الطريقة يجب أن يكون عددًا صحيحًا غير سالب.";
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
      return "الحد الأدنى لقيمة الطلب غير صالح.";
    }

    if (
      form.maximumOrderAmount !==
        null &&
      (
        !Number.isFinite(
          form.maximumOrderAmount,
        ) ||
        form.maximumOrderAmount <
          0
      )
    ) {
      return "الحد الأعلى لقيمة الطلب غير صالح.";
    }

    if (
      form.minimumOrderAmount !==
        null &&
      form.maximumOrderAmount !==
        null &&
      form.minimumOrderAmount >
        form.maximumOrderAmount
    ) {
      return "الحد الأعلى لقيمة الطلب يجب أن يكون أكبر من أو مساويًا للحد الأدنى.";
    }

    if (
      form.type ===
        "Pickup" &&
      !activeLocations.some(
        (
          location,
        ) =>
          location.id ===
          form.pickupLocationId,
      )
    ) {
      return "حدد موقع استلام فعّالًا من مواقع المتجر.";
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
      ShippingMethodInput = {
      ...form,

      code:
        form.code
          .trim()
          .toLowerCase(),

      name:
        form.name
          .trim(),

      isEnabled:
        form.isEnabled,

      pickupLocationId:
        form.type ===
          "Pickup"
          ? form.pickupLocationId
          : null,

      price:
        form.type ===
          "Free" ||
        form.type ===
          "Pickup"
          ? 0
          : form.price,
    };

    setBusy(true);
    setError("");
    setNotice("");

    try {
      const editing =
        Boolean(
          editingId,
        );

      if (editingId) {
        await updateShippingMethod(
          tenantId,
          {
            ...input,
            id:
              editingId,
          },
        );
      } else {
        await createShippingMethod(
          tenantId,
          input,
        );
      }

      await load();

      setEditorOpen(false);
      setEditingId(null);

      setNotice(
        editing
          ? "تم تحديث طريقة التسليم وحفظها على السيرفر."
          : input.type ===
              "Pickup"
            ? "تم إنشاء طريقة الاستلام."
            : "تم إنشاء طريقة التوصيل.",
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر حفظ طريقة التسليم.",
      );
    } finally {
      setBusy(false);
    }
  }

  async function toggle(
    method: ShippingMethod,
  ) {
    if (
      !tenantId ||
      busy
    ) {
      return;
    }


    const action =
      method.isEnabled
        ? "إيقاف"
        : "تفعيل";

    if (
      !window.confirm(
        `${action} ${method.name}؟`,
      )
    ) {
      return;
    }

    setBusy(true);
    setError("");
    setNotice("");

    try {
      await updateShippingMethod(
        tenantId,
        {
          ...method,
          isEnabled:
            !method.isEnabled,
        },
      );

      await load();

      setNotice(
        `تم ${action} الطريقة وحفظ التغيير على السيرفر.`,
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر تغيير حالة الطريقة.",
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <main
      dir="rtl"
      className="rukn-shipping-v2"
    >
      <header className="rukn-shipping-v2-head">
        <div>
          <span>
            إدارة تنفيذ الطلبات
          </span>

          <h1>
            التوصيل والاستلام
          </h1>

          <p>
            نظّم طرق تسليم الطلبات، رسومها، شروط قيم الطلب، ونقاط الاستلام الفعلية من شاشة واحدة.
          </p>
        </div>

        <div className="rukn-shipping-v2-head-actions">
          <button
            type="button"
            className="secondary"
            disabled={
              loading ||
              busy
            }
            onClick={() =>
              void load()
            }
          >
            <RefreshCw
              size={16}
              className={
                loading
                  ? "is-spinning"
                  : ""
              }
            />

            تحديث
          </button>

          <button
            type="button"
            className="primary"
            disabled={
              !tenantId
            }
            onClick={() =>
              beginCreate(
                "FlatRate",
              )
            }
          >
            <Plus
              size={16}
            />

            إضافة طريقة
          </button>
        </div>
      </header>

      {error ? (
        <div
          role="alert"
          className="rukn-shipping-v2-alert is-error"
        >
          {error}
        </div>
      ) : null}

      {notice ? (
        <div
          role="status"
          className="rukn-shipping-v2-alert is-success"
        >
          <Check
            size={15}
          />

          {notice}
        </div>
      ) : null}

      {!tenantId ? (
        <div className="rukn-shipping-v2-alert is-warning">
          اختر متجرًا من لوحة الإدارة أولًا.
        </div>
      ) : null}

      <section className="rukn-shipping-v2-metrics">
        <Metric
          icon={
            PackageCheck
          }
          title="طرق التسليم"
          value={
            methods.length
          }
          note="كل الطرق المسجلة"
        />

        <Metric
          icon={
            Truck
          }
          title="توصيل للعنوان"
          value={
            deliveryCount
          }
          note="رسوم ثابتة أو مجاني"
          tone="delivery"
        />

        <Metric
          icon={
            MapPin
          }
          title="استلام مفعّل"
          value={
            activePickupCount
          }
          note={`${number(
            pickupCount,
          )} طريقة استلام مسجلة`}
          tone="pickup"
        />

        <Metric
          icon={
            Store
          }
          title="مواقع نشطة"
          value={
            activeLocations.length
          }
          note={`${number(
            locations.length,
          )} موقع إجمالي`}
          tone="location"
        />
      </section>

      <section className="rukn-shipping-v2-workspace">
        <div className="rukn-shipping-v2-toolbar">
          <div className="rukn-shipping-v2-search">
            <Search
              size={16}
            />

            <input
              value={
                query
              }
              onChange={(
                event,
              ) =>
                setQuery(
                  event.target.value,
                )
              }
              placeholder="ابحث باسم الطريقة أو الرمز أو موقع الاستلام..."
            />
          </div>

          <div className="rukn-shipping-v2-tabs">
            <button
              type="button"
              data-active={
                filter ===
                "all"
              }
              onClick={() =>
                setFilter(
                  "all",
                )
              }
            >
              الكل
              <span>
                {number(
                  methods.length,
                )}
              </span>
            </button>

            <button
              type="button"
              data-active={
                filter ===
                "delivery"
              }
              onClick={() =>
                setFilter(
                  "delivery",
                )
              }
            >
              التوصيل
              <span>
                {number(
                  deliveryCount,
                )}
              </span>
            </button>

            <button
              type="button"
              data-active={
                filter ===
                "pickup"
              }
              onClick={() =>
                setFilter(
                  "pickup",
                )
              }
            >
              الاستلام
              <span>
                {number(
                  pickupCount,
                )}
              </span>
            </button>
          </div>
        </div>

        <div className="rukn-shipping-v2-resultbar">
          <span>
            عرض
            {" "}
            <strong>
              {number(
                visible.length,
              )}
            </strong>
            {" "}
            طريقة
          </span>

          <span>
            مرتبة حسب ترتيب ظهورها للعميل
          </span>
        </div>

        {loading ? (
          <div className="rukn-shipping-v2-loading">
            {Array.from({
              length: 4,
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
          <div className="rukn-shipping-v2-empty">
            <span>
              <Truck
                size={22}
              />
            </span>

            <h2>
              {methods.length ===
              0
                ? "لا توجد طرق تسليم بعد"
                : "لا توجد نتائج مطابقة"}
            </h2>

            <p>
              {methods.length ===
              0
                ? "أضف طريقة توصيل أو اربط أحد مواقع المتجر بطريقة استلام."
                : "جرّب تغيير البحث أو التبويب الحالي."}
            </p>

            {methods.length ===
            0 ? (
              <div>
                <button
                  type="button"
                  onClick={() =>
                    beginCreate(
                      "FlatRate",
                    )
                  }
                >
                  <Truck
                    size={15}
                  />

                  إضافة توصيل
                </button>

                <button
                  type="button"
                  className="secondary"
                  disabled={
                    activeLocations.length ===
                    0
                  }
                  onClick={() =>
                    beginCreate(
                      "Pickup",
                    )
                  }
                >
                  <MapPin
                    size={15}
                  />

                  إضافة استلام
                </button>
              </div>
            ) : null}
          </div>
        ) : (
          <div className="rukn-shipping-v2-table-wrap">
            <table>
              <thead>
                <tr>
                  <th>
                    الطريقة
                  </th>

                  <th>
                    النوع
                  </th>

                  <th>
                    الرسوم
                  </th>

                  <th>
                    نطاق الطلب
                  </th>

                  <th>
                    موقع الاستلام
                  </th>

                  <th>
                    الحالة
                  </th>

                  <th>
                    الترتيب
                  </th>

                  <th>
                    الإجراءات
                  </th>
                </tr>
              </thead>

              <tbody>
                {visible.map(
                  (
                    method,
                  ) => {
                    const location =
                      method.pickupLocationId
                        ? locationById.get(
                            method.pickupLocationId,
                          )
                        : null;

                    const state =
                      stateMeta(
                        method,
                      );

                    return (
                      <tr
                        key={
                          method.id
                        }
                      >
                        <td>
                          <div className="rukn-shipping-v2-method">
                            <span
                              data-kind={
                                method.type ===
                                "Pickup"
                                  ? "pickup"
                                  : "delivery"
                              }
                            >
                              {method.type ===
                              "Pickup" ? (
                                <MapPin
                                  size={15}
                                />
                              ) : (
                                <Truck
                                  size={15}
                                />
                              )}
                            </span>

                            <div>
                              <strong>
                                {
                                  method.name
                                }
                              </strong>

                              <small
                                dir="ltr"
                              >
                                {
                                  method.code
                                }
                              </small>
                            </div>
                          </div>
                        </td>

                        <td>
                          <div className="rukn-shipping-v2-type">
                            <strong>
                              {
                                typeLabels[
                                  method.type
                                ]
                              }
                            </strong>

                            <small
                              dir="ltr"
                            >
                              {
                                method.currency
                              }
                            </small>
                          </div>
                        </td>

                        <td>
                          <strong className="rukn-shipping-v2-price">
                            {methodFee(
                              method,
                            )}
                          </strong>
                        </td>

                        <td>
                          <span className="rukn-shipping-v2-range">
                            {orderRange(
                              method,
                            )}
                          </span>
                        </td>

                        <td>
                          {location ? (
                            <div className="rukn-shipping-v2-location">
                              <strong>
                                {
                                  location.name
                                }
                              </strong>

                              <small>
                                {
                                  location.city
                                }
                                {" · "}
                                <span
                                  dir="ltr"
                                >
                                  {
                                    location.countryCode
                                  }
                                </span>
                              </small>
                            </div>
                          ) : (
                            <span className="rukn-shipping-v2-dash">
                              —
                            </span>
                          )}
                        </td>

                        <td>
                          <span
                            className="rukn-shipping-v2-status"
                            data-tone={
                              state.tone
                            }
                          >
                            <i />

                            {
                              state.label
                            }
                          </span>
                        </td>

                        <td>
                          <span
                            className="rukn-shipping-v2-sort"
                            dir="ltr"
                          >
                            #
                            {number(
                              method.sortOrder,
                            )}
                          </span>
                        </td>

                        <td>
                          <div className="rukn-shipping-v2-actions">
                            <button
                              type="button"
                              title="تعديل"
                              aria-label={`تعديل ${method.name}`}
                              disabled={
                                busy
                              }
                              onClick={() =>
                                beginEdit(
                                  method,
                                )
                              }
                            >
                              <Pencil
                                size={14}
                              />
                            </button>

                            <button
                              type="button"
                              className={
                                method.isEnabled
                                  ? "pause"
                                  : "activate"
                              }
                              disabled={
                                busy
                              }
                              onClick={() =>
                                void toggle(
                                  method,
                                )
                              }
                            >
                              {method.isEnabled ? (
                                <CirclePause
                                  size={14}
                                />
                              ) : (
                                <Check
                                  size={14}
                                />
                              )}

                              {method.isEnabled
                                ? "إيقاف"
                                : "تفعيل"}
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

      <section className="rukn-shipping-v2-locations">
        <header>
          <div>
            <span>
              نقاط الاستلام
            </span>

            <h2>
              مواقع المتجر
            </h2>

            <p>
              هذه المواقع تأتي مباشرة من مواقع التخزين والاستلام في المتجر.
            </p>
          </div>

          <button
            type="button"
            disabled={
              activeLocations.length ===
              0
            }
            onClick={() =>
              beginCreate(
                "Pickup",
              )
            }
          >
            <Plus
              size={15}
            />

            إضافة طريقة استلام
          </button>
        </header>

        {locations.length ===
        0 ? (
          <div className="rukn-shipping-v2-no-locations">
            <MapPin
              size={18}
            />

            <div>
              <strong>
                لا توجد مواقع متجر حتى الآن
              </strong>

              <p>
                أضف موقعًا من صفحة المخزون أولًا، وبعدها يمكنك ربطه بطريقة استلام.
              </p>
            </div>
          </div>
        ) : (
          <div className="rukn-shipping-v2-location-list">
            {locations.map(
              (
                location,
              ) => {
                const linked =
                  methods.filter(
                    (
                      method,
                    ) =>
                      method.type ===
                        "Pickup" &&
                      method.pickupLocationId ===
                        location.id,
                  );

                const enabled =
                  linked.filter(
                    (
                      method,
                    ) =>
                      method.isEnabled,
                  ).length;

                return (
                  <article
                    key={
                      location.id
                    }
                  >
                    <span className="icon">
                      <MapPin
                        size={15}
                      />
                    </span>

                    <div className="main">
                      <strong>
                        {
                          location.name
                        }
                      </strong>

                      <small>
                        {
                          location.city
                        }
                        {" · "}
                        <span
                          dir="ltr"
                        >
                          {
                            location.countryCode
                          }
                        </span>
                      </small>
                    </div>

                    <div className="linked">
                      <span>
                        طرق الاستلام
                      </span>

                      <strong>
                        {number(
                          linked.length,
                        )}
                      </strong>
                    </div>

                    <div className="linked">
                      <span>
                        المفعّلة
                      </span>

                      <strong>
                        {number(
                          enabled,
                        )}
                      </strong>
                    </div>

                    <span
                      className="location-state"
                      data-active={
                        location.isActive
                      }
                    >
                      <i />

                      {location.isActive
                        ? "نشط"
                        : "متوقف"}
                    </span>

                    <button
                      type="button"
                      disabled={
                        !location.isActive
                      }
                      onClick={() => {
                        const next =
                          fresh(
                            "Pickup",
                          );

                        next.pickupLocationId =
                          location.id;

                        setForm(
                          next,
                        );

                        setEditingId(
                          null,
                        );

                        setError("");
                        setNotice("");
                        setEditorOpen(
                          true,
                        );
                      }}
                    >
                      ربط استلام
                    </button>
                  </article>
                );
              },
            )}
          </div>
        )}
      </section>

      <section className="rukn-shipping-v2-integration-note">
        <PackageCheck
          size={16}
        />

        <div>
          <strong>
            تكامل شركات الشحن غير مفعّل بعد
          </strong>

          <p>
            اسم طريقة التوصيل هنا ليس ربطًا آليًا مع شركة شحن، ولا ينشئ بوليصة أو رقم تتبع تلقائيًا.
          </p>
        </div>
      </section>

      {editorOpen ? (
        <div className="rukn-shipping-v2-layer">
          <button
            type="button"
            className="rukn-shipping-v2-backdrop"
            aria-label="إغلاق"
            onClick={
              closeEditor
            }
          />

          <aside className="rukn-shipping-v2-drawer">
            <form
              onSubmit={(
                event,
              ) =>
                void save(
                  event,
                )
              }
            >
              <header className="rukn-shipping-v2-drawer-head">
                <div>
                  <span>
                    {editingId
                      ? "تعديل الطريقة"
                      : "طريقة جديدة"}
                  </span>

                  <h2>
                    {editingId
                      ? "تعديل طريقة التسليم"
                      : form.type ===
                          "Pickup"
                        ? "إضافة طريقة استلام"
                        : "إضافة طريقة توصيل"}
                  </h2>

                  <p>
                    اضبط النوع والرسوم وشروط الطلب وظهور الطريقة للعميل.
                  </p>
                </div>

                <button
                  type="button"
                  className="close"
                  aria-label="إغلاق"
                  disabled={
                    busy
                  }
                  onClick={
                    closeEditor
                  }
                >
                  <X
                    size={17}
                  />
                </button>
              </header>

              <div className="rukn-shipping-v2-drawer-body">
                <section className="rukn-shipping-v2-form-section">
                  <header>
                    <Truck
                      size={15}
                    />

                    <div>
                      <h3>
                        تعريف الطريقة
                      </h3>

                      <p>
                        الاسم والرمز والنوع الذي سيظهر في إعدادات المتجر.
                      </p>
                    </div>
                  </header>

                  <div className="rukn-shipping-v2-form-grid">
                    <label>
                      <span>
                        اسم الطريقة
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
                        placeholder={
                          form.type ===
                          "Pickup"
                            ? "استلام من الفرع الرئيسي"
                            : "توصيل داخل المدينة"
                        }
                        required
                      />
                    </label>

                    <label>
                      <span>
                        رمز الطريقة
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
                            event.target.value,
                          )
                        }
                        placeholder={
                          form.type ===
                          "Pickup"
                            ? "pickup_main"
                            : "delivery_city"
                        }
                        required
                      />
                    </label>

                    <label>
                      <span>
                        النوع
                      </span>

                      <select
                        value={
                          form.type
                        }
                        onChange={(
                          event,
                        ) => {
                          const type =
                            event.target
                              .value as
                              ShippingMethodInput["type"];

                          setForm(
                            (
                              current,
                            ) => ({
                              ...current,
                              type,
                              price:
                                type ===
                                  "FlatRate"
                                  ? current.price
                                  : 0,
                              pickupLocationId:
                                type ===
                                  "Pickup"
                                  ? current.pickupLocationId ??
                                    activeLocations[0]?.id ??
                                    null
                                  : null,
                              isEnabled:
                                current.isEnabled,
                            }),
                          );
                        }}
                      >
                        <option value="FlatRate">
                          توصيل برسوم ثابتة
                        </option>

                        <option value="Free">
                          توصيل مجاني
                        </option>

                        <option value="Pickup">
                          استلام من موقع المتجر
                        </option>
                      </select>
                    </label>

                    <label>
                      <span>
                        العملة
                      </span>

                      <select
                        dir="ltr"
                        value={
                          form.currency
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "currency",
                            event.target.value,
                          )
                        }
                      >
                        {currencyOptions.map(
                          (
                            currency,
                          ) => (
                            <option
                              key={
                                currency
                              }
                              value={
                                currency
                              }
                            >
                              {
                                currency
                              }
                            </option>
                          ),
                        )}
                      </select>
                    </label>
                  </div>
                </section>

                <section className="rukn-shipping-v2-form-section">
                  <header>
                    <PackageCheck
                      size={15}
                    />

                    <div>
                      <h3>
                        الرسوم وشروط الطلب
                      </h3>

                      <p>
                        حدد رسوم الطريقة والقيم التي يمكن للعميل استخدامها ضمنها.
                      </p>
                    </div>
                  </header>

                  <div className="rukn-shipping-v2-form-grid">
                    <label>
                      <span>
                        رسوم التوصيل
                      </span>

                      <input
                        dir="ltr"
                        type="number"
                        min="0"
                        step="0.01"
                        disabled={
                          form.type !==
                          "FlatRate"
                        }
                        value={
                          form.type ===
                          "FlatRate"
                            ? form.price
                            : 0
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "price",
                            Number(
                              event.target.value,
                            ),
                          )
                        }
                      />

                      <small>
                        {form.type ===
                        "FlatRate"
                          ? `بالعملة ${form.currency}`
                          : "هذه الطريقة مجانية."}
                      </small>
                    </label>

                    <label>
                      <span>
                        ترتيب الظهور
                      </span>

                      <input
                        dir="ltr"
                        type="number"
                        min="0"
                        step="1"
                        value={
                          form.sortOrder
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "sortOrder",
                            Number(
                              event.target.value,
                            ),
                          )
                        }
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
                        placeholder="بلا حد"
                      />
                    </label>

                    <label>
                      <span>
                        أعلى قيمة للطلب
                      </span>

                      <input
                        dir="ltr"
                        type="number"
                        min="0"
                        step="0.01"
                        value={
                          form.maximumOrderAmount ??
                          ""
                        }
                        onChange={(
                          event,
                        ) =>
                          patch(
                            "maximumOrderAmount",
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
                  </div>
                </section>

                {form.type ===
                "Pickup" ? (
                  <section className="rukn-shipping-v2-form-section">
                    <header>
                      <MapPin
                        size={15}
                      />

                      <div>
                        <h3>
                          نقطة الاستلام
                        </h3>

                        <p>
                          اربط الطريقة بموقع متجر فعّال وحدد إن كانت متاحة للعملاء.
                        </p>
                      </div>
                    </header>

                    {activeLocations.length ===
                    0 ? (
                      <div className="rukn-shipping-v2-form-warning">
                        لا يوجد موقع استلام نشط. أضف أو فعّل موقعًا من صفحة المخزون أولًا.
                      </div>
                    ) : (
                      <>
                        <label>
                          <span>
                            موقع الاستلام
                          </span>

                          <select
                            required
                            value={
                              form.pickupLocationId ??
                              ""
                            }
                            onChange={(
                              event,
                            ) =>
                              patch(
                                "pickupLocationId",
                                event.target.value ||
                                  null,
                              )
                            }
                          >
                            <option value="">
                              اختر الموقع
                            </option>

                            {activeLocations.map(
                              (
                                location,
                              ) => (
                                <option
                                  key={
                                    location.id
                                  }
                                  value={
                                    location.id
                                  }
                                >
                                  {
                                    location.name
                                  }
                                  {" — "}
                                  {
                                    location.city
                                  }
                                </option>
                              ),
                            )}
                          </select>
                        </label>

                        <label className="rukn-shipping-v2-switch">
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

                          <div>
                            <strong>
                              إتاحة الاستلام للعملاء
                            </strong>

                            <span>
                              عند التفعيل يمكن للعميل اختيار هذه النقطة ضمن طرق الاستلام المتاحة.
                            </span>
                          </div>
                        </label>
                      </>
                    )}
                  </section>
                ) : (
                  <section className="rukn-shipping-v2-form-section">
                    <label className="rukn-shipping-v2-switch">
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

                      <div>
                        <strong>
                          إتاحة التوصيل للعملاء
                        </strong>

                        <span>
                          عند التفعيل تظهر هذه الطريقة للعملاء الذين لديهم عنوان توصيل صالح وتطابق طلباتهم شروط الطريقة.
                        </span>
                      </div>
                    </label>
                  </section>
                )}

                {error ? (
                  <div className="rukn-shipping-v2-drawer-error">
                    {error}
                  </div>
                ) : null}
              </div>

              <footer className="rukn-shipping-v2-drawer-footer">
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
                    !tenantId ||
                    (
                      form.type ===
                        "Pickup" &&
                      activeLocations.length ===
                        0
                    )
                  }
                >
                  <Check
                    size={16}
                  />

                  {busy
                    ? "جاري الحفظ..."
                    : editingId
                      ? "حفظ التعديلات"
                      : "إضافة الطريقة"}
                </button>
              </footer>
            </form>
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
}: {
  icon: LucideIcon;
  title: string;
  value: number;
  note: string;
  tone?:
    | "plain"
    | "delivery"
    | "pickup"
    | "location";
}) {
  return (
    <article
      className="rukn-shipping-v2-metric"
      data-tone={
        tone
      }
    >
      <span className="icon">
        <Icon
          size={18}
        />
      </span>

      <div>
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
      </div>
    </article>
  );
}