import api from '@/api/httpClient';

export const SystemService = {
  root() {
    return api.get('/');
  },
  health() {
    return api.get('/health');
  }
};
