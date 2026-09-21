import {
  apiUrl,
  authorizedApiFetch,
} from "../../auth/authSession";

export interface ProductVariant {
  variantId: string;
  name: string;
  sku: string;
  isDefault: boolean;
  priceOverride: number | null;
  priceOverrideCurrency: string | null;
  trackInventory: boolean;
  quantity: number;
  lowStockThreshold: number;
  continueSellingWhenOutOfStock: boolean;
  isAvailableForSale: boolean;
  isEnabled: boolean;
}

export interface Product {
  productId: string;
  name: string;
  slug: string;
  description: string | null;
  categoryId: string | null;
  price: number;
  currency: string;
  compareAtPrice: number | null;
  status: string;
  isVisible: boolean;
  createdAtUtc: string;
  variants: ProductVariant[];
}

export interface CreateProductInput {
  name: string;
  slug: string;
  description: string | null;
  categoryId: string | null;
  price: number;
  compareAtPrice: number | null;
  currency: string;
  sku: string;
  trackInventory: boolean;
  quantity: number;
  lowStockThreshold: number;
  continueSellingWhenOutOfStock: boolean;
  verticalCode: string;
  primaryImageUrl: string | null;
  publishImmediately: boolean;
}

export interface ProductImage {
  imageId: string;
  url: string;
  altText: string | null;
  sortOrder: number;
  isPrimary: boolean;
}

export interface ProductImagesResponse {
  productId: string;
  images: ProductImage[];
}

export interface UpdateProductInput {
  name: string;
  slug: string;
  description: string | null;
  categoryId: string | null;
  price: number;
  compareAtPrice: number | null;
  currency: string;
  sku: string;
  trackInventory: boolean;
  quantity: number;
  lowStockThreshold: number;
  continueSellingWhenOutOfStock: boolean;
  primaryImageUrl: string | null;
}

export class ProductsApiError extends Error {
  readonly status: number;
  readonly code: string | null;

  constructor(
    message: string,
    status: number,
    code: string | null = null,
  ) {
    super(message);
    this.name = "ProductsApiError";
    this.status = status;
    this.code = code;
  }
}

export function getCurrentTenantId(): string | null {
  const raw = window.localStorage.getItem(
    "ofoq.admin.current-store",
  );

  if (!raw) return null;

  try {
    const value = JSON.parse(raw) as Record<string, unknown>;
    const candidate = value.tenantId ?? value.TenantId;

    return typeof candidate === "string" && candidate.trim()
      ? candidate.trim()
      : null;
  } catch {
    return null;
  }
}

async function throwApiError(
  response: Response,
): Promise<never> {
  let code: string | null = null;
  let message = "تعذر تنفيذ العملية.";

  try {
    const payload =
      (await response.json()) as {
        code?: unknown;
        message?: unknown;
        title?: unknown;
      };

    if (typeof payload.code === "string") {
      code = payload.code;
    }

    if (
      typeof payload.message === "string" &&
      payload.message.trim()
    ) {
      message = payload.message;
    } else if (
      typeof payload.title === "string" &&
      payload.title.trim()
    ) {
      message = payload.title;
    }
  } catch {
    // HTTP status still tells us enough.
  }

  if (response.status === 401) {
    message = "انتهت جلسة تسجيل الدخول. سجل دخولك من جديد.";
  }

  if (response.status === 403) {
    message = "ما عندك صلاحية لإدارة منتجات هذا المتجر.";
  }

  throw new ProductsApiError(
    message,
    response.status,
    code,
  );
}

function productsPath(
  tenantId: string,
) {
  return `/api/tenants/${encodeURIComponent(
    tenantId,
  )}/backoffice/products`;
}

