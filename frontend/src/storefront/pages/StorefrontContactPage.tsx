import {
  ExternalLink,
  Globe2,
  MapPin,
  MessageCircle,
  Phone,
  Share2,
} from "lucide-react";

import {
  useMemo,
} from "react";

import {
  useQuery,
} from "@tanstack/react-query";

import {
  Link,
  useParams,
} from "react-router";

import {
  StorefrontFooter,
} from "../components/StorefrontFooter";

import {
  StorefrontHeader,
} from "../components/StorefrontHeader";

import { parseVisualContent } from "../config/visualContent";

import {
  createLiveStorefrontConfig,
} from "../config/liveStorefrontConfig";

import {
  getStorefrontInfo,
} from "../data/storefrontApi";

import {
  createThemeStyle,
  resolveStorefrontConfig,
} from "../theme/themeEngine";

import {
  getThemePreset,
} from "../theme/themePresets";

export function StorefrontContactPage() {
  const { storeSlug = "" } = useParams<{
    storeSlug: string;
  }>();

  const storeQuery = useQuery({
    queryKey: [
      "storefront-runtime",
      storeSlug,
      "info",
    ],
    queryFn: () => getStorefrontInfo(storeSlug),
    enabled: Boolean(storeSlug.trim()),
    retry: 1,
    staleTime: 5 * 60_000,
  });

  const store = storeQuery.data ?? null;

  const config = useMemo(
    () =>
      store
        ? resolveStorefrontConfig(
            createLiveStorefrontConfig(store),
          )
        : null,
    [store],
  );

  if (storeQuery.isPending) {
    return (
      <div
        dir="rtl"
        className="flex min-h-screen items-center justify-center bg-[#f6f4ee] text-sm text-black/45"
      >
        جاري تحميل بيانات التواصل...
      </div>
    );
  }

  if (storeQuery.isError || !store || !config) {
    return (
      <div
        dir="rtl"
        className="flex min-h-screen items-center justify-center bg-[#f6f4ee] px-5 text-center"
      >
        <div>
          <h1 className="text-xl font-semibold">تعذر فتح صفحة التواصل</h1>
          <Link
            to={`/store/${encodeURIComponent(storeSlug)}`}
            className="mt-5 inline-flex text-sm underline"
          >
            العودة للمتجر
          </Link>
        </div>
      </div>
    );
  }

  const contact = store.contact;
  const contactMethods = [
    contact.customerServicePhone
      ? {
          key: "service",
          label: "خدمة العملاء",
          value: contact.customerServicePhone,
          href: `tel:${contact.customerServicePhone}`,
          icon: Phone,
        }
      : null,
    contact.secondaryPhone
      ? {
          key: "secondary",
          label: "رقم اتصال إضافي",
          value: contact.secondaryPhone,
          href: `tel:${contact.secondaryPhone}`,
          icon: Phone,
        }
      : null,
    contact.landlinePhone
      ? {
          key: "landline",
          label: "الهاتف الأرضي",
          value: contact.landlinePhone,
          href: `tel:${contact.landlinePhone}`,
          icon: Phone,
        }
      : null,
    contact.whatsAppNumber
      ? {
          key: "whatsapp",
          label: "WhatsApp",
          value: contact.whatsAppNumber,
          href: whatsappUrl(contact.whatsAppNumber),
          icon: MessageCircle,
        }
      : null,
  ].filter((item): item is NonNullable<typeof item> => Boolean(item));

  const hasPublicContact =
    contactMethods.length > 0 ||
    Boolean(contact.websiteUrl) ||
    Boolean(contact.physicalAddress) ||
    Boolean(contact.commercialRegistrationNumber) ||
    contact.socialLinks.length > 0;

  const theme = getThemePreset(config.themeId);
  const flagship = config.themeId === "mobile-flagship" || theme.experience === "signature";
  const smartMarket = config.themeId === "mobile-smart-market" || theme.experience === "market";

  return (
    <div
      dir="rtl"
      data-store-theme={config.themeId}
      style={createThemeStyle(config)}
      className="min-h-screen bg-[var(--store-canvas)] font-[var(--store-font)] text-[var(--store-ink)]"
    >
      <StorefrontHeader
        storeSlug={storeSlug}
        storeName={config.storeName}
        logoUrl={config.logoUrl}
        announcement={config.announcement}
        themeId={config.themeId}
      />

      <main className="store-container py-10 md:py-16 lg:py-20">
        <div className="mb-9 flex items-center gap-2 text-[11px] font-medium text-[var(--store-muted)]">
          <Link
            to={`/store/${encodeURIComponent(storeSlug)}`}
            className="transition hover:text-[var(--store-ink)]"
          >
            الرئيسية
          </Link>
          <span>/</span>
          <span>تواصل معنا</span>
        </div>

        <section
          className={[
            "overflow-hidden border border-black/[0.07]",
            flagship
              ? "rounded-[34px] bg-[var(--store-surface)] shadow-[0_28px_90px_rgba(23,22,18,0.07)]"
              : smartMarket
                ? "rounded-[24px] bg-white shadow-[0_20px_65px_rgba(26,42,69,0.07)]"
                : "rounded-[24px] bg-[var(--store-surface)]",
          ].join(" ")}
        >
          <div className="border-b border-black/[0.06] px-7 py-10 md:px-12 md:py-14 lg:px-16">
            <p className="text-[10px] font-semibold tracking-[0.08em] text-[var(--store-accent)]">
              CONTACT
            </p>
            <h1 className="mt-4 max-w-[850px] text-[clamp(2.6rem,6vw,5.4rem)] font-semibold leading-[1.08] tracking-[-0.055em]">
              نحن قريبون منك.
            </h1>
            <p className="mt-5 max-w-[680px] text-[14px] leading-8 text-[var(--store-ink-soft)]">
              اختر وسيلة التواصل الأنسب لك، أو زر موقعنا إذا كان للمتجر فرع فعلي.
            </p>
          </div>

          <div className="grid gap-6 px-7 py-10 md:px-12 md:py-14 lg:grid-cols-[1.05fr_.95fr] lg:px-16">
            <div>
              <div className="flex items-center gap-2">
                <Phone size={17} />
                <h2 className="text-[18px] font-semibold">قنوات التواصل</h2>
              </div>

              {contactMethods.length > 0 ? (
                <div className="mt-5 grid gap-3 sm:grid-cols-2">
                  {contactMethods.map((item) => {
                    const Icon = item.icon;

                    return (
                      <a
                        key={item.key}
                        href={item.href}
                        className="group rounded-[18px] border border-black/[0.07] bg-[var(--store-soft)] p-5 transition hover:-translate-y-0.5 hover:border-black/15"
                      >
                        <div className="flex size-10 items-center justify-center rounded-[12px] bg-[var(--store-surface)] text-[var(--store-accent)] shadow-sm">
                          <Icon size={17} />
                        </div>
                        <p className="mt-5 text-[10px] font-semibold text-[var(--store-muted)]">{item.label}</p>
                        <p dir="ltr" className="mt-1 text-right text-[16px] font-semibold tabular-nums">{item.value}</p>
                      </a>
                    );
                  })}
                </div>
              ) : (
                <EmptyBlock text="لم يحدد المتجر أرقام تواصل عامة حتى الآن." />
              )}

              {contact.websiteUrl ? (
                <a
                  href={contact.websiteUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="mt-3 flex items-center justify-between gap-4 rounded-[18px] border border-black/[0.07] bg-[var(--store-soft)] p-5 transition hover:border-black/15"
                >
                  <div className="flex items-center gap-3">
                    <div className="flex size-10 items-center justify-center rounded-[12px] bg-[var(--store-surface)] text-[var(--store-accent)]">
                      <Globe2 size={17} />
                    </div>
                    <div>
                      <p className="text-[10px] font-semibold">الموقع الإلكتروني</p>
                      <p className="mt-1 text-[9px] text-[var(--store-muted)]">زيارة الموقع الرسمي للمتجر</p>
                    </div>
                  </div>
                  <ExternalLink size={15} />
                </a>
              ) : null}
            </div>

            <div className="space-y-4">
              <div className="flex items-center gap-2">
                <MapPin size={17} />
                <h2 className="text-[18px] font-semibold">الموقع والمعلومات</h2>
              </div>

              {contact.physicalAddress ? (
                <div className="rounded-[20px] border border-black/[0.07] bg-[var(--store-soft)] p-6">
                  <p className="text-[9px] font-semibold text-[var(--store-accent)]">موقع المتجر</p>
                  <p className="mt-3 text-[14px] leading-8 text-[var(--store-ink-soft)]">{contact.physicalAddress}</p>
                  {contact.googleMapsUrl ? (
                    <a
                      href={contact.googleMapsUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="mt-5 inline-flex h-10 items-center gap-2 rounded-[10px] bg-[var(--store-ink)] px-4 text-[10px] font-semibold text-[var(--store-ink-contrast)]"
                    >
                      فتح على الخريطة
                      <ExternalLink size={13} />
                    </a>
                  ) : null}
                </div>
              ) : null}

              {contact.commercialRegistrationNumber ? (
                <div className="rounded-[18px] border border-black/[0.07] bg-[var(--store-soft)] p-5">
                  <p className="text-[9px] text-[var(--store-muted)]">رقم السجل التجاري</p>
                  <p dir="ltr" className="mt-2 text-right text-[19px] font-semibold tabular-nums">{contact.commercialRegistrationNumber}</p>
                </div>
              ) : null}

              {contact.socialLinks.length > 0 ? (
                <div className="rounded-[20px] border border-black/[0.07] bg-[var(--store-soft)] p-6">
                  <div className="flex items-center gap-2">
                    <Share2 size={16} />
                    <p className="text-[12px] font-semibold">حساباتنا الرسمية</p>
                  </div>
                  <div className="mt-5 grid grid-cols-2 gap-2 sm:grid-cols-3">
                    {contact.socialLinks.map((item) => (
                      <a
                        key={`${item.platformCode}-${item.url}`}
                        href={item.url}
                        target="_blank"
                        rel="noreferrer"
                        className="rounded-[11px] border border-black/[0.06] bg-[var(--store-surface)] px-3 py-3 text-center text-[10px] font-semibold transition hover:border-black/15"
                      >
                        {socialLabel(item.platformCode, item.label)}
                      </a>
                    ))}
                  </div>
                </div>
              ) : null}

              {!hasPublicContact ? (
                <EmptyBlock text="بيانات التواصل العامة لم تتم إضافتها بعد." />
              ) : null}
            </div>
          </div>
        </section>
      </main>

      <StorefrontFooter
        storeSlug={storeSlug}
        storeName={config.storeName}
        logoUrl={config.logoUrl}
        themeId={config.themeId}
        contact={contact}
        description={parseVisualContent(storeQuery.data?.presentation.visualContentJson).footerDescription}
      />
    </div>
  );
}

function EmptyBlock({ text }: { text: string }) {
  return (
    <div className="mt-5 rounded-[18px] border border-dashed border-black/10 bg-[var(--store-soft)] p-6 text-[11px] leading-7 text-[var(--store-muted)]">
      {text}
    </div>
  );
}

function whatsappUrl(number: string) {
  const digits = number.replace(/\D+/g, "");
  return digits ? `https://wa.me/${digits}` : "#";
}

function socialLabel(platformCode: string, label: string | null) {
  if (label?.trim()) {
    return label.trim();
  }

  const labels: Record<string, string> = {
    instagram: "Instagram",
    facebook: "Facebook",
    snapchat: "Snapchat",
    tiktok: "TikTok",
    x: "X",
    youtube: "YouTube",
    linkedin: "LinkedIn",
    telegram: "Telegram",
  };

  return labels[platformCode] ?? platformCode;
}
