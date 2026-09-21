import { StorefrontCartLink } from "./StorefrontCartLink";
import {
  Menu,
  Search,
  ShoppingBag,
  UserRound,
} from "lucide-react";

import {
  useQuery,
} from "@tanstack/react-query";

import {
  getStorefrontNavigation,
} from "../data/storefrontApi";

import {
  getThemePreset,
} from "../theme/themePresets";

import type {
  ThemeId,
} from "../theme/theme.types";

interface Props {
  storeSlug: string;
  storeName: string;
  logoUrl: string;
  announcement: string;
  themeId: ThemeId;
}

export function StorefrontHeader({
  storeSlug,
  storeName,
  logoUrl,
  announcement,
  themeId,
}: Props) {
  const theme =
    getThemePreset(themeId);

  const currentStorefrontPath =
  typeof window !== "undefined"
    ? `${window.location.pathname}${window.location.search}`
    : `/store/${encodeURIComponent(storeSlug)}`;

const accountHref =
  `/store/${encodeURIComponent(storeSlug)}/account?returnTo=${encodeURIComponent(
    currentStorefrontPath,
  )}`;

  if (theme.headerStyle === "vertical-signature") {
    return (
      <>
        {announcement ? (
          <div className="border-b border-black/[0.06] bg-[var(--store-ink)] py-2.5 text-center text-[11px] font-medium text-[var(--store-ink-contrast)]">
            {announcement}
          </div>
        ) : null}

        <header className="sticky top-0 z-40 border-b border-black/[0.065] bg-[var(--store-canvas)]/94 backdrop-blur-xl">
          <div className="store-container flex h-[92px] items-center gap-7">
            <button
              type="button"
              aria-label="القائمة"
              className="flex size-11 items-center justify-center rounded-full border border-black/[0.08] md:hidden"
            >
              <Menu size={19} />
            </button>

            <a href="#" className="shrink-0 text-[27px] font-semibold tracking-[-0.055em]">
              <StoreBrand storeName={storeName} logoUrl={logoUrl} />
            </a>

            <nav className="hidden items-center gap-8 text-[12px] font-semibold text-[var(--store-ink-soft)] md:flex">
              <a href="#products" className="transition hover:text-[var(--store-accent)]">
                {theme.productLabel ?? "المنتجات"}
              </a>
              <a href="#categories" className="transition hover:text-[var(--store-accent)]">
                {theme.categoryLabel ?? "الأقسام"}
              </a>
              <a href="#story" className="transition hover:text-[var(--store-accent)]">
                عن المتجر
              </a>
              <StorefrontDynamicNavigation storeSlug={storeSlug} />
            </nav>

            <div className="mr-auto flex items-center gap-1">
              <button type="button" aria-label="البحث" className="flex size-11 items-center justify-center rounded-full transition hover:bg-black/[0.045]">
                <Search size={19} />
              </button>
              <a href={accountHref} aria-label="الحساب" className="hidden size-11 items-center justify-center rounded-full transition hover:bg-black/[0.045] sm:flex"><UserRound size={19} /></a>
              <StorefrontCartLink storeSlug={storeSlug} aria-label="السلة" className="flex size-11 items-center justify-center rounded-full bg-[var(--store-ink)] text-[var(--store-ink-contrast)] transition hover:-translate-y-0.5">
                <ShoppingBag size={18} />
              </StorefrontCartLink>
            </div>
          </div>
        </header>
      </>
    );
  }

  if (theme.headerStyle === "vertical-market") {
    return (
      <>
        {announcement ? (
          <div className="bg-[var(--store-accent)] py-2.5 text-center text-[11px] font-semibold text-[var(--store-accent-contrast)]">
            {announcement}
          </div>
        ) : null}

        <header className="sticky top-0 z-40 border-b border-black/[0.07] bg-[var(--store-surface)]/96 backdrop-blur-xl">
          <div className="store-container flex min-h-[86px] items-center gap-4 md:gap-6">
            <button type="button" aria-label="القائمة" className="flex size-10 items-center justify-center rounded-[12px] border border-black/[0.08] lg:hidden">
              <Menu size={20} />
            </button>

            <a href="#" className="shrink-0 text-[24px] font-bold tracking-[-0.045em]">
              <StoreBrand storeName={storeName} logoUrl={logoUrl} />
            </a>

            <button type="button" className="hidden h-12 flex-1 items-center gap-3 rounded-[14px] border border-black/[0.07] bg-[var(--store-canvas)] px-4 text-right text-[12px] font-medium text-[var(--store-muted)] md:flex">
              <Search size={18} />
              <span>{theme.searchPlaceholder ?? "ابحث في المتجر"}</span>
            </button>

            <div className="mr-auto flex items-center gap-1">
              <button type="button" aria-label="البحث" className="flex size-10 items-center justify-center rounded-[11px] md:hidden">
                <Search size={19} />
              </button>
              <a href={accountHref} aria-label="الحساب" className="flex size-10 items-center justify-center rounded-[11px]"><UserRound size={19} /></a>
              <StorefrontCartLink storeSlug={storeSlug} aria-label="السلة" className="flex size-10 items-center justify-center rounded-[11px] bg-[var(--store-ink)] text-[var(--store-ink-contrast)]">
                <ShoppingBag size={18} />
              </StorefrontCartLink>
            </div>
          </div>

          <div className="hidden border-t border-black/[0.05] md:block">
            <nav className="store-container flex h-12 items-center gap-8 text-[11px] font-semibold text-[var(--store-ink-soft)]">
              <a href="#categories" className="text-[var(--store-accent)]">
                {theme.categoryLabel ?? "الأقسام"}
              </a>
              <a href="#products">{theme.productLabel ?? "المنتجات"}</a>
              <a href="#products">الأحدث</a>
              <a href="#products">العروض</a>
              <StorefrontDynamicNavigation storeSlug={storeSlug} />
            </nav>
          </div>
        </header>
      </>
    );
  }

  if (
    themeId ===
    "mobile-flagship"
  ) {
    return (
      <>
        {announcement ? (
          <div className="bg-[var(--store-ink)] py-2.5 text-center text-[12px] font-medium text-[var(--store-ink-contrast)]">
            {announcement}
          </div>
        ) : null}

        <header className="sticky top-0 z-40 border-b border-black/[0.07] bg-[var(--store-canvas)]/95 backdrop-blur-xl">
          <div className="store-container flex h-[92px] items-center gap-7">
            <button
              type="button"
              aria-label="القائمة"
              className="flex size-11 items-center justify-center rounded-full border border-black/[0.08] md:hidden"
            >
              <Menu size={20} />
            </button>

            <a
              href="#"
              className="shrink-0 text-[27px] font-semibold tracking-[-0.055em]"
            >
              <StoreBrand
                storeName={storeName}
                logoUrl={logoUrl}
              />
            </a>

            <nav className="hidden items-center gap-8 text-[13px] font-semibold text-[var(--store-ink-soft)] md:flex">
              <a href="#products" className="transition hover:text-[var(--store-accent)]">
                أحدث الأجهزة
              </a>
              <a href="#categories" className="transition hover:text-[var(--store-accent)]">
                الفئات
              </a>
              <a href="#products" className="transition hover:text-[var(--store-accent)]">
                المختارات
              </a>
              <StorefrontDynamicNavigation storeSlug={storeSlug} />
            </nav>

            <div className="mr-auto flex items-center gap-1">
              <button
                type="button"
                aria-label="البحث"
                className="flex size-11 items-center justify-center rounded-full transition hover:bg-black/[0.045]"
              >
                <Search size={19} />
              </button>
              <a href={accountHref}
                aria-label="الحساب"
                className="hidden size-11 items-center justify-center rounded-full transition hover:bg-black/[0.045] sm:flex"
              ><UserRound size={19} /></a>
              <StorefrontCartLink storeSlug={storeSlug}
                aria-label="السلة"
                className="flex size-11 items-center justify-center rounded-full bg-[var(--store-ink)] text-[var(--store-ink-contrast)] transition hover:opacity-90"
              >
                <ShoppingBag size={18} />
              </StorefrontCartLink>
            </div>
          </div>
        </header>
      </>
    );
  }

  if (
    themeId ===
    "mobile-smart-market"
  ) {
    return (
      <>
        {announcement ? (
          <div className="bg-[var(--store-accent)] py-2.5 text-center text-[12px] font-semibold text-[var(--store-accent-contrast)]">
            {announcement}
          </div>
        ) : null}

        <header className="sticky top-0 z-40 border-b border-black/[0.08] bg-white/95 backdrop-blur-xl">
          <div className="store-container flex min-h-[86px] items-center gap-4 md:gap-6">
            <button
              type="button"
              aria-label="القائمة"
              className="flex size-10 items-center justify-center rounded-[11px] border border-black/[0.09] lg:hidden"
            >
              <Menu size={20} />
            </button>

            <a
              href="#"
              className="shrink-0 text-[25px] font-bold tracking-[-0.045em]"
            >
              <StoreBrand
                storeName={storeName}
                logoUrl={logoUrl}
              />
            </a>

            <button
              type="button"
              className="hidden h-12 flex-1 items-center gap-3 rounded-[14px] border border-black/[0.08] bg-[var(--store-canvas)] px-4 text-right text-[13px] font-medium text-[var(--store-muted)] md:flex"
            >
              <Search size={18} />
              <span>ابحث عن جوال أو موديل</span>
            </button>

            <div className="mr-auto flex items-center gap-1">
              <button
                type="button"
                aria-label="البحث"
                className="flex size-10 items-center justify-center rounded-[11px] md:hidden"
              >
                <Search size={19} />
              </button>
              <a href={accountHref}
                aria-label="الحساب"
                className="flex size-10 items-center justify-center rounded-[11px]"
              ><UserRound size={19} /></a>
              <StorefrontCartLink storeSlug={storeSlug}
                aria-label="السلة"
                className="flex size-10 items-center justify-center rounded-[11px] bg-[var(--store-ink)] text-[var(--store-ink-contrast)]"
              >
                <ShoppingBag size={18} />
              </StorefrontCartLink>
            </div>
          </div>

          <div className="hidden border-t border-black/[0.055] md:block">
            <nav className="store-container flex h-12 items-center gap-8 text-[12px] font-semibold text-[var(--store-ink-soft)]">
              <a href="#categories" className="text-[var(--store-accent)]">
                تصفح الأقسام
              </a>
              <a href="#products">الجوالات</a>
              <a href="#products">الأحدث</a>
              <a href="#products">العروض</a>
              <StorefrontDynamicNavigation storeSlug={storeSlug} />
            </nav>
          </div>
        </header>
      </>
    );
  }

  if (
    themeId ===
    "maison"
  ) {
    return (
      <>
        {announcement ? (
          <div className="border-b border-black/10 py-2.5 text-center text-[10px] tracking-[0.08em] text-[var(--store-ink-soft)]">
            {announcement}
          </div>
        ) : null}

        <header className="border-b border-black/[0.08] bg-[var(--store-canvas)]">
          <div className="store-container">
            <div className="flex h-[92px] items-center justify-between">
              <div className="flex w-1/3 items-center">
                <button
                  type="button"
                  aria-label="القائمة"
                  className="flex size-10 items-center justify-center"
                >
                  <Menu
                    size={18}
                    strokeWidth={1.4}
                  />
                </button>
              </div>

              <a
                href="#"
                className="w-1/3 text-center text-[25px] font-medium tracking-[0.12em]"
              >
                <StoreBrand
                  storeName={storeName}
                  logoUrl={logoUrl}
                />
              </a>

              <div className="flex w-1/3 justify-end gap-1">
                <button
                  type="button"
                  aria-label="البحث"
                  className="flex size-10 items-center justify-center"
                >
                  <Search
                    size={18}
                    strokeWidth={1.4}
                  />
                </button>

                <StorefrontCartLink storeSlug={storeSlug}
                  aria-label="السلة"
                  className="flex size-10 items-center justify-center"
                >
                  <ShoppingBag
                    size={18}
                    strokeWidth={1.4}
                  />
                </StorefrontCartLink>
              </div>
            </div>

            <nav className="hidden h-12 items-center justify-center gap-10 border-t border-black/[0.06] text-[11px] md:flex">
              <a href="#products">
                المجموعة
              </a>

              <a href="#products">
                الجديد
              </a>

              <a href="#story">
                عن الدار
              </a>
              <StorefrontDynamicNavigation storeSlug={storeSlug} />
            </nav>
          </div>
        </header>
      </>
    );
  }

  if (
    themeId ===
    "commerce"
  ) {
    return (
      <>
        {announcement ? (
          <div className="bg-[var(--store-accent)] py-2.5 text-center text-[11px] font-semibold text-[var(--store-accent-contrast)]">
            {announcement}
          </div>
        ) : null}

        <header className="sticky top-0 z-40 border-b border-black/[0.08] bg-white">
          <div className="store-container flex min-h-[84px] items-center gap-6">
            <button
              type="button"
              aria-label="القائمة"
              className="flex size-10 items-center justify-center lg:hidden"
            >
              <Menu size={21} />
            </button>

            <a
              href="#"
              className="shrink-0 text-[23px] font-bold tracking-[-0.04em]"
            >
              <StoreBrand
                storeName={storeName}
                logoUrl={logoUrl}
              />
            </a>

            <button
              type="button"
              className="hidden h-11 flex-1 items-center gap-3 rounded-[10px] border border-black/10 bg-[var(--store-canvas)] px-4 text-right text-[12px] text-[var(--store-muted)] md:flex"
            >
              <Search size={17} />

              <span>
                ابحث عن منتج أو قسم
              </span>
            </button>

            <nav className="hidden shrink-0 items-center gap-5 text-[12px] font-semibold xl:flex">
              <a href="#products">
                المنتجات
              </a>

              <a href="#products">
                العروض
              </a>

              <a href="#story">
                عن المتجر
              </a>
              <StorefrontDynamicNavigation storeSlug={storeSlug} />
            </nav>

            <div className="mr-auto flex items-center gap-1">
              <a href={accountHref}
                aria-label="الحساب"
                className="flex size-10 items-center justify-center"
              ><UserRound size={19} /></a>

              <StorefrontCartLink storeSlug={storeSlug}
                aria-label="السلة"
                className="relative flex size-10 items-center justify-center"
              >
                <ShoppingBag size={20} />


              </StorefrontCartLink>
            </div>
          </div>
        </header>
      </>
    );
  }

  if (
    themeId ===
    "technical"
  ) {
    return (
      <>
        <header className="border-b border-black/[0.1] bg-[var(--store-surface)]">
          <div className="store-container flex min-h-[76px] items-center gap-7">
            <a
              href="#"
              className="shrink-0 text-[22px] font-bold tracking-[-0.045em]"
            >
              <StoreBrand
                storeName={storeName}
                logoUrl={logoUrl}
              />
            </a>

            <nav className="hidden items-center gap-7 text-[12px] font-medium lg:flex">
              <a href="#products">
                المنتجات
              </a>

              <a href="#products">
                التصنيفات
              </a>

              <a href="#story">
                الدعم
              </a>
              <StorefrontDynamicNavigation storeSlug={storeSlug} />
            </nav>

            <div className="mr-auto flex items-center gap-2">
              <button
                type="button"
                className="hidden h-10 min-w-[220px] items-center gap-2 rounded-[7px] border border-black/10 bg-[var(--store-canvas)] px-3 text-[11px] text-[var(--store-muted)] md:flex"
              >
                <Search size={16} />
                ابحث بالموديل أو المنتج
              </button>

              <button
                type="button"
                className="flex size-10 items-center justify-center"
              >
                <UserRound size={18} />
              </button>

              <StorefrontCartLink storeSlug={storeSlug}
                className="flex size-10 items-center justify-center"
              >
                <ShoppingBag size={18} />
              </StorefrontCartLink>
            </div>
          </div>
        </header>

        {announcement ? (
          <div className="border-b border-black/[0.08] bg-[var(--store-soft)] py-2 text-center text-[10px] font-medium text-[var(--store-ink-soft)]">
            {announcement}
          </div>
        ) : null}
      </>
    );
  }

  if (
    themeId ===
    "studio"
  ) {
    return (
      <>
        {announcement ? (
          <div className="bg-[var(--store-ink)] py-2 text-center text-[10px] text-[var(--store-ink-contrast)]">
            {announcement}
          </div>
        ) : null}

        <header className="bg-[var(--store-canvas)]">
          <div className="store-container flex h-[92px] items-center justify-between">
            <div className="flex items-center gap-4">
              <button
                type="button"
                className="flex size-11 items-center justify-center rounded-full border border-black/10"
              >
                <Menu size={18} />
              </button>

              <a
                href="#"
                className="text-[25px] font-bold tracking-[-0.06em]"
              >
                <StoreBrand
                  storeName={storeName}
                  logoUrl={logoUrl}
                />
              </a>
            </div>

            <nav className="hidden items-center gap-2 md:flex">
              <a
                href="#products"
                className="rounded-full border border-black/10 px-5 py-2.5 text-[11px]"
              >
                الجديد
              </a>

              <a
                href="#products"
                className="rounded-full border border-black/10 px-5 py-2.5 text-[11px]"
              >
                المجموعة
              </a>

              <a
                href="#story"
                className="rounded-full border border-black/10 px-5 py-2.5 text-[11px]"
              >
                القصة
              </a>
              <StorefrontDynamicNavigation
                storeSlug={storeSlug}
                linkClassName="rounded-full border border-black/10 px-5 py-2.5 text-[11px]"
              />
            </nav>

            <div className="flex gap-1">
              <button
                type="button"
                className="flex size-11 items-center justify-center rounded-full border border-black/10"
              >
                <Search size={18} />
              </button>

              <StorefrontCartLink storeSlug={storeSlug}
                className="flex size-11 items-center justify-center rounded-full bg-[var(--store-ink)] text-[var(--store-ink-contrast)]"
              >
                <ShoppingBag size={18} />
              </StorefrontCartLink>
            </div>
          </div>
        </header>
      </>
    );
  }

  return (
    <>
      {announcement ? (
        <div className="bg-[var(--store-ink)] py-2 text-center text-[11px] text-[var(--store-ink-contrast)]">
          {announcement}
        </div>
      ) : null}

      <header className="sticky top-0 z-40 border-b border-black/[0.08] bg-[var(--store-canvas)]/95 backdrop-blur-xl">
        <div className="store-container flex h-[78px] items-center justify-between gap-8">
          <div className="flex items-center gap-9">
            <button
              type="button"
              className="flex size-10 items-center justify-center md:hidden"
            >
              <Menu size={21} />
            </button>

            <a
              href="#"
              className="text-[23px] font-bold tracking-[-0.045em]"
            >
              <StoreBrand
                storeName={storeName}
                logoUrl={logoUrl}
              />
            </a>

            <nav className="hidden items-center gap-8 text-[13px] font-medium md:flex">
              <a href="#products">
                الجديد
              </a>

              <a href="#products">
                المجموعة
              </a>

              <a href="#story">
                قصتنا
              </a>
              <StorefrontDynamicNavigation storeSlug={storeSlug} />
            </nav>
          </div>

          <div className="flex items-center gap-1">
            <button
              type="button"
              className="hidden h-10 items-center gap-2 border-b border-black/20 px-1 text-[12px] text-[var(--store-ink-soft)] lg:flex"
            >
              <Search size={17} />
              ابحث في المتجر
            </button>

            <button
              type="button"
              className="flex size-10 items-center justify-center"
            >
              <UserRound size={19} />
            </button>

            <StorefrontCartLink storeSlug={storeSlug}
              className="relative flex size-10 items-center justify-center"
            >
              <ShoppingBag size={19} />


            </StorefrontCartLink>
          </div>
        </div>
      </header>
    </>
  );
}

