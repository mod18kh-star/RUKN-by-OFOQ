import {
  useStorefrontContent,
} from "../data/storefrontContent";

import type {
  StorefrontConfig,
} from "../theme/theme.types";

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

  if (
    !section.enabled ||
    categories.length === 0
  ) {
    return null;
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
                slug={
                  category.slug
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
                href={`#category-${category.slug}`}
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
                href={`#category-${category.slug}`}
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
                href={`#category-${category.slug}`}
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
              href={`#category-${category.slug}`}
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
  slug,
  image,
}: {
  name: string;
  slug: string;
  image: string;
}) {
  return (
    <a
      href={`#category-${slug}`}
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
