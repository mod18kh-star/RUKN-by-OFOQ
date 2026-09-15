import {
  ArrowDown,
  ArrowUp,

  Layers3,
  LayoutTemplate,
  Palette,
  Plus,
  Save,
  Type,
} from "lucide-react";

import {
  useEffect,
  useState,
} from "react";

import {
  BuilderField,
  BuilderPanel,
  BuilderSwitch,
  BuilderTextarea,
  ChoiceCard,
  ImageUrlField,
} from "./store-builder/BuilderControls";

import {
  StorePreviewFrame,
} from "./store-builder/StorePreviewFrame";

import {
  UpgradeNoticeDialog,
} from "./store-builder/UpgradeNoticeDialog";

import {
  SectionLayoutControls,
} from "./store-builder/SectionLayoutControls";

import {
  loadStorefrontConfig,
  saveStorefrontConfig,
} from "../../storefront/config/storefrontConfig";

import {
  STOREFRONT_FONTS,
} from "../../storefront/theme/fonts";

import {
  getPlanEntitlements,
} from "../../storefront/theme/planEntitlements";

import {
  THEME_PRESETS,
} from "../../storefront/theme/themePresets";

import type {
  HeroLayout,
  ProductCardStyle,
  StorefrontConfig,
  StorefrontSectionKey,
} from "../../storefront/theme/theme.types";

type BuilderTab =
  | "content"
  | "design"
  | "sections";

const tabs: Array<{
  id: BuilderTab;
  label: string;
  description: string;
  icon: typeof Palette;
}> = [
  {
    id:
      "content",

    label:
      "المحتوى",

    description:
      "النصوص والصور",

    icon:
      Type,
  },

  {
    id:
      "design",

    label:
      "التصميم",

    description:
      "الثيم وطريقة العرض",

    icon:
      Palette,
  },

  {
    id:
      "sections",

    label:
      "الأقسام",

    description:
      "الإضافة والترتيب",

    icon:
      Layers3,
  },
];

const CARD_STYLES: Array<{
  id: ProductCardStyle;
  name: string;
  description: string;
}> = [
  {
    id:
      "minimal",

    name:
      "Minimal",

    description:
      "صورة واسم وسعر بأقل عناصر ممكنة",
  },

  {
    id:
      "editorial",

    name:
      "Editorial",

    description:
      "عرض بصري هادئ للبراندات",
  },

  {
    id:
      "commerce",

    name:
      "Commerce",

    description:
      "أوضح للبيع والإضافة للسلة",
  },

  {
    id:
      "compact",

    name:
      "Compact",

    description:
      "مناسب للكتالوجات الكبيرة",
  },

  {
    id:
      "technical",

    name:
      "Technical",

    description:
      "معلومات أكثر للمنتجات التقنية",
  },
];

const HERO_LAYOUTS: Array<{
  id: HeroLayout;
  name: string;
  description: string;
}> = [
  {
    id:
      "single",

    name:
      "صورة واحدة كبيرة",

    description:
      "تركيز كامل على مشهد رئيسي واحد",
  },

  {
    id:
      "wide-portrait",

    name:
      "كبيرة + جانبية",

    description:
      "صورة رئيسية مع صورة ثانية داعمة",
  },

  {
    id:
      "equal",

    name:
      "صورتان متساويتان",

    description:
      "تقسيم متوازن لمساحتين بصريتين",
  },

  {
    id:
      "focus-one",

    name:
      "تركيز على منتج",

    description:
      "النص والمنتج يأخذان الأولوية",
  },

  {
    id:
      "focus-two",

    name:
      "تركيز على منتجين",

    description:
      "عرض منتجين أو صورتين رئيسيتين",
  },
];

const PLAN_LABELS: Record<
  StorefrontConfig["planTier"],
  string
> = {
  free:
    "Free",

  business:
    "Business",

  pro:
    "Pro",

  elite:
    "Elite",
};

