import api from '@/api/httpClient';

export interface PermissionCatalogResponse {
  permissions: string[];
  groups: Array<{
    name: string;
    permissions: string[];
  }>;
}

export const PermissionsService = {
  getCatalog() {
    return api.get<PermissionCatalogResponse>('/api/identity-admin/permissions/catalog');
  }
};
