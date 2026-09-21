import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  ArrowRight,
  Boxes,
  Check,
  ExternalLink,
  History,
  LoaderCircle,
  LockKeyhole,
  PackageCheck,
  Pencil,
  Play,
  RefreshCw,
  Settings2,
  ShieldCheck,
  SlidersHorizontal,
  Store,
  Users,
  X,
} from "lucide-react";

import {
  Link,
  useParams,
} from "react-router";

import {
  StatusBadge,
} from "./PlatformDashboardPage";

import {
  activatePlatformStore,
  changePlatformStoreVertical,
  getPlatformStore,
  setPlatformOwnerEmail,
  setPlatformStoreCapability,
  setPlatformStoreSubscription,
  suspendPlatformStore,
  updatePlatformStoreIdentity,
  type PlatformCapability,
  type PlatformStoreDetail,
} from "./platformApi";

import {
  beginPlatformTenantAdministration,
} from "../auth/platformTenantAdministration";

import {
  saveAdminStore,
} from "../admin/store-setup/storeSetupStorage";

type Section =
  | "overview"
  | "capabilities"
  | "audit";

type DialogState =
  | {
      kind: "status";
      action:
        | "suspend"
        | "activate";
    }
  | {
      kind: "identity";
    }
  | {
      kind: "vertical";
    }
  | {
      kind: "owner-email";
    }
  | {
      kind: "subscription";
    }
  | {
      kind: "capability";
      capability: PlatformCapability;
      nextValue: boolean | null;
    }
  | null;

const verticalLabels: Record<
  string,
  string
> = {
  "general-retail": "تجارة عامة",
  apparel: "أزياء",
  footwear: "أحذية",
  "mobile-phones": "جوالات",
  perfumes: "عطور",
  electronics: "إلكترونيات",
  subscriptions: "اشتراكات",
  services: "خدمات",
  "car-rental": "تأجير سيارات",
  "real-estate": "عقارات",
  restaurants: "مطاعم",
  "delivery-marketplace": "توصيل",
  grocery: "بقالة ومواد غذائية",
  "automotive-parts": "قطع سيارات",
  "digital-products": "منتجات رقمية",
  "jewelry-watches": "مجوهرات وساعات",
  "furniture-decor": "أثاث وديكور",
  cosmetics: "تجميل وعناية",
  "events-tickets": "فعاليات وتذاكر",
  "wholesale-b2b": "جملة وB2B",
  "personalized-gifts": "هدايا مخصصة",
  "equipment-rental": "تأجير معدات",
  "home-goods": "مستلزمات منزلية",
};

const capabilityLabels: Record<
  string,
  string
> = {
  PhysicalStock: "مخزون فعلي",
  Variants: "متغيرات المنتج",
  MultiWarehouse: "مستودعات متعددة",
  Barcode: "باركود",
  SerialTracking: "تتبع الرقم التسلسلي",
  ImeiTracking: "تتبع IMEI",
  LotTracking: "تتبع الدفعات",
  ExpiryTracking: "تتبع الصلاحية",
  Suppliers: "الموردون",
  PurchaseOrders: "أوامر الشراء",
  GoodsReceiving: "استلام البضائع",
  StockTransfers: "نقل المخزون",
  StockCounts: "جرد المخزون",
  Returns: "المرتجعات",
  Bundles: "الحزم",
  PreOrder: "الطلب المسبق",
  BackOrder: "الطلب عند نفاد المخزون",
  Warranty: "الضمان",
  CustomConfiguration: "تخصيص المنتج",
  RecipeInventory: "مخزون الوصفات",
  CompatibilityMatrix: "مصفوفة التوافق",
  Bookings: "الحجوزات",
  AssetCalendar: "تقويم الأصول",
  Reservations: "الحجز المسبق",
  Deposits: "العربون",
  Maintenance: "الصيانة",
  Listings: "القوائم",
  Leads: "العملاء المحتملون",
  Maps: "الخرائط",
  Appointments: "المواعيد",
  RecurringBilling: "الفوترة المتكررة",
  Entitlements: "الاستحقاقات",
  DigitalDelivery: "التسليم الرقمي",
  LicenseKeys: "مفاتيح التراخيص",
  SeatInventory: "مقاعد الفعاليات",
  QrTickets: "تذاكر QR",
  Delivery: "التوصيل",
  Drivers: "السائقون",
  DeliveryZones: "مناطق التوصيل",
  Branches: "الفروع",
  TableOrdering: "الطلب من الطاولة",
  KitchenWorkflow: "سير عمل المطبخ",
  AddOns: "الإضافات",
  B2B: "بيع للشركات",
  PriceTiers: "شرائح الأسعار",
  MinimumOrderQuantity: "حد أدنى للطلب",
  CreditTerms: "شروط الائتمان",
};

const auditLabels: Record<
  string,
  string
> = {
  "store.status.suspended": "إيقاف المتجر",
  "store.status.activated": "تفعيل المتجر",
  "store.identity.updated": "تعديل هوية المتجر",
  "store.vertical.changed": "تغيير النشاط الأساسي",
  "store.capability.override_set": "تعديل خاصية",
  "store.capability.override_reset": "إرجاع خاصية للوضع التلقائي",
  "store.owner.email_changed": "تغيير بريد المالك",
  "store.subscription.updated": "تعديل الاشتراك",
  "platform.request.approved": "الموافقة على طلب",
  "platform.request.rejected": "رفض طلب",
  "platform.request.more_info": "طلب معلومات إضافية",
};

