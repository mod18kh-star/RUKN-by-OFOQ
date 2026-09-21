export type VerticalFieldType =
  | "text"
  | "textarea"
  | "number"
  | "select";

export type VerticalFieldGroup =
  | "identity"
  | "details"
  | "material"
  | "design"
  | "care"
  | "performance"
  | "display"
  | "power"
  | "connectivity"
  | "compatibility"
  | "warranty";

export type VerticalFieldOption = {
  value: string;
  label: string;
};

export type VerticalFieldVisibilityRule = {
  key: string;
  values: readonly string[];
};

export type VerticalFieldDefinition = {
  key: string;
  label: string;
  type: VerticalFieldType;

  group: VerticalFieldGroup;

  placeholder?: string;
  helpText?: string;

  required?: boolean;

  options?: readonly VerticalFieldOption[];

  visibleWhen?: VerticalFieldVisibilityRule;
};

export type VariantDimensionDefinition = {
  key: string;
  label: string;

  placeholder?: string;
  helpText?: string;
};

export type ProductVerticalDefinition = {
  code: string;

  label: string;
  badgeLabel: string;

  description?: string;

  fields: readonly VerticalFieldDefinition[];

  variantDimensions: readonly VariantDimensionDefinition[];
};