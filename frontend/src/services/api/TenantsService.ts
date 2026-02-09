import api from '@/api/httpClient';

export interface CreateTenantRequest {
  name: string;
  slug: string;
  subdomain: string;
  planId: string;
}

export interface OnboardingRequest {
  adminEmail: string;
  adminPassword: string;
}

export interface UpdateTenantSettingsRequest {
  tenantId: string;
  settingsJson?: string;
  apiRequestLimitPerDay?: number;
  storageLimitMb?: number;
}

export interface AssignPlanRequest {
  tenantId: string;
  planId: string;
}

export interface SuspendTenantRequest {
  tenantId: string;
  reason?: string;
}

export interface TenantSummaryDto {
  tenantId: string;
  name: string;
  slug: string;
  schema: string;
  plan: string;
  status: string;
}

export const TenantsService = {
  getAll() {
    return api.get<TenantSummaryDto[]>('/api/admin/tenants');
  },
  create(payload: CreateTenantRequest) {
    return api.post<{ tenantId: string }>('/api/admin/tenants', payload);
  },
  onboard(tenantId: string, payload: OnboardingRequest) {
    return api.post(`/api/admin/tenants/${tenantId}/onboard`, payload);
  },
  updateSettings(payload: UpdateTenantSettingsRequest) {
    return api.put('/api/admin/tenants/settings', payload);
  },
  assignPlan(payload: AssignPlanRequest) {
    return api.put('/api/admin/tenants/assign-plan', payload);
  },
  suspend(payload: SuspendTenantRequest) {
    return api.put('/api/admin/tenants/suspend', payload);
  }
};
