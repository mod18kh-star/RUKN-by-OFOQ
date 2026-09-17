import {
  useEffect,
  useState,
} from "react";

import {
  Activity,
  BadgeCheck,
  Building2,
  ChevronLeft,
  ClipboardCheck,
  LayoutDashboard,
  LogOut,
  LockKeyhole,
  Settings2,
  ShieldCheck,
  Store,
  Users,
} from "lucide-react";

import {
  Link,
  NavLink,
  Outlet,
  useNavigate,
} from "react-router";

import {
  bootstrapFirstPlatformAdmin,
  getPlatformMe,
  getPlatformRequestStats,
  PlatformApiError,
  type PlatformMe,
} from "./platformApi";

import {
  logoutSession,
} from "../auth/authSession";

const navItems = [
  {
    to: "/platform",
    label: "نظرة عامة",
    icon: LayoutDashboard,
    end: true,
  },
  {
    to: "/platform/stores",
    label: "المتاجر",
    icon: Store,
    end: false,
  },
  {
    to: "/platform/requests",
    label: "الطلبات",
    icon: ClipboardCheck,
    end: false,
  },
  {
    to: "/platform/access",
    label: "فريق ركن",
    icon: Users,
    end: false,
  },
];

const futureItems = [
  {
    label:
      "التحقق KYC / KYB",
    icon:
      BadgeCheck,
  },
  {
    label:
      "الخصائص والباقات",
    icon:
      Settings2,
  },
  {
    label:
      "سجل الإدارة",
    icon:
      Activity,
  },
];

