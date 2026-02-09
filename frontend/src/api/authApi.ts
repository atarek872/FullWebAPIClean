import api from '@/api/httpClient';
import type { LoginRequest, LoginResponse } from '@/types/auth';

export const authApi = {
  login(payload: LoginRequest) {
    return api.post<LoginResponse>('/api/auth/login', payload);
  },
  revokeAll() {
    return api.post('/api/auth/revoke-all');
  }
};
