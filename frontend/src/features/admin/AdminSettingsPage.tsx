import {
  ArrowLeft,
  Building2,
  CheckCircle2,
  ChevronLeft,
  Clock3,
  History,
  KeyRound,
  Mail,
  RefreshCw,
  ShieldCheck,
  SlidersHorizontal,
  Store,
  UserRound,
  X,
  XCircle,
  type LucideIcon,
} from "lucide-react";

import {
  Link,
} from "react-router";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

import {
  getMyPlatformRequests,
  submitOwnerEmailChangeRequest,
  submitPlanChangeRequest,
  submitStoreIdentityChangeRequest,
  type MerchantPlatformRequest,
} from "./requests/merchantRequestsApi";

import {
  readAdminStore,
} from "./store-setup/storeSetupStorage";

import {
  VERTICAL_OPTIONS,
} from "./store-setup/verticalCatalog";

import {
  StoreContactSettings,
} from "./store-profile/StoreContactSettings";

import {
  authRequest,
  getCurrentUser,
} from "../auth/authSession";

import {
  type MfaReopenPolicy,
} from "../auth/mfaSessionPolicy";

import "./AdminSettingsV3.css";

type RequestKind =
  | "plan"
  | "identity"
  | "email"
  | null;

type SettingsSection =
  | "account"
  | "store"
  | "security"
  | "requests";

const statusLabels: Record<string, string> = {
  Pending: "قيد المراجعة",
  MoreInfoRequested: "مطلوب معلومات",
  Approved: "تمت الموافقة",
  Rejected: "مرفوض",
};

const typeLabels: Record<string, string> = {
  StoreRegistration: "اعتماد المتجر",
  PlanChange: "تغيير الباقة",
  StoreIdentityChange: "تغيير بيانات المتجر",
  OwnerEmailChange: "تغيير بريد المالك",
};

function storeStatusLabel(
  value: string | undefined,
) {
  const status =
    value
      ?.trim()
      .toLowerCase();

  if (
    status === "active"
  ) {
    return "فعال";
  }

  if (
    status === "suspended"
  ) {
    return "موقوف";
  }

  return "قيد الإعداد";
}