export async function getProducts(
  tenantId: string,
): Promise<Product[]> {
  const response =
    await authorizedApiFetch(
      `${productsPath(tenantId)}/`,
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as Product[];
}

export async function createProduct(
  tenantId: string,
  input: CreateProductInput,
): Promise<Product> {
  const request = {
    name: input.name,
    slug: input.slug,
    description: input.description,
    categoryId: input.categoryId,
    price: input.price,
    compareAtPrice: input.compareAtPrice,
    currency: input.currency,
    sku: input.sku,
    trackInventory: input.trackInventory,
    quantity: input.quantity,
    lowStockThreshold: input.lowStockThreshold,
    continueSellingWhenOutOfStock:
      input.continueSellingWhenOutOfStock,
    verticalCode: input.verticalCode,
    primaryImageUrl:
      input.primaryImageUrl,
  };

  const response =
    await authorizedApiFetch(
      `${productsPath(tenantId)}/`,
      {
        method: "POST",
        body: JSON.stringify(request),
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as Product;
}

async function changeProductState(
  tenantId: string,
  productId: string,
  action:
    | "publish"
    | "draft"
    | "archive"
    | "show"
    | "hide",
): Promise<Product> {
  const response =
    await authorizedApiFetch(
      `${productsPath(tenantId)}/${encodeURIComponent(
        productId,
      )}/${action}`,
      {
        method: "POST",
      },
    );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as Product;
}

export function publishProduct(
  tenantId: string,
  productId: string,
) {
  return changeProductState(
    tenantId,
    productId,
    "publish",
  );
}

export function moveProductToDraft(
  tenantId: string,
  productId: string,
) {
  return changeProductState(
    tenantId,
    productId,
    "draft",
  );
}

export function archiveProduct(
  tenantId: string,
  productId: string,
) {
  return changeProductState(
    tenantId,
    productId,
    "archive",
  );
}

export function showProduct(
  tenantId: string,
  productId: string,
) {
  return changeProductState(
    tenantId,
    productId,
    "show",
  );
}

export function hideProduct(
  tenantId: string,
  productId: string,
) {
  return changeProductState(
    tenantId,
    productId,
    "hide",
  );
}

export async function getProductImages(
  tenantId: string,
  productId: string,
): Promise<ProductImagesResponse> {
  const response =
    await authorizedApiFetch(
      `${productsPath(tenantId)}/${encodeURIComponent(
        productId,
      )}/images`,
    );

  if (!response.ok) {
    return throwApiError(
      response,
    );
  }

  return (await response.json()) as
    ProductImagesResponse;
}

export async function updateProduct(
  tenantId: string,
  productId: string,
  input: UpdateProductInput,
): Promise<Product> {
  const response =
    await authorizedApiFetch(
      `${productsPath(tenantId)}/${encodeURIComponent(
        productId,
      )}`,
      {
        method: "PUT",
        body: JSON.stringify({
          name: input.name,
          slug: input.slug,
          description:
            input.description,
          categoryId:
            input.categoryId,
          price:
            input.price,
          compareAtPrice:
            input.compareAtPrice,
          currency:
            input.currency,
          sku:
            input.sku,
        }),
      },
    );

  if (!response.ok) {
    return throwApiError(
      response,
    );
  }

  return (await response.json()) as
    Product;
}

export async function updateProductInventory(
  tenantId: string,
  productId: string,
  input: Pick<
    UpdateProductInput,
    | "trackInventory"
    | "quantity"
    | "lowStockThreshold"
    | "continueSellingWhenOutOfStock"
  >,
): Promise<Product> {
  const response =
    await authorizedApiFetch(
      `${productsPath(tenantId)}/${encodeURIComponent(
        productId,
      )}/inventory`,
      {
        method: "PUT",
        body: JSON.stringify({
          trackInventory:
            input.trackInventory,
          quantity:
            input.quantity,
          lowStockThreshold:
            input.lowStockThreshold,
          continueSellingWhenOutOfStock:
            input.continueSellingWhenOutOfStock,
        }),
      },
    );

  if (!response.ok) {
    return throwApiError(
      response,
    );
  }

  return (await response.json()) as
    Product;
}

export async function setPrimaryProductImage(
  tenantId: string,
  productId: string,
  imageUrl: string | null,
  altText: string | null,
): Promise<ProductImagesResponse> {
  const cleanUrl =
    imageUrl?.trim() ??
    "";

  const response =
    await authorizedApiFetch(
      `${productsPath(tenantId)}/${encodeURIComponent(
        productId,
      )}/images`,
      {
        method: "PUT",
        body: JSON.stringify({
          images:
            cleanUrl
              ? [
                  {
                    url:
                      cleanUrl,

                    altText:
                      altText?.trim() ||
                      null,

                    isPrimary:
                      true,
                  },
                ]
              : [],
        }),
      },
    );

  if (!response.ok) {
    return throwApiError(
      response,
    );
  }

  return (await response.json()) as
    ProductImagesResponse;
}

export interface ProductOptionValue {
  valueId: string;
  value: string;
  sortOrder: number;
}

export interface ProductOption {
  optionId: string;
  name: string;
  sortOrder: number;
  values: ProductOptionValue[];
}

export interface StructuredProductVariant {
  variantId: string;
  name: string;
  sku: string;
  isDefault: boolean;
  priceOverride: number | null;
  priceOverrideCurrency: string | null;
  trackInventory: boolean;
  quantity: number;
  lowStockThreshold: number;
  continueSellingWhenOutOfStock: boolean;
  isAvailableForSale: boolean;
  isEnabled: boolean;
  selections: Array<{
    optionId: string;
    optionName: string;
    valueId: string;
    value: string;
  }>;
}

export interface ProductAttributeField {
  key: string;
  label: string;
  valueType: string;
  allowedValues: string[];
  value: string | null;
}

export interface ProductAttributesResponse {
  productId: string;
  vertical: string;
  verticalCode: string;
  attributes: ProductAttributeField[];
}

export async function uploadProductAsset(
  tenantId: string,
  file: File,
): Promise<string> {
  const formData = new FormData();
  formData.set("file", file);

  const response = await authorizedApiFetch(
    `${productsPath(tenantId)}/assets`,
    { method: "POST", body: formData },
  );

  if (!response.ok) return throwApiError(response);
  const payload = (await response.json()) as { assetUrl: string };
  return apiUrl(payload.assetUrl);
}

export async function setProductImages(
  tenantId: string,
  productId: string,
  images: Array<{ url: string; altText: string | null; isPrimary: boolean }>,
): Promise<ProductImagesResponse> {
  const response = await authorizedApiFetch(
    `${productsPath(tenantId)}/${encodeURIComponent(productId)}/images`,
    {
      method: "PUT",
      body: JSON.stringify({ images }),
    },
  );

  if (!response.ok) return throwApiError(response);
  return (await response.json()) as ProductImagesResponse;
}

export async function getProductAttributes(
  tenantId: string,
  productId: string,
): Promise<ProductAttributesResponse> {
  const response = await authorizedApiFetch(
    `${productsPath(tenantId)}/${encodeURIComponent(productId)}/attributes`,
  );
  if (!response.ok) return throwApiError(response);
  return (await response.json()) as ProductAttributesResponse;
}

export async function setProductAttributes(
  tenantId: string,
  productId: string,
  values: Array<{ key: string; value: string }>,
): Promise<ProductAttributesResponse> {
  const response = await authorizedApiFetch(
    `${productsPath(tenantId)}/${encodeURIComponent(productId)}/attributes`,
    {
      method: "PUT",
      body: JSON.stringify({ values }),
    },
  );
  if (!response.ok) return throwApiError(response);
  return (await response.json()) as ProductAttributesResponse;
}

export async function createProductOption(
  tenantId: string,
  productId: string,
  name: string,
  sortOrder: number,
): Promise<ProductOption> {
  const response = await authorizedApiFetch(
    `${productsPath(tenantId)}/${encodeURIComponent(productId)}/options/`,
    {
      method: "POST",
      body: JSON.stringify({ name, sortOrder }),
    },
  );
  if (!response.ok) return throwApiError(response);
  return (await response.json()) as ProductOption;
}

export async function createProductOptionValue(
  tenantId: string,
  productId: string,
  optionId: string,
  value: string,
  sortOrder: number,
): Promise<ProductOptionValue> {
  const response = await authorizedApiFetch(
    `${productsPath(tenantId)}/${encodeURIComponent(productId)}/options/${encodeURIComponent(optionId)}/values`,
    {
      method: "POST",
      body: JSON.stringify({ value, sortOrder }),
    },
  );
  if (!response.ok) return throwApiError(response);
  return (await response.json()) as ProductOptionValue;
}

export async function createStructuredProductVariant(
  tenantId: string,
  productId: string,
  input: {
    name: string;
    sku: string;
    priceOverride: number | null;
    trackInventory: boolean;
    quantity: number;
    lowStockThreshold: number;
    continueSellingWhenOutOfStock: boolean;
    optionValueIds: string[];
  },
): Promise<StructuredProductVariant> {
  const response = await authorizedApiFetch(
    `${productsPath(tenantId)}/${encodeURIComponent(productId)}/variants/`,
    { method: "POST", body: JSON.stringify(input) },
  );
  if (!response.ok) return throwApiError(response);
  return (await response.json()) as StructuredProductVariant;
}

export interface CommerceVerticalProfile {
  verticalType: string;
  code: string;
  enabled: boolean;
  primary: boolean;
}

export interface CommerceCapabilityProfile {
  capabilityType: string;
  enabled: boolean;
  overridden: boolean;
}

export interface CommerceProfile {
  verticals: CommerceVerticalProfile[];
  capabilities: CommerceCapabilityProfile[];
}

export async function getCommerceProfile(
  tenantId: string,
): Promise<CommerceProfile> {
  const response = await authorizedApiFetch(
    `/api/tenants/${encodeURIComponent(tenantId)}/backoffice/commerce/profile`,
  );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as CommerceProfile;
}
