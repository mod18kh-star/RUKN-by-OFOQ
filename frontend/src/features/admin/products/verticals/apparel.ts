import type {
  ProductVerticalDefinition,
} from "./types";

export const apparelVertical = {
  code: "apparel",

  label: "الملابس",

  badgeLabel: "ملابس",

  description:
    "مواصفات خاصة بمنتجات الملابس مع فصل اللون والمقاس كخيارات منتج مستقلة.",

  fields: [
    {
      key: "brand",
      label: "العلامة التجارية",
      type: "text",
      group: "identity",
      placeholder: "مثال: Zara / Nike / Adidas",
    },

    {
      key: "garment-type",
      label: "نوع القطعة",
      type: "text",
      group: "identity",
      placeholder:
        "تيشيرت، قميص، بنطال، جاكيت، فستان...",
    },

    {
      key: "gender",
      label: "الفئة",
      type: "select",
      group: "identity",

      options: [
        {
          value: "men",
          label: "رجالي",
        },

        {
          value: "women",
          label: "نسائي",
        },

        {
          value: "kids",
          label: "أطفال",
        },

        {
          value: "unisex",
          label: "للجنسين",
        },
      ],
    },

    {
      key: "material",
      label: "الخامة / التركيبة",
      type: "text",
      group: "material",

      placeholder:
        "مثال: 95% قطن، 5% إيلاستين",
    },

    {
      key: "fabric",
      label: "نوع القماش",
      type: "text",
      group: "material",

      placeholder:
        "قطن، دنيم، كتان، صوف، بوليستر...",
    },

    {
      key: "fit",
      label: "القصة",
      type: "select",
      group: "design",

      options: [
        {
          value: "slim",
          label: "Slim Fit",
        },

        {
          value: "regular",
          label: "Regular Fit",
        },

        {
          value: "relaxed",
          label: "Relaxed Fit",
        },

        {
          value: "oversized",
          label: "Oversized",
        },
      ],
    },

    {
      key: "style",
      label: "النمط",
      type: "text",
      group: "design",

      placeholder:
        "كاجوال، رسمي، رياضي...",
    },

    {
      key: "season",
      label: "الموسم",
      type: "select",
      group: "details",

      options: [
        {
          value: "summer",
          label: "صيفي",
        },

        {
          value: "winter",
          label: "شتوي",
        },

        {
          value: "spring",
          label: "ربيعي",
        },

        {
          value: "autumn",
          label: "خريفي",
        },

        {
          value: "all-seasons",
          label: "جميع المواسم",
        },
      ],
    },

    {
      key: "sleeve-type",
      label: "نوع الأكمام",
      type: "text",
      group: "design",

      placeholder:
        "قصير، طويل، بدون أكمام...",
    },

    {
      key: "neckline",
      label: "نوع الياقة",
      type: "text",
      group: "design",

      placeholder:
        "دائرية، V، بولو، كلاسيكية...",
    },

    {
      key: "length",
      label: "الطول",
      type: "text",
      group: "design",

      placeholder:
        "قصير، عادي، طويل...",
    },

    {
      key: "pattern",
      label: "النقشة / التصميم",
      type: "text",
      group: "design",

      placeholder:
        "سادة، مخطط، مطبوع...",
    },

    {
      key: "size-system",
      label: "نظام المقاسات",
      type: "select",
      group: "details",

      options: [
        {
          value: "letter",
          label: "XS / S / M / L / XL",
        },

        {
          value: "eu",
          label: "EU",
        },

        {
          value: "us",
          label: "US",
        },

        {
          value: "uk",
          label: "UK",
        },

        {
          value: "custom",
          label: "مقاسات خاصة",
        },
      ],
    },

    {
      key: "country-of-origin",
      label: "بلد الصنع",
      type: "text",
      group: "details",

      placeholder:
        "مثال: تركيا",
    },

    {
      key: "care-instructions",
      label: "تعليمات الغسيل والعناية",
      type: "textarea",
      group: "care",

      placeholder:
        "مثال: غسيل بدرجة 30°، لا يستخدم المبيض، كي بحرارة منخفضة...",
    },
  ],

  variantDimensions: [
    {
      key: "color",
      label: "اللون",

      placeholder:
        "أسود، أبيض، بيج...",

      helpText:
        "كل لون يمكن أن يملك مخزوناً وSKU مختلفاً.",
    },

    {
      key: "size",
      label: "المقاس",

      placeholder:
        "S، M، L، XL أو المقاس الرقمي",

      helpText:
        "كل مقاس يمكن أن يملك مخزوناً وSKU مختلفاً.",
    },
  ],
} as const satisfies ProductVerticalDefinition;
