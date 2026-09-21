import {
  useParams,
} from "react-router";

import {
  useStorefrontContent,
} from "../data/storefrontContent";

import type {
  StorefrontConfig,
} from "../theme/theme.types";

import {
  getThemePreset,
} from "../theme/themePresets";

import {
  SmartImage,
} from "./SmartImage";

export function CategoryShowcaseSection({
  config,
}: {
  config:
    StorefrontConfig;
}) {
  const {
    categories,
  } =
    useStorefrontContent();

  const section =
    config.categorySection;

  const {
    storeSlug = "demo",
  } = useParams<{
    storeSlug: string;
  }>();

  const categoryHref =
    (slug: string) =>
      `/store/${encodeURIComponent(storeSlug)}/categories/${encodeURIComponent(slug)}`;

  if (
    !section.enabled ||
    categories.length === 0
  ) {
    return null;
  }

  const theme =
    getThemePreset(config.themeId);

  if (config.categoryCardLayout && config.categoryCardLayout !== "theme-default") {
    const compact = config.categoryCardLayout === "compact";
    return (
      <section id="categories" className="store-container py-12 md:py-20">
        <div className="mb-6">
          <p data-rukn-target="categoryEyebrow" className="text-xs font-semibold text-[var(--store-accent)]">{section.eyebrow || "الأقسام"}</p>
          <h2 data-rukn-target="categories" className="mt-2 text-3xl font-bold">{section.title}</h2>
        </div>
        <div className={compact ? "grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4" : "grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4"}>
          {categories.map((category, index) => (
            <a data-rukn-target="categoryLayout" key={category.categoryId} href={categoryHref(category.slug)}
              className={compact
                ? "flex min-w-0 items-center gap-3 rounded-2xl border border-black/10 bg-[var(--store-surface)] p-3"
                : "overflow-hidden rounded-2xl border border-black/10 bg-[var(--store-surface)]"}>
              <SmartImage src={categoryImage(category.imageUrl, index)} alt={category.name}
                className={compact ? "h-20 w-20 shrink-0 rounded-xl object-cover" : "aspect-square w-full object-cover"}/>
              <div className={compact ? "min-w-0 flex-1" : "p-3"}>
                <span className="text-[10px] text-[var(--store-muted)]">{String(index + 1).padStart(2, "0")}</span>
                <h3 className="mt-1 truncate text-sm font-semibold">{category.name}</h3>
              </div>
            </a>
          ))}
        </div>
      </section>
    );
  }

  if (theme.experience === "signature" && config.themeId !== "mobile-flagship") {
    return (
      <section id="categories" className="store-container py-20 md:py-28">
        <div className="mb-10 flex flex-wrap items-end justify-between gap-5">
          <div>
            <p className="text-[11px] font-semibold text-[var(--store-accent)]">
              {section.eyebrow || theme.categoryLabel || "الأقسام"}
            </p>
            <h2 className="mt-2 max-w-3xl text-[clamp(2rem,3.6vw,4.2rem)] font-semibold leading-[1.08] tracking-[-0.055em]">
              {section.title}
            </h2>
          </div>
          <span className="hidden text-[10px] text-[var(--store-muted)] md:block">
            تصفح {theme.productLabel ?? "المنتجات"} حسب {theme.categoryLabel ?? "القسم"}
          </span>
        </div>

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {categories.map((category, index) => (
            <a
              key={category.categoryId}
              href={categoryHref(category.slug)}
              className={[
                "group relative min-h-[300px] overflow-hidden rounded-[calc(var(--store-radius)+4px)] border border-black/[0.06] bg-[var(--store-soft)] shadow-[0_18px_45px_rgba(12,14,18,.05)]",
                index === 0 ? "md:col-span-2 lg:min-h-[430px]" : "",
              ].join(" ")}
            >
              <SmartImage
                src={categoryImage(category.imageUrl, index)}
                alt={category.name}
                className="absolute inset-0 h-full w-full object-cover transition duration-700 group-hover:scale-[1.05]"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/76 via-black/10 to-transparent" />
              <div className="absolute inset-x-0 bottom-0 flex items-end justify-between gap-4 p-5 text-white md:p-6">
                <div>
                  <span dir="ltr" className="store-money text-[9px] font-semibold tracking-[0.13em] text-white/58">
                    {String(index + 1).padStart(2, "0")}
                  </span>
                  <h3 className="mt-2 text-[21px] font-semibold tracking-[-0.04em] md:text-[24px]">{category.name}</h3>
                </div>
                <span className="flex size-11 items-center justify-center rounded-full border border-white/25 bg-white/[0.10] text-[18px] backdrop-blur transition group-hover:-translate-x-1">←</span>
              </div>
            </a>
          ))}
        </div>
      </section>
    );
  }

  if (theme.experience === "market" && config.themeId !== "mobile-smart-market") {
    return (
      <section id="categories" className="store-container py-14 md:py-20">
        <div className="mb-7 flex items-end justify-between gap-5">
          <div>
            <p className="text-[10px] font-semibold text-[var(--store-accent)]">{section.eyebrow || theme.categoryLabel || "الأقسام"}</p>
            <h2 className="mt-2 text-[clamp(1.9rem,3vw,3.2rem)] font-bold tracking-[-0.045em]">{section.title}</h2>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4">
          {categories.map((category, index) => (
            <a
              key={category.categoryId}
              href={categoryHref(category.slug)}
              className="group relative flex min-h-[126px] items-center gap-4 overflow-hidden rounded-[calc(var(--store-radius)+2px)] border border-black/[0.07] bg-[var(--store-surface)] p-3.5 shadow-[0_8px_26px_rgba(20,34,58,.035)] transition duration-300 hover:-translate-y-0.5 hover:border-[var(--store-accent)] hover:shadow-[0_16px_34px_rgba(20,34,58,.07)]"
            >
              <div className="size-[94px] shrink-0 overflow-hidden rounded-[calc(var(--store-radius)-2px)] bg-[var(--store-soft)]">
                <SmartImage src={categoryImage(category.imageUrl, index)} alt={category.name} className="h-full w-full object-cover transition duration-500 group-hover:scale-[1.05]" />
              </div>
              <div className="min-w-0">
                <span dir="ltr" className="store-money text-[9px] font-bold text-[var(--store-accent)]">{String(index + 1).padStart(2, "0")}</span>
                <p className="mt-1 text-[14px] font-bold leading-6 tracking-[-0.02em]">{category.name}</p>
                <span className="mt-1.5 block text-[9px] font-medium text-[var(--store-muted)]">استعرض {theme.productLabel ?? "المنتجات"} ←</span>
              </div>
            </a>
          ))}
        </div>
      </section>
    );
  }

  if (
    config.themeId ===
    "mobile-flagship"
  ) {
    return (
      <section
        id="categories"
        className="store-container py-20 md:py-28"
      >
        <div className="mb-10 flex flex-wrap items-end justify-between gap-5">
          <div>
            <p className="text-[12px] font-semibold text-[var(--store-accent)]">
              {section.eyebrow || "اختر فئتك"}
            </p>
            <h2 className="mt-2 max-w-3xl text-[clamp(2rem,3.8vw,4.4rem)] font-semibold leading-[1.08] tracking-[-0.055em]">
              {section.title}
            </h2>
          </div>
          <span className="hidden text-[11px] text-[var(--store-muted)] md:block">
            تصفح الأجهزة حسب القسم
          </span>
        </div>

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {categories.map((category, index) => (
            <a
              key={category.categoryId}
              href={categoryHref(category.slug)}
              className={[
                "group relative min-h-[300px] overflow-hidden rounded-[28px] border border-black/[0.06] bg-[var(--store-soft)] shadow-[0_18px_45px_rgba(12,14,18,.06)]",
                index === 0
                  ? "md:col-span-2 lg:min-h-[440px]"
                  : "",
              ].join(" ")}
            >
              <SmartImage
                src={categoryImage(category.imageUrl, index)}
                alt={category.name}
                className="absolute inset-0 h-full w-full object-cover transition duration-700 group-hover:scale-[1.055]"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black/76 via-black/12 to-black/[0.02]" />
              <div className="absolute inset-x-0 bottom-0 flex items-end justify-between gap-4 p-5 text-white md:p-6">
                <div>
                  <span className="store-money text-[10px] font-semibold tracking-[0.12em] text-white/60" dir="ltr">
                    {String(index + 1).padStart(2, "0")}
                  </span>
                  <h3 className="mt-2 text-[22px] font-semibold tracking-[-0.04em] md:text-[25px]">
                    {category.name}
                  </h3>
                </div>
                <span className="flex size-11 items-center justify-center rounded-full border border-white/25 bg-white/[0.10] text-[18px] shadow-sm backdrop-blur transition group-hover:-translate-x-1">
                  ←
                </span>
              </div>
            </a>
          ))}
        </div>
      </section>
    );
  }

  if (
    config.themeId ===
    "mobile-smart-market"
  ) {
    return (
      <section
        id="categories"
        className="store-container py-14 md:py-20"
      >
        <div className="mb-7 flex items-end justify-between gap-5">
          <div>
            <p className="text-[11px] font-semibold text-[var(--store-accent)]">
              {section.eyebrow || "الأقسام"}
            </p>
            <h2 className="mt-2 text-[clamp(1.9rem,3vw,3.2rem)] font-bold tracking-[-0.045em]">
              {section.title}
            </h2>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4">
          {categories.map((category, index) => (
            <a
              key={category.categoryId}
              href={categoryHref(category.slug)}
              className="group relative flex min-h-[126px] items-center gap-4 overflow-hidden rounded-[18px] border border-black/[0.07] bg-white p-3.5 shadow-[0_8px_26px_rgba(20,34,58,.04)] transition duration-300 hover:-translate-y-0.5 hover:border-[var(--store-accent)]/30 hover:shadow-[0_16px_34px_rgba(20,34,58,.08)]"
            >
              <div className="size-[94px] shrink-0 overflow-hidden rounded-[15px] bg-[var(--store-soft)]">
                <SmartImage
                  src={categoryImage(category.imageUrl, index)}
                  alt={category.name}
                  className="h-full w-full object-cover transition duration-500 group-hover:scale-[1.055]"
                />
              </div>
              <div className="min-w-0">
                <span className="store-money text-[9px] font-bold text-[var(--store-accent)]" dir="ltr">
                  {String(index + 1).padStart(2, "0")}
                </span>
                <p className="mt-1 text-[15px] font-bold leading-6 tracking-[-0.02em]">
                  {category.name}
                </p>
                <span className="mt-1.5 block text-[10px] font-medium text-[var(--store-muted)]">
                  استعرض الأجهزة ←
                </span>
              </div>
            </a>
          ))}
        </div>
      </section>
    );
  }

  if (
    config.themeId ===
    "commerce"
  ) {
    return (
      <section
        id="categories"
        className="store-container py-14 md:py-20"
      >
        <SectionHeading
          eyebrow={
            section.eyebrow
          }
          title={
            section.title
          }
          strong
        />

        <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
          {categories.map(
            (
              category,
              index,
            ) => (
              <CategoryCommerceCard
                key={
                  category.categoryId
                }
                name={
                  category.name
                }
                href={
                  categoryHref(category.slug)
                }
                image={
                  categoryImage(category.imageUrl, index)
                }
              />
            ),
          )}
        </div>
      </section>
    );
  }

  if (
    config.themeId ===
    "maison"
  ) {
    return (
      <section
        id="categories"
        className="store-container py-28 md:py-36"
      >
        <div className="mb-14 text-center">
          <p className="text-[10px] tracking-[0.1em] text-[var(--store-muted)]">
            {section.eyebrow}
          </p>

          <h2 className="mt-4 text-[clamp(2rem,3vw,3.7rem)] font-normal tracking-[-0.035em]">
            {section.title}
          </h2>
        </div>

        <div className="grid grid-cols-2 gap-5 md:grid-cols-4">
          {categories.map(
            (
              category,
              index,
            ) => (
              <a
                key={
                  category.categoryId
                }
                href={categoryHref(category.slug)}
                className="group text-center"
              >
                <div className="aspect-[3/4] overflow-hidden">
                  <SmartImage
                    src={
                      categoryImage(category.imageUrl, index)
                    }
                    alt={
                      category.name
                    }
                    className="h-full w-full object-cover transition duration-500 group-hover:scale-[1.02]"
                  />
                </div>

                <p className="mt-4 text-[12px] font-medium">
                  {category.name}
                </p>
              </a>
            ),
          )}
        </div>
      </section>
    );
  }

  if (
    config.themeId ===
    "studio"
  ) {
    return (
      <section
        id="categories"
        className="store-container py-20 md:py-28"
      >
        <SectionHeading
          eyebrow={
            section.eyebrow
          }
          title={
            section.title
          }
        />

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {categories.map(
            (
              category,
              index,
            ) => (
              <a
                key={
                  category.categoryId
                }
                href={categoryHref(category.slug)}
                className={[
                  "group relative min-h-[300px] overflow-hidden rounded-[var(--store-radius)]",
                  index === 0
                    ? "lg:col-span-2 lg:min-h-[440px]"
                    : "",
                ].join(" ")}
              >
                <SmartImage
                  src={
                    categoryImage(category.imageUrl, index)
                  }
                  alt={
                    category.name
                  }
                  className="absolute inset-0 h-full w-full object-cover transition duration-500 group-hover:scale-[1.02]"
                />

                <div className="absolute inset-0 bg-gradient-to-t from-black/55 via-transparent to-transparent" />

                <p className="absolute bottom-5 right-5 text-[18px] font-semibold text-white">
                  {category.name}
                </p>
              </a>
            ),
          )}
        </div>
      </section>
    );
  }

  if (
    config.themeId ===
    "technical"
  ) {
    return (
      <section
        id="categories"
        className="store-container py-16 md:py-20"
      >
        <div className="mb-7 border-b border-black/10 pb-5">
          <p className="text-[10px] font-semibold uppercase tracking-[0.08em] text-[var(--store-accent)]">
            {section.eyebrow}
          </p>

          <h2 className="mt-2 text-[clamp(1.8rem,2.6vw,3rem)] font-semibold tracking-[-0.045em]">
            {section.title}
          </h2>
        </div>

        <div className="grid grid-cols-2 gap-px overflow-hidden rounded-[var(--store-radius)] border border-black/10 bg-black/10 md:grid-cols-4">
          {categories.map(
            (
              category,
              index,
            ) => (
              <a
                key={
                  category.categoryId
                }
                href={categoryHref(category.slug)}
                className="bg-[var(--store-surface)] p-4"
              >
                <span className="text-[9px] text-[var(--store-muted)]">
                  {String(
                    index + 1,
                  ).padStart(
                    2,
                    "0",
                  )}
                </span>

                <SmartImage
                  src={
                    categoryImage(category.imageUrl, index)
                  }
                  alt={
                    category.name
                  }
                  className="mt-3 aspect-square w-full object-cover"
                />

                <p className="mt-3 text-[11px] font-semibold">
                  {category.name}
                </p>
              </a>
            ),
          )}
        </div>
      </section>
    );
  }

  return (
    <section
      id="categories"
      className="store-container py-20 md:py-28"
    >
      <SectionHeading
        eyebrow={
          section.eyebrow
        }
        title={
          section.title
        }
      />

      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        {categories.map(
          (
            category,
            index,
          ) => (
            <a
              key={
                category.categoryId
              }
              href={categoryHref(category.slug)}
              className="group"
            >
              <div className="aspect-[4/5] overflow-hidden bg-[var(--store-soft)]">
                <SmartImage
                  src={
                    categoryImage(category.imageUrl, index)
                  }
                  alt={
                    category.name
                  }
                  className="h-full w-full object-cover transition duration-500 group-hover:scale-[1.02]"
                />
              </div>

              <p className="mt-3 text-[12px] font-medium">
                {category.name}
              </p>
            </a>
          ),
        )}
      </div>
    </section>
  );
}

