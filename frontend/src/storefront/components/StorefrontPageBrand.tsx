import {
  useEffect,
  useState,
} from "react";

import {
  getStorefrontInfo,
} from "../data/storefrontApi";

interface StorefrontPageBrandProps {
  storeSlug: string;
  className?: string;
}

interface ResolvedStoreName {
  slug: string;
  name: string;
}

export function StorefrontPageBrand({
  storeSlug,
  className = "",
}: StorefrontPageBrandProps) {
  const normalizedSlug =
    storeSlug.trim();

  const [resolved, setResolved] =
    useState<ResolvedStoreName | null>(null);

  useEffect(() => {
    let active = true;

    if (!normalizedSlug) {
      return () => {
        active = false;
      };
    }

    void getStorefrontInfo(
      normalizedSlug,
    )
      .then((store) => {
        const name =
          store.name?.trim();

        if (
          active &&
          name
        ) {
          setResolved({
            slug: normalizedSlug,
            name,
          });
        }
      })
      .catch(() => {
        // Keep the route slug as a safe fallback.
      });

    return () => {
      active = false;
    };
  }, [normalizedSlug]);

  const storeName =
    resolved?.slug === normalizedSlug
      ? resolved.name
      : normalizedSlug || "المتجر";

  return (
    <span className={className}>
      {storeName}
    </span>
  );
}