const configuredCdnBase =
  normalizeCdnBase(
    import.meta.env
      .VITE_MEDIA_CDN_BASE_URL ??
      "",
  );

const optimizerEnabled =
  parseBoolean(
    import.meta.env
      .VITE_MEDIA_BUNNY_OPTIMIZER_ENABLED ??
      "false",
  );

const configuredQuality =
  clampQuality(
    Number(
      import.meta.env
        .VITE_MEDIA_IMAGE_QUALITY ??
        82,
    ),
  );

export const DEFAULT_RESPONSIVE_WIDTHS =
  [
    320,
    480,
    640,
    960,
    1280,
    1600,
  ] as const;

interface ResponsiveMediaOptions {
  widths?:
    readonly number[];

  quality?:
    number;

  fallbackWidth?:
    number;
}

interface ResponsiveMediaResult {
  src:
    string;

  srcSet?:
    string;
}

export function resolveMediaUrl(
  source: string,
) {
  const normalized =
    source.trim();

  if (!normalized) {
    return "";
  }

  if (
    isSpecialUrl(
      normalized,
    ) ||
    isAbsoluteUrl(
      normalized,
    )
  ) {
    return normalized;
  }

  if (
    !configuredCdnBase
  ) {
    return normalized;
  }

  try {
    return new URL(
      normalized.replace(
        /^\/+/,
        "",
      ),
      `${configuredCdnBase}/`,
    ).toString();
  }
  catch {
    return normalized;
  }
}

export function buildResponsiveMedia(
  source: string,
  options:
    ResponsiveMediaOptions = {},
): ResponsiveMediaResult {
  const resolved =
    resolveMediaUrl(
      source,
    );

  if (
    !resolved ||
    !optimizerEnabled ||
    !isConfiguredCdnUrl(
      resolved,
    )
  ) {
    return {
      src:
        resolved,
    };
  }

  const widths =
    normalizeWidths(
      options.widths ??
      DEFAULT_RESPONSIVE_WIDTHS,
    );

  if (
    widths.length === 0
  ) {
    return {
      src:
        resolved,
    };
  }

  const quality =
    clampQuality(
      options.quality ??
      configuredQuality,
    );

  const requestedFallback =
    options.fallbackWidth ??
    960;

  const fallbackWidth =
    nearestWidth(
      widths,
      requestedFallback,
    );

  const src =
    buildBunnyOptimizerUrl(
      resolved,
      {
        width:
          fallbackWidth,

        quality,
      },
    );

  const srcSet =
    widths
      .map(
        (width) =>
          `${buildBunnyOptimizerUrl(
            resolved,
            {
              width,
              quality,
            },
          )} ${width}w`,
      )
      .join(", ");

  return {
    src,
    srcSet,
  };
}

export function isMediaCdnConfigured() {
  return Boolean(
    configuredCdnBase,
  );
}

export function isMediaOptimizerEnabled() {
  return (
    Boolean(
      configuredCdnBase,
    ) &&
    optimizerEnabled
  );
}

function buildBunnyOptimizerUrl(
  source: string,
  options: {
    width:
      number;

    quality:
      number;
  },
) {
  try {
    const url =
      new URL(
        source,
      );

    url.searchParams.set(
      "width",
      String(
        options.width,
      ),
    );

    url.searchParams.set(
      "quality",
      String(
        options.quality,
      ),
    );

    return url.toString();
  }
  catch {
    return source;
  }
}

function isConfiguredCdnUrl(
  source: string,
) {
  if (
    !configuredCdnBase
  ) {
    return false;
  }

  try {
    const sourceUrl =
      new URL(
        source,
      );

    const cdnUrl =
      new URL(
        configuredCdnBase,
      );

    return (
      sourceUrl.origin ===
      cdnUrl.origin
    );
  }
  catch {
    return false;
  }
}

function normalizeWidths(
  widths:
    readonly number[],
) {
  return [
    ...new Set(
      widths
        .map(
          (width) =>
            Math.round(
              width,
            ),
        )
        .filter(
          (width) =>
            Number.isFinite(
              width,
            ) &&
            width >=
              64 &&
            width <=
              3840,
        ),
    ),
  ].sort(
    (
      left,
      right,
    ) =>
      left -
      right,
  );
}

function nearestWidth(
  widths:
    number[],
  requested:
    number,
) {
  return widths.reduce(
    (
      closest,
      width,
    ) =>
      Math.abs(
        width -
        requested,
      ) <
      Math.abs(
        closest -
        requested,
      )
        ? width
        : closest,
    widths[0],
  );
}

function clampQuality(
  quality: number,
) {
  if (
    !Number.isFinite(
      quality,
    )
  ) {
    return 82;
  }

  return Math.min(
    100,
    Math.max(
      1,
      Math.round(
        quality,
      ),
    ),
  );
}

function normalizeCdnBase(
  value: string,
) {
  const normalized =
    value
      .trim()
      .replace(
        /\/+$/,
        "",
      );

  if (
    !normalized
  ) {
    return "";
  }

  try {
    const candidate =
      normalized.includes(
        "://",
      )
        ? normalized
        : `https://${normalized}`;

    const url =
      new URL(
        candidate,
      );

    return url.origin;
  }
  catch {
    return "";
  }
}

function isAbsoluteUrl(
  value: string,
) {
  return /^https?:\/\//i.test(
    value,
  );
}

function isSpecialUrl(
  value: string,
) {
  return (
    value.startsWith(
      "data:",
    ) ||
    value.startsWith(
      "blob:",
    )
  );
}

function parseBoolean(
  value: string,
) {
  return (
    value
      .trim()
      .toLowerCase() ===
    "true"
  );
}