function CategoryCommerceCard({
  name,
  href,
  image,
}: {
  name: string;
  href: string;
  image: string;
}) {
  return (
    <a
      href={href}
      className="flex items-center gap-3 rounded-[var(--store-radius)] border border-black/[0.08] bg-[var(--store-surface)] p-3"
    >
      <SmartImage
        src={
          image
        }
        alt={
          name
        }
        className="size-16 rounded-[8px] object-cover"
      />

      <p className="text-[12px] font-semibold">
        {name}
      </p>
    </a>
  );
}

function SectionHeading({
  eyebrow,
  title,
  strong = false,
}: {
  eyebrow: string;
  title: string;
  strong?: boolean;
}) {
  return (
    <div className="mb-9">
      <p
        className={[
          "text-[11px]",
          strong
            ? "font-semibold text-[var(--store-accent)]"
            : "text-[var(--store-muted)]",
        ].join(" ")}
      >
        {eyebrow}
      </p>

      <h2
        className={[
          "mt-2 text-[clamp(1.9rem,3vw,3.2rem)] tracking-[-0.045em]",
          strong
            ? "font-bold"
            : "font-medium",
        ].join(" ")}
      >
        {title}
      </h2>
    </div>
  );
}

function categoryImage(
  imageUrl: string | null,
  index: number,
) {
  void index;

  return imageUrl?.trim() ??
    "";
}
