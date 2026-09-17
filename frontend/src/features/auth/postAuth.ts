import {
  getCurrentUser,
  getMyTenants,
  type CurrentUser,
} from "./authSession";

import {
  developmentMfaBypassEnabled,
} from "./developmentSecurity";

import {
  clearAdminStore,
  saveAdminStore,
} from "../admin/store-setup/storeSetupStorage";

export function safeInternalPath(
  value: string | null | undefined,
) {
  if (
    value &&
    value.startsWith("/") &&
    !value.startsWith("//")
  ) {
    return value;
  }

  return null;
}

export async function hydrateCurrentTenant() {
  const tenants =
    await getMyTenants();

  const tenant =
    tenants[0] ?? null;

  if (!tenant) {
    return null;
  }

  saveAdminStore({
    tenantId:
      tenant.tenantId,
    name:
      tenant.name,
    slug:
      tenant.slug,
    status:
      tenant.status,
  });

  return tenant;
}

export function defaultDestinationForUser(
  user: CurrentUser,
) {
  /*
   * A real PlatformAdministrator is a distinct platform account
   * context and defaults to /platform.
   *
   * Merchant approval does not grant this role, so normal merchant
   * accounts continue to land in /admin after approval.
   */
  if (
    user.platformRoles.includes(
      "PlatformAdministrator",
    )
  ) {
    if (
      !developmentMfaBypassEnabled() &&
      (
        !user.mfaEnabled ||
        !user.sessionMfaVerified
      )
    ) {
      return "/security/setup?returnTo=%2Fplatform";
    }

    return "/platform";
  }

  if (user.hasTenantMemberships) {
    if (
      !developmentMfaBypassEnabled() &&
      (
        !user.mfaEnabled ||
        !user.sessionMfaVerified
      )
    ) {
      return "/security/setup?returnTo=%2Fadmin";
    }

    return "/admin";
  }

  return "/start/onboarding";
}

export async function resolvePostAuthDestination(
  requestedReturnTo?: string | null,
) {
  const user =
    await getCurrentUser();

  if (
    !user.hasTenantMemberships
  ) {
    clearAdminStore();
  }

  const tenant =
    user.hasTenantMemberships
      ? await hydrateCurrentTenant()
      : null;

  const requested =
    safeInternalPath(
      requestedReturnTo,
    );

  /*
   * Explicit Platform navigation is a separate account context.
   * A Platform Administrator who also owns a store can still
   * enter /platform intentionally without the merchant Draft
   * state hijacking that navigation.
   */
  if (
    requested?.startsWith(
      "/platform",
    )
  ) {
    if (
      !user.platformRoles.includes(
        "PlatformAdministrator",
      ) ||
      (
        !developmentMfaBypassEnabled() &&
        (
          !user.mfaEnabled ||
          !user.sessionMfaVerified
        )
      )
    ) {
      return defaultDestinationForUser(
        user,
      );
    }

    return requested;
  }

  if (
    tenant?.status === "Draft"
  ) {
    return "/start/review";
  }

  if (
    tenant?.status === "Suspended"
  ) {
    return "/start/review?state=suspended";
  }

  if (requested) {
    if (
      requested.startsWith("/admin") &&
      (
        !user.hasTenantMemberships ||
        (
          !developmentMfaBypassEnabled() &&
          (
            !user.mfaEnabled ||
            !user.sessionMfaVerified
          )
        )
      )
    ) {
      return defaultDestinationForUser(
        user,
      );
    }

    return requested;
  }

  return defaultDestinationForUser(
    user,
  );
}
