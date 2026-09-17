import {
  useEffect,
  useState,
} from "react";

import {
  Navigate,
  Outlet,
  useLocation,
} from "react-router";

import {
  clearLocalSession,
  getCurrentUser,
} from "./authSession";

import {
  developmentMfaBypassEnabled,
} from "./developmentSecurity";

import {
  hydrateCurrentTenant,
} from "./postAuth";

type GuardKind =
  | "authenticated"
  | "tenant-backoffice"
  | "platform";

export function ProtectedRoute({
  kind,
}: {
  kind: GuardKind;
}) {
  const location =
    useLocation();

  const [state, setState] =
    useState<
      | "loading"
      | "ready"
      | "login"
      | "security"
      | "onboarding"
      | "review"
      | "suspended"
      | "forbidden"
    >("loading");

  useEffect(() => {
    let cancelled = false;

    async function check() {
      try {
        const user =
          await getCurrentUser();

        if (
          kind ===
          "tenant-backoffice"
        ) {
          if (
            !user.hasTenantMemberships
          ) {
            if (!cancelled) {
              setState(
                "onboarding",
              );
            }

            return;
          }

          const tenant =
            await hydrateCurrentTenant();

          if (
            tenant?.status === "Draft"
          ) {
            if (!cancelled) {
              setState(
                "review",
              );
            }

            return;
          }

          if (
            tenant?.status === "Suspended"
          ) {
            if (!cancelled) {
              setState(
                "suspended",
              );
            }

            return;
          }

          if (
            !developmentMfaBypassEnabled() &&
            (
              !user.mfaEnabled ||
              !user.sessionMfaVerified
            )
          ) {
            if (!cancelled) {
              setState(
                "security",
              );
            }

            return;
          }
        }

        if (
          kind === "platform"
        ) {
          if (
            !user.platformRoles.includes(
              "PlatformAdministrator",
            )
          ) {
            if (!cancelled) {
              setState(
                "forbidden",
              );
            }

            return;
          }

          if (
            !developmentMfaBypassEnabled() &&
            user.mfaEnabled &&
            !user.sessionMfaVerified
          ) {
            if (!cancelled) {
              setState(
                "security",
              );
            }

            return;
          }
        }

        if (!cancelled) {
          setState("ready");
        }
      } catch {
        clearLocalSession();

        if (!cancelled) {
          setState("login");
        }
      }
    }

    void check();

    return () => {
      cancelled = true;
    };
  }, [kind]);

  if (state === "loading") {
    return (
      <div
        dir="rtl"
        className="flex min-h-screen items-center justify-center bg-[#f5f3ed] px-5 text-[#15211d]"
      >
        <div className="w-full max-w-[420px] rounded-[16px] border border-black/[0.07] bg-white p-7 text-center">
          <div className="mx-auto h-1.5 w-28 overflow-hidden rounded-full bg-black/[0.06]">
            <div className="h-full w-1/2 animate-pulse rounded-full bg-[#b58a4b]" />
          </div>
          <p className="mt-4 text-[10px] text-black/45">
            جاري التحقق من الجلسة…
          </p>
        </div>
      </div>
    );
  }

  if (state === "login") {
    const returnTo =
      encodeURIComponent(
        `${location.pathname}${location.search}`,
      );

    return (
      <Navigate
        replace
        to={`/start/login?mode=login&returnTo=${returnTo}`}
      />
    );
  }

  if (state === "security") {
    const returnTo =
      encodeURIComponent(
        `${location.pathname}${location.search}`,
      );

    return (
      <Navigate
        replace
        to={`/security/setup?returnTo=${returnTo}`}
      />
    );
  }

  if (state === "onboarding") {
    return (
      <Navigate
        replace
        to="/start/onboarding"
      />
    );
  }

  if (state === "review") {
    return (
      <Navigate
        replace
        to="/start/review"
      />
    );
  }

  if (state === "suspended") {
    return (
      <Navigate
        replace
        to="/start/review?state=suspended"
      />
    );
  }

  if (state === "forbidden") {
    return (
      <Navigate
        replace
        to="/start/login?mode=login&returnTo=%2Fplatform&reason=platform-access"
      />
    );
  }

  return <Outlet />;
}
