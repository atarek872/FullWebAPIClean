import api from '@/api/httpClient';

export interface CreateManagedUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles: string[];
}

export interface UpdateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
}

export interface SetUserRolesRequest {
  roles: string[];
}

export interface UserDetailsResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles: string[];
}

export const UsersService = {
  getAll() {
    return api.get<UserDetailsResponse[]>('/api/identity-admin/users');
  },
  getById(userId: string) {
    return api.get<UserDetailsResponse>(`/api/identity-admin/users/${userId}`);
  },
  exportCsv() {
    return api.get<Blob>('/api/identity-admin/users/export', { responseType: 'blob' });
  },
  create(payload: CreateManagedUserRequest) {
    return api.post<{ message: string; id: string }>('/api/identity-admin/users', payload);
  },
  update(userId: string, payload: UpdateUserRequest) {
    return api.put<{ message: string }>(`/api/identity-admin/users/${userId}`, payload);
  },
  setRoles(userId: string, payload: SetUserRolesRequest) {
    return api.put<{ message: string }>(`/api/identity-admin/users/${userId}/roles`, payload);
  },
  remove(userId: string) {
    return api.delete<{ message: string }>(`/api/identity-admin/users/${userId}`);
  }
};
