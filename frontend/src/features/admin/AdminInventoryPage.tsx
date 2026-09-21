import { AdminPickupSetup } from "./AdminPickupSetup";
import { useCallback, useEffect, useState } from "react";
import { Boxes, MapPin, Pencil, Plus, Warehouse, X } from "lucide-react";
import { authorizedApiFetch } from "../auth/authSession";
import { getCurrentTenantId, getProducts, type Product } from "./products/productsApi";

type Location = {
  id: string;
  code: string;
  name: string;
  phone: string | null;
  countryCode: string;
  city: string;
  region: string | null;
  line1: string;
  line2: string | null;
  isDefault: boolean;
  isActive: boolean;
};

type LocationForm = Omit<Location, "id">;

const emptyForm = (): LocationForm => ({
  code: "",
  name: "",
  phone: null,
  countryCode: "SY",
  city: "",
  region: null,
  line1: "",
  line2: null,
  isDefault: false,
  isActive: true,
});

const fieldClass =
  "h-11 w-full rounded-xl border border-black/10 bg-white px-3 text-sm outline-none focus:border-[#315F5B]";

async function responseError(response: Response) {
  try {
    const body = (await response.json()) as {
      message?: string;
      title?: string;
    };
    return body.message || body.title || `HTTP ${response.status}`;
  } catch {
    return `تعذر تنفيذ العملية (${response.status}).`;
  }
}

