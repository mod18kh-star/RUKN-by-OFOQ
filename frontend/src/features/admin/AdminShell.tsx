import {
  Boxes,
  CreditCard,
  TicketPercent,
  Truck,
  Warehouse,
  ClipboardList,
  FileText,
  FolderTree,
  LayoutDashboard,
  LogOut,
  Menu,
  Settings2,
  Store,
  X,
} from "lucide-react";

import {
  useEffect,
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

export function AdminShell() {
  const navigate = useNavigate();

  const [store, setStore] = useState<AdminStore | null>(() => readAdminStore());
  const [mobileOpen, setMobileOpen] = useState(false);
  const [accountOpen, setAccountOpen] = useState(false);

  useEffect(() => {
    function refreshStore() {
      setStore(readAdminStore());
    }

    window.addEventListener("storage", refreshStore);
    window.addEventListener(STORE_UPDATED_EVENT, refreshStore);

    return () => {
      window.removeEventListener("storage", refreshStore);
      window.removeEventListener(STORE_UPDATED_EVENT, refreshStore);
    };
  }, []);

  const storeName = store?.name ?? "متجر ركن";
  const storeDomain = store?.slug
    ? `${store.slug}.ofoq.store`
    : "لم يتم إعداد رابط المتجر بعد";

  const initials = store?.name
    ? store.name
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map((part) => part[0])
        .join("")
    : "ر";

  const status = (store?.status ?? "Draft").toLowerCase();

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
    <div className="min-h-screen bg-[#f4f4f0] text-[#111513]">
      <Sidebar className="fixed inset-y-0 right-0 z-40 hidden w-[248px] lg:flex" />

      {mobileOpen ? (
        <div className="fixed inset-0 z-50 lg:hidden">
          <button
            type="button"
            aria-label="إغلاق القائمة"
            onClick={() => setMobileOpen(false)}
            className="absolute inset-0 bg-black/25 backdrop-blur-[1px]"
          />
          <div className="absolute inset-y-0 right-0 w-[276px] max-w-[86vw] shadow-2xl">
            <Sidebar
              className="h-full w-full"
              onNavigate={() => setMobileOpen(false)}
              mobileClose={() => setMobileOpen(false)}
            />
          </div>
        </div>
      ) : null}

      <div className="lg:mr-[248px]">
        <header className="sticky top-0 z-30 flex h-[70px] items-center justify-between border-b border-black/[0.065] bg-[#f8f8f5]/95 px-4 backdrop-blur-xl md:px-7">
          <div className="flex min-w-0 items-center gap-3">
            <button
              type="button"
              aria-label="فتح القائمة"
              onClick={() => setMobileOpen(true)}
              className="flex size-9 shrink-0 items-center justify-center rounded-[9px] border border-black/[0.07] bg-white lg:hidden"
            >
              <Menu size={17} />
            </button>

            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <p className="truncate text-[11px] font-semibold">{storeName}</p>
                <span
                  className={`hidden rounded-full px-2 py-0.5 text-[7px] font-semibold sm:inline-flex ${
                    status === "active"
                      ? "bg-emerald-50 text-emerald-700"
                      : status === "suspended"
                        ? "bg-red-50 text-red-700"
                        : "bg-amber-50 text-amber-700"
                  }`}
                >
                  {status === "active"
                    ? "فعال"
                    : status === "suspended"
                      ? "موقوف"
                      : "قيد الإعداد"}
                </span>
              </div>
              <p
                dir="ltr"
                className="mt-0.5 truncate text-left text-[8px] text-black/32"
              >
                {storeDomain}
              </p>
            </div>
          </div>

          <div className="relative">
            <button
              type="button"
              aria-label="قائمة الحساب"
              aria-expanded={accountOpen}
              onClick={() => setAccountOpen((value) => !value)}
              className="flex size-9 items-center justify-center rounded-full border border-black/[0.06] bg-[#ddd8ca] text-[9px] font-bold"
            >
              {initials}
            </button>

            {accountOpen ? (
              <div className="absolute left-0 top-12 w-[210px] overflow-hidden rounded-[13px] border border-black/[0.08] bg-white p-1.5 shadow-[0_18px_45px_rgba(0,0,0,0.12)]">
                <div className="border-b border-black/[0.055] px-3 py-3">
                  <p className="truncate text-[9px] font-semibold">{storeName}</p>
                  <p className="mt-1 truncate text-[8px] text-black/34">
                    إدارة المتجر
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => {
                    setAccountOpen(false);
                    navigate("/admin/settings");
                  }}
                  className="mt-1 flex w-full items-center gap-2 rounded-[8px] px-3 py-2.5 text-right text-[9px] text-black/62 hover:bg-black/[0.035]"
                >
                  <Settings2 size={13} />
                  الإعدادات
                </button>
                <button
                  type="button"
                  onClick={() => void signOut()}
                  className="flex w-full items-center gap-2 rounded-[8px] px-3 py-2.5 text-right text-[9px] text-red-700 hover:bg-red-50"
                >
                  <LogOut size={13} />
                  تسجيل الخروج
                </button>
              </div>
            ) : null}
          </div>
        </header>

        <main className="px-4 py-6 md:px-7 md:py-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

function Sidebar({
  className,
  onNavigate,
  mobileClose,
}: {
  className?: string;
  onNavigate?: () => void;
  mobileClose?: () => void;
}) {
  return (
    <aside
      className={`flex flex-col border-l border-black/[0.065] bg-[#fbfbf9] ${className ?? ""}`}
    >
      <div className="flex h-[70px] items-center justify-between border-b border-black/[0.065] px-6">
        <div>
          <div className="text-[20px] font-bold tracking-[-0.055em]">ركن</div>
          <div className="mt-0.5 text-[7px] font-semibold tracking-[0.16em] text-black/30">
            BY OFOQ
          </div>
        </div>

        {mobileClose ? (
          <button
            type="button"
            aria-label="إغلاق القائمة"
            onClick={mobileClose}
            className="flex size-8 items-center justify-center rounded-[8px] border border-black/[0.07] bg-white"
          >
            <X size={14} />
          </button>
        ) : null}
      </div>

      <div className="flex-1 px-3 py-5">
        <p className="mb-3 px-3 text-[8px] font-semibold tracking-[0.04em] text-black/30">
          إدارة المتجر
        </p>

        <nav className="space-y-1">
          {navigation.map((item) => {
            const Icon = item.icon;

            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                onClick={onNavigate}
                className={({ isActive }) =>
                  [
                    "flex h-10 items-center gap-3 rounded-[9px] px-3 text-[10px] font-medium transition",
                    isActive
                      ? "bg-[#e9e9e4] text-[#111513]"
                      : "text-black/52 hover:bg-black/[0.03] hover:text-black/75",
                  ].join(" ")
                }
              >
                <Icon size={16} strokeWidth={1.6} />
                {item.label}
              </NavLink>
            );
          })}
        </nav>
      </div>

      <div className="border-t border-black/[0.055] px-5 py-4">
        <p className="text-[8px] leading-4 text-black/28">
          ركن لإدارة التجارة الإلكترونية
        </p>
      </div>
    </aside>
  );
}