export function AdminSettingsPage() {
  const store =
    readAdminStore();

  const [
    activeSection,
    setActiveSection,
  ] =
    useState<SettingsSection>(
      "account",
    );

  const [
    contactOpen,
    setContactOpen,
  ] =
    useState(false);

  const [
    requests,
    setRequests,
  ] =
    useState<
      MerchantPlatformRequest[]
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
    kind,
    setKind,
  ] =
    useState<RequestKind>(
      null,
    );

  const [
    busy,
    setBusy,
  ] =
    useState(false);

  const [
    planCode,
    setPlanCode,
  ] =
    useState(
      "pro",
    );

  const [
    billingCycle,
    setBillingCycle,
  ] =
    useState<
      "Monthly" |
      "Annual"
    >(
      "Annual",
    );

  const [
    reason,
    setReason,
  ] =
    useState("");

  const [
    name,
    setName,
  ] =
    useState(
      store?.name ??
      "",
    );

  const [
    slug,
    setSlug,
  ] =
    useState(
      store?.slug ??
      "",
    );

  const [
    verticalCode,
    setVerticalCode,
  ] =
    useState(
      store?.verticalCode ??
      "",
    );

  const [
    email,
    setEmail,
  ] =
    useState("");

  const [
    mfaPolicy,
    setMfaPolicy,
  ] =
    useState<MfaReopenPolicy>(
      "EveryBrowserSession",
    );

  const [
    securityLoading,
    setSecurityLoading,
  ] =
    useState(true);

  const [
    securityBusy,
    setSecurityBusy,
  ] =
    useState(false);

  const [
    securityMessage,
    setSecurityMessage,
  ] =
    useState<
      string |
      null
    >(null);

  const [
    securityError,
    setSecurityError,
  ] =
    useState<
      string |
      null
    >(null);

  const loadSecurity =
    useCallback(
      async () => {
        try {
          const user =
            await getCurrentUser();

          setMfaPolicy(
            user.mfaReopenPolicy,
          );

          setSecurityError(
            null,
          );
        } catch (
          exception
        ) {
          setSecurityError(
            exception instanceof
              Error
              ? exception.message
              : "تعذر تحميل إعدادات الأمان.",
          );
        } finally {
          setSecurityLoading(
            false,
          );
        }
      },
      [],
    );

  const load =
    useCallback(
      async () => {
        if (
          !store?.tenantId
        ) {
          setLoading(
            false,
          );

          return;
        }

        try {
          const result =
            await getMyPlatformRequests(
              store.tenantId,
            );

          setRequests(
            result,
          );

          setError(
            null,
          );
        } catch (
          exception
        ) {
          setError(
            exception instanceof
              Error
              ? exception.message
              : "تعذر تحميل الطلبات.",
          );
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
            void loadSecurity();
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
      loadSecurity,
    ],
  );

  useEffect(
    () => {
      if (
        !kind &&
        !contactOpen
      ) {
        return;
      }

      const previous =
        document.body
          .style
          .overflow;

      document.body
        .style
        .overflow =
        "hidden";

      return () => {
        document.body
          .style
          .overflow =
          previous;
      };
    },
    [
      kind,
      contactOpen,
    ],
  );

  const pendingCount =
    useMemo(
      () =>
        requests.filter(
          (
            request,
          ) =>
            request.status ===
            "Pending",
        ).length,
      [
        requests,
      ],
    );

  const verticalLabel =
    useMemo(
      () =>
        VERTICAL_OPTIONS.find(
          (
            item,
          ) =>
            item.code ===
            store?.verticalCode,
        )?.label ??
        "غير محدد",
      [
        store?.verticalCode,
      ],
    );

  async function updateMfaPolicy(
    nextPolicy:
      MfaReopenPolicy,
  ) {
    setSecurityBusy(
      true,
    );

    setSecurityMessage(
      null,
    );

    setSecurityError(
      null,
    );

    try {
      const result =
        await authRequest<{
          policy:
            MfaReopenPolicy;
        }>(
          "/api/auth/mfa/reopen-policy",
          {
            method:
              "PUT",

            body:
              JSON.stringify({
                policy:
                  nextPolicy,
              }),
          },
        );

      setMfaPolicy(
        result.policy,
      );

      setSecurityMessage(
        "تم حفظ سياسة إعادة التحقق.",
      );
    } catch (
      exception
    ) {
      setSecurityError(
        exception instanceof
          Error
          ? exception.message
          : "تعذر حفظ إعداد الأمان.",
      );
    } finally {
      setSecurityBusy(
        false,
      );
    }
  }

  function openRequest(
    nextKind:
      Exclude<
        RequestKind,
        null
      >,
  ) {
    setKind(
      nextKind,
    );

    setReason("");
    setError(null);

    if (
      nextKind ===
      "identity"
    ) {
      setName(
        store?.name ??
        "",
      );

      setSlug(
        store?.slug ??
        "",
      );

      setVerticalCode(
        store?.verticalCode ??
        "",
      );
    }
  }

  async function submit() {
    if (
      !store?.tenantId ||
      !kind
    ) {
      return;
    }

    const cleanReason =
      reason.trim();

    if (
      cleanReason.length <
      3
    ) {
      setError(
        "اكتب سببًا واضحًا للطلب.",
      );

      return;
    }

    setBusy(
      true,
    );

    setError(
      null,
    );

    try {
      if (
        kind ===
        "plan"
      ) {
        await submitPlanChangeRequest(
          store.tenantId,
          {
            planCode,
            billingCycle,
            reason:
              cleanReason,
          },
        );
      } else if (
        kind ===
        "identity"
      ) {
        await submitStoreIdentityChangeRequest(
          store.tenantId,
          {
            name:
              name.trim() ||
              null,

            slug:
              slug.trim() ||
              null,

            verticalCode:
              verticalCode ||
              null,

            reason:
              cleanReason,
          },
        );
      } else {
        await submitOwnerEmailChangeRequest(
          store.tenantId,
          {
            email:
              email.trim(),

            reason:
              cleanReason,
          },
        );
      }

      setKind(
        null,
      );

      setReason("");
      setEmail("");

      await load();
    } catch (
      exception
    ) {
      setError(
        exception instanceof
          Error
          ? exception.message
          : "تعذر إرسال الطلب.",
      );
    } finally {
      setBusy(
        false,
      );
    }
  }

  if (!store) {
    return (
      <main
        dir="rtl"
        className="rukn-settings-v3"
      >
        <div className="rukn-settings-v3-empty">
          لم يتم العثور على متجر حالي.
        </div>
      </main>
    );
  }

  return (
    <main
      dir="rtl"
      className="rukn-settings-v3"
    >
      <header className="rukn-settings-v3-head">
        <div>
          <span className="eyebrow">
            RUKN SETTINGS
          </span>

          <h1>
            الإعدادات
          </h1>

          <p>
            مكان هادئ لإدارة حسابك، متجرك، الأمان، والتغييرات التي تحتاج مراجعة.
          </p>
        </div>

        {pendingCount >
        0 ? (
          <button
            type="button"
            className="rukn-settings-v3-pending"
            onClick={() =>
              setActiveSection(
                "requests",
              )
            }
          >
            <span>
              قيد المراجعة
            </span>

            <strong>
              {pendingCount.toLocaleString(
                "en-US",
              )}
            </strong>
          </button>
        ) : (
          <div className="rukn-settings-v3-clear">
            <i />

            لا توجد طلبات معلّقة
          </div>
        )}
      </header>

      {error &&
      !kind ? (
        <div
          role="alert"
          className="rukn-settings-v3-alert"
        >
          {error}
        </div>
      ) : null}

      <div className="rukn-settings-v3-layout">
        <nav className="rukn-settings-v3-nav">
          <SettingsNavItem
            icon={
              UserRound
            }
            title="حسابي"
            description="الباقة والبريد والنشاط"
            active={
              activeSection ===
              "account"
            }
            onClick={() =>
              setActiveSection(
                "account",
              )
            }
          />

          <SettingsNavItem
            icon={
              Store
            }
            title="المتجر"
            description="الهوية والتواصل والعنوان"
            active={
              activeSection ===
              "store"
            }
            onClick={() =>
              setActiveSection(
                "store",
              )
            }
          />

          <SettingsNavItem
            icon={
              KeyRound
            }
            title="الأمان"
            description="التحقق وحماية الجلسة"
            active={
              activeSection ===
              "security"
            }
            onClick={() =>
              setActiveSection(
                "security",
              )
            }
          />

          <SettingsNavItem
            icon={
              History
            }
            title="طلبات التغييرات"
            description="المراجعات والقرارات"
            active={
              activeSection ===
              "requests"
            }
            badge={
              pendingCount
            }
            onClick={() =>
              setActiveSection(
                "requests",
              )
            }
          />
        </nav>

        <section className="rukn-settings-v3-content">
          {activeSection ===
          "account" ? (
            <AccountSection
              onPlan={() =>
                openRequest(
                  "plan",
                )
              }
              onIdentity={() =>
                openRequest(
                  "identity",
                )
              }
              onEmail={() =>
                openRequest(
                  "email",
                )
              }
              verticalLabel={
                verticalLabel
              }
            />
          ) : null}

          {activeSection ===
          "store" ? (
            <StoreSection
              name={
                store.name
              }
              slug={
                store.slug
              }
              status={
                storeStatusLabel(
                  store.status,
                )
              }
              verticalLabel={
                verticalLabel
              }
              onIdentity={() =>
                openRequest(
                  "identity",
                )
              }
              onContact={() =>
                setContactOpen(
                  true,
                )
              }
            />
          ) : null}

          {activeSection ===
          "security" ? (
            <SecuritySection
              mfaPolicy={
                mfaPolicy
              }
              loading={
                securityLoading
              }
              busy={
                securityBusy
              }
              message={
                securityMessage
              }
              error={
                securityError
              }
              onChange={
                updateMfaPolicy
              }
            />
          ) : null}

          {activeSection ===
          "requests" ? (
            <RequestsSection
              requests={
                requests
              }
              loading={
                loading
              }
              onRefresh={() =>
                void load()
              }
            />
          ) : null}
        </section>
      </div>

      {kind ? (
        <RequestDrawer
          kind={
            kind
          }
          busy={
            busy
          }
          error={
            error
          }
          planCode={
            planCode
          }
          billingCycle={
            billingCycle
          }
          name={
            name
          }
          slug={
            slug
          }
          verticalCode={
            verticalCode
          }
          email={
            email
          }
          reason={
            reason
          }
          onPlanCode={
            setPlanCode
          }
          onBillingCycle={
            setBillingCycle
          }
          onName={
            setName
          }
          onSlug={
            setSlug
          }
          onVerticalCode={
            setVerticalCode
          }
          onEmail={
            setEmail
          }
          onReason={
            setReason
          }
          onClose={() => {
            if (
              !busy
            ) {
              setKind(
                null,
              );

              setError(
                null,
              );
            }
          }}
          onSubmit={() =>
            void submit()
          }
        />
      ) : null}

      {contactOpen ? (
        <div className="rukn-settings-v3-layer">
          <button
            type="button"
            className="rukn-settings-v3-backdrop"
            aria-label="إغلاق"
            onClick={() =>
              setContactOpen(
                false,
              )
            }
          />

          <aside className="rukn-settings-v3-store-drawer">
            <header>
              <div>
                <span>
                  إعداد المتجر
                </span>

                <h2>
                  التواصل والعنوان
                </h2>

                <p>
                  هذه التفاصيل لا تظهر في صفحة الإعدادات الرئيسية إلا عند الحاجة لتعديلها.
                </p>
              </div>

              <button
                type="button"
                aria-label="إغلاق"
                onClick={() =>
                  setContactOpen(
                    false,
                  )
                }
              >
                <X
                  size={18}
                />
              </button>
            </header>

            <div className="rukn-settings-v3-store-host">
              <StoreContactSettings
                tenantId={
                  store.tenantId
                }
              />
            </div>
          </aside>
        </div>
      ) : null}
    </main>
  );
}