export function PlatformStoreDetailPage() {
  const { tenantId = "" } = useParams();

  const [store, setStore] =
    useState<PlatformStoreDetail | null>(null);

  const [section, setSection] =
    useState<Section>("overview");

  const [loading, setLoading] =
    useState(true);

  const [busy, setBusy] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  const [dialog, setDialog] =
    useState<DialogState>(null);

  const [reason, setReason] =
    useState("");

  const [name, setName] =
    useState("");

  const [slug, setSlug] =
    useState("");

  const [verticalCode, setVerticalCode] =
    useState("");

  const [ownerEmail, setOwnerEmail] =
    useState("");

  const [planCode, setPlanCode] =
    useState("business");

  const [billingCycle, setBillingCycle] =
    useState<"Monthly" | "Annual">("Annual");

  const [subscriptionStatus, setSubscriptionStatus] =
    useState<"Active" | "Suspended">("Active");

  const [capabilitySearch, setCapabilitySearch] =
    useState("");

  const load = useCallback(
    async () => {
      if (!tenantId) {
        setError("معرف المتجر غير صالح.");
        setLoading(false);
        return;
      }

      try {
        const result =
          await getPlatformStore(tenantId);

        setStore(result);
        setError(null);
      } catch (exception) {
        setError(
          exception instanceof Error
            ? exception.message
            : "تعذر تحميل المتجر.",
        );
      } finally {
        setLoading(false);
      }
    },
    [tenantId],
  );

  useEffect(() => {
    const timer = window.setTimeout(
      () => {
        void load();
      },
      0,
    );

    return () => {
      window.clearTimeout(timer);
    };
  }, [load]);

  const visibleCapabilities =
    useMemo(() => {
      if (!store) {
        return [];
      }

      const query =
        capabilitySearch
          .trim()
          .toLowerCase();

      if (!query) {
        return store.capabilities;
      }

      return store.capabilities.filter(
        (item) => {
          const label =
            capabilityLabels[
              item.capability
            ] ?? item.capability;

          return (
            item.capability
              .toLowerCase()
              .includes(query) ||
            label
              .toLowerCase()
              .includes(query)
          );
        },
      );
    }, [store, capabilitySearch]);

  function openDialog(
    next: Exclude<
      DialogState,
      null
    >,
  ) {
    if (!store) {
      return;
    }

    setReason("");
    setError(null);

    if (next.kind === "identity") {
      setName(store.name);
      setSlug(store.slug);
    }

    if (next.kind === "vertical") {
      setVerticalCode(
        store.primaryVerticalCode ??
          store.availableVerticals[0]
            ?.code ??
          "",
      );
    }

    if (next.kind === "owner-email") {
      setOwnerEmail(store.ownerEmail ?? "");
    }

    if (next.kind === "subscription") {
      setPlanCode(store.planCode ?? store.availablePlans[0]?.code ?? "business");
      setBillingCycle(
        store.billingCycle === "Monthly" ? "Monthly" : "Annual",
      );
      setSubscriptionStatus(
        store.subscriptionStatus === "Suspended" ? "Suspended" : "Active",
      );
    }

    setDialog(next);
  }

  async function submitDialog() {
    if (!store || !dialog || busy) {
      return;
    }

    if (!reason.trim()) {
      setError(
        "اكتب سبب الإجراء قبل الحفظ.",
      );
      return;
    }

    setBusy(true);
    setError(null);

    try {
      if (dialog.kind === "status") {
        if (dialog.action === "suspend") {
          await suspendPlatformStore(
            store.tenantId,
            reason.trim(),
          );
        } else {
          await activatePlatformStore(
            store.tenantId,
            reason.trim(),
          );
        }
      }

      if (dialog.kind === "identity") {
        await updatePlatformStoreIdentity(
          store.tenantId,
          {
            name: name.trim(),
            slug: slug.trim(),
            reason: reason.trim(),
          },
        );
      }

      if (dialog.kind === "vertical") {
        await changePlatformStoreVertical(
          store.tenantId,
          {
            verticalCode,
            reason: reason.trim(),
          },
        );
      }

      if (dialog.kind === "owner-email") {
        await setPlatformOwnerEmail(
          store.tenantId,
          {
            email: ownerEmail.trim(),
            reason: reason.trim(),
          },
        );
      }

      if (dialog.kind === "subscription") {
        await setPlatformStoreSubscription(
          store.tenantId,
          {
            planCode,
            billingCycle,
            status: subscriptionStatus,
            reason: reason.trim(),
          },
        );
      }

      if (dialog.kind === "capability") {
        await setPlatformStoreCapability(
          store.tenantId,
          dialog.capability.capability,
          {
            isEnabled: dialog.nextValue,
            reason: reason.trim(),
          },
        );
      }

      setDialog(null);
      await load();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر تنفيذ الإجراء.",
      );
    } finally {
      setBusy(false);
    }
  }

  if (loading) {
    return (
      <div className="flex min-h-[520px] items-center justify-center">
        <div className="text-center">
          <RefreshCw
            size={20}
            className="mx-auto animate-spin text-black/25"
          />
          <p className="mt-4 text-[11px] text-black/40">
            جاري تحميل ملف المتجر
          </p>
        </div>
      </div>
    );
  }

  if (!store) {
    return (
      <div className="mx-auto max-w-[900px] rounded-[18px] border border-red-200 bg-white p-7 shadow-[0_18px_55px_rgba(18,17,14,0.05)]">
        <p className="text-[12px] font-semibold text-red-700">
          {error ?? "المتجر غير موجود."}
        </p>
      </div>
    );
  }

  const primaryVerticalLabel =
    store.primaryVerticalCode
      ? verticalLabels[
          store.primaryVerticalCode
        ] ?? store.primaryVerticalCode
      : "لم يحدد بعد";

  return (
    <div className="mx-auto max-w-[1420px] pb-10">
      <Link
        to="/platform/stores"
        className="inline-flex items-center gap-2 text-[10px] font-semibold text-black/42 transition hover:text-black"
      >
        <ArrowRight size={14} />
        العودة للمتاجر
      </Link>

      <section className="mt-5 overflow-hidden rounded-[24px] border border-black/[0.065] bg-[#fbfaf7] shadow-[0_24px_70px_rgba(26,22,14,0.055)]">
        <div className="border-b border-black/[0.06] px-6 py-6 md:px-8 md:py-7">
          <div className="flex flex-col gap-6 xl:flex-row xl:items-end xl:justify-between">
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-3">
                <h1 className="truncate text-[30px] font-semibold tracking-[-0.045em] md:text-[36px]">
                  {store.name}
                </h1>
                <StatusBadge value={store.status} />
              </div>

              <div className="mt-3 flex flex-wrap items-center gap-x-5 gap-y-2 text-[10px] text-black/42">
                <span>{primaryVerticalLabel}</span>
                <span className="hidden size-1 rounded-full bg-black/18 sm:block" />
                <span dir="ltr" className="text-left">
                  {store.slug}.ofoq.store
                </span>
                <span className="hidden size-1 rounded-full bg-black/18 sm:block" />
                <span dir="ltr" className="text-left">
                  {store.ownerEmail ?? "مالك غير محدد"}
                </span>
              </div>
            </div>

            <div className="flex flex-wrap gap-2">
              <a
                href={`/store/${store.slug}`}
                target="_blank"
                rel="noreferrer"
                className="inline-flex h-11 items-center gap-2 rounded-[10px] border border-black/[0.09] bg-white px-4 text-[10px] font-semibold transition hover:border-black/20"
              >
                فتح المتجر
                <ExternalLink size={13} />
              </a>

              <button
                type="button"
                onClick={() =>
                  openDialog({
                    kind: "identity",
                  })
                }
                className="inline-flex h-11 items-center gap-2 rounded-[10px] bg-[#0b0e16] px-4 text-[10px] font-semibold text-white transition hover:bg-black"
              >
                <Pencil size={13} />
                تعديل البيانات
              </button>
            </div>
          </div>
        </div>

        <div className="grid gap-px bg-black/[0.055] sm:grid-cols-3">
          <Metric
            label="المنتجات"
            value={store.productsCount}
            icon={Boxes}
          />
          <Metric
            label="الطلبات"
            value={store.ordersCount}
            icon={PackageCheck}
          />
          <Metric
            label="فريق المتجر"
            value={store.teamSize}
            icon={Users}
          />
        </div>
      </section>

      {error ? (
        <div className="mt-4 flex items-start justify-between gap-4 rounded-[13px] border border-red-200 bg-red-50 px-4 py-3 text-[10px] text-red-700">
          <span>{error}</span>
          <button
            type="button"
            onClick={() => setError(null)}
            className="text-red-700/65"
            aria-label="إغلاق الرسالة"
          >
            <X size={13} />
          </button>
        </div>
      ) : null}

      <div className="mt-6 flex gap-1 overflow-x-auto rounded-[13px] border border-black/[0.06] bg-white p-1.5 shadow-[0_12px_35px_rgba(20,18,13,0.025)]">
        <SectionButton
          active={section === "overview"}
          onClick={() => setSection("overview")}
          icon={Store}
        >
          نظرة المتجر
        </SectionButton>
        <SectionButton
          active={section === "capabilities"}
          onClick={() => setSection("capabilities")}
          icon={SlidersHorizontal}
        >
          الخصائص
        </SectionButton>
        <SectionButton
          active={section === "audit"}
          onClick={() => setSection("audit")}
          icon={History}
        >
          سجل الإدارة
        </SectionButton>
      </div>

      {section === "overview" ? (
        <OverviewSection
          store={store}
          primaryVerticalLabel={primaryVerticalLabel}
          openDialog={openDialog}
        />
      ) : null}

      {section === "capabilities" ? (
        <section className="mt-5 rounded-[20px] border border-black/[0.065] bg-white p-5 shadow-[0_18px_60px_rgba(20,18,13,0.035)] md:p-7">
          <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
            <div>
              <p className="text-[9px] font-semibold text-[#986f3a]">
                CAPABILITY CONTROL
              </p>
              <h2 className="mt-2 text-[20px] font-semibold tracking-[-0.025em]">
                خصائص المتجر
              </h2>
              <p className="mt-2 max-w-[700px] text-[10px] leading-6 text-black/42">
                الوضع التلقائي يأتي من نشاط المتجر. استخدم التخصيص فقط عندما يحتاج هذا المتجر استثناءً واضحًا.
              </p>
            </div>

            <input
              value={capabilitySearch}
              onChange={(event) =>
                setCapabilitySearch(
                  event.target.value,
                )
              }
              placeholder="ابحث عن خاصية"
              className="h-10 w-full rounded-[9px] border border-black/[0.09] bg-[#faf9f6] px-3 text-[10px] outline-none transition placeholder:text-black/28 focus:border-black/25 md:w-[250px]"
            />
          </div>

          <div className="mt-6 divide-y divide-black/[0.055] border-y border-black/[0.055]">
            {visibleCapabilities.map(
              (capability) => (
                <CapabilityRow
                  key={capability.capability}
                  capability={capability}
                  onChange={(nextValue) =>
                    openDialog({
                      kind: "capability",
                      capability,
                      nextValue,
                    })
                  }
                />
              ),
            )}
          </div>
        </section>
      ) : null}

      {section === "audit" ? (
        <AuditSection store={store} />
      ) : null}

      {dialog ? (
        <ActionDialog
          dialog={dialog}
          store={store}
          busy={busy}
          reason={reason}
          setReason={setReason}
          name={name}
          setName={setName}
          slug={slug}
          setSlug={setSlug}
          verticalCode={verticalCode}
          setVerticalCode={setVerticalCode}
          ownerEmail={ownerEmail}
          setOwnerEmail={setOwnerEmail}
          planCode={planCode}
          setPlanCode={setPlanCode}
          billingCycle={billingCycle}
          setBillingCycle={setBillingCycle}
          subscriptionStatus={subscriptionStatus}
          setSubscriptionStatus={setSubscriptionStatus}
          onClose={() => {
            if (!busy) {
              setDialog(null);
              setError(null);
            }
          }}
          onSubmit={() => {
            void submitDialog();
          }}
        />
      ) : null}
    </div>
  );
}

