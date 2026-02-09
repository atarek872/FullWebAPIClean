import api from '@/api/httpClient';

export interface RoleSummaryResponse {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface RoleDetailsResponse extends RoleSummaryResponse {
  permissions: string[];
  groups: string[];
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
}

export interface UpdateRoleRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface AddGroupToRoleRequest {
  groupName: string;
}

export interface SetRolePermissionsRequest {
  permissions: string[];
}

export const RolesService = {
  getAll() {
    return api.get<RoleSummaryResponse[]>('/api/identity-admin/roles');
  },
  getById(roleId: string) {
    return api.get<RoleDetailsResponse>(`/api/identity-admin/roles/${roleId}`);
  },
  create(payload: CreateRoleRequest) {
    return api.post<{ message: string; id: string }>('/api/identity-admin/roles', payload);
  },
  update(roleId: string, payload: UpdateRoleRequest) {
    return api.put<{ message: string }>(`/api/identity-admin/roles/${roleId}`, payload);
  },
  setPermissions(roleId: string, payload: SetRolePermissionsRequest) {
    return api.put<{ message: string }>(`/api/identity-admin/roles/${roleId}/permissions`, payload);
  },
  addGroup(roleId: string, payload: AddGroupToRoleRequest) {
    return api.post<{ message: string }>(`/api/identity-admin/roles/${roleId}/groups`, payload);
  },
  removePermission(roleId: string, permission: string) {
    return api.delete<{ message: string }>(`/api/identity-admin/roles/${roleId}/permissions/${permission}`);
  },
  remove(roleId: string) {
    return api.delete<{ message: string }>(`/api/identity-admin/roles/${roleId}`);
  }
};
