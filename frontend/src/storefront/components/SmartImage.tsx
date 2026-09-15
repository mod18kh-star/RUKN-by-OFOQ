import {
  useState,
  type ImgHTMLAttributes,
  type SyntheticEvent,
} from "react";

import {
  buildResponsiveMedia,
  resolveMediaUrl,
} from "../media/mediaUrl";

const DEFAULT_FALLBACK =
  "data:image/svg+xml;charset=UTF-8,%3Csvg xmlns='http://www.w3.org/2000/svg' width='1200' height='900' viewBox='0 0 1200 900'%3E%3Crect width='1200' height='900' fill='%23ecece7'/%3E%3C/svg%3E";

interface SmartImageProps
  extends Omit<
    ImgHTMLAttributes<HTMLImageElement>,
    | "src"
    | "alt"
    | "decoding"
  > {
  src:
    string;

  alt:
    string;

  priority?:
    boolean;

  fallbackSrc?:
    string;

  responsiveWidths?:
    readonly number[];

  quality?:
    number;

  fallbackWidth?:
    number;
}

export function SmartImage({
  src,
  alt,
  priority = false,
  fallbackSrc =
    DEFAULT_FALLBACK,
  responsiveWidths,
  quality,
  fallbackWidth,
  loading,
  fetchPriority,
  className = "",
  onLoad,
  onError,
  draggable,
  srcSet,
  sizes,
  ...props
}: SmartImageProps) {
  const [
    failedSource,
    setFailedSource,
  ] =
    useState<
      string | null
    >(
      null,
    );

  const [
    loadedSource,
    setLoadedSource,
  ] =
    useState<
      string | null
    >(
      null,
    );

  const requestedSource =
    src.trim()
      ? src
      : fallbackSrc;

  const failed =
    failedSource ===
    requestedSource;

  const responsive =
    buildResponsiveMedia(
      requestedSource,
      {
        widths:
          responsiveWidths,

        quality,

        fallbackWidth,
      },
    );

  const fallback =
    resolveMediaUrl(
      fallbackSrc,
    );

  const resolvedSource =
    failed
      ? fallback
      : responsive.src;

  const resolvedSrcSet =
    failed
      ? undefined
      : (
          srcSet ??
          responsive.srcSet
        );

  const loaded =
    loadedSource ===
    resolvedSource;

  function handleLoad(
    event:
      SyntheticEvent<HTMLImageElement>,
  ) {
    setLoadedSource(
      resolvedSource,
    );

    onLoad?.(
      event,
    );
  }

  function handleError(
    event:
      SyntheticEvent<HTMLImageElement>,
  ) {
    if (
      !failed &&
      fallback &&
      fallback !==
        resolvedSource
    ) {
      setFailedSource(
        requestedSource,
      );

      setLoadedSource(
        null,
      );
    }

    onError?.(
      event,
    );
  }

  return (
    <img
      {...props}
      src={
        resolvedSource
      }
      srcSet={
        resolvedSrcSet
      }
      sizes={
        sizes
      }
      alt={
        alt
      }
      loading={
        priority
          ? "eager"
          : (
              loading ??
              "lazy"
            )
      }
      fetchPriority={
        priority
          ? "high"
          : (
              fetchPriority ??
              "auto"
            )
      }
      decoding="async"
      draggable={
        draggable ??
        false
      }
      onLoad={
        handleLoad
      }
      onError={
        handleError
      }
      className={[
        "bg-[var(--store-soft)] transition-opacity duration-300",
        loaded
          ? "opacity-100"
          : "opacity-0",
        className,
      ]
        .filter(
          Boolean,
        )
        .join(
          " ",
        )}
    />
  );
}