function OverviewSection({
  store,
  primaryVerticalLabel,
  openDialog,
}: {
  store: PlatformStoreDetail;
  primaryVerticalLabel: string;
  openDialog: (
    dialog: Exclude<DialogState, null>,
  ) => void;
}) {
  return (
    <div className="mt-5 grid gap-5 xl:grid-cols-[1.35fr_.65fr]">
      <div className="space-y-5">
        <section className="rounded-[20px] border border-black/[0.065] bg-white p-6 shadow-[0_18px_60px_rgba(20,18,13,0.035)] md:p-7">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[9px] font-semibold text-[#986f3a]">
                STORE IDENTITY
              </p>
              <h2 className="mt-2 text-[18px] font-semibold tracking-[-0.02em]">
                هوية المتجر
              </h2>
            </div>

            <button
              type="button"
              onClick={() =>
                openDialog({
                  kind: "identity",
                })
              }
              className="inline-flex h-9 items-center gap-2 rounded-[8px] border border-black/[0.08] px-3 text-[9px] font-semibold transition hover:border-black/20"
            >
              <Pencil size={12} />
              تعديل
            </button>
          </div>

          <div className="mt-6 divide-y divide-black/[0.055]">
            <InfoRow
              label="اسم المتجر"
              value={store.name}
            />
            <InfoRow
              label="الرابط الأساسي"
              value={`${store.slug}.ofoq.store`}
              ltr
            />
            <div className="flex items-center justify-between gap-4 py-3">
              <div className="min-w-0">
                <p className="text-[9px] text-black/34">المالك</p>
                <p dir="ltr" className="mt-1 truncate text-left text-[10px] font-medium">
                  {store.ownerEmail ?? "غير معروف"}
                </p>
              </div>
              <button
                type="button"
                onClick={() => openDialog({ kind: "owner-email" })}
                className="shrink-0 rounded-[8px] border border-black/[0.08] px-3 py-2 text-[8px] font-semibold hover:border-black/20"
              >
                {store.ownerEmail ? "تغيير البريد" : "ربط المالك"}
              </button>
            </div>
            <InfoRow
              label="تاريخ الإنشاء"
              value={formatDate(
                store.createdAtUtc,
              )}
            />
          </div>
        </section>

        <section className="rounded-[20px] border border-black/[0.065] bg-white p-6 shadow-[0_18px_60px_rgba(20,18,13,0.035)] md:p-7">
          <div className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="text-[9px] font-semibold text-[#986f3a]">
                PRIMARY ACTIVITY
              </p>
              <h2 className="mt-2 text-[18px] font-semibold">
                {primaryVerticalLabel}
              </h2>
              <p className="mt-2 text-[10px] text-black/40">
                {store.primaryVerticalCode ??
                  "النشاط لم يكتمل بعد"}
              </p>
            </div>

            <button
              type="button"
              onClick={() =>
                openDialog({
                  kind: "vertical",
                })
              }
              className="inline-flex h-10 items-center justify-center gap-2 rounded-[9px] border border-black/[0.09] bg-[#faf9f6] px-4 text-[9px] font-semibold transition hover:border-black/20"
            >
              <Settings2 size={13} />
              تغيير النشاط
            </button>
          </div>
        </section>
      </div>

      <aside className="space-y-5">
        <section className="overflow-hidden rounded-[20px] bg-[#0a0d15] text-white shadow-[0_22px_65px_rgba(6,8,14,0.16)]">
          <div className="border-b border-white/[0.08] p-6">
            <div className="flex items-center justify-between gap-4">
              <div className="flex size-10 items-center justify-center rounded-[10px] bg-[#d0aa70] text-[#0a0d15]">
                <ShieldCheck size={18} />
              </div>
              <span className="text-[9px] font-medium text-white/34">
                PLATFORM CONTROL
              </span>
            </div>

            <h2 className="mt-6 text-[19px] font-semibold tracking-[-0.02em]">
              التحكم التشغيلي
            </h2>
            <p className="mt-3 text-[10px] leading-6 text-white/48">
              كل إجراء حساس يحتاج سببًا ويتم تسجيله في سجل إدارة مستقل.
            </p>
          </div>

          <div className="space-y-2 p-4">
            {store.status === "Suspended" ? (
              <button
                type="button"
                onClick={() =>
                  openDialog({
                    kind: "status",
                    action: "activate",
                  })
                }
                className="flex h-11 w-full items-center justify-center gap-2 rounded-[9px] bg-[#d0aa70] text-[10px] font-semibold text-[#080b14] transition hover:bg-[#d8b782]"
              >
                <Play size={13} />
                إعادة تشغيل المتجر
              </button>
            ) : (
              <button
                type="button"
                onClick={() =>
                  openDialog({
                    kind: "status",
                    action: "suspend",
                  })
                }
                className="flex h-11 w-full items-center justify-center gap-2 rounded-[9px] border border-red-300/20 bg-red-400/[0.08] text-[10px] font-semibold text-red-200 transition hover:bg-red-400/[0.12]"
              >
                <LockKeyhole size={13} />
                إيقاف المتجر
              </button>
            )}

            <button
              type="button"
              onClick={() => {
                beginPlatformTenantAdministration(
                  store.tenantId,
                );

                saveAdminStore({
                  tenantId:
                    store.tenantId,
                  name:
                    store.name,
                  slug:
                    store.slug,
                  status:
                    store.status,
                  verticalType:
                    store.primaryVertical ??
                    undefined,
                  verticalCode:
                    store.primaryVerticalCode ??
                    undefined,
                });

                window.location.assign(
                  `/admin?platformTenant=${encodeURIComponent(store.tenantId)}`,
                );
              }}
              className="flex h-11 w-full items-center justify-center gap-2 rounded-[9px] border border-[#d0aa70]/45 bg-[#d0aa70]/10 text-[10px] font-semibold text-[#ead5b3] transition hover:border-[#d0aa70]/70 hover:bg-[#d0aa70]/15"
            >
              <Store size={13} />
              إدارة المتجر كمنصة
            </button>
          </div>
        </section>

        <section className="rounded-[20px] border border-black/[0.065] bg-[#f0e7d8] p-6">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-[9px] font-semibold text-[#8b632f]">
                SUBSCRIPTION
              </p>
              <h3 className="mt-3 text-[18px] font-semibold">
                {store.planName ?? "لا توجد باقة مفعلة"}
              </h3>
            </div>
            {store.pendingRequests > 0 ? (
              <span className="rounded-full bg-white/65 px-2.5 py-1 text-[8px] font-semibold text-[#8b632f]">
                {store.pendingRequests} طلب
              </span>
            ) : null}
          </div>

          <div className="mt-4 grid grid-cols-2 gap-2 text-[9px]">
            <div className="rounded-[10px] bg-white/55 p-3">
              <p className="text-black/35">الحالة</p>
              <p className="mt-1 font-semibold">
                {store.subscriptionStatus === "Suspended" ? "موقوفة" : store.subscriptionStatus === "Active" ? "فعالة" : "غير مربوطة"}
              </p>
            </div>
            <div className="rounded-[10px] bg-white/55 p-3">
              <p className="text-black/35">الفوترة</p>
              <p className="mt-1 font-semibold">
                {store.billingCycle === "Monthly" ? "شهري" : store.billingCycle === "Annual" ? "سنوي" : "—"}
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={() => openDialog({ kind: "subscription" })}
            className="mt-4 flex h-10 w-full items-center justify-center gap-2 rounded-[9px] bg-[#0a0d15] text-[9px] font-semibold text-white"
          >
            <Settings2 size={13} />
            إدارة الاشتراك
          </button>
        </section>
      </aside>
    </div>
  );
}

function CapabilityRow({
  capability,
  onChange,
}: {
  capability: PlatformCapability;
  onChange: (
    value: boolean | null,
  ) => void;
}) {
  const label =
    capabilityLabels[
      capability.capability
    ] ?? capability.capability;

  return (
    <div className="grid gap-4 py-4 md:grid-cols-[1fr_auto] md:items-center">
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <p className="text-[11px] font-semibold">
            {label}
          </p>
          <span
            className={[
              "rounded-full px-2 py-1 text-[8px] font-semibold",
              capability.effectiveEnabled
                ? "bg-emerald-50 text-emerald-700"
                : "bg-black/[0.045] text-black/42",
            ].join(" ")}
          >
            {capability.effectiveEnabled
              ? "مفعلة"
              : "غير مفعلة"}
          </span>
          {capability.overrideValue !== null ? (
            <span className="rounded-full bg-[#f0e7d8] px-2 py-1 text-[8px] font-semibold text-[#8b632f]">
              Override
            </span>
          ) : null}
        </div>

        <p className="mt-1.5 text-[9px] text-black/35">
          المصدر الافتراضي: {capability.defaultEnabled ? "النشاط يفعّلها" : "غير مفعلة لهذا النشاط"}
        </p>
      </div>

      <div className="flex w-fit rounded-[9px] border border-black/[0.07] bg-[#f7f5f0] p-1">
        <CapabilityChoice
          active={capability.overrideValue === null}
          onClick={() => onChange(null)}
        >
          تلقائي
        </CapabilityChoice>
        <CapabilityChoice
          active={capability.overrideValue === true}
          onClick={() => onChange(true)}
        >
          تفعيل
        </CapabilityChoice>
        <CapabilityChoice
          active={capability.overrideValue === false}
          onClick={() => onChange(false)}
        >
          إيقاف
        </CapabilityChoice>
      </div>
    </div>
  );
}

function CapabilityChoice({
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
        "h-8 rounded-[7px] px-3 text-[8px] font-semibold transition",
        active
          ? "bg-white text-black shadow-[0_3px_12px_rgba(0,0,0,0.06)]"
          : "text-black/38 hover:text-black/70",
      ].join(" ")}
    >
      {children}
    </button>
  );
}

