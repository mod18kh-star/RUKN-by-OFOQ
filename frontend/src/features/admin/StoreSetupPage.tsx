import {
  ArrowLeft,
  Check,
  ChevronLeft,
  Search,
  Store,
} from "lucide-react";

import {
  useMemo,
  useState,
} from "react";

import {
  useNavigate,
} from "react-router";

import {
  configurePrimaryVertical,
  createTenant,
  StoreSetupApiError,
  storeSetupApiConfigured,
} from "./store-setup/storeSetupApi";

import {
  findVertical,
  VERTICAL_OPTIONS,
} from "./store-setup/verticalCatalog";

import {
  readAdminStore,
  saveAdminStore,
} from "./store-setup/storeSetupStorage";

import {
  MerchantRequestApiError,
  submitRegistrationRequest,
} from "./requests/merchantRequestsApi";

import {
  readSelectedBilling,
  readSelectedPlan,
} from "../onboarding/onboardingStorage";

import type {
  AdminStore,
} from "./store-setup/storeSetup.types";

export function StoreSetupPage() {
  const navigate =
    useNavigate();

  const existing =
    readAdminStore();

  const [
    name,
    setName,
  ] =
    useState(
      existing?.name ??
        "",
    );

  const [
    slug,
    setSlug,
  ] =
    useState(
      existing?.slug ??
        "",
    );

  const [
    slugTouched,
    setSlugTouched,
  ] =
    useState(
      Boolean(
        existing?.slug,
      ),
    );

  const [
    verticalType,
    setVerticalType,
  ] =
    useState(
      existing?.verticalType ??
        "GeneralRetail",
    );

  const [
    query,
    setQuery,
  ] =
    useState(
      "",
    );

  const [
    pendingStore,
    setPendingStore,
  ] =
    useState<
      AdminStore | null
    >(
      existing ??
        null,
    );

  const [
    saving,
    setSaving,
  ] =
    useState(
      false,
    );

  const [
    error,
    setError,
  ] =
    useState<
      string | null
    >(
      null,
    );

  const filtered =
    useMemo(
      () => {
        const normalized =
          query
            .trim()
            .toLowerCase();

        if (!normalized) {
          return VERTICAL_OPTIONS;
        }

        return VERTICAL_OPTIONS.filter(
          (item) =>
            item.label
              .toLowerCase()
              .includes(
                normalized,
              ) ||
            item.description
              .toLowerCase()
              .includes(
                normalized,
              ) ||
            item.type
              .toLowerCase()
              .includes(
                normalized,
              ),
        );
      },
      [
        query,
      ],
    );

  const selected =
    findVertical(
      verticalType,
    );

  function handleNameChange(
    value: string,
  ) {
    setName(
      value,
    );

    if (!slugTouched) {
      setSlug(
        slugify(
          value,
        ),
      );
    }
  }

  async function handleSave() {
    const cleanName =
      name.trim();

    const cleanSlug =
      slugify(
        slug,
      );

    if (
      cleanName.length <
      2
    ) {
      setError(
        "اكتب اسمًا واضحًا للمتجر.",
      );

      return;
    }

    if (
      cleanSlug.length <
      2
    ) {
      setError(
        "اكتب رابط متجر بالإنجليزية مثل noor-store.",
      );

      return;
    }

    if (!verticalType) {
      setError(
        "اختر نوع النشاط.",
      );

      return;
    }

    if (
      !storeSetupApiConfigured()
    ) {
      setError(
        "لم يتم ضبط VITE_API_BASE_URL بعد. الصفحة جاهزة، لكن الحفظ الحقيقي يحتاج عنوان الـAPI.",
      );

      return;
    }

    setSaving(
      true,
    );

    setError(
      null,
    );

    try {
      let store =
        pendingStore;

      if (!store) {
        const created =
          await createTenant({
            name:
              cleanName,

            slug:
              cleanSlug,
          });

        store = {
          tenantId:
            created.tenantId,

          name:
            created.name,

          slug:
            created.slug,

          status:
            created.status,
        };

        setPendingStore(
          store,
        );

        saveAdminStore(
          store,
        );
      }

      const vertical =
        await configurePrimaryVertical(
          store.tenantId,
          verticalType,
        );

      const completed:
        AdminStore = {
        ...store,

        verticalType:
          vertical.verticalType,

        verticalCode:
          vertical.code,
      };

      saveAdminStore(
        completed,
      );

      setPendingStore(
        completed,
      );

      const selectedPlan =
        readSelectedPlan() ??
        "business";

      const selectedBilling =
        readSelectedBilling() ===
        "monthly"
          ? "Monthly"
          : "Annual";

      await submitRegistrationRequest(
        completed.tenantId,
        {
          planCode:
            selectedPlan,
          billingCycle:
            selectedBilling,
        },
      );

      navigate(
        "/start/review",
        {
          replace: true,
        },
      );
    }
    catch (exception) {
      if (
        exception instanceof
        MerchantRequestApiError &&
        exception.code ===
          "platform_request_already_pending"
      ) {
        navigate(
          "/start/review",
          {
            replace: true,
          },
        );

        return;
      }

      if (
        exception instanceof
        StoreSetupApiError ||
        exception instanceof
          MerchantRequestApiError
      ) {
        if (
          exception instanceof
            StoreSetupApiError &&
          exception.code ===
            "tenant_slug_already_exists"
        ) {
          setError(
            "هذا رابط المتجر مستخدم بالفعل. اختر رابطًا مختلفًا.",
          );
        }
        else {
          setError(
            exception.message,
          );
        }
      }
      else {
        setError(
          "حدث خطأ غير متوقع أثناء حفظ المتجر.",
        );
      }
    }
    finally {
      setSaving(
        false,
      );
    }
  }

  const isCreated =
    Boolean(
      pendingStore
        ?.tenantId,
    );

  return (
    <div className="mx-auto max-w-[1180px]">
      <div className="mb-8 flex flex-wrap items-start justify-between gap-5">
        <div>
          <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-black/[0.08] bg-white px-3 py-1.5 text-[10px] font-semibold text-[var(--ink-soft)]">
            <Store
              size={
                14
              }
            />

            إعداد المتجر
          </div>

          <h1 className="text-[30px] font-semibold tracking-[-0.045em] md:text-[36px]">
            خلينا نجهز متجرك
          </h1>

          <p className="mt-3 max-w-[640px] text-[12px] leading-6 text-[var(--ink-soft)]">
            اسم المتجر ورابطه ونوع النشاط كافيين للبدء. تقدر تكمل باقي التفاصيل لاحقًا.
          </p>
        </div>

        {existing?.verticalType ? (
          <button
            type="button"
            onClick={() =>
              navigate(
                "/admin",
              )
            }
            className="inline-flex h-10 items-center gap-2 rounded-[8px] border border-black/[0.1] bg-white px-4 text-[11px] font-semibold"
          >
            العودة للوحة التحكم

            <ArrowLeft
              size={
                15
              }
            />
          </button>
        ) : null}
      </div>

      <div className="grid gap-6 xl:grid-cols-[380px_1fr]">
        <aside className="h-fit border border-black/[0.07] bg-[#fbfbf9] p-5 xl:sticky xl:top-[96px]">
          <h2 className="text-[14px] font-semibold">
            معلومات المتجر
          </h2>

          <p className="mt-1 text-[10px] leading-5 text-[var(--ink-muted)]">
            هذه المعلومات تظهر في لوحة الإدارة وتحدد رابط المتجر.
          </p>

          <label className="mt-6 block">
            <span className="mb-2 block text-[11px] font-semibold">
              اسم المتجر
            </span>

            <input
              value={
                name
              }
              disabled={
                isCreated
              }
              onChange={(event) =>
                handleNameChange(
                  event.target.value,
                )
              }
              placeholder="مثال: نور"
              className="h-11 w-full rounded-[8px] border border-black/[0.1] bg-white px-3 text-[12px] outline-none transition focus:border-black/30 disabled:bg-black/[0.025] disabled:text-black/50"
            />
          </label>

          <label className="mt-4 block">
            <span className="mb-2 block text-[11px] font-semibold">
              رابط المتجر
            </span>

            <div className="flex h-11 overflow-hidden rounded-[8px] border border-black/[0.1] bg-white focus-within:border-black/30">
              <input
                dir="ltr"
                value={
                  slug
                }
                disabled={
                  isCreated
                }
                onChange={(event) => {
                  setSlugTouched(
                    true,
                  );

                  setSlug(
                    slugify(
                      event.target.value,
                    ),
                  );
                }}
                placeholder="noor"
                className="min-w-0 flex-1 bg-transparent px-3 text-left text-[12px] outline-none disabled:bg-black/[0.025] disabled:text-black/50"
              />

              <span
                dir="ltr"
                className="flex items-center border-l border-black/[0.07] bg-black/[0.025] px-3 text-[10px] text-[var(--ink-muted)]"
              >
                .ofoq.store
              </span>
            </div>

            <p className="mt-2 text-[9px] leading-4 text-[var(--ink-muted)]">
              استخدم أحرفًا إنجليزية وأرقامًا وشرطة فقط.
            </p>
          </label>

          <div className="mt-6 border-t border-black/[0.07] pt-5">
            <span className="text-[10px] text-[var(--ink-muted)]">
              نوع النشاط المختار
            </span>

            <div className="mt-2 text-[13px] font-semibold">
              {selected?.label ??
                "لم يتم الاختيار"}
            </div>

            <p className="mt-1 text-[10px] leading-5 text-[var(--ink-soft)]">
              {selected?.description}
            </p>
          </div>

          {error ? (
            <div className="mt-5 rounded-[8px] border border-red-200 bg-red-50 px-3 py-3 text-[10px] leading-5 text-red-700">
              {error}
            </div>
          ) : null}

          {isCreated &&
          !pendingStore
            ?.verticalType ? (
            <div className="mt-5 rounded-[8px] border border-amber-200 bg-amber-50 px-3 py-3 text-[10px] leading-5 text-amber-800">
              تم إنشاء المتجر. بقي فقط حفظ نوع النشاط، لذلك لن ننشئ متجرًا ثانيًا عند إعادة المحاولة.
            </div>
          ) : null}

          <button
            type="button"
            disabled={
              saving
            }
            onClick={
              handleSave
            }
            className="mt-6 flex h-11 w-full items-center justify-center gap-2 rounded-[8px] bg-[#234239] px-4 text-[11px] font-semibold text-white transition hover:bg-[#1d3831] disabled:cursor-not-allowed disabled:opacity-60"
          >
            {saving
              ? "جاري الحفظ..."
              : isCreated
                ? "حفظ نوع النشاط"
                : "إنشاء المتجر والمتابعة"}

            {!saving ? (
              <ChevronLeft
                size={
                  16
                }
              />
            ) : null}
          </button>
        </aside>

        <section className="border border-black/[0.07] bg-[#fbfbf9] p-5 md:p-6">
          <div className="flex flex-wrap items-end justify-between gap-4 border-b border-black/[0.07] pb-5">
            <div>
              <h2 className="text-[15px] font-semibold">
                اختر نوع نشاطك
              </h2>

              <p className="mt-1 text-[10px] leading-5 text-[var(--ink-muted)]">
                الاختيار يساعد OFOQ على تجهيز الخصائص والمواصفات المناسبة لمتجرك.
              </p>
            </div>

            <label className="relative block w-full sm:w-[260px]">
              <Search
                size={
                  15
                }
                className="absolute right-3 top-1/2 -translate-y-1/2 text-black/35"
              />

              <input
                value={
                  query
                }
                onChange={(event) =>
                  setQuery(
                    event.target.value,
                  )
                }
                placeholder="ابحث عن نشاط"
                className="h-10 w-full rounded-[8px] border border-black/[0.09] bg-white pr-9 pl-3 text-[11px] outline-none focus:border-black/25"
              />
            </label>
          </div>

          <div className="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {filtered.map(
              (item) => {
                const active =
                  item.type ===
                  verticalType;

                return (
                  <button
                    key={
                      item.type
                    }
                    type="button"
                    onClick={() =>
                      setVerticalType(
                        item.type,
                      )
                    }
                    className={[
                      "relative min-h-[132px] border p-4 text-right transition",
                      active
                        ? "border-[#234239] bg-[#eef2ef]"
                        : "border-black/[0.07] bg-white hover:border-black/20",
                    ].join(
                      " ",
                    )}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="text-[12px] font-semibold">
                          {item.label}
                        </div>

                        <div
                          dir="ltr"
                          className="mt-1 text-left text-[9px] text-[var(--ink-muted)]"
                        >
                          {item.code}
                        </div>
                      </div>

                      <div
                        className={[
                          "flex size-6 shrink-0 items-center justify-center rounded-full border",
                          active
                            ? "border-[#234239] bg-[#234239] text-white"
                            : "border-black/[0.12] text-transparent",
                        ].join(
                          " ",
                        )}
                      >
                        <Check
                          size={
                            13
                          }
                        />
                      </div>
                    </div>

                    <p className="mt-4 text-[10px] leading-5 text-[var(--ink-soft)]">
                      {item.description}
                    </p>
                  </button>
                );
              },
            )}
          </div>

          {filtered.length ===
          0 ? (
            <div className="py-16 text-center text-[11px] text-[var(--ink-muted)]">
              ما لقينا نشاط مطابق. اختر متجر عام وتقدر تكمل إعداداتك لاحقًا.
            </div>
          ) : null}
        </section>
      </div>
    </div>
  );
}

function slugify(
  value: string,
) {
  return value
    .trim()
    .toLowerCase()
    .replace(
      /[^a-z0-9]+/g,
      "-",
    )
    .replace(
      /^-+|-+$/g,
      "",
    )
    .slice(
      0,
      63,
    );
}