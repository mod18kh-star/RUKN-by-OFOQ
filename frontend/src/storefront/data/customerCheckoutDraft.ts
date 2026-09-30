import { getExpectedUserId } from "../../features/auth/authSession";

const GUID =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function storageKey(storeSlug: string): string | null {
  const userId =
    getExpectedUserId()
      ?.trim()
      .toLowerCase();

  const normalizedStore =
    storeSlug.trim().toLowerCase();

  if (!userId || !normalizedStore) {
    return null;
  }

  // This key only isolates browser-local checkout state.
  // Authentication and authorization remain enforced by the API.
  return `rukn.active-checkout:${encodeURIComponent(
    userId,
  )}:${encodeURIComponent(
    normalizedStore,
  )}`;
}

export function readActiveCheckoutOrderId(
  storeSlug: string,
): string | null {
  const key = storageKey(storeSlug);
  if (!key) return null;

  try {
    const value = window.localStorage.getItem(key);
    return value && GUID.test(value) ? value : null;
  } catch {
    return null;
  }
}

export function saveActiveCheckoutOrderId(
  storeSlug: string,
  orderId: string,
): void {
  const key = storageKey(storeSlug);
  if (!key || !GUID.test(orderId)) return;

  try {
    window.localStorage.setItem(key, orderId);
  } catch {
    // The existing order remains accessible from the customer account.
  }
}

export function clearActiveCheckoutOrderId(
  storeSlug: string,
  orderId?: string,
): void {
  const key = storageKey(storeSlug);
  if (!key) return;

  try {
    if (!orderId || window.localStorage.getItem(key) === orderId) {
      window.localStorage.removeItem(key);
    }
  } catch {
    // Storage may be unavailable.
  }
}

// The pre-receipt manual-payment draft is scoped by customer and store. It is
// NOT an Order and is never shown to the merchant until proof is submitted.
export interface ManualCheckoutDraft {
  cartId: string;
  lines: { productVariantId: string; quantity: number; unitPrice: number }[];
  shippingMethodId: string;
  customerAddressId: string | null;
  paymentAccountId: string;
  customerPhone: string;
  couponCode: string | null;
  expectedAmount: number;
  expectedCurrency: string;
  idempotencyKey: string;
}

function manualDraftKey(storeSlug: string): string | null {
  const key = storageKey(storeSlug);
  return key ? `${key}:unsubmitted-manual-draft` : null;
}
export function saveManualCheckoutDraft(
  storeSlug: string,
  draft: ManualCheckoutDraft,
): void {
  const key = manualDraftKey(storeSlug);

  if (!key) {
    throw new Error(
      "يجب تسجيل الدخول قبل إتمام الشراء.",
    );
  }

  const serialized =
    JSON.stringify(draft);

  // Keep the active checkout draft available during
  // normal navigation and browser refreshes.
  window.sessionStorage.setItem(
    key,
    serialized,
  );

  try {
    window.localStorage.setItem(
      key,
      serialized,
    );
  } catch {
    // Session storage remains the primary store.
  }
}

function isUsableManualCheckoutDraft(
  value: unknown,
): value is ManualCheckoutDraft {
  if (
    !value ||
    typeof value !== "object"
  ) {
    return false;
  }

  const draft =
    value as Partial<ManualCheckoutDraft>;

  if (
    typeof draft.cartId !== "string" ||
    !draft.cartId.trim()
  ) {
    return false;
  }

  if (
    typeof draft.shippingMethodId !== "string" ||
    !draft.shippingMethodId.trim()
  ) {
    return false;
  }

  if (
    draft.customerAddressId !== null &&
    (
      typeof draft.customerAddressId !== "string" ||
      !GUID.test(draft.customerAddressId)
    )
  ) {
    return false;
  }

  if (
    typeof draft.paymentAccountId !== "string" ||
    !draft.paymentAccountId.trim()
  ) {
    return false;
  }

  if (
    typeof draft.idempotencyKey !== "string" ||
    !draft.idempotencyKey.trim()
  ) {
    return false;
  }

  if (
    typeof draft.expectedCurrency !== "string" ||
    !draft.expectedCurrency.trim()
  ) {
    return false;
  }

  if (
    typeof draft.expectedAmount !== "number" ||
    !Number.isFinite(
      draft.expectedAmount,
    )
  ) {
    return false;
  }

  if (
    !Array.isArray(draft.lines) ||
    draft.lines.length === 0 ||
    draft.lines.length > 100
  ) {
    return false;
  }

  return true;
}

export function readManualCheckoutDraft(
  storeSlug: string,
): ManualCheckoutDraft | null {
  const key = manualDraftKey(storeSlug);

  if (!key) {
    return null;
  }

  try {
    const serialized =
      window.sessionStorage.getItem(key) ??
      window.localStorage.getItem(key);

    if (!serialized) {
      return null;
    }

    const parsed =
      JSON.parse(serialized) as unknown;

    if (
      !isUsableManualCheckoutDraft(parsed)
    ) {
      return null;
    }

    // Restore it to session storage when the fallback
    // copy was used.
    window.sessionStorage.setItem(
      key,
      JSON.stringify(parsed),
    );

    return parsed;
  } catch {
    return null;
  }
}

export function clearManualCheckoutDraft(
  storeSlug: string,
): void {
  const key = manualDraftKey(storeSlug);

  if (!key) {
    return;
  }

  try {
    window.sessionStorage.removeItem(key);
  } catch {
    // Ignore unavailable session storage.
  }

  try {
    window.localStorage.removeItem(key);
  } catch {
    // Ignore unavailable local storage.
  }
}
