import {
  useCallback,
  useEffect,
  useState,
} from "react";

import {
  Link,
  useParams,
} from "react-router";

import {
  ArrowRight,
  Minus,
  Plus,
  ShoppingBag,
  Trash2,
} from "lucide-react";

import {
  getAccessToken,
} from "../../features/auth/authSession";

import {
  getCustomerCart,
  removeCustomerCartItem,
  updateCustomerCartQuantity,
  type CustomerCart,
  type CustomerCartItem,
} from "../data/customerCartApi";

function formatAmount(
  amount: number,
  currency: string | null,
) {
  return `${amount.toLocaleString("ar", {
    maximumFractionDigits: 2,
  })} ${currency ?? ""}`;
}

function errorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }

  return "تعذر إتمام العملية. حاول مجددًا.";
}

function comparisonSaving(item: CustomerCartItem): number {
  const previous = item.compareAtPrice;

  if (
    previous == null ||
    !Number.isFinite(previous) ||
    previous <= item.unitPrice
  ) {
    return 0;
  }

  return previous - item.unitPrice;
}
export function StorefrontCartPage() {
  const { storeSlug = "" } = useParams<{
    storeSlug: string;
  }>();


  const storePath =
    `/store/${encodeURIComponent(storeSlug)}`;

  const [cart, setCart] =
    useState<CustomerCart | null>(null);

  const [loading, setLoading] =
    useState(true);

  const [busyId, setBusyId] =
    useState<string | null>(null);

  const [error, setError] =
    useState<string | null>(null);

  const [authenticated, setAuthenticated] =
    useState(() => Boolean(getAccessToken()));

  const loginPath =
    `${storePath}/account/login?mode=login&returnTo=${encodeURIComponent(
      `${storePath}/cart`,
    )}`;

  const refresh = useCallback(async () => {
    const result = await getCustomerCart(storeSlug);

    setCart(result);
  }, [storeSlug]);

  useEffect(() => {
    let active = true;

    async function load() {
      if (!getAccessToken()) {
        setAuthenticated(false);
        setLoading(false);
        return;
      }

      setAuthenticated(true);
      setLoading(true);
      setError(null);

      try {
        const result = await getCustomerCart(storeSlug);

        if (active) {
          setCart(result);
        }
      } catch (caught) {
        if (active) {
          setError(errorMessage(caught));
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
  }, [storeSlug]);

  async function changeQuantity(
    item: CustomerCartItem,
    quantity: number,
  ) {
    if (busyId) return;

    if (quantity <= 0) {
      await removeItem(item);
      return;
    }

    setBusyId(item.id);
    setError(null);

    try {
      await updateCustomerCartQuantity(
        storeSlug,
        item.id,
        quantity,
      );

      setCart(await getCustomerCart(storeSlug));

      window.dispatchEvent(
        new CustomEvent("rukn:cart-updated", {
          detail: { storeSlug },
        }),
      );
    } catch (caught) {
      setError(errorMessage(caught));
    } finally {
      setBusyId(null);
    }
  }

  async function removeItem(
    item: CustomerCartItem,
  ) {
    if (busyId) return;

    setBusyId(item.id);
    setError(null);

    try {
      await removeCustomerCartItem(
        storeSlug,
        item.id,
      );

      setCart(await getCustomerCart(storeSlug));

      window.dispatchEvent(
        new CustomEvent("rukn:cart-updated", {
          detail: { storeSlug },
        }),
      );
    } catch (caught) {
      setError(errorMessage(caught));
    } finally {
      setBusyId(null);
    }
  }

  const items = cart?.items ?? [];

  const productSavings = items.reduce(
    (total, item) =>
      total + comparisonSaving(item) * item.quantity,
    0,
  );

  return (
    <main
      dir="rtl"
      className="min-h-screen bg-[#f8f8f6] px-4 py-10 text-[#23362c] sm:py-16"
    >
      <div className="mx-auto max-w-4xl">

        <div className="mb-8 flex items-center justify-between gap-4">
          <Link
            to={storePath}
            className="inline-flex items-center gap-2 text-sm font-medium text-[#53685a]"
          >
            <ArrowRight size={17} />
            العودة إلى المتجر
          </Link>

          <span className="text-xs font-semibold tracking-widest">
            RUKN
          </span>
        </div>

        <header className="mb-8">
          <p className="mb-2 text-xs text-[#718176]">
            مشترياتك
          </p>

          <h1 className="text-3xl font-semibold">
            سلة التسوق
          </h1>

          <p className="mt-3 text-sm leading-7 text-[#718176]">
            راجع المنتجات والكميات قبل إتمام طلبك.
          </p>
        </header>

        {error && (
          <div
            role="alert"
            className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
          >
            <p>{error}</p>

            <button
              type="button"
              onClick={() => {
                setLoading(true);
                setError(null);

                void refresh()
                  .catch((caught) => {
                    setError(errorMessage(caught));
                  })
                  .finally(() => {
                    setLoading(false);
                  });
              }}
              className="mt-3 text-sm font-semibold underline underline-offset-4"
            >
              إعادة المحاولة
            </button>
          </div>
        )}

        {loading ? (
          <div className="rounded-2xl border border-black/[0.06] bg-white p-8 text-sm text-[#718176]">
            جاري تحميل سلتك...
          </div>
        ) : !authenticated ? (
          <div className="rounded-2xl border border-black/[0.06] bg-white px-6 py-12 text-center">
            <ShoppingBag
              size={32}
              className="mx-auto mb-5 text-[#193c30]"
            />

            <h2 className="text-xl font-semibold">
              سجّل دخولك لعرض سلتك
            </h2>

            <p className="mt-3 text-sm leading-7 text-[#718176]">
              يمكنك استخدام حساب ركن نفسه في جميع المتاجر.
            </p>

            <Link
              to={loginPath}
              className="mx-auto mt-6 flex h-11 max-w-xs items-center justify-center rounded-xl bg-[#193c30] px-5 text-sm font-semibold"
              style={{ color: "#ffffff" }}
            >
              تسجيل الدخول
            </Link>
          </div>
        ) : !error && items.length === 0 ? (
          <div className="rounded-2xl border border-black/[0.06] bg-white px-6 py-14 text-center">
            <ShoppingBag
              size={34}
              className="mx-auto mb-5 text-[#193c30]"
            />

            <h2 className="text-xl font-semibold">
              سلتك فارغة حاليًا
            </h2>

            <p className="mt-3 text-sm leading-7 text-[#718176]">
              لم تضف أي منتجات بعد. تصفح المتجر واكتشف ما يناسبك.
            </p>

            <Link
              to={storePath}
              className="mx-auto mt-6 flex h-11 max-w-xs items-center justify-center rounded-xl bg-[#193c30] px-5 text-sm font-semibold"
              style={{ color: "#ffffff" }}
            >
              تصفح المنتجات
            </Link>
          </div>
        ) : items.length > 0 ? (
          <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_280px]">

            <div className="space-y-3">
              {items.map((item) => (
                <article
                  key={item.id}
                  className="rounded-2xl border border-black/[0.06] bg-white p-5"
                >
                  <div className="flex items-start gap-4">

                    <div className="flex size-24 shrink-0 items-center justify-center overflow-hidden rounded-2xl border border-black/[0.06] bg-[#f7f8f6] p-2 sm:size-28">
                      {item.primaryImageUrl ? (
                        <img
                          src={item.primaryImageUrl}
                          alt={item.primaryImageAltText || item.productName || "صورة المنتج"}
                          loading="lazy"
                          className="h-full w-full object-contain"
                        />
                      ) : (
                        <ShoppingBag
                          size={25}
                          className="text-[#86948a]"
                        />
                      )}
                    </div>

                    <div className="min-w-0 flex-1">

                      <h3 className="text-base font-semibold leading-7 text-[#23362c]">
                        {item.productName || "تعذر عرض تفاصيل المنتج"}
                      </h3>

                      {item.variantName &&
                        item.variantName !== item.productName && (
                          <p className="mt-1 text-xs leading-6 text-[#738077]">
                            {item.variantName}
                          </p>
                        )}

                      {comparisonSaving(item) > 0 &&
                        item.compareAtPrice != null && (
                          <p className="mt-3 text-sm text-[#8c948d] line-through">
                            {formatAmount(
                              item.compareAtPrice,
                              item.currency,
                            )}
                          </p>
                        )}

                      <p className="mt-2 text-lg font-semibold text-[#193c30]">
                        {formatAmount(
                          item.unitPrice,
                          item.currency,
                        )}
                      </p>

                      {comparisonSaving(item) > 0 && (
                        <span className="mt-2 inline-flex rounded-lg bg-[#edf6ed] px-2.5 py-1 text-xs font-medium text-[#28653c]">
                          وفّرت {formatAmount(
                            comparisonSaving(item),
                            item.currency,
                          )} للقطعة
                        </span>
                      )}

                    </div>
                  </div>

                  <div className="mt-5 flex flex-wrap items-center justify-between gap-3 border-t border-black/[0.06] pt-4">

                    <div className="flex h-10 items-center rounded-xl border border-black/10">

                      <button
                        type="button"
                        aria-label="تقليل الكمية"
                        disabled={Boolean(busyId)}
                        onClick={() =>
                          void changeQuantity(
                            item,
                            item.quantity - 1,
                          )
                        }
                        className="flex size-10 items-center justify-center disabled:opacity-40"
                      >
                        <Minus size={15} />
                      </button>

                      <span className="min-w-8 text-center text-sm font-semibold">
                        {item.quantity}
                      </span>

                      <button
                        type="button"
                        aria-label="زيادة الكمية"
                        disabled={Boolean(busyId)}
                        onClick={() =>
                          void changeQuantity(
                            item,
                            item.quantity + 1,
                          )
                        }
                        className="flex size-10 items-center justify-center disabled:opacity-40"
                      >
                        <Plus size={15} />
                      </button>
                    </div>

                    <button
                      type="button"
                      disabled={Boolean(busyId)}
                      onClick={() => void removeItem(item)}
                      className="inline-flex items-center gap-2 text-sm text-red-700 disabled:opacity-40"
                    >
                      <Trash2 size={15} />
                      حذف
                    </button>
                  </div>

                  <div className="mt-4 flex justify-between text-sm">
                    <span className="text-[#718176]">
                      إجمالي المنتج
                    </span>

                    <span className="font-semibold">
                      {formatAmount(
                        item.lineTotal,
                        item.currency,
                      )}
                    </span>
                  </div>
                </article>
              ))}
            </div>

            <aside className="h-fit rounded-2xl border border-black/[0.06] bg-white p-5">
              <h2 className="text-base font-semibold">
                ملخص السلة
              </h2>

              <div className="mt-6 flex justify-between gap-3 text-sm">
                <span className="text-[#718176]">
                  عدد المنتجات
                </span>

                <span>{cart?.totalQuantity ?? 0}</span>
              </div>

              {productSavings > 0 && (
                <>
                  <div className="mt-4 flex justify-between gap-3 text-sm">
                    <span className="text-[#718176]">
                      قبل تخفيضات المنتجات
                    </span>

                    <span className="text-[#8a948c] line-through">
                      {formatAmount(
                        (cart?.totalAmount ?? 0) + productSavings,
                        cart?.currency ?? null,
                      )}
                    </span>
                  </div>

                  <div className="mt-3 flex justify-between gap-3 text-sm">
                    <span className="font-medium text-[#28653c]">
                      توفير المنتجات
                    </span>

                    <span className="font-semibold text-[#28653c]">
                      −{formatAmount(
                        productSavings,
                        cart?.currency ?? null,
                      )}
                    </span>
                  </div>
                </>
              )}

              <div className="mt-4 flex justify-between gap-3 border-t border-black/[0.06] pt-4">
                <span className="text-sm font-semibold">
                  إجمالي المنتجات
                </span>

                <span className="text-lg font-semibold">
                  {formatAmount(
                    cart?.totalAmount ?? 0,
                    cart?.currency ?? null,
                  )}
                </span>
              </div>

              <p className="mt-4 text-xs leading-6 text-[#718176]">
                يتم حساب الشحن والخصومات عند إتمام الطلب.
                لا يتم تحصيل أي مبلغ من هذه الصفحة.
              </p>

              <Link
                to={`${storePath}/checkout`}
                className="mt-6 flex h-12 w-full items-center justify-center rounded-xl bg-[#193c30] px-5 text-sm font-semibold"
                style={{ color: "#ffffff" }}
              >
                إتمام الطلب
              </Link>



              <Link
                to={storePath}
                className="mt-5 block text-center text-sm font-medium text-[#315f45]"
              >
                متابعة التسوق
              </Link>
            </aside>
          </div>
        ) : null}
      </div>
    </main>
  );
}