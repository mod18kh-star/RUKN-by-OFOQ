import { authRequest } from "../../features/auth/authSession";
import { getStorefrontInfo } from "./storefrontApi";

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export interface CustomerReviewResult {
  id: string;
  productId: string;
  rating: number;
  body: string | null;
  status: string;
}

export async function submitVerifiedProductReview(
  storeSlug: string, productId: string, rating: number, body: string,
): Promise<CustomerReviewResult> {
  if (!guidPattern.test(productId) || !Number.isInteger(rating) || rating < 1 || rating > 5 || body.length > 4000) {
    throw new Error("تحقق من المنتج والتقييم والنص المرسل.");
  }
  const store = await getStorefrontInfo(storeSlug);
  if (!guidPattern.test(store.tenantId)) throw new Error("تعذر تحديد المتجر.");
  return authRequest<CustomerReviewResult>(`/api/tenants/${store.tenantId}/account/reviews`, {
    method: "POST", body: JSON.stringify({ productId, rating, body: body.trim() || null }),
  });
}