function AuditSection({
  store,
}: {
  store: PlatformStoreDetail;
}) {
  return (
    <section className="mt-5 rounded-[20px] border border-black/[0.065] bg-white p-5 shadow-[0_18px_60px_rgba(20,18,13,0.035)] md:p-7">
      <div>
        <p className="text-[9px] font-semibold text-[#986f3a]">
          PLATFORM AUDIT
        </p>
        <h2 className="mt-2 text-[20px] font-semibold tracking-[-0.025em]">
          سجل الإدارة
        </h2>
        <p className="mt-2 text-[10px] leading-6 text-black/42">
          آخر الإجراءات التي نفذها فريق ركن على هذا المتجر.
        </p>
      </div>

      {store.recentAudit.length === 0 ? (
        <div className="mt-7 rounded-[14px] border border-dashed border-black/10 bg-[#faf9f6] px-5 py-10 text-center">
          <History
            size={19}
            className="mx-auto text-black/22"
          />
          <p className="mt-3 text-[10px] text-black/38">
            لم تسجل إجراءات إدارية بعد
          </p>
        </div>
      ) : (
        <div className="mt-7 divide-y divide-black/[0.055] border-y border-black/[0.055]">
          {store.recentAudit.map(
            (entry) => (
              <div
                key={entry.auditEntryId}
                className="grid gap-3 py-5 md:grid-cols-[170px_1fr_190px] md:items-start"
              >
                <div>
                  <p className="text-[10px] font-semibold">
                    {auditLabels[entry.action] ?? entry.action}
                  </p>
                  <p className="mt-1 text-[8px] text-black/30">
                    {entry.action}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] leading-6 text-black/58">
                    {entry.reason}
                  </p>
                  <p
                    dir="ltr"
                    className="mt-2 text-left text-[8px] text-black/28"
                  >
                    actor {entry.actorUserId}
                  </p>
                </div>

                <p className="text-[9px] text-black/36 md:text-left">
                  {formatDate(entry.occurredAtUtc)}
                </p>
              </div>
            ),
          )}
        </div>
      )}
    </section>
  );
}

