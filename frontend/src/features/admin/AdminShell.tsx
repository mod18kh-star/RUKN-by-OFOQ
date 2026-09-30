import {
  Boxes,
  ClipboardList,
  CreditCard,
  FileText,
  FolderTree,
  LayoutDashboard,
  LogOut,
  Menu,
  Moon,
  Settings2,
  Star,
  Store,
  Sun,
  TicketPercent,
  Truck,
  Warehouse,
  X,
} from "lucide-react";

import {
  useEffect,
  useRef,
  useState,
} from "react";

import {
  NavLink,
  Outlet,
  useNavigate,
} from "react-router";

import {
  logoutSession,
} from "../auth/authSession";

import {
  readAdminStore,
  STORE_UPDATED_EVENT,
} from "./store-setup/storeSetupStorage";

import type {
  AdminStore,
} from "./store-setup/storeSetup.types";

import "./AdminHumanV4.css";
import "./AdminHumanV6.css";
import "./AdminHumanV61.css";
import "./AdminHoverSidebarV7.css";

const navigation = [
  {
    label: "الرئيسية",
    to: "/admin",
    icon: LayoutDashboard,
    end: true,
  },
  {
    label: "المنتجات",
    to: "/admin/products",
    icon: Boxes,
  },
  {
    label: "المخزون",
    to: "/admin/inventory",
    icon: Warehouse,
  },
  {
    label: "الأقسام",
    to: "/admin/categories",
    icon: FolderTree,
  },
  {
    label: "الطلبات",
    to: "/admin/orders",
    icon: ClipboardList,
  },
  {
    label: "تقييمات العملاء",
    to: "/admin/reviews",
    icon: Star,
  },
  {
    label: "واجهة المتجر",
    to: "/admin/store",
    icon: Store,
  },
  {
    label: "الصفحات",
    to: "/admin/pages",
    icon: FileText,
  },
  {
    label: "الكوبونات والخصومات",
    to: "/admin/coupons",
    icon: TicketPercent,
  },
  {
    label: "الدفع",
    to: "/admin/payments",
    icon: CreditCard,
  },
  {
    label: "التوصيل",
    to: "/admin/shipping",
    icon: Truck,
  },
  {
    label: "الإعدادات",
    to: "/admin/settings",
    icon: Settings2,
  },
];

function statusLabel(
  status: string,
) {
  if (status === "active") {
    return "فعال";
  }

  if (status === "suspended") {
    return "موقوف";
  }

  return "قيد الإعداد";
}