function AccountSection({
  onPlan,
  onIdentity,
  onEmail,
  verticalLabel,
}: {
  onPlan:
    () => void;
  onIdentity:
    () => void;
  onEmail:
    () => void;
  verticalLabel:
    string;
}) {
  return (
    <>
      <SectionIntro
        eyebrow="ACCOUNT"
        title="حسابي"
        description="الأشياء المرتبطة بالحساب التجاري نفسه، بدون تشتيتها بين عدة شاشات."
      />

      <div className="rukn-settings-v3-list">
        <SettingAction
          icon={
            SlidersHorizontal
          }
          title="الباقة والفوترة"
          description="طلب تغيير الباقة أو دورة الفوترة."
          value="إدارة الباقة"
          onClick={
            onPlan
          }
        />

        <SettingAction
          icon={
            Mail
          }
          title="بريد المالك"
          description="تغيير البريد الأساسي المرتبط بمالك المتجر."
          value="تغيير البريد"
          onClick={
            onEmail
          }
        />

        <SettingAction
          icon={
            Building2
          }
          title="النشاط والتخصص"
          description="تعديل النشاط الأساسي أو بيانات هوية المتجر."
          value={
            verticalLabel
          }
          onClick={
            onIdentity
          }
        />
      </div>
    </>
  );
}

