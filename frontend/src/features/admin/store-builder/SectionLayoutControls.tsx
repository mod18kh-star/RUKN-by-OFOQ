import type {
  Dispatch,
  SetStateAction,
} from "react";

import {
  LayoutGrid,
  Rows3,
} from "lucide-react";

import {
  getPlanEntitlements,
} from "../../../storefront/theme/planEntitlements";

import type {
  ProductSectionLayout,
  StorefrontConfig,
  StorySectionLayout,
} from "../../../storefront/theme/theme.types";

import {
  BuilderPanel,
  ChoiceCard,
} from "./BuilderControls";

const PRODUCT_LAYOUTS: Array<{
  id: ProductSectionLayout;
  name: string;
  description: string;
}> = [
  {
    id: "theme-default",
    name: "تلقائي حسب الثيم",
    description:
      "الثيم يختار التوزيع الأنسب لشخصيته.",
  },

  {
    id: "grid-3",
    name: "3 منتجات بالصف",
    description:
      "صور أكبر ومساحة أوسع لكل منتج.",
  },

  {
    id: "featured-grid",
    name: "منتج بارز + شبكة",
    description:
      "منتج يأخذ أولوية بصرية مع بقية المنتجات.",
  },

  {
    id: "horizontal",
    name: "عرض أفقي",
    description:
      "مناسب للأقسام الكبيرة والتصفح الأفقي.",
  },

  {
    id: "spotlight",
    name: "تركيز رئيسي",
    description:
      "منتج رئيسي كبير مع منتجات مساندة.",
  },
];

const STORY_LAYOUTS: Array<{
  id: StorySectionLayout;
  name: string;
  description: string;
}> = [
  {
    id: "theme-default",
    name: "تلقائي حسب الثيم",
    description:
      "يحافظ على شخصية الثيم الأصلية.",
  },

  {
    id: "split-start",
    name: "الصورة أولا",
    description:
      "الصورة بجانب النص وتأخذ الأولوية.",
  },

  {
    id: "split-end",
    name: "النص أولا",
    description:
      "النص يبدأ القسم ثم تأتي الصورة.",
  },

  {
    id: "centered",
    name: "مركزي هادئ",
    description:
      "النص في المنتصف مع صورة واسعة.",
  },

  {
    id: "full-bleed",
    name: "صورة كاملة",
    description:
      "الصورة تغطي القسم والنص يظهر فوقها.",
  },
];

export function SectionLayoutControls({
  config,
  setConfig,
  onLocked,
}: {
  config: StorefrontConfig;

  setConfig:
    Dispatch<
      SetStateAction<StorefrontConfig>
    >;

  onLocked:
    (featureName: string) => void;
}) {
  const rights =
    getPlanEntitlements(
      config.planTier,
    );

  return (
    <>
      <BuilderPanel
        title="توزيع قسم المنتجات"
        description="طريقة عرض المنتجات مستقلة عن الثيم ويمكن تغييرها حسب الباقة."
        action={
          <LayoutGrid
            size={17}
            className="text-black/35"
          />
        }
      >
        <div className="grid gap-3 sm:grid-cols-2">
          {PRODUCT_LAYOUTS.map(
            (layout) => {
              const allowed =
                rights.productSectionLayouts.includes(
                  layout.id,
                );

              return (
                <ChoiceCard
                  key={layout.id}
                  name={layout.name}
                  description={
                    layout.description
                  }
                  selected={
                    config.productSection.layout ===
                    layout.id
                  }
                  allowed={allowed}
                  onSelect={() =>
                    setConfig(
                      (current) => ({
                        ...current,

                        productSection: {
                          ...current.productSection,

                          layout:
                            layout.id,
                        },
                      }),
                    )
                  }
                  onLocked={() =>
                    onLocked(
                      `توزيع المنتجات: ${layout.name}`,
                    )
                  }
                  preview={
                    <ProductLayoutPreview
                      layout={
                        layout.id
                      }
                    />
                  }
                />
              );
            },
          )}
        </div>
      </BuilderPanel>

      <BuilderPanel
        title="توزيع قسم القصة"
        description="تحكم بطريقة توزيع النص والصورة داخل قسم القصة."
        action={
          <Rows3
            size={17}
            className="text-black/35"
          />
        }
      >
        <div className="grid gap-3 sm:grid-cols-2">
          {STORY_LAYOUTS.map(
            (layout) => {
              const allowed =
                rights.storySectionLayouts.includes(
                  layout.id,
                );

              return (
                <ChoiceCard
                  key={layout.id}
                  name={layout.name}
                  description={
                    layout.description
                  }
                  selected={
                    config.story.layout ===
                    layout.id
                  }
                  allowed={allowed}
                  onSelect={() =>
                    setConfig(
                      (current) => ({
                        ...current,

                        story: {
                          ...current.story,

                          layout:
                            layout.id,
                        },
                      }),
                    )
                  }
                  onLocked={() =>
                    onLocked(
                      `توزيع القصة: ${layout.name}`,
                    )
                  }
                  preview={
                    <StoryLayoutPreview
                      layout={
                        layout.id
                      }
                    />
                  }
                />
              );
            },
          )}
        </div>
      </BuilderPanel>
    </>
  );
}

