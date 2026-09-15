import {
  Menu,
  Search,
  ShoppingBag,
  UserRound,
} from "lucide-react";

import type {
  ThemeId,
} from "../theme/theme.types";

interface Props {
  storeName: string;
  announcement: string;
  themeId: ThemeId;
}

export function StorefrontHeader({
  storeName,
  announcement,
  themeId,
}: Props) {
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
                {storeName}
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

                <button
                  type="button"
                  aria-label="السلة"
                  className="flex size-10 items-center justify-center"
                >
                  <ShoppingBag
                    size={18}
                    strokeWidth={1.4}
                  />
                </button>
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
          <div className="bg-[var(--store-accent)] py-2.5 text-center text-[11px] font-semibold text-white">
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
              {storeName}
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
            </nav>

            <div className="mr-auto flex items-center gap-1">
              <button
                type="button"
                aria-label="الحساب"
                className="flex size-10 items-center justify-center"
              >
                <UserRound size={19} />
              </button>

              <button
                type="button"
                aria-label="السلة"
                className="relative flex size-10 items-center justify-center"
              >
                <ShoppingBag size={20} />

                <span className="absolute -left-1 top-0 flex size-[18px] items-center justify-center rounded-full bg-[var(--store-accent)] text-[9px] font-bold text-white">
                  2
                </span>
              </button>
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
              {storeName}
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

              <button
                type="button"
                className="flex size-10 items-center justify-center"
              >
                <ShoppingBag size={18} />
              </button>
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
          <div className="bg-[var(--store-ink)] py-2 text-center text-[10px] text-white">
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
                {storeName}
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
            </nav>

            <div className="flex gap-1">
              <button
                type="button"
                className="flex size-11 items-center justify-center rounded-full border border-black/10"
              >
                <Search size={18} />
              </button>

              <button
                type="button"
                className="flex size-11 items-center justify-center rounded-full bg-[var(--store-ink)] text-white"
              >
                <ShoppingBag size={18} />
              </button>
            </div>
          </div>
        </header>
      </>
    );
  }

  return (
    <>
      {announcement ? (
        <div className="bg-[var(--store-ink)] py-2 text-center text-[11px] text-white">
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
              {storeName}
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

            <button
              type="button"
              className="relative flex size-10 items-center justify-center"
            >
              <ShoppingBag size={19} />

              <span className="absolute left-0 top-0 flex size-[17px] items-center justify-center rounded-full bg-[var(--store-ink)] text-[9px] text-white">
                2
              </span>
            </button>
          </div>
        </div>
      </header>
    </>
  );
}