function StoreSection({
  name,
  slug,
  status,
  verticalLabel,
  onIdentity,
  onContact,
}: {
  name:
    string;
  slug:
    string;
  status:
    string;
  verticalLabel:
    string;
  onIdentity:
    () => void;
  onContact:
    () => void;
}) {
  return (
    <>
      <SectionIntro
        eyebrow="STORE"
        title="إعداد المتجر"
        description="العميل يرى متجرًا واحدًا؛ لذلك نجمع الهوية والتواصل والعنوان في مكان مرتب وواضح."
      />

      <div className="rukn-settings-v3-store-summary">
        <div className="identity">
          <span className="avatar">
            <Store
              size={20}
            />
          </span>

          <div>
            <strong>
              {name}
            </strong>

            <small
              dir="ltr"
            >
              {slug
                ? `${slug}.ofoq.store`
                : "Store URL not configured"}
            </small>
          </div>
        </div>

        <dl>
          <div>
            <dt>
              الحالة
            </dt>

            <dd>
              {status}
            </dd>
          </div>

          <div>
            <dt>
              النشاط
            </dt>

            <dd>
              {verticalLabel}
            </dd>
          </div>
        </dl>
      </div>

      <div className="rukn-settings-v3-list">
        <SettingAction
          icon={
            Building2
          }
          title="هوية المتجر"
          description="الاسم، الرابط، والنشاط الأساسي."
          value="تعديل الهوية"
          onClick={
            onIdentity
          }
        />

        <SettingAction
          icon={
            Store
          }
          title="التواصل والعنوان"
          description="أرقام التواصل، WhatsApp، الموقع، والعنوان."
          value="إدارة البيانات"
          onClick={
            onContact
          }
        />

        <Link
          to="/admin/store"
          className="rukn-settings-v3-setting-action"
        >
          <span className="icon">
            <SlidersHorizontal
              size={16}
            />
          </span>

          <div className="copy">
            <strong>
              واجهة المتجر
            </strong>

            <small>
              الألوان، العرض، والمحتوى المرئي للمتجر.
            </small>
          </div>

          <span className="value">
            تخصيص الواجهة
          </span>

          <ChevronLeft
            size={16}
          />
        </Link>
      </div>
    </>
  );
}