function ActionDialog({
  dialog,
  store,
  busy,
  reason,
  setReason,
  name,
  setName,
  slug,
  setSlug,
  verticalCode,
  setVerticalCode,
  ownerEmail,
  setOwnerEmail,
  planCode,
  setPlanCode,
  billingCycle,
  setBillingCycle,
  subscriptionStatus,
  setSubscriptionStatus,
  onClose,
  onSubmit,
}: {
  dialog: Exclude<DialogState, null>;
  store: PlatformStoreDetail;
  busy: boolean;
  reason: string;
  setReason: (value: string) => void;
  name: string;
  setName: (value: string) => void;
  slug: string;
  setSlug: (value: string) => void;
  verticalCode: string;
  setVerticalCode: (value: string) => void;
  ownerEmail: string;
  setOwnerEmail: (value: string) => void;
  planCode: string;
  setPlanCode: (value: string) => void;
  billingCycle: "Monthly" | "Annual";
  setBillingCycle: (value: "Monthly" | "Annual") => void;
  subscriptionStatus: "Active" | "Suspended";
  setSubscriptionStatus: (value: "Active" | "Suspended") => void;
  onClose: () => void;
  onSubmit: () => void;
}) {
  const content = getDialogContent(dialog);

  return (
    <div className="fixed inset-0 z-[90] flex items-center justify-center bg-[#05070c]/56 px-4 py-6 backdrop-blur-[2px]">
      <button
        type="button"
        aria-label="إغلاق"
        className="absolute inset-0"
        onClick={onClose}
      />

      <div
        role="dialog"
        aria-modal="true"
        className="relative z-10 w-full max-w-[560px] overflow-hidden rounded-[20px] border border-white/10 bg-[#fbfaf7] shadow-[0_34px_100px_rgba(0,0,0,0.28)]"
      >
        <div className="flex items-start justify-between gap-4 border-b border-black/[0.06] px-6 py-5">
          <div>
            <p className="text-[9px] font-semibold text-[#966d38]">
              PLATFORM ACTION
            </p>
            <h2 className="mt-2 text-[18px] font-semibold">
              {content.title}
            </h2>
            <p className="mt-2 text-[9px] leading-5 text-black/40">
              {content.description}
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={busy}
            className="flex size-8 shrink-0 items-center justify-center rounded-full border border-black/[0.07] bg-white text-black/45"
            aria-label="إغلاق"
          >
            <X size={13} />
          </button>
        </div>

        <div className="space-y-4 px-6 py-5">
          {dialog.kind === "identity" ? (
            <>
              <Field label="اسم المتجر">
                <input
                  value={name}
                  onChange={(event) =>
                    setName(event.target.value)
                  }
                  className="h-11 w-full rounded-[9px] border border-black/[0.09] bg-white px-3 text-[11px] outline-none focus:border-black/25"
                />
              </Field>

              <Field label="الرابط الأساسي">
                <div className="flex h-11 overflow-hidden rounded-[9px] border border-black/[0.09] bg-white focus-within:border-black/25">
                  <input
                    dir="ltr"
                    value={slug}
                    onChange={(event) =>
                      setSlug(event.target.value)
                    }
                    className="min-w-0 flex-1 px-3 text-left text-[11px] outline-none"
                  />
                  <span className="flex items-center border-l border-black/[0.07] bg-[#f7f5f0] px-3 text-[9px] text-black/35">
                    .ofoq.store
                  </span>
                </div>
              </Field>
            </>
          ) : null}

          {dialog.kind === "vertical" ? (
            <Field label="النشاط الأساسي الجديد">
              <select
                value={verticalCode}
                onChange={(event) =>
                  setVerticalCode(
                    event.target.value,
                  )
                }
                className="h-11 w-full rounded-[9px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/25"
              >
                {store.availableVerticals.map(
                  (item) => (
                    <option
                      key={item.code}
                      value={item.code}
                    >
                      {verticalLabels[item.code] ?? item.code}
                    </option>
                  ),
                )}
              </select>
            </Field>
          ) : null}

          {dialog.kind === "owner-email" ? (
            <Field label="بريد المالك">
              <input
                type="email"
                dir="ltr"
                value={ownerEmail}
                onChange={(event) => setOwnerEmail(event.target.value)}
                placeholder="owner@example.com"
                className="h-11 w-full rounded-[9px] border border-black/[0.09] bg-white px-3 text-left text-[11px] outline-none focus:border-black/25"
              />
              {!store.ownerEmail ? (
                <p className="mt-2 text-[8px] leading-5 text-black/35">
                  إذا لم يكن للمتجر مالك حاليًا، يجب أن يكون هناك حساب مستخدم فعال بهذا البريد حتى يتم ربطه كمالك.
                </p>
              ) : (
                <p className="mt-2 text-[8px] leading-5 text-black/35">
                  تغيير البريد يغيّر بريد تسجيل دخول المالك ويُسجل كإجراء أمني في سجل الإدارة.
                </p>
              )}
            </Field>
          ) : null}

          {dialog.kind === "subscription" ? (
            <>
              <Field label="الباقة">
                <select
                  value={planCode}
                  onChange={(event) => setPlanCode(event.target.value)}
                  className="h-11 w-full rounded-[9px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/25"
                >
                  {store.availablePlans.map((plan) => (
                    <option key={plan.code} value={plan.code}>
                      {plan.name}
                    </option>
                  ))}
                </select>
              </Field>

              <div className="grid grid-cols-2 gap-3">
                <Field label="الفوترة">
                  <select
                    value={billingCycle}
                    onChange={(event) => setBillingCycle(event.target.value as "Monthly" | "Annual")}
                    className="h-11 w-full rounded-[9px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/25"
                  >
                    <option value="Monthly">شهري</option>
                    <option value="Annual">سنوي</option>
                  </select>
                </Field>

                <Field label="الحالة">
                  <select
                    value={subscriptionStatus}
                    onChange={(event) => setSubscriptionStatus(event.target.value as "Active" | "Suspended")}
                    className="h-11 w-full rounded-[9px] border border-black/[0.09] bg-white px-3 text-[10px] outline-none focus:border-black/25"
                  >
                    <option value="Active">فعالة</option>
                    <option value="Suspended">موقوفة</option>
                  </select>
                </Field>
              </div>
            </>
          ) : null}

          {dialog.kind === "capability" ? (
            <div className="rounded-[12px] border border-black/[0.07] bg-white p-4">
              <p className="text-[10px] font-semibold">
                {capabilityLabels[
                  dialog.capability.capability
                ] ?? dialog.capability.capability}
              </p>
              <p className="mt-2 text-[9px] leading-5 text-black/40">
                الحالة الجديدة: {dialog.nextValue === null ? "تلقائي حسب النشاط" : dialog.nextValue ? "مفعلة لهذا المتجر" : "موقوفة لهذا المتجر"}
              </p>
            </div>
          ) : null}

          <Field label="سبب الإجراء" required>
            <textarea
              value={reason}
              onChange={(event) =>
                setReason(event.target.value)
              }
              maxLength={500}
              rows={4}
              placeholder="مثال: طلب العميل تعديل النشاط بعد مراجعة بيانات المتجر"
              className="w-full resize-none rounded-[10px] border border-black/[0.09] bg-white px-3 py-3 text-[10px] leading-6 outline-none placeholder:text-black/25 focus:border-black/25"
            />
            <div className="mt-1.5 flex justify-between text-[8px] text-black/28">
              <span>سيظهر السبب في سجل الإدارة</span>
              <span>{reason.length}/500</span>
            </div>
          </Field>
        </div>

        <div className="flex items-center justify-end gap-2 border-t border-black/[0.06] bg-white px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            disabled={busy}
            className="h-10 rounded-[9px] border border-black/[0.08] px-4 text-[9px] font-semibold text-black/55 disabled:opacity-40"
          >
            إلغاء
          </button>

          <button
            type="button"
            onClick={onSubmit}
            disabled={
              busy ||
              !reason.trim()
            }
            className={[
              "inline-flex h-10 items-center gap-2 rounded-[9px] px-5 text-[9px] font-semibold disabled:opacity-35",
              dialog.kind === "status" &&
              dialog.action === "suspend"
                ? "bg-[#8f2f2f] text-white"
                : "bg-[#0a0d15] text-white",
            ].join(" ")}
          >
            {busy ? (
              <LoaderCircle
                size={13}
                className="animate-spin"
              />
            ) : (
              <Check size={13} />
            )}
            {busy ? "جاري الحفظ" : content.submitLabel}
          </button>
        </div>
      </div>
    </div>
  );
}