export function StoreBuilderPage() {
  const [
    config,
    setConfig,
  ] =
    useState<StorefrontConfig>(
      () =>
        loadStorefrontConfig(),
    );

  const [
    activeTab,
    setActiveTab,
  ] =
    useState<BuilderTab>(
      "content",
    );

  const [
    lockedFeature,
    setLockedFeature,
  ] =
    useState<
      string | null
    >(
      null,
    );

  const rights =
    getPlanEntitlements(
      config.planTier,
    );

  useEffect(() => {
    const timer =
      window.setTimeout(
        () => {
          saveStorefrontConfig(
            config,
          );
        },
        180,
      );

    return () =>
      window.clearTimeout(
        timer,
      );
  }, [
    config,
  ]);

  function saveNow() {
    saveStorefrontConfig(
      config,
    );
  }

  function updateHero(
    key:
      keyof StorefrontConfig["hero"],
    value:
      string,
  ) {
    setConfig(
      (current) => ({
        ...current,

        hero: {
          ...current.hero,

          [key]:
            value,
        },
      }),
    );
  }

  function updateProductSection(
    key:
      keyof StorefrontConfig["productSection"],
    value:
      string | boolean,
  ) {
    setConfig(
      (current) => ({
        ...current,

        productSection: {
          ...current.productSection,

          [key]:
            value,
        },
      }),
    );
  }

  function updateStory(
    key:
      keyof StorefrontConfig["story"],
    value:
      string | boolean,
  ) {
    setConfig(
      (current) => ({
        ...current,

        story: {
          ...current.story,

          [key]:
            value,
        },
      }),
    );
  }

  function moveSection(
    index: number,
    direction:
      | -1
      | 1,
  ) {
    if (
      !rights.canReorderSections
    ) {
      setLockedFeature(
        "إعادة ترتيب الأقسام",
      );

      return;
    }

    const target =
      index +
      direction;

    if (
      target < 0 ||
      target >=
        config.sectionOrder.length
    ) {
      return;
    }

    const next = [
      ...config.sectionOrder,
    ];

    [
      next[index],
      next[target],
    ] = [
      next[target],
      next[index],
    ];

    setConfig(
      (current) => ({
        ...current,

        sectionOrder:
          next,
      }),
    );
  }

  return (
    <>
      <div className="mx-auto max-w-[1600px]">
        <div className="mb-7 flex flex-wrap items-start justify-between gap-5">
          <div>
            <div className="mb-2 flex items-center gap-2">
              <p className="text-[11px] text-[var(--ink-muted)]">
                تصميم المتجر
              </p>

              <span className="rounded-full bg-[#e5eee9] px-2.5 py-1 text-[9px] font-semibold text-[#315a49]">
                {
                  PLAN_LABELS[
                    config.planTier
                  ]
                }
              </span>
            </div>

            <h1 className="text-[32px] font-semibold tracking-[-0.045em]">
              ابنِ واجهة متجرك
            </h1>

            <p className="mt-2 max-w-2xl text-[12px] leading-7 text-[var(--ink-soft)]">
              المحتوى ملكك بالكامل. الثيم والخطوط والوضعيات والأقسام المتاحة تعتمد على باقتك.
            </p>
          </div>

          <button
            type="button"
            onClick={
              saveNow
            }
            className="inline-flex h-11 items-center gap-2 rounded-[9px] bg-[#1d201e] px-5 text-[11px] font-semibold text-white"
          >
            <Save
              size={15}
            />

            حفظ الآن
          </button>
        </div>

        <div className="mb-6 grid gap-2 rounded-[12px] border border-black/[0.07] bg-[#fbfbf9] p-2 md:grid-cols-3">
          {tabs.map(
            (tab) => {
              const Icon =
                tab.icon;

              return (
                <button
                  key={
                    tab.id
                  }
                  type="button"
                  onClick={() =>
                    setActiveTab(
                      tab.id,
                    )
                  }
                  className={[
                    "flex items-center gap-3 rounded-[9px] px-4 py-3 text-right transition",
                    activeTab ===
                    tab.id
                      ? "bg-[#e9ece8] text-[#26352f]"
                      : "hover:bg-black/[0.025]",
                  ].join(" ")}
                >
                  <span className="flex size-9 shrink-0 items-center justify-center rounded-[8px] bg-white">
                    <Icon
                      size={16}
                      strokeWidth={1.7}
                    />
                  </span>

                  <span>
                    <span className="block text-[11px] font-semibold">
                      {
                        tab.label
                      }
                    </span>

                    <span className="mt-0.5 block text-[9px] text-[var(--ink-muted)]">
                      {
                        tab.description
                      }
                    </span>
                  </span>
                </button>
              );
            },
          )}
        </div>

        <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_540px]">
          <div className="min-w-0 space-y-5">
            {activeTab ===
            "content" ? (
              <ContentEditor
                config={
                  config
                }
                setConfig={
                  setConfig
                }
                updateHero={
                  updateHero
                }
                updateProductSection={
                  updateProductSection
                }
                updateStory={
                  updateStory
                }
              />
            ) : null}

            {activeTab ===
            "design" ? (
              <DesignEditor
                config={
                  config
                }
                setConfig={
                  setConfig
                }
                onLocked={
                  setLockedFeature
                }
              />
            ) : null}

            {activeTab ===
            "sections" ? (
              <SectionsEditor
                config={
                  config
                }
                setConfig={
                  setConfig
                }
                moveSection={
                  moveSection
                }
                updateProductSection={
                  updateProductSection
                }
                updateStory={
                  updateStory
                }
                onLocked={
                  setLockedFeature
                }
              />
            ) : null}
          </div>

          <div className="xl:sticky xl:top-[92px]">
            <StorePreviewFrame
              config={
                config
              }
            />
          </div>
        </div>
      </div>

      <UpgradeNoticeDialog
        open={
          lockedFeature !==
          null
        }
        featureName={
          lockedFeature ??
          ""
        }
        onClose={() =>
          setLockedFeature(
            null,
          )
        }
      />
    </>
  );
}