function SecuritySection({
  mfaPolicy,
  loading,
  busy,
  message,
  error,
  onChange,
}: {
  mfaPolicy:
    MfaReopenPolicy;
  loading:
    boolean;
  busy:
    boolean;
  message:
    string |
    null;
  error:
    string |
    null;
  onChange:
    (
      policy:
        MfaReopenPolicy,
    ) => Promise<void>;
}) {
  return (
    <>
      <SectionIntro
        eyebrow="SECURITY"
        title="أمان الحساب"
        description="إعدادات قليلة ومباشرة، بدون تشتيت المستخدم بتفاصيل تقنية غير ضرورية."
      />

      <div className="rukn-settings-v3-security">
        <div className="security-row">
          <span className="icon">
            <ShieldCheck
              size={17}
            />
          </span>

          <div>
            <strong>
              إعادة طلب رمز التحقق
            </strong>

            <small>
              اختر متى يطلب ركن رمز MFA عند إعادة فتح لوحة الإدارة.
            </small>
          </div>

          <select
            value={
              mfaPolicy
            }
            disabled={
              loading ||
              busy
            }
            onChange={(
              event,
            ) =>
              void onChange(
                event.target
                  .value as
                  MfaReopenPolicy,
              )
            }
          >
            <option value="EveryBrowserSession">
              كل جلسة متصفح جديدة
            </option>

            <option value="Minutes15">
              بعد 15 دقيقة
            </option>

            <option value="Minutes30">
              بعد 30 دقيقة
            </option>

            <option value="Minutes60">
              بعد 60 دقيقة
            </option>
          </select>
        </div>

        <div className="security-note">
          <KeyRound
            size={15}
          />

          <span>
            يوجد حد أمان ثابت قدره 6 ساعات من آخر تحقق MFA.
          </span>

          <Link
            to="/security/setup?returnTo=%2Fadmin%2Fsettings&reauth=1"
          >
            تأكيد الرمز الآن
            <ArrowLeft
              size={12}
            />
          </Link>
        </div>

        {message ? (
          <p className="success">
            {message}
          </p>
        ) : null}

        {error ? (
          <p className="error">
            {error}
          </p>
        ) : null}
      </div>
    </>
  );
}

function RequestsSection({
  requests,
  loading,
  onRefresh,
}: {
  requests:
    MerchantPlatformRequest[];
  loading:
    boolean;
  onRefresh:
    () => void;
}) {
  return (
    <>
      <div className="rukn-settings-v3-section-head">
        <SectionIntro
          eyebrow="REQUESTS"
          title="طلبات التغييرات"
          description="سجل مختصر للتغييرات الحساسة التي أرسلتها للمراجعة."
        />

        <button
          type="button"
          className="refresh"
          disabled={
            loading
          }
          onClick={
            onRefresh
          }
          aria-label="تحديث"
        >
          <RefreshCw
            size={15}
            className={
              loading
                ? "animate-spin"
                : ""
            }
          />
        </button>
      </div>

      {loading ? (
        <div className="rukn-settings-v3-loading">
          جاري تحميل الطلبات...
        </div>
      ) : requests.length ===
        0 ? (
        <div className="rukn-settings-v3-empty-inline">
          لم ترسل أي طلبات تغيير حتى الآن.
        </div>
      ) : (
        <div className="rukn-settings-v3-request-list">
          {requests.map(
            (
              request,
            ) => (
              <article
                key={
                  request.requestId
                }
              >
                <div className="main">
                  <strong>
                    {
                      typeLabels[
                        request.type
                      ] ??
                      request.type
                    }
                  </strong>

                  <small>
                    {
                      request.summary
                    }
                  </small>
                </div>

                <RequestStatus
                  value={
                    request.status
                  }
                />

                <time>
                  {new Intl.DateTimeFormat(
                    "ar-SA-u-nu-latn",
                    {
                      dateStyle:
                        "medium",
                    },
                  ).format(
                    new Date(
                      request.requestedAtUtc,
                    ),
                  )}
                </time>

                {request.reviewReason ? (
                  <p className="review-note">
                    ملاحظة الإدارة:
                    {" "}
                    {
                      request.reviewReason
                    }
                  </p>
                ) : null}
              </article>
            ),
          )}
        </div>
      )}
    </>
  );
}

