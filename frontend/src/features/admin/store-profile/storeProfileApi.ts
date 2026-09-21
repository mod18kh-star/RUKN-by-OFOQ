import {
  authorizedApiFetch,
} from "../../auth/authSession";

export interface StoreSocialLink {
  socialLinkId: string;
  platformCode: string;
  label: string | null;
  url: string;
  sortOrder: number;
  isVisible: boolean;
}

export interface StoreProfile {
  tenantId: string;
  name: string;
  slug: string;
  status: string;
  websiteUrl: string | null;
  whatsAppNumber: string | null;
  customerServicePhone: string | null;
  secondaryPhone: string | null;
  landlinePhone: string | null;
  physicalAddress: string | null;
  googleMapsUrl: string | null;
  commercialRegistrationNumber: string | null;
  commercialRegistrationNotApplicable: boolean;
  showWebsite: boolean;
  showWhatsApp: boolean;
  showCustomerServicePhone: boolean;
  showSecondaryPhone: boolean;
  showLandlinePhone: boolean;
  showPhysicalAddress: boolean;
  showCommercialRegistration: boolean;
  socialLinks: StoreSocialLink[];
}

export interface UpdateStoreProfileInput {
  websiteUrl: string | null;
  whatsAppNumber: string | null;
  customerServicePhone: string | null;
  secondaryPhone: string | null;
  landlinePhone: string | null;
  physicalAddress: string | null;
  googleMapsUrl: string | null;
  commercialRegistrationNumber: string | null;
  commercialRegistrationNotApplicable: boolean;
  showWebsite: boolean;
  showWhatsApp: boolean;
  showCustomerServicePhone: boolean;
  showSecondaryPhone: boolean;
  showLandlinePhone: boolean;
  showPhysicalAddress: boolean;
  showCommercialRegistration: boolean;
}

export interface StoreSocialLinkInput {
  platformCode: string;
  label: string | null;
  url: string;
  isVisible: boolean;
}

export class StoreProfileApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "StoreProfileApiError";
    this.status = status;
  }
}

function profilePath(tenantId: string) {
  return `/api/tenants/${encodeURIComponent(
    tenantId,
  )}/backoffice/store-profile`;
}

async function throwApiError(
  response: Response,
): Promise<never> {
  let message = "تعذر حفظ بيانات المتجر.";

  try {
    const payload =
      (await response.json()) as {
        message?: unknown;
        title?: unknown;
      };

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
    // The HTTP status is enough when no JSON body was returned.
  }

  if (response.status === 401) {
    message = "انتهت جلسة تسجيل الدخول. سجل دخولك من جديد.";
  }

  if (response.status === 403) {
    message = "ما عندك صلاحية لتعديل بيانات هذا المتجر.";
  }

  throw new StoreProfileApiError(
    message,
    response.status,
  );
}

export async function getStoreProfile(
  tenantId: string,
) {
  const response = await authorizedApiFetch(
    profilePath(tenantId),
  );

  if (!response.ok) {
    return throwApiError(response);
  }

  return (await response.json()) as StoreProfile;
}

export async function updateStoreProfile(
  tenantId: string,
  input: UpdateStoreProfileInput,
) {
  const response = await authorizedApiFetch(
    profilePath(tenantId),
    {
      method: "PUT",
      body: JSON.stringify(input),
    },
  );

  if (!response.ok) {
    return throwApiError(response);
  }
}

export async function replaceStoreSocialLinks(
  tenantId: string,
  socialLinks: StoreSocialLinkInput[],
) {
  const response = await authorizedApiFetch(
    `${profilePath(tenantId)}/social-links`,
    {
      method: "PUT",
      body: JSON.stringify({ socialLinks }),
    },
  );

  if (!response.ok) {
    return throwApiError(response);
  }
}