function getDialogContent(
  dialog: Exclude<DialogState, null>,
) {
  if (dialog.kind === "identity") {
    return {
      title: "تعديل هوية المتجر",
      description:
        "التعديل يتم مباشرة بصلاحية المنصة ويسجل في سجل الإدارة.",
      submitLabel: "حفظ التعديل",
    };
  }

  if (dialog.kind === "vertical") {
    return {
      title: "تغيير النشاط الأساسي",
      description:
        "سيتم تحديث النشاط الذي تعتمد عليه خصائص المتجر الافتراضية.",
      submitLabel: "تغيير النشاط",
    };
  }

  if (dialog.kind === "owner-email") {
    return {
      title: "تغيير بريد مالك المتجر",
      description:
        "إجراء أمني مباشر من إدارة ركن. يتم تسجيل البريد القديم والجديد والسبب في سجل الإدارة.",
      submitLabel: "حفظ البريد",
    };
  }

  if (dialog.kind === "subscription") {
    return {
      title: "إدارة اشتراك المتجر",
      description:
        "يمكنك تعيين الباقة أو ترقيتها أو إيقاف الاشتراك يدويًا مع سبب إداري واضح.",
      submitLabel: "حفظ الاشتراك",
    };
  }

  if (dialog.kind === "capability") {
    return {
      title: "تعديل خاصية المتجر",
      description:
        "هذا التغيير خاص بهذا المتجر ولا يغير الإعداد الافتراضي لبقية المتاجر.",
      submitLabel: "تأكيد التعديل",
    };
  }

  return dialog.action === "suspend"
    ? {
        title: "إيقاف المتجر",
        description:
          "سيتم منع الوصول التشغيلي للمتجر إلى أن تعيد تشغيله من المنصة.",
        submitLabel: "إيقاف المتجر",
      }
    : {
        title: "إعادة تشغيل المتجر",
        description:
          "سيعود المتجر إلى الحالة الفعالة وسيتم تسجيل سبب إعادة التشغيل.",
        submitLabel: "إعادة التشغيل",
      };
}