function SettingsNavItem({
  icon: Icon,
  title,
  description,
  active,
  badge = 0,
  onClick,
}: {
  icon:
    LucideIcon;
  title:
    string;
  description:
    string;
  active:
    boolean;
  badge?:
    number;
  onClick:
    () => void;
}) {
  return (
    <button
      type="button"
      data-active={
        active
      }
      onClick={
        onClick
      }
    >
      <span className="icon">
        <Icon
          size={17}
        />
      </span>

      <span className="copy">
        <strong>
          {title}
        </strong>

        <small>
          {description}
        </small>
      </span>

      {badge >
      0 ? (
        <span className="badge">
          {badge.toLocaleString(
            "en-US",
          )}
        </span>
      ) : (
        <ChevronLeft
          size={15}
          className="arrow"
        />
      )}
    </button>
  );
}

function SectionIntro({
  eyebrow,
  title,
  description,
}: {
  eyebrow:
    string;
  title:
    string;
  description:
    string;
}) {
  return (
    <header className="rukn-settings-v3-intro">
      <span>
        {eyebrow}
      </span>

      <h2>
        {title}
      </h2>

      <p>
        {description}
      </p>
    </header>
  );
}

function SettingAction({
  icon: Icon,
  title,
  description,
  value,
  onClick,
}: {
  icon:
    LucideIcon;
  title:
    string;
  description:
    string;
  value:
    string;
  onClick:
    () => void;
}) {
  return (
    <button
      type="button"
      className="rukn-settings-v3-setting-action"
      onClick={
        onClick
      }
    >
      <span className="icon">
        <Icon
          size={16}
        />
      </span>

      <span className="copy">
        <strong>
          {title}
        </strong>

        <small>
          {description}
        </small>
      </span>

      <span className="value">
        {value}
      </span>

      <ChevronLeft
        size={16}
      />
    </button>
  );
}

