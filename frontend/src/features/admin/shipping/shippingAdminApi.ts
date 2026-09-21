import { authorizedApiFetch } from "../../auth/authSession";

export interface ShippingMethod {
  id: string;
  code: string;
  name: string;
  type: "Pickup" | "FlatRate" | "Free";
  price: number;
  currency: string;
  minimumOrderAmount: number | null;
  maximumOrderAmount: number | null;
  pickupLocationId: string | null;
  sortOrder: number;
  isEnabled: boolean;
}

export interface FulfillmentLocation {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  countryCode: string;
  city: string;
}

export type ShippingMethodInput = Omit<ShippingMethod, "id">;
const base = (tenantId: string) =>
  `/api/tenants/${encodeURIComponent(tenantId)}/backoffice/fulfillment`;

async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const message = response.status === 401 ? "يرجى تسجيل الدخول مجددًا." :
      response.status === 403 ? "ليس لديك صلاحية إدارة إعدادات التوصيل." :
      response.status === 400 ? "بيانات طريقة التوصيل غير مقبولة. تحقق من الموقع والسعر والعملة." :
      "تعذر إتمام الطلب على السيرفر. حاول مجددًا.";
    throw new Error(message);
  }
  return (await response.json()) as T;
}

export async function listLocations(tenantId: string) {
  return parse<FulfillmentLocation[]>(await authorizedApiFetch(`${base(tenantId)}/locations`));
}
export async function listShippingMethods(tenantId: string) {
  return parse<ShippingMethod[]>(await authorizedApiFetch(`${base(tenantId)}/shipping-methods`));
}
export async function createShippingMethod(tenantId: string, input: ShippingMethodInput) {
  return parse<ShippingMethod>(await authorizedApiFetch(`${base(tenantId)}/shipping-methods`, {
    method: "POST", body: JSON.stringify(input),
  }));
}
export async function updateShippingMethod(tenantId: string, input: ShippingMethod) {
  return parse<ShippingMethod>(await authorizedApiFetch(
    `${base(tenantId)}/shipping-methods/${encodeURIComponent(input.id)}`,
    { method: "PUT", body: JSON.stringify(input) },
  ));
}
