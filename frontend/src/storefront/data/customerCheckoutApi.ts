import { authRequest } from "../../features/auth/authSession";
import { getStorefrontInfo } from "./storefrontApi";

export interface CheckoutShippingMethod {
  id: string;
  name: string;
  type: string;
  price: number;
  currency: string;
  minimumOrderAmount: number | null;
  maximumOrderAmount: number | null;
  isEnabled: boolean;
}

export interface CheckoutOrderItem {
  id: string;
  productId: string;
  productVariantId: string;
  productName: string;
  variantName: string;
  sku: string;
  unitPrice: number;
  currency: string;
  quantity: number;
  lineTotal: number;
}

export interface CheckoutOrder {
  orderId: string;
  sourceCartId: string;
  status: string;
  currency: string;
  totalQuantity: number;
  subtotalAmount: number;
  shippingAmount: number;
  discountAmount: number;
  totalAmount: number;
  appliedCouponCode: string | null;
  shippingMethodName: string | null;
  createdAtUtc: string;
  idempotentReplay: boolean;
  items: CheckoutOrderItem[];
}

const GUID =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

async function tenantPath(storeSlug: string) {
  const store = await getStorefrontInfo(storeSlug);

  if (!GUID.test(store.tenantId)) {
    throw new Error("تعذر تحديد المتجر الحالي.");
  }

  return `/api/tenants/${store.tenantId}`;
}

export async function getCheckoutShippingMethods(
  storeSlug: string,
): Promise<CheckoutShippingMethod[]> {
  const path = await tenantPath(storeSlug);

  return authRequest<CheckoutShippingMethod[]>(
    `${path}/shipping/methods`,
    { method: "GET" },
  );
}

export interface CheckoutCouponQuote {
  couponCode: string;
  currency: string;
  subtotalAmount: number;
  shippingAmount: number;
  discountAmount: number;
  totalAmount: number;
}

// Authenticated quote: never creates an order and never reserves coupon usage.
// The checkout endpoint rechecks all values under its own transaction.
export async function previewCheckoutCoupon(
  storeSlug: string,
  shippingMethodId: string,
  couponCode: string,
): Promise<CheckoutCouponQuote> {
  if (!GUID.test(shippingMethodId) || !couponCode || couponCode.length > 60) {
    throw new Error("اختر طريقة الاستلام وأدخل رمز كوبون صحيحًا.");
  }
  const path = await tenantPath(storeSlug);
  return authRequest<CheckoutCouponQuote>(`${path}/checkout/quote`, {
    method: "POST",
    body: JSON.stringify({ shippingMethodId, couponCode }),
  });
}

export async function submitCustomerCheckout(
  storeSlug: string,
  shippingMethodId: string,
  idempotencyKey: string,
  couponCode: string | null = null,
): Promise<CheckoutOrder> {
  if (
    !GUID.test(shippingMethodId) ||
    !GUID.test(idempotencyKey)
  ) {
    throw new Error("بيانات تأكيد الطلب غير صالحة.");
  }

  const path = await tenantPath(storeSlug);

  return authRequest<CheckoutOrder>(
    `${path}/checkout/`,
    {
      method: "POST",
      headers: {
        "Idempotency-Key": idempotencyKey,
      },
      body: JSON.stringify({
        customerAddressId: null,
        shippingMethodId,
        couponCode: couponCode?.trim().toUpperCase() || null,
      }),
    },
  );
}