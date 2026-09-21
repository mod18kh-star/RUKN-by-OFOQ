import "./StorefrontCartLink.css";
import {
  useEffect,
  useState,
  type ReactNode,
} from "react";

import { Link } from "react-router";

import { getAccessToken } from "../../features/auth/authSession";

import { getCustomerCart } from "../data/customerCartApi";

interface Props {
  storeSlug: string;
  className?: string;
  children: ReactNode;
  "aria-label"?: string;
}

export function StorefrontCartLink({
  storeSlug,
  className = "",
  children,
  "aria-label": ariaLabel = "السلة",
}: Props) {
  const [count, setCount] = useState(0);

  useEffect(() => {
    let active = true;
    let revision = 0;

    async function refresh() {
      const currentRevision = ++revision;

      if (!getAccessToken()) {
        if (active) setCount(0);
        return;
      }

      try {
        const cart = await getCustomerCart(storeSlug);

        if (active && currentRevision === revision) {
          setCount(cart?.totalQuantity ?? 0);
        }
      } catch {
        if (active && currentRevision === revision) {
          setCount(0);
        }
      }
    }

    function onCartUpdated(event: Event) {
      const detail = (event as CustomEvent<{
        storeSlug?: string;
      }>).detail;

      if (detail?.storeSlug === storeSlug) {
        void refresh();
      }
    }

    function onWindowFocus() {
      void refresh();
    }

    void refresh();

    window.addEventListener(
      "rukn:cart-updated",
      onCartUpdated,
    );

    window.addEventListener(
      "focus",
      onWindowFocus,
    );

    return () => {
      active = false;
      revision++;

      window.removeEventListener(
        "rukn:cart-updated",
        onCartUpdated,
      );

      window.removeEventListener(
        "focus",
        onWindowFocus,
      );
    };
  }, [storeSlug]);

  const cartHref =
    `/store/${encodeURIComponent(storeSlug)}/cart`;

  return (
    <Link
      to={cartHref}
      aria-label={`${ariaLabel}${count > 0 ? `، ${count} منتجات` : ""}`}
      className={`${className} rukn-cart-header-link relative`}
    >
      {children}

      {count > 0 && (
        <span
          className="absolute -left-1 -top-1 flex min-h-5 min-w-5 items-center justify-center rounded-full bg-[#193c30] px-1 text-[10px] font-bold leading-none"
          style={{
            color: "#ffffff",
            backgroundColor: "#193c30",
          }}
        >
          {count > 99 ? "99+" : count}
        </span>
      )}
    </Link>
  );
}