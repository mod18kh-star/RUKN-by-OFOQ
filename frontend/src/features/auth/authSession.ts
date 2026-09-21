import {
  shouldUsePlatformTenantAdministration,
} from "./platformTenantAdministration";
export const ACCESS_TOKEN_KEY =
  "ofoq.access-token";

export const ACCESS_TOKEN_EXPIRY_KEY =
  "ofoq.access-token-expires-at";

export const AUTH_EMAIL_KEY =
  "ofoq.auth.email";

export const AUTH_USER_ID_KEY =
  "ofoq.auth.user-id";

const ADMIN_CURRENT_STORE_KEY =
  "ofoq.admin.current-store";

const API_BASE = (
  import.meta.env.VITE_API_BASE_URL ?? ""
)
  .trim()
  .replace(/\/+$/, "");

export interface CurrentUser {
  userId: string;
  email: string;
  status: string;
  emailVerified: boolean;
  mfaEnabled: boolean;
  sessionMfaVerified: boolean;
  authenticationMethods: string[];
  platformRoles: string[];
  hasTenantMemberships: boolean;
  sessionId: string | null;
  fullName: string | null;
  phoneNumber: string | null;
}

export interface MyTenant {
  tenantId: string;
  name: string;
  slug: string;
  status: string;
  role: string;
  membershipCreatedAtUtc: string;
}

export interface RefreshSessionResponse {
  accessToken: string;
  expiresAtUtc: string;
}

export class AuthSessionError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "AuthSessionError";
    this.status = status;
  }
}

export function authApiConfigured() {
  return Boolean(API_BASE);
}

export function apiUrl(path: string) {
  return `${API_BASE}${path}`;
}

export function getAccessToken() {
  try {
    return window.localStorage.getItem(
      ACCESS_TOKEN_KEY,
    );
  } catch {
    return null;
  }
}

function readJwtUserId(
  token: string,
) {
  try {
    const parts =
      token.split(".");

    if (parts.length < 2) {
      return null;
    }

    const normalized =
      parts[1]
        .replace(/-/g, "+")
        .replace(/_/g, "/");

    const padded =
      normalized.padEnd(
        Math.ceil(
          normalized.length / 4,
        ) * 4,
        "=",
      );

    const payload =
      JSON.parse(
        window.atob(
          padded,
        ),
      ) as {
        sub?: unknown;
      };

    return typeof payload.sub === "string" &&
      payload.sub.trim()
      ? payload.sub
          .trim()
          .toLowerCase()
      : null;
  } catch {
    return null;
  }
}

export function getExpectedUserId() {
  try {
    return (
      window.localStorage.getItem(
        AUTH_USER_ID_KEY,
      ) ?? ""
    )
      .trim()
      .toLowerCase() || null;
  } catch {
    return null;
  }
}

function accessTokenMatchesExpectedUser(
  token: string,
) {
  const expectedUserId =
    getExpectedUserId();

  if (!expectedUserId) {
    return true;
  }

  const tokenUserId =
    readJwtUserId(
      token,
    );

  return tokenUserId ===
    expectedUserId;
}

export function saveAccessSession(
  token: string,
  expiresAtUtc: string,
  email?: string,
  userId?: string,
) {
  const tokenUserId =
    readJwtUserId(
      token,
    );

  const explicitUserId =
    userId
      ?.trim()
      .toLowerCase() ??
    null;

  if (
    explicitUserId &&
    tokenUserId &&
    explicitUserId !==
      tokenUserId
  ) {
    throw new AuthSessionError(
      "ط¸â€،ط¸ث†ط¸ظ¹ط·آ© ط·آ¬ط¸â€‍ط·آ³ط·آ© ط·آ§ط¸â€‍ط·آ¯ط·آ®ط¸ث†ط¸â€‍ ط·ط›ط¸ظ¹ط·آ± ط¸â€¦ط·ع¾ط·آ·ط·آ§ط·آ¨ط¸â€ڑط·آ©.",
      409,
    );
  }

  const resolvedUserId =
    explicitUserId ??
    tokenUserId;

  window.localStorage.setItem(
    ACCESS_TOKEN_KEY,
    token,
  );

  window.localStorage.setItem(
    ACCESS_TOKEN_EXPIRY_KEY,
    expiresAtUtc,
  );

  if (email) {
    window.localStorage.setItem(
      AUTH_EMAIL_KEY,
      email,
    );
  }

  if (resolvedUserId) {
    window.localStorage.setItem(
      AUTH_USER_ID_KEY,
      resolvedUserId,
    );
  }
}

export function clearLocalSession() {
  try {
    window.localStorage.removeItem(
      ACCESS_TOKEN_KEY,
    );

    window.localStorage.removeItem(
      ACCESS_TOKEN_EXPIRY_KEY,
    );

    window.localStorage.removeItem(
      AUTH_EMAIL_KEY,
    );

    window.localStorage.removeItem(
      AUTH_USER_ID_KEY,
    );

    window.localStorage.removeItem(
      ADMIN_CURRENT_STORE_KEY,
    );
  } catch {
    // Storage can be unavailable in restrictive browser contexts.
  }
}