function ContentEditor({
  config,
  setConfig,
  updateHero,
  updateProductSection,
  updateStory,
}: {
  config:
    StorefrontConfig;

  setConfig:
    React.Dispatch<
      React.SetStateAction<StorefrontConfig>
    >;

  updateHero: (
    key:
      keyof StorefrontConfig["hero"],
    value: string,
  ) => void;

  updateProductSection: (
    key:
      keyof StorefrontConfig["productSection"],
    value:
      string | boolean,
  ) => void;

  updateStory: (
    key:
      keyof StorefrontConfig["story"],
    value:
      string | boolean,
  ) => void;
}) {
  return (
    <>
      <BuilderPanel
        title="هوية المتجر"
        description="هذه البيانات متاحة للتعديل في جميع الباقات."
      >
        <div className="grid gap-4 md:grid-cols-2">
          <BuilderField
            label="اسم المتجر"
            previewTarget="store.name"
            value={
              config.storeName
            }
            onChange={(value) =>
              setConfig(
                (current) => ({
                  ...current,

                  storeName:
                    value,
                }),
              )
            }
          />

          <BuilderField
            label="شريط الإعلان"
            previewTarget="announcement"
            value={
              config.announcement
            }
            onChange={(value) =>
              setConfig(
                (current) => ({
                  ...current,

                  announcement:
                    value,
                }),
              )
            }
          />
        </div>
      </BuilderPanel>

      <BuilderPanel
        title="واجهة البداية"
        description="النصوص والصور مستقلة عن الثيم."
      >
        <div className="grid gap-4 md:grid-cols-2">
          <BuilderField
            label="النص الصغير"
            previewTarget="hero.eyebrow"
            value={
              config.hero.eyebrow
            }
            onChange={(value) =>
              updateHero(
                "eyebrow",
                value,
              )
            }
          />

          <BuilderField
            label="العنوان الرئيسي"
            previewTarget="hero.title"
            value={
              config.hero.title
            }
            onChange={(value) =>
              updateHero(
                "title",
                value,
              )
            }
          />

          <div className="md:col-span-2">
            <BuilderTextarea
              label="الوصف"
              previewTarget="hero.description"
              value={
                config.hero.description
              }
              onChange={(value) =>
                updateHero(
                  "description",
                  value,
                )
              }
              rows={3}
            />
          </div>

          <BuilderField
            label="نص الزر"
            previewTarget="hero.cta"
            value={
              config.hero.ctaLabel
            }
            onChange={(value) =>
              updateHero(
                "ctaLabel",
                value,
              )
            }
          />

          <BuilderField
            label="رابط الزر"
            value={
              config.hero.ctaHref
            }
            onChange={(value) =>
              updateHero(
                "ctaHref",
                value,
              )
            }
          />

          <div className="md:col-span-2">
            <ImageUrlField
              label="الصورة الرئيسية"
              previewTarget="hero.primaryImage"
              value={
                config.hero.primaryImage
              }
              onChange={(value) =>
                updateHero(
                  "primaryImage",
                  value,
                )
              }
            />
          </div>

          <div className="md:col-span-2">
            <ImageUrlField
              label="الصورة الثانية"
              previewTarget="hero.secondaryImage"
              value={
                config.hero.secondaryImage
              }
              onChange={(value) =>
                updateHero(
                  "secondaryImage",
                  value,
                )
              }
            />
          </div>
        </div>
      </BuilderPanel>

      {config.categorySection.enabled ? (
        <BuilderPanel
          title="قسم التصنيفات"
          description="لاحقا ستأتي التصنيفات والصور تلقائيا من كتالوج المتجر."
        >
          <div className="grid gap-4 md:grid-cols-2">
            <BuilderField
              label="النص الصغير"
              previewTarget="categories.eyebrow"
              value={
                config.categorySection.eyebrow
              }
              onChange={(value) =>
                setConfig(
                  (current) => ({
                    ...current,

                    categorySection: {
                      ...current.categorySection,

                      eyebrow:
                        value,
                    },
                  }),
                )
              }
            />

            <BuilderField
              label="عنوان القسم"
              previewTarget="categories.title"
              value={
                config.categorySection.title
              }
              onChange={(value) =>
                setConfig(
                  (current) => ({
                    ...current,

                    categorySection: {
                      ...current.categorySection,

                      title:
                        value,
                    },
                  }),
                )
              }
            />
          </div>
        </BuilderPanel>
      ) : null}

      <BuilderPanel
        title="قسم المنتجات"
        description="عدل الكلام الذي يظهر فوق مجموعة المنتجات."
      >
        <div className="grid gap-4 md:grid-cols-2">
          <BuilderField
            label="النص الصغير"
            previewTarget="products.eyebrow"
            value={
              config.productSection.eyebrow
            }
            onChange={(value) =>
              updateProductSection(
                "eyebrow",
                value,
              )
            }
          />

          <BuilderField
            label="عنوان القسم"
            previewTarget="products.title"
            value={
              config.productSection.title
            }
            onChange={(value) =>
              updateProductSection(
                "title",
                value,
              )
            }
          />
        </div>
      </BuilderPanel>

      {config.bannerSection.enabled ? (
        <BuilderPanel
          title="البانر الترويجي"
          description="استخدمه لحملة أو مجموعة أو عرض مهم."
        >
          <div className="grid gap-4 md:grid-cols-2">
            <BuilderField
              label="النص الصغير"
              previewTarget="banner.eyebrow"
              value={
                config.bannerSection.eyebrow
              }
              onChange={(value) =>
                setConfig(
                  (current) => ({
                    ...current,

                    bannerSection: {
                      ...current.bannerSection,

                      eyebrow:
                        value,
                    },
                  }),
                )
              }
            />

            <BuilderField
              label="العنوان"
              previewTarget="banner.title"
              value={
                config.bannerSection.title
              }
              onChange={(value) =>
                setConfig(
                  (current) => ({
                    ...current,

                    bannerSection: {
                      ...current.bannerSection,

                      title:
                        value,
                    },
                  }),
                )
              }
            />

            <div className="md:col-span-2">
              <BuilderTextarea
                label="النص"
                previewTarget="banner.body"
                value={
                  config.bannerSection.body
                }
                onChange={(value) =>
                  setConfig(
                    (current) => ({
                      ...current,

                      bannerSection: {
                        ...current.bannerSection,

                        body:
                          value,
                      },
                    }),
                  )
                }
              />
            </div>

            <BuilderField
              label="نص الزر"
              previewTarget="banner.cta"
              value={
                config.bannerSection.ctaLabel
              }
              onChange={(value) =>
                setConfig(
                  (current) => ({
                    ...current,

                    bannerSection: {
                      ...current.bannerSection,

                      ctaLabel:
                        value,
                    },
                  }),
                )
              }
            />

            <BuilderField
              label="رابط الزر"
              value={
                config.bannerSection.ctaHref
              }
              onChange={(value) =>
                setConfig(
                  (current) => ({
                    ...current,

                    bannerSection: {
                      ...current.bannerSection,

                      ctaHref:
                        value,
                    },
                  }),
                )
              }
            />

            <div className="md:col-span-2">
              <ImageUrlField
                label="صورة البانر"
                previewTarget="banner.image"
                value={
                  config.bannerSection.image
                }
                onChange={(value) =>
                  setConfig(
                    (current) => ({
                      ...current,

                      bannerSection: {
                        ...current.bannerSection,

                        image:
                          value,
                      },
                    }),
                  )
                }
              />
            </div>
          </div>
        </BuilderPanel>
      ) : null}

      <BuilderPanel
        title="قسم القصة"
        description="قصة البراند أو المجموعة أو أي رسالة تريد إبرازها."
      >
        <div className="grid gap-4 md:grid-cols-2">
          <BuilderField
            label="النص الصغير"
            previewTarget="story.eyebrow"
            value={
              config.story.eyebrow
            }
            onChange={(value) =>
              updateStory(
                "eyebrow",
                value,
              )
            }
          />

          <BuilderField
            label="العنوان"
            previewTarget="story.title"
            value={
              config.story.title
            }
            onChange={(value) =>
              updateStory(
                "title",
                value,
              )
            }
          />

          <div className="md:col-span-2">
            <BuilderTextarea
              label="النص"
              previewTarget="story.body"
              value={
                config.story.body
              }
              onChange={(value) =>
                updateStory(
                  "body",
                  value,
                )
              }
            />
          </div>

          <BuilderField
            label="نص الزر"
            previewTarget="story.cta"
            value={
              config.story.ctaLabel
            }
            onChange={(value) =>
              updateStory(
                "ctaLabel",
                value,
              )
            }
          />

          <BuilderField
            label="رابط الزر"
            value={
              config.story.ctaHref
            }
            onChange={(value) =>
              updateStory(
                "ctaHref",
                value,
              )
            }
          />

          <div className="md:col-span-2">
            <ImageUrlField
              label="صورة القسم"
              previewTarget="story.image"
              value={
                config.story.image
              }
              onChange={(value) =>
                updateStory(
                  "image",
                  value,
                )
              }
            />
          </div>
        </div>
      </BuilderPanel>
    </>
  );
}

