import {
  getAccessToken,
} from "../../features/auth/authSession";

import {
  addCustomerCartItem,
} from "../data/customerCartApi";

import {
  ArrowRight,
  Check,
  PackageCheck,
  ShieldCheck,
  ShoppingBag,
} from "lucide-react";

import {
  useMemo,
  useState,
} from "react";

import {
  useQuery,
} from "@tanstack/react-query";

import {
  Link,
  useNavigate,
  useParams,
} from "react-router";

import {
  StorefrontHeader,
} from "../components/StorefrontHeader";

import {
  StorefrontFooter,
} from "../components/StorefrontFooter";

import {
  SmartImage,
} from "../components/SmartImage";

import {
  DEFAULT_STOREFRONT_CONFIG,
} from "../config/storefrontConfig";

import { parseVisualContent } from "../config/visualContent";

import {
  createLiveStorefrontConfig,
} from "../config/liveStorefrontConfig";

import {
  EMPTY_STOREFRONT_CONTACT,
  getStorefrontInfo,
  getStorefrontProduct,
  type StorefrontVariant,
} from "../data/storefrontApi";

import {
  createThemeStyle,
  resolveStorefrontConfig,
} from "../theme/themeEngine";

import {
  getThemePreset,
} from "../theme/themePresets";

import {
  formatStorefrontMoney,
} from "../utils/money";

