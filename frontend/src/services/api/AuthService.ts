import api from '@/api/httpClient';
import type { LoginRequest, LoginResponse } from '@/types/auth';

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  tenantId: string;
  roles?: string[];
}

export interface RefreshTokenRequest {
  refreshToken: string;
  tenantId: string;
}

export interface RevokeTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
}

export const AuthService = {
  register(payload: RegisterRequest) {
    return api.post<{ message: string }>('/api/auth/register', payload);
  },
  login(payload: LoginRequest) {
    return api.post<LoginResponse>('/api/auth/login', payload);
  },
  refresh(payload: RefreshTokenRequest) {
    return api.post<RefreshTokenResponse>('/api/auth/refresh', payload);
  },
  revoke(payload: RevokeTokenRequest) {
    return api.post('/api/auth/revoke', payload);
  },
  revokeAll() {
    return api.post('/api/auth/revoke-all');
  }
};
