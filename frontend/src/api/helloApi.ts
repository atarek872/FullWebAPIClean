import api from '@/api/httpClient';

export const helloApi = {
  getHello() {
    return api.get<{ message: string; timestamp: string }>('/api/hello');
  }
};