function StorefrontDynamicNavigation({
  storeSlug,
  linkClassName = "",
}: {
  storeSlug: string;
  linkClassName?: string;
}) {
  const navigationQuery = useQuery({
    queryKey: [
      "storefront-runtime",
      storeSlug,
      "navigation",
      "Header",
    ],
    queryFn: () => getStorefrontNavigation(storeSlug, "Header"),
    enabled: Boolean(storeSlug.trim()),
    retry: 1,
    staleTime: 60_000,
  });

  const items = (navigationQuery.data ?? [])
    .filter((item) => !item.parentItemId)
    .slice(0, 4);

  return (
    <>
      {items.map((item) => {
        const external = /^https?:\/\//i.test(item.href);

        return (
          <a
            key={item.id}
            href={item.href}
            target={external ? "_blank" : undefined}
            rel={external ? "noreferrer" : undefined}
            className={`transition hover:text-[var(--store-accent)] ${linkClassName}`}
          >
            {item.label}
          </a>
        );
      })}
      <a
        href={`/store/${encodeURIComponent(storeSlug)}/contact`}
        className={`transition hover:text-[var(--store-accent)] ${linkClassName}`}
      >
        تواصل معنا
      </a>
    </>
  );
}

function StoreBrand({
  storeName,
  logoUrl,
}: {
  storeName: string;
  logoUrl: string;
}) {
  if (logoUrl.trim()) {
    return (
      <img
        src={logoUrl}
        alt={storeName}
        className="max-h-11 max-w-[170px] object-contain"
      />
    );
  }

  return (
    <>
      {storeName}
    </>
  );
}
