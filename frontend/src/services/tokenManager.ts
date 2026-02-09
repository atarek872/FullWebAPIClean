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

interface JwtPayload {
  permission?: string | string[];
  permissions?: string[];
  scp?: string | string[];
}

const decodeJwtPayload = (token: string): JwtPayload | null => {
  const [, payload] = token.split('.');
  if (!payload) {
    return null;
  }

  try {
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = atob(normalized);
    return JSON.parse(decoded) as JwtPayload;
  } catch {
    return null;
  }
};

const normalizePermissions = (value: string | string[] | undefined) => {
  if (!value) {
    return [];
  }

  if (Array.isArray(value)) {
    return value;
  }

  return value.split(' ').filter(Boolean);
};

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
  },
  getPermissions() {
    const token = this.getSession()?.accessToken;
    if (!token) {
      return [];
    }

    const payload = decodeJwtPayload(token);
    if (!payload) {
      return [];
    }

    const unique = new Set<string>([
      ...normalizePermissions(payload.permission),
      ...normalizePermissions(payload.permissions),
      ...normalizePermissions(payload.scp)
    ]);

    return [...unique];
  }
};
