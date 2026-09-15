import {
  Boxes,
  ClipboardList,
  LayoutDashboard,
  Menu,
  Settings2,
  Store,
} from "lucide-react";

import {
  NavLink,
  Outlet,
} from "react-router";

const navigation = [
  {
    label: "نظرة عامة",
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
    label: "الطلبات",
    to: "/admin/orders",
    icon: ClipboardList,
  },
  {
    label: "تصميم المتجر",
    to: "/admin/store",
    icon: Store,
  },
  {
    label: "الإعدادات",
    to: "/admin/settings",
    icon: Settings2,
  },
];

export function AdminShell() {
  return (
    <div className="min-h-screen bg-[#f3f3ef]">
      <aside className="fixed inset-y-0 right-0 hidden w-[250px] border-l border-black/[0.07] bg-[#fbfbf9] lg:block">
        <div className="flex h-[72px] items-center border-b border-black/[0.07] px-7">
          <div className="text-[21px] font-bold tracking-[-0.055em]">
            OFOQ
          </div>
        </div>

        <div className="px-4 py-6">
          <p className="mb-3 px-3 text-[10px] font-semibold text-[var(--ink-muted)]">
            الإدارة
          </p>

          <nav className="space-y-1">
            {navigation.map((item) => {
              const Icon =
                item.icon;

              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) =>
                    [
                      "flex h-11 items-center gap-3 rounded-[8px] px-3 text-[12px] font-medium transition",
                      isActive
                        ? "bg-[#e8eae5] text-[var(--ink)]"
                        : "text-[var(--ink-soft)] hover:bg-black/[0.035] hover:text-[var(--ink)]",
                    ].join(" ")
                  }
                >
                  <Icon
                    size={18}
                    strokeWidth={1.6}
                  />

                  {item.label}
                </NavLink>
              );
            })}
          </nav>
        </div>
      </aside>

      <div className="lg:mr-[250px]">
        <header className="sticky top-0 z-30 flex h-[72px] items-center justify-between border-b border-black/[0.07] bg-[#f8f8f5]/95 px-5 backdrop-blur-xl md:px-8">
          <div className="flex items-center gap-3">
            <button
              type="button"
              className="flex size-10 items-center justify-center lg:hidden"
            >
              <Menu size={21} />
            </button>

            <div>
              <p className="text-[12px] font-semibold">
                متجر نور
              </p>

              <p className="text-[10px] text-[var(--ink-muted)]">
                noor.ofoq.store
              </p>
            </div>
          </div>

          <div className="flex size-9 items-center justify-center rounded-full bg-[#ded9cc] text-[10px] font-bold">
            ON
          </div>
        </header>

        <main className="px-5 py-7 md:px-8 md:py-9">
          <Outlet />
        </main>
      </div>
    </div>
  );
}