async function readErrorMessage(
  response: Response,
  fallback: string,
) {
  try {
    const payload = (await response.json()) as {
      message?: unknown;
      title?: unknown;
    };

    if (
      typeof payload.message === "string" &&
      payload.message.trim()
    ) {
      return payload.message;
    }

    if (
      typeof payload.title === "string" &&
      payload.title.trim()
    ) {
      return payload.title;
    }
  } catch {
    // HTTP status is enough.
  }

  return fallback;
}

async function revokeBrowserRefreshSession() {
  try {
    await fetch(
      apiUrl("/api/auth/logout"),
      {
        method: "POST",
        credentials: "include",
        headers: {
          Accept:
            "application/json",
        },
      },
    );
  } catch {
    // Best effort: local identity must still be cleared.
  }
}

export async function refreshAccessToken() {
  if (!authApiConfigured()) {
    throw new AuthSessionError(
      "ط·آ¹ط¸â€ ط¸ث†ط·آ§ط¸â€  ط·آ§ط¸â€‍ط¸â‚¬API ط·ط›ط¸ظ¹ط·آ± ط¸â€¦ط·آ¶ط·آ¨ط¸ث†ط·آ·.",
      0,
    );
  }

  const expectedUserId =
    getExpectedUserId();

  const response = await fetch(
    apiUrl("/api/auth/refresh"),
    {
      method: "POST",
      credentials: "include",
      headers: {
        Accept: "application/json",
      },
    },
  );

  if (!response.ok) {
    clearLocalSession();

    throw new AuthSessionError(
      await readErrorMessage(
        response,
        "ط·آ§ط¸â€ ط·ع¾ط¸â€،ط·ع¾ ط·آ¬ط¸â€‍ط·آ³ط·آ© ط·ع¾ط·آ³ط·آ¬ط¸ظ¹ط¸â€‍ ط·آ§ط¸â€‍ط·آ¯ط·آ®ط¸ث†ط¸â€‍.",
      ),
      response.status,
    );
  }

  const result =
    (await response.json()) as RefreshSessionResponse;

  const refreshedUserId =
    readJwtUserId(
      result.accessToken,
    );

  if (
    expectedUserId &&
    (
      !refreshedUserId ||
      refreshedUserId !==
        expectedUserId
    )
  ) {
    await revokeBrowserRefreshSession();
    clearLocalSession();

    throw new AuthSessionError(
      "ط·ع¾ط¸â€¦ ط·ع¾ط·آ³ط·آ¬ط¸ظ¹ط¸â€‍ ط·آ¯ط·آ®ط¸ث†ط¸â€‍ ط·آ­ط·آ³ط·آ§ط·آ¨ ط¸â€¦ط·آ®ط·ع¾ط¸â€‍ط¸ظ¾ ط¸ظ¾ط¸ظ¹ ط¸â€ ط¸ظ¾ط·آ³ ط·آ§ط¸â€‍ط¸â€¦ط·ع¾ط·آµط¸ظ¾ط·آ­. ط·آ³ط·آ¬ط¸â€کط¸â€‍ ط·آ§ط¸â€‍ط·آ¯ط·آ®ط¸ث†ط¸â€‍ ط·آ¨ط·آ§ط¸â€‍ط·آ­ط·آ³ط·آ§ط·آ¨ ط·آ§ط¸â€‍ط¸â€¦ط·آ·ط¸â€‍ط¸ث†ط·آ¨ ط¸â€¦ط¸â€  ط·آ¬ط·آ¯ط¸ظ¹ط·آ¯.",
      409,
    );
  }

  saveAccessSession(
    result.accessToken,
    result.expiresAtUtc,
    undefined,
    refreshedUserId ?? undefined,
  );

  return result.accessToken;
}

