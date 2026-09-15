import type {
  ThemeId,
} from "./theme.types";

export type HeaderStyle =
  | "editorial"
  | "centered"
  | "catalog"
  | "studio"
  | "technical";

export type SectionRhythm =
  | "airy"
  | "luxury"
  | "dense"
  | "creative"
  | "structured";

export interface ThemePreset {
  id: ThemeId;
  name: string;
  description: string;

  canvas: string;
  surface: string;
  soft: string;

  ink: string;
  inkSoft: string;
  muted: string;

  accent: string;

  radius: string;
  contentWidth: string;
  imageRatio: string;

  headerStyle: HeaderStyle;
  rhythm: SectionRhythm;
}

export const THEME_PRESETS: ThemePreset[] = [
  {
    id: "editorial",
    name: "Editorial",
    description:
      "صور قوية ومساحات هادئة للأزياء والبراندات الحديثة.",

    canvas: "#f6f5f1",
    surface: "#ffffff",
    soft: "#ebe8df",

    ink: "#191916",
    inkSoft: "#5d5c56",
    muted: "#89877f",

    accent: "#27463d",

    radius: "2px",
    contentWidth: "1440px",
    imageRatio: "4 / 5",

    headerStyle: "editorial",
    rhythm: "airy",
  },

  {
    id: "maison",
    name: "Maison",
    description:
      "فخامة هادئة للعطور والمجوهرات والساعات.",

    canvas: "#f4f0e8",
    surface: "#fbf8f2",
    soft: "#e6ded1",

    ink: "#211d18",
    inkSoft: "#655e55",
    muted: "#91877b",

    accent: "#6b4e38",

    radius: "0px",
    contentWidth: "1360px",
    imageRatio: "3 / 4",

    headerStyle: "centered",
    rhythm: "luxury",
  },

  {
    id: "commerce",
    name: "Commerce",
    description:
      "واجهة مباشرة وسريعة للمتاجر متعددة المنتجات.",

    canvas: "#f7f7f5",
    surface: "#ffffff",
    soft: "#ecefea",

    ink: "#171817",
    inkSoft: "#565b58",
    muted: "#818783",

    accent: "#185744",

    radius: "12px",
    contentWidth: "1500px",
    imageRatio: "1 / 1",

    headerStyle: "catalog",
    rhythm: "dense",
  },

  {
    id: "studio",
    name: "Studio",
    description:
      "شخصية مرنة للديكور والهدايا والمنتجات الإبداعية.",

    canvas: "#f2f2ee",
    surface: "#fcfcfa",
    soft: "#e5e7e1",

    ink: "#171917",
    inkSoft: "#555b56",
    muted: "#838b84",

    accent: "#44594b",

    radius: "22px",
    contentWidth: "1460px",
    imageRatio: "4 / 5",

    headerStyle: "studio",
    rhythm: "creative",
  },

  {
    id: "technical",
    name: "Technical",
    description:
      "واجهة منظمة للإلكترونيات والمنتجات ذات المواصفات.",

    canvas: "#f3f6f6",
    surface: "#ffffff",
    soft: "#e8eeee",

    ink: "#162021",
    inkSoft: "#536164",
    muted: "#819094",

    accent: "#185563",

    radius: "8px",
    contentWidth: "1500px",
    imageRatio: "1 / 1",

    headerStyle: "technical",
    rhythm: "structured",
  },
];

export function getThemePreset(
  id: ThemeId,
) {
  return (
    THEME_PRESETS.find(
      (theme) =>
        theme.id === id,
    ) ??
    THEME_PRESETS[0]
  );
}