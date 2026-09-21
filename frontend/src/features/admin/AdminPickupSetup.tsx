import { useEffect, useState } from "react";

import { authorizedApiFetch } from "../auth/authSession";

interface Location {
  id: string;
  name: string;
  isActive: boolean;
}

interface ShippingMethod {
  id: string;
  name: string;
  type: string;
  currency: string;
  pickupLocationId: string | null;
  isEnabled: boolean;
}

interface Props {
  tenantId: string | null;
  locations: Location[];
}

async function responseError(response: Response) {
  try {
    const data = (await response.json()) as {
      message?: string;
      title?: string;
    };

    return (
      data.message ??
      data.title ??
      `HTTP ${response.status}`
    );
  } catch {
    return `تعذر تنفيذ العملية (${response.status}).`;
  }
}

export function AdminPickupSetup({
  tenantId,
  locations,
}: Props) {
  const [locationId, setLocationId] = useState("");

  const [currency, setCurrency] = useState("SAR");

  const [methods, setMethods] = useState<ShippingMethod[]>([]);

  const [loading, setLoading] = useState(false);

  const [saving, setSaving] = useState(false);

  const [error, setError] = useState("");

  const [notice, setNotice] = useState("");

  const activeLocations = locations.filter(
    (location) => location.isActive
  );

  const selectedLocation =
    activeLocations.find(
      (location) => location.id === locationId
    ) ?? activeLocations[0];

  const base = tenantId
    ? `/api/tenants/${encodeURIComponent(tenantId)}/backoffice/fulfillment`
    : "";

  async function refreshMethods() {
    if (!tenantId) return;

    const response = await authorizedApiFetch(
      `${base}/shipping-methods`
    );

    if (!response.ok) {
      throw new Error(await responseError(response));
    }

    const data = (await response.json()) as ShippingMethod[];

    setMethods(data);
  }

  useEffect(() => {
    if (!tenantId) return;

    let active = true;

    async function load() {
      setLoading(true);

      try {
        const response = await authorizedApiFetch(
          `/api/tenants/${encodeURIComponent(
            tenantId!
          )}/backoffice/fulfillment/shipping-methods`
        );

        if (!response.ok) {
          throw new Error(await responseError(response));
        }

        const data =
          (await response.json()) as ShippingMethod[];

        if (active) {
          setMethods(data);
          setError("");
        }
      } catch (caught) {
        if (active) {
          setError(
            caught instanceof Error
              ? caught.message
              : "تعذر تحميل طرق الاستلام."
          );
        }
      } finally {
        if (active) setLoading(false);
      }
    }

    void load();

    return () => {
      active = false;
    };
  }, [tenantId]);

  const existing = methods.find(
    (method) =>
      method.type.toLowerCase() === "pickup" &&
      method.pickupLocationId === selectedLocation?.id &&
      method.currency === currency
  );

  async function activatePickup() {
    if (
      !tenantId ||
      !selectedLocation ||
      saving ||
      loading
    ) {
      return;
    }

    setError("");
    setNotice("");

    if (existing?.isEnabled) {
      setNotice(
        "طريقة الاستلام مفعّلة مسبقًا لهذا الموقع والعملة."
      );
      return;
    }

    if (existing && !existing.isEnabled) {
      setError(
        "توجد طريقة استلام معطّلة لهذا الموقع. يجب إعادة تفعيلها بدل إنشاء طريقة مكررة."
      );
      return;
    }

    setSaving(true);

    try {
      const response = await authorizedApiFetch(
        `${base}/shipping-methods`,
        {
          method: "POST",
          body: JSON.stringify({
            code:
              "pickup-" +
              crypto.randomUUID().slice(0, 12),

            name: `استلام من ${selectedLocation.name}`,

            type: "Pickup",

            price: 0,

            currency,

            minimumOrderAmount: null,

            maximumOrderAmount: null,

            pickupLocationId: selectedLocation.id,

            sortOrder: 0,

            isEnabled: true,
          }),
        }
      );

      if (!response.ok) {
        throw new Error(await responseError(response));
      }

      await refreshMethods();

      setNotice(
        "تم إنشاء طريقة الاستلام. تحقق من ظهورها ضمن الطرق المفعّلة."
      );
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر إنشاء طريقة الاستلام."
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <section
      dir="rtl"
      className="rounded-2xl border border-black/10 bg-white p-5 sm:p-6"
    >
      <div className="mb-6">
        <h2 className="text-lg font-semibold">
          إعدادات الاستلام من المتجر
        </h2>

        <p className="mt-2 text-xs leading-7 text-black/50">
          اختر موقعًا حقيقيًا لإتاحة استلام طلبات
          العملاء منه.
        </p>
      </div>

      {error && (
        <p
          role="alert"
          className="mb-4 rounded-xl bg-red-50 p-4 text-sm text-red-700"
        >
          {error}
        </p>
      )}

      {notice && (
        <p
          role="status"
          className="mb-4 rounded-xl bg-green-50 p-4 text-sm text-green-800"
        >
          {notice}
        </p>
      )}

      {activeLocations.length === 0 ? (
        <p className="rounded-xl bg-[#faf9f6] p-4 text-sm text-black/60">
          أضف موقعًا نشطًا من قسم مواقع التخزين أولًا.
        </p>
      ) : (
        <div className="space-y-5">
          <label className="block">
            <span className="mb-2 block text-xs font-medium">
              موقع الاستلام
            </span>

            <select
              value={selectedLocation?.id ?? ""}
              onChange={(event) =>
                setLocationId(event.target.value)
              }
              className="h-12 w-full rounded-xl border border-black/10 bg-white px-4 text-sm"
            >
              {activeLocations.map((location) => (
                <option
                  key={location.id}
                  value={location.id}
                >
                  {location.name}
                </option>
              ))}
            </select>
          </label>

          <label className="block">
            <span className="mb-2 block text-xs font-medium">
              عملة الطلب
            </span>

            <select
              value={currency}
              onChange={(event) =>
                setCurrency(event.target.value)
              }
              className="h-12 w-full rounded-xl border border-black/10 bg-white px-4 text-sm"
            >
              <option value="SAR">ريال سعودي - SAR</option>
              <option value="AED">درهم إماراتي - AED</option>
              <option value="USD">دولار أمريكي - USD</option>
            </select>
          </label>

          <div className="flex items-center justify-between rounded-xl bg-[#faf9f6] p-4 text-sm">
            <span>رسوم الاستلام</span>
            <strong>مجاني</strong>
          </div>

          {existing && (
            <p className="text-xs text-[#315F5B]">
              {existing.isEnabled
                ? "طريقة الاستلام مفعّلة لهذا الموقع."
                : "توجد طريقة استلام معطّلة لهذا الموقع."}
            </p>
          )}

          <button
            type="button"
            onClick={() => void activatePickup()}
            disabled={
              saving ||
              loading ||
              !!existing?.isEnabled
            }
            className="min-h-12 w-full rounded-xl bg-[#193C30] px-5 text-sm font-semibold disabled:opacity-50"
            style={{ color: "#ffffff" }}
          >
            {saving
              ? "جاري الحفظ..."
              : loading
                ? "جاري التحميل..."
                : existing?.isEnabled
                  ? "الاستلام مفعّل"
                  : "تفعيل الاستلام من هذا الموقع"}
          </button>
        </div>
      )}
    </section>
  );
}