import {
  ArrowLeft,
} from "lucide-react";

import type {
  StorefrontConfig,
} from "../theme/theme.types";

import {
  SmartImage,
} from "./SmartImage";

export function PromoBannerSection({
  config,
}: {
  config: StorefrontConfig;
}) {
  const section =
    config.bannerSection;

  if (
    !section.enabled
  ) {
    return null;
  }

  const splitLayout =
    config.themeId ===
      "commerce" ||
    config.themeId ===
      "technical";

  if (splitLayout) {
    return (
      <section
        id="banner"
        className="store-container py-14 md:py-20"
      >
        <div className="grid overflow-hidden rounded-[var(--store-radius)] bg-[var(--store-surface)] md:grid-cols-2">
          <SmartImage
            src={
              section.image
            }
            alt={
              section.title
            }
            className="h-full min-h-[390px] w-full object-cover"
          />

          <div className="flex items-center bg-[var(--store-soft)] p-8 md:p-12">
            <div>
              <p className="text-[10px] font-semibold text-[var(--store-accent)]">
                {section.eyebrow}
              </p>

              <h2 className="mt-3 text-[clamp(2rem,3.2vw,3.8rem)] font-semibold leading-[1.15] tracking-[-0.05em]">
                {section.title}
              </h2>

              <p className="mt-5 max-w-lg text-[14px] leading-8 text-[var(--store-ink-soft)]">
                {section.body}
              </p>

              {section.ctaLabel ? (
                <a
                  href={
                    section.ctaHref
                  }
                  className="mt-7 inline-flex items-center gap-2 bg-[var(--store-ink)] px-5 py-3 text-[11px] font-semibold text-white"
                >
                  {section.ctaLabel}

                  <ArrowLeft
                    size={14}
                  />
                </a>
              ) : null}
            </div>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section
      id="banner"
      className="store-container py-16 md:py-24"
    >
      <div className="relative flex min-h-[560px] overflow-hidden rounded-[var(--store-radius)]">
        <SmartImage
          src={
            section.image
          }
          alt={
            section.title
          }
          className="absolute inset-0 h-full w-full object-cover"
        />

        <div className="absolute inset-0 bg-gradient-to-t from-black/65 via-black/15 to-transparent" />

        <div
          className={[
            "relative mt-auto max-w-3xl p-8 text-white md:p-12",
            config.themeId ===
            "maison"
              ? "mx-auto text-center"
              : "",
          ].join(" ")}
        >
          <p className="text-[11px] text-white/65">
            {section.eyebrow}
          </p>

          <h2 className="mt-4 text-[clamp(2.4rem,4vw,4.8rem)] font-medium leading-[1.12] tracking-[-0.05em]">
            {section.title}
          </h2>

          <p
            className={[
              "mt-5 max-w-xl text-[14px] leading-8 text-white/75",
              config.themeId ===
              "maison"
                ? "mx-auto"
                : "",
            ].join(" ")}
          >
            {section.body}
          </p>

          {section.ctaLabel ? (
            <a
              href={
                section.ctaHref
              }
              className="mt-7 inline-flex items-center gap-2 border-b border-white/50 pb-2 text-[11px] font-semibold"
            >
              {section.ctaLabel}

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