import {
  authRequest,
} from "../../features/auth/authSession";

import {
  getStorefrontInfo,
} from "./storefrontApi";

export interface CustomerCartItem {
  id: string;
  productId: string;
  productVariantId: string;
  unitPrice: number;
  currency: string;
  quantity: number;
  lineTotal: number;
  productName?: string | null;
  productSlug?: string | null;
  variantName?: string | null;
  primaryImageUrl?: string | null;
  primaryImageAltText?: string | null;
  compareAtPrice?: number | null;
}

export interface CustomerCart {
  id: string;
  currency: string | null;
  totalQuantity: number;
  totalAmount: number;
  items: CustomerCartItem[];
}

export interface AddCustomerCartItem {
  productId: string;
  productVariantId: string;
  quantity: number;
}

const GUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

async function cartPath(
  storeSlug: string,
): Promise<string> {
  const store = await getStorefrontInfo(storeSlug);

  if (
    !store.tenantId ||
    !GUID_PATTERN.test(store.tenantId)
  ) {
    throw new Error(
      "تعذر تحديد المتجر الحالي. حاول تحديث الصفحة.",
    );
  }

  return `/api/tenants/${store.tenantId}/cart`;
}

export async function getCustomerCart(
  storeSlug: string,
): Promise<CustomerCart | null> {
  const path = await cartPath(storeSlug);

  const result = await authRequest<
    CustomerCart | undefined
  >(
    `${path}/`,
    {
      method: "GET",
    },
  );

  return result ?? null;
}

export async function addCustomerCartItem(
  storeSlug: string,
  item: AddCustomerCartItem,
): Promise<CustomerCart> {
  if (
    !GUID_PATTERN.test(item.productId) ||
    !GUID_PATTERN.test(item.productVariantId) ||
    !Number.isSafeInteger(item.quantity) ||
    item.quantity <= 0
  ) {
    throw new Error(
      "تأكد من اختيار المنتج والكمية المطلوبة.",
    );
  }

  const path = await cartPath(storeSlug);

  return authRequest<CustomerCart>(
    `${path}/items`,
    {
      method: "POST",
      body: JSON.stringify(item),
    },
  );
}

export async function updateCustomerCartQuantity(
  storeSlug: string,
  itemId: string,
  quantity: number,
): Promise<CustomerCart> {
  if (
    !GUID_PATTERN.test(itemId) ||
    !Number.isSafeInteger(quantity) ||
    quantity <= 0
  ) {
    throw new Error(
      "الكمية المطلوبة غير صالحة.",
    );
  }

  const path = await cartPath(storeSlug);

  return authRequest<CustomerCart>(
    `${path}/items/${itemId}`,
    {
      method: "PUT",
      body: JSON.stringify({
        quantity,
      }),
    },
  );
}

export async function removeCustomerCartItem(
  storeSlug: string,
  itemId: string,
): Promise<CustomerCart> {
  if (!GUID_PATTERN.test(itemId)) {
    throw new Error(
      "تعذر تحديد المنتج المطلوب حذفه.",
    );
  }

  const path = await cartPath(storeSlug);

  return authRequest<CustomerCart>(
    `${path}/items/${itemId}`,
    {
      method: "DELETE",
    },
  );
}

export async function clearCustomerCart(
  storeSlug: string,
): Promise<CustomerCart | null> {
  const path = await cartPath(storeSlug);

  const result = await authRequest<
    CustomerCart | undefined
  >(
    `${path}/`,
    {
      method: "DELETE",
    },
  );

  return result ?? null;
}