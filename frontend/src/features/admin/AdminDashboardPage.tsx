import {
  ArrowUpLeft,
  CircleDollarSign,
  PackageCheck,
  ShoppingBag,
  TrendingUp,
} from "lucide-react";

import { Link } from "react-router";

const metrics = [
  {
    label: "المبيعات",
    value: "42,850 ر.س",
    note: "+12.4%",
    icon: CircleDollarSign,
  },
  {
    label: "الطلبات",
    value: "184",
    note: "+8.1%",
    icon: ShoppingBag,
  },
  {
    label: "متوسط الطلب",
    value: "233 ر.س",
    note: "+3.7%",
    icon: TrendingUp,
  },
  {
    label: "بانتظار التجهيز",
    value: "12",
    note: "يحتاج متابعة",
    icon: PackageCheck,
  },
];

const orders = [
  ["#1048", "سارة أحمد", "348 ر.س", "قيد التجهيز"],
  ["#1047", "محمد علي", "1,240 ر.س", "مدفوع"],
  ["#1046", "ريم خالد", "590 ر.س", "تم الشحن"],
  ["#1045", "عبدالله سالم", "215 ر.س", "تم التوصيل"],
];

export function AdminDashboardPage() {
  return (
    <div className="mx-auto max-w-[1320px]">
      <div className="mb-8 flex flex-wrap items-end justify-between gap-5">
        <div>
          <p className="mb-2 text-[11px] text-[var(--ink-muted)]">
            الثلاثاء، 15 سبتمبر
          </p>

          <h1 className="text-[30px] font-semibold tracking-[-0.045em]">
            صباح الخير
          </h1>

          <p className="mt-2 text-[12px] text-[var(--ink-soft)]">
            هذه أهم الأشياء التي تحتاج انتباهك اليوم
          </p>
        </div>

        <Link
          to="/store/demo"
          className="inline-flex h-10 items-center gap-2 rounded-[8px] border border-black/[0.12] bg-white px-4 text-[11px] font-semibold"
        >
          فتح المتجر

          <ArrowUpLeft size={15} />
        </Link>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {metrics.map((metric) => {
          const Icon =
            metric.icon;

          return (
            <article
              key={metric.label}
              className="border border-black/[0.07] bg-[#fbfbf9] p-5"
            >
              <div className="mb-8 flex items-center justify-between">
                <span className="text-[11px] text-[var(--ink-soft)]">
                  {metric.label}
                </span>

                <Icon
                  size={18}
                  strokeWidth={1.5}
                  className="text-[var(--ink-muted)]"
                />
              </div>

              <div className="text-[26px] font-semibold tracking-[-0.04em]">
                {metric.value}
              </div>

              <p className="mt-2 text-[10px] text-[var(--success)]">
                {metric.note}
              </p>
            </article>
          );
        })}
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-[1.35fr_.65fr]">
        <section className="border border-black/[0.07] bg-[#fbfbf9]">
          <div className="flex items-center justify-between border-b border-black/[0.07] px-5 py-4">
            <div>
              <h2 className="text-[13px] font-semibold">
                أحدث الطلبات
              </h2>

              <p className="mt-1 text-[10px] text-[var(--ink-muted)]">
                آخر العمليات في المتجر
              </p>
            </div>

            <button
              type="button"
              className="text-[11px] font-semibold"
            >
              كل الطلبات
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full min-w-[620px] border-collapse text-right">
              <thead>
                <tr className="border-b border-black/[0.06] text-[10px] text-[var(--ink-muted)]">
                  <th className="px-5 py-3">
                    الطلب
                  </th>

                  <th className="px-5 py-3">
                    العميل
                  </th>

                  <th className="px-5 py-3">
                    المبلغ
                  </th>

                  <th className="px-5 py-3">
                    الحالة
                  </th>
                </tr>
              </thead>

              <tbody>
                {orders.map((order) => (
                  <tr
                    key={order[0]}
                    className="border-b border-black/[0.05] last:border-0"
                  >
                    <td className="px-5 py-4 text-[11px] font-semibold">
                      {order[0]}
                    </td>

                    <td className="px-5 py-4 text-[11px]">
                      {order[1]}
                    </td>

                    <td className="px-5 py-4 text-[11px]">
                      {order[2]}
                    </td>

                    <td className="px-5 py-4 text-[10px] text-[var(--ink-soft)]">
                      {order[3]}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="bg-[#234239] p-7 text-white">
          <p className="text-[10px] text-white/60">
            أداء هذا الشهر
          </p>

          <div className="mt-10">
            <div className="text-[43px] font-semibold tracking-[-0.055em]">
              76%
            </div>

            <p className="mt-3 max-w-xs text-[12px] leading-6 text-white/70">
              اقتربت من أفضل أداء شهري للمتجر خلال هذا العام
            </p>
          </div>

          <div className="mt-11 h-1.5 overflow-hidden rounded-full bg-white/15">
            <div className="h-full w-[76%] rounded-full bg-white" />
          </div>
        </section>
      </div>
    </div>
  );
}