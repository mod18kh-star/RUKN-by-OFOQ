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
    !value.startsWith("//") &&
    !value.startsWith("/security/setup")
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
   * Platform Administrator:
   * Development may use its dedicated bootstrap bypass.
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

  /*
   * Merchant Back Office:
   * API authorization always requires pwd + mfa.
   * Do not apply the Platform development bypass here.
   */
  if (
    user.hasTenantMemberships
  ) {
    if (
      !user.mfaEnabled ||
      !user.sessionMfaVerified
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

  const requested =
    safeInternalPath(
      requestedReturnTo,
    );

  const isPlatformAdministrator =
    user.platformRoles.includes(
      "PlatformAdministrator",
    );

  /*
   * Platform Administration remains a separate context.
   */
  if (
    requested?.startsWith(
      "/platform",
    )
  ) {
    if (
      !isPlatformAdministrator ||
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

  /*
   * Platform admins default to /platform.
   * Do not run merchant tenant hydration first.
   */
  if (
    isPlatformAdministrator &&
    !requested
  ) {
    return defaultDestinationForUser(
      user,
    );
  }

  /*
   * IMPORTANT:
   *
   * Merchant security must be resolved BEFORE loading the tenant.
   *
   * First login:
   * pwd -> security setup -> authenticator enrollment.
   *
   * Returning login:
   * pwd -> MFA challenge -> admin.
   */
  if (
    user.hasTenantMemberships &&
    (
      !user.mfaEnabled ||
      !user.sessionMfaVerified
    )
  ) {
    const returnTo =
      requested?.startsWith("/admin")
        ? requested
        : "/admin";

    return `/security/setup?returnTo=${encodeURIComponent(
      returnTo,
    )}`;
  }

  if (
    !user.hasTenantMemberships
  ) {
    clearAdminStore();

    return requested ??
      defaultDestinationForUser(
        user,
      );
  }

  /*
   * Only hydrate the merchant tenant AFTER the security gate passed.
   */
  const tenant =
    await hydrateCurrentTenant();

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
      requested.startsWith(
        "/admin",
      ) &&
      !user.hasTenantMemberships
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