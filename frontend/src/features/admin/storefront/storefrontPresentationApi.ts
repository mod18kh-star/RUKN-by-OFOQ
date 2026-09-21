import {
  apiUrl,
  authorizedApiFetch,
} from "../../auth/authSession";

export interface StorefrontPresentationSettings {
  tenantId: string;
  logoUrl: string | null;
  coverImageUrl: string | null;
  announcement: string | null;
  primaryColor: string | null;
  accentColor: string | null;
  themePresetCode: string;
  fontCode: string;
  showCategoriesOnHome: boolean;
  showProductsOnHome: boolean;
  categorySectionTitle: string;
  productSectionTitle: string;
  visualContentJson?: string;
}

export interface UpdateStorefrontPresentationInput {
  logoUrl: string | null;
  coverImageUrl: string | null;
  announcement: string | null;
  primaryColor: string | null;
  accentColor: string | null;
  themePresetCode: string;
  fontCode: string;
  showCategoriesOnHome: boolean;
  showProductsOnHome: boolean;
  categorySectionTitle: string;
  productSectionTitle: string;
  visualContentJson?: string;
}

export class StorefrontPresentationApiError
  extends Error {
  readonly status: number;

  constructor(
    message: string,
    status: number,
  ) {
    super(message);
    this.name =
      "StorefrontPresentationApiError";
    this.status =
      status;
  }
}

async function throwApiError(
  response: Response,
): Promise<never> {
  let message =
    "تعذر حفظ إعدادات واجهة المتجر.";

  try {
    const payload =
      (await response.json()) as {
        message?: unknown;
        title?: unknown;
        assetUrl?: unknown;
      };

    if (
      typeof payload.message ===
        "string" &&
      payload.message.trim()
    ) {
      message =
        payload.message;
    } else if (
      typeof payload.title ===
        "string" &&
      payload.title.trim()
    ) {
      message =
        payload.title;
    }
  } catch {
    // Keep the fallback message.
  }

  throw new StorefrontPresentationApiError(
    message,
    response.status,
  );
}

function path(
  tenantId: string,
) {
  return `/api/tenants/${encodeURIComponent(
    tenantId,
  )}/backoffice/storefront-presentation`;
}

export async function getStorefrontPresentation(
  tenantId: string,
) {
  const response =
    await authorizedApiFetch(
      path(
        tenantId,
      ),
    );

  if (!response.ok) {
    return throwApiError(
      response,
    );
  }

  return (await response.json()) as
    StorefrontPresentationSettings;
}

export async function updateStorefrontPresentation(
  tenantId: string,
  input: UpdateStorefrontPresentationInput,
) {
  const response =
    await authorizedApiFetch(
      path(
        tenantId,
      ),
      {
        method:
          "PUT",

        body:
          JSON.stringify(
            input,
          ),
      },
    );

  if (!response.ok) {
    return throwApiError(
      response,
    );
  }

  return (await response.json()) as
    StorefrontPresentationSettings;
}

export async function uploadStorefrontAsset(
  tenantId: string,
  slot: "logo" | "cover",
  file: File,
) {
  const formData = new FormData();
  formData.set("slot", slot);
  formData.set("file", file);

  const response = await authorizedApiFetch(
    `${path(tenantId)}/asset`,
    {
      method: "POST",
      body: formData,
      headers: undefined,
    },
  );

  if (!response.ok) {
    return throwApiError(response);
  }

  const payload = (await response.json()) as {
    assetUrl: string;
  };

  return apiUrl(payload.assetUrl);
}
