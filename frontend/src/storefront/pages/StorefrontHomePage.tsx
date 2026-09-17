import {
  ArrowLeft,
  Check,
  PackageCheck,
  RefreshCw,
  ShieldCheck,
  Truck,
} from "lucide-react";

import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  useQuery,
} from "@tanstack/react-query";

import {
  useParams,
} from "react-router";

import {
  ProductCard,
} from "../components/ProductCard";

import {
  CategoryShowcaseSection,
} from "../components/CategoryShowcaseSection";

import {
  PromoBannerSection,
} from "../components/PromoBannerSection";

import {
  StorefrontHeader,
} from "../components/StorefrontHeader";

import {
  PreviewHighlightBridge,
} from "../components/PreviewHighlightBridge";

import {
  loadStorefrontConfig,
} from "../config/storefrontConfig";

import {
  createLiveStorefrontConfig,
} from "../config/liveStorefrontConfig";

import {
  getStorefrontInfo,
  storefrontApiIsConfigured,
} from "../data/storefrontApi";

import {
  createThemeStyle,
  resolveStorefrontConfig,
} from "../theme/themeEngine";

import {
  StorefrontContentProvider,
} from "../data/StorefrontContentProvider";

import {
  useStorefrontContent,
} from "../data/storefrontContent";

import {
  SmartImage,
} from "../components/SmartImage";

type ResolvedConfig =
  ReturnType<
    typeof resolveStorefrontConfig
  >;

export function StorefrontHomePage() {
  const params =
    useParams<{
      storeSlug:
        string;
    }>();

  const storeSlug =
    params.storeSlug ??
    "demo";

  const liveStore =
    storeSlug !==
      "demo";

  const [
    previewConfig,
    setPreviewConfig,
  ] =
    useState(
      () =>
        resolveStorefrontConfig(
          loadStorefrontConfig(),
        ),
    );

  const storeQuery =
    useQuery({
      queryKey: [
        "storefront-runtime",
        storeSlug,
        "info",
      ],

      queryFn: () =>
        getStorefrontInfo(
          storeSlug,
        ),

      enabled:
        liveStore &&
        storefrontApiIsConfigured,

      retry:
        1,

      staleTime:
        5 * 60_000,
    });

  const config =
    useMemo(
      () =>
        liveStore
          ? (
              storeQuery.data
                ? resolveStorefrontConfig(
                    createLiveStorefrontConfig(
                      storeQuery.data,
                    ),
                  )
                : resolveStorefrontConfig(
                    {
                      ...createLiveStorefrontConfig({
                        name:
                          storeSlug,
                        slug:
                          storeSlug,
                        vertical:
                          null,
                        verticalCode:
                          null,
                        presentation: {
                          logoUrl:
                            null,
                          coverImageUrl:
                            null,
                          announcement:
                            null,
                          primaryColor:
                            null,
                          accentColor:
                            null,
                          themePresetCode:
                            "editorial",
                          fontCode:
                            "plex",
                          showCategoriesOnHome:
                            true,
                          showProductsOnHome:
                            true,
                          categorySectionTitle:
                            "تصفح الأقسام",
                          productSectionTitle:
                            "منتجات المتجر",
                        },
                      }),
                    },
                  )
            )
          : previewConfig,
      [
        liveStore,
        previewConfig,
        storeQuery.data,
        storeSlug,
      ],
    );

  useEffect(() => {
    if (liveStore) {
      return;
    }

    function refreshConfig() {
      setPreviewConfig(
        resolveStorefrontConfig(
          loadStorefrontConfig(),
        ),
      );
    }

    function receivePreviewConfig(
      event: MessageEvent,
    ) {
      if (
        event.origin !==
        window.location.origin
      ) {
        return;
      }

      const payload =
        event.data as {
          type?: string;
          config?: ReturnType<
            typeof loadStorefrontConfig
          >;
        };

      if (
        payload.type !==
          "OFOQ_STOREFRONT_PREVIEW_CONFIG" ||
        !payload.config
      ) {
        return;
      }

      setPreviewConfig(
        resolveStorefrontConfig(
          payload.config,
        ),
      );
    }

    window.addEventListener(
      "storage",
      refreshConfig,
    );

    window.addEventListener(
      "focus",
      refreshConfig,
    );

    window.addEventListener(
      "message",
      receivePreviewConfig,
    );

    return () => {
      window.removeEventListener(
        "storage",
        refreshConfig,
      );

      window.removeEventListener(
        "focus",
        refreshConfig,
      );

      window.removeEventListener(
        "message",
        receivePreviewConfig,
      );
    };
  }, [
    liveStore,
  ]);

  const heroVisible =
    Boolean(
      config.hero.title.trim() ||
      config.hero.description.trim() ||
      config.hero.primaryImage.trim() ||
      config.hero.secondaryImage.trim(),
    );

  return (
    <StorefrontContentProvider config={config}>
      <div
      dir="rtl"
      data-store-theme={
        config.themeId
      }
      style={
        createThemeStyle(
          config,
        )
      }
      className="min-h-screen bg-[var(--store-canvas)] font-[var(--store-font)] text-[var(--store-ink)]"
    >
      <PreviewHighlightBridge />

      <StorefrontHeader
        storeName={
          config.storeName
        }
        logoUrl={
          config.logoUrl
        }
        announcement={
          config.announcement
        }
        themeId={
          config.themeId
        }
      />

      <main>
        {heroVisible ? (
          <ThemeHero
            config={config}
          />
        ) : null}

        {config.themeId ===
        "commerce" ? (
          <CommerceBenefits />
        ) : null}

        {config.themeId ===
        "technical" ? (
          <TechnicalTrustBar />
        ) : null}

        {config.sectionOrder.map(
          (section) => {
            if (
              section ===
                "categories" &&
              config.categorySection.enabled
            ) {
              return (
                <CategoryShowcaseSection
                  key="categories"
                  config={config}
                />
              );
            }

            if (
              section ===
                "products" &&
              config.productSection.enabled
            ) {
              return (
                <ThemeProductSection
                  key="products"
                  config={config}
                />
              );
            }

            if (
              section ===
                "banner" &&
              config.bannerSection.enabled
            ) {
              return (
                <PromoBannerSection
                  key="banner"
                  config={config}
                />
              );
            }

            if (
              section ===
                "story" &&
              config.story.enabled
            ) {
              return (
                <ThemeStorySection
                  key="story"
                  config={config}
                />
              );
            }

            return null;
          },
        )}
      </main>

      <ThemeFooter
        config={config}
      />
    </div>
    </StorefrontContentProvider>
  );
}