function DesignEditor({
  config,
  setConfig,
  onLocked,
}: {
  config:
    StorefrontConfig;

  setConfig:
    React.Dispatch<
      React.SetStateAction<StorefrontConfig>
    >;

  onLocked:
    (name: string) =>
      void;
}) {
  const rights =
    getPlanEntitlements(
      config.planTier,
    );

  return (
    <>
      <BuilderPanel
        title="الثيم"
        description="الثيم يغير شخصية المتجر وتكوين الصفحة."
        action={
          <LayoutTemplate
            size={17}
            className="text-black/35"
          />
        }
      >
        <div className="grid gap-3 md:grid-cols-2">
          {THEME_PRESETS.map(
            (theme) => {
              const allowed =
                rights.themes.includes(
                  theme.id,
                );

              return (
                <ChoiceCard
                  key={
                    theme.id
                  }
                  name={
                    theme.name
                  }
                  description={
                    theme.description
                  }
                  selected={
                    config.themeId ===
                    theme.id
                  }
                  allowed={
                    allowed
                  }
                  onSelect={() =>
                    setConfig(
                      (current) => ({
                        ...current,

                        themeId:
                          theme.id,
                      }),
                    )
                  }
                  onLocked={() =>
                    onLocked(
                      `ثيم ${theme.name}`,
                    )
                  }
                  preview={
                    <div
                      className="flex h-12 overflow-hidden rounded-[7px]"
                      style={{
                        background:
                          theme.canvas,
                      }}
                    >
                      <div
                        className="w-[56%]"
                        style={{
                          background:
                            theme.accent,
                        }}
                      />

                      <div
                        className="flex flex-1 items-center justify-center"
                        style={{
                          color:
                            theme.ink,
                        }}
                      >
                        <span className="h-1.5 w-8 rounded-full bg-current opacity-25" />
                      </div>
                    </div>
                  }
                />
              );
            },
          )}
        </div>
      </BuilderPanel>

      <BuilderPanel
        title="الخط"
        description="الخطوط المتاحة تتبع الباقة."
      >
        <div className="grid gap-3 sm:grid-cols-2">
          {STOREFRONT_FONTS.map(
            (font) => {
              const allowed =
                rights.fonts.includes(
                  font.id,
                );

              return (
                <ChoiceCard
                  key={
                    font.id
                  }
                  name={
                    font.name
                  }
                  description="أفق متجر عربي"
                  titleStyle={{
                    fontFamily:
                      font.family,
                  }}
                  selected={
                    config.fontId ===
                    font.id
                  }
                  allowed={
                    allowed
                  }
                  onSelect={() =>
                    setConfig(
                      (current) => ({
                        ...current,

                        fontId:
                          font.id,
                      }),
                    )
                  }
                  onLocked={() =>
                    onLocked(
                      `خط ${font.name}`,
                    )
                  }
                />
              );
            },
          )}
        </div>
      </BuilderPanel>

      <BuilderPanel
        title="بطاقات المنتجات"
        description="اختر كمية المعلومات وطريقة عرض المنتج."
      >
        <div className="grid gap-3 sm:grid-cols-2">
          {CARD_STYLES.map(
            (card) => {
              const allowed =
                rights.cardStyles.includes(
                  card.id,
                );

              return (
                <ChoiceCard
                  key={
                    card.id
                  }
                  name={
                    card.name
                  }
                  description={
                    card.description
                  }
                  selected={
                    config.productCardStyle ===
                    card.id
                  }
                  allowed={
                    allowed
                  }
                  onSelect={() =>
                    setConfig(
                      (current) => ({
                        ...current,

                        productCardStyle:
                          card.id,
                      }),
                    )
                  }
                  onLocked={() =>
                    onLocked(
                      `بطاقة ${card.name}`,
                    )
                  }
                />
              );
            },
          )}
        </div>
      </BuilderPanel>

      <BuilderPanel
        title="بداية الصفحة"
        description="حدد ما الذي يأخذ الأولوية عند دخول الزائر."
      >
        <div className="grid gap-3 sm:grid-cols-2">
          {HERO_LAYOUTS.map(
            (layout) => {
              const allowed =
                rights.heroLayouts.includes(
                  layout.id,
                );

              return (
                <ChoiceCard
                  key={
                    layout.id
                  }
                  name={
                    layout.name
                  }
                  description={
                    layout.description
                  }
                  selected={
                    config.heroLayout ===
                    layout.id
                  }
                  allowed={
                    allowed
                  }
                  onSelect={() =>
                    setConfig(
                      (current) => ({
                        ...current,

                        heroLayout:
                          layout.id,
                      }),
                    )
                  }
                  onLocked={() =>
                    onLocked(
                      layout.name,
                    )
                  }
                />
              );
            },
          )}
        </div>
      </BuilderPanel>

      <SectionLayoutControls
        config={
          config
        }
        setConfig={
          setConfig
        }
        onLocked={
          onLocked
        }
      />
    </>
  );
}

