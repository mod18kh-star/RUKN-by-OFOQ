import {
  Building2,
  ExternalLink,
  Globe2,
  Landmark,
  MapPin,
  MessageCircle,
  Phone,
  Save,
  Share2,
  Smartphone,
} from "lucide-react";

import {
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  getStoreProfile,
  replaceStoreSocialLinks,
  updateStoreProfile,
  type StoreProfile,
  type StoreSocialLinkInput,
} from "./storeProfileApi";

interface Props {
  tenantId: string;
}

interface ProfileDraft {
  websiteUrl: string;
  whatsAppNumber: string;
  customerServicePhone: string;
  secondaryPhone: string;
  landlinePhone: string;
  physicalAddress: string;
  googleMapsUrl: string;
  commercialRegistrationNumber: string;
  commercialRegistrationNotApplicable: boolean;
  showWebsite: boolean;
  showWhatsApp: boolean;
  showCustomerServicePhone: boolean;
  showSecondaryPhone: boolean;
  showLandlinePhone: boolean;
  showPhysicalAddress: boolean;
  showCommercialRegistration: boolean;
}

interface SocialDraft {
  platformCode: string;
  label: string;
  url: string;
  isVisible: boolean;
}

const SOCIAL_PLATFORMS = [
  { code: "instagram", label: "Instagram", hint: "instagram.com/yourstore" },
  { code: "facebook", label: "Facebook", hint: "facebook.com/yourstore" },
  { code: "snapchat", label: "Snapchat", hint: "snapchat.com/add/yourstore" },
  { code: "tiktok", label: "TikTok", hint: "tiktok.com/@yourstore" },
  { code: "x", label: "X", hint: "x.com/yourstore" },
  { code: "youtube", label: "YouTube", hint: "youtube.com/@yourstore" },
  { code: "linkedin", label: "LinkedIn", hint: "linkedin.com/company/yourstore" },
  { code: "telegram", label: "Telegram", hint: "t.me/yourstore" },
] as const;

const EMPTY_PROFILE: ProfileDraft = {
  websiteUrl: "",
  whatsAppNumber: "",
  customerServicePhone: "",
  secondaryPhone: "",
  landlinePhone: "",
  physicalAddress: "",
  googleMapsUrl: "",
  commercialRegistrationNumber: "",
  commercialRegistrationNotApplicable: false,
  showWebsite: false,
  showWhatsApp: false,
  showCustomerServicePhone: false,
  showSecondaryPhone: false,
  showLandlinePhone: false,
  showPhysicalAddress: false,
  showCommercialRegistration: false,
};

