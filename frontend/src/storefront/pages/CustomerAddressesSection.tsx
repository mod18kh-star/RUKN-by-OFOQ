import {
  useEffect,
  useState,
  type FormEvent,
} from "react";

import {
  Building2,
  Check,
  House,
  Link2,
  LoaderCircle,
  LocateFixed,
  MapPin,
  Pencil,
  Plus,
  Trash2,
  X,
} from "lucide-react";

import {
  authRequest,
} from "../../features/auth/authSession";

interface SavedAddress {
  id: string;
  userId: string;
  label: string;
  recipientName: string;
  phone: string;
  countryCode: string;
  region: string | null;
  city: string;
  postalCode: string | null;
  line1: string;
  line2: string | null;
  latitude: number | null;
  longitude: number | null;
  mapUrl: string | null;
  deliveryNotes: string | null;
  isDefault: boolean;
}

interface AddressDraft {
  label: string;
  customLabel: string;
  recipientName: string;
  phone: string;
  countryCode: string;
  region: string;
  city: string;
  postalCode: string;
  line1: string;
  line2: string;
  latitude: number | null;
  longitude: number | null;
  mapUrl: string;
  deliveryNotes: string;
  isDefault: boolean;
}

const emptyDraft: AddressDraft = {
  label: "المنزل",
  customLabel: "",
  recipientName: "",
  phone: "",
  countryCode: "",
  region: "",
  city: "",
  postalCode: "",
  line1: "",
  line2: "",
  latitude: null,
  longitude: null,
  mapUrl: "",
  deliveryNotes: "",
  isDefault: false,
};

const inputClass =
  "mt-2 h-11 w-full rounded-xl border border-black/10 bg-white px-3 text-sm text-[#24352d] outline-none transition focus:border-[#537260]";

const labelClass =
  "block text-sm font-medium text-[#24352d]";

const primaryClass =
  "inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-[#193c30] px-5 text-sm font-semibold text-white transition hover:bg-[#295541] disabled:cursor-not-allowed disabled:opacity-50";

const secondaryClass =
  "inline-flex min-h-10 items-center justify-center gap-2 rounded-xl border border-black/10 bg-white px-4 text-sm font-medium text-[#34483b] transition hover:bg-[#f4f6f2] disabled:opacity-50";

function labelIcon(label: string) {
  if (label === "المنزل") {
    return <House size={19} />;
  }

  if (label === "المكتب") {
    return <Building2 size={19} />;
  }

  return <MapPin size={19} />;
}

function mapLink(address: SavedAddress) {
  if (
    address.latitude !== null &&
    address.longitude !== null
  ) {
    return `https://www.google.com/maps/search/?api=1&query=${address.latitude},${address.longitude}`;
  }

  if (address.mapUrl) {
    return address.mapUrl;
  }

  return null;
}

function formatError(error: unknown) {
  if (error instanceof Error) {
    if ("status" in error && error.status === 401) {
      return "انتهت جلسة الدخول. سجّل دخولك مجددًا.";
    }

    return error.message;
  }

  return "تعذر إتمام العملية. حاول مرة أخرى.";
}

function toRequest(draft: AddressDraft) {
  const label =
    draft.label === "آخر"
      ? draft.customLabel.trim()
      : draft.label;

  return {
    label,
    recipientName: draft.recipientName.trim(),
    phone: draft.phone.trim(),
    countryCode: draft.countryCode.trim().toUpperCase(),
    region: draft.region.trim() || null,
    city: draft.city.trim(),
    postalCode: draft.postalCode.trim() || null,
    line1: draft.line1.trim(),
    line2: draft.line2.trim() || null,
    latitude: draft.latitude,
    longitude: draft.longitude,
    mapUrl: draft.mapUrl.trim() || null,
    deliveryNotes: draft.deliveryNotes.trim() || null,
    isDefault: draft.isDefault,
  };
}

export function CustomerAddressesSection() {
  const [addresses, setAddresses] =
    useState<SavedAddress[]>([]);

  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [locating, setLocating] = useState(false);

  const [editingId, setEditingId] =
    useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);

  const [draft, setDraft] =
    useState<AddressDraft>({ ...emptyDraft });

  const [error, setError] =
    useState<string | null>(null);

  const [success, setSuccess] =
    useState<string | null>(null);

  async function refreshAddresses() {
    const result = await authRequest<SavedAddress[]>(
      "/api/customer/addresses",
      { method: "GET" },
    );

    setAddresses(result);
  }

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const result = await authRequest<SavedAddress[]>(
          "/api/customer/addresses",
          { method: "GET" },
        );

        if (active) {
          setAddresses(result);
        }
      } catch (caught) {
        if (active) {
          setError(formatError(caught));
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void load();

    return () => {
      active = false;
    };
  }, []);

  function updateDraft<K extends keyof AddressDraft>(
    key: K,
    value: AddressDraft[K],
  ) {
    setDraft((current) => ({
      ...current,
      [key]: value,
    }));
  }

  function startCreate() {
    setEditingId(null);
    setDraft({ ...emptyDraft });
    setError(null);
    setSuccess(null);
    setFormOpen(true);
  }

  function startEdit(address: SavedAddress) {
    const knownLabel =
      address.label === "المنزل" ||
      address.label === "المكتب";

    setEditingId(address.id);

    setDraft({
      label: knownLabel ? address.label : "آخر",
      customLabel: knownLabel ? "" : address.label,
      recipientName: address.recipientName,
      phone: address.phone,
      countryCode: address.countryCode,
      region: address.region ?? "",
      city: address.city,
      postalCode: address.postalCode ?? "",
      line1: address.line1,
      line2: address.line2 ?? "",
      latitude: address.latitude,
      longitude: address.longitude,
      mapUrl: address.mapUrl ?? "",
      deliveryNotes: address.deliveryNotes ?? "",
      isDefault: address.isDefault,
    });

    setError(null);
    setSuccess(null);
    setFormOpen(true);
  }

  function cancelEdit() {
    setFormOpen(false);
    setEditingId(null);
    setDraft({ ...emptyDraft });
    setError(null);
  }

  function locateMe() {
    if (!navigator.geolocation) {
      setError(
        "متصفحك لا يدعم تحديد الموقع. يمكنك إضافة رابط الموقع يدويًا.",
      );

      return;
    }

    if (!window.isSecureContext) {
      setError(
        "تحديد الموقع يحتاج اتصال HTTPS أو التشغيل على localhost.",
      );

      return;
    }

    setLocating(true);
    setError(null);

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setDraft((current) => ({
          ...current,
          latitude: Number(
            position.coords.latitude.toFixed(6),
          ),
          longitude: Number(
            position.coords.longitude.toFixed(6),
          ),
        }));

        setLocating(false);
      },
      () => {
        setError(
          "تعذر تحديد الموقع. تأكد من السماح للمتصفح بالوصول إلى موقعك، أو أضف رابط الموقع يدويًا.",
        );

        setLocating(false);
      },
      {
        enableHighAccuracy: true,
        timeout: 15000,
        maximumAge: 0,
      },
    );
  }

  async function saveAddress(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    if (busy) return;

    const request = toRequest(draft);

    if (!request.label) {
      setError("اكتب اسم العنوان.");
      return;
    }

    if (!/^[A-Z]{2}$/.test(request.countryCode)) {
      setError(
        "اكتب رمز الدولة من حرفين، مثل SA أو SY أو AE.",
      );

      return;
    }

    if (request.mapUrl) {
      try {
        const url = new URL(request.mapUrl);

        if (
          url.protocol !== "https:" ||
          url.username ||
          url.password
        ) {
          throw new Error();
        }
      } catch {
        setError(
          "اكتب رابط موقع صحيحًا يبدأ بـ https://",
        );

        return;
      }
    }

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      if (editingId) {
        await authRequest<SavedAddress>(
          `/api/customer/addresses/${editingId}`,
          {
            method: "PUT",
            body: JSON.stringify(request),
          },
        );
      } else {
        await authRequest<SavedAddress>(
          "/api/customer/addresses",
          {
            method: "POST",
            body: JSON.stringify(request),
          },
        );
      }

      await refreshAddresses();

      setSuccess(
        editingId
          ? "تم تحديث العنوان بنجاح."
          : "تم حفظ العنوان بنجاح.",
      );

      setFormOpen(false);
      setEditingId(null);
      setDraft({ ...emptyDraft });
    } catch (caught) {
      setError(formatError(caught));
    } finally {
      setBusy(false);
    }
  }

  async function setDefault(address: SavedAddress) {
    if (busy || address.isDefault) return;

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await authRequest<SavedAddress>(
        `/api/customer/addresses/${address.id}/default`,
        { method: "PUT" },
      );

      await refreshAddresses();

      setSuccess("تم تعيين العنوان الافتراضي.");
    } catch (caught) {
      setError(formatError(caught));
    } finally {
      setBusy(false);
    }
  }

  async function deleteAddress(address: SavedAddress) {
    if (busy) return;

    const confirmed = window.confirm(
      `هل تريد حذف عنوان "${address.label}"؟`,
    );

    if (!confirmed) return;

    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await authRequest<void>(
        `/api/customer/addresses/${address.id}`,
        { method: "DELETE" },
      );

      await refreshAddresses();

      if (editingId === address.id) {
        cancelEdit();
      }

      setSuccess("تم حذف العنوان.");
    } catch (caught) {
      setError(formatError(caught));
    } finally {
      setBusy(false);
    }
  }

  return (
    <section
      dir="rtl"
      className="rounded-2xl border border-black/[0.07] bg-white p-5 sm:p-7"
    >
      <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="mb-3 flex size-11 items-center justify-center rounded-xl bg-[#edf3ee] text-[#193c30]">
            <MapPin size={20} />
          </div>

          <h2 className="text-lg font-semibold text-[#20382d]">
            عناويني
          </h2>

          <p className="mt-2 text-sm leading-6 text-[#748077]">
            احفظ عناوينك واستخدمها عند التسوق من متاجر ركن.
          </p>
        </div>

        {!formOpen && (
          <button
            type="button"
            onClick={startCreate}
            className={primaryClass}
            style={{ color: "#ffffff" }}
          >
            <Plus size={17} />
            إضافة عنوان
          </button>
        )}
      </div>

      {error && (
        <div
          role="alert"
          className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm leading-6 text-red-800"
        >
          {error}
        </div>
      )}

      {success && (
        <div
          role="status"
          className="mb-5 rounded-xl border border-green-200 bg-green-50 p-4 text-sm text-green-800"
        >
          {success}
        </div>
      )}

      {loading ? (
        <p className="text-sm text-[#748077]">
          جاري تحميل عناوينك...
        </p>
      ) : (
        <div className="space-y-3">
          {addresses.length === 0 && !formOpen && (
            <div className="rounded-xl bg-[#f7f8f5] px-5 py-7 text-center">
              <MapPin
                size={25}
                className="mx-auto mb-3 text-[#738675]"
              />

              <p className="text-sm font-medium">
                لم تحفظ أي عنوان بعد
              </p>

              <p className="mt-2 text-xs leading-6 text-[#748077]">
                أضف عنوان المنزل أو المكتب لتستخدمه لاحقًا
                عند طلب المنتجات التي تحتاج توصيلًا.
              </p>
            </div>
          )}

          {addresses.map((address) => {
            const destination = mapLink(address);

            return (
              <article
                key={address.id}
                className="rounded-xl border border-black/[0.08] p-4 sm:p-5"
              >
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex items-center gap-2">
                    <span className="text-[#476b51]">
                      {labelIcon(address.label)}
                    </span>

                    <h3 className="text-sm font-semibold">
                      {address.label}
                    </h3>

                    {address.isDefault && (
                      <span className="rounded-full bg-[#eaf2e9] px-2 py-1 text-[11px] font-medium text-[#315e3e]">
                        الافتراضي
                      </span>
                    )}
                  </div>
                </div>

                <p className="mt-3 text-sm font-medium">
                  {address.recipientName}
                </p>

                <p className="mt-1 text-sm leading-7 text-[#748077]">
                  {address.line1}
                  {address.line2 ? `، ${address.line2}` : ""}
                  {"، "}
                  {address.city}
                  {address.region ? `، ${address.region}` : ""}
                  {"، "}
                  {address.countryCode}
                </p>

                <p
                  dir="ltr"
                  className="mt-1 text-right text-sm text-[#748077]"
                >
                  {address.phone}
                </p>

                {address.deliveryNotes && (
                  <p className="mt-3 rounded-lg bg-[#f7f8f5] p-3 text-xs leading-6 text-[#66746a]">
                    ملاحظات التوصيل: {address.deliveryNotes}
                  </p>
                )}

                {destination && (
                  <a
                    href={destination}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="mt-3 inline-flex items-center gap-2 text-xs font-medium text-[#315f45] underline underline-offset-4"
                  >
                    <MapPin size={14} />
                    عرض الموقع على الخريطة
                  </a>
                )}

                <div className="mt-5 flex flex-wrap gap-2 border-t border-black/[0.06] pt-4">
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => startEdit(address)}
                    className={secondaryClass}
                  >
                    <Pencil size={15} />
                    تعديل
                  </button>

                  {!address.isDefault && (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => void setDefault(address)}
                      className={secondaryClass}
                    >
                      <Check size={15} />
                      تعيين كافتراضي
                    </button>
                  )}

                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => void deleteAddress(address)}
                    className={`${secondaryClass} text-red-700`}
                  >
                    <Trash2 size={15} />
                    حذف
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      )}

      {formOpen && (
        <form
          onSubmit={saveAddress}
          className="mt-6 space-y-6 border-t border-black/[0.07] pt-6"
        >
          <div className="flex items-center justify-between gap-3">
            <h3 className="text-base font-semibold">
              {editingId
                ? "تعديل العنوان"
                : "إضافة عنوان جديد"}
            </h3>

            <button
              type="button"
              disabled={busy}
              onClick={cancelEdit}
              aria-label="إغلاق النموذج"
              className="flex size-9 items-center justify-center rounded-lg hover:bg-black/5"
            >
              <X size={18} />
            </button>
          </div>

          <div>
            <span className={labelClass}>
              اسم العنوان
            </span>

            <div className="mt-3 flex flex-wrap gap-2">
              {(["المنزل", "المكتب", "آخر"] as const).map(
                (option) => (
                  <button
                    key={option}
                    type="button"
                    onClick={() =>
                      updateDraft("label", option)
                    }
                    className={`rounded-xl border px-4 py-2.5 text-sm transition ${
                      draft.label === option
                        ? "border-[#193c30] bg-[#edf3ee] text-[#193c30]"
                        : "border-black/10 text-[#748077]"
                    }`}
                  >
                    {option}
                  </button>
                ),
              )}
            </div>

            {draft.label === "آخر" && (
              <input
                className={inputClass}
                placeholder="مثل: بيت العائلة"
                maxLength={80}
                value={draft.customLabel}
                onChange={(event) =>
                  updateDraft(
                    "customLabel",
                    event.target.value,
                  )
                }
                required
              />
            )}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label
                htmlFor="address-recipient"
                className={labelClass}
              >
                اسم المستلم
              </label>

              <input
                id="address-recipient"
                className={inputClass}
                maxLength={160}
                autoComplete="name"
                value={draft.recipientName}
                onChange={(event) =>
                  updateDraft(
                    "recipientName",
                    event.target.value,
                  )
                }
                required
              />
            </div>

            <div>
              <label
                htmlFor="address-phone"
                className={labelClass}
              >
                رقم الجوال
              </label>

              <input
                id="address-phone"
                type="tel"
                dir="ltr"
                className={inputClass}
                maxLength={40}
                autoComplete="tel"
                value={draft.phone}
                onChange={(event) =>
                  updateDraft(
                    "phone",
                    event.target.value,
                  )
                }
                required
              />
            </div>

            <div>
              <label
                htmlFor="address-country"
                className={labelClass}
              >
                رمز الدولة
              </label>

              <input
                id="address-country"
                className={inputClass}
                dir="ltr"
                maxLength={2}
                placeholder="SA / SY / AE"
                value={draft.countryCode}
                onChange={(event) =>
                  updateDraft(
                    "countryCode",
                    event.target.value.toUpperCase(),
                  )
                }
                required
              />
            </div>

            <div>
              <label
                htmlFor="address-city"
                className={labelClass}
              >
                المدينة
              </label>

              <input
                id="address-city"
                className={inputClass}
                maxLength={120}
                autoComplete="address-level2"
                value={draft.city}
                onChange={(event) =>
                  updateDraft(
                    "city",
                    event.target.value,
                  )
                }
                required
              />
            </div>

            <div>
              <label
                htmlFor="address-region"
                className={labelClass}
              >
                المنطقة أو المحافظة
              </label>

              <input
                id="address-region"
                className={inputClass}
                maxLength={120}
                value={draft.region}
                onChange={(event) =>
                  updateDraft(
                    "region",
                    event.target.value,
                  )
                }
              />
            </div>

            <div>
              <label
                htmlFor="address-postal"
                className={labelClass}
              >
                الرمز البريدي (اختياري)
              </label>

              <input
                id="address-postal"
                className={inputClass}
                maxLength={32}
                value={draft.postalCode}
                onChange={(event) =>
                  updateDraft(
                    "postalCode",
                    event.target.value,
                  )
                }
              />
            </div>
          </div>

          <div>
            <label
              htmlFor="address-line1"
              className={labelClass}
            >
              الحي والشارع والعنوان التفصيلي
            </label>

            <input
              id="address-line1"
              className={inputClass}
              maxLength={240}
              autoComplete="street-address"
              value={draft.line1}
              onChange={(event) =>
                updateDraft(
                  "line1",
                  event.target.value,
                )
              }
              placeholder="الحي، الشارع، رقم المبنى"
              required
            />
          </div>

          <div>
            <label
              htmlFor="address-line2"
              className={labelClass}
            >
              تفاصيل إضافية (اختياري)
            </label>

            <input
              id="address-line2"
              className={inputClass}
              maxLength={240}
              value={draft.line2}
              onChange={(event) =>
                updateDraft(
                  "line2",
                  event.target.value,
                )
              }
              placeholder="الشقة، الطابق، المدخل"
            />
          </div>

          <div className="space-y-4 rounded-xl bg-[#f7f8f5] p-4">
            <div>
              <h4 className="text-sm font-semibold">
                تحديد موقع التوصيل
              </h4>

              <p className="mt-2 text-xs leading-6 text-[#748077]">
                يمكنك استخدام موقعك الحالي بعد السماح
                للمتصفح بالوصول إليه، أو إضافة رابط
                الموقع من تطبيق الخرائط.
              </p>
            </div>

            <button
              type="button"
              disabled={locating || busy}
              onClick={locateMe}
              className={secondaryClass}
            >
              {locating ? (
                <LoaderCircle
                  size={16}
                  className="animate-spin"
                />
              ) : (
                <LocateFixed size={16} />
              )}

              {locating
                ? "جاري تحديد الموقع..."
                : "تحديد موقعي تلقائيًا"}
            </button>

            {draft.latitude !== null &&
              draft.longitude !== null && (
                <div className="space-y-2 rounded-lg border border-green-200 bg-white p-3">
                  <p className="text-xs font-medium text-[#315f45]">
                    تم تحديد الموقع
                  </p>

                  <a
                    href={`https://www.google.com/maps/search/?api=1&query=${draft.latitude},${draft.longitude}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-2 text-xs text-[#315f45] underline underline-offset-4"
                  >
                    <MapPin size={14} />
                    معاينة الموقع على الخرائط
                  </a>

                  <button
                    type="button"
                    onClick={() =>
                      setDraft((current) => ({
                        ...current,
                        latitude: null,
                        longitude: null,
                      }))
                    }
                    className="block text-xs text-red-700"
                  >
                    إزالة الموقع المحدد
                  </button>
                </div>
              )}

            <div>
              <label
                htmlFor="address-map-url"
                className={labelClass}
              >
                رابط الموقع من الخرائط (اختياري)
              </label>

              <div className="relative">
                <Link2
                  size={16}
                  className="absolute right-3 top-5 text-[#829084]"
                />

                <input
                  id="address-map-url"
                  type="url"
                  dir="ltr"
                  className={`${inputClass} pr-10`}
                  maxLength={2048}
                  placeholder="https://maps.google.com/..."
                  value={draft.mapUrl}
                  onChange={(event) =>
                    updateDraft(
                      "mapUrl",
                      event.target.value,
                    )
                  }
                />
              </div>
            </div>

            <p className="text-xs leading-6 text-[#748077]">
              تحديد الموقع يضيف الإحداثيات فقط.
              تأكد أيضًا من كتابة المدينة والعنوان
              التفصيلي حتى يتمكن المندوب من الوصول إليك.
            </p>
          </div>

          <div>
            <label
              htmlFor="address-notes"
              className={labelClass}
            >
              ملاحظات إضافية للمندوب
            </label>

            <textarea
              id="address-notes"
              rows={3}
              maxLength={600}
              className={`${inputClass} h-auto min-h-24 resize-y py-3`}
              placeholder="مثلًا: المدخل من الخلف، الطابق الثاني، اتصل قبل الوصول..."
              value={draft.deliveryNotes}
              onChange={(event) =>
                updateDraft(
                  "deliveryNotes",
                  event.target.value,
                )
              }
            />
          </div>

          <label className="flex cursor-pointer items-center gap-3 text-sm text-[#34483b]">
            <input
              type="checkbox"
              checked={draft.isDefault}
              onChange={(event) =>
                updateDraft(
                  "isDefault",
                  event.target.checked,
                )
              }
              className="size-4 accent-[#193c30]"
            />

            تعيين هذا العنوان كعنوان افتراضي
          </label>

          <div className="flex flex-wrap gap-3 border-t border-black/[0.07] pt-5">
            <button
              type="submit"
              disabled={busy || locating}
              className={primaryClass}
              style={{ color: "#ffffff" }}
            >
              {busy
                ? "جاري الحفظ..."
                : editingId
                  ? "حفظ التعديلات"
                  : "حفظ العنوان"}
            </button>

            <button
              type="button"
              disabled={busy}
              onClick={cancelEdit}
              className={secondaryClass}
            >
              إلغاء
            </button>
          </div>
        </form>
      )}
    </section>
  );
}