function SectionsEditor({
  config,
  setConfig,
  moveSection,
  updateProductSection,
  updateStory,
  onLocked,
}: {
  config:
    StorefrontConfig;

  setConfig:
    React.Dispatch<
      React.SetStateAction<StorefrontConfig>
    >;

  moveSection: (
    index: number,
    direction:
      | -1
      | 1,
  ) => void;

  updateProductSection: (
    key:
      keyof StorefrontConfig["productSection"],
    value:
      string | boolean,
  ) => void;

  updateStory: (
    key:
      keyof StorefrontConfig["story"],
    value:
      string | boolean,
  ) => void;

  onLocked:
    (name: string) =>
      void;
}) {
  const rights =
    getPlanEntitlements(
      config.planTier,
    );

  const activeExtraCount =
    Number(
      config.categorySection.enabled,
    ) +
    Number(
      config.bannerSection.enabled,
    );

  function toggleExtra(
    type:
      | "categories"
      | "banner",
  ) {
    if (
      !rights.extraSectionTypes.includes(
        type,
      )
    ) {
      onLocked(
        type ===
        "categories"
          ? "قسم التصنيفات"
          : "البانر الترويجي",
      );

      return;
    }

    const enabled =
      type ===
      "categories"
        ? config.categorySection.enabled
        : config.bannerSection.enabled;

    if (
      !enabled &&
      activeExtraCount >=
        rights.maxExtraSections
    ) {
      onLocked(
        "أقسام إضافية",
      );

      return;
    }

    setConfig(
      (current) => {
        if (
          type ===
          "categories"
        ) {
          return {
            ...current,

            categorySection: {
              ...current.categorySection,

              enabled:
                !current.categorySection.enabled,
            },
          };
        }

        return {
          ...current,

          bannerSection: {
            ...current.bannerSection,

            enabled:
              !current.bannerSection.enabled,
          },
        };
      },
    );
  }

  function getSectionMeta(
    section:
      StorefrontSectionKey,
  ) {
    switch (
      section
    ) {
      case "categories":
        return {
          title:
            "قسم التصنيفات",

          visible:
            config.categorySection.enabled,
        };

      case "products":
        return {
          title:
            "قسم المنتجات",

          visible:
            config.productSection.enabled,
        };

      case "banner":
        return {
          title:
            "البانر الترويجي",

          visible:
            config.bannerSection.enabled,
        };

      case "story":
        return {
          title:
            "قسم القصة",

          visible:
            config.story.enabled,
        };
    }
  }

  return (
    <>
      <BuilderPanel
        title="مكتبة الأقسام"
        description="أضف ما يحتاجه المتجر فقط. الأقسام المتاحة تعتمد على الباقة."
        action={
          <Plus
            size={17}
            className="text-black/35"
          />
        }
      >
        <div className="grid gap-3 sm:grid-cols-2">
          <SectionLibraryCard
            title="التصنيفات"
            description="اعرض أقسام المتجر بصريا ليسهل على الزائر الوصول لما يريد."
            enabled={
              config.categorySection.enabled
            }
            locked={
              !rights.extraSectionTypes.includes(
                "categories",
              )
            }
            onClick={() =>
              toggleExtra(
                "categories",
              )
            }
          />

          <SectionLibraryCard
            title="بانر ترويجي"
            description="مساحة مستقلة لحملة أو مجموعة أو رسالة مهمة."
            enabled={
              config.bannerSection.enabled
            }
            locked={
              !rights.extraSectionTypes.includes(
                "banner",
              )
            }
            onClick={() =>
              toggleExtra(
                "banner",
              )
            }
          />
        </div>
      </BuilderPanel>

      <BuilderPanel
        title="إظهار الأقسام الأساسية"
        description="إخفاء القسم لا يحذف محتواه."
      >
        <div className="space-y-3">
          <BuilderSwitch
            checked={
              config.productSection.enabled
            }
            onChange={(checked) =>
              updateProductSection(
                "enabled",
                checked,
              )
            }
            label="قسم المنتجات"
            description="مجموعة المنتجات الرئيسية."
          />

          <BuilderSwitch
            checked={
              config.story.enabled
            }
            onChange={(checked) =>
              updateStory(
                "enabled",
                checked,
              )
            }
            label="قسم القصة"
            description="مساحة للنص والصورة وقصة البراند."
          />
        </div>
      </BuilderPanel>

      <BuilderPanel
        title="ترتيب الصفحة"
        description="غيّر ترتيب الأقسام لتحديد رحلة الزائر."
      >
        <div className="space-y-2">
          {config.sectionOrder.map(
            (
              section,
              index,
            ) => {
              const meta =
                getSectionMeta(
                  section,
                );

              return (
                <div
                  key={
                    section
                  }
                  className={[
                    "flex items-center justify-between gap-4 rounded-[10px] border border-black/[0.08] bg-white px-4 py-3 transition",
                    meta.visible
                      ? ""
                      : "opacity-45",
                  ].join(" ")}
                >
                  <div className="flex items-center gap-3">
                    <span className="flex size-8 items-center justify-center rounded-full bg-[#f0f0ec] text-[10px] font-semibold text-[var(--ink-muted)]">
                      {index + 1}
                    </span>

                    <div>
                      <p className="text-[11px] font-semibold">
                        {meta.title}
                      </p>

                      <p className="mt-0.5 text-[9px] text-[var(--ink-muted)]">
                        {meta.visible
                          ? "ظاهر في المتجر"
                          : "مخفي حاليا"}
                      </p>
                    </div>
                  </div>

                  <div className="flex gap-1">
                    <button
                      type="button"
                      disabled={
                        index === 0
                      }
                      onClick={() =>
                        moveSection(
                          index,
                          -1,
                        )
                      }
                      className="flex size-9 items-center justify-center rounded-[7px] border border-black/[0.08] bg-white disabled:opacity-20"
                    >
                      <ArrowUp
                        size={14}
                      />
                    </button>

                    <button
                      type="button"
                      disabled={
                        index ===
                        config.sectionOrder.length -
                          1
                      }
                      onClick={() =>
                        moveSection(
                          index,
                          1,
                        )
                      }
                      className="flex size-9 items-center justify-center rounded-[7px] border border-black/[0.08] bg-white disabled:opacity-20"
                    >
                      <ArrowDown
                        size={14}
                      />
                    </button>
                  </div>
                </div>
              );
            },
          )}
        </div>
      </BuilderPanel>
    </>
  );
}

