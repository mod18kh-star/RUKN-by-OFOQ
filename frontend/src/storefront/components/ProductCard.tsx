import {
  Heart,
  Plus,
} from "lucide-react";

import type {
  ProductCardStyle,
} from "../theme/theme.types";

interface ProductCardProps {
  name: string;
  category: string;
  price: string;

  compareAtPrice?: string;
  image: string;
  secondaryImage?: string;
  badge?: string;

  variant:
    ProductCardStyle;
}

export function ProductCard({
  name,
  category,
  price,
  compareAtPrice,
  image,
  secondaryImage,
  badge,
  variant,
}: ProductCardProps) {
  const boxed =
    variant === "commerce" ||
    variant === "technical";

  const compact =
    variant === "compact";

  return (
    <article
      className={[
        "group",
        boxed
          ? "border border-black/10 bg-[var(--store-surface)] p-3"
          : "",
        compact
          ? "grid grid-cols-[105px_1fr] gap-4 border-b border-black/10 py-4"
          : "",
      ].join(" ")}
    >
      <div
        className={[
          "relative overflow-hidden bg-[var(--store-soft)]",
          compact
            ? "aspect-square"
            : "aspect-[var(--store-image-ratio)]",
        ].join(" ")}
      >
        <img
          src={image}
          alt={name}
          className="h-full w-full object-cover transition duration-500 group-hover:scale-[1.015]"
        />

        {secondaryImage &&
        !compact ? (
          <img
            src={secondaryImage}
            alt=""
            className="absolute inset-0 h-full w-full object-cover opacity-0 transition duration-500 group-hover:opacity-100"
          />
        ) : null}

        {badge &&
        variant !==
          "minimal" ? (
          <span className="absolute right-3 top-3 bg-white px-2.5 py-1 text-[11px] font-semibold">
            {badge}
          </span>
        ) : null}

        {!compact ? (
          <button
            type="button"
            className="absolute left-3 top-3 flex size-9 items-center justify-center rounded-full bg-white opacity-0 transition group-hover:opacity-100"
          >
            <Heart size={17} />
          </button>
        ) : null}

        {variant !==
          "minimal" &&
        !compact ? (
          <button
            type="button"
            className="absolute bottom-3 left-3 flex size-10 items-center justify-center rounded-full bg-[var(--store-ink)] text-white opacity-0 transition group-hover:opacity-100"
          >
            <Plus size={18} />
          </button>
        ) : null}
      </div>

      <div
        className={
          compact
            ? "self-center"
            : "pt-4"
        }
      >
        <p className="mb-1 text-[11px] text-[var(--store-muted)]">
          {category}
        </p>

        <div className="flex items-start justify-between gap-4">
          <h3 className="text-[14px] font-medium">
            {name}
          </h3>

          <div className="shrink-0 text-[13px]">
            <span className="font-semibold">
              {price}
            </span>

            {compareAtPrice ? (
              <span className="mr-2 text-[11px] text-[var(--store-muted)] line-through">
                {compareAtPrice}
              </span>
            ) : null}
          </div>
        </div>

        {variant ===
        "commerce" ? (
          <button
            type="button"
            className="mt-4 w-full bg-[var(--store-ink)] py-2.5 text-[11px] font-semibold text-white"
          >
            إضافة للسلة
          </button>
        ) : null}

        {variant ===
        "technical" ? (
          <p className="mt-2 text-[11px] leading-5 text-[var(--store-ink-soft)]">
            المواصفات والتفاصيل متاحة
          </p>
        ) : null}
      </div>
    </article>
  );
}