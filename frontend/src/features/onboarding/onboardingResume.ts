import {
  getMyTenants,
} from "../auth/authSession";

export interface ExistingMerchantStore {
  tenantId: string;
  name: string;
  slug: string;
  status: string;
  role: string;
  membershipCreatedAtUtc: string;
}

export async function findExistingMerchantStore():
  Promise<ExistingMerchantStore | null> {
  const stores =
    await getMyTenants();

  if (!stores.length) {
    return null;
  }

  const store =
    stores.find(
      (item) =>
        item.role
          .trim()
          .toLowerCase() ===
        "owner",
    ) ??
    stores[0];

  window.localStorage.setItem(
    "ofoq.admin.current-store",
    JSON.stringify({
      tenantId:
        store.tenantId,
      name:
        store.name,
      slug:
        store.slug,
      status:
        store.status,
    }),
  );

  window.dispatchEvent(
    new Event(
      "ofoq:admin-store-updated",
    ),
  );

  return store;
}
