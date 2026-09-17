export interface AdminStore {
  tenantId: string;
  name: string;
  slug: string;
  status: string;
  verticalType?: string;
  verticalCode?: string;
}

export interface CreateTenantResponse {
  tenantId: string;
  name: string;
  slug: string;
  status: string;
}

export interface CommerceVerticalResponse {
  verticalType: string;
  code: string;
  enabled: boolean;
  primary: boolean;
}

export interface ApiProblem {
  code?: string;
  message?: string;
}