function ThemeHeroAction({
  config,
  light = false,
}: {
  config: ResolvedConfig;
  light?: boolean;
}) {
  const hero =
    config.hero;

  const isCommerce =
    config.themeId ===
    "commerce";

  const isTechnical =
    config.themeId ===
    "technical";

  if (
    !hero.ctaLabel
  ) {
    return null;
  }

  if (
    isCommerce &&
    !light
  ) {
    return (
      <a
        href={
          hero.ctaHref
        }
        className="mt-8 inline-flex items-center gap-3 rounded-[9px] bg-[var(--store-accent)] px-6 py-3.5 text-[12px] font-semibold text-white"
      >
        {hero.ctaLabel}

        <ArrowLeft
          size={16}
        />
      </a>
    );
  }

  if (
    isTechnical &&
    !light
  ) {
    return (
      <a
        href={
          hero.ctaHref
        }
        className="mt-8 inline-flex items-center gap-3 rounded-[7px] bg-[var(--store-ink)] px-5 py-3 text-[11px] font-semibold text-white"
      >
        {hero.ctaLabel}

        <ArrowLeft
          size={15}
        />
      </a>
    );
  }

  return (
    <a
      href={
        hero.ctaHref
      }
      className={[
        "mt-8 inline-flex items-center gap-3 border-b pb-2 text-[12px] font-semibold",
        light
          ? "border-white/60"
          : "border-black/35",
      ].join(" ")}
    >
      {hero.ctaLabel}

      <ArrowLeft
        size={16}
      />
    </a>
  );
}

function ThemeHeroEyebrow({
  config,
  light = false,
}: {
  config: ResolvedConfig;
  light?: boolean;
}) {
  const hero =
    config.hero;

  if (
    !hero.eyebrow
  ) {
    return null;
  }

  const isMaison =
    config.themeId ===
    "maison";

  const isTechnical =
    config.themeId ===
    "technical";

  const eyebrowClass =
    isMaison
      ? "tracking-[0.08em]"
      : isTechnical
        ? "font-semibold uppercase tracking-[0.08em] text-[var(--store-accent)]"
        : "";

  return (
    <p
      className={[
        "mb-4 text-[11px]",
        eyebrowClass,
        light
          ? "text-white/70"
          : "text-[var(--store-muted)]",
      ].join(" ")}
    >
      {hero.eyebrow}
    </p>
  );
}

