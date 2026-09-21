import { apparelVertical } from "./apparel";
import { electronicsVertical } from "./electronics";
import { generalRetailVertical } from "./general-retail";
import { mobilePhonesVertical } from "./mobile-phones";
import { specializedVerticals } from "./specialized";
import type { ProductVerticalDefinition } from "./types";

const specializedVerticalRegistry = Object.fromEntries(
  specializedVerticals.map((definition) => [
    definition.code,
    definition,
  ]),
) as Record<string, ProductVerticalDefinition>;

const verticalRegistry: Record<string, ProductVerticalDefinition> = {
  "general-retail": generalRetailVertical,
  apparel: apparelVertical,
  "mobile-phones": mobilePhonesVertical,
  electronics: electronicsVertical,
  ...specializedVerticalRegistry,
};

export function normalizeVerticalCode(
  value?: string | null,
) {
  return (
    value
      ?.trim()
      .toLowerCase()
      .replaceAll("_", "-") ?? ""
  );
}

export function getProductVerticalDefinition(
  verticalCode?: string | null,
): ProductVerticalDefinition | null {
  const normalized =
    normalizeVerticalCode(verticalCode);

  return (
    verticalRegistry[normalized]
    ?? null
  );
}

export function hasProductVerticalDefinition(
  verticalCode?: string | null,
) {
  return (
    getProductVerticalDefinition(
      verticalCode,
    ) !== null
  );
}

export {
  verticalRegistry,
};