export function AdminInventoryPage() {
  const tenantId = getCurrentTenantId();

  const [locations, setLocations] = useState<Location[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(Boolean(tenantId));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<LocationForm>(emptyForm);

  const locationsUrl =
    `/api/tenants/${encodeURIComponent(tenantId ?? "")}/backoffice/fulfillment/locations`;

  const refresh = useCallback(async () => {
    if (!tenantId) {
      setError("لم يتم تحديد المتجر الحالي.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError("");

    try {
      const [response, productResult] = await Promise.all([
        authorizedApiFetch(
          `/api/tenants/${encodeURIComponent(tenantId)}/backoffice/fulfillment/locations`
        ),
        getProducts(tenantId),
      ]);

      if (!response.ok) throw new Error(await responseError(response));

      setLocations((await response.json()) as Location[]);
      setProducts(productResult);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "تعذر تحميل المخزون.");
    } finally {
      setLoading(false);
    }
  }, [tenantId]);

  useEffect(() => {
    let active = true;
    if (!tenantId) return;
    // The initial request updates state only after the asynchronous fetch.
    // Manual refreshes continue to use refresh(), including its loading state.
    void Promise.all([
      authorizedApiFetch(`/api/tenants/${encodeURIComponent(tenantId)}/backoffice/fulfillment/locations`),
      getProducts(tenantId),
    ]).then(async ([response, productResult]) => {
      if (!response.ok) throw new Error(await responseError(response));
      const nextLocations = (await response.json()) as Location[];
      if (!active) return;
      setLocations(nextLocations);
      setProducts(productResult);
      setError("");
    }).catch(caught => {
      if (active) setError(caught instanceof Error ? caught.message : "تعذر تحميل المخزون.");
    }).finally(() => {
      if (active) setLoading(false);
    });
    return () => { active = false; };
  }, [tenantId]);

  function openCreate() {
    setEditingId(null);
    setForm(emptyForm());
    setError("");
    setMessage("");
    setFormOpen(true);
  }

  function openEdit(location: Location) {
    const { id, ...values } = location;
    setEditingId(id);
    setForm(values);
    setError("");
    setMessage("");
    setFormOpen(true);
  }

  async function saveLocation() {
    if (saving || !tenantId) return;

    if (
      !form.code.trim() ||
      !form.name.trim() ||
      !form.city.trim() ||
      !form.line1.trim() ||
      !/^[A-Za-z]{2}$/.test(form.countryCode)
    ) {
      setError("أكمل اسم الموقع ورمزه والمدينة والعنوان ورمز الدولة.");
      return;
    }

    setSaving(true);
    setError("");

    try {
      const response = await authorizedApiFetch(
        editingId
          ? `${locationsUrl}/${encodeURIComponent(editingId)}`
          : locationsUrl,
        {
          method: editingId ? "PUT" : "POST",
          body: JSON.stringify({
            ...form,
            code: form.code.trim(),
            name: form.name.trim(),
            countryCode: form.countryCode.trim().toUpperCase(),
            city: form.city.trim(),
            line1: form.line1.trim(),
          }),
        }
      );

      if (!response.ok) throw new Error(await responseError(response));

      setFormOpen(false);
      setMessage("تم حفظ الموقع بنجاح.");
      await refresh();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "تعذر حفظ الموقع.");
    } finally {
      setSaving(false);
    }
  }

  const rows = products.flatMap((product) =>
    product.variants.map((variant) => ({
      productName: product.name,
      variant,
    }))
  );

  const totalUnits = rows.reduce(
    (sum, row) =>
      sum + (row.variant.trackInventory ? row.variant.quantity : 0),
    0
  );

  return (
    <div dir="rtl" className="mx-auto max-w-[1380px] space-y-6 text-[#193c30]">
      <header>
        <p className="text-xs text-[#668176]">إدارة المتجر</p>
        <h1 className="mt-2 text-3xl font-semibold">المخزون</h1>
        <p className="mt-2 text-sm text-black/50">
          إدارة مواقع التخزين ومتابعة كميات المنتجات.
        </p>
      </header>

      {(error || (!tenantId ? "لم يتم تحديد المتجر الحالي." : "")) && (
        <p role="alert" className="rounded-xl bg-red-50 p-4 text-sm text-red-700">
          {error || "لم يتم تحديد المتجر الحالي."}
        </p>
      )}

      {message && (
        <p role="status" className="rounded-xl bg-green-50 p-4 text-sm text-green-800">
          {message}
        </p>
      )}

      <div className="grid gap-3 sm:grid-cols-3">
        {[
          ["المنتجات", products.length],
          ["إجمالي الوحدات", totalUnits],
          ["المواقع النشطة", locations.filter((x) => x.isActive).length],
        ].map(([label, value]) => (
          <div key={label} className="rounded-2xl border border-black/10 bg-white p-5">
            <p className="text-xs text-black/50">{label}</p>
            <p className="mt-3 text-3xl font-semibold">{value}</p>
          </div>
        ))}
      </div>

      <section className="rounded-2xl border border-black/10 bg-white p-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold">مواقع التخزين والاستلام</h2>
            <p className="mt-2 text-xs text-black/50">
              أضف محلاتك ومستودعاتك وحدد الموقع الرئيسي.
            </p>
          </div>

          <button
            type="button"
            onClick={openCreate}
            className="inline-flex items-center gap-2 rounded-xl bg-[#193c30] px-5 py-3 text-sm font-semibold text-white"
            style={{ color: "#ffffff" }}
          >
            <Plus size={16} /> إضافة موقع
          </button>
        </div>

        <div className="mt-5 grid gap-3 md:grid-cols-2">
          {locations.map((location) => (
            <article key={location.id} className="rounded-xl border border-black/10 p-4">
              <div className="flex items-start gap-3">
                <Warehouse size={21} className="mt-1 shrink-0" />

                <div className="min-w-0 flex-1">
                  <h3 className="font-semibold">{location.name}</h3>
                  <p className="mt-2 text-xs text-black/50">
                    {location.city} · {location.line1}
                  </p>
                  <p className="mt-2 text-xs text-black/50">
                    {location.isDefault ? "الموقع الرئيسي · " : ""}
                    {location.isActive ? "نشط" : "معطّل"}
                  </p>
                </div>

                <button
                  type="button"
                  aria-label={`تعديل ${location.name}`}
                  onClick={() => openEdit(location)}
                  className="rounded-lg border border-black/10 p-2"
                >
                  <Pencil size={16} />
                </button>
              </div>
            </article>
          ))}

          {!loading && locations.length === 0 && (
            <p className="rounded-xl bg-[#f7f7f4] p-5 text-sm text-black/50">
              لا توجد مواقع بعد. أضف موقعك الأول.
            </p>
          )}
        </div>
      </section>

            <AdminPickupSetup tenantId={tenantId} locations={locations} />

      <section className="overflow-hidden rounded-2xl border border-black/10 bg-white">
        <div className="border-b border-black/10 p-5">
          <h2 className="text-lg font-semibold">مخزون المنتجات</h2>
          <p className="mt-2 text-xs text-black/50">
            الكميات الإجمالية الحالية. سيتم ربط كمية كل مستودع في المرحلة التالية.
          </p>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[520px] text-right text-sm">
            <thead className="bg-[#faf9f6] text-black/60">
              <tr>
                <th className="p-4">المنتج</th>
                <th className="p-4">SKU</th>
                <th className="p-4">الكمية</th>
                <th className="p-4">الحالة</th>
              </tr>
            </thead>

            <tbody>
              {rows.map(({ productName, variant }) => (
                <tr key={variant.variantId} className="border-t border-black/10">
                  <td className="p-4">
                    <Boxes size={15} className="ml-2 inline" />
                    {productName}
                    <p className="mt-1 text-xs text-black/40">{variant.name}</p>
                  </td>

                  <td dir="ltr" className="p-4 text-left">{variant.sku}</td>

                  <td className="p-4 font-semibold">
                    {variant.trackInventory ? variant.quantity : "—"}
                  </td>

                  <td className="p-4 text-xs">
                    {!variant.trackInventory
                      ? "غير متتبع"
                      : variant.quantity <= variant.lowStockThreshold
                        ? "مخزون منخفض"
                        : "متوفر"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {!loading && rows.length === 0 && (
          <p className="p-6 text-center text-sm text-black/50">
            لا توجد منتجات بعد.
          </p>
        )}
      </section>

      {loading && (
        <p className="text-center text-sm text-black/50">جاري تحميل المخزون...</p>
      )}

      {formOpen && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/40 p-4">
          <div className="max-h-[90vh] w-full max-w-xl overflow-y-auto rounded-2xl bg-[#faf9f6] p-6">
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-semibold">
                {editingId ? "تعديل الموقع" : "إضافة موقع"}
              </h2>

              <button
                type="button"
                aria-label="إغلاق"
                disabled={saving}
                onClick={() => setFormOpen(false)}
              >
                <X size={19} />
              </button>
            </div>

            <div className="mt-5 grid gap-4 sm:grid-cols-2">
              {([
                ["name", "اسم الموقع"],
                ["code", "رمز الموقع"],
                ["countryCode", "رمز الدولة"],
                ["city", "المدينة"],
                ["region", "المنطقة"],
                ["phone", "رقم التواصل"],
                ["line1", "العنوان"],
                ["line2", "تفاصيل إضافية"],
              ] as const).map(([key, label]) => (
                <label key={key}>
                  <span className="mb-2 block text-xs">{label}</span>
                  <input
                    className={fieldClass}
                    value={form[key] ?? ""}
                    onChange={(e) =>
                      setForm((current) => ({
                        ...current,
                        [key]: e.target.value,
                      }))
                    }
                  />
                </label>
              ))}
            </div>

            <label className="mt-5 flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isDefault}
                onChange={(e) =>
                  setForm((current) => ({
                    ...current,
                    isDefault: e.target.checked,
                  }))
                }
              />
              تعيين كموقع رئيسي
            </label>

            {editingId && (
              <label className="mt-3 flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(e) =>
                    setForm((current) => ({
                      ...current,
                      isActive: e.target.checked,
                    }))
                  }
                />
                الموقع نشط
              </label>
            )}

            <button
              type="button"
              disabled={saving}
              onClick={() => void saveLocation()}
              className="mt-6 w-full rounded-xl bg-[#193c30] px-5 py-3 text-sm font-semibold text-white disabled:opacity-50"
              style={{ color: "#ffffff" }}
            >
              {saving ? "جاري الحفظ..." : "حفظ الموقع"}
            </button>
          </div>
        </div>
      )}

      <p className="text-xs text-black/40">
        <MapPin size={13} className="ml-1 inline" />
        توزيع المخزون على المستودعات وربطه بإتمام الطلب سيُفعّل لاحقًا.
      </p>
    </div>
  );
}