function ProductLayoutPreview({
  layout,
}: {
  layout: ProductSectionLayout;
}) {
  if (
    layout === "featured-grid"
  ) {
    return (
      <div className="grid h-12 grid-cols-4 gap-1">
        <span className="col-span-2 row-span-2 rounded-[3px] bg-black/60" />

        <span className="rounded-[3px] bg-black/20" />
        <span className="rounded-[3px] bg-black/20" />

        <span className="rounded-[3px] bg-black/20" />
        <span className="rounded-[3px] bg-black/20" />
      </div>
    );
  }

  if (
    layout === "horizontal"
  ) {
    return (
      <div className="flex h-12 gap-1 overflow-hidden">
        <span className="w-[31%] shrink-0 rounded-[3px] bg-black/40" />
        <span className="w-[31%] shrink-0 rounded-[3px] bg-black/25" />
        <span className="w-[31%] shrink-0 rounded-[3px] bg-black/20" />
        <span className="w-[31%] shrink-0 rounded-[3px] bg-black/15" />
      </div>
    );
  }

  if (
    layout === "spotlight"
  ) {
    return (
      <div className="grid h-12 grid-cols-[1.4fr_.6fr] gap-1">
        <span className="rounded-[3px] bg-black/55" />

        <span className="grid gap-1">
          <span className="rounded-[3px] bg-black/25" />
          <span className="rounded-[3px] bg-black/18" />
        </span>
      </div>
    );
  }

  if (
    layout === "grid-3"
  ) {
    return (
      <div className="grid h-12 grid-cols-3 gap-1">
        <span className="rounded-[3px] bg-black/35" />
        <span className="rounded-[3px] bg-black/25" />
        <span className="rounded-[3px] bg-black/20" />
      </div>
    );
  }

  return (
    <div className="grid h-12 grid-cols-4 gap-1">
      <span className="rounded-[3px] bg-black/40" />
      <span className="rounded-[3px] bg-black/30" />
      <span className="rounded-[3px] bg-black/25" />
      <span className="rounded-[3px] bg-black/20" />
    </div>
  );
}

function StoryLayoutPreview({
  layout,
}: {
  layout: StorySectionLayout;
}) {
  if (
    layout === "centered"
  ) {
    return (
      <div className="flex h-12 flex-col items-center justify-center gap-1 rounded-[3px] bg-black/[0.05]">
        <span className="h-1.5 w-12 rounded-full bg-black/45" />
        <span className="h-1 w-20 rounded-full bg-black/20" />
        <span className="mt-1 h-4 w-[70%] rounded-[2px] bg-black/12" />
      </div>
    );
  }

  if (
    layout === "full-bleed"
  ) {
    return (
      <div className="relative h-12 overflow-hidden rounded-[3px] bg-black/55">
        <span className="absolute bottom-2 right-2 h-1.5 w-14 rounded-full bg-white/80" />
      </div>
    );
  }

  const imageFirst =
    layout === "split-start";

  return (
    <div className="grid h-12 grid-cols-2 gap-1">
      <span
        className={[
          "rounded-[3px] bg-black/35",
          imageFirst
            ? "order-1"
            : "order-2",
        ].join(" ")}
      />

      <span
        className={[
          "flex flex-col justify-center gap-1 rounded-[3px] bg-black/[0.04] px-2",
          imageFirst
            ? "order-2"
            : "order-1",
        ].join(" ")}
      >
        <span className="h-1.5 w-10 rounded-full bg-black/40" />
        <span className="h-1 w-14 rounded-full bg-black/15" />
      </span>
    </div>
  );
}