export function StoreContactSettings({ tenantId }: Props) {
  const [profile, setProfile] = useState<ProfileDraft>(EMPTY_PROFILE);
  const [socials, setSocials] = useState<SocialDraft[]>(() =>
    SOCIAL_PLATFORMS.map((item) => ({
      platformCode: item.code,
      label: item.label,
      url: "",
      isVisible: true,
    })),
  );
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError(null);

      try {
        const result = await getStoreProfile(tenantId);

        if (cancelled) return;

        setProfile(profileFromResponse(result));
        setSocials(socialDraftsFromResponse(result));
      } catch (exception) {
        if (cancelled) return;

        setError(
          exception instanceof Error
            ? exception.message
            : "تعذر تحميل بيانات المتجر والتواصل.",
        );
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, [tenantId]);

  const publicCount = useMemo(() => {
    const flags = [
      profile.showWebsite && Boolean(profile.websiteUrl.trim()),
      profile.showWhatsApp && Boolean(profile.whatsAppNumber.trim()),
      profile.showCustomerServicePhone && Boolean(profile.customerServicePhone.trim()),
      profile.showSecondaryPhone && Boolean(profile.secondaryPhone.trim()),
      profile.showLandlinePhone && Boolean(profile.landlinePhone.trim()),
      profile.showPhysicalAddress && Boolean(profile.physicalAddress.trim()),
      profile.showCommercialRegistration && Boolean(profile.commercialRegistrationNumber.trim()),
      ...socials.map((item) => item.isVisible && Boolean(item.url.trim())),
    ];

    return flags.filter(Boolean).length;
  }, [profile, socials]);

  function patchProfile<K extends keyof ProfileDraft>(
    key: K,
    value: ProfileDraft[K],
  ) {
    setProfile((current) => ({
      ...current,
      [key]: value,
    }));
    setSuccess(null);
  }

  function patchSocial(
    platformCode: string,
    patch: Partial<SocialDraft>,
  ) {
    setSocials((current) =>
      current.map((item) =>
        item.platformCode === platformCode
          ? { ...item, ...patch }
          : item,
      ),
    );
    setSuccess(null);
  }

  async function save() {
    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      await updateStoreProfile(tenantId, {
        websiteUrl: nullable(profile.websiteUrl),
        whatsAppNumber: nullable(profile.whatsAppNumber),
        customerServicePhone: nullable(profile.customerServicePhone),
        secondaryPhone: nullable(profile.secondaryPhone),
        landlinePhone: nullable(profile.landlinePhone),
        physicalAddress: nullable(profile.physicalAddress),
        googleMapsUrl: nullable(profile.googleMapsUrl),
        commercialRegistrationNumber: profile.commercialRegistrationNotApplicable
          ? null
          : nullable(profile.commercialRegistrationNumber),
        commercialRegistrationNotApplicable: profile.commercialRegistrationNotApplicable,
        showWebsite: profile.showWebsite,
        showWhatsApp: profile.showWhatsApp,
        showCustomerServicePhone: profile.showCustomerServicePhone,
        showSecondaryPhone: profile.showSecondaryPhone,
        showLandlinePhone: profile.showLandlinePhone,
        showPhysicalAddress: profile.showPhysicalAddress,
        showCommercialRegistration:
          !profile.commercialRegistrationNotApplicable &&
          profile.showCommercialRegistration,
      });

      const socialPayload: StoreSocialLinkInput[] = socials
        .filter((item) => item.url.trim())
        .map((item) => ({
          platformCode: item.platformCode,
          label: nullable(item.label),
          url: item.url.trim(),
          isVisible: item.isVisible,
        }));

      await replaceStoreSocialLinks(tenantId, socialPayload);

      setSuccess("تم حفظ بيانات التواصل والظهور في المتجر.");
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "تعذر حفظ بيانات التواصل.",
      );
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return (
      <section className="mt-6 rounded-[20px] border border-black/[0.065] bg-white p-8 text-center text-[10px] text-black/40">
        جاري تحميل بيانات المتجر والتواصل...
      </section>
    );
  }

  return (
    <section className="mt-6 overflow-hidden rounded-[22px] border border-black/[0.065] bg-white shadow-[0_18px_60px_rgba(0,0,0,0.035)]">
      <div className="flex flex-col gap-4 border-b border-black/[0.06] px-5 py-5 md:flex-row md:items-center md:justify-between md:px-7">
        <div className="flex items-start gap-3">
          <div className="flex size-11 shrink-0 items-center justify-center rounded-[13px] bg-[#f0e9dc] text-[#8a6335]">
            <MessageCircle size={18} />
          </div>
          <div>
            <p className="text-[9px] font-semibold text-[#9a713f]">STORE CONTACT</p>
            <h2 className="mt-1 text-[18px] font-semibold tracking-[-0.025em]">بيانات المتجر والتواصل</h2>
            <p className="mt-2 max-w-[680px] text-[9px] leading-6 text-black/42">
              خزّن معلوماتك مرة واحدة وحدد ما يظهر للعميل. البيانات المخفية تبقى متاحة داخل إدارة ركن فقط.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="rounded-[12px] bg-[#f8f6f1] px-4 py-2.5 text-center">
            <p className="text-[7px] text-black/35">ظاهر للعميل</p>
            <p dir="ltr" className="mt-0.5 text-[17px] font-semibold tabular-nums">{publicCount}</p>
          </div>
          <button
            type="button"
            onClick={() => void save()}
            disabled={saving}
            className="inline-flex h-10 items-center gap-2 rounded-[10px] bg-[#0b0e14] px-4 text-[9px] font-semibold text-white transition hover:bg-black disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Save size={13} />
            {saving ? "جاري الحفظ..." : "حفظ البيانات"}
          </button>
        </div>
      </div>

      {error ? (
        <div className="mx-5 mt-5 rounded-[12px] border border-red-200 bg-red-50 px-4 py-3 text-[9px] text-red-700 md:mx-7">
          {error}
        </div>
      ) : null}

      {success ? (
        <div className="mx-5 mt-5 rounded-[12px] border border-emerald-200 bg-emerald-50 px-4 py-3 text-[9px] text-emerald-700 md:mx-7">
          {success}
        </div>
      ) : null}

      <div className="grid gap-6 p-5 md:p-7 xl:grid-cols-2">
        <SettingsPanel
          icon={Phone}
          eyebrow="CONTACT CHANNELS"
          title="أرقام وخدمة العملاء"
          description="أضف أكثر من وسيلة تواصل، وبعدها اختر كل وسيلة تريد عرضها في المتجر."
        >
          <ContactField
            label="رقم خدمة العملاء"
            icon={Phone}
            value={profile.customerServicePhone}
            onChange={(value) => patchProfile("customerServicePhone", value)}
            placeholder="+966 5X XXX XXXX"
            visible={profile.showCustomerServicePhone}
            onVisibleChange={(value) => patchProfile("showCustomerServicePhone", value)}
          />
          <ContactField
            label="رقم اتصال إضافي"
            icon={Smartphone}
            value={profile.secondaryPhone}
            onChange={(value) => patchProfile("secondaryPhone", value)}
            placeholder="+966 5X XXX XXXX"
            visible={profile.showSecondaryPhone}
            onVisibleChange={(value) => patchProfile("showSecondaryPhone", value)}
          />
          <ContactField
            label="الرقم الأرضي"
            icon={Landmark}
            value={profile.landlinePhone}
            onChange={(value) => patchProfile("landlinePhone", value)}
            placeholder="011 XXX XXXX"
            visible={profile.showLandlinePhone}
            onVisibleChange={(value) => patchProfile("showLandlinePhone", value)}
          />
          <ContactField
            label="WhatsApp"
            icon={MessageCircle}
            value={profile.whatsAppNumber}
            onChange={(value) => patchProfile("whatsAppNumber", value)}
            placeholder="+966 5X XXX XXXX"
            visible={profile.showWhatsApp}
            onVisibleChange={(value) => patchProfile("showWhatsApp", value)}
          />
          <ContactField
            label="الموقع الإلكتروني"
            icon={Globe2}
            value={profile.websiteUrl}
            onChange={(value) => patchProfile("websiteUrl", value)}
            placeholder="https://example.com"
            visible={profile.showWebsite}
            onVisibleChange={(value) => patchProfile("showWebsite", value)}
            dir="ltr"
          />
        </SettingsPanel>

        <div className="space-y-6">
          <SettingsPanel
            icon={MapPin}
            eyebrow="PHYSICAL LOCATION"
            title="موقع المتجر"
            description="إذا عندك فرع أو معرض فعلي، أضف العنوان ورابط الموقع على الخريطة."
          >
            <label className="block">
              <span className="mb-2 block text-[8px] font-semibold text-black/48">العنوان</span>
              <textarea
                value={profile.physicalAddress}
                onChange={(event) => patchProfile("physicalAddress", event.target.value)}
                placeholder="المدينة، الحي، الشارع، اسم المبنى أو المعرض"
                className="min-h-[88px] w-full resize-none rounded-[11px] border border-black/[0.08] bg-[#fbfaf7] p-3 text-[10px] leading-6 outline-none transition focus:border-black/20"
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-[8px] font-semibold text-black/48">رابط Google Maps</span>
              <div className="relative">
                <ExternalLink size={13} className="absolute right-3 top-1/2 -translate-y-1/2 text-black/25" />
                <input
                  dir="ltr"
                  value={profile.googleMapsUrl}
                  onChange={(event) => patchProfile("googleMapsUrl", event.target.value)}
                  placeholder="https://maps.google.com/..."
                  className="h-10 w-full rounded-[11px] border border-black/[0.08] bg-[#fbfaf7] pr-9 pl-3 text-left text-[9px] outline-none transition focus:border-black/20"
                />
              </div>
            </label>

            <VisibilityRow
              label="إظهار الموقع للعميل"
              description="يعرض العنوان ورابط الخريطة في صفحة التواصل والفوتر."
              checked={profile.showPhysicalAddress}
              onChange={(value) => patchProfile("showPhysicalAddress", value)}
            />
          </SettingsPanel>

          <SettingsPanel
            icon={Building2}
            eyebrow="BUSINESS INFO"
            title="المعلومات التجارية"
            description="السجل يبقى محفوظًا عندنا حتى لو اخترت عدم عرضه في المتجر."
          >
            <label className="block">
              <span className="mb-2 block text-[8px] font-semibold text-black/48">رقم السجل التجاري</span>
              <input
                dir="ltr"
                value={profile.commercialRegistrationNumber}
                disabled={profile.commercialRegistrationNotApplicable}
                onChange={(event) => patchProfile("commercialRegistrationNumber", event.target.value)}
                placeholder="Commercial Registration No."
                className="h-10 w-full rounded-[11px] border border-black/[0.08] bg-[#fbfaf7] px-3 text-left text-[10px] outline-none transition focus:border-black/20 disabled:cursor-not-allowed disabled:opacity-45"
              />
            </label>

            <label className="flex cursor-pointer items-center gap-3 rounded-[11px] border border-black/[0.06] bg-[#fbfaf7] px-3 py-3">
              <input
                type="checkbox"
                checked={profile.commercialRegistrationNotApplicable}
                onChange={(event) => {
                  patchProfile("commercialRegistrationNotApplicable", event.target.checked);
                  if (event.target.checked) {
                    patchProfile("showCommercialRegistration", false);
                  }
                }}
                className="size-4 accent-black"
              />
              <div>
                <p className="text-[9px] font-semibold">لا ينطبق على نشاطي حاليًا</p>
                <p className="mt-1 text-[8px] text-black/36">لن يظهر رقم سجل تجاري في واجهة المتجر.</p>
              </div>
            </label>

            <VisibilityRow
              label="إظهار رقم السجل للعميل"
              description="يظهر في أسفل المتجر وصفحة التواصل."
              checked={profile.showCommercialRegistration}
              disabled={profile.commercialRegistrationNotApplicable}
              onChange={(value) => patchProfile("showCommercialRegistration", value)}
            />
          </SettingsPanel>
        </div>
      </div>

      <div className="border-t border-black/[0.06] p-5 md:p-7">
        <div className="flex items-start gap-3">
          <div className="flex size-10 shrink-0 items-center justify-center rounded-[12px] bg-[#eef2f6] text-[#455b76]">
            <Share2 size={17} />
          </div>
          <div>
            <p className="text-[9px] font-semibold text-[#58708b]">SOCIAL MEDIA</p>
            <h3 className="mt-1 text-[15px] font-semibold">حسابات التواصل الاجتماعي</h3>
            <p className="mt-1.5 text-[9px] leading-6 text-black/40">
              أضف الروابط التي تستخدمها فقط. كل منصة تقدر تخليها ظاهرة أو داخلية بشكل مستقل.
            </p>
          </div>
        </div>

        <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {socials.map((social) => {
            const platform = SOCIAL_PLATFORMS.find((item) => item.code === social.platformCode);

            return (
              <div key={social.platformCode} className="rounded-[15px] border border-black/[0.065] bg-[#fbfaf7] p-3.5">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="text-[9px] font-semibold">{social.label}</p>
                    <p className="mt-0.5 text-[7px] uppercase tracking-[0.08em] text-black/30">{social.platformCode}</p>
                  </div>
                  <MiniVisibility
                    checked={social.isVisible}
                    disabled={!social.url.trim()}
                    onChange={(value) => patchSocial(social.platformCode, { isVisible: value })}
                  />
                </div>

                <input
                  dir="ltr"
                  value={social.url}
                  onChange={(event) => patchSocial(social.platformCode, { url: event.target.value })}
                  placeholder={platform?.hint ?? "https://..."}
                  className="mt-3 h-9 w-full rounded-[9px] border border-black/[0.07] bg-white px-2.5 text-left text-[8px] outline-none transition focus:border-black/20"
                />
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}

function SettingsPanel({
  icon: Icon,
  eyebrow,
  title,
  description,
  children,
}: {
  icon: typeof Phone;
  eyebrow: string;
  title: string;
  description: string;
  children: React.ReactNode;
}) {
  return (
    <div className="rounded-[18px] border border-black/[0.065] bg-[#fcfbf8] p-5">
      <div className="flex items-start gap-3">
        <div className="flex size-9 shrink-0 items-center justify-center rounded-[10px] bg-white text-black/55 shadow-sm">
          <Icon size={15} />
        </div>
        <div>
          <p className="text-[7px] font-semibold tracking-[0.08em] text-black/30">{eyebrow}</p>
          <h3 className="mt-1 text-[13px] font-semibold">{title}</h3>
          <p className="mt-1.5 text-[8px] leading-5 text-black/38">{description}</p>
        </div>
      </div>
      <div className="mt-5 space-y-4">{children}</div>
    </div>
  );
}

function ContactField({
  label,
  icon: Icon,
  value,
  onChange,
  placeholder,
  visible,
  onVisibleChange,
  dir = "rtl",
}: {
  label: string;
  icon: typeof Phone;
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
  visible: boolean;
  onVisibleChange: (value: boolean) => void;
  dir?: "rtl" | "ltr";
}) {
  return (
    <div>
      <div className="mb-2 flex items-center justify-between gap-3">
        <span className="text-[8px] font-semibold text-black/48">{label}</span>
        <MiniVisibility checked={visible} disabled={!value.trim()} onChange={onVisibleChange} />
      </div>
      <div className="relative">
        <Icon size={13} className="absolute right-3 top-1/2 -translate-y-1/2 text-black/25" />
        <input
          dir={dir}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder={placeholder}
          className={`h-10 w-full rounded-[11px] border border-black/[0.08] bg-white pr-9 pl-3 text-[9px] outline-none transition focus:border-black/20 ${dir === "ltr" ? "text-left" : ""}`}
        />
      </div>
    </div>
  );
}

function VisibilityRow({
  label,
  description,
  checked,
  onChange,
  disabled = false,
}: {
  label: string;
  description: string;
  checked: boolean;
  onChange: (value: boolean) => void;
  disabled?: boolean;
}) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-[11px] border border-black/[0.06] bg-white px-3 py-3">
      <div>
        <p className="text-[9px] font-semibold">{label}</p>
        <p className="mt-1 text-[8px] text-black/36">{description}</p>
      </div>
      <MiniVisibility checked={checked} disabled={disabled} onChange={onChange} />
    </div>
  );
}

function MiniVisibility({
  checked,
  disabled = false,
  onChange,
}: {
  checked: boolean;
  disabled?: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={`relative h-5 w-9 shrink-0 rounded-full transition ${checked ? "bg-[#11151d]" : "bg-black/12"} disabled:cursor-not-allowed disabled:opacity-35`}
      aria-pressed={checked}
      aria-label={checked ? "ظاهر للعميل" : "مخفي عن العميل"}
    >
      <span
        className={`absolute top-0.5 size-4 rounded-full bg-white shadow transition ${checked ? "right-[18px]" : "right-0.5"}`}
      />
    </button>
  );
}

function nullable(value: string) {
  const clean = value.trim();
  return clean ? clean : null;
}

function profileFromResponse(profile: StoreProfile): ProfileDraft {
  return {
    websiteUrl: profile.websiteUrl ?? "",
    whatsAppNumber: profile.whatsAppNumber ?? "",
    customerServicePhone: profile.customerServicePhone ?? "",
    secondaryPhone: profile.secondaryPhone ?? "",
    landlinePhone: profile.landlinePhone ?? "",
    physicalAddress: profile.physicalAddress ?? "",
    googleMapsUrl: profile.googleMapsUrl ?? "",
    commercialRegistrationNumber: profile.commercialRegistrationNumber ?? "",
    commercialRegistrationNotApplicable: profile.commercialRegistrationNotApplicable,
    showWebsite: profile.showWebsite,
    showWhatsApp: profile.showWhatsApp,
    showCustomerServicePhone: profile.showCustomerServicePhone,
    showSecondaryPhone: profile.showSecondaryPhone,
    showLandlinePhone: profile.showLandlinePhone,
    showPhysicalAddress: profile.showPhysicalAddress,
    showCommercialRegistration: profile.showCommercialRegistration,
  };
}

function socialDraftsFromResponse(profile: StoreProfile): SocialDraft[] {
  const existing = new Map(
    profile.socialLinks.map((item) => [item.platformCode, item]),
  );

  const known = SOCIAL_PLATFORMS.map((platform) => {
    const saved = existing.get(platform.code);
    existing.delete(platform.code);

    return {
      platformCode: platform.code,
      label: saved?.label?.trim() || platform.label,
      url: saved?.url ?? "",
      isVisible: saved?.isVisible ?? true,
    };
  });

  const custom = [...existing.values()].map((item) => ({
    platformCode: item.platformCode,
    label: item.label?.trim() || item.platformCode,
    url: item.url,
    isVisible: item.isVisible,
  }));

  return [...known, ...custom];
}
