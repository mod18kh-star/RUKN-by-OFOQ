import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import {
  ArrowRight,
  Package,
  CreditCard,
  Landmark,
  Wallet,
  ChevronDown,
  MapPin,
} from "lucide-react";

import {
  getAccessToken,
  getCurrentUser,
} from "../../features/auth/authSession";

import {
  getCustomerCart,
  type CustomerCart,
} from "../data/customerCartApi";

import {
  createCheckoutAddress,
  getCheckoutAddresses,
  getCheckoutShippingMethods,
  previewCheckoutCoupon,
  type CheckoutCouponQuote,
  type CheckoutShippingMethod,
  type CustomerCheckoutAddress,
  type CustomerCheckoutAddressInput,
} from "../data/customerCheckoutApi";

import { listManualMethods, type ManualMethodSummary } from "../data/manualCheckoutApi";
import { saveManualCheckoutDraft } from "../data/customerCheckoutDraft";
import { StorefrontPageBrand } from "../components/StorefrontPageBrand";

function money(value: number, currency: string | null) {
  const amount = new Intl.NumberFormat("en-US", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value);
  return `${amount} ${currency ?? ""}`.trim();
}

// The customer should see clear Arabic messages, not untranslated API strings or broken text.
function couponErrorInArabic(caught: unknown): string {
  const raw = caught instanceof Error ? caught.message.trim() : "";
  const normalized = raw.toLowerCase();
  if (/invalid,? disabled|active date range|expired|not active|not found|invalid coupon/.test(normalized)) {
    return "رمز الخصم غير صالح أو انتهت صلاحيته. تحقق من الرمز وحاول مجددًا.";
  }
  if (/currency|عملة/.test(normalized)) {
    return "لا يمكن استخدام هذا الكوبون مع عملة طلبك.";
  }
  if (/usage|limit|exhaust|already used|maximum.*use/.test(normalized)) {
    return "وصل هذا الكوبون إلى الحد المسموح لاستخدامه.";
  }
  if (/minimum|minimum order|min.?order/.test(normalized)) {
    return "لم يصل طلبك إلى الحد الأدنى المطلوب لاستخدام هذا الكوبون.";
  }
  if (/not eligible|not applicable|not included|product|category|scope/.test(normalized)) {
    return "هذا الكوبون لا يشمل المنتجات الموجودة في سلتك.";
  }
  if (/401|403|unauthorized|forbidden/.test(normalized)) {
    return "يرجى تسجيل الدخول مجددًا لتطبيق الكوبون.";
  }
  if (/404|not found|fetch|network|connection/.test(normalized)) {
    return "تعذر التحقق من الكوبون الآن. حاول مرة أخرى بعد قليل.";
  }
  // Accept an intelligible Arabic error, but never surface a corrupted or English-only fallback.
  if (/^[\u0600-\u06ff\s\d.,،:؛()٪%+\-–«»!?]+$/.test(raw) && /[\u0621-\u064a]{3}/.test(raw)) {
    return raw;
  }
  return "تعذر تطبيق الكوبون. تحقق من الرمز وشروط الخصم وحاول مجددًا.";
}

