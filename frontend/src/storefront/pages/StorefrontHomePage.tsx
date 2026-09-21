import {
  ArrowLeft,
  BatteryCharging,
  Camera,
  Check,
  PackageCheck,
  RefreshCw,
  ShieldCheck,
  Smartphone,
  Truck,
  Wifi,
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
  Link,
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
  StorefrontFooter,
} from "../components/StorefrontFooter";

import {
  PreviewHighlightBridge,
} from "../components/PreviewHighlightBridge";

import {
  loadStorefrontConfig,
} from "../config/storefrontConfig";

import { parseVisualContent } from "../config/visualContent";

import {
  createLiveStorefrontConfig,
} from "../config/liveStorefrontConfig";

import {
  EMPTY_STOREFRONT_CONTACT,
  type StorefrontInfo,
  getStorefrontInfo,
  storefrontApiIsConfigured,
} from "../data/storefrontApi";

import {
  createThemeStyle,
  resolveStorefrontConfig,
} from "../theme/themeEngine";

import {
  getThemePreset,
} from "../theme/themePresets";

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
  const inVisualEditor = liveStore &&
    new URLSearchParams(window.location.search).get("builderPreview") === "1" &&
    window.parent !== window;
  const [draftPresentation, setDraftPresentation] =
    useState<StorefrontInfo["presentation"] | null>(null);

  useEffect(() => {
    if (!inVisualEditor) return;
    function receiveDraft(event: MessageEvent) {
      if (event.origin !== window.location.origin || event.source !== window.parent ||
          event.data?.type !== "RUKN_VISUAL_DRAFT") return;
      const presentation = event.data.presentation;
      if (!presentation || typeof presentation !== "object" ||
          typeof presentation.themePresetCode !== "string" ||
          typeof presentation.fontCode !== "string" ||
          typeof presentation.visualContentJson !== "string") return;
      setDraftPresentation(presentation as StorefrontInfo["presentation"]);
    }
    window.addEventListener("message", receiveDraft);
    return () => window.removeEventListener("message", receiveDraft);
  }, [inVisualEditor]);

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
                      inVisualEditor && draftPresentation
                        ? { ...storeQuery.data, presentation: { ...storeQuery.data.presentation, ...draftPresentation } }
                        : storeQuery.data,
                    ),
                  )
                : resolveStorefrontConfig(
                    {
                      ...createLiveStorefrontConfig({
                        tenantId: "",
                        name:
                          storeSlug,
                        slug:
                          storeSlug,
                        vertical:
                          null,
                        verticalCode:
                          null,
                        contact: {
                          websiteUrl: null,
                          whatsAppNumber: null,
                          customerServicePhone: null,
                          secondaryPhone: null,
                          landlinePhone: null,
                          physicalAddress: null,
                          googleMapsUrl: null,
                          commercialRegistrationNumber: null,
                          socialLinks: [],
                        },
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
        inVisualEditor,
        draftPresentation,
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

  const activeTheme =
    getThemePreset(config.themeId);

  const heroVisible =
    Boolean(activeTheme.experience) ||
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
      className="min-h-screen bg-[var(--store-canvas)] font-[var(--store-font)] text-[var(--store-body-text)]"
    >
      <PreviewHighlightBridge />

      <StorefrontHeader
        storeSlug={storeSlug}
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

      <main data-rukn-home="1">
        {heroVisible ? (
          <ThemeHero
            config={config}
          />
        ) : null}

        {config.themeId ===
        "mobile-flagship" ? (
          <MobileFlagshipBand config={config} />
        ) : null}

        {config.themeId ===
        "mobile-smart-market" ? (
          <MobileSmartMarketBand config={config} />
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

      <StorefrontFooter
        storeSlug={storeSlug}
        storeName={config.storeName}
        logoUrl={config.logoUrl}
        themeId={config.themeId}
        contact={storeQuery.data?.contact ?? EMPTY_STOREFRONT_CONTACT}
        description={parseVisualContent(inVisualEditor && draftPresentation ? draftPresentation.visualContentJson : storeQuery.data?.presentation.visualContentJson).footerDescription}
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
        className="mt-8 inline-flex items-center gap-3 rounded-[9px] bg-[var(--store-accent)] px-6 py-3.5 text-[12px] font-semibold text-[var(--store-accent-contrast)]"
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
        className="mt-8 inline-flex items-center gap-3 rounded-[7px] bg-[var(--store-button-bg)] px-5 py-3 text-[11px] font-semibold text-[var(--store-button-text)]"
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

  if (
    config.themeId ===
    "mobile-flagship"
  ) {
    return (
      <MobileFlagshipHero
        config={config}
      />
    );
  }

  if (
    config.themeId ===
    "mobile-smart-market"
  ) {
    return (
      <MobileSmartMarketHero
        config={config}
      />
    );
  }

  const activeTheme =
    getThemePreset(config.themeId);

  if (activeTheme.experience === "signature") {
    return (
      <VerticalSignatureHero
        config={config}
      />
    );
  }

  if (activeTheme.experience === "market") {
    return (
      <VerticalMarketHero
        config={config}
      />
    );
  }

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
function VerticalSignatureHero({
  config,
}: {
  config: ResolvedConfig;
}) {
  const theme = getThemePreset(config.themeId);
  const hero = config.hero;
  const image = hero.primaryImage.trim() || config.coverImageUrl.trim();
  const title = hero.title.trim() || theme.defaultHeroTitle || `اكتشف ${theme.productLabel ?? "المنتجات"}`;
  const description = hero.description.trim() || theme.defaultHeroDescription || theme.description;
  const ctaLabel = hero.ctaLabel.trim() || `استكشف ${theme.productLabel ?? "المنتجات"}`;
  const ctaHref = hero.ctaHref.trim() || "#products";
  const features = theme.featureLabels ?? ["اختيار أوضح", "صور أكبر", "تفاصيل مرتبة"];

  return (
    <section className="store-container py-6 md:py-10">
      <div className="vertical-signature-hero overflow-hidden rounded-[calc(var(--store-radius)+8px)] border border-black/[0.065] bg-[var(--store-surface)] shadow-[0_30px_85px_rgba(16,18,18,.08)]">
        <div className="grid min-h-[650px] lg:grid-cols-[.9fr_1.1fr]">
          <div className="relative flex items-center p-7 md:p-11 lg:p-14 xl:p-16">
            <div className="absolute inset-y-16 right-0 hidden w-px bg-gradient-to-b from-transparent via-black/10 to-transparent lg:block" />
            <div className="relative z-10 max-w-[690px] vertical-theme-reveal">
              <div className="mb-7 flex flex-wrap items-center gap-3">
                <span className="rounded-full border border-black/[0.08] bg-[var(--store-canvas)] px-4 py-2 text-[10px] font-semibold text-[var(--store-ink-soft)]">
                  {theme.verticalLabel ?? config.storeName}
                </span>
                <span dir="ltr" className="text-[10px] font-bold tracking-[0.14em] text-[var(--store-accent)]">
                  {theme.name.toUpperCase()}
                </span>
              </div>

              <h1 className="max-w-[780px] text-[clamp(3.1rem,5.8vw,6.8rem)] font-semibold leading-[1] tracking-[-0.07em]">
                {title}
              </h1>

              <p className="mt-7 max-w-xl text-[15px] leading-8 text-[var(--store-ink-soft)] md:text-[17px] md:leading-9">
                {description}
              </p>

              <div className="mt-9 flex flex-wrap items-center gap-3">
                <a href={ctaHref} className="inline-flex h-14 items-center gap-3 rounded-full bg-[var(--store-button-bg)] px-7 text-[12px] font-semibold text-[var(--store-button-text)] transition hover:-translate-y-0.5 hover:shadow-[0_14px_28px_rgba(16,17,20,.13)]">
                  {ctaLabel}
                  <ArrowLeft size={16} />
                </a>
                <a href="#categories" className="inline-flex h-14 items-center rounded-full border border-black/[0.09] bg-[var(--store-surface)] px-6 text-[12px] font-semibold text-[var(--store-ink-soft)] transition hover:border-black/20">
                  {theme.categoryLabel ?? "تصفح الأقسام"}
                </a>
              </div>

              <div className="mt-10 grid max-w-xl grid-cols-3 border-t border-black/[0.08] pt-6">
                {features.slice(0, 3).map((label, index) => (
                  <FlagshipMicroStat key={label} value={String(index + 1).padStart(2, "0")} label={label} />
                ))}
              </div>
            </div>
          </div>

          <div className="relative min-h-[430px] overflow-hidden bg-[var(--store-ink)] p-3 md:p-5 lg:min-h-[650px]">
            <div className="relative h-full min-h-[430px] overflow-hidden rounded-[calc(var(--store-radius)+2px)] border border-white/[0.08] bg-black/10 lg:min-h-[610px]">
              <div className="absolute left-[8%] top-[8%] size-72 rounded-full bg-[var(--store-accent)]/24 blur-[92px]" />
              <div className="absolute bottom-[8%] right-[8%] size-64 rounded-full bg-white/[0.07] blur-[85px]" />
              {image ? (
                <SmartImage src={image} alt={title} priority sizes="(max-width: 1023px) 100vw, 55vw" className="vertical-signature-cover h-full min-h-[430px] w-full object-cover lg:min-h-[610px]" />
              ) : (
                <div className="flex h-full min-h-[430px] items-center justify-center text-center text-white lg:min-h-[610px]">
                  <div className="max-w-sm px-8">
                    <p className="text-[11px] font-semibold tracking-[0.12em] text-white/45">{theme.verticalLabel}</p>
                    <p className="mt-4 text-[34px] font-semibold leading-tight tracking-[-0.05em]">{config.storeName}</p>
                  </div>
                </div>
              )}
              <div className="absolute inset-x-0 bottom-0 h-48 bg-gradient-to-t from-black/75 via-black/18 to-transparent" />
              <div className="absolute bottom-7 left-7 right-7 text-white md:bottom-9 md:left-9 md:right-9">
                <span className="text-[9px] font-semibold tracking-[0.15em] text-white/48" dir="ltr">SIGNATURE</span>
                <p className="mt-2 max-w-md text-[16px] font-semibold leading-7">{theme.description}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function VerticalMarketHero({
  config,
}: {
  config: ResolvedConfig;
}) {
  const theme = getThemePreset(config.themeId);
  const hero = config.hero;
  const image = hero.primaryImage.trim() || config.coverImageUrl.trim();
  const title = hero.title.trim() || theme.defaultHeroTitle || `كل ${theme.productLabel ?? "المنتجات"} في مكان واحد.`;
  const description = hero.description.trim() || theme.defaultHeroDescription || theme.description;
  const ctaLabel = hero.ctaLabel.trim() || `ابدأ التصفح`;
  const ctaHref = hero.ctaHref.trim() || "#categories";
  const features = theme.featureLabels ?? ["الأقسام", "المنتجات", "العروض"];

  return (
    <section className="store-container py-5 md:py-8">
      <div className="vertical-market-hero relative overflow-hidden rounded-[calc(var(--store-radius)+8px)] border border-black/[0.06] bg-[var(--store-surface)] shadow-[0_24px_70px_rgba(28,42,50,.07)]">
        <div className="absolute inset-x-0 top-0 h-1 bg-[var(--store-accent)]" />
        <div className="grid min-h-[590px] lg:grid-cols-[.92fr_1.08fr]">
          <div className="flex items-center p-7 md:p-11 lg:p-14 xl:p-16">
            <div className="max-w-2xl vertical-theme-reveal">
              <span className="inline-flex items-center rounded-full bg-[var(--store-accent)]/10 px-4 py-2 text-[10px] font-bold text-[var(--store-accent)]">
                {theme.verticalLabel ?? config.storeName}
              </span>
              <p className="mt-7 text-[12px] font-bold text-[var(--store-muted)]">{config.storeName}</p>
              <h1 className="mt-3 text-[clamp(3rem,5.4vw,6rem)] font-bold leading-[1.01] tracking-[-0.064em]">{title}</h1>
              <p className="mt-6 max-w-xl text-[15px] leading-8 text-[var(--store-ink-soft)] md:text-[16px] md:leading-9">{description}</p>

              <div className="mt-8 flex flex-wrap gap-3">
                <a href={ctaHref} className="inline-flex h-14 items-center gap-3 rounded-[14px] bg-[var(--store-accent)] px-7 text-[12px] font-bold text-[var(--store-accent-contrast)] shadow-[0_12px_26px_rgba(23,32,45,.11)] transition hover:-translate-y-0.5">
                  {ctaLabel}
                  <ArrowLeft size={16} />
                </a>
                <a href="#products" className="inline-flex h-14 items-center rounded-[14px] border border-black/[0.08] bg-[var(--store-surface)] px-6 text-[12px] font-semibold">
                  {theme.productLabel ?? "المنتجات"}
                </a>
              </div>

              <div className="mt-9 flex flex-wrap gap-2">
                {features.slice(0, 3).map((label) => <MarketChip key={label} label={label} />)}
              </div>
            </div>
          </div>

          <div className="relative min-h-[430px] overflow-hidden bg-[var(--store-soft)] lg:min-h-full">
            <div className="absolute -left-24 -top-24 size-80 rounded-full bg-[var(--store-accent)]/18 blur-[90px]" />
            <div className="absolute -bottom-32 right-0 size-96 rounded-full bg-white/70 blur-[90px]" />
            {image ? (
              <SmartImage src={image} alt={title} priority sizes="(max-width: 1023px) 100vw, 55vw" className="vertical-market-cover h-full min-h-[430px] w-full object-cover lg:min-h-[590px]" />
            ) : (
              <div className="flex h-full min-h-[430px] items-center justify-center lg:min-h-[590px]">
                <div className="rounded-[24px] border border-black/[0.07] bg-white/75 px-10 py-14 text-center shadow-[0_24px_50px_rgba(30,45,55,.08)] backdrop-blur">
                  <p className="text-[11px] font-bold text-[var(--store-accent)]">{theme.name}</p>
                  <p className="mt-3 text-[28px] font-bold tracking-[-0.04em]">{config.storeName}</p>
                  <p className="mt-3 text-[11px] text-[var(--store-muted)]">{theme.searchPlaceholder}</p>
                </div>
              </div>
            )}

            <div className="absolute bottom-5 left-5 right-5 grid grid-cols-3 gap-2 rounded-[18px] border border-white/60 bg-white/90 p-3 shadow-[0_16px_35px_rgba(30,55,90,.10)] backdrop-blur md:bottom-8 md:left-8 md:right-8 md:p-4">
              {features.slice(0, 3).map((label, index) => (
                <MobileMarketStat key={label} label={String(index + 1).padStart(2, "0")} value={label} />
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}


// This slot reads product names, prices and images from the live catalog only.
// The editor stores product identifiers and layout preferences, never product prices.
function MobileShowcaseDisplay({ config, fallbackImage, dark }: {
  config: ResolvedConfig; fallbackImage: string; dark: boolean;
}) {
  const { products } = useStorefrontContent();
  const hero = config.hero;
  const mode = hero.showcaseMode || "default";
  const preferred = (hero.featuredProductIds || "").split(",").filter(Boolean);
  const selected = preferred.length
    ? preferred.map((id) => products.find((product) => product.productId === id)).filter((product): product is typeof products[number] => Boolean(product))
    : products;
  const visible = selected.slice(0, mode === "product" ? 1 : 4);
  const image = (hero.showcaseImage || "").trim() || fallbackImage;
  const shared = dark ? "text-white" : "text-[var(--store-ink)]";
  useEffect(() => {
    if (window.parent === window || new URLSearchParams(window.location.search).get("builderPreview") !== "1") return;
    window.parent.postMessage({ type: "RUKN_VISUAL_PRODUCTS", products: products.map((product) => ({ id: product.productId, name: product.name })).slice(0, 80) }, window.location.origin);
  }, [products]);
  if (mode === "text") return (
    <div data-rukn-target="heroShowcaseMode" className={`flex min-h-[430px] items-center justify-center p-8 text-center lg:min-h-[610px] ${shared}`}>
      <h2 className="max-w-xl whitespace-pre-line text-[clamp(2rem,4.5vw,4.8rem)] font-bold leading-tight">
        {hero.showcaseText || hero.title || config.storeName}
      </h2>
    </div>
  );
  if ((mode === "product" || mode === "products") && visible.length) return (
    <div data-rukn-target="heroShowcaseMode" className="flex min-h-[430px] items-center justify-center p-5 lg:min-h-[610px]">
      <div className={`grid w-full max-w-2xl gap-3 ${visible.length === 1 ? "grid-cols-1 max-w-sm" : "grid-cols-2"}`}>
        {visible.map((product) => (
          <a key={product.productId} href={product.href || "#products"} className="min-w-0 overflow-hidden rounded-2xl bg-white p-3 text-[#111827] shadow-xl">
            {product.image ? <SmartImage src={product.image} alt={product.name} className="aspect-square w-full rounded-xl object-contain" />
              : <div className="flex aspect-square items-center justify-center rounded-xl bg-slate-100"><Smartphone size={40}/></div>}
            <h3 className="mt-3 line-clamp-2 text-sm font-semibold">{product.name}</h3>
            <p className="mt-1 text-xs font-bold">{product.price}</p>
          </a>
        ))}
      </div>
    </div>
  );
  if (image || mode === "image") return (
    <div data-rukn-target="heroShowcaseMode" className="relative min-h-[430px] lg:min-h-[610px]">
      {image ? <SmartImage src={image} alt={hero.showcaseText || hero.title || config.storeName} priority
        className="h-full min-h-[430px] w-full object-cover lg:min-h-[610px]" />
        : <div className={`flex min-h-[430px] items-center justify-center text-center lg:min-h-[610px] ${shared}`}>
            <span className="text-xl font-semibold">{hero.showcaseText || config.storeName}</span>
          </div>}
    </div>
  );
  // Legacy placeholder stays unchanged until the merchant selects a new slot mode.
  return dark ? (
    <div data-rukn-target="heroShowcaseMode" className="mobile-theme-float flex h-full min-h-[430px] items-center justify-center lg:min-h-[650px]">
      <div className="relative flex h-[430px] w-[238px] items-center justify-center rounded-[48px] border border-white/20 bg-white/[0.07] md:h-[520px] md:w-[286px]">
        <div className="absolute top-3 h-6 w-24 rounded-full bg-black/45" />
        <Smartphone size={94} strokeWidth={1.05} className="text-white/30" />
      </div>
    </div>
  ) : (
    <div data-rukn-target="heroShowcaseMode" className="flex min-h-[440px] items-center justify-center lg:min-h-[610px]">
      <div className="relative flex h-[405px] w-[225px] items-center justify-center rounded-[46px] border-[5px] border-[var(--store-ink)] bg-white">
        <div className="absolute top-2.5 h-5 w-20 rounded-full bg-[var(--store-ink)]" />
        <Smartphone size={86} strokeWidth={1.15} className="text-[var(--store-accent)]/45" />
      </div>
    </div>
  );
}

function MobileFlagshipHero({
  config,
}: {
  config: ResolvedConfig;
}) {
  const hero = config.hero;
  const image =
    hero.primaryImage.trim() ||
    config.coverImageUrl.trim();

  const title =
    hero.title.trim() ||
    "الجهاز الذي تريده، في واجهة تليق به.";

  const description =
    hero.description.trim() ||
    "اكتشف الأجهزة بصور كبيرة وتفاصيل مقروءة، وانتقل من الاختيار إلى صفحة المنتج بسهولة.";

  const ctaLabel =
    hero.ctaLabel.trim() ||
    "استكشف الأجهزة";

  const ctaHref =
    hero.ctaHref.trim() ||
    "#products";

  return (
    <section className="store-container py-6 md:py-10">
      <div className="mobile-flagship-hero overflow-hidden rounded-[34px] border border-black/[0.07] bg-[var(--store-surface)] shadow-[0_32px_90px_rgba(12,14,18,.10)]">
        <div className="grid min-h-[690px] lg:grid-cols-[.82fr_1.18fr]">
          <div className="relative flex items-center p-7 md:p-12 lg:p-14 xl:p-16">
            <div className="absolute inset-y-14 right-0 w-px bg-gradient-to-b from-transparent via-black/10 to-transparent" />
            <div className="relative z-10 max-w-[670px] mobile-theme-reveal">
              <div className="mb-7 flex flex-wrap items-center gap-3">
                <span className="inline-flex items-center gap-2 rounded-full border border-black/[0.08] bg-[var(--store-canvas)] px-4 py-2 text-[11px] font-semibold text-[var(--store-ink-soft)]">
                  <Smartphone size={15} />
                  {config.storeName}
                </span>
                <span className="text-[10px] font-semibold tracking-[0.18em] text-[var(--store-accent)]" dir="ltr">
                  FLAGSHIP
                </span>
              </div>

              <h1 data-rukn-target="heroTitle" className="max-w-[760px] text-[clamp(3.15rem,6vw,6.9rem)] font-semibold leading-[.99] tracking-[-0.072em]">
                {title}
              </h1>

              <p data-rukn-target="heroDescription" className="mt-7 max-w-xl text-[15px] leading-8 text-[var(--store-ink-soft)] md:text-[17px] md:leading-9">
                {description}
              </p>

              <div className="mt-9 flex flex-wrap items-center gap-4">
                <a
                  data-rukn-target="heroCtaLabel"
                  href={ctaHref}
                  className="inline-flex h-14 items-center gap-3 rounded-full bg-[var(--store-button-bg)] px-7 text-[12px] font-semibold text-[var(--store-button-text)] transition hover:-translate-y-0.5 hover:shadow-[0_14px_28px_rgba(16,17,20,.15)]"
                >
                  {ctaLabel}
                  <ArrowLeft size={16} />
                </a>
                <a
                  href="#categories"
                  className="inline-flex h-14 items-center rounded-full border border-black/[0.09] bg-white px-6 text-[12px] font-semibold text-[var(--store-ink-soft)] transition hover:border-black/20"
                >
                  تصفح الأقسام
                </a>
              </div>

              <div className="mt-10 grid max-w-xl grid-cols-3 border-t border-black/[0.08] pt-6">
                <FlagshipMicroStat value="01" label={hero.heroStat1 || "تصفح"} editTarget="heroStat1" />
                <FlagshipMicroStat value="02" label={hero.heroStat2 || "قارن"} editTarget="heroStat2" />
                <FlagshipMicroStat value="03" label={hero.heroStat3 || "اختر"} editTarget="heroStat3" />
              </div>
            </div>
          </div>

          <div className="relative min-h-[430px] bg-[#0b0d11] p-3 md:p-5 lg:min-h-[690px]">
            <div className="relative h-full min-h-[430px] overflow-hidden rounded-[28px] border border-white/[0.08] bg-[#11141a] lg:min-h-[650px]">
              <div className="absolute left-[10%] top-[8%] size-72 rounded-full bg-[var(--store-accent)]/20 blur-[90px]" />
              <div className="absolute bottom-[4%] right-[12%] size-64 rounded-full bg-white/[0.06] blur-[85px]" />

              <MobileShowcaseDisplay config={config} fallbackImage={image} dark />

              {(hero.showcaseMode || "default") === "default" ? (<>
              <div className="absolute inset-x-0 bottom-0 h-48 bg-gradient-to-t from-black/70 via-black/20 to-transparent" />
              <div className="absolute bottom-6 left-6 right-6 flex items-end justify-between gap-5 text-white md:bottom-8 md:left-8 md:right-8">
                <div>
                  <span className="text-[10px] font-semibold tracking-[0.12em] text-white/48" dir="ltr">
                    FEATURED
                  </span>
                  <p data-rukn-target="heroFeaturedCaption" className="mt-2 max-w-sm text-[16px] font-semibold leading-7 md:text-[18px]">
                    {hero.heroFeaturedCaption || "أجهزة مختارة، صور واضحة، وتفاصيل تساعدك على الاختيار."}
                  </p>
                </div>
                <span className="hidden size-12 items-center justify-center rounded-full border border-white/20 bg-white/[0.08] backdrop-blur md:flex">
                  <ArrowLeft size={18} />
                </span>
              </div>
              </>) : null}
            </div>
          </div>
        </div>

        <div className="grid border-t border-black/[0.07] bg-[var(--store-canvas)] md:grid-cols-3">
          <MobileHeroNote icon={Camera} title={hero.heroNote1Title || "صورة أوضح"} body={hero.heroNote1Body || "المنتج يبقى محور التجربة"} titleTarget="heroNote1Title" bodyTarget="heroNote1Body" dark />
          <MobileHeroNote icon={BatteryCharging} title={hero.heroNote2Title || "تفاصيل مقروءة"} body={hero.heroNote2Body || "الاسم والسعر يظهران بوضوح"} titleTarget="heroNote2Title" bodyTarget="heroNote2Body" dark />
          <MobileHeroNote icon={Wifi} title={hero.heroNote3Title || "تنقل هادئ"} body={hero.heroNote3Body || "أقسام ومنتجات بدون ازدحام"} titleTarget="heroNote3Title" bodyTarget="heroNote3Body" dark />
        </div>
      </div>
    </section>
  );
}

function MobileSmartMarketHero({
  config,
}: {
  config: ResolvedConfig;
}) {
  const hero = config.hero;
  const image =
    hero.primaryImage.trim() ||
    config.coverImageUrl.trim();

  const title =
    hero.title.trim() ||
    "كل الجوالات. بشكل أسهل.";

  const description =
    hero.description.trim() ||
    "تصفح الأقسام والأسعار والمنتجات بوضوح من أول نظرة، وافتح أي جهاز لمراجعة صوره وخياراته.";

  const ctaLabel =
    hero.ctaLabel.trim() ||
    "ابدأ التصفح";

  const ctaHref =
    hero.ctaHref.trim() ||
    "#categories";

  return (
    <section className="store-container py-5 md:py-8">
      <div className="mobile-market-hero relative overflow-hidden rounded-[28px] border border-black/[0.06] bg-white shadow-[0_28px_80px_rgba(30,55,90,.08)]">
        <div className="absolute inset-x-0 top-0 h-1 bg-[var(--store-accent)]" />
        <div className="grid min-h-[610px] lg:grid-cols-[.9fr_1.1fr]">
          <div className="flex items-center p-7 md:p-11 lg:p-14 xl:p-16">
            <div className="max-w-2xl mobile-theme-reveal">
              <span className="inline-flex items-center gap-2 rounded-full bg-[var(--store-accent)]/10 px-4 py-2 text-[11px] font-bold text-[var(--store-accent)]">
                <Smartphone size={15} />
                الجوالات في مكان واحد
              </span>

              <p className="mt-7 text-[13px] font-bold text-[var(--store-muted)]">
                {config.storeName}
              </p>

              <h1 data-rukn-target="heroTitle" className="mt-3 text-[clamp(3rem,5.5vw,6.1rem)] font-bold leading-[1.01] tracking-[-0.065em]">
                {title}
              </h1>

              <p data-rukn-target="heroDescription" className="mt-6 max-w-xl text-[15px] leading-8 text-[var(--store-ink-soft)] md:text-[16px] md:leading-9">
                {description}
              </p>

              <div className="mt-8 flex flex-wrap gap-3">
                <a
                  data-rukn-target="heroCtaLabel"
                  href={ctaHref}
                  className="inline-flex h-14 items-center gap-3 rounded-[14px] bg-[var(--store-accent)] px-7 text-[12px] font-bold text-[var(--store-accent-contrast)] shadow-[0_12px_26px_rgba(23,32,45,.12)] transition hover:-translate-y-0.5"
                >
                  {ctaLabel}
                  <ArrowLeft size={16} />
                </a>
                <a
                  href="#products"
                  className="inline-flex h-14 items-center rounded-[14px] border border-black/[0.09] bg-white px-6 text-[12px] font-semibold"
                >
                  أحدث المنتجات
                </a>
              </div>

              <div className="mt-9 flex flex-wrap gap-2">
                <MarketChip label="المنتجات" />
                <MarketChip label="الأقسام" />
                <MarketChip label="التفاصيل" />
              </div>
            </div>
          </div>

          <div className="relative min-h-[440px] overflow-hidden bg-[var(--store-soft)] lg:min-h-full">
            <div className="absolute -left-24 -top-24 size-80 rounded-full bg-[var(--store-accent)]/16 blur-[90px]" />
            <div className="absolute -bottom-32 right-0 size-96 rounded-full bg-white/90 blur-[90px]" />

            <MobileShowcaseDisplay config={config} fallbackImage={image} dark={false} />

            {(hero.showcaseMode || "default") === "default" ? (<>
            <div className="absolute bottom-5 left-5 right-5 grid grid-cols-3 gap-2 rounded-[18px] border border-white/60 bg-white/90 p-3 shadow-[0_16px_35px_rgba(30,55,90,.12)] backdrop-blur md:bottom-8 md:left-8 md:right-8 md:p-4">
              <MobileMarketStat label="01" value={hero.heroStat1 || "تصفح"} editTarget="heroStat1" />
              <MobileMarketStat label="02" value={hero.heroStat2 || "قارن"} editTarget="heroStat2" />
              <MobileMarketStat label="03" value={hero.heroStat3 || "اختر"} editTarget="heroStat3" />
            </div>
            </>) : null}
          </div>
        </div>
      </div>
    </section>
  );
}

function MobileHeroNote({
  icon: Icon,
  title,
  body,
  titleTarget,
  bodyTarget,
  dark = false,
}: {
  icon: React.ComponentType<{
    size?: number;
    strokeWidth?: number;
  }>;
  title: string;
  body: string;
  titleTarget?: string;
  bodyTarget?: string;
  dark?: boolean;
}) {
  return (
    <div className={[
      "flex items-center gap-4 px-6 py-5 md:border-l md:px-8 md:last:border-l-0",
      dark ? "border-black/[0.07]" : "border-white/10",
    ].join(" ")}>
      <span className={[
        "flex size-10 shrink-0 items-center justify-center rounded-full",
        dark
          ? "bg-[var(--store-surface)] text-[var(--store-accent)] shadow-sm"
          : "bg-white/[0.08] text-[var(--store-accent)]",
      ].join(" ")}>
        <Icon size={18} strokeWidth={1.7} />
      </span>
      <div>
        <p data-rukn-target={titleTarget} className={[
          "text-[12px] font-semibold",
          dark ? "text-[var(--store-ink)]" : "text-white",
        ].join(" ")}>{title}</p>
        <p data-rukn-target={bodyTarget} className={[
          "mt-1 text-[10px]",
          dark ? "text-[var(--store-muted)]" : "text-white/48",
        ].join(" ")}>{body}</p>
      </div>
    </div>
  );
}

function FlagshipMicroStat({
  value,
  label,
  editTarget,
}: {
  value: string;
  label: string;
  editTarget?: string;
}) {
  return (
    <div className="border-l border-black/[0.08] px-4 first:pr-0 last:border-l-0">
      <span className="store-money text-[11px] font-semibold text-[var(--store-accent)]" dir="ltr">
        {value}
      </span>
      <strong data-rukn-target={editTarget} className="mt-1 block text-[12px] font-semibold text-[var(--store-ink-soft)]">
        {label}
      </strong>
    </div>
  );
}

function MarketChip({ label }: { label: string }) {
  return (
    <span className="rounded-full border border-black/[0.07] bg-[var(--store-canvas)] px-3.5 py-2 text-[10px] font-semibold text-[var(--store-ink-soft)]">
      {label}
    </span>
  );
}

function MobileMarketStat({
  label,
  value,
  editTarget,
}: {
  label: string;
  value: string;
  editTarget?: string;
}) {
  return (
    <div className="text-center">
      <span className="store-money block text-[9px] font-bold text-[var(--store-accent)]" dir="ltr">{label}</span>
      <strong data-rukn-target={editTarget} className="mt-1 block text-[12px] font-bold">{value}</strong>
    </div>
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
    case "mobile-flagship":
      return (
        <MobileFlagshipProducts
          config={config}
        />
      );

    case "mobile-smart-market":
      return (
        <MobileSmartMarketProducts
          config={config}
        />
      );

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

function MobileFlagshipProducts({
  config,
}: {
  config: ResolvedConfig;
}) {
  const { products } =
    useStorefrontContent();

  const lead =
    products[0];

  const remaining =
    products.slice(1);

  return (
    <section
      id="products"
      className="store-container py-20 md:py-28"
    >
      <div className="mb-10 flex flex-wrap items-end justify-between gap-6">
        <div>
          <p className="text-[12px] font-semibold text-[var(--store-accent)]">
            {config.productSection.eyebrow || "مختارات"}
          </p>
          <h2 className="mt-2 max-w-3xl text-[clamp(2.2rem,4vw,4.8rem)] font-semibold leading-[1.06] tracking-[-0.06em]">
            {config.productSection.title}
          </h2>
        </div>
        <span className="max-w-[270px] text-[11px] leading-6 text-[var(--store-muted)]">
          صور أكبر، تفاصيل أقل ازدحامًا، وتركيز مباشر على الجهاز والسعر.
        </span>
      </div>

      {lead ? (
        <article className="group grid overflow-hidden rounded-[26px] bg-[var(--store-ink)] text-[var(--store-ink-contrast)] shadow-[0_25px_70px_rgba(11,13,17,.13)] lg:grid-cols-[1.1fr_.9fr]">
          <Link
            to={lead.href ?? "#products"}
            className="relative min-h-[430px] overflow-hidden bg-white/[0.06] lg:min-h-[610px]"
          >
            <SmartImage
              src={lead.image}
              alt={lead.name}
              sizes="(max-width: 1023px) 100vw, 58vw"
              className="h-full w-full object-contain p-6 transition duration-700 group-hover:scale-[1.035] md:p-10"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-black/25 via-transparent to-transparent" />
          </Link>

          <div className="flex items-center p-7 md:p-10 lg:p-12">
            <div>
              <span className="text-[11px] font-medium text-[var(--store-accent)]">
                {lead.category}
              </span>
              <h3 className="mt-3 text-[clamp(2.4rem,4vw,4.9rem)] font-semibold leading-[1.03] tracking-[-0.065em]">
                {lead.name}
              </h3>
              {lead.description ? (
                <p className="mt-5 line-clamp-3 text-[13px] leading-7 text-white/58">
                  {lead.description}
                </p>
              ) : null}
              <div className="mt-7 flex flex-wrap items-baseline gap-3">
                <strong className="store-money text-[24px] font-bold tracking-[-0.02em]">
                  {lead.price}
                </strong>
                {lead.compareAtPrice ? (
                  <span className="store-money text-[13px] text-white/35 line-through">
                    {lead.compareAtPrice}
                  </span>
                ) : null}
              </div>
              <Link
                to={lead.href ?? "#products"}
                className="mt-8 inline-flex h-12 items-center gap-3 rounded-full bg-white px-5 text-[11px] font-semibold text-[#101114]"
              >
                عرض الجهاز
                <ArrowLeft size={15} />
              </Link>
            </div>
          </div>
        </article>
      ) : null}

      {remaining.length > 0 ? (
        <div className="mt-8 grid grid-cols-2 gap-x-5 gap-y-12 md:grid-cols-3 lg:grid-cols-4">
          {remaining.map((product) => (
            <ProductCard
              key={product.productId}
              {...product}
              variant="mobile-flagship"
            />
          ))}
        </div>
      ) : null}
    </section>
  );
}

function MobileSmartMarketProducts({
  config,
}: {
  config: ResolvedConfig;
}) {
  const { products } =
    useStorefrontContent();

  return (
    <section
      id="products"
      className="store-container py-16 md:py-24"
    >
      <div className="mb-8 flex flex-wrap items-end justify-between gap-5">
        <div>
          <p className="text-[11px] font-bold text-[var(--store-accent)]">
            {config.productSection.eyebrow || "الجوالات"}
          </p>
          <h2 className="mt-2 text-[clamp(2rem,3.3vw,3.7rem)] font-bold tracking-[-0.05em]">
            {config.productSection.title}
          </h2>
        </div>
        <div className="rounded-full border border-black/[0.07] bg-white px-4 py-2.5 text-[10px] font-semibold text-[var(--store-muted)] shadow-sm">
          <span className="store-money font-bold text-[var(--store-accent)]" dir="ltr">{products.length}</span> منتج في العرض
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
        {products.map((product) => (
          <ProductCard
            key={product.productId}
            {...product}
            variant="mobile-market"
          />
        ))}
      </div>
    </section>
  );
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
          <button className="rounded-full bg-[var(--store-button-bg)] px-4 py-2 text-[var(--store-button-text)]">
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

            <div className="relative -mt-14 ml-auto max-w-xl rounded-[var(--store-radius)] bg-[var(--store-ink)] p-8 text-[var(--store-ink-contrast)] md:p-10 lg:absolute lg:bottom-10 lg:left-0 lg:mt-0">
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
                className="mt-7 bg-[var(--store-ink)] px-6 py-3 text-[11px] font-semibold text-[var(--store-ink-contrast)]"
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

function MobileFlagshipBand({ config }: { config: ResolvedConfig }) {
  return (
    <section className="store-container">
      <div className="grid overflow-hidden rounded-[20px] border border-black/[0.07] bg-white md:grid-cols-3">
        <MobileThemeBenefit
          icon={Smartphone}
          title={config.hero.flagshipBand1Title || "اكتشف الأجهزة"}
          body={config.hero.flagshipBand1Body || "تصفح المنتجات المتاحة في المتجر"}
          titleTarget="flagshipBand1Title"
          bodyTarget="flagshipBand1Body"
        />
        <MobileThemeBenefit
          icon={Camera}
          title={config.hero.flagshipBand2Title || "تصفح الأقسام"}
          body={config.hero.flagshipBand2Body || "انتقل مباشرة إلى الفئة المناسبة"}
          titleTarget="flagshipBand2Title"
          bodyTarget="flagshipBand2Body"
        />
        <MobileThemeBenefit
          icon={BatteryCharging}
          title={config.hero.flagshipBand3Title || "شاهد التفاصيل"}
          body={config.hero.flagshipBand3Body || "افتح المنتج لمراجعة الخيارات والمواصفات"}
          titleTarget="flagshipBand3Title"
          bodyTarget="flagshipBand3Body"
        />
      </div>
    </section>
  );
}

function MobileSmartMarketBand({ config }: { config: ResolvedConfig }) {
  return (
    <section className="store-container">
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <Benefit
          icon={Smartphone}
          title={config.hero.smartBand1Title || "تصفح حسب القسم"}
          body={config.hero.smartBand1Body || "اعثر على الفئة المناسبة بسرعة"}
          titleTarget="smartBand1Title"
          bodyTarget="smartBand1Body"
        />
        <Benefit
          icon={Wifi}
          title={config.hero.smartBand2Title || "اختر الجهاز"}
          body={config.hero.smartBand2Body || "شاهد المنتجات المتاحة بوضوح"}
          titleTarget="smartBand2Title"
          bodyTarget="smartBand2Body"
        />
        <Benefit
          icon={Camera}
          title={config.hero.smartBand3Title || "راجع الصور"}
          body={config.hero.smartBand3Body || "افتح الجهاز وشاهد صوره وتفاصيله"}
          titleTarget="smartBand3Title"
          bodyTarget="smartBand3Body"
        />
        <Benefit
          icon={PackageCheck}
          title={config.hero.smartBand4Title || "السعر واضح"}
          body={config.hero.smartBand4Body || "السعر والخيارات الأساسية أمامك"}
          titleTarget="smartBand4Title"
          bodyTarget="smartBand4Body"
        />
      </div>
    </section>
  );
}

function MobileThemeBenefit({
  icon: Icon,
  title,
  body,
  titleTarget,
  bodyTarget,
}: {
  icon: React.ComponentType<{
    size?: number;
    strokeWidth?: number;
  }>;
  title: string;
  body: string;
  titleTarget?: string;
  bodyTarget?: string;
}) {
  return (
    <div className="flex items-center gap-4 border-black/[0.06] p-5 md:border-l md:p-6 md:last:border-l-0">
      <span className="flex size-11 shrink-0 items-center justify-center rounded-full bg-[var(--store-soft)] text-[var(--store-accent)]">
        <Icon size={18} strokeWidth={1.7} />
      </span>
      <div>
        <p data-rukn-target={titleTarget} className="text-[12px] font-semibold">{title}</p>
        <p data-rukn-target={bodyTarget} className="mt-1 text-[10px] leading-5 text-[var(--store-muted)]">{body}</p>
      </div>
    </div>
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
  titleTarget,
  bodyTarget,
}: {
  icon:
    React.ComponentType<{
      size?: number;
      strokeWidth?: number;
    }>;
  title: string;
  body: string;
  titleTarget?: string;
  bodyTarget?: string;
}) {
  return (
    <div className="bg-[var(--store-surface)] p-5">
      <Icon
        size={20}
        strokeWidth={1.7}
      />

      <p data-rukn-target={titleTarget} className="mt-4 text-[12px] font-semibold">
        {title}
      </p>

      <p data-rukn-target={bodyTarget} className="mt-1 text-[10px] text-[var(--store-muted)]">
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