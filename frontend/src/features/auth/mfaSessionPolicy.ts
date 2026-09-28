export type MfaReopenPolicy =
  | "EveryBrowserSession"
  | "Minutes15"
  | "Minutes30"
  | "Minutes60";

export const MFA_REAUTH_REQUIRED_EVENT =
  "ofoq:mfa-reauth-required";

const markerPrefix =
  "ofoq_mfa_browser_";

interface BrowserMfaUser {
  userId: string;
  sessionId: string | null;
  mfaEnabled: boolean;
  sessionMfaVerified: boolean;
  mfaReopenPolicy: MfaReopenPolicy;
  sessionMfaVerifiedAtUtc: string | null;
}

function readTokenContext(token: string) {
  try {
    const part = token.split(".")[1];

    if (!part) {
      return null;
    }

    const normalized =
      part
        .replace(/-/g, "+")
        .replace(/_/g, "/");

    const payload =
      JSON.parse(
        window.atob(
          normalized.padEnd(
            Math.ceil(
              normalized.length / 4,
            ) * 4,
            "=",
          ),
        ),
      ) as {
        sub?: unknown;
        sid?: unknown;
        amr?: unknown;
      };

    const methods =
      Array.isArray(payload.amr)
        ? payload.amr.filter(
            (value): value is string =>
              typeof value === "string",
          )
        : typeof payload.amr === "string"
          ? [payload.amr]
          : [];

    return {
      userId:
        typeof payload.sub === "string"
          ? payload.sub
              .trim()
              .toLowerCase()
          : null,

      sessionId:
        typeof payload.sid === "string"
          ? payload.sid
              .trim()
              .toLowerCase()
          : null,

      methods,
    };
  } catch {
    return null;
  }
}

function markerKey(
  userId: string,
  sessionId: string,
) {
  const clean =
    (value: string) =>
      value
        .trim()
        .toLowerCase()
        .replace(
          /[^a-z0-9-]/g,
          "",
        );

  return `${markerPrefix}${clean(
    userId,
  )}_${clean(sessionId)}`;
}

function setMarker(
  key: string,
) {
  const secure =
    window.location.protocol ===
    "https:"
      ? "; Secure"
      : "";

  /*
   * Session cookie only.
   * It contains no credential or secret.
   *
   * The API remains authoritative for MFA.
   */
  document.cookie =
    `${key}=1; Path=/; SameSite=Lax${secure}`;
}

export function markMfaBrowserSessionFromToken(
  token: string,
) {
  const context =
    readTokenContext(
      token,
    );

  if (
    !context?.userId ||
    !context.sessionId ||
    !context.methods.includes(
      "mfa",
    )
  ) {
    return;
  }

  try {
    setMarker(
      markerKey(
        context.userId,
        context.sessionId,
      ),
    );
  } catch {
    // Browser cookies may be unavailable.
  }
}

export function markMfaBrowserSession(
  user: Pick<
    BrowserMfaUser,
    "userId" | "sessionId"
  >,
) {
  if (!user.sessionId) {
    return;
  }

  try {
    setMarker(
      markerKey(
        user.userId,
        user.sessionId,
      ),
    );
  } catch {
    // Browser cookies may be unavailable.
  }
}

function hasMarker(
  user: Pick<
    BrowserMfaUser,
    "userId" | "sessionId"
  >,
) {
  if (!user.sessionId) {
    return false;
  }

  try {
    const expected =
      `${markerKey(
        user.userId,
        user.sessionId,
      )}=1`;

    return document.cookie
      .split(";")
      .some(
        (part) =>
          part.trim() ===
          expected,
      );
  } catch {
    return false;
  }
}

export function shouldRequireMfaForBrowserSession(
  user: BrowserMfaUser,
) {
  if (
    !user.mfaEnabled ||
    !user.sessionMfaVerified ||
    !user.sessionId
  ) {
    return true;
  }

  /*
   * Once this browser session was accepted,
   * 15/30/60-minute policies do NOT expire while
   * the panel remains open.
   */
  if (hasMarker(user)) {
    return false;
  }

  if (
    user.mfaReopenPolicy ===
    "EveryBrowserSession"
  ) {
    return true;
  }

  const verifiedAt =
    user.sessionMfaVerifiedAtUtc
      ? Date.parse(
          user.sessionMfaVerifiedAtUtc,
        )
      : Number.NaN;

  if (
    !Number.isFinite(
      verifiedAt,
    )
  ) {
    return true;
  }

  const minutes =
    user.mfaReopenPolicy ===
    "Minutes15"
      ? 15
      : user.mfaReopenPolicy ===
          "Minutes30"
        ? 30
        : 60;

  return (
    Date.now() -
      verifiedAt >=
    minutes * 60_000
  );
}

export function accessTokenHasMfa(
  token: string,
) {
  return Boolean(
    readTokenContext(
      token,
    )?.methods.includes(
      "mfa",
    ),
  );
}

export function notifyMfaReauthenticationRequired() {
  try {
    window.dispatchEvent(
      new Event(
        MFA_REAUTH_REQUIRED_EVENT,
      ),
    );
  } catch {
    // No-op outside browser event environment.
  }
}

export function clearMfaBrowserSessionMarkers() {
  try {
    const secure =
      window.location.protocol ===
      "https:"
        ? "; Secure"
        : "";

    for (
      const part of
      document.cookie.split(";")
    ) {
      const key =
        part
          .trim()
          .split("=")[0];

      if (
        !key.startsWith(
          markerPrefix,
        )
      ) {
        continue;
      }

      document.cookie =
        `${key}=; Max-Age=0; Path=/; SameSite=Lax${secure}`;
    }
  } catch {
    // Browser cookies may be unavailable.
  }
}