function SectionLibraryCard({
  title,
  description,
  enabled,
  locked,
  onClick,
}: {
  title: string;
  description: string;
  enabled: boolean;
  locked: boolean;
  onClick: () => void;
}) {
  return (
    <div className="rounded-[11px] border border-black/[0.08] bg-white p-4">
      <div className="flex min-h-[82px] items-start justify-between gap-4">
        <div>
          <p className="text-[12px] font-semibold">
            {title}
          </p>

          <p className="mt-1.5 max-w-xs text-[9px] leading-5 text-[var(--ink-muted)]">
            {description}
          </p>
        </div>

        {locked ? (
          <span className="rounded-full bg-[#efeee9] px-2 py-1 text-[9px] text-[var(--ink-muted)]">
            ترقية
          </span>
        ) : null}
      </div>

      <button
        type="button"
        onClick={
          onClick
        }
        className={[
          "mt-4 h-9 w-full rounded-[8px] border text-[10px] font-semibold transition",
          enabled
            ? "border-[#8e4b42]/20 bg-[#f8efed] text-[#8e4b42]"
            : "border-black/[0.08] bg-[#f5f5f1] hover:bg-[#ecece7]",
        ].join(" ")}
      >
        {enabled
          ? "إزالة من الصفحة"
          : locked
            ? "متاح مع الترقية"
            : "إضافة إلى الصفحة"}
      </button>
    </div>
  );
}