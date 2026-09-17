import type {
  AdminStore,
} from "./storeSetup.types";

const STORAGE_KEY =
  "ofoq.admin.current-store";

export const STORE_UPDATED_EVENT =
  "ofoq:admin-store-updated";

export function readAdminStore():
  AdminStore | null {
  try {
    const value =
      window.localStorage.getItem(
        STORAGE_KEY,
      );

    if (!value) {
      return null;
    }

    const parsed =
      JSON.parse(
        value,
      ) as Partial<AdminStore>;

    if (
      !parsed.tenantId ||
      !parsed.name ||
      !parsed.slug
    ) {
      return null;
    }

    return {
      tenantId:
        parsed.tenantId,

      name:
        parsed.name,

      slug:
        parsed.slug,

      status:
        parsed.status ??
        "Unknown",

      verticalType:
        parsed.verticalType,

      verticalCode:
        parsed.verticalCode,
    };
  }
  catch {
    return null;
  }
}

export function saveAdminStore(
  store: AdminStore,
) {
  window.localStorage.setItem(
    STORAGE_KEY,
    JSON.stringify(
      store,
    ),
  );

  window.dispatchEvent(
    new CustomEvent(
      STORE_UPDATED_EVENT,
    ),
  );
}
export function clearAdminStore() {
  window.localStorage.removeItem(
    STORAGE_KEY,
  );

  window.dispatchEvent(
    new CustomEvent(
      STORE_UPDATED_EVENT,
    ),
  );
}
