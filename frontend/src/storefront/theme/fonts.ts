import type {
  FontId,
} from "./theme.types";

export const STOREFRONT_FONTS: Array<{
  id: FontId;
  name: string;
  family: string;
}> = [
  {
    id: "plex",
    name: "Plex Arabic",
    family:
      '"IBM Plex Sans Arabic", "Segoe UI", Arial, sans-serif',
  },

  {
    id: "tajawal",
    name: "Tajawal",
    family:
      '"Tajawal", "Segoe UI", Arial, sans-serif',
  },

  {
    id: "cairo",
    name: "Cairo",
    family:
      '"Cairo", "Segoe UI", Arial, sans-serif',
  },

  {
    id: "readex",
    name: "Readex",
    family:
      '"Readex Pro", "Segoe UI", Arial, sans-serif',
  },
];

export function getFont(
  id: FontId,
) {
  return (
    STOREFRONT_FONTS.find(
      (font) =>
        font.id === id,
    ) ??
    STOREFRONT_FONTS[0]
  );
}