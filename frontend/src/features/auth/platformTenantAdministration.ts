const SESSION_STORAGE_KEY =
  "ofoq.platform.tenant-administration";

/*
 * Legacy key from the temporary persistence experiment.
 * It must never influence normal merchant login.
 */
const LEGACY_PERSISTED_STORAGE_KEY =
  "ofoq.platform.tenant-administration.persisted";

export interface PlatformTenantAdministrationContext {
  tenantId: string;
}

function isBrowser() {
  return typeof window !== "undefined";
}

function normalizeTenantId(
  value: unknown,
): string | null {
  if (typeof value !== "string") {
    return null;
  }

  const normalized =
    value.trim().toLowerCase();

  return normalized || null;
}

function clearLegacyPersistedContext() {
  if (!isBrowser()) {
    return;
  }

  window.localStorage.removeItem(
    LEGACY_PERSISTED_STORAGE_KEY,
  );
}

export function beginPlatformTenantAdministration(
  tenantId: string,
) {
  if (!isBrowser()) {
    return;
  }

  clearLegacyPersistedContext();

  const normalized =
    normalizeTenantId(tenantId);

  if (!normalized) {
    throw new Error(
      "A valid tenant ID is required for platform administration.",
    );
  }

  window.sessionStorage.setItem(
    SESSION_STORAGE_KEY,
    JSON.stringify({
      tenantId: normalized,
    }),
  );
}

export function clearPlatformTenantAdministration() {
  if (!isBrowser()) {
    return;
  }

  window.sessionStorage.removeItem(
    SESSION_STORAGE_KEY,
  );

  clearLegacyPersistedContext();
}

export function getPlatformTenantAdministrationTenantId():
  string | null {
  if (!isBrowser()) {
    return null;
  }

  /*
   * Always remove the old localStorage mirror so merchant logins
   * can never inherit Platform Administration state.
   */
  clearLegacyPersistedContext();

  try {
    const raw =
      window.sessionStorage.getItem(
        SESSION_STORAGE_KEY,
      );

    if (!raw) {
      return null;
    }

    const parsed =
      JSON.parse(raw) as {
        tenantId?: unknown;
      };

    const tenantId =
      normalizeTenantId(
        parsed.tenantId,
      );

    if (!tenantId) {
      window.sessionStorage.removeItem(
        SESSION_STORAGE_KEY,
      );

      return null;
    }

    return tenantId;
  }
  catch {
    window.sessionStorage.removeItem(
      SESSION_STORAGE_KEY,
    );

    return null;
  }
}

export function shouldUsePlatformTenantAdministration(
  path: string,
) {
  const tenantId =
    getPlatformTenantAdministrationTenantId();

  if (!tenantId) {
    return false;
  }

  let pathname: string;

  try {
    pathname =
      new URL(
        path,
        window.location.origin,
      ).pathname;
  }
  catch {
    pathname =
      path.split("?")[0]
        ?.split("#")[0] ??
      path;
  }

  const tenantPrefix =
    `/api/tenants/${encodeURIComponent(
      tenantId,
    )}/`;

  return pathname
    .toLowerCase()
    .startsWith(
      tenantPrefix.toLowerCase(),
    );
}