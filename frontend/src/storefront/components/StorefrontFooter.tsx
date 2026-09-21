import {
  ExternalLink,
  Globe2,
  MapPin,
  MessageCircle,
  Phone,
} from "lucide-react";

import {
  useQuery,
} from "@tanstack/react-query";

import {
  Link,
} from "react-router";

import {
  getStorefrontNavigation,
  type StorefrontContact,
} from "../data/storefrontApi";

import type {
  ThemeId,
} from "../theme/theme.types";

interface Props {
  storeSlug: string;
  storeName: string;
  logoUrl: string;
  themeId: ThemeId;
  contact: StorefrontContact;
  description?: string;
}

export function StorefrontFooter({
  storeSlug,
  storeName,
  logoUrl,
  themeId,
  contact,
  description,
}: Props) {
  const footerNavigationQuery = useQuery({
    queryKey: [
      "storefront-runtime",
      storeSlug,
      "navigation",
      "Footer",
    ],
    queryFn: () => getStorefrontNavigation(storeSlug, "Footer"),
    enabled: Boolean(storeSlug.trim()),
    retry: 1,
    staleTime: 60_000,
  });

  const footerItems = (footerNavigationQuery.data ?? [])
    .filter((item) => !item.parentItemId)
    .slice(0, 8);

  const contactItems = [
    contact.customerServicePhone
      ? {
          key: "primary-phone",
          label: "خدمة العملاء",
          value: contact.customerServicePhone,
          href: `tel:${contact.customerServicePhone}`,
          icon: Phone,
        }
      : null,
    contact.secondaryPhone
      ? {
          key: "secondary-phone",
          label: "اتصال إضافي",
          value: contact.secondaryPhone,
          href: `tel:${contact.secondaryPhone}`,
          icon: Phone,
        }
      : null,
    contact.landlinePhone
      ? {
          key: "landline-phone",
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

  const isFlagship = themeId === "mobile-flagship";
  const isSmartMarket = themeId === "mobile-smart-market";

  return (
    <footer
      className={[
        "mt-16 border-t border-black/[0.07]",
        isFlagship
          ? "bg-[var(--store-ink)] text-[var(--store-ink-contrast)]"
          : isSmartMarket
            ? "bg-[#0e1726] text-white"
            : "bg-[var(--store-surface)] text-[var(--store-ink)]",
      ].join(" ")}
    >
      <div className="store-container py-12 md:py-16">
        <div className="grid gap-10 md:grid-cols-2 xl:grid-cols-[1.15fr_.8fr_1fr_1fr]">
          <div>
            <StoreFooterBrand storeName={storeName} logoUrl={logoUrl} />
            <p data-rukn-target="footerDescription" className={`mt-5 max-w-[370px] text-[12px] leading-7 ${isFlagship || isSmartMarket ? "text-white/58" : "text-[var(--store-muted)]"}`}>
              {description || "كل ما تحتاجه للوصول إلى المتجر وخدمة العملاء وحساباتنا الرسمية في مكان واحد."}
            </p>

            {contact.socialLinks.length > 0 ? (
              <div className="mt-6 flex flex-wrap gap-2">
                {contact.socialLinks.map((item) => (
                  <a
                    key={`${item.platformCode}-${item.url}`}
                    href={item.url}
                    target="_blank"
                    rel="noreferrer"
                    aria-label={socialLabel(item.platformCode, item.label)}
                    title={socialLabel(item.platformCode, item.label)}
                    className={socialButtonClass(isFlagship || isSmartMarket)}
                  >
                    {socialIcon(item.platformCode)}
                  </a>
                ))}
              </div>
            ) : null}
          </div>

          <FooterColumn title="المتجر" dark={isFlagship || isSmartMarket}>
            <FooterLink to={`/store/${encodeURIComponent(storeSlug)}`} label="الرئيسية" dark={isFlagship || isSmartMarket} />
            <a href={`/store/${encodeURIComponent(storeSlug)}#categories`} className={footerLinkClass(isFlagship || isSmartMarket)}>الأقسام</a>
            <a href={`/store/${encodeURIComponent(storeSlug)}#products`} className={footerLinkClass(isFlagship || isSmartMarket)}>المنتجات</a>
            <FooterLink to={`/store/${encodeURIComponent(storeSlug)}/contact`} label="تواصل معنا" dark={isFlagship || isSmartMarket} />
            {footerItems.map((item) => {
              const external = /^https?:\/\//i.test(item.href);

              return external ? (
                <a key={item.id} href={item.href} target="_blank" rel="noreferrer" className={footerLinkClass(isFlagship || isSmartMarket)}>
                  {item.label}
                </a>
              ) : (
                <FooterLink key={item.id} to={item.href} label={item.label} dark={isFlagship || isSmartMarket} />
              );
            })}
          </FooterColumn>

          <FooterColumn title="التواصل" dark={isFlagship || isSmartMarket}>
            {contactItems.length > 0 ? (
              contactItems.map((item) => {
                const Icon = item.icon;

                return (
                  <a key={item.key} href={item.href} className={`group flex items-start gap-2.5 ${isFlagship || isSmartMarket ? "text-white/62 hover:text-white" : "text-[var(--store-ink-soft)] hover:text-[var(--store-ink)]"}`}>
                    <Icon size={14} className="mt-0.5 shrink-0 opacity-70" />
                    <span>
                      <span className="block text-[8px] opacity-60">{item.label}</span>
                      <span dir="ltr" className="mt-0.5 block text-right text-[10px] font-medium tabular-nums">{item.value}</span>
                    </span>
                  </a>
                );
              })
            ) : (
              <p className={`text-[10px] leading-6 ${isFlagship || isSmartMarket ? "text-white/42" : "text-[var(--store-muted)]"}`}>
                بيانات التواصل ستظهر هنا عند إضافتها من لوحة المتجر.
              </p>
            )}
          </FooterColumn>

          <FooterColumn title="الموقع والمعلومات" dark={isFlagship || isSmartMarket}>
            {contact.physicalAddress ? (
              <div className={`flex items-start gap-2.5 ${isFlagship || isSmartMarket ? "text-white/62" : "text-[var(--store-ink-soft)]"}`}>
                <MapPin size={14} className="mt-1 shrink-0" />
                <div>
                  <p className="text-[10px] leading-6">{contact.physicalAddress}</p>
                  {contact.googleMapsUrl ? (
                    <a href={contact.googleMapsUrl} target="_blank" rel="noreferrer" className="mt-2 inline-flex items-center gap-1 text-[9px] font-semibold text-[var(--store-accent)]">
                      فتح الموقع
                      <ExternalLink size={11} />
                    </a>
                  ) : null}
                </div>
              </div>
            ) : null}

            {contact.websiteUrl ? (
              <a href={contact.websiteUrl} target="_blank" rel="noreferrer" className={`flex items-center gap-2 text-[10px] ${isFlagship || isSmartMarket ? "text-white/62 hover:text-white" : "text-[var(--store-ink-soft)] hover:text-[var(--store-ink)]"}`}>
                <Globe2 size={14} />
                الموقع الإلكتروني
                <ExternalLink size={11} />
              </a>
            ) : null}

            {contact.commercialRegistrationNumber ? (
              <div className={`rounded-[12px] border px-3 py-2.5 ${isFlagship || isSmartMarket ? "border-white/10 bg-white/[0.04]" : "border-black/[0.06] bg-black/[0.02]"}`}>
                <p className={`text-[8px] ${isFlagship || isSmartMarket ? "text-white/40" : "text-[var(--store-muted)]"}`}>السجل التجاري</p>
                <p dir="ltr" className="mt-1 text-right text-[11px] font-semibold tabular-nums">{contact.commercialRegistrationNumber}</p>
              </div>
            ) : null}
          </FooterColumn>
        </div>

        <div className={`mt-12 flex flex-col gap-3 border-t pt-5 text-[9px] md:flex-row md:items-center md:justify-between ${isFlagship || isSmartMarket ? "border-white/10 text-white/36" : "border-black/[0.06] text-[var(--store-muted)]"}`}>
          <p>© {new Date().getFullYear()} {storeName}. جميع الحقوق محفوظة.</p>
          <p>متجر إلكتروني مُدار عبر ركن</p>
        </div>
      </div>
    </footer>
  );
}

function StoreFooterBrand({
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
        className="max-h-12 max-w-[190px] object-contain"
      />
    );
  }

  return (
    <p className="text-[26px] font-semibold tracking-[-0.045em]">
      {storeName}
    </p>
  );
}

function FooterColumn({
  title,
  dark,
  children,
}: {
  title: string;
  dark: boolean;
  children: React.ReactNode;
}) {
  return (
    <div>
      <p className={`text-[10px] font-semibold ${dark ? "text-white/90" : "text-[var(--store-ink)]"}`}>{title}</p>
      <div className="mt-5 space-y-3">{children}</div>
    </div>
  );
}

function FooterLink({
  to,
  label,
  dark,
}: {
  to: string;
  label: string;
  dark: boolean;
}) {
  return (
    <Link to={to} className={footerLinkClass(dark)}>
      {label}
    </Link>
  );
}

function footerLinkClass(dark: boolean) {
  return `block text-[10px] transition ${dark ? "text-white/58 hover:text-white" : "text-[var(--store-ink-soft)] hover:text-[var(--store-ink)]"}`;
}

function whatsappUrl(number: string) {
  const digits = number.replace(/\D+/g, "");
  return digits ? `https://wa.me/${digits}` : "#";
}


function socialButtonClass(
  dark: boolean,
) {
  void dark;

  return "store-social-link inline-flex size-10 items-center justify-center rounded-full border border-current bg-transparent text-[var(--store-accent)] transition duration-200 hover:-translate-y-0.5";
}

function socialIcon(platformCode: string) {
  const common = {
    width: 18,
    height: 18,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
    "aria-hidden": true,
  };

  switch (platformCode) {
    case "instagram":
      return (
        <svg {...common}>
          <rect x="3.5" y="3.5" width="17" height="17" rx="5" />
          <circle cx="12" cy="12" r="4" />
          <circle cx="17.4" cy="6.8" r=".8" fill="currentColor" stroke="none" />
        </svg>
      );
    case "facebook":
      return (
        <svg {...common}>
          <path d="M13.5 20v-7h2.6l.4-3h-3V8.1c0-.9.3-1.6 1.6-1.6h1.7V3.8c-.3 0-1.3-.1-2.4-.1-2.4 0-4 1.4-4 4.1V10H8v3h2.4v7" />
        </svg>
      );
    case "snapchat":
      return (
        <svg {...common}>
          <path d="M8.2 9.2c0-3 1.5-5 3.8-5s3.8 2 3.8 5c0 1 .3 1.7.9 2.3.6.6 1.4.9 2.2 1.2-.4 1.1-1.2 1.5-2.3 1.7-.3 1.5-1.3 2.2-2.8 2.4-.5.1-.9.5-1.8 1.1-.9-.6-1.3-1-1.8-1.1-1.5-.2-2.5-.9-2.8-2.4-1.1-.2-1.9-.6-2.3-1.7.8-.3 1.6-.6 2.2-1.2.6-.6.9-1.3.9-2.3Z" />
        </svg>
      );
    case "tiktok":
      return (
        <svg {...common}>
          <path d="M14 4v10.2a3.8 3.8 0 1 1-3-3.7" />
          <path d="M14 5c1.1 2 2.6 3.1 4.7 3.3" />
        </svg>
      );
    case "x":
      return (
        <svg {...common}>
          <path d="M5 4l14 16" />
          <path d="M19 4L5 20" />
        </svg>
      );
    case "youtube":
      return (
        <svg {...common}>
          <rect x="3" y="6" width="18" height="12" rx="4" />
          <path d="m10 9 5 3-5 3Z" fill="currentColor" stroke="none" />
        </svg>
      );
    case "linkedin":
      return (
        <svg {...common}>
          <circle cx="6.2" cy="7" r="1.2" fill="currentColor" stroke="none" />
          <path d="M5 10v8" />
          <path d="M10 18v-5.2c0-1.8 1-2.8 2.6-2.8 1.8 0 2.9 1.1 2.9 3.2V18" />
          <path d="M10 10v8" />
        </svg>
      );
    case "telegram":
      return (
        <svg {...common}>
          <path d="m4 11 16-6-4.5 14-4.2-4-3 2.4.7-4.7L18 7.5" />
        </svg>
      );
    default:
      return <Globe2 size={17} strokeWidth={1.8} />;
  }
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