export async function authorizedApiFetch(
  path: string,
  init: RequestInit = {},
  retry = true,
) {
  if (!authApiConfigured()) {
    throw new AuthSessionError(
      "ط·آ¹ط¸â€ ط¸ث†ط·آ§ط¸â€  ط·آ§ط¸â€‍ط¸â‚¬API ط·ط›ط¸ظ¹ط·آ± ط¸â€¦ط·آ¶ط·آ¨ط¸ث†ط·آ·.",
      0,
    );
  }

  let token = getAccessToken();

  if (
    token &&
    !accessTokenMatchesExpectedUser(
      token,
    )
  ) {
    await revokeBrowserRefreshSession();
    clearLocalSession();

    throw new AuthSessionError(
      "ط·آ¬ط¸â€‍ط·آ³ط·آ© ط·آ§ط¸â€‍ط¸â€¦ط·ع¾ط·آµط¸ظ¾ط·آ­ ط¸â€¦ط·آ±ط·ع¾ط·آ¨ط·آ·ط·آ© ط·آ¨ط·آ­ط·آ³ط·آ§ط·آ¨ ط¸â€¦ط·آ®ط·ع¾ط¸â€‍ط¸ظ¾. ط·آ³ط·آ¬ط¸â€کط¸â€‍ ط·آ§ط¸â€‍ط·آ¯ط·آ®ط¸ث†ط¸â€‍ ط·آ¨ط·آ­ط·آ³ط·آ§ط·آ¨ط¸ئ’ ط¸â€¦ط¸â€  ط·آ¬ط·آ¯ط¸ظ¹ط·آ¯.",
      409,
    );
  }

  if (!token) {
    try {
      token = await refreshAccessToken();
    } catch (error) {
      if (
        error instanceof AuthSessionError &&
        error.status === 409
      ) {
        throw error;
      }

      token = null;
    }
  }

  const headers = new Headers(init.headers);

  if (
    shouldUsePlatformTenantAdministration(
      path,
    )
  ) {
    headers.set(
      "X-OFOQ-Platform-Administration",
      "1",
    );
  }

  headers.set(
    "Accept",
    "application/json",
  );

  if (
    init.body &&
    !(init.body instanceof FormData) &&
    !headers.has("Content-Type")
  ) {
    headers.set(
      "Content-Type",
      "application/json",
    );
  }

  if (token) {
    headers.set(
      "Authorization",
      `Bearer ${token}`,
    );
  }

  const response = await fetch(
    apiUrl(path),
    {
      ...init,
      credentials: "include",
      headers,
    },
  );

  if (
    response.status === 401 &&
    retry
  ) {
    try {
      const nextToken =
        await refreshAccessToken();

      const retryHeaders =
        new Headers(init.headers);

      if (
        shouldUsePlatformTenantAdministration(
          path,
        )
      ) {
        retryHeaders.set(
          "X-OFOQ-Platform-Administration",
          "1",
        );
      }

      retryHeaders.set(
        "Accept",
        "application/json",
      );

      if (
        init.body &&
        !(init.body instanceof FormData) &&
        !retryHeaders.has(
          "Content-Type",
        )
      ) {
        retryHeaders.set(
          "Content-Type",
          "application/json",
        );
      }

      retryHeaders.set(
        "Authorization",
        `Bearer ${nextToken}`,
      );

      return await fetch(
        apiUrl(path),
        {
          ...init,
          credentials: "include",
          headers: retryHeaders,
        },
      );
    } catch (error) {
      clearLocalSession();

      if (
        error instanceof AuthSessionError &&
        error.status === 409
      ) {
        throw error;
      }
    }
  }

  return response;
}

export async function getCurrentUser() {
  const response =
    await authorizedApiFetch(
      "/api/auth/me",
    );

  if (!response.ok) {
    throw new AuthSessionError(
      await readErrorMessage(
        response,
        "ط·ع¾ط·آ¹ط·آ°ط·آ± ط·ع¾ط·آ­ط¸â€¦ط¸ظ¹ط¸â€‍ ط·آ¨ط¸ظ¹ط·آ§ط¸â€ ط·آ§ط·ع¾ ط·آ§ط¸â€‍ط·آ­ط·آ³ط·آ§ط·آ¨.",
      ),
      response.status,
    );
  }

  return (await response.json()) as CurrentUser;
}

export async function getMyTenants() {
  const response =
    await authorizedApiFetch(
      "/api/tenants/mine",
    );

  if (!response.ok) {
    throw new AuthSessionError(
      await readErrorMessage(
        response,
        "ط·ع¾ط·آ¹ط·آ°ط·آ± ط·ع¾ط·آ­ط¸â€¦ط¸ظ¹ط¸â€‍ ط·آ§ط¸â€‍ط¸â€¦ط·ع¾ط·آ§ط·آ¬ط·آ± ط·آ§ط¸â€‍ط¸â€¦ط·آ±ط·ع¾ط·آ¨ط·آ·ط·آ© ط·آ¨ط·آ§ط¸â€‍ط·آ­ط·آ³ط·آ§ط·آ¨.",
      ),
      response.status,
    );
  }

  return (await response.json()) as MyTenant[];
}

export async function authRequest<T>(
  path: string,
  init: RequestInit,
): Promise<T> {
  const response =
    await authorizedApiFetch(
      path,
      init,
    );

  if (!response.ok) {
    throw new AuthSessionError(
      await readErrorMessage(
        response,
        "ط·ع¾ط·آ¹ط·آ°ط·آ± ط·ع¾ط¸â€ ط¸ظ¾ط¸ظ¹ط·آ° ط·آ§ط¸â€‍ط·آ¹ط¸â€¦ط¸â€‍ط¸ظ¹ط·آ©.",
      ),
      response.status,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function logoutSession() {
  try {
    await authorizedApiFetch(
      "/api/auth/logout",
      {
        method: "POST",
      },
      false,
    );
  } finally {
    clearLocalSession();
  }
}