export function AdminShell() {
  const navigate =
    useNavigate();

  const [
    store,
    setStore,
  ] =
    useState<AdminStore | null>(
      () => readAdminStore(),
    );

  const [
    mobileOpen,
    setMobileOpen,
  ] =
    useState(false);

  const [
    accountOpen,
    setAccountOpen,
  ] =
    useState(false);

  const [
    theme,
    setTheme,
  ] =
    useState<"light" | "dark">(
      () => {
        try {
          const stored =
            window.localStorage.getItem(
              "rukn_admin_theme",
            );

          if (
            stored === "light" ||
            stored === "dark"
          ) {
            return stored;
          }

          return window.matchMedia(
            "(prefers-color-scheme: dark)",
          ).matches
            ? "dark"
            : "light";
        } catch {
          return "light";
        }
      },
    );

  useEffect(
    () => {
      document.documentElement.dataset.adminTheme =
        theme;

      try {
        window.localStorage.setItem(
          "rukn_admin_theme",
          theme,
        );
      } catch {
        // Storage can be unavailable.
      }

      return () => {
        delete document.documentElement.dataset.adminTheme;
      };
    },
    [
      theme,
    ],
  );

  useEffect(
    () => {
      function refreshStore() {
        setStore(
          readAdminStore(),
        );
      }

      window.addEventListener(
        "storage",
        refreshStore,
      );

      window.addEventListener(
        STORE_UPDATED_EVENT,
        refreshStore,
      );

      return () => {
        window.removeEventListener(
          "storage",
          refreshStore,
        );

        window.removeEventListener(
          STORE_UPDATED_EVENT,
          refreshStore,
        );
      };
    },
    [],
  );

  const storeName =
    store?.name ??
    "متجر ركن";

  const storeDomain =
    store?.slug
      ? `${store.slug}.ofoq.store`
      : "رابط المتجر غير مكتمل";

  const status =
    (
      store?.status ??
      "Draft"
    ).toLowerCase();

  const initials =
    store?.name
      ? store.name
          .split(/\s+/)
          .filter(Boolean)
          .slice(0, 2)
          .map(
            (part) =>
              part[0],
          )
          .join("")
      : "ر";

  async function signOut() {
    setAccountOpen(false);

    try {
      await logoutSession();
    } finally {
      navigate(
        "/start/login?mode=login",
        {
          replace: true,
        },
      );
    }
  }

  return (
    <div className="rukn-admin-shell rukn-h6-shell min-h-screen">
      <Sidebar
        className="rukn-h6-sidebar-desktop"
      />

      {mobileOpen ? (
        <div className="rukn-h6-mobile-layer">
          <button
            type="button"
            className="rukn-h6-mobile-backdrop"
            aria-label="إغلاق القائمة"
            onClick={() =>
              setMobileOpen(false)
            }
          />

          <div className="rukn-h6-mobile-drawer">
            <Sidebar
              className="h-full"
              onNavigate={() =>
                setMobileOpen(false)
              }
              onClose={() =>
                setMobileOpen(false)
              }
            />
          </div>
        </div>
      ) : null}

      <div className="rukn-h6-content">
        <header className="rukn-h6-topbar">
          <div className="rukn-h6-topbar-context">
            <button
              type="button"
              className="rukn-h6-mobile-button"
              aria-label="فتح القائمة"
              onClick={() =>
                setMobileOpen(true)
              }
            >
              <Menu
                size={18}
              />
            </button>

            <span className="rukn-h6-store-icon">
              <Store
                size={17}
              />
            </span>

            <div className="rukn-h6-store-meta">
              <div>
                <strong>
                  {storeName}
                </strong>

                <span
                  data-status={
                    status
                  }
                >
                  {statusLabel(
                    status,
                  )}
                </span>
              </div>

              <small
                dir="ltr"
              >
                {storeDomain}
              </small>
            </div>
          </div>

          <div className="rukn-h6-topbar-actions">
            <button
              type="button"
              className="rukn-h6-round-action"
              aria-label={
                theme === "dark"
                  ? "تفعيل الوضع الفاتح"
                  : "تفعيل الوضع الليلي"
              }
              onClick={() =>
                setTheme(
                  (current) =>
                    current === "dark"
                      ? "light"
                      : "dark",
                )
              }
            >
              {theme === "dark" ? (
                <Sun
                  size={17}
                />
              ) : (
                <Moon
                  size={17}
                />
              )}
            </button>

            <div className="rukn-h6-account">
              <button
                type="button"
                className="rukn-h6-avatar"
                aria-label="قائمة الحساب"
                aria-expanded={
                  accountOpen
                }
                onClick={() =>
                  setAccountOpen(
                    (current) =>
                      !current,
                  )
                }
              >
                {initials}
              </button>

              {accountOpen ? (
                <div className="rukn-h6-account-menu">
                  <div>
                    <strong>
                      {storeName}
                    </strong>

                    <span>
                      إدارة المتجر
                    </span>
                  </div>

                  <button
                    type="button"
                    onClick={() => {
                      setAccountOpen(
                        false,
                      );

                      navigate(
                        "/admin/settings",
                      );
                    }}
                  >
                    <Settings2
                      size={15}
                    />

                    الإعدادات
                  </button>

                  <button
                    type="button"
                    className="danger"
                    onClick={() =>
                      void signOut()
                    }
                  >
                    <LogOut
                      size={15}
                    />

                    تسجيل الخروج
                  </button>
                </div>
              ) : null}
            </div>
          </div>
        </header>

        <main className="rukn-admin-main rukn-h6-main">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

function Sidebar({
  className,
  onNavigate,
  onClose,
}: {
  className?: string;
  onNavigate?: () => void;
  onClose?: () => void;
}) {
  const sidebarRef =
    useRef<HTMLElement | null>(
      null,
    );

  const [
    navigationLock,
    setNavigationLock,
  ] =
    useState(false);

  useEffect(
    () => {
      if (
        !navigationLock
      ) {
        return;
      }

      function releaseWhenPointerLeaves(
        event: PointerEvent,
      ) {
        const sidebar =
          sidebarRef.current;

        if (!sidebar) {
          setNavigationLock(
            false,
          );

          return;
        }

        const rect =
          sidebar.getBoundingClientRect();

        const outside =
          event.clientX <
            rect.left ||
          event.clientX >
            rect.right ||
          event.clientY <
            rect.top ||
          event.clientY >
            rect.bottom;

        if (outside) {
          setNavigationLock(
            false,
          );
        }
      }

      window.addEventListener(
        "pointermove",
        releaseWhenPointerLeaves,
        {
          passive: true,
        },
      );

      return () => {
        window.removeEventListener(
          "pointermove",
          releaseWhenPointerLeaves,
        );
      };
    },
    [
      navigationLock,
    ],
  );

  return (
    <aside
      ref={sidebarRef}
      style={
        navigationLock &&
        !onClose
          ? {
              pointerEvents:
                "none",
            }
          : undefined
      }
      className={[
        "rukn-h6-sidebar",
        className ??
          "",
      ].join(" ")}
    >
      <div className="rukn-h6-brand">
        <div>
          <strong>
            ركن
          </strong>

          <span>
            BY OFOQ
          </span>
        </div>

        {onClose ? (
          <button
            type="button"
            aria-label="إغلاق القائمة"
            onClick={
              onClose
            }
          >
            <X
              size={16}
            />
          </button>
        ) : null}
      </div>

      <div className="rukn-h6-navigation">
        <p>
          إدارة المتجر
        </p>

        <nav>
          {navigation.map(
            (
              item,
            ) => {
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
                  onClick={(
                    event,
                  ) => {
                    /*
                     * Clicking a desktop navigation item used to keep
                     * :hover / :focus-within active while the new page
                     * was already visible.
                     *
                     * Blur the link and temporarily remove the desktop
                     * sidebar from pointer hit-testing. It becomes
                     * interactive again after the pointer genuinely
                     * leaves its current area.
                     */
                    if (
                      event.detail >
                      0
                    ) {
                      event.currentTarget.blur();

                      if (
                        !onClose
                      ) {
                        setNavigationLock(
                          true,
                        );
                      }
                    }

                    onNavigate?.();
                  }}
                  className={({
                    isActive,
                  }) =>
                    isActive
                      ? "active"
                      : ""
                  }
                >
                  <span>
                    <Icon
                      size={17}
                      strokeWidth={
                        1.65
                      }
                    />
                  </span>

                  {item.label}
                </NavLink>
              );
            },
          )}
        </nav>
      </div>

      <footer className="rukn-h6-sidebar-footer">
        <strong>
          RUKN
        </strong>

        <span>
          مساحة تشغيل متجرك
        </span>
      </footer>
    </aside>
  );
}