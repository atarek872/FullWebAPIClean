import axios from 'axios';
import { env } from '@/config/env';
import { tokenManager } from '@/services/tokenManager';

const api = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 15000
});

let isRefreshing = false;
let waitingQueue: Array<(token: string | null) => void> = [];

const resolveQueue = (token: string | null) => {
  waitingQueue.forEach((cb) => cb(token));
  waitingQueue = [];
};

api.interceptors.request.use((config) => {
  const session = tokenManager.getSession();
  const tenantId = tokenManager.getTenantId();

  if (session?.accessToken) {
    config.headers.Authorization = `Bearer ${session.accessToken}`;
  }

  if (tenantId) {
    config.headers['X-Tenant-ID'] = tenantId;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config;

    if (error.response?.status !== 401 || original._retry) {
      return Promise.reject(error);
    }

    const session = tokenManager.getSession();
    const tenantId = tokenManager.getTenantId();
    if (!session?.refreshToken || !tenantId) {
      tokenManager.clearSession();
      return Promise.reject(error);
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        waitingQueue.push((token) => {
          if (!token) reject(error);
          else {
            original.headers.Authorization = `Bearer ${token}`;
            resolve(api(original));
          }
        });
      });
    }

    original._retry = true;
    isRefreshing = true;

    try {
      const { data } = await axios.post(`${env.apiBaseUrl}/api/auth/refresh`, {
        refreshToken: session.refreshToken,
        tenantId
      });

      const nextSession = { ...session, accessToken: data.accessToken, refreshToken: data.refreshToken };
      tokenManager.setSession(nextSession);
      resolveQueue(data.accessToken);

      original.headers.Authorization = `Bearer ${data.accessToken}`;
      return api(original);
    } catch (refreshError) {
      tokenManager.clearSession();
      resolveQueue(null);
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

export default api;
