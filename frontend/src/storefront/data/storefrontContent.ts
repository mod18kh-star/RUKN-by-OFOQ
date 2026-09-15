import {
  createContext,
  useContext,
} from "react";

import type {
  StorefrontCategory,
} from "./storefrontApi";

export interface RuntimeProduct {
  productId:
    string;

  slug:
    string;

  name:
    string;

  category:
    string;

  price:
    string;

  compareAtPrice?:
    string;

  image:
    string;

  primaryImage:
    string;

  secondaryImage?:
    string;

  badge?:
    string;
}

export interface StorefrontContentValue {
  products:
    RuntimeProduct[];

  categories:
    StorefrontCategory[];

  usingDemoData:
    boolean;

  isLoading:
    boolean;

  hasError:
    boolean;
}

export const StorefrontContentContext =
  createContext<
    StorefrontContentValue |
    null
  >(
    null,
  );

export function useStorefrontContent() {
  const value =
    useContext(
      StorefrontContentContext,
    );

  if (!value) {
    throw new Error(
      "useStorefrontContent must be used inside StorefrontContentProvider.",
    );
  }

  return value;
}