export function PlatformShell() {
  const navigate =
    useNavigate();

  const [me, setMe] =
    useState<PlatformMe | null>(
      null,
    );

  const [loading, setLoading] =
    useState(true);

  const [denied, setDenied] =
    useState(false);

  const [unauthorized, setUnauthorized] =
    useState(false);

  const [bootstrapBusy, setBootstrapBusy] =
    useState(false);

  const [message, setMessage] =
    useState<string | null>(
      null,
    );

  const [pendingRequests, setPendingRequests] =
    useState(0);

  function loadAccess() {
    setLoading(true);
    setDenied(false);
    setUnauthorized(false);
    setMessage(null);

    void getPlatformMe()
      .then(
        (result) => {
          setMe(
            result,
          );

          void getPlatformRequestStats()
            .then((stats) => {
              setPendingRequests(stats.pending);
            })
            .catch(() => {
              setPendingRequests(0);
            });
        },
      )
      .catch(
        (error: unknown) => {
          if (
            error instanceof
              PlatformApiError &&
            error.status === 401
          ) {
            setUnauthorized(
              true,
            );

            return;
          }

          if (
            error instanceof
              PlatformApiError &&
            error.status === 403
          ) {
            setDenied(
              true,
            );

            return;
          }

          setMessage(
            error instanceof Error
              ? error.message
              : "تعذر التحقق من صلاحية الحساب.",
          );
        },
      )
      .finally(
        () => {
          setLoading(
            false,
          );
        },
      );
  }

  useEffect(
    () => {
      const timer =
        window.setTimeout(
          loadAccess,
          0,
        );

      return () => {
        window.clearTimeout(
          timer,
        );
      };
    },
    [],
  );

  async function signOut() {
    try {
      await logoutSession();
    } finally {
      navigate(
        "/start/login?mode=login&returnTo=%2Fplatform",
        {
          replace: true,
        },
      );
    }
  }

  async function bootstrap() {
    setBootstrapBusy(
      true,
    );

    setMessage(null);

    try {
      await bootstrapFirstPlatformAdmin();

      loadAccess();
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "تعذر تفعيل حساب Super Admin.",
      );
    } finally {
      setBootstrapBusy(
        false,
      );
    }
  }

  if (loading) {
    return (
      <div
        dir="rtl"
        className="flex min-h-screen items-center justify-center bg-[#070a13] text-white"
      >
        <div className="text-center">
          <div className="mx-auto size-9 animate-pulse rounded-full bg-[#d0aa70]" />

          <p className="mt-5 text-[13px] font-semibold">
            جاري فتح مركز إدارة ركن
          </p>
        </div>
      </div>
    );
  }

  if (unauthorized) {
    return (
      <AccessScreen
        title="سجل دخولك أولًا"
        description="مركز إدارة ركن يحتاج جلسة دخول صالحة."
      >
        <Link
          to="/start/login?returnTo=%2Fplatform"
          style={{
            color:
              "#080b14",
          }}
          className="inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#d0aa70] px-5 text-[11px] font-semibold"
        >
          تسجيل الدخول
          <ChevronLeft
            size={14}
          />
        </Link>
      </AccessScreen>
    );
  }

  if (denied) {
    return (
      <AccessScreen
        title="هذا الحساب ليس Super Admin بعد"
        description="في بيئة التطوير فقط يمكنك جعل الحساب الحالي أول مدير لمنصة ركن. بعد إنشاء أول مدير يتوقف هذا المسار تلقائيًا عن منح أي حساب آخر الصلاحية."
      >
        <button
          type="button"
          onClick={
            bootstrap
          }
          disabled={
            bootstrapBusy
          }
          style={{
            color:
              "#080b14",
          }}
          className="inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#d0aa70] px-5 text-[11px] font-semibold disabled:opacity-50"
        >
          <ShieldCheck
            size={15}
          />

          {bootstrapBusy
            ? "جاري التفعيل..."
            : "تفعيل هذا الحساب كأول Super Admin"}
        </button>

        {message ? (
          <p className="mt-4 max-w-[480px] text-[10px] leading-6 text-red-300">
            {message}
          </p>
        ) : null}
      </AccessScreen>
    );
  }

  if (!me) {
    return (
      <AccessScreen
        title="تعذر فتح مركز الإدارة"
        description={
          message ??
          "حاول تحديث الصفحة."
        }
      >
        <button
          type="button"
          onClick={
            loadAccess
          }
          className="h-11 rounded-[9px] border border-white/15 px-5 text-[11px] font-semibold text-white"
        >
          إعادة المحاولة
        </button>
      </AccessScreen>
    );
  }

  return (
    <div
      dir="rtl"
      className="min-h-screen bg-[#f3f1eb] text-[#080b14]"
      style={{
        fontFamily:
          '"Readex Pro", "IBM Plex Sans Arabic", "Segoe UI", Arial, sans-serif',
      }}
    >
      <aside className="fixed inset-y-0 right-0 z-40 hidden w-[250px] flex-col bg-[#070a13] text-white lg:flex">
        <div className="border-b border-white/[0.08] px-6 py-6">
          <div className="flex items-center gap-3">
            <div className="flex size-10 items-center justify-center rounded-[11px] bg-[#d0aa70] font-bold text-[#070a13]">
              ر
            </div>

            <div>
              <p className="text-[16px] font-semibold">
                ركن
              </p>

              <p className="mt-1 text-[9px] font-medium tracking-[0.08em] text-white/36">
                PLATFORM CONTROL
              </p>
            </div>
          </div>
        </div>

        <div className="px-4 py-5">
          <p className="px-3 text-[9px] font-semibold text-[#d0aa70]">
            إدارة المنصة
          </p>

          <nav className="mt-3 space-y-1">
            {navItems.map(
              (item) => {
                const Icon =
                  item.icon;

                return (
                  <NavLink
                    key={
                      item.to
                    }
                    to={
                      item.to
                    }
                    end={
                      item.end
                    }
                    className={({
                      isActive,
                    }) =>
                      [
                        "flex h-11 items-center gap-3 rounded-[9px] px-3 text-[11px] font-medium transition",
                        isActive
                          ? "bg-white/[0.09] text-white"
                          : "text-white/50 hover:bg-white/[0.05] hover:text-white",
                      ].join(
                        " ",
                      )
                    }
                  >
                    <Icon
                      size={16}
                    />

                    <span>{item.label}</span>

                    {item.to === "/platform/requests" && pendingRequests > 0 ? (
                      <span className="mr-auto inline-flex min-w-5 items-center justify-center rounded-full bg-[#d0aa70] px-1.5 py-0.5 text-[8px] font-bold text-[#070a13]">
                        {pendingRequests > 99 ? "99+" : pendingRequests}
                      </span>
                    ) : null}
                  </NavLink>
                );
              },
            )}
          </nav>

          <div className="my-5 h-px bg-white/[0.08]" />

          <p className="px-3 text-[9px] font-semibold text-white/28">
            إدارة متقدمة
          </p>

          <div className="mt-3 space-y-1">
            {futureItems.map(
              (item) => {
                const Icon =
                  item.icon;

                return (
                  <div
                    key={
                      item.label
                    }
                    className="flex h-10 items-center gap-3 px-3 text-[10px] text-white/27"
                  >
                    <Icon
                      size={15}
                    />

                    <span>
                      {
                        item.label
                      }
                    </span>

                    <LockKeyhole
                      size={11}
                      className="mr-auto"
                    />
                  </div>
                );
              },
            )}
          </div>
        </div>

        <div className="mt-auto border-t border-white/[0.08] p-5">
          <div className="rounded-[11px] bg-white/[0.05] p-4">
            <p className="text-[10px] font-semibold text-[#d0aa70]">
              Super Admin
            </p>

            <p
              dir="ltr"
              className="mt-2 truncate text-left text-[9px] text-white/42"
            >
              {me.userId}
            </p>

            <p className="mt-2 text-[9px] text-white/30">
              {me.environment}
            </p>
          </div>
        </div>
      </aside>

      <div className="lg:pr-[250px]">
        <header className="sticky top-0 z-30 flex h-[68px] items-center justify-between border-b border-black/[0.07] bg-[#f3f1eb]/94 px-5 backdrop-blur-xl md:px-8">
          <div>
            <p className="text-[9px] font-semibold text-[#9c7441]">
              RUKN PLATFORM
            </p>

            <p className="mt-1 text-[12px] font-semibold">
              مركز إدارة ركن
            </p>
          </div>

          <div className="flex items-center gap-3">
            <Link
              to="/admin"
              className="hidden text-[10px] font-semibold text-black/45 hover:text-black sm:block"
            >
              لوحة التاجر
            </Link>

            <button
              type="button"
              onClick={() => void signOut()}
              className="inline-flex h-9 items-center gap-2 rounded-[9px] border border-black/[0.08] bg-white px-3 text-[10px] font-semibold text-black/48 transition hover:text-black"
              title="تسجيل الخروج"
            >
              <LogOut size={13} />

              <span className="hidden sm:inline">
                خروج
              </span>
            </button>

            <div className="flex items-center gap-2 rounded-full border border-black/[0.08] bg-white px-3 py-2">
              <ShieldCheck
                size={14}
                className="text-[#956b35]"
              />

              <span className="text-[10px] font-semibold">
                Super Admin
              </span>
            </div>
          </div>
        </header>

        <main className="p-5 md:p-8 lg:p-9">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

function AccessScreen({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: React.ReactNode;
}) {
  return (
    <div
      dir="rtl"
      className="flex min-h-screen items-center justify-center bg-[#070a13] px-5 text-white"
    >
      <div className="w-full max-w-[620px] rounded-[22px] border border-white/10 bg-[#0c111d] p-8 shadow-2xl md:p-10">
        <div className="flex size-12 items-center justify-center rounded-[13px] bg-[#d0aa70] text-[#080b14]">
          <Building2
            size={21}
          />
        </div>

        <p className="mt-7 text-[10px] font-semibold text-[#d0aa70]">
          RUKN PLATFORM CONTROL
        </p>

        <h1 className="mt-3 text-[27px] font-semibold tracking-[-0.035em]">
          {title}
        </h1>

        <p className="mt-4 max-w-[520px] text-[12px] leading-7 text-white/48">
          {description}
        </p>

        <div className="mt-7">
          {children}
        </div>
      </div>
    </div>
  );
}