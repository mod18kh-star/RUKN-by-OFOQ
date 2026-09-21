import {
  ArrowLeft,
  Heart,
  Plus,
} from "lucide-react";

import {
  useState,
} from "react";

import {
  Link,
} from "react-router";

import {
  SmartImage,
} from "./SmartImage";

import type {
  ProductCardStyle,
} from "../theme/theme.types";

interface ProductCardProps {
  name: string;
  category: string;
  price: string;
  description?: string;
  href?: string;
  compareAtPrice?: string;
  image: string;
  secondaryImage?: string;
  badge?: string;
  variant: ProductCardStyle;
}

export function ProductCard({
  name,
  category,
  price,
  description,
  href,
  compareAtPrice,
  image,
  secondaryImage,
  badge,
  variant,
}: ProductCardProps) {
  const [secondaryRequested, setSecondaryRequested] = useState(false);

  const mobileFlagship =
    variant === "mobile-flagship";

  const mobileMarket =
    variant === "mobile-market";

  const verticalSignature =
    variant === "vertical-signature";

  const verticalMarket =
    variant === "vertical-market";

  const boxed =
    variant === "commerce" ||
    variant === "technical" ||
    mobileMarket ||
    verticalMarket;

  const compact =
    variant === "compact";

  function requestSecondary() {
    if (
      secondaryImage &&
      !secondaryRequested
    ) {
      setSecondaryRequested(true);
    }
  }

  const imageBlock = (
    <div
      className={[
        "relative overflow-hidden bg-[var(--store-soft)]",
        compact || mobileMarket || verticalMarket
          ? "aspect-square"
          : "aspect-[var(--store-image-ratio)]",
        mobileFlagship
          ? "mobile-flagship-product-media rounded-[26px]"
          : mobileMarket
            ? "mobile-market-product-media rounded-[16px]"
            : verticalSignature
              ? "vertical-signature-product-media rounded-[calc(var(--store-radius)+2px)]"
              : verticalMarket
                ? "vertical-market-product-media rounded-[var(--store-radius)]"
                : "",
      ].join(" ")}
    >
      <SmartImage
        src={image}
        alt={name}
        responsiveWidths={[240, 360, 480, 640, 800]}
        fallbackWidth={640}
        sizes="(max-width: 767px) 50vw, (max-width: 1023px) 33vw, 25vw"
        className={[
          "h-full w-full transition",
          mobileFlagship
            ? "object-contain p-5 duration-700 group-hover:scale-[1.04] md:p-7"
            : mobileMarket
              ? "object-contain p-4 duration-500 group-hover:scale-[1.03]"
              : verticalSignature
                ? "object-cover duration-700 group-hover:scale-[1.035]"
                : verticalMarket
                  ? "object-cover duration-500 group-hover:scale-[1.025]"
                  : "object-cover duration-500 group-hover:scale-[1.015]",
        ].join(" ")}
      />

      {secondaryImage && secondaryRequested && !compact ? (
        <SmartImage
          src={secondaryImage}
          alt=""
          aria-hidden="true"
          fetchPriority="low"
          responsiveWidths={[240, 360, 480, 640, 800]}
          fallbackWidth={640}
          sizes="(max-width: 767px) 50vw, (max-width: 1023px) 33vw, 25vw"
          className={[
            "absolute inset-0 h-full w-full opacity-0 transition duration-700 group-hover:opacity-100",
            mobileFlagship
              ? "object-contain p-5 md:p-7"
              : mobileMarket
                ? "object-contain p-4"
                : verticalSignature || verticalMarket
                  ? "object-cover"
                  : "object-cover",
          ].join(" ")}
        />
      ) : null}

      {badge && variant !== "minimal" ? (
        <span
          className={[
            "absolute right-3 top-3 px-2.5 py-1 text-[11px] font-semibold",
            mobileFlagship
              ? "rounded-full bg-black/75 text-white backdrop-blur"
              : mobileMarket || verticalMarket
                ? "rounded-full bg-[var(--store-accent)] text-[var(--store-accent-contrast)]"
                : verticalSignature
                  ? "rounded-full bg-[var(--store-surface)]/90 text-[var(--store-ink)] shadow-sm backdrop-blur"
                  : "bg-white",
          ].join(" ")}
        >
          {badge}
        </span>
      ) : null}

      {!compact && !mobileMarket && !verticalMarket ? (
        <span
          aria-hidden="true"
          className={[
            "absolute left-3 top-3 flex size-9 items-center justify-center rounded-full bg-white transition",
            mobileFlagship
              ? "translate-y-1 opacity-0 shadow-sm group-hover:translate-y-0 group-hover:opacity-100"
              : "opacity-0 group-hover:opacity-100",
          ].join(" ")}
        >
          <Heart size={17} />
        </span>
      ) : null}

      {variant !== "minimal" && !compact && !mobileFlagship && !mobileMarket && !verticalSignature && !verticalMarket ? (
        <span className="absolute bottom-3 left-3 flex size-10 items-center justify-center rounded-full bg-[var(--store-ink)] text-[var(--store-ink-contrast)] opacity-0 transition group-hover:opacity-100">
          <Plus size={18} />
        </span>
      ) : null}
    </div>
  );

  return (
    <article
      data-rukn-target="productCardStyle"
      className={[
        "group",
        boxed
          ? "border border-black/10 bg-[var(--store-surface)] p-3"
          : "",
        compact
          ? "grid grid-cols-[105px_1fr] gap-4 border-b border-black/10 py-4"
          : "",
        mobileFlagship
          ? "mobile-flagship-product-card rounded-[26px]"
          : "",
        mobileMarket
          ? "mobile-market-product-card rounded-[20px] shadow-[0_8px_28px_rgba(20,34,58,.045)] transition duration-300 hover:-translate-y-1 hover:shadow-[0_20px_46px_rgba(20,34,58,.10)]"
          : "",
        verticalSignature
          ? "vertical-signature-product-card rounded-[calc(var(--store-radius)+2px)] transition duration-500 hover:-translate-y-1"
          : "",
        verticalMarket
          ? "vertical-market-product-card rounded-[calc(var(--store-radius)+2px)] shadow-[0_8px_24px_rgba(20,34,45,.035)] transition duration-300 hover:-translate-y-1 hover:shadow-[0_18px_42px_rgba(20,34,45,.08)]"
          : "",
      ].join(" ")}
      onPointerEnter={requestSecondary}
      onFocusCapture={requestSecondary}
    >
      {href ? (
        <Link to={href} className="block">
          {imageBlock}
        </Link>
      ) : imageBlock}

      <div
        className={[
          compact
            ? "self-center"
            : mobileFlagship
              ? "px-1 pb-2 pt-5"
              : mobileMarket
                ? "px-2 pb-2 pt-4"
                : verticalSignature
                  ? "px-1 pb-2 pt-5"
                  : verticalMarket
                    ? "px-2 pb-2 pt-4"
                    : "pt-4",
        ].join(" ")}
      >
        <p
          className={[
            "mb-1 text-[var(--store-muted)]",
            mobileFlagship || verticalSignature
              ? "text-[11px] font-semibold text-[var(--store-accent)]"
              : mobileMarket || verticalMarket
                ? "text-[11px] font-semibold"
                : "text-[11px]",
          ].join(" ")}
        >
          {category}
        </p>

        {mobileFlagship || verticalSignature ? (
          <div>
            {href ? (
              <Link
                to={href}
                className="block hover:opacity-75"
              >
                <h3 className="text-[19px] font-semibold leading-7 tracking-[-0.04em] md:text-[21px]">
                  {name}
                </h3>
              </Link>
            ) : (
              <h3 className="text-[19px] font-semibold leading-7 tracking-[-0.04em] md:text-[21px]">
                {name}
              </h3>
            )}

            <div className="mt-3 flex flex-wrap items-baseline gap-2">
              <span className="store-money text-[18px] font-bold tracking-[-0.02em]">
                {price}
              </span>
              {compareAtPrice ? (
                <span className="store-money text-[12px] text-[var(--store-muted)] line-through">
                  {compareAtPrice}
                </span>
              ) : null}
            </div>

            {description ? (
              <p className="mt-3 line-clamp-2 text-[12px] leading-6 text-[var(--store-ink-soft)]">
                {description}
              </p>
            ) : null}
          </div>
        ) : (
          <>
            <div className="flex items-start justify-between gap-4">
              {href ? (
                <Link
                  to={href}
                  className="hover:underline"
                >
                  <h3
                    className={[
                      "font-medium",
                      mobileMarket || verticalMarket
                        ? "text-[16px] font-semibold leading-6 tracking-[-0.025em]"
                        : "text-[14px]",
                    ].join(" ")}
                  >
                    {name}
                  </h3>
                </Link>
              ) : (
                <h3
                  className={[
                    "font-medium",
                    mobileMarket || verticalMarket
                      ? "text-[16px] font-semibold leading-6 tracking-[-0.025em]"
                      : "text-[14px]",
                  ].join(" ")}
                >
                  {name}
                </h3>
              )}

              <div
                className={[
                  "shrink-0",
                  mobileMarket || verticalMarket
                    ? "text-[14px]"
                    : "text-[13px]",
                ].join(" ")}
              >
                <span className={mobileMarket || verticalMarket ? "store-money text-[16px] font-bold" : "store-money font-semibold"}>
                  {price}
                </span>
                {compareAtPrice ? (
                  <span className="store-money mr-2 text-[11px] text-[var(--store-muted)] line-through">
                    {compareAtPrice}
                  </span>
                ) : null}
              </div>
            </div>

            {description ? (
              <p className="mt-2 line-clamp-2 text-[11px] leading-5 text-[var(--store-ink-soft)]">
                {description}
              </p>
            ) : null}
          </>
        )}

        {(mobileFlagship || verticalSignature) && href ? (
          <Link
            to={href}
            className="mt-5 inline-flex items-center gap-2 text-[11px] font-semibold text-[var(--store-ink-soft)] transition hover:text-[var(--store-accent)]"
          >
            استعرض التفاصيل
            <ArrowLeft size={14} />
          </Link>
        ) : null}

        {variant === "commerce" && href ? (
          <Link
            to={href}
            className="mt-4 block w-full bg-[var(--store-ink)] py-2.5 text-center text-[11px] font-semibold text-[var(--store-ink-contrast)]"
          >
            عرض المنتج
          </Link>
        ) : null}

        {variant === "technical" ? (
          <p className="mt-2 text-[11px] leading-5 text-[var(--store-ink-soft)]">
            المواصفات والتفاصيل متاحة
          </p>
        ) : null}

        {(mobileMarket || verticalMarket) && href ? (
          <Link
            to={href}
            className="mt-4 flex h-11 w-full items-center justify-center gap-2 rounded-[12px] bg-[var(--store-ink)] text-[12px] font-bold text-[var(--store-ink-contrast)] transition hover:-translate-y-0.5 hover:opacity-95"
          >
            عرض التفاصيل
            <ArrowLeft size={14} />
          </Link>
        ) : null}
      </div>
    </article>
  );
}