function RequestDrawer({
  kind,
  busy,
  error,
  planCode,
  billingCycle,
  name,
  slug,
  verticalCode,
  email,
  reason,
  onPlanCode,
  onBillingCycle,
  onName,
  onSlug,
  onVerticalCode,
  onEmail,
  onReason,
  onClose,
  onSubmit,
}: {
  kind:
    Exclude<
      RequestKind,
      null
    >;
  busy:
    boolean;
  error:
    string |
    null;
  planCode:
    string;
  billingCycle:
    "Monthly" |
    "Annual";
  name:
    string;
  slug:
    string;
  verticalCode:
    string;
  email:
    string;
  reason:
    string;
  onPlanCode:
    (
      value:
        string,
    ) => void;
  onBillingCycle:
    (
      value:
        "Monthly" |
        "Annual",
    ) => void;
  onName:
    (
      value:
        string,
    ) => void;
  onSlug:
    (
      value:
        string,
    ) => void;
  onVerticalCode:
    (
      value:
        string,
    ) => void;
  onEmail:
    (
      value:
        string,
    ) => void;
  onReason:
    (
      value:
        string,
    ) => void;
  onClose:
    () => void;
  onSubmit:
    () => void;
}) {
  return (
    <div className="rukn-settings-v3-layer">
      <button
        type="button"
        className="rukn-settings-v3-backdrop"
        aria-label="إغلاق"
        onClick={
          onClose
        }
      />

      <aside className="rukn-settings-v3-request-drawer">
        <header>
          <div>
            <span>
              طلب مراجعة
            </span>

            <h2>
              {kind ===
              "plan"
                ? "تغيير الباقة"
                : kind ===
                    "identity"
                  ? "هوية ونشاط المتجر"
                  : "تغيير بريد المالك"}
            </h2>

            <p>
              لن يتم تطبيق التغيير الحساس قبل مراجعته واعتماده.
            </p>
          </div>

          <button
            type="button"
            disabled={
              busy
            }
            aria-label="إغلاق"
            onClick={
              onClose
            }
          >
            <X
              size={18}
            />
          </button>
        </header>

        <div className="body">
          {kind ===
          "plan" ? (
            <>
              <Field
                label="الباقة المطلوبة"
              >
                <select
                  value={
                    planCode
                  }
                  onChange={(
                    event,
                  ) =>
                    onPlanCode(
                      event.target.value,
                    )
                  }
                >
                  <option value="business">
                    Business
                  </option>

                  <option value="pro">
                    Pro
                  </option>

                  <option value="extra">
                    Extra
                  </option>
                </select>
              </Field>

              <Field
                label="دورة الفوترة"
              >
                <select
                  value={
                    billingCycle
                  }
                  onChange={(
                    event,
                  ) =>
                    onBillingCycle(
                      event.target
                        .value as
                        "Monthly" |
                        "Annual",
                    )
                  }
                >
                  <option value="Monthly">
                    شهري
                  </option>

                  <option value="Annual">
                    سنوي
                  </option>
                </select>
              </Field>
            </>
          ) : kind ===
            "identity" ? (
            <>
              <Field
                label="اسم المتجر"
              >
                <input
                  value={
                    name
                  }
                  onChange={(
                    event,
                  ) =>
                    onName(
                      event.target.value,
                    )
                  }
                />
              </Field>

              <Field
                label="رابط المتجر"
              >
                <input
                  dir="ltr"
                  value={
                    slug
                  }
                  onChange={(
                    event,
                  ) =>
                    onSlug(
                      event.target.value,
                    )
                  }
                />
              </Field>

              <Field
                label="النشاط الأساسي"
              >
                <select
                  value={
                    verticalCode
                  }
                  onChange={(
                    event,
                  ) =>
                    onVerticalCode(
                      event.target.value,
                    )
                  }
                >
                  <option value="">
                    بدون تغيير
                  </option>

                  {VERTICAL_OPTIONS.map(
                    (
                      option,
                    ) => (
                      <option
                        key={
                          option.code
                        }
                        value={
                          option.code
                        }
                      >
                        {
                          option.label
                        }
                      </option>
                    ),
                  )}
                </select>
              </Field>
            </>
          ) : (
            <Field
              label="البريد الجديد"
            >
              <input
                type="email"
                dir="ltr"
                value={
                  email
                }
                onChange={(
                  event,
                ) =>
                  onEmail(
                    event.target.value,
                  )
                }
                placeholder="owner@example.com"
              />
            </Field>
          )}

          <Field
            label="سبب الطلب"
          >
            <textarea
              value={
                reason
              }
              onChange={(
                event,
              ) =>
                onReason(
                  event.target.value,
                )
              }
              placeholder="اكتب السبب باختصار..."
            />
          </Field>

          {error ? (
            <div
              role="alert"
              className="drawer-error"
            >
              {error}
            </div>
          ) : null}
        </div>

        <footer>
          <button
            type="button"
            className="secondary"
            disabled={
              busy
            }
            onClick={
              onClose
            }
          >
            إلغاء
          </button>

          <button
            type="button"
            className="primary"
            disabled={
              busy
            }
            onClick={
              onSubmit
            }
          >
            <ShieldCheck
              size={15}
            />

            {busy
              ? "جاري الإرسال..."
              : "إرسال للمراجعة"}
          </button>
        </footer>
      </aside>
    </div>
  );
}

function RequestStatus({
  value,
}: {
  value:
    string;
}) {
  const Icon =
    value ===
    "Approved"
      ? CheckCircle2
      : value ===
          "Rejected"
        ? XCircle
        : Clock3;

  return (
    <span
      className="rukn-settings-v3-request-status"
      data-status={
        value
      }
    >
      <Icon
        size={13}
      />

      {
        statusLabels[
          value
        ] ??
        value
      }
    </span>
  );
}

function Field({
  label,
  children,
}: {
  label:
    string;
  children:
    ReactNode;
}) {
  return (
    <label className="rukn-settings-v3-field">
      <span>
        {label}
      </span>

      {children}
    </label>
  );
}