function SectionButton({
  active,
  onClick,
  icon: Icon,
  children,
}: {
  active: boolean;
  onClick: () => void;
  icon: typeof Store;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        "inline-flex h-9 shrink-0 items-center gap-2 rounded-[9px] px-4 text-[9px] font-semibold transition",
        active
          ? "bg-[#0b0e16] text-white"
          : "text-black/42 hover:bg-black/[0.035] hover:text-black",
      ].join(" ")}
    >
      <Icon size={13} />
      {children}
    </button>
  );
}

function Metric({
  label,
  value,
  icon: Icon,
}: {
  label: string;
  value: number;
  icon: typeof Store;
}) {
  return (
    <div className="bg-[#fbfaf7] px-6 py-5">
      <div className="flex items-center justify-between">
        <p className="text-[9px] font-medium text-black/38">
          {label}
        </p>
        <Icon
          size={15}
          className="text-[#9a713f]"
        />
      </div>
      <p className="mt-4 text-[24px] font-semibold tracking-[-0.045em]">
        {value}
      </p>
    </div>
  );
}

function InfoRow({
  label,
  value,
  ltr = false,
}: {
  label: string;
  value: string;
  ltr?: boolean;
}) {
  return (
    <div className="grid gap-2 py-4 sm:grid-cols-[150px_1fr] sm:items-center">
      <span className="text-[9px] font-medium text-black/36">
        {label}
      </span>
      <span
        dir={ltr ? "ltr" : undefined}
        className={[
          "text-[10px] font-semibold",
          ltr ? "text-left" : "",
        ].join(" ")}
      >
        {value}
      </span>
    </div>
  );
}

function Field({
  label,
  required = false,
  children,
}: {
  label: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-[9px] font-semibold text-black/52">
        {label}
        {required ? (
          <span className="mr-1 text-[#9a713f]">
            *
          </span>
        ) : null}
      </span>
      {children}
    </label>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(
    "ar-SA",
    {
      dateStyle: "medium",
      timeStyle: "short",
    },
  ).format(new Date(value));
}