function ThemeHero({
  config,
}: {
  config: ResolvedConfig;
}) {
  const hero =
    config.hero;

  const isMaison =
    config.themeId ===
    "maison";

  const isCommerce =
    config.themeId ===
    "commerce";

  const isStudio =
    config.themeId ===
    "studio";

  const isTechnical =
    config.themeId ===
    "technical";

  const containerClass =
    isStudio ||
    isCommerce ||
    isTechnical
      ? "rounded-[var(--store-radius)]"
      : "";

  const headingClass =
    isMaison
      ? "font-normal tracking-[-0.04em]"
      : isCommerce
        ? "font-bold tracking-[-0.055em]"
        : isStudio
          ? "font-semibold tracking-[-0.065em]"
          : isTechnical
            ? "font-semibold tracking-[-0.05em]"
            : "font-medium tracking-[-0.055em]";

  if (
    config.heroLayout ===
    "single"
  ) {
    return (
      <section className="store-container py-6 md:py-10">
        <div
          className={[
            "relative flex min-h-[680px] overflow-hidden bg-[var(--store-soft)]",
            containerClass,
          ].join(" ")}
        >
          <SmartImage
            src={
              hero.primaryImage
            }
            alt={
              hero.title
            }
            className="absolute inset-0 h-full w-full object-cover"
           priority sizes="100vw"/>

          <div className="absolute inset-0 bg-gradient-to-t from-black/65 via-black/10 to-transparent" />

          <div
            className={[
              "relative mt-auto max-w-5xl p-8 text-white md:p-12 lg:p-14",
              isMaison
                ? "mx-auto text-center"
                : "",
            ].join(" ")}
          >
            <ThemeHeroEyebrow
              config={config}
              light
            />

            <h1
              className={[
                "text-[clamp(3rem,6vw,6.4rem)] leading-[1.03]",
                headingClass,
              ].join(" ")}
            >
              {hero.title}
            </h1>

            {hero.description ? (
              <p
                className={[
                  "mt-5 max-w-xl text-[14px] leading-8 text-white/75",
                  isMaison
                    ? "mx-auto"
                    : "",
                ].join(" ")}
              >
                {
                  hero.description
                }
              </p>
            ) : null}

            <ThemeHeroAction
              config={config}
              light
            />
          </div>
        </div>
      </section>
    );
  }

  if (
    config.heroLayout ===
    "wide-portrait"
  ) {
    return (
      <section className="store-container py-6 md:py-10">
        <div className="grid min-h-[680px] gap-4 lg:grid-cols-[1.18fr_.82fr]">
          <div
            className={[
              "relative flex min-h-[560px] overflow-hidden bg-[var(--store-soft)]",
              containerClass,
            ].join(" ")}
          >
            <SmartImage
              src={
                hero.primaryImage
              }
              alt={
                hero.title
              }
              className="absolute inset-0 h-full w-full object-cover"
             priority sizes="100vw"/>

            <div className="absolute inset-0 bg-gradient-to-t from-black/65 via-black/5 to-transparent" />

            <div className="relative mt-auto max-w-4xl p-8 text-white md:p-12">
              <ThemeHeroEyebrow
                config={config}
                light
              />

              <h1
                className={[
                  "text-[clamp(2.8rem,5vw,5.8rem)] leading-[1.04]",
                  headingClass,
                ].join(" ")}
              >
                {hero.title}
              </h1>

              <ThemeHeroAction
                config={config}
                light
              />
            </div>
          </div>

          <div
            className={[
              "relative min-h-[440px] overflow-hidden bg-[var(--store-soft)]",
              containerClass,
            ].join(" ")}
          >
            <SmartImage
              src={
                hero.secondaryImage
              }
              alt=""
              className="absolute inset-0 h-full w-full object-cover"
             loading="eager" sizes="100vw"/>

            {isStudio ? (
              <div className="absolute inset-x-5 bottom-5 rounded-[18px] bg-[var(--store-canvas)]/92 p-5 backdrop-blur">
                <p className="text-[11px] text-[var(--store-muted)]">
                  {hero.eyebrow}
                </p>

                <p className="mt-2 text-[18px] font-semibold leading-7">
                  {
                    hero.description
                  }
                </p>
              </div>
            ) : null}
          </div>
        </div>
      </section>
    );
  }

  if (
    config.heroLayout ===
    "equal"
  ) {
    return (
      <section className="store-container py-6 md:py-10">
        <div className="grid min-h-[650px] gap-4 lg:grid-cols-2">
          <div
            className={[
              "relative flex min-h-[500px] overflow-hidden bg-[var(--store-soft)]",
              containerClass,
            ].join(" ")}
          >
            <SmartImage
              src={
                hero.primaryImage
              }
              alt={
                hero.title
              }
              className="absolute inset-0 h-full w-full object-cover"
             priority sizes="100vw"/>

            <div className="absolute inset-0 bg-gradient-to-t from-black/65 via-black/5 to-transparent" />

            <div className="relative mt-auto p-8 text-white md:p-11">
              <ThemeHeroEyebrow
                config={config}
                light
              />

              <h1
                className={[
                  "text-[clamp(2.6rem,4.5vw,5rem)] leading-[1.05]",
                  headingClass,
                ].join(" ")}
              >
                {hero.title}
              </h1>

              <ThemeHeroAction
                config={config}
                light
              />
            </div>
          </div>

          <div
            className={[
              "relative min-h-[500px] overflow-hidden bg-[var(--store-soft)]",
              containerClass,
            ].join(" ")}
          >
            <SmartImage
              src={
                hero.secondaryImage
              }
              alt=""
              className="absolute inset-0 h-full w-full object-cover"
             loading="eager" sizes="100vw"/>
          </div>
        </div>
      </section>
    );
  }

  if (
    config.heroLayout ===
    "focus-one"
  ) {
    return (
      <section className="store-container py-6 md:py-12">
        <div
          className={[
            "grid min-h-[620px] overflow-hidden bg-[var(--store-surface)] lg:grid-cols-[.9fr_1.1fr]",
            containerClass,
            isTechnical
              ? "border border-black/10"
              : "",
          ].join(" ")}
        >
          <div className="flex items-center p-8 md:p-12 lg:p-14">
            <div className="max-w-xl">
              <ThemeHeroEyebrow
                config={config}
              />

              <h1
                className={[
                  "text-[clamp(2.7rem,4.8vw,5.3rem)] leading-[1.07]",
                  headingClass,
                ].join(" ")}
              >
                {hero.title}
              </h1>

              {hero.description ? (
                <p className="mt-6 max-w-lg text-[14px] leading-8 text-[var(--store-ink-soft)]">
                  {
                    hero.description
                  }
                </p>
              ) : null}

              {isTechnical ? (
                <div className="mt-6 flex flex-wrap gap-2">
                  <TechnicalChip>
                    معلومات واضحة
                  </TechnicalChip>

                  <TechnicalChip>
                    مواصفات منظمة
                  </TechnicalChip>

                  <TechnicalChip>
                    دعم موثوق
                  </TechnicalChip>
                </div>
              ) : null}

              <ThemeHeroAction
                config={config}
              />
            </div>
          </div>

          <div className="bg-[var(--store-soft)]">
            <SmartImage
              src={
                hero.primaryImage
              }
              alt={
                hero.title
              }
              className="h-full min-h-[480px] w-full object-cover"
             priority sizes="100vw"/>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="store-container py-6 md:py-12">
      <div
        className={[
          "grid min-h-[620px] overflow-hidden bg-[var(--store-surface)] lg:grid-cols-[.75fr_1.25fr]",
          containerClass,
          isTechnical
            ? "border border-black/10"
            : "",
        ].join(" ")}
      >
        <div className="flex items-center p-8 md:p-12">
          <div className="max-w-lg">
            <ThemeHeroEyebrow
              config={config}
            />

            <h1
              className={[
                "text-[clamp(2.5rem,4.2vw,4.8rem)] leading-[1.07]",
                headingClass,
              ].join(" ")}
            >
              {hero.title}
            </h1>

            {hero.description ? (
              <p className="mt-5 text-[14px] leading-8 text-[var(--store-ink-soft)]">
                {
                  hero.description
                }
              </p>
            ) : null}

            <ThemeHeroAction
              config={config}
            />
          </div>
        </div>

        <div className="grid grid-cols-2">
          <SmartImage
            src={
              hero.primaryImage
            }
            alt={
              hero.title
            }
            className="h-full min-h-[480px] w-full object-cover"
           priority sizes="100vw"/>

          <SmartImage
            src={
              hero.secondaryImage
            }
            alt=""
            className="h-full min-h-[480px] w-full object-cover"
           loading="eager" sizes="100vw"/>
        </div>
      </div>
    </section>
  );
}
function ThemeProductSection({
  config,
}: {
  config: ResolvedConfig;
}) {
  const {
    products,
  } =
    useStorefrontContent();

  if (
    products.length ===
    0
  ) {
    return null;
  }

  if (
    config.productSection.layout !==
    "theme-default"
  ) {
    return (
      <CustomProductLayout
        config={config}
      />
    );
  }

  switch (
    config.themeId
  ) {
    case "maison":
      return (
        <MaisonProducts
          config={config}
        />
      );

    case "commerce":
      return (
        <CommerceProducts
          config={config}
        />
      );

    case "studio":
      return (
        <StudioProducts
          config={config}
        />
      );

    case "technical":
      return (
        <TechnicalProducts
          config={config}
        />
      );

    case "editorial":
    default:
      return (
        <EditorialProducts
          config={config}
        />
      );
  }
}

function EditorialProducts({
  config,
}: {
  config: ResolvedConfig;
}) {
  const {
    products,
  } =
    useStorefrontContent();

  return (
    <section
      id="products"
      className="store-container py-24 md:py-32"
    >
      <SectionHeading
        config={config}
      />

      <div className="grid grid-cols-2 gap-x-4 gap-y-12 md:grid-cols-3 lg:grid-cols-4">
        {products.map(
          (product) => (
            <ProductCard
              key={product.name}
              {...product}
              variant={
                config.productCardStyle
              }
            />
          ),
        )}
      </div>
    </section>
  );
}

function MaisonProducts({
  config,
}: {
  config: ResolvedConfig;
}) {
  const {
    products,
  } =
    useStorefrontContent();

  return (
    <section
      id="products"
      className="store-container py-28 md:py-40"
    >
      <div className="mb-16 text-center">
        <p className="text-[10px] tracking-[0.08em] text-[var(--store-muted)]">
          {
            config.productSection.eyebrow
          }
        </p>

        <h2 className="mx-auto mt-4 max-w-3xl text-[clamp(2rem,3.2vw,3.8rem)] font-normal tracking-[-0.035em]">
          {
            config.productSection.title
          }
        </h2>

        <div className="mx-auto mt-7 h-px w-14 bg-[var(--store-ink)]/30" />
      </div>

      <div className="grid grid-cols-2 gap-x-6 gap-y-16 md:grid-cols-3">
        {products
          .slice(0, 3)
          .map(
            (product) => (
              <ProductCard
                key={product.name}
                {...product}
                variant={
                  config.productCardStyle
                }
              />
            ),
          )}
      </div>
    </section>
  );
}

function CommerceProducts({
  config,
}: {
  config: ResolvedConfig;
}) {
  const {
    products,
  } =
    useStorefrontContent();

  return (
    <section
      id="products"
      className="store-container py-16 md:py-20"
    >
      <div className="mb-7 flex flex-wrap items-end justify-between gap-5">
        <div>
          <p className="text-[11px] font-semibold text-[var(--store-accent)]">
            {
              config.productSection.eyebrow
            }
          </p>

          <h2 className="mt-2 text-[clamp(1.8rem,2.5vw,3rem)] font-bold tracking-[-0.04em]">
            {
              config.productSection.title
            }
          </h2>
        </div>

        <div className="flex gap-2 text-[10px]">
          <button className="rounded-full bg-[var(--store-ink)] px-4 py-2 text-white">
            الكل
          </button>

          <button className="rounded-full border border-black/10 bg-white px-4 py-2">
            جديد
          </button>

          <button className="rounded-full border border-black/10 bg-white px-4 py-2">
            عروض
          </button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4">
        {products.map(
          (product) => (
            <ProductCard
              key={product.name}
              {...product}
              variant={
                config.productCardStyle
              }
            />
          ),
        )}
      </div>
    </section>
  );
}

function StudioProducts({
  config,
}: {
  config: ResolvedConfig;
}) {
  const {
    products,
  } =
    useStorefrontContent();

  return (
    <section
      id="products"
      className="store-container py-20 md:py-28"
    >
      <SectionHeading
        config={config}
      />

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {products.map(
          (
            product,
            index,
          ) => (
            <div
              key={product.name}
              className={
                index === 0
                  ? "lg:col-span-2 lg:row-span-2"
                  : ""
              }
            >
              <ProductCard
                {...product}
                variant={
                  config.productCardStyle
                }
              />
            </div>
          ),
        )}
      </div>
    </section>
  );
}

function TechnicalProducts({
  config,
}: {
  config: ResolvedConfig;
}) {
  const {
    products,
  } =
    useStorefrontContent();

  return (
    <section
      id="products"
      className="store-container py-16 md:py-20"
    >
      <div className="mb-8 border-b border-black/10 pb-6">
        <p className="text-[10px] font-semibold uppercase tracking-[0.08em] text-[var(--store-accent)]">
          {
            config.productSection.eyebrow
          }
        </p>

        <div className="mt-2 flex flex-wrap items-end justify-between gap-4">
          <h2 className="text-[clamp(1.8rem,2.5vw,3rem)] font-semibold tracking-[-0.045em]">
            {
              config.productSection.title
            }
          </h2>

          <span className="text-[11px] text-[var(--store-muted)]">
            4 منتجات
          </span>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4">
        {products.map(
          (product) => (
            <ProductCard
              key={product.name}
              {...product}
              variant={
                config.productCardStyle
              }
            />
          ),
        )}
      </div>
    </section>
  );
}

function ThemeStorySection({
  config,
}: {
  config: ResolvedConfig;
}) {
  if (
    config.story.layout !==
    "theme-default"
  ) {
    return (
      <CustomStoryLayout
        config={config}
      />
    );
  }

  switch (
    config.themeId
  ) {
    case "maison":
      return (
        <section
          id="story"
          className="border-y border-black/[0.08] py-28 md:py-40"
        >
          <div className="store-container text-center">
            <p className="text-[10px] tracking-[0.1em] text-[var(--store-muted)]">
              {config.story.eyebrow}
            </p>

            <h2 className="mx-auto mt-5 max-w-4xl text-[clamp(2.2rem,4vw,4.5rem)] font-normal leading-[1.2] tracking-[-0.04em]">
              {config.story.title}
            </h2>

            <p className="mx-auto mt-7 max-w-xl text-[14px] leading-8 text-[var(--store-ink-soft)]">
              {config.story.body}
            </p>

            <a
              href={
                config.story.ctaHref
              }
              className="mt-9 inline-flex border-b border-black/40 pb-2 text-[11px]"
            >
              {config.story.ctaLabel}
            </a>
          </div>
        </section>
      );

    case "commerce":
      return (
        <section
          id="story"
          className="store-container pb-20 pt-10"
        >
          <div className="grid overflow-hidden rounded-[var(--store-radius)] bg-[var(--store-soft)] md:grid-cols-2">
            <SmartImage
              src={
                config.story.image
              }
              alt={
                config.story.title
              }
              className="h-full min-h-[380px] w-full object-cover"
            />

            <div className="flex items-center p-8 md:p-12">
              <div>
                <p className="text-[11px] font-semibold text-[var(--store-accent)]">
                  {
                    config.story.eyebrow
                  }
                </p>

                <h2 className="mt-3 text-[clamp(2rem,3vw,3.5rem)] font-bold leading-[1.15] tracking-[-0.045em]">
                  {
                    config.story.title
                  }
                </h2>

                <p className="mt-5 text-[14px] leading-8 text-[var(--store-ink-soft)]">
                  {
                    config.story.body
                  }
                </p>
              </div>
            </div>
          </div>
        </section>
      );

    case "studio":
      return (
        <section
          id="story"
          className="store-container py-20 md:py-28"
        >
          <div className="relative">
            <div className="mr-auto w-full overflow-hidden rounded-[var(--store-radius)] lg:w-[74%]">
              <SmartImage
                src={
                  config.story.image
                }
                alt={
                  config.story.title
                }
                className="aspect-[16/10] h-full w-full object-cover"
              />
            </div>

            <div className="relative -mt-14 ml-auto max-w-xl rounded-[var(--store-radius)] bg-[var(--store-ink)] p-8 text-white md:p-10 lg:absolute lg:bottom-10 lg:left-0 lg:mt-0">
              <p className="text-[10px] text-white/55">
                {
                  config.story.eyebrow
                }
              </p>

              <h2 className="mt-4 text-[clamp(2rem,3vw,3.4rem)] font-semibold leading-[1.15] tracking-[-0.05em]">
                {
                  config.story.title
                }
              </h2>

              <p className="mt-5 text-[13px] leading-7 text-white/70">
                {
                  config.story.body
                }
              </p>
            </div>
          </div>
        </section>
      );

    case "technical":
      return (
        <section
          id="story"
          className="store-container py-16 md:py-20"
        >
          <div className="grid gap-8 border-t border-black/10 pt-12 lg:grid-cols-[.8fr_1.2fr]">
            <div>
              <p className="text-[10px] font-semibold uppercase tracking-[0.08em] text-[var(--store-accent)]">
                {
                  config.story.eyebrow
                }
              </p>

              <h2 className="mt-4 text-[clamp(2rem,3vw,3.4rem)] font-semibold leading-[1.15] tracking-[-0.045em]">
                {
                  config.story.title
                }
              </h2>

              <p className="mt-5 text-[14px] leading-8 text-[var(--store-ink-soft)]">
                {
                  config.story.body
                }
              </p>

              <div className="mt-7 space-y-3">
                <TechnicalLine>
                  معلومات واضحة قبل الشراء
                </TechnicalLine>

                <TechnicalLine>
                  مواصفات منظمة
                </TechnicalLine>

                <TechnicalLine>
                  دعم ومتابعة
                </TechnicalLine>
              </div>
            </div>

            <SmartImage
              src={
                config.story.image
              }
              alt={
                config.story.title
              }
              className="aspect-[16/9] h-full w-full rounded-[var(--store-radius)] object-cover"
            />
          </div>
        </section>
      );

    case "editorial":
    default:
      return (
        <section
          id="story"
          className="bg-[var(--store-soft)] py-24 md:py-32"
        >
          <div className="store-container grid gap-12 lg:grid-cols-[.8fr_1.2fr] lg:items-center">
            <div>
              <p className="mb-4 text-[12px] text-[var(--store-muted)]">
                {
                  config.story.eyebrow
                }
              </p>

              <h2 className="text-[clamp(1.8rem,3vw,3.2rem)] font-medium leading-[1.25] tracking-[-0.04em]">
                {
                  config.story.title
                }
              </h2>

              <p className="mt-6 max-w-lg text-[14px] leading-8 text-[var(--store-ink-soft)]">
                {
                  config.story.body
                }
              </p>

              <a
                href={
                  config.story.ctaHref
                }
                className="mt-8 inline-flex items-center gap-2 border-b border-current/40 pb-2 text-[12px] font-semibold"
              >
                {
                  config.story.ctaLabel
                }

                <ArrowLeft size={16} />
              </a>
            </div>

            <div className="aspect-[16/10] overflow-hidden">
              <SmartImage
                src={
                  config.story.image
                }
                alt={
                  config.story.title
                }
                className="h-full w-full object-cover"
              />
            </div>
          </div>
        </section>
      );
  }
}

function CustomProductLayout({
  config,
}: {
  config: ResolvedConfig;
}) {
  const {
    products,
  } =
    useStorefrontContent();

  const layout =
    config.productSection.layout;

  if (
    layout ===
    "grid-3"
  ) {
    return (
      <section
        id="products"
        className="store-container py-20 md:py-28"
      >
        <SectionHeading
          config={config}
        />

        <div className="grid grid-cols-2 gap-x-4 gap-y-10 md:grid-cols-3 md:gap-x-6">
          {products.map(
            (product) => (
              <ProductCard
                key={product.name}
                {...product}
                variant={
                  config.productCardStyle
                }
              />
            ),
          )}
        </div>
      </section>
    );
  }

  if (
    layout ===
    "featured-grid"
  ) {
    const [
      featured,
      ...rest
    ] = products;

    return (
      <section
        id="products"
        className="store-container py-20 md:py-28"
      >
        <SectionHeading
          config={config}
        />

        <div className="grid gap-5 lg:grid-cols-2">
          <div className="min-w-0">
            <ProductCard
              {...featured}
              variant={
                config.productCardStyle
              }
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            {rest.map(
              (product) => (
                <ProductCard
                  key={product.name}
                  {...product}
                  variant={
                    config.productCardStyle
                  }
                />
              ),
            )}
          </div>
        </div>
      </section>
    );
  }

  if (
    layout ===
    "horizontal"
  ) {
    return (
      <section
        id="products"
        className="py-20 md:py-28"
      >
        <div className="store-container">
          <SectionHeading
            config={config}
          />
        </div>

        <div className="store-container overflow-x-auto pb-4">
          <div className="flex gap-4">
            {products.map(
              (product) => (
                <div
                  key={product.name}
                  className="w-[76vw] max-w-[340px] shrink-0 md:w-[320px]"
                >
                  <ProductCard
                    {...product}
                    variant={
                      config.productCardStyle
                    }
                  />
                </div>
              ),
            )}
          </div>
        </div>
      </section>
    );
  }

  if (
    layout ===
    "spotlight"
  ) {
    const [
      featured,
      ...rest
    ] = products;

    return (
      <section
        id="products"
        className="store-container py-20 md:py-28"
      >
        <SectionHeading
          config={config}
        />

        <div className="grid overflow-hidden rounded-[var(--store-radius)] bg-[var(--store-surface)] lg:grid-cols-[1.15fr_.85fr]">
          <div className="bg-[var(--store-soft)]">
            <SmartImage
              src={
                featured.image
              }
              alt={
                featured.name
              }
              className="h-full min-h-[520px] w-full object-cover"
            />
          </div>

          <div className="flex flex-col justify-between p-7 md:p-10">
            <div>
              <p className="text-[11px] text-[var(--store-muted)]">
                {
                  featured.category
                }
              </p>

              <h3 className="mt-3 text-[clamp(2rem,3vw,3.7rem)] font-semibold tracking-[-0.045em]">
                {
                  featured.name
                }
              </h3>

              <p className="mt-3 text-[17px] font-semibold">
                {
                  featured.price
                }
              </p>

              <button
                type="button"
                className="mt-7 bg-[var(--store-ink)] px-6 py-3 text-[11px] font-semibold text-white"
              >
                عرض المنتج
              </button>
            </div>

            <div className="mt-10 grid grid-cols-3 gap-3">
              {rest.map(
                (product) => (
                  <div
                    key={product.name}
                    className="min-w-0"
                  >
                    <div className="aspect-square overflow-hidden bg-[var(--store-soft)]">
                      <SmartImage
                        src={
                          product.image
                        }
                        alt={
                          product.name
                        }
                        className="h-full w-full object-cover"
                      />
                    </div>

                    <p className="mt-2 truncate text-[10px] font-semibold">
                      {
                        product.name
                      }
                    </p>
                  </div>
                ),
              )}
            </div>
          </div>
        </div>
      </section>
    );
  }

  return null;
}

function CustomStoryLayout({
  config,
}: {
  config: ResolvedConfig;
}) {
  const story =
    config.story;

  if (
    story.layout ===
    "centered"
  ) {
    return (
      <section
        id="story"
        className="py-24 md:py-32"
      >
        <div className="store-container text-center">
          <p className="text-[11px] text-[var(--store-muted)]">
            {story.eyebrow}
          </p>

          <h2 className="mx-auto mt-4 max-w-4xl text-[clamp(2rem,4vw,4.5rem)] font-medium leading-[1.2] tracking-[-0.045em]">
            {story.title}
          </h2>

          <p className="mx-auto mt-6 max-w-2xl text-[14px] leading-8 text-[var(--store-ink-soft)]">
            {story.body}
          </p>

          {story.ctaLabel ? (
            <a
              href={
                story.ctaHref
              }
              className="mt-8 inline-flex items-center gap-2 border-b border-black/30 pb-2 text-[11px] font-semibold"
            >
              {story.ctaLabel}

              <ArrowLeft
                size={14}
              />
            </a>
          ) : null}

          <div className="mx-auto mt-12 max-w-[1100px] overflow-hidden rounded-[var(--store-radius)]">
            <SmartImage
              src={
                story.image
              }
              alt={
                story.title
              }
              className="aspect-[16/8] h-full w-full object-cover"
            />
          </div>
        </div>
      </section>
    );
  }

  if (
    story.layout ===
    "full-bleed"
  ) {
    return (
      <section
        id="story"
        className="store-container py-16 md:py-24"
      >
        <div className="relative flex min-h-[650px] overflow-hidden rounded-[var(--store-radius)]">
          <SmartImage
            src={
              story.image
            }
            alt={
              story.title
            }
            className="absolute inset-0 h-full w-full object-cover"
          />

          <div className="absolute inset-0 bg-gradient-to-t from-black/65 via-black/15 to-transparent" />

          <div className="relative mt-auto max-w-3xl p-8 text-white md:p-12">
            <p className="text-[11px] text-white/70">
              {story.eyebrow}
            </p>

            <h2 className="mt-4 text-[clamp(2.3rem,4vw,4.8rem)] font-medium leading-[1.15] tracking-[-0.045em]">
              {story.title}
            </h2>

            <p className="mt-5 max-w-xl text-[14px] leading-8 text-white/75">
              {story.body}
            </p>

            {story.ctaLabel ? (
              <a
                href={
                  story.ctaHref
                }
                className="mt-7 inline-flex items-center gap-2 border-b border-white/50 pb-2 text-[11px] font-semibold"
              >
                {story.ctaLabel}

                <ArrowLeft
                  size={14}
                />
              </a>
            ) : null}
          </div>
        </div>
      </section>
    );
  }

  const imageFirst =
    story.layout ===
    "split-start";

  return (
    <section
      id="story"
      className="bg-[var(--store-soft)] py-20 md:py-28"
    >
      <div className="store-container grid items-center gap-10 lg:grid-cols-2">
        <div
          className={
            imageFirst
              ? "lg:order-1"
              : "lg:order-2"
          }
        >
          <SmartImage
            src={
              story.image
            }
            alt={
              story.title
            }
            className="aspect-[4/3] h-full w-full rounded-[var(--store-radius)] object-cover"
          />
        </div>

        <div
          className={[
            "max-w-xl",
            imageFirst
              ? "lg:order-2"
              : "lg:order-1",
          ].join(" ")}
        >
          <p className="text-[11px] text-[var(--store-muted)]">
            {story.eyebrow}
          </p>

          <h2 className="mt-4 text-[clamp(2rem,3.4vw,4rem)] font-medium leading-[1.2] tracking-[-0.045em]">
            {story.title}
          </h2>

          <p className="mt-6 text-[14px] leading-8 text-[var(--store-ink-soft)]">
            {story.body}
          </p>

          {story.ctaLabel ? (
            <a
              href={
                story.ctaHref
              }
              className="mt-8 inline-flex items-center gap-2 border-b border-black/30 pb-2 text-[11px] font-semibold"
            >
              {story.ctaLabel}

              <ArrowLeft
                size={14}
              />
            </a>
          ) : null}
        </div>
      </div>
    </section>
  );
}

function CommerceBenefits() {
  return (
    <section className="store-container">
      <div className="grid grid-cols-2 gap-px overflow-hidden rounded-[var(--store-radius)] border border-black/[0.08] bg-black/[0.08] md:grid-cols-4">
        <Benefit
          icon={Truck}
          title="شحن سريع"
          body="توصيل موثوق"
        />

        <Benefit
          icon={RefreshCw}
          title="استبدال سهل"
          body="إجراءات واضحة"
        />

        <Benefit
          icon={ShieldCheck}
          title="دفع آمن"
          body="حماية للطلبات"
        />

        <Benefit
          icon={PackageCheck}
          title="منتجات مختارة"
          body="جودة موثوقة"
        />
      </div>
    </section>
  );
}

function TechnicalTrustBar() {
  return (
    <section className="store-container">
      <div className="grid border-y border-black/10 md:grid-cols-3">
        <TechnicalTrust
          title="معلومات دقيقة"
          body="التفاصيل والمواصفات في مكان واحد"
        />

        <TechnicalTrust
          title="توافق واضح"
          body="اعرف ما يناسب احتياجك قبل الطلب"
        />

        <TechnicalTrust
          title="دعم موثوق"
          body="متابعة قبل وبعد الشراء"
        />
      </div>
    </section>
  );
}

function ThemeFooter({
  config,
}: {
  config: ResolvedConfig;
}) {
  if (
    config.themeId ===
    "maison"
  ) {
    return (
      <footer className="border-t border-black/[0.08] py-20">
        <div className="store-container text-center">
          <div className="text-[25px] tracking-[0.12em]">
            {config.storeName}
          </div>

          <div className="mt-8 flex flex-wrap justify-center gap-8 text-[10px] text-[var(--store-muted)]">
            <a href="#">
              المجموعة
            </a>

            <a href="#">
              التواصل
            </a>

            <a href="#">
              التوصيل
            </a>

            <a href="#">
              سياسة الاستبدال
            </a>
          </div>
        </div>
      </footer>
    );
  }

  if (
    config.themeId ===
    "commerce"
  ) {
    return (
      <footer className="bg-[var(--store-ink)] py-14 text-white">
        <div className="store-container grid gap-10 md:grid-cols-3">
          <div>
            <div className="text-[23px] font-bold">
              {config.storeName}
            </div>

            <p className="mt-3 text-[12px] text-white/60">
              تجربة شراء سريعة وواضحة
            </p>
          </div>

          <div className="text-[11px] leading-8 text-white/70">
            المنتجات
            <br />
            العروض
            <br />
            الطلبات
          </div>

          <div className="text-[11px] leading-8 text-white/70">
            المساعدة
            <br />
            التوصيل
            <br />
            الاستبدال
          </div>
        </div>
      </footer>
    );
  }

  return (
    <footer className="border-t border-black/[0.08] py-16">
      <div className="store-container">
        <div className="text-[23px] font-bold">
          {config.storeName}
        </div>

        <p className="mt-3 text-[12px] text-[var(--store-ink-soft)]">
          متجر مبني على OFOQ Commerce
        </p>
      </div>
    </footer>
  );
}

function SectionHeading({
  config,
}: {
  config: ResolvedConfig;
}) {
  return (
    <div className="mb-10 flex items-end justify-between gap-8">
      <div>
        <p className="mb-3 text-[11px] text-[var(--store-muted)]">
          {
            config.productSection.eyebrow
          }
        </p>

        <h2 className="text-[clamp(1.8rem,3vw,3rem)] font-medium tracking-[-0.04em]">
          {
            config.productSection.title
          }
        </h2>
      </div>

      <a
        href="#"
        className="hidden items-center gap-2 border-b border-black/25 pb-1 text-[11px] md:flex"
      >
        مشاهدة الكل

        <ArrowLeft size={14} />
      </a>
    </div>
  );
}

function TechnicalChip({
  children,
}: {
  children:
    React.ReactNode;
}) {
  return (
    <span className="rounded-[5px] border border-black/10 bg-[var(--store-soft)] px-3 py-2 text-[10px] font-medium">
      {children}
    </span>
  );
}

function TechnicalLine({
  children,
}: {
  children:
    React.ReactNode;
}) {
  return (
    <div className="flex items-center gap-3 text-[12px] text-[var(--store-ink-soft)]">
      <span className="flex size-5 items-center justify-center rounded-full bg-[var(--store-soft)]">
        <Check
          size={12}
          strokeWidth={2}
        />
      </span>

      {children}
    </div>
  );
}

function Benefit({
  icon: Icon,
  title,
  body,
}: {
  icon:
    React.ComponentType<{
      size?: number;
      strokeWidth?: number;
    }>;
  title: string;
  body: string;
}) {
  return (
    <div className="bg-[var(--store-surface)] p-5">
      <Icon
        size={20}
        strokeWidth={1.7}
      />

      <p className="mt-4 text-[12px] font-semibold">
        {title}
      </p>

      <p className="mt-1 text-[10px] text-[var(--store-muted)]">
        {body}
      </p>
    </div>
  );
}

function TechnicalTrust({
  title,
  body,
}: {
  title: string;
  body: string;
}) {
  return (
    <div className="border-black/10 py-5 md:border-l md:px-6 md:last:border-l-0">
      <p className="text-[11px] font-semibold">
        {title}
      </p>

      <p className="mt-1 text-[10px] text-[var(--store-muted)]">
        {body}
      </p>
    </div>
  );
}