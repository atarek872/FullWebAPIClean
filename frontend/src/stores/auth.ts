import { defineStore } from 'pinia';
import { authApi } from '@/api/authApi';
import type { LoginRequest } from '@/types/auth';
import { tokenManager } from '@/services/tokenManager';

interface AuthState {
  loading: boolean;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
  } | null;
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    loading: false,
    user: tokenManager.getSession()?.user ?? null
  }),
  getters: {
    isAuthenticated: (state) => Boolean(state.user)
  },
  actions: {
    async login(payload: LoginRequest) {
      this.loading = true;
      try {
        const { data } = await authApi.login(payload);
        tokenManager.setSession(data);
        tokenManager.setTenantId(payload.tenantId);
        this.user = data.user;
      } finally {
        this.loading = false;
      }
    },
    async logout() {
      try {
        await authApi.revokeAll();
      } catch {
        // ignore logout API failures on client side
      }
      tokenManager.clearSession();
      this.user = null;
    }
  }
});
