import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import {
  ArrowRight,
  Package,
  CreditCard,
  Landmark,
  Wallet,
  ChevronDown,
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
  getCheckoutShippingMethods,
  submitCustomerCheckout,
  previewCheckoutCoupon,
  type CheckoutCouponQuote,
  type CheckoutShippingMethod,
} from "../data/customerCheckoutApi";

import { listManualMethods, selectManualMethod, type ManualMethodSummary } from "../data/manualCheckoutApi";

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
        const [cartResult, shippingResult, paymentResult] = await Promise.all([
          getCustomerCart(storeSlug),
          getCheckoutShippingMethods(storeSlug),
          listManualMethods(storeSlug),
        ]);

        if (!active) return;

        const availablePickup = shippingResult.filter(
          (method) =>
            method.isEnabled &&
            method.type.toLowerCase() === "pickup" &&
            method.currency === cartResult?.currency &&
            (method.minimumOrderAmount == null ||
              (cartResult?.totalAmount ?? 0) >= method.minimumOrderAmount) &&
            (method.maximumOrderAmount == null ||
              (cartResult?.totalAmount ?? 0) <= method.maximumOrderAmount),
        );

        setManualMethods(paymentResult);
        setPaymentChoice("");
        setCart(cartResult);
        setMethods(availablePickup);
        setMethodId(availablePickup[0]?.id ?? "");
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

  async function applyCoupon() {
    if (submitting || couponBusy || !cart?.items.length || !methodId) return;
    const code = couponCode.trim().toUpperCase();
    setCouponError("");
    setAppliedCoupon(null);
    if (!/^[A-Z0-9][A-Z0-9_-]{1,59}$/.test(code)) {
      setCouponError("أدخل رمز كوبون صالحًا (2–60 حرفًا أو رقمًا).");
      return;
    }
    setCouponBusy(true);
    try {
      const quote = await previewCheckoutCoupon(storeSlug, methodId, code);
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
      !methods.some((method) => method.id === methodId)
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
      const result = await submitCustomerCheckout(
        storeSlug,
        methodId,
        requestKey.current,
        appliedCoupon?.couponCode ?? null,
      );

      // The order exists and is UNPAID. A failed selection must NOT send the
      // customer back to checkout: retry only on this existing order's payment page.
      let selectionError: string | undefined;
      try {
        await selectManualMethod(storeSlug, result.orderId, paymentChoice, normalizedPhone);
      } catch (caught) {
        selectionError = caught instanceof Error ? caught.message : "تعذر تثبيت وسيلة الدفع للطلب.";
      }
      window.dispatchEvent(new CustomEvent("rukn:cart-updated", { detail: { storeSlug } }));
      navigate(`${base}/orders/${result.orderId}/payment`, {
        state: { accountId: paymentChoice, customerPhone: normalizedPhone, selectionError },
        replace: true,
      });
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
        <Link
          to={`${base}/cart`}
          className="mb-8 inline-flex items-center gap-2 text-sm text-[#53685a]"
        >
          <ArrowRight size={17} />
          العودة إلى السلة
        </Link>

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
                راجع تفاصيل طلبك واختر طريقة الاستلام والدفع المناسبة.
              </p>
            </header>

            <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
              <div className="space-y-5">
                <section className="rounded-2xl border border-[#e7eae5] bg-white p-6">
                  <h2 className="mb-4 text-lg font-semibold">
                    طريقة الاستلام
                  </h2>

                  {methods.length === 0 ? (
                    <p className="rounded-xl bg-[#f7f5ef] p-4 text-sm leading-7 text-[#7a6040]">
                      لم يفعّل المتجر طريقة استلام مناسبة لهذا الطلب.
                      لن يتم إنشاء الطلب قبل توفير طريقة استلام صحيحة.
                    </p>
                  ) : (
                    <div className="space-y-3">
                      {methods.map((method) => (
                        <label
                          key={method.id}
                          className="flex cursor-pointer items-center gap-3 rounded-xl border border-[#e4e9e2] p-4"
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

                          <span className="flex-1 text-sm font-medium">
                            {method.name}
                          </span>

                          <span className="text-sm">
                            {money(method.price, method.currency)}
                          </span>
                        </label>
                      ))}
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
                    <button type="button" disabled={!couponCode.trim() || couponBusy || submitting || !methodId}
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
                  disabled={!methodId || submitting || couponBusy || (Boolean(couponCode.trim()) && !appliedCoupon) || !paymentChoice || !manualMethods.some(method => method.id === paymentChoice) || contactPhone.trim().length < 7}
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