export function StorefrontCheckoutPage() {
  const { storeSlug = "" } = useParams<{
    storeSlug: string;
  }>();

  const base = `/store/${encodeURIComponent(storeSlug)}`;
  const navigate = useNavigate();
  const [manualMethods, setManualMethods] = useState<ManualMethodSummary[]>([]);
  const [paymentChoice, setPaymentChoice] = useState("");
  const [paymentExpanded, setPaymentExpanded] = useState(false);
  const [contactPhone, setContactPhone] = useState("");

  const [cart, setCart] = useState<CustomerCart | null>(null);
  const [methods, setMethods] = useState<CheckoutShippingMethod[]>([]);
  const [methodId, setMethodId] = useState("");
  const [addresses, setAddresses] = useState<CustomerCheckoutAddress[]>([]);
  const [addressId, setAddressId] = useState("");

  const [addressEditorOpen, setAddressEditorOpen] = useState(false);
  const [addressSaving, setAddressSaving] = useState(false);
  const [addressError, setAddressError] = useState("");

  const [deliveryMapUrl, setDeliveryMapUrl] = useState("");
  const [deliveryLatitude, setDeliveryLatitude] =
    useState<number | null>(null);
  const [deliveryLongitude, setDeliveryLongitude] =
    useState<number | null>(null);
  const [deliveryAccuracyMeters, setDeliveryAccuracyMeters] =
    useState<number | null>(null);
  const [locatingAddress, setLocatingAddress] = useState(false);

  const [addressForm, setAddressForm] =
    useState<CustomerCheckoutAddressInput>({
      label: "موقع التوصيل",
      recipientName: "",
      phone: "",
      countryCode: "",
      region: "",
      city: "",
      postalCode: "",
      line1: "",
      line2: "",
      isDefault: false,
    });

  const [couponCode, setCouponCode] = useState("");
  const [appliedCoupon, setAppliedCoupon] = useState<CheckoutCouponQuote | null>(null);
  const [couponBusy, setCouponBusy] = useState(false);
  const [couponError, setCouponError] = useState("");

  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const requestKey = useRef<string | null>(null);
  const phoneEditedByCustomer = useRef(false);

  useEffect(() => {
    let active = true;

    async function load() {
      if (!getAccessToken()) {
        setLoading(false);
        return;
      }

      try {
        const [
          cartResult,
          shippingResult,
          paymentResult,
          addressResult,
        ] = await Promise.all([
          getCustomerCart(storeSlug),
          getCheckoutShippingMethods(storeSlug),
          listManualMethods(storeSlug),
          getCheckoutAddresses(storeSlug),
        ]);

        if (!active) return;

        const availableMethods = shippingResult.filter(
          (method) =>
            method.isEnabled &&
            method.currency === cartResult?.currency &&
            (method.minimumOrderAmount == null ||
              (cartResult?.totalAmount ?? 0) >= method.minimumOrderAmount) &&
            (method.maximumOrderAmount == null ||
              (cartResult?.totalAmount ?? 0) <= method.maximumOrderAmount),
        );

        const activeAddresses = addressResult.filter(
          (address) => address.isActive,
        );

        const preferredAddress =
          activeAddresses.find((address) => address.isDefault) ??
          activeAddresses[0] ??
          null;

        setManualMethods(paymentResult);
        setPaymentChoice("");
        setCart(cartResult);
        setMethods(availableMethods);
        setMethodId(availableMethods[0]?.id ?? "");
        setAddresses(activeAddresses);
        setAddressId(preferredAddress?.addressId ?? "");
      } catch (caught) {
        if (active) {
          setError(
            caught instanceof Error
              ? caught.message
              : "تعذر تحميل معلومات إتمام الطلب.",
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
  }, [storeSlug]);

  // Read only the signed-in customer's own profile; never infer a phone from a JWT or another account.
  // Loading is asynchronous, so an explicitly entered number always takes priority.
  useEffect(() => {
    if (!getAccessToken()) return;
    let active = true;
    void getCurrentUser().then(user => {
      const registeredPhone = user.phoneNumber?.trim();
      if (active && registeredPhone && !phoneEditedByCustomer.current) {
        setContactPhone(previous => previous.trim() ? previous : registeredPhone);
      }
    }).catch(() => {
      // A missing profile phone is not a checkout error: let the customer type one.
    });
    return () => { active = false; };
  }, []);

  const selectedMethod =
    methods.find((method) => method.id === methodId) ?? null;

  const deliveryRequiresAddress =
    selectedMethod !== null &&
    selectedMethod.type.toLowerCase() !== "pickup";

  const selectedAddress =
    addresses.find((address) => address.addressId === addressId) ?? null;

  function patchAddress<
    K extends keyof CustomerCheckoutAddressInput
  >(
    key: K,
    value: CustomerCheckoutAddressInput[K],
  ) {
    setAddressForm((current) => ({
      ...current,
      [key]: value,
    }));
  }

  function detectDeliveryLocation() {
    if (
      locatingAddress ||
      addressSaving
    ) {
      return;
    }

    if (!navigator.geolocation) {
      setAddressError(
        "تحديد الموقع غير مدعوم في هذا المتصفح. يمكنك لصق رابط Google Maps يدويًا.",
      );

      return;
    }

    if (!window.isSecureContext) {
      setAddressError(
        "تحديد الموقع يحتاج HTTPS أو التشغيل على localhost.",
      );

      return;
    }

    setLocatingAddress(true);
    setAddressError("");

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const latitude =
          Number(
            position.coords.latitude.toFixed(6),
          );

        const longitude =
          Number(
            position.coords.longitude.toFixed(6),
          );

        const accuracyMeters =
          Number.isFinite(
            position.coords.accuracy,
          )
            ? Math.max(
                0,
                Math.round(
                  position.coords.accuracy,
                ),
              )
            : null;

        const url =
          `https://www.google.com/maps/search/?api=1&query=${latitude},${longitude}`;

        setDeliveryLatitude(
          latitude,
        );

        setDeliveryLongitude(
          longitude,
        );

        setDeliveryAccuracyMeters(
          accuracyMeters,
        );

        setDeliveryMapUrl(
          url,
        );

        setLocatingAddress(
          false,
        );

        setAddressError(
          "",
        );
      },
      () => {
        setLocatingAddress(
          false,
        );

        setAddressError(
          "تعذر تحديد موقعك. اسمح للمتصفح باستخدام الموقع أو الصق رابط Google Maps يدويًا.",
        );
      },
      {
        enableHighAccuracy: true,
        timeout: 15000,
        maximumAge: 60000,
      },
    );
  }

  async function saveDeliveryAddress() {
    if (addressSaving) {
      return;
    }

    let normalizedMapUrl = "";

    if (deliveryMapUrl.trim()) {
      try {
        const parsed =
          new URL(
            deliveryMapUrl.trim(),
          );

        const host =
          parsed.hostname.toLowerCase();

        const googleMapsUrl =
          parsed.protocol === "https:" &&
          !parsed.username &&
          !parsed.password &&
          (
            host === "maps.app.goo.gl" ||
            host === "maps.google.com" ||
            (
              host === "goo.gl" &&
              parsed.pathname.startsWith(
                "/maps",
              )
            ) ||
            (
              (
                host === "google.com" ||
                host === "www.google.com" ||
                host.endsWith(".google.com")
              ) &&
              parsed.pathname.startsWith(
                "/maps",
              )
            )
          );

        if (!googleMapsUrl) {
          throw new Error(
            "INVALID_MAP_URL",
          );
        }

        normalizedMapUrl =
          parsed.toString();
      } catch {
        setAddressError(
          "ألصق رابط Google Maps صحيحًا، أو استخدم زر «حدد موقعي الحالي».",
        );

        return;
      }
    }

    const hasCoordinates =
      deliveryLatitude !== null &&
      deliveryLongitude !== null;

    if (
      !normalizedMapUrl &&
      hasCoordinates
    ) {
      normalizedMapUrl =
        `https://www.google.com/maps/search/?api=1&query=${deliveryLatitude},${deliveryLongitude}`;
    }

    if (
      !hasCoordinates &&
      !normalizedMapUrl
    ) {
      setAddressError(
        "حدد موقع التوصيل أولًا أو ألصق رابط Google Maps.",
      );

      return;
    }

    if (
      normalizedMapUrl.length >
      240
    ) {
      setAddressError(
        "رابط الموقع طويل جدًا. من Google Maps استخدم «مشاركة» ثم انسخ رابط المشاركة المختصر.",
      );

      return;
    }

    const deliveryNotes =
      addressForm.line2?.trim() ||
      null;

    const input: CustomerCheckoutAddressInput = {
      label:
        addressForm.label.trim() ||
        "موقع التوصيل",

      recipientName:
        addressForm.recipientName.trim(),

      phone:
        addressForm.phone.trim(),

      countryCode: "",
      region: null,
      city: "",
      postalCode: null,

      line1:
        normalizedMapUrl,

      line2:
        deliveryNotes,

      latitude:
        deliveryLatitude,

      longitude:
        deliveryLongitude,

      accuracyMeters:
        deliveryAccuracyMeters,

      mapUrl:
        normalizedMapUrl,

      deliveryNotes,

      isDefault:
        addresses.length === 0,
    };

    if (!input.recipientName) {
      setAddressError(
        "أدخل اسم المستلم.",
      );

      return;
    }

    if (
      input.phone.length < 7 ||
      input.phone.length > 40 ||
      (input.phone.match(/\d/g)?.length ?? 0) < 7 ||
      !/^[+\d()\-\s]+$/.test(input.phone)
    ) {
      setAddressError(
        "أدخل رقم هاتف صحيحًا للمستلم.",
      );

      return;
    }

    setAddressSaving(true);
    setAddressError("");

    try {
      const created =
        await createCheckoutAddress(
          storeSlug,
          input,
        );

      setAddresses((current) => {
        const normalized =
          created.isDefault
            ? current.map((address) => ({
                ...address,
                isDefault: false,
              }))
            : current;

        return [
          created,
          ...normalized,
        ];
      });

      setAddressId(
        created.addressId,
      );

      setAddressEditorOpen(
        false,
      );

      setAddressForm({
        label: "موقع التوصيل",
        recipientName: "",
        phone: "",
        countryCode: "",
        region: "",
        city: "",
        postalCode: "",
        line1: "",
        line2: "",
        isDefault: false,
      });

      setDeliveryLatitude(
        null,
      );

      setDeliveryLongitude(
        null,
      );

      setDeliveryAccuracyMeters(
        null,
      );

      setDeliveryMapUrl("");

      requestKey.current =
        null;

      setAppliedCoupon(
        null,
      );

      setCouponError(
        "",
      );
    } catch (caught) {
      setAddressError(
        caught instanceof Error
          ? caught.message
          : "تعذر حفظ موقع التوصيل.",
      );
    } finally {
      setAddressSaving(
        false,
      );
    }
  }

  async function applyCoupon() {
    if (
      submitting ||
      couponBusy ||
      !cart?.items.length ||
      !methodId ||
      !selectedMethod ||
      (deliveryRequiresAddress && !selectedAddress)
    ) return;
    const code = couponCode.trim().toUpperCase();
    setCouponError("");
    setAppliedCoupon(null);
    if (!/^[A-Z0-9][A-Z0-9_-]{1,59}$/.test(code)) {
      setCouponError("أدخل رمز كوبون صالحًا (2–60 حرفًا أو رقمًا).");
      return;
    }
    setCouponBusy(true);
    try {
      const quote = await previewCheckoutCoupon(
        storeSlug,
        methodId,
        code,
        deliveryRequiresAddress ? selectedAddress?.addressId ?? null : null,
      );
      // A preview is not a reservation. The server will validate again at checkout.
      if (quote.currency !== cart.currency || quote.couponCode !== code ||
          !Number.isFinite(quote.discountAmount) || quote.discountAmount <= 0 ||
          !Number.isFinite(quote.totalAmount) || quote.totalAmount <= 0) {
        throw new Error("تعذر اعتماد هذا الكوبون لطلبك. تحقق من عملة المتجر وقيمة الطلب.");
      }
      setCouponCode(code);
      setAppliedCoupon(quote);
      requestKey.current = null;
    } catch (caught) {
      setCouponError(couponErrorInArabic(caught));
    } finally {
      setCouponBusy(false);
    }
  }

  async function confirmOrder() {
    if (
      submitting ||
      !cart?.items.length ||
      !methodId ||
      !selectedMethod ||
      !methods.some((method) => method.id === methodId) ||
      (deliveryRequiresAddress && !selectedAddress)
    ) {
      return;
    }

    // Never silently convert checkout into an order without an explicit payment choice.
    if (couponCode.trim() && (!appliedCoupon || appliedCoupon.couponCode !== couponCode.trim().toUpperCase())) {
      setCouponError("اضغط «تطبيق الكوبون» أولًا، أو امسح الرمز للمتابعة دون خصم.");
      return;
    }
    if (couponBusy) return;
    if (!paymentChoice || !manualMethods.some(method => method.id === paymentChoice)) {
      setPaymentExpanded(true);
      setError("اختر طريقة الدفع المفعّلة قبل المتابعة. إذا لم تظهر خيارات، فعلى التاجر تفعيل حساب الدفع أولًا.");
      return;
    }
    const normalizedPhone = contactPhone.trim();
    if (normalizedPhone.length < 7 || normalizedPhone.length > 40 ||
        (normalizedPhone.match(/\d/g)?.length ?? 0) < 7 ||
        !/^[+\d()\-\s]+$/.test(normalizedPhone)) {
      setPaymentExpanded(true);
      setError("أدخل رقم تواصل صحيحًا لإرسال تفاصيل مراجعة الدفع عند الحاجة.");
      return;
    }

    if (!requestKey.current) {
      requestKey.current = crypto.randomUUID();
    }

    setSubmitting(true);
    setError(null);

    try {
      // No Order is created when the customer opens manual-payment details.
      // The cart remains editable; the draft is scoped to this signed-in user/store.
      const selectedShipping = methods.find(method => method.id === methodId);
      if (!selectedShipping || !cart?.currency) throw new Error("تعذر تحديد الشحن أو عملة السلة.");

      const customerAddressId =
        selectedShipping.type.toLowerCase() === "pickup"
          ? null
          : selectedAddress?.addressId ?? null;

      if (
        selectedShipping.type.toLowerCase() !== "pickup" &&
        !customerAddressId
      ) {
        throw new Error("اختر عنوان توصيل صالحًا قبل المتابعة.");
      }

      const expectedAmount = Number((appliedCoupon?.totalAmount ?? (cart.totalAmount + selectedShipping.price)).toFixed(2));
      if (expectedAmount <= 0) throw new Error("قيمة الطلب غير صالحة للدفع اليدوي.");
      saveManualCheckoutDraft(storeSlug, {
        cartId: cart.id,
        lines: cart.items.map(item => ({
          productVariantId: item.productVariantId, quantity: item.quantity, unitPrice: item.unitPrice,
        })),
        shippingMethodId: methodId,
        customerAddressId,
        paymentAccountId: paymentChoice,
        customerPhone: normalizedPhone,
        couponCode: appliedCoupon?.couponCode ?? null,
        expectedAmount,
        expectedCurrency: cart.currency,
        idempotencyKey: crypto.randomUUID(),
      });
      navigate(`${base}/orders/draft/payment`, { replace: false });
      return;
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : "تعذر تأكيد الطلب. يمكنك إعادة المحاولة بنفس الطلب.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <main dir="rtl" className="min-h-screen bg-[#faf9f7] p-8">
        <p className="text-sm text-[#657368]">
          جاري تجهيز صفحة إتمام الطلب...
        </p>
      </main>
    );
  }

  return (
    <main
      dir="rtl"
      className="min-h-screen bg-[#faf9f7] px-4 py-8 text-[#21352a] sm:py-14"
    >
      <div className="mx-auto max-w-5xl">
        <div className="mb-8 flex items-center justify-between gap-4">
          <Link
            to={`${base}/cart`}
            className="inline-flex items-center gap-2 text-sm text-[#53685a]"
          >
            <ArrowRight size={17} />
            العودة إلى السلة
          </Link>

          <StorefrontPageBrand
            storeSlug={storeSlug}
            className="text-xs font-semibold tracking-wide text-[#53685a]"
          />
        </div>

        {error && (
          <div
            role="alert"
            className="mb-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
          >
            {error}
          </div>
        )}

        {!getAccessToken() ? (
          <section className="rounded-2xl bg-white p-8 text-center">
            <h1 className="text-xl font-bold">
              سجّل الدخول لإتمام طلبك
            </h1>

            <Link
              to={`${base}/account/login?mode=login&returnTo=${encodeURIComponent(`${base}/checkout`)}`}
              className="mt-6 inline-flex rounded-xl bg-[#193c30] px-6 py-3 text-sm font-semibold"
              style={{ color: "#ffffff" }}
            >
              تسجيل الدخول
            </Link>
          </section>
        ) : !cart?.items.length ? (
          <section className="rounded-2xl bg-white p-8 text-center">
            <Package className="mx-auto mb-4" size={32} />

            <h1 className="text-xl font-bold">
              سلتك فارغة حاليًا
            </h1>

            <Link to={base} className="mt-5 inline-block text-sm underline">
              متابعة التسوق
            </Link>
          </section>
        ) : (
          <>
            <header className="mb-8">
              <p className="text-xs text-[#708176]">
                مشترياتك
              </p>

              <h1 className="mt-2 text-3xl font-bold">
                إتمام الطلب
              </h1>

              <p className="mt-3 text-sm text-[#718176]">
                راجع تفاصيل طلبك واختر طريقة التوصيل أو الاستلام والدفع المناسبة.
              </p>
            </header>

            <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
              <div className="space-y-5">
                <section className="rounded-2xl border border-[#e7eae5] bg-white p-6">
                  <h2 className="mb-4 text-lg font-semibold">
                    طريقة التسليم
                  </h2>

                  {methods.length === 0 ? (
                    <p className="rounded-xl bg-[#f7f5ef] p-4 text-sm leading-7 text-[#7a6040]">
                      لا توجد طريقة توصيل أو استلام مفعّلة ومناسبة لهذا الطلب حاليًا.
                    </p>
                  ) : (
                    <div className="space-y-3">
                      {methods.map((method) => (
                        <label
                          key={method.id}
                          className={`flex cursor-pointer items-center gap-3 rounded-xl border p-4 ${
                            methodId === method.id
                              ? "border-[#315F5B] bg-[#f5faf7]"
                              : "border-[#e4e9e2]"
                          }`}
                        >
                          <input
                            type="radio"
                            name="shippingMethod"
                            value={method.id}
                            checked={methodId === method.id}
                            onChange={() => {
                              setMethodId(method.id);
                              requestKey.current = null;
                              setAppliedCoupon(null);
                              setCouponError("");
                            }}
                          />

                          <span className="flex min-w-0 flex-1 items-center gap-2 text-sm font-medium">
                            {method.type.toLowerCase() === "pickup" ? (
                              <Package size={17} className="shrink-0 text-[#315F5B]" />
                            ) : (
                              <MapPin size={17} className="shrink-0 text-[#315F5B]" />
                            )}

                            <span className="truncate">
                              {method.name}
                            </span>
                          </span>

                          <span className="text-sm">
                            {method.price === 0
                              ? "مجاني"
                              : money(method.price, method.currency)}
                          </span>
                        </label>
                      ))}

                      {deliveryRequiresAddress && (
                        <div className="mt-4 border-t border-[#edf0eb] pt-5">
                          <div className="mb-3 flex items-center gap-2">
                            <MapPin size={18} className="text-[#315F5B]" />

                            <div>
                              <h3 className="text-sm font-semibold">
                                موقع التوصيل
                              </h3>

                              <p className="mt-1 text-xs leading-6 text-[#718176]">
                                اختر موقعًا محفوظًا أو أضف موقع التوصيل الحالي.
                              </p>
                            </div>
                          </div>

                          {addresses.length > 0 ? (
                            <div className="space-y-3">
                              <select
                                value={addressId}
                                onChange={(event) => {
                                  setAddressId(event.target.value);
                                  requestKey.current = null;
                                  setAppliedCoupon(null);
                                  setCouponError("");
                                }}
                                className="h-12 w-full rounded-xl border border-[#e4e9e2] bg-white px-4 text-sm outline-none focus:border-[#315F5B]"
                              >
                                {addresses.map((address) => (
                                  <option
                                    key={address.addressId}
                                    value={address.addressId}
                                  >
                                    {address.label} — {(address.mapUrl || (address.latitude !== null && address.longitude !== null)) ? "موقع محدد على الخريطة" : [address.city, address.line1].filter(Boolean).join(" — ")}
                                  </option>
                                ))}
                              </select>

                              {!addressEditorOpen ? (
                                <button
                                  type="button"
                                  onClick={() => {
                                    setAddressError("");
                                    setAddressEditorOpen(true);
                                  }}
                                  className="text-sm font-semibold text-[#315F5B] underline underline-offset-4"
                                >
                                  + إضافة عنوان توصيل آخر
                                </button>
                              ) : null}
                            </div>
                          ) : !addressEditorOpen ? (
                            <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-[#f7f5ef] px-4 py-3">
                              <div>
                                <p className="text-sm font-medium text-[#6f5638]">
                                  لا يوجد موقع توصيل محفوظ.
                                </p>

                                <p className="mt-1 text-xs leading-6 text-[#8a765d]">
                                  حدد موقعك الآن وسيتم اختياره تلقائيًا لهذا الطلب.
                                </p>
                              </div>

                              <button
                                type="button"
                                onClick={() => {
                                  setAddressError("");
                                  setAddressEditorOpen(true);
                                }}
                                className="min-h-10 rounded-lg border border-[#d8cec0] bg-white px-4 text-sm font-semibold text-[#5f4b34]"
                              >
                                + إضافة عنوان
                              </button>
                            </div>
                          ) : null}

                          {addressEditorOpen ? (
                            <div className="mt-4 border-t border-[#edf0eb] pt-5">
                              <div className="mb-4 flex items-start justify-between gap-4">
                                <div>
                                  <h4 className="text-sm font-semibold">
                                    إضافة موقع التوصيل
                                  </h4>

                                  <p className="mt-1 text-xs leading-6 text-[#718176]">
                                    حدد الموقع وأدخل بيانات المستلم بدون مغادرة صفحة إتمام الطلب.
                                  </p>
                                </div>

                                {addresses.length > 0 ? (
                                  <button
                                    type="button"
                                    disabled={addressSaving}
                                    onClick={() => {
                                      setAddressEditorOpen(false);
                                      setAddressError("");
                                    }}
                                    className="text-xs text-[#718176] underline"
                                  >
                                    إلغاء
                                  </button>
                                ) : null}
                              </div>

                              <section className="mb-5 rounded-xl border border-[#dfe7e1] bg-[#f8faf8] p-4">
                                <div className="flex items-start justify-between gap-4">
                                  <div>
                                    <h4 className="text-sm font-semibold text-[#21352a]">
                                      موقع التوصيل
                                    </h4>

                                    <p className="mt-1 text-xs leading-6 text-[#718176]">
                                      حدد موقعك بضغطة واحدة أو الصق رابط Google Maps. لا تحتاج لكتابة المدينة أو الشارع.
                                    </p>
                                  </div>

                                  <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-white text-[#315F5B]">
                                    <MapPin size={17} />
                                  </span>
                                </div>

                                <div className="mt-4 flex flex-wrap gap-2">
                                  <button
                                    type="button"
                                    disabled={
                                      locatingAddress ||
                                      addressSaving
                                    }
                                    onClick={
                                      detectDeliveryLocation
                                    }
                                    className="min-h-11 rounded-xl bg-[#315F5B] px-5 text-sm font-semibold text-white disabled:opacity-50"
                                  >
                                    {locatingAddress
                                      ? "جاري تحديد موقعك..."
                                      : "حدد موقعي الحالي"}
                                  </button>

                                  <a
                                    href="https://www.google.com/maps"
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="inline-flex min-h-11 items-center justify-center rounded-xl border border-[#dfe7e1] bg-white px-4 text-sm font-medium text-[#315F5B]"
                                  >
                                    فتح Google Maps
                                  </a>
                                </div>

                                <label className="mt-4 block text-sm">
                                  <span className="mb-2 block text-xs font-medium text-[#586a60]">
                                    رابط الموقع من Google Maps
                                  </span>

                                  <input
                                    dir="ltr"
                                    type="url"
                                    value={
                                      deliveryMapUrl
                                    }
                                    disabled={
                                      addressSaving
                                    }
                                    onChange={(event) => {
                                      setDeliveryMapUrl(
                                        event.target.value,
                                      );

                                      setDeliveryLatitude(
                                        null,
                                      );

                                      setDeliveryLongitude(
                                        null,
                                      );

                                      setDeliveryAccuracyMeters(
                                        null,
                                      );

                                      setAddressError("");
                                    }}
                                    placeholder="https://maps.app.goo.gl/..."
                                    className="h-11 w-full rounded-xl border border-[#e4e9e2] bg-white px-3 text-left outline-none focus:border-[#315F5B]"
                                  />
                                </label>

                                {deliveryMapUrl ? (
                                  <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
                                    <span className="text-xs text-[#4f675a]">
                                      {deliveryLatitude !== null &&
                                      deliveryLongitude !== null
                                        ? deliveryAccuracyMeters !== null
                                          ? `تم تحديد موقعك بدقة تقريبية ${deliveryAccuracyMeters} متر.`
                                          : "تم تحديد موقعك."
                                        : "تم حفظ رابط الموقع."}
                                    </span>

                                    <a
                                      href={
                                        deliveryMapUrl
                                      }
                                      target="_blank"
                                      rel="noopener noreferrer"
                                      className="text-xs font-semibold text-[#315F5B] underline underline-offset-4"
                                    >
                                      معاينة الموقع على الخريطة
                                    </a>
                                  </div>
                                ) : null}
                              </section>

                              <div className="mb-4 flex items-center gap-3">
                                <span className="h-px flex-1 bg-[#edf0ed]" />

                                <span className="shrink-0 text-[11px] font-semibold text-[#65766c]">
                                  بيانات المستلم
                                </span>

                                <span className="h-px flex-1 bg-[#edf0ed]" />
                              </div>

                              <div className="grid gap-3 sm:grid-cols-2">
                                <label className="text-sm">
                                  <span className="mb-2 block text-xs font-semibold text-[#4d6256]">
                                    اسم المستلم
                                  </span>

                                  <input
                                    value={addressForm.recipientName}
                                    maxLength={160}
                                    disabled={addressSaving}
                                    autoComplete="name"
                                    onChange={(event) =>
                                      patchAddress(
                                        "recipientName",
                                        event.target.value,
                                      )
                                    }
                                    placeholder="الاسم الكامل"
                                    className="h-11 w-full rounded-xl border border-[#e4e9e2] bg-white px-3 outline-none focus:border-[#315F5B]"
                                  />
                                </label>

                                <label className="text-sm">
                                  <span className="mb-2 block text-xs font-semibold text-[#4d6256]">
                                    رقم الهاتف
                                  </span>

                                  <input
                                    dir="ltr"
                                    type="tel"
                                    value={addressForm.phone}
                                    maxLength={40}
                                    autoComplete="tel"
                                    disabled={addressSaving}
                                    onChange={(event) =>
                                      patchAddress(
                                        "phone",
                                        event.target.value,
                                      )
                                    }
                                    placeholder="+9665XXXXXXXX"
                                    className="h-11 w-full rounded-xl border border-[#e4e9e2] bg-white px-3 text-left outline-none focus:border-[#315F5B]"
                                  />
                                </label>

                                <label className="text-sm sm:col-span-2">
                                  <span className="mb-2 block text-xs font-semibold text-[#4d6256]">
                                    ملاحظة للمندوب
                                    <span className="mr-1 font-normal text-[#87938c]">
                                      اختياري
                                    </span>
                                  </span>

                                  <input
                                    value={addressForm.line2 ?? ""}
                                    maxLength={240}
                                    disabled={addressSaving}
                                    onChange={(event) =>
                                      patchAddress(
                                        "line2",
                                        event.target.value,
                                      )
                                    }
                                    placeholder="مثال: اتصل قبل الوصول، المدخل الخلفي..."
                                    className="h-11 w-full rounded-xl border border-[#e4e9e2] bg-white px-3 outline-none focus:border-[#315F5B]"
                                  />
                                </label>
                              </div>
                              {addressError ? (
                                <p
                                  role="alert"
                                  className="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800"
                                >
                                  {addressError}
                                </p>
                              ) : null}

                              <div className="mt-4 flex flex-wrap items-center gap-2">
                                <button
                                  type="button"
                                  disabled={addressSaving}
                                  onClick={() =>
                                    void saveDeliveryAddress()
                                  }
                                  className="min-h-11 rounded-xl bg-[#315F5B] px-5 text-sm font-semibold text-white disabled:opacity-50"
                                >
                                  {addressSaving
                                    ? "جاري حفظ الموقع..."
                                    : "حفظ واستخدام هذا الموقع"}
                                </button>

                                {addresses.length > 0 ? (
                                  <button
                                    type="button"
                                    disabled={addressSaving}
                                    onClick={() => {
                                      setAddressEditorOpen(false);
                                      setAddressError("");
                                    }}
                                    className="min-h-11 rounded-xl border border-[#e4e9e2] px-4 text-sm disabled:opacity-50"
                                  >
                                    إلغاء
                                  </button>
                                ) : null}
                              </div>
                            </div>
                          ) : null}
                        </div>
                      )}
                    </div>
                  )}
                </section>

                <section className="rounded-2xl border border-[#e7eae5] bg-white p-6">
                  <button
                    type="button"
                    aria-expanded={paymentExpanded}
                    aria-controls="rukn-checkout-payment-options"
                    onClick={() => setPaymentExpanded(value => !value)}
                    className="flex min-h-12 w-full items-center justify-between gap-3 text-right"
                  >
                    <span className="flex items-center gap-3">
                      <CreditCard size={22} className="text-[#315F5B]" />
                      <span><strong className="block text-lg">طريقة الدفع</strong>
                        <small className="mt-1 block text-xs font-normal text-[#718176]">
                          {paymentChoice ? manualMethods.find(method => method.id === paymentChoice)?.name : "اضغط هنا لعرض طرق الدفع واختيار إحداها"}
                        </small>
                      </span>
                    </span>
                    <ChevronDown size={18} className={paymentExpanded ? "rotate-180" : ""} aria-hidden="true" />
                  </button>
                  <div id="rukn-checkout-payment-options" hidden={!paymentExpanded} className="mt-4 space-y-3 border-t border-[#edf0eb] pt-4">
                    {manualMethods.length === 0 ? (
                      <p role="status" className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm leading-7 text-amber-900">
                        لا تتوفر طرق دفع حاليًا. يرجى التواصل مع المتجر.
                      </p>
                    ) : (
                      <>
                        <p className="text-xs leading-6 text-[#718176]">اختر حساب الدفع المناسب، ثم أكمل طلبك.</p>
                        {manualMethods.map(method => (
                          <label key={method.id}
                            className={`flex cursor-pointer items-center gap-3 rounded-xl border p-4 text-sm transition-colors ${paymentChoice === method.id ? "border-[#315F5B] bg-[#f3faf7]" : "border-[#e4e9e2]"}`}>
                            <input type="radio" name="checkoutPaymentMethod" value={method.id}
                              checked={paymentChoice === method.id} onChange={() => setPaymentChoice(method.id)} />
                            {method.kind === "bank" ? <Landmark size={19} className="shrink-0 text-[#315F5B]" /> : <Wallet size={19} className="shrink-0 text-[#315F5B]" />}
                            <span className="min-w-0 font-medium">{method.name}
                              <small className="mt-1 block break-all text-xs font-normal text-black/50" dir="auto">{method.maskedReference}</small>
                            </span>
                          </label>
                        ))}
                        <label className="block text-sm">رقم هاتف العميل للتواصل بشأن الطلب
                          <input type="tel" dir="ltr" value={contactPhone} maxLength={40} autoComplete="tel"
                            onChange={event => { phoneEditedByCustomer.current = true; setContactPhone(event.target.value); }} placeholder="+9665XXXXXXXX"
                            className="mt-2 block h-12 w-full rounded-xl border border-[#e4e9e2] px-4" />
                        </label>
                      </>
                    )}
                  </div>
                </section>
                <section className="rounded-2xl border border-[#e7eae5] bg-white p-6">
                  <h2 className="mb-4 text-lg font-semibold">كوبون الخصم</h2>
                  <p className="mb-3 text-xs leading-6 text-[#718176]">
                    عندك كوبون خصم؟ أدخل الرمز واستفد من العرض.
                  </p>
                  <div className="flex flex-wrap items-end gap-2">
                    <label className="min-w-[170px] flex-1 space-y-2 text-sm">
                      <span className="font-medium">رمز الكوبون</span>
                      <input dir="ltr" type="text" autoComplete="off" maxLength={60}
                        value={couponCode} disabled={submitting || couponBusy}
                        placeholder="مثال: DA200"
                        onChange={event => {
                          setCouponCode(event.target.value.toUpperCase().trimStart());
                          setAppliedCoupon(null);
                          setCouponError("");
                          requestKey.current = null;
                        }}
                        onKeyDown={event => {
                          if (event.key === "Enter") {
                            event.preventDefault();
                            void applyCoupon();
                          }
                        }}
                        className="block min-h-12 w-full rounded-xl border border-[#e4e9e2] px-4 text-left" />
                    </label>
                    <button type="button" disabled={!couponCode.trim() || couponBusy || submitting || !methodId || (deliveryRequiresAddress && !selectedAddress)}
                      onClick={() => void applyCoupon()}
                      className="min-h-12 rounded-xl bg-[#315F5B] px-5 text-sm font-semibold text-white disabled:opacity-50">
                      {couponBusy ? "جارٍ التحقق…" : "تطبيق الكوبون"}
                    </button>
                    {(couponCode || appliedCoupon) && <button type="button" disabled={submitting || couponBusy}
                      onClick={() => {setCouponCode(""); setAppliedCoupon(null); setCouponError(""); requestKey.current = null;}}
                      className="min-h-12 rounded-xl border border-[#e4e9e2] px-4 text-sm">إزالة</button>}
                  </div>
                  {couponError && <p role="alert" className="mt-3 rounded-xl bg-red-50 p-3 text-sm text-red-800">{couponError}</p>}
                  {appliedCoupon && <p role="status" className="mt-3 rounded-xl bg-[#eef8f2] p-3 text-sm text-[#24553e]">
                    تم تطبيق الكوبون {appliedCoupon.couponCode} — وفّرت <bdi dir="ltr">{money(appliedCoupon.discountAmount, appliedCoupon.currency)}</bdi>.
                  </p>}
                </section>
                <section className="rounded-2xl border border-[#e7eae5] bg-white p-6">
                  <h2 className="mb-4 text-lg font-semibold">
                    المنتجات المطلوبة
                  </h2>

                  <div className="space-y-4">
                    {cart.items.map((item) => (
                      <div
                        key={item.id}
                        className="flex items-center gap-4 border-b border-[#edf0eb] pb-4 last:border-0"
                      >
                        <div className="flex size-16 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-[#f7f8f6] p-2">
                          {item.primaryImageUrl ? (
                            <img
                              src={item.primaryImageUrl}
                              alt={item.productName ?? "صورة المنتج"}
                              className="h-full w-full object-contain"
                            />
                          ) : (
                            <Package size={22} />
                          )}
                        </div>

                        <div className="min-w-0 flex-1">
                          <p className="text-sm font-semibold">
                            {item.productName ?? "منتج"}
                          </p>

                          {item.variantName && (
                            <p className="mt-1 text-xs text-[#718176]">
                              {item.variantName}
                            </p>
                          )}

                          <p className="mt-1 text-xs text-[#718176]">
                            الكمية: {item.quantity}
                          </p>
                        </div>

                        <span className="text-sm font-semibold">
                          {money(item.lineTotal, item.currency)}
                        </span>
                      </div>
                    ))}
                  </div>
                </section>
              </div>

              <aside className="rounded-2xl border border-[#e7eae5] bg-white p-6 lg:sticky lg:top-6">
                <h2 className="text-lg font-semibold">
                  ملخص الطلب
                </h2>

                <div className="mt-6 flex justify-between text-sm">
                  <span>عدد المنتجات</span>
                  <span>{cart.totalQuantity}</span>
                </div>

                <div className="mt-4 flex justify-between text-sm">
                  <span>إجمالي أسعار المنتجات</span>
                  <bdi dir="ltr">{money(cart.totalAmount, cart.currency)}</bdi>
                </div>

                {methodId && (
                  <div className="mt-4 flex justify-between text-sm">
                    <span>{methods.find((method) => method.id === methodId)?.type.toLowerCase() === "pickup" ? "رسوم الاستلام" : "رسوم التوصيل"}</span>
                    <bdi dir="ltr">{money(methods.find((method) => method.id === methodId)?.price ?? 0, cart.currency)}</bdi>
                  </div>
                )}

                {appliedCoupon && <div className="mt-4 flex justify-between gap-3 text-sm font-semibold text-[#24553e]">
                  <span>خصم الكوبون ({appliedCoupon.couponCode})</span>
                  <bdi dir="ltr">− {money(appliedCoupon.discountAmount, appliedCoupon.currency)}</bdi>
                </div>}
                <div className="mt-4 flex justify-between border-t border-[#e7eae5] pt-4 text-base font-bold">
                  <span>{appliedCoupon ? "المبلغ الإجمالي بعد الخصم" : "المبلغ الإجمالي"}</span>
                  <bdi dir="ltr">{money(appliedCoupon?.totalAmount ?? (cart.totalAmount + (methods.find(m => m.id === methodId)?.price ?? 0)), cart.currency)}</bdi>
                </div>

                <button
                  type="button"
                  onClick={() => void confirmOrder()}
                  disabled={!methodId || submitting || couponBusy || (Boolean(couponCode.trim()) && !appliedCoupon) || !paymentChoice || !manualMethods.some(method => method.id === paymentChoice) || contactPhone.trim().length < 7 || (deliveryRequiresAddress && !selectedAddress)}
                  className="mt-5 flex min-h-12 w-full items-center justify-center gap-2 rounded-xl bg-[#193c30] px-5 text-sm font-semibold disabled:cursor-not-allowed disabled:opacity-50"
                  style={{ color: "#ffffff" }}
                >
                  {submitting ? "جاري إنشاء الطلب..." : "المتابعة إلى الدفع"}
                </button>

              </aside>
            </div>
          </>
        )}
      </div>
    </main>
  );
}