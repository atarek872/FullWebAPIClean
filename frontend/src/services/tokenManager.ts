import { STORAGE_KEYS } from '@/constants/storageKeys';
import { storage } from '@/utils/storage';

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
  };
}

export const tokenManager = {
  getSession() {
    return storage.get<AuthSession>(STORAGE_KEYS.auth);
  },
  setSession(session: AuthSession) {
    storage.set(STORAGE_KEYS.auth, session);
  },
  clearSession() {
    storage.remove(STORAGE_KEYS.auth);
  },
  getTenantId() {
    return storage.get<string>(STORAGE_KEYS.tenantId);
  },
  setTenantId(tenantId: string) {
    storage.set(STORAGE_KEYS.tenantId, tenantId);
  }
};