export function StorefrontProductPage() {
  const {
    storeSlug = "",
    productSlug = "",
  } = useParams<{
    storeSlug: string;
    productSlug: string;
  }>();

  const navigate = useNavigate();

  const productQuery =
    useQuery({
      queryKey: [
        "storefront-product",
        storeSlug,
        productSlug,
      ],
      queryFn: () =>
        getStorefrontProduct(
          storeSlug,
          productSlug,
        ),
      enabled:
        Boolean(
          storeSlug &&
          productSlug,
        ),
      retry: 1,
    });

  const storeQuery =
    useQuery({
      queryKey: [
        "storefront-product",
        storeSlug,
        "store",
      ],
      queryFn: () =>
        getStorefrontInfo(
          storeSlug,
        ),
      enabled:
        Boolean(storeSlug),
      retry: 1,
      staleTime:
        5 * 60_000,
    });

  const config =
    resolveStorefrontConfig(
      storeQuery.data
        ? createLiveStorefrontConfig(
            storeQuery.data,
          )
        : {
            ...DEFAULT_STOREFRONT_CONFIG,
            storeName:
              storeSlug ||
              "المتجر",
          },
    );

  const product =
    productQuery.data;

  const saleVariants =
    useMemo(
      () =>
        product?.variants.filter(
          (variant) =>
            !variant.isDefault,
        ) ?? [],
      [product],
    );

  const defaultVariant =
    product?.variants.find(
      (variant) =>
        variant.isDefault,
    ) ??
    product?.variants[0] ??
    null;

  const [selectedId, setSelectedId] =
    useState<string | null>(null);

  const selected:
    StorefrontVariant | null =
    saleVariants.find(
      (variant) =>
        variant.variantId ===
        selectedId,
    ) ??
    saleVariants[0] ??
    defaultVariant;

  const images =
    product?.images.length
      ? product.images
      : product?.primaryImageUrl
        ? [
            {
              imageId: "primary",
              url: product.primaryImageUrl,
              altText:
                product.primaryImageAltText,
              sortOrder: 0,
              isPrimary: true,
            },
          ]
        : [];

  const [imageIndex, setImageIndex] =
    useState(0);

  const [cartBusy, setCartBusy] =
    useState(false);

  const [cartMessage, setCartMessage] =
    useState<string | null>(null);

  const [cartError, setCartError] =
    useState<string | null>(null);

  async function handleAddToCart() {
    if (cartBusy || !product || !selected) {
      return;
    }

    if (
      !product.availableForSale ||
      !selected.availableForSale
    ) {
      setCartError(
        "هذا المنتج أو الخيار المحدد غير متاح حاليًا.",
      );

      return;
    }

    if (!getAccessToken()) {
      const returnTo = encodeURIComponent(
        `${window.location.pathname}${window.location.search}`,
      );

      navigate(
        `/store/${encodeURIComponent(storeSlug)}/account/login?mode=login&returnTo=${returnTo}`,
      );

      return;
    }

    setCartBusy(true);
    setCartError(null);
    setCartMessage(null);

    try {
      await addCustomerCartItem(
        storeSlug,
        {
          productId: product.productId,
          productVariantId: selected.variantId,
          quantity: 1,
        },
      );

      setCartMessage(
        "تمت إضافة المنتج إلى سلتك بنجاح.",
      );

      window.dispatchEvent(
        new CustomEvent("rukn:cart-updated", {
          detail: { storeSlug },
        }),
      );
    } catch (caught) {
      const status =
        caught instanceof Error &&
        "status" in caught
          ? Number(caught.status)
          : null;

      if (status === 401) {
        const returnTo = encodeURIComponent(
          `${window.location.pathname}${window.location.search}`,
        );

        navigate(
          `/store/${encodeURIComponent(storeSlug)}/account/login?mode=login&returnTo=${returnTo}`,
        );

        return;
      }

      setCartError(
        caught instanceof Error
          ? caught.message
          : "تعذرت إضافة المنتج إلى السلة. حاول مرة أخرى.",
      );
    } finally {
      setCartBusy(false);
    }
  }


  if (
    productQuery.isPending ||
    storeQuery.isPending
  ) {
    return (
      <div
        dir="rtl"
        className="mx-auto max-w-[1300px] px-5 py-24 text-center text-sm text-black/45"
      >
        جاري تحميل المنتج...
      </div>
    );
  }

  if (
    productQuery.isError ||
    !product
  ) {
    return (
      <div
        dir="rtl"
        className="mx-auto max-w-[1000px] px-5 py-24 text-center"
      >
        <p className="text-xl font-semibold">
          تعذر فتح المنتج
        </p>
        <Link
          to={`/store/${storeSlug}`}
          className="mt-5 inline-flex items-center gap-2 text-sm underline"
        >
          العودة للمتجر
        </Link>
      </div>
    );
  }

  const activeImage =
    images[
      Math.min(
        imageIndex,
        Math.max(
          images.length - 1,
          0,
        ),
      )
    ]?.url ??
    product.primaryImageUrl ??
    null;

  const theme =
    getThemePreset(config.themeId);

  const isFlagship =
    config.themeId === "mobile-flagship" ||
    theme.experience === "signature";

  const isSmartMarket =
    config.themeId === "mobile-smart-market" ||
    theme.experience === "market";

  return (
    <div
      dir="rtl"
      data-store-theme={config.themeId}
      style={createThemeStyle(config)}
      className="min-h-screen bg-[var(--store-canvas)] font-[var(--store-font)] text-[var(--store-body-text)]"
    >
      <StorefrontHeader
        storeSlug={storeSlug}
        storeName={config.storeName}
        logoUrl={config.logoUrl}
        announcement={config.announcement}
        themeId={config.themeId}
      />

      <main
        className={[
          "store-container py-7 md:py-10",
          isFlagship
            ? "md:py-14"
            : "",
        ].join(" ")}
      >
        <Link
          to={`/store/${encodeURIComponent(storeSlug)}`}
          className="inline-flex items-center gap-2 text-[11px] font-medium text-[var(--store-muted)] transition hover:text-[var(--store-ink)]"
        >
          <ArrowRight size={14} />
          العودة للمتجر
        </Link>

        <div
          className={[
            "mt-7 grid gap-8 lg:grid-cols-[1.05fr_.95fr]",
            isFlagship
              ? "lg:gap-12"
              : "",
          ].join(" ")}
        >
          <section>
            <div
              className={[
                "overflow-hidden bg-[var(--store-surface)]",
                isFlagship
                  ? "mobile-flagship-product-stage vertical-signature-product-stage rounded-[calc(var(--store-radius)+6px)] border border-black/[0.06] shadow-[0_28px_80px_rgba(10,12,16,.08)]"
                  : isSmartMarket
                    ? "mobile-market-product-stage vertical-market-product-stage rounded-[calc(var(--store-radius)+4px)] border border-black/[0.06] shadow-[0_18px_55px_rgba(30,55,90,.06)]"
                    : "",
              ].join(" ")}
            >
              {activeImage ? (
                <SmartImage
                  src={activeImage}
                  alt={product.name}
                  className={[
                    "aspect-square w-full object-contain",
                    isFlagship
                      ? "p-7 md:p-12"
                      : isSmartMarket
                        ? "p-5 md:p-8"
                        : "",
                  ].join(" ")}
                  sizes="(max-width:1024px) 100vw, 55vw"
                />
              ) : (
                <div className="flex aspect-square w-full flex-col items-center justify-center bg-black/[0.025] text-black/25">
                  <PackageCheck size={42} />
                  <span className="mt-3 text-[11px] font-semibold">
                    لا توجد صورة للمنتج
                  </span>
                </div>
              )}
            </div>

            {images.length > 1 ? (
              <div className="mt-3 grid grid-cols-5 gap-2">
                {images.map(
                  (image, index) => (
                    <button
                      key={image.imageId}
                      type="button"
                      onClick={() =>
                        setImageIndex(index)
                      }
                      className={[
                        "overflow-hidden border bg-white",
                        isFlagship ||
                        isSmartMarket
                          ? "rounded-[12px]"
                          : "",
                        imageIndex === index
                          ? "border-[var(--store-accent)]"
                          : "border-black/10",
                      ].join(" ")}
                    >
                      <SmartImage
                        src={image.url}
                        alt={
                          image.altText ??
                          product.name
                        }
                        className={[
                          "aspect-square w-full",
                          isFlagship || isSmartMarket
                            ? "object-contain p-1.5"
                            : "object-cover",
                        ].join(" ")}
                      />
                    </button>
                  ),
                )}
              </div>
            ) : null}
          </section>

          <section
            className={[
              "lg:sticky lg:top-28 lg:self-start",
              isSmartMarket
                ? "rounded-[20px] border border-black/[0.06] bg-white p-6 shadow-[0_14px_40px_rgba(20,34,58,.045)] md:p-8"
                : "",
            ].join(" ")}
          >
            <p
              className={[
                "text-[12px] font-medium",
                isFlagship ||
                isSmartMarket
                  ? "text-[var(--store-accent)]"
                  : "text-[var(--store-muted)]",
              ].join(" ")}
            >
              {product.categoryName ??
                "جوال"}
            </p>

            <h1
              className={[
                "mt-3 font-semibold leading-[1.04] tracking-[-0.055em]",
                isFlagship
                  ? "text-[clamp(2.8rem,5vw,5.5rem)]"
                  : isSmartMarket
                    ? "text-[clamp(2.35rem,4.2vw,4.6rem)]"
                    : "text-[clamp(2.1rem,4vw,4.5rem)]",
              ].join(" ")}
            >
              {product.name}
            </h1>

            {product.description ? (
              <p className="mt-5 max-w-2xl text-[14px] leading-8 text-[var(--store-ink-soft)]">
                {product.description}
              </p>
            ) : null}

            <div className="mt-6 flex items-baseline gap-3">
              <span
                className={[
                  "font-semibold",
                  "store-money",
                  isFlagship
                    ? "text-[30px] font-bold tracking-[-0.02em]"
                    : isSmartMarket
                      ? "text-[28px] font-bold tracking-[-0.02em]"
                      : "text-2xl",
                ].join(" ")}
              >
                {formatStorefrontMoney(
                  selected?.price ??
                    product.price,
                  selected?.currency ??
                    product.currency,
                )}
              </span>
              {product.compareAtPrice ? (
                <span className="store-money text-sm text-[var(--store-muted)] line-through">
                  {formatStorefrontMoney(
                    product.compareAtPrice,
                    product.currency,
                  )}
                </span>
              ) : null}
            </div>

            {saleVariants.length ? (
              <div className="mt-8">
                <p className="text-[12px] font-semibold">
                  الخيارات المتوفرة
                </p>
                <div className="mt-3 flex flex-wrap gap-2">
                  {saleVariants.map(
                    (variant) => (
                      <button
                        key={variant.variantId}
                        type="button"
                        disabled={
                          !variant.availableForSale
                        }
                        onClick={() =>
                          setSelectedId(
                            variant.variantId,
                          )
                        }
                        className={[
                          "border px-4 py-3 text-[11px] font-semibold disabled:opacity-35",
                          isFlagship ||
                          isSmartMarket
                            ? "rounded-[12px]"
                            : "rounded-[10px]",
                          selected?.variantId ===
                          variant.variantId
                            ? "border-[var(--store-ink)] bg-[var(--store-ink)] text-[var(--store-ink-contrast)]"
                            : "border-black/10 bg-white",
                        ].join(" ")}
                      >
                        {variant.name}
                        {variant.trackInventory &&
                        variant.quantity !==
                          null ? (
                          <span className="store-money mr-2 text-[9px] opacity-60" dir="ltr">
                            ({variant.quantity})
                          </span>
                        ) : null}
                      </button>
                    ),
                  )}
                </div>
              </div>
            ) : null}

            {/* RUKN_STOCK_NOTICE_V1 */}
            {(!product.availableForSale ||
              !selected?.availableForSale) && (
              <div
                role="status"
                className="mt-6 rounded-xl border border-[#e8e6df] bg-[#f7f5ef] px-5 py-4"
              >
                <p className="text-[13px] font-semibold text-[#302e29]">
                  {!selected
                    ? "يرجى تحديد خيارات المنتج"
                    : !selected.availableForSale &&
                        selected.trackInventory &&
                        selected.quantity === 0
                      ? "هذا المنتج غير متوفر حاليًا"
                      : "المنتج أو الخيار المحدد غير متاح حاليًا"}
                </p>

                <p className="mt-2 text-[11px] leading-7 text-[#776f62]">
                  {!selected
                    ? "اختر اللون أو السعة أو المقاس المطلوب للمتابعة، إذا كان متوفرًا."
                    : !selected.availableForSale &&
                        selected.trackInventory &&
                        selected.quantity === 0
                      ? "نفدت الكمية المتاحة من هذا المنتج. يمكنك العودة لاحقًا أو استعراض المنتجات الأخرى."
                      : "لا يمكن إضافة هذا المنتج إلى السلة حاليًا. يمكنك اختيار منتج آخر متوفر في المتجر."}
                </p>
              </div>
            )}
<div className="mt-7 grid gap-2 sm:grid-cols-2">
              <button
                type="button"
                onClick={() => void handleAddToCart()}
                disabled={!product.availableForSale || !selected?.availableForSale || cartBusy}
                className={[
                  "inline-flex h-13 items-center justify-center gap-2 bg-[var(--store-button-bg)] px-5 text-[12px] font-semibold text-[var(--store-button-text)] disabled:opacity-60",
                  isFlagship
                    ? "rounded-full"
                    : "rounded-[12px]",
                ].join(" ")}
              >
                <ShoppingBag size={16} />
                {cartBusy
                  ? "جاري الإضافة..."
                  : !selected
                    ? "اختر خيارات المنتج"
                    : !selected.availableForSale &&
                        selected.trackInventory &&
                        selected.quantity === 0
                      ? "نفدت الكمية"
                      : !product.availableForSale ||
                          !selected.availableForSale
                        ? "غير متوفر حاليًا"
                        : "إضافة للسلة"}
              </button>
              <button
                type="button"
                disabled
                className={[
                  "h-13 border border-black/15 bg-white px-5 text-[12px] font-semibold disabled:opacity-60",
                  isFlagship
                    ? "rounded-full"
                    : "rounded-[12px]",
                ].join(" ")}
              >
                اشتري الآن
              </button>
            </div>

                        {cartMessage ? (
              <p
                role="status"
                className="mt-3 rounded-xl border border-green-200 bg-green-50 px-4 py-3 text-sm font-medium text-green-800"
              >
                {cartMessage}
              </p>
            ) : null}

            {cartError ? (
              <p
                role="alert"
                className="mt-3 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
              >
                {cartError}
              </p>
            ) : null}

            <p className="mt-2 text-[11px] leading-6 text-[var(--store-muted)]">
              يمكنك إضافة المنتجات المتاحة إلى سلتك. سيتم تفعيل صفحة السلة وإتمام الطلب في الخطوات التالية.
            </p>

            <div
              className={[
                "mt-7 grid grid-cols-3 gap-2 text-center text-[9px] text-[var(--store-ink-soft)]",
              ].join(" ")}
            >
              <ProductTrustItem
                icon={PackageCheck}
                label="المخزون"
                rounded={
                  isFlagship ||
                  isSmartMarket
                }
              />
              <ProductTrustItem
                icon={ShieldCheck}
                label="المواصفات"
                rounded={
                  isFlagship ||
                  isSmartMarket
                }
              />
              <ProductTrustItem
                icon={Check}
                label="الخيارات"
                rounded={
                  isFlagship ||
                  isSmartMarket
                }
              />
            </div>
          </section>
        </div>

        {product.attributes.length ? (
          <section className="mt-16 border-t border-black/10 pt-10 md:mt-20 md:pt-12">
            <div className="mb-7 flex flex-wrap items-end justify-between gap-4">
              <div>
                <p className="text-[11px] font-semibold text-[var(--store-accent)]">
                  تفاصيل {theme.productSingularLabel ?? "المنتج"}
                </p>
                <h2 className="mt-2 text-[clamp(2rem,3.2vw,3.7rem)] font-semibold tracking-[-0.05em]">
                  المواصفات
                </h2>
              </div>
              {isFlagship ? (
                <span className="text-[10px] text-[var(--store-muted)]">
                  كل ما تحتاج معرفته قبل الاختيار
                </span>
              ) : null}
            </div>

            <div
              className={[
                "grid overflow-hidden border border-black/10 bg-black/10 md:grid-cols-2",
                isFlagship ||
                isSmartMarket
                  ? "gap-2 border-0 bg-transparent"
                  : "gap-px",
              ].join(" ")}
            >
              {product.attributes.map(
                (attribute) => (
                  <div
                    key={attribute.key}
                    className={[
                      "flex items-center justify-between gap-5 bg-white px-5 py-4",
                      isFlagship ||
                      isSmartMarket
                        ? "rounded-[14px] border border-black/[0.06]"
                        : "",
                    ].join(" ")}
                  >
                    <span className="text-[11px] text-[var(--store-muted)]">
                      {attribute.label}
                    </span>
                    <span className="text-[12px] font-semibold">
                      {attribute.value}
                    </span>
                  </div>
                ),
              )}
            </div>
          </section>
        ) : null}
      </main>

      <StorefrontFooter
        storeSlug={storeSlug}
        storeName={config.storeName}
        logoUrl={config.logoUrl}
        themeId={config.themeId}
        contact={storeQuery.data?.contact ?? EMPTY_STOREFRONT_CONTACT}
        description={parseVisualContent(storeQuery.data?.presentation.visualContentJson).footerDescription}
      />
    </div>
  );
}

function ProductTrustItem({
  icon: Icon,
  label,
  rounded,
}: {
  icon: typeof Check;
  label: string;
  rounded: boolean;
}) {
  return (
    <div
      className={[
        "border border-black/[0.07] bg-white p-3",
        rounded
          ? "rounded-[12px]"
          : "",
      ].join(" ")}
    >
      <Icon
        className="mx-auto mb-2"
        size={17}
      />
      {label}
    </div>
  );
}
