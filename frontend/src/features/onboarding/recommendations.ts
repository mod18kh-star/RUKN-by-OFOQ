import type {
  OfoqPlan,
  OnboardingAnswers,
} from "./onboardingStorage";

interface VerticalRecommendation {
  type: string;
  code: string;
  label: string;
}

export interface OnboardingRecommendation {
  primary: VerticalRecommendation;
  alternatives:
    VerticalRecommendation[];
  plan: OfoqPlan;
  reasons: string[];
  services: string[];
}

const verticalMap: Record<
  string,
  VerticalRecommendation
> = {
  apparel: {
    type: "Apparel",
    code: "apparel",
    label: "أزياء وملابس",
  },

  footwear: {
    type: "Footwear",
    code: "footwear",
    label: "أحذية",
  },

  mobile: {
    type: "MobilePhones",
    code: "mobile-phones",
    label: "هواتف وجوالات",
  },

  electronics: {
    type: "Electronics",
    code: "electronics",
    label: "إلكترونيات",
  },

  beauty: {
    type: "Cosmetics",
    code: "cosmetics",
    label: "تجميل وعناية",
  },

  perfumes: {
    type: "Perfumes",
    code: "perfumes",
    label: "عطور",
  },

  restaurants: {
    type: "Restaurants",
    code: "restaurants",
    label: "مطاعم",
  },

  grocery: {
    type: "Grocery",
    code: "grocery",
    label: "بقالة ومواد غذائية",
  },

  furniture: {
    type: "FurnitureAndDecor",
    code: "furniture-decor",
    label: "أثاث وديكور",
  },

  home: {
    type: "HomeGoods",
    code: "home-goods",
    label: "منتجات منزلية",
  },

  automotive: {
    type: "AutomotiveParts",
    code: "automotive-parts",
    label: "قطع سيارات",
  },

  gifts: {
    type: "PersonalizedGifts",
    code: "personalized-gifts",
    label: "هدايا مخصصة",
  },

  jewelry: {
    type: "JewelryAndWatches",
    code: "jewelry-watches",
    label: "مجوهرات وساعات",
  },

  realestate: {
    type: "RealEstate",
    code: "real-estate",
    label: "عقارات",
  },

  digital: {
    type: "DigitalProducts",
    code: "digital-products",
    label: "منتجات رقمية",
  },

  services: {
    type: "Services",
    code: "services",
    label: "خدمات",
  },

  subscriptions: {
    type: "Subscriptions",
    code: "subscriptions",
    label: "اشتراكات",
  },

  rental: {
    type: "EquipmentRental",
    code: "equipment-rental",
    label: "تأجير",
  },

  events: {
    type: "EventsAndTickets",
    code: "events-tickets",
    label: "فعاليات وتذاكر",
  },

  wholesale: {
    type: "WholesaleB2B",
    code: "wholesale-b2b",
    label: "بيع بالجملة",
  },
};

const generalRetail:
  VerticalRecommendation = {
    type: "GeneralRetail",
    code: "general-retail",
    label: "متجر عام",
  };

function has(
  answers: OnboardingAnswers,
  id: string,
  value: string,
) {
  return (
    answers[id]?.includes(
      value,
    ) ?? false
  );
}

function selectedCount(
  answers: OnboardingAnswers,
  id: string,
) {
  return (
    answers[id]?.length ?? 0
  );
}

export function buildRecommendation(
  answers: OnboardingAnswers,
): OnboardingRecommendation {
  const activityValues =
    answers.activityCategory ??
    [];

  const verticals =
    activityValues
      .map(
        (value) =>
          verticalMap[value],
      )
      .filter(
        (
          value,
        ): value is VerticalRecommendation =>
          Boolean(value),
      );

  if (
    verticals.length === 0
  ) {
    if (
      has(
        answers,
        "offerType",
        "services",
      )
    ) {
      verticals.push(
        verticalMap.services,
      );
    } else if (
      has(
        answers,
        "offerType",
        "subscriptions",
      )
    ) {
      verticals.push(
        verticalMap.subscriptions,
      );
    } else if (
      has(
        answers,
        "offerType",
        "digital",
      )
    ) {
      verticals.push(
        verticalMap.digital,
      );
    } else {
      verticals.push(
        generalRetail,
      );
    }
  }

  let plan: OfoqPlan =
    "business";

  const team =
    answers.teamSize?.[0];

  const volume =
    answers.salesVolume?.[0];

  const channels =
    selectedCount(
      answers,
      "salesChannels",
    );

  if (
    team === "21-plus" ||
    volume === "5000-plus"
  ) {
    plan =
      "extra";
  } else if (
    team === "2-5" ||
    team === "6-20" ||
    volume === "50-500" ||
    volume === "500-5000" ||
    has(
      answers,
      "customerType",
      "b2b",
    ) ||
    channels >= 3
  ) {
    plan =
      "pro";
  }

  const reasons: string[] =
    [];

  if (
    has(
      answers,
      "projectStage",
      "existing",
    )
  ) {
    reasons.push(
      "عندك نشاط قائم وتحتاج بداية مرتبة بدون إعادة بناء شغلك من الصفر",
    );
  }

  if (
    has(
      answers,
      "deliveryNeeds",
      "delivery",
    )
  ) {
    reasons.push(
      "منتجاتك تحتاج توصيل ومتابعة طلبات",
    );
  }

  if (
    has(
      answers,
      "deliveryNeeds",
      "inventory",
    )
  ) {
    reasons.push(
      "عندك مخزون يحتاج إدارة واضحة",
    );
  }

  if (
    team &&
    team !== "solo"
  ) {
    reasons.push(
      "في أكثر من شخص رح يشتغل على المتجر",
    );
  }

  if (
    has(
      answers,
      "customerType",
      "b2b",
    )
  ) {
    reasons.push(
      "جزء من بيعك موجه للشركات",
    );
  }

  if (
    reasons.length === 0
  ) {
    reasons.push(
      "بدايتك مناسبة لإعداد بسيط تقدر توسّعه لاحقًا",
    );
  }

  const services: string[] =
    [];

  if (
    has(
      answers,
      "deliveryNeeds",
      "delivery",
    )
  ) {
    services.push(
      "إعداد الشحن والتوصيل",
    );
  }

  if (
    has(
      answers,
      "deliveryNeeds",
      "inventory",
    )
  ) {
    services.push(
      "إدارة المخزون والتنبيهات",
    );
  }

  if (
    has(
      answers,
      "licenseStatus",
      "none",
    ) ||
    has(
      answers,
      "licenseStatus",
      "in-progress",
    )
  ) {
    services.push(
      "تجهيز مسار التحقق التجاري عندما تصبح مستنداتك جاهزة",
    );
  }

  if (
    has(
      answers,
      "bankingStatus",
      "personal",
    ) ||
    has(
      answers,
      "bankingStatus",
      "none",
    ) ||
    has(
      answers,
      "bankingStatus",
      "opening",
    )
  ) {
    services.push(
      "تجهيز المدفوعات الإلكترونية عند جاهزية حسابك التجاري",
    );
  }

  if (
    team &&
    team !== "solo"
  ) {
    services.push(
      "صلاحيات مناسبة لأفراد الفريق",
    );
  }

  if (
    has(
      answers,
      "customerType",
      "b2b",
    )
  ) {
    services.push(
      "إعدادات مناسبة للبيع للشركات",
    );
  }

  return {
    primary:
      verticals[0] ??
      generalRetail,

    alternatives:
      verticals.slice(
        1,
        3,
      ),

    plan,
    reasons:
      reasons.slice(0, 3),

    services:
      services.